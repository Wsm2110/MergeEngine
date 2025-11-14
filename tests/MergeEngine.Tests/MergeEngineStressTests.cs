using MergeEngine;
using MergeEngine.Extensions;

namespace MergeEngine.Tests;

public class MergeEngineStressTests
{
    [Fact]
    public void StressConflictStormStillConverges()
    {
        var rnd = new Random();
        var engine = new MergeEngine<UnitState>();

        var replicas = new List<UnitState> { new(), new(), new(), new(), new() };

        for (int i = 0; i < 8000; i++)
        {
            var idx = rnd.Next(replicas.Count);
            replicas[idx].Update(o => o.BlueForces.Add($"BF-{rnd.Next(100)}"), $"N-{idx}");
        }

        for (int i = 0; i < 10000; i++)
        {
            var l = rnd.Next(replicas.Count);
            var r = rnd.Next(replicas.Count);
            replicas[l] = engine.Merge(replicas[l], replicas[r]);
        }

        for (int i = 1; i < replicas.Count; i++)
            Assert.True(replicas[0].BlueForces.SetEquals(replicas[i].BlueForces));
    }

    [Fact]
    public void MergeOperationIsAssociativeCommutativeAndIdempotent()
    {
        var engine = new MergeEngine<UnitState>();

        // Register correct CRDT rule for BlueForces
        engine.SetRule(x => x.BlueForces, MergeRules.SetUnion<string>());

        var a = new UnitState();
        var b = new UnitState();
        var c = new UnitState();

        a.Update(o => o.BlueForces.Add("X"), "A");
        b.Update(o => o.BlueForces.Add("Y"), "B");
        c.Update(o => o.BlueForces.Add("Z"), "C");

        // Commutative
        Assert.True(engine.Merge(a, b).BlueForces.SetEquals(engine.Merge(b, a).BlueForces));

        // Associative
        var leftAssoc = engine.Merge(a, engine.Merge(b, c));
        var rightAssoc = engine.Merge(engine.Merge(a, b), c);
        Assert.True(leftAssoc.BlueForces.SetEquals(rightAssoc.BlueForces));

        // Idempotent
        Assert.True(a.BlueForces.SetEquals(engine.Merge(a, a).BlueForces));
    }

    [Fact]
    public void ReplicasConvergeAfterNetworkPartitionHeals()
    {
        var rnd = new Random();
        var nodes = new[] { "A", "B", "C", "D" };
        var engine = new MergeEngine<UnitState>();

        var replicas = new List<UnitState> { new(), new(), new(), new() };

        // Partition groups: [A,B] and [C,D]
        var group1 = new[] { replicas[0], replicas[1] };
        var group2 = new[] { replicas[2], replicas[3] };

        // Phase 1 — isolated updates inside partition
        for (int i = 0; i < 300; i++)
        {
            group1[rnd.Next(2)].Update(o => o.BlueForces.Add($"G1-{i}"), $"A");
            group2[rnd.Next(2)].Update(o => o.BlueForces.Add($"G2-{i}"), $"C");
        }

        // Phase 2 — internal merging inside each partition
        for (int i = 0; i < 500; i++)
        {
            group1[0] = engine.Merge(group1[0], group1[1]);
            group1[1] = engine.Merge(group1[1], group1[0]);

            group2[0] = engine.Merge(group2[0], group2[1]);
            group2[1] = engine.Merge(group2[1], group2[0]);
        }

        // Phase 3 — simulate reconnection
        for (int i = 0; i < 1500; i++)
        {
            var left = rnd.Next(replicas.Count);
            var right = rnd.Next(replicas.Count);
            replicas[left] = engine.Merge(replicas[left], replicas[right]);
        }

        // Assert eventual convergence
        for (int i = 1; i < replicas.Count; i++)
            Assert.True(replicas[0].BlueForces.SetEquals(replicas[i].BlueForces));
    }

    [Fact]
    public void AllReplicasEventuallyConverge()
    {
        var rnd = new Random();
        var nodes = new[] { "A", "B", "C", "D" };
        var engine = new MergeEngine<UnitState>();

        // Create 4 replicas of the same logical object
        var replicas = new List<UnitState>
            {
                new(), new(), new(), new()
            };

        // -------------------------
        // PHASE 1 — Perform random isolated updates (no merging)
        // -------------------------
        for (int i = 0; i < 500; i++)
        {
            var idx = rnd.Next(replicas.Count);
            var node = nodes[rnd.Next(nodes.Length)];

            replicas[idx].Update(obj =>
            {
                obj.Speed = rnd.Next(0, 200);
                obj.IsArmed = rnd.Next(0, 2) == 1;
                obj.BlueForces.Add($"BF-{rnd.Next(0, 5)}");
            }, node);
        }

        // -------------------------
        // PHASE 2 — Repeated merge gossip until convergence
        // -------------------------
        for (int i = 0; i < 5000; i++)
        {
            var left = rnd.Next(replicas.Count);
            var right = rnd.Next(replicas.Count);

            replicas[left] = engine.Merge(replicas[left], replicas[right]);
            replicas[right] = engine.Merge(replicas[right], replicas[left]);
        }

        // -------------------------
        // ASSERT — Strong eventual consistency
        // -------------------------
        for (int i = 1; i < replicas.Count; i++)
        {
            Assert.Equal(replicas[0].Speed, replicas[i].Speed);
            Assert.Equal(replicas[0].IsArmed, replicas[i].IsArmed);
            Assert.True(replicas[0].BlueForces.SetEquals(replicas[i].BlueForces));
        }
    }
}