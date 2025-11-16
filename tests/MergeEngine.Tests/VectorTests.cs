using MergeEngine.Core;
using MergeEngine.Tests.Models;

namespace MergeEngine.Tests;

public class VectorClockTests
{
    [Fact]
    public void Reflexivity_Clock_Equals_Itself()
    {
        var vc = new VectorClock();
        vc.Increment("A");

        Assert.Equal(VectorClockRelation.Equal, vc.Compare(vc));
    }

    [Fact]
    public void Antisymmetry_Ordering_Is_Reversed()
    {
        var a = new VectorClock();
        var b = new VectorClock();

        a.Increment("A"); // [A:1]
        b.Increment("A");
        b.Increment("A"); // [A:2]

        Assert.Equal(VectorClockRelation.Before, a.Compare(b));
        Assert.Equal(VectorClockRelation.After, b.Compare(a));
    }

    [Fact]
    public void Concurrent_Is_Symmetric()
    {
        var a = new VectorClock();
        var b = new VectorClock();

        a.Increment("A"); // [A:1]
        b.Increment("B"); // [B:1]

        Assert.Equal(VectorClockRelation.Concurrent, a.Compare(b));
        Assert.Equal(VectorClockRelation.Concurrent, b.Compare(a));
    }

    [Fact]
    public void Monotonicity_Always_Increases()
    {
        var vc = new VectorClock();
        vc.Increment("A");
        var original = vc.Versions["A"];

        vc.Increment("A");

        Assert.True(vc.Versions["A"] > original);
    }

    [Fact]
    public void MergeClock_Is_Superset_Of_Both_Clocks()
    {
        var a = new VectorClock();
        var b = new VectorClock();

        a.Increment("A");
        b.Increment("B");
        b.Increment("B");

        var merged = a.Merge(b);

        Assert.Equal(1, merged.Versions["A"]);
        Assert.Equal(2, merged.Versions["B"]);
    }

    [Fact]
    public void Antisymmetry_Order_Is_Reversed()
    {
        var a = new VectorClock();
        var b = new VectorClock();

        a.Increment("A");        // [A:1]
        b.Increment("A");
        b.Increment("A");        // [A:2]

        Assert.Equal(VectorClockRelation.Before, a.Compare(b));
        Assert.Equal(VectorClockRelation.After, b.Compare(a));
    }

    [Fact]
    public void Concurrency_Is_Symmetric()
    {
        var a = new VectorClock();
        var b = new VectorClock();

        a.Increment("A");        // [A:1]
        b.Increment("B");        // [B:1]

        Assert.Equal(VectorClockRelation.Concurrent, a.Compare(b));
        Assert.Equal(VectorClockRelation.Concurrent, b.Compare(a));
    }

    [Fact]
    public void MergeClock_Contains_MaxElementsAcrossNodes()
    {
        var a = new VectorClock();
        var b = new VectorClock();

        a.Increment("A");        // [A:1]
        b.Increment("B");        // [B:1]
        b.Increment("B");        // [B:2]

        var merged = a.Merge(b);

        Assert.Equal(1, merged.Versions["A"]);
        Assert.Equal(2, merged.Versions["B"]);
    }

    [Fact]
    public void Monotonicity_IncrementsAlwaysIncreaseValue()
    {
        var vc = new VectorClock();
        vc.Increment("X");
        var before = vc.Versions["X"];

        vc.Increment("X");
        var after = vc.Versions["X"];

        Assert.True(after > before);
    }

    [Fact]
    public void EqualClocks_Are_Equal()
    {
        var a = new UnitState();
        var b = new UnitState();

        a.Clock.Increment("A");  // [A:1]
        b.Clock.Increment("A");  // [A:1]

        Assert.Equal(VectorClockRelation.Equal, a.Clock.Compare(b.Clock));
    }

    [Fact]
    public void Merging_EqualClocks_Produces_IdenticalClock()
    {
        var a = new UnitState { Callsign = "Alpha" };
        var b = new UnitState { Callsign = "Alpha" };

        a.Clock.Increment("A");  // [A:1]
        b.Clock.Increment("A");  // [A:1]

        var engine = new MergeEngine<UnitState>();
        var merged = engine.Merge(a, b);

        Assert.Equal("Alpha", merged.Callsign);
        Assert.Equal(1, merged.Clock.Versions["A"]);
        Assert.Single(merged.Clock.Versions);
    }

    [Fact]
    public void EqualClocks_DoNotIntroduceNewClockEntries()
    {
        var a = new UnitState();
        var b = new UnitState();

        a.Clock.Increment("A");
        b.Clock.Increment("A");

        var engine = new MergeEngine<UnitState>();
        var merged = engine.Merge(a, b);

        Assert.False(merged.Clock.Versions.ContainsKey("B")); // no phantom nodes
    }

    [Fact]
    public void EqualClocks_Are_Idempotent_WhenMergedMultipleTimes()
    {
        var a = new UnitState();
        a.Clock.Increment("A");

        var engine = new MergeEngine<UnitState>();
        var merged1 = engine.Merge(a, a);
        var merged2 = engine.Merge(merged1, a);

        Assert.Equal(VectorClockRelation.Equal, merged1.Clock.Compare(merged2.Clock));
    }

    [Fact]
    public void EqualClockResolution_DeterministicallyChoosesRemoteValue()
    {
        var local = new UnitState { Callsign = "Local" };
        var remote = new UnitState { Callsign = "Remote" };

        local.Clock.Increment("A");     // [A:1]
        remote.Clock.Increment("A");    // [A:1]

        var engine = new MergeEngine<UnitState>();
        var result = engine.Merge(local, remote);

        // Deterministic branch: Equal → remote chosen.
        Assert.Equal("Remote", result.Callsign);
    }

    [Fact]
    public void EqualClocks_MergeAgain_BiDirectional_ProducesIdenticalObjects()
    {
        var a = new UnitState { Callsign = "Alpha", Speed = 10, IsArmed = true };
        var b = new UnitState { Callsign = "Alpha", Speed = 10, IsArmed = true };

        // Both have identical causal history
        a.Clock.Increment("A");   // [A:1]
        b.Clock.Increment("A");   // [A:1]

        var engine = new MergeEngine<UnitState>();

        // First merge results in a fully converged object
        var merged1 = engine.Merge(a, b);

        // Merge again from both directions
        var mergedFromOriginal = engine.Merge(merged1, a);
        var mergedReverse = engine.Merge(merged1, b);

        // All resulting objects should be logically identical
        Assert.Equal(mergedFromOriginal.Callsign, mergedReverse.Callsign);
        Assert.Equal(mergedFromOriginal.Speed, mergedReverse.Speed);
        Assert.Equal(mergedFromOriginal.IsArmed, mergedReverse.IsArmed);
        Assert.Equal(mergedFromOriginal.BlueForces, mergedReverse.BlueForces);

        // Vector clock must remain equal and unchanged
        Assert.Equal(VectorClockRelation.Equal, mergedFromOriginal.Clock.Compare(mergedReverse.Clock));
        Assert.Equal(merged1.Clock.Versions, mergedFromOriginal.Clock.Versions);
        Assert.Equal(merged1.Clock.Versions, mergedReverse.Clock.Versions);
    }

    [Fact]
    public void EqualClocks_Imply_EqualState()
    {
        var a = new UnitState { Callsign = "Alpha", Speed = 10, IsArmed = false };
        var b = new UnitState { Callsign = "Alpha", Speed = 10, IsArmed = false };

        a.Clock.Increment("A");
        b.Clock.Increment("A");

        Assert.Equal(VectorClockRelation.Equal, a.Clock.Compare(b.Clock));
        Assert.Equal(a.Callsign, b.Callsign);
        Assert.Equal(a.Speed, b.Speed);
        Assert.Equal(a.IsArmed, b.IsArmed);
    }
}