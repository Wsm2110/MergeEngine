using MergeEngine;
using MergeEngine.Core;
using MergeEngine.Extensions;
using MergeEngine.Tests.Models;

namespace MergeEngine.Tests;

public class MergeEngineTests
{
    private readonly string NodeA = "nodeA";
    private readonly string NodeB = "nodeB";

    private MergeEngine<UnitState> CreateEngine()
    {
        var engine = new MergeEngine<UnitState>();
        engine.SetRule(x => x.Speed, MergeRules.MaxDouble());
        engine.SetRule(x => x.BlueForces, MergeRules.SetUnion<string>());
        engine.SetRule(x => x.IsArmed, MergeRules.OrBoolean());
        return engine;
    }

    [Fact]
    public void Merge_LocalNull_ReturnsRemote()
    {
        var engine = CreateEngine();
        var remote = new UnitState { Speed = 50 };
        remote.Clock.Increment("A");

        var result = engine.Merge(null, remote);

        Assert.Equal(50, result.Speed);
        Assert.Equal(1, result.Clock.Versions["A"]);
    }

    [Fact]
    public void Merge_RemoteNull_ReturnsLocal()
    {
        var engine = CreateEngine();
        var local = new UnitState { Speed = 30 };
        local.Clock.Increment("A");

        var result = engine.Merge(local, null);

        Assert.Equal(30, result.Speed);
        Assert.Equal(1, result.Clock.Versions["A"]);
    }

    [Fact]
    public void Merge_Before_RemoteOverwritesLocal()
    {
        var engine = CreateEngine();
        var local = new UnitState { Speed = 10, IsArmed = false };
        var remote = new UnitState { Speed = 20, IsArmed = true };

        local.Clock.Increment("A");     // A:1
        remote.Clock.Increment("A");    // A:1
        remote.Clock.Increment("A");    // A:2 => remote AFTER

        var result = engine.Merge(local, remote);

        Assert.Equal(20, result.Speed);
        Assert.True(result.IsArmed);
    }

    [Fact]
    public void Merge_After_LocalOverwritesRemote()
    {
        var engine = CreateEngine();
        var local = new UnitState { Speed = 45, IsArmed = true };
        var remote = new UnitState { Speed = 15, IsArmed = false };

        remote.Clock.Increment("B");   // B:1
        local.Clock.Increment("B");    // B:1
        local.Clock.Increment("B");    // B:2 => local AFTER

        var result = engine.Merge(local, remote);

        Assert.Equal(45, result.Speed);
        Assert.True(result.IsArmed);
    }

    [Fact]
    public void Merge_Equal_TakesRemoteDeterministically()
    {
        var engine = CreateEngine();
        var local = new UnitState { Speed = 30 };
        var remote = new UnitState { Speed = 80 };

        local.Clock.Increment("X");     // X:1
        remote.Clock.Increment("X");    // X:1 => Equal

        var result = engine.Merge(local, remote);

        Assert.Equal(80, result.Speed);
    }

    [Fact]
    public void Merge_Concurrent_UsesCustomRules()
    {
        var engine = CreateEngine();
        var local = new UnitState { Speed = 50, IsArmed = false, BlueForces = new HashSet<string> { "A" } };
        var remote = new UnitState { Speed = 70, IsArmed = true, BlueForces = new HashSet<string> { "B" } };

        local.Clock.Increment("A");
        remote.Clock.Increment("B");

        var result = engine.Merge(local, remote);

        Assert.Equal(70, result.Speed);                             // MaxDouble
        Assert.True(result.IsArmed);                                // ORBoolean => true
        Assert.Contains("A", result.BlueForces);                    // Set union
        Assert.Contains("B", result.BlueForces);
    }

    [Fact]
    public void VectorClock_MergeTakesMaxPerNode()
    {
        var a = new VectorClock();
        var b = new VectorClock();

        a.Increment("node1");  // 1
        a.Increment("node1");  // 2
        b.Increment("node1");  // 1
        b.Increment("node2");  // 1
        b.Increment("node2");  // 2

        var merged = a.Merge(b);

        Assert.Equal(2, merged.Versions["node1"]);
        Assert.Equal(2, merged.Versions["node2"]);
    }

    [Fact]
    public void Merge_LocalAfterRemote_ShouldTakeLocalValue()
    {
        var engine = CreateEngine();

        var local = new UnitState { Callsign = "Alpha", Speed = 45 };
        local.Clock.Increment(NodeA);

        var remote = new UnitState { Callsign = "Alpha", Speed = 10 };
        remote.Clock.Increment(NodeA);      // local clock now AFTER because we increment more
        local.Clock.Increment(NodeA);

        var merged = engine.Merge(local, remote);

        Assert.Equal(45, merged.Speed);
    }

    [Fact]
    public void Merge_RemoteAfterLocal_ShouldTakeRemoteValue()
    {
        var engine = CreateEngine();

        var local = new UnitState { Callsign = "Alpha", Speed = 10 };
        local.Clock.Increment(NodeA);

        var remote = new UnitState { Callsign = "Alpha", Speed = 50 };
        remote.Clock.Increment(NodeA);
        remote.Clock.Increment(NodeA);

        var merged = engine.Merge(local, remote);

        Assert.Equal(50, merged.Speed);
    }

    [Fact]
    public void Merge_ConcurrentUpdates_ShouldApplyCustomRules()
    {
        var engine = CreateEngine();

        var local = new UnitState { IsArmed = false, Speed = 40 };
        var remote = new UnitState { IsArmed = true, Speed = 35 };

        // Simulate true concurrency
        local.Clock.Increment(NodeA);
        remote.Clock.Increment(NodeB);

        var merged = engine.Merge(local, remote);

        Assert.True(merged.IsArmed);         // OR rule
        Assert.Equal(40, merged.Speed);      // MaxDouble rule
    }

    [Fact]
    public void Merge_UnionBehaviorForBlueForces()
    {
        var engine = CreateEngine();

        var local = new UnitState { BlueForces = new HashSet<string> { "A", "B" } };
        var remote = new UnitState { BlueForces = new HashSet<string> { "C" } };

        local.Clock.Increment(NodeA);
        remote.Clock.Increment(NodeB);

        var merged = engine.Merge(local, remote);

        Assert.Contains("A", merged.BlueForces);
        Assert.Contains("B", merged.BlueForces);
        Assert.Contains("C", merged.BlueForces);
        Assert.Equal(3, merged.BlueForces.Count);
    }

    [Fact]
    public void Update_ShouldApplyActionAndIncrementVectorClock()
    {
        var obj = new UnitState();

        obj.Update(o => o.Callsign = "Alpha", NodeA);

        Assert.Equal("Alpha", obj.Callsign);
        Assert.Equal(1, obj.Clock.Versions[NodeA]);
    }

    [Fact]
    public void Update_MultipleNodeUpdates_ShouldTrackHistoryIndependently()
    {
        var obj = new UnitState();

        obj.Update(o => o.Callsign = "A", NodeA);
        obj.Update(o => o.Callsign = "A1", NodeA);
        obj.Update(o => o.Callsign = "A1-B1", NodeB);

        Assert.Equal(2, obj.Clock.Versions[NodeA]);
        Assert.Equal(1, obj.Clock.Versions[NodeB]);
    }

    [Fact]
    public void Update_ExceptionDuringAction_ShouldNotIncrementClock()
    {
        var obj = new UnitState();

        Assert.Throws<InvalidOperationException>(() =>
            obj.Update(_ => throw new InvalidOperationException(), NodeA));

        Assert.False(obj.Clock.Versions.ContainsKey(NodeA));
    }

    [Fact]
    public void CaseBefore_RemoteShouldWin()
    {
        var engine = CreateEngine();

        var local = new UnitState { Speed = 10, IsArmed = false };
        var remote = new UnitState { Speed = 20, IsArmed = true };

        local.Clock.Increment("A");        // vA:1
        remote.Clock.Increment("A");      // vA:1
        remote.Clock.Increment("A");      // vA:2  => remote AFTER

        var result = engine.Merge(local, remote);

        Assert.Equal(20, result.Speed);
        Assert.True(result.IsArmed);
    }

    [Fact]
    public void CaseAfter_LocalShouldWin()
    {
        var engine = CreateEngine();

        var local = new UnitState { Callsign = "a",  Speed = 15, IsArmed = true };
        var remote = new UnitState { Callsign = "B",  Speed = 30, IsArmed = false };

        remote.Clock.Increment("B");       // vB:1
        local.Clock.Increment("B");        // vB:1
        local.Clock.Increment("B");        // vB:2 => local AFTER

        var result = engine.Merge(local, remote);

        Assert.Equal(15, result.Speed);
        Assert.True(result.IsArmed);
    }

    [Fact]
    public void CaseEqual_ShouldUseRemoteForDeterminism()
    {
        var engine = CreateEngine();

        var local = new UnitState { Speed = 25, IsArmed = true };
        var remote = new UnitState { Speed = 25, IsArmed = false };

        local.Clock.Increment("X");        // vX:1
        remote.Clock.Increment("X");       // vX:1 => Equal

        var result = engine.Merge(local, remote);

        Assert.Equal(25, result.Speed);
        Assert.False(result.IsArmed);      // Remote chosen by LWW determinism
    }

    [Fact]
    public void CaseConcurrent_ShouldApplyCustomRules()
    {
        var engine = CreateEngine();

        var local = new UnitState
        {
            Speed = 40,
            IsArmed = false,
            BlueForces = new HashSet<string> { "A" }
        };

        var remote = new UnitState
        {
            Speed = 50,
            IsArmed = true,
            BlueForces = new HashSet<string> { "B" }
        };

        local.Clock.Increment("A");        // vA:1
        remote.Clock.Increment("B");       // vB:1 => Concurrent

        var result = engine.Merge(local, remote);

        // From MaxDouble merge rule
        Assert.Equal(50, result.Speed);

        // From OrBoolean merge rule
        Assert.True(result.IsArmed);

        // From SetUnion merge rule
        Assert.Contains("A", result.BlueForces);
        Assert.Contains("B", result.BlueForces);
        Assert.Equal(2, result.BlueForces.Count);
    }

    [Fact]
    public void Update_ShouldApplyChangeToObject()
    {
        var obj = new UnitState();

        obj.Update(o => o.Callsign = "Alpha", NodeA);

        Assert.Equal("Alpha", obj.Callsign);
    }

    [Fact]
    public void Update_ShouldIncrementClockForNode()
    {
        var obj = new UnitState();

        obj.Update(o => o.Callsign = "Alpha", NodeA);

        Assert.Equal(1, obj.Clock.Versions[NodeA]);
    }

    [Fact]
    public void Update_MultipleUpdates_ShouldAccumulateClock()
    {
        var obj = new UnitState();

        obj.Update(o => o.Callsign = "Alpha", NodeA);
        obj.Update(o => o.Callsign = "Bravo", NodeA);

        Assert.Equal("Bravo", obj.Callsign);
        Assert.Equal(2, obj.Clock.Versions[NodeA]);
    }

    [Fact]
    public void Update_ShouldTrackIndependentNodes()
    {
        var obj = new UnitState();

        obj.Update(o => o.Callsign = "Alpha", NodeA);
        obj.Update(o => o.Callsign = "Alpha1", NodeA);
        obj.Update(o => o.Callsign = "Alpha1-B1", NodeB);

        Assert.Equal(2, obj.Clock.Versions[NodeA]);
        Assert.Equal(1, obj.Clock.Versions[NodeB]);
    }

    [Fact]
    public void Update_ShouldThrowIfActionIsNull()
    {
        var obj = new UnitState();

        Assert.Throws<ArgumentNullException>(() =>
            obj.Update(null!, NodeA));
    }

    [Fact]
    public void Update_ShouldThrowIfNodeIdIsNull()
    {
        var obj = new UnitState();

        Assert.Throws<ArgumentNullException>(() =>
            obj.Update(o => o.Callsign = "Alpha", null!));
    }

    [Fact]
    public void Update_ShouldNotIncrementClockIfUpdateThrows()
    {
        var obj = new UnitState();

        Assert.Throws<InvalidOperationException>(() =>
            obj.Update(_ => throw new InvalidOperationException(), NodeA));

        Assert.False(obj.Clock.Versions.ContainsKey(NodeA));
    }

    [Fact]
    public void MergeInto_Before_RemoteWins()
    {
        var local = new UnitState { Speed = 10, IsArmed = false };
        var remote = new UnitState { Speed = 20, IsArmed = true };

        local.Clock.Increment("A");        // A:1
        remote.Clock.Increment("A");       // A:1
        remote.Clock.Increment("A");       // A:2 (remote AFTER)

        var engine = CreateEngine();

        engine.MergeInto(local, remote);  // update local in place

        Assert.Equal(20, local.Speed);
        Assert.Equal(true, local.IsArmed);
    }

    [Fact]
    public void MergeInto_After_LocalWins()
    {
        var local = new UnitState { Speed = 15, IsArmed = true };
        var remote = new UnitState { Speed = 30, IsArmed = false };

        remote.Clock.Increment("B");       // B:1
        local.Clock.Increment("B");        // B:1
        local.Clock.Increment("B");        // B:2 (local AFTER)

        var engine = CreateEngine();

        engine.MergeInto(local, remote);

        Assert.Equal(15, local.Speed);
        Assert.True(local.IsArmed);
    }

    [Fact]
    public void MergeInto_Equal_UsesRemoteValue()
    {
        var local = new UnitState { Speed = 25, IsArmed = true };
        var remote = new UnitState { Speed = 999, IsArmed = false };

        local.Clock.Increment("X");        // X:1
        remote.Clock.Increment("X");       // X:1 (Equal)

        var engine = CreateEngine();

        engine.MergeInto(local, remote);

        Assert.Equal(999, local.Speed);    // deterministic remote win
        Assert.False(local.IsArmed);
    }

    [Fact]
    public void MergeInto_Concurrent_UsesMergeRules()
    {
        var local = new UnitState { Speed = 40, IsArmed = false, BlueForces = new HashSet<string> { "A" } };
        var remote = new UnitState { Speed = 50, IsArmed = true, BlueForces = new HashSet<string> { "B" } };

        // concurrent
        local.Clock.Increment("A");        // A:1
        remote.Clock.Increment("B");       // B:1

        var engine = CreateEngine();

        // configure merge rule for BlueForces
        engine.SetRule(u => u.BlueForces, MergeRules.SetUnion<string>());

        engine.MergeInto(local, remote);

        Assert.Contains("A", local.BlueForces);
        Assert.Contains("B", local.BlueForces);
    }

    [Fact]
    public void MergeInto_ReturnsSameInstanceReference()
    {
        var local = new UnitState();
        var remote = new UnitState();
        local.Clock.Increment("A");
        remote.Clock.Increment("A");

        var engine = CreateEngine();

        var returned = engine.MergeInto(local, remote);

        Assert.Same(local, returned);
    }

    [Fact]
    public void Compare_WhenClocksAreIdentical_ShouldReturnEqual()
    {
        // Arrange
        var clockA = new VectorClock();
        var clockB = new VectorClock();

        clockA.Increment("Node1");   // Node1:1
        clockA.Increment("Node2");   // Node2:1

        clockB.Increment("Node1");   // Node1:1
        clockB.Increment("Node2");   // Node2:1

        // Act
        var relation = clockA.Compare(clockB);

        // Assert
        Assert.Equal(VectorClockRelation.Equal, relation);
    }

    [Fact]
    public void Merge_Before_RemoteWins()
    {
        var engine = new MergeEngine<UnitState>();

        var local = new UnitState { Speed = 10 };
        var remote = new UnitState { Speed = 20 };

        local.Clock.Increment("A");    // [A:1]
        remote.Clock.Increment("A");   // [A:1]
        remote.Clock.Increment("A");   // [A:2] => remote AFTER

        var result = engine.Merge(local, remote);

        Assert.Equal(20, result.Speed);   // mathematical expected outcome
    }

    [Fact]
    public void Merge_After_LocalWins()
    {
        var engine = new MergeEngine<UnitState>();

        var local = new UnitState { Speed = 15 };
        var remote = new UnitState { Speed = 30 };

        remote.Clock.Increment("B");        // [B:1]
        local.Clock.Increment("B");         // [B:1]
        local.Clock.Increment("B");         // [B:2] => local AFTER

        var result = engine.Merge(local, remote);

        Assert.Equal(15, result.Speed);
    }

    [Fact]
    public void Merge_Concurrent_UsesCustomRule()
    {
        var engine = new MergeEngine<UnitState>();
        engine.SetRule(x => x.Speed, MergeRules.MaxDouble());

        var local = new UnitState { Speed = 40 };
        var remote = new UnitState { Speed = 50 };

        local.Clock.Increment("L");         // [L:1]
        remote.Clock.Increment("R");        // [R:1] => concurrent

        var result = engine.Merge(local, remote);

        Assert.Equal(50, result.Speed);  // MAX rule
    }
}