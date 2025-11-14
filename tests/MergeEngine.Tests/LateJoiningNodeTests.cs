using MergeEngine.Extensions;

namespace MergeEngine.Tests;

public class LateJoiningNodeTests
{
    private MergeEngine<UnitState> CreateEngine() => new MergeEngine<UnitState>();

    [Fact]
    public void NewNodeJoiningAfterHeavyMerging_ShouldStillConverge()
    {
        var engine = CreateEngine();

        // Existing long-lived replicas
        var nodeA = new UnitState();
        var nodeB = new UnitState();

        // A and B merge and update many times
        for (int i = 0; i < 50; i++)
        {
            nodeA.Update(n => n.BlueForces.Add("A" + i), "Node-A");
            nodeB.Update(n => n.BlueForces.Add("B" + i), "Node-B");

            // occasional merges
            if (i % 3 == 0)
                nodeA = engine.Merge(nodeA, nodeB);
            else
                nodeB = engine.Merge(nodeB, nodeA);
        }

        // Late-joining node C
        var nodeC = new UnitState();
        nodeC.Update(n => n.BlueForces.Add("C-INIT"), "Node-C");

        // Node C merges with A and B
        nodeC = engine.Merge(nodeC, nodeA);
        nodeC = engine.Merge(nodeC, nodeB);

        // A and B merge C into their state too
        nodeA = engine.Merge(nodeA, nodeC);
        nodeB = engine.Merge(nodeB, nodeC);

        // All replicas should converge
        Assert.True(nodeA.BlueForces.SetEquals(nodeB.BlueForces));
        Assert.True(nodeB.BlueForces.SetEquals(nodeC.BlueForces));
    }

    [Fact]
    public void LateJoinerShouldPreserveOlderHistoryFromExistingNodes()
    {
        var engine = CreateEngine();
        engine.SetRule(v => v.IsArmed, MergeRules.OrBoolean());
        
        var a = new UnitState();
        var b = new UnitState();

        // A and B have a rich merge history
        for (int i = 1; i <= 20; i++)
        {
            a.Update(s => s.Speed = i, "Node-A");
            b.Update(s => s.Speed = i * 2, "Node-B");

            a = engine.Merge(a, b);
            b = engine.Merge(b, a);
        }

        // New node (C) joins
        var c = new UnitState();
        c.Update(s => s.IsArmed = true, "Node-C");

        // Merge C with historical nodes
        c = engine.Merge(c, a);

        // After joining, C should include historic values
        Assert.Equal(a.Speed, c.Speed);

        // C should retain its own update as well
        Assert.True(c.IsArmed);
    }


    [Fact]
    public void LateJoiningNodeShouldNotBreakVectorClockRelations()
    {
        var engine = new MergeEngine<UnitState>();
        engine.SetRule(v => v.Speed, MergeRules.MaxDouble());

        var a = new UnitState();   
        var b = new UnitState();

        for (int i = 0; i < 10; i++)
        {
            a.Update(x => x.Speed = 5 + i, "Node-A");
            b.Update(x => x.Speed = 10 + i, "Node-B");

            a = engine.Merge(a, b);
        }

        var c = new UnitState();

        //conflict
        c.Update(x => x.Speed = 999, "Node-C");

        // merge late joiner
        c = engine.Merge(c, a);

        Assert.Equal(999, c.Speed); // C wins (Concurrent -> local wins)
        Assert.Contains("Node-C", c.Clock.Versions.Keys);
        Assert.Contains("Node-A", c.Clock.Versions.Keys);
        Assert.Contains("Node-B", c.Clock.Versions.Keys);
    }


}