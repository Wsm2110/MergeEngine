using MergeEngine.Core;

namespace MergeEngine.Tests;

public class VectorClockMathTests
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
}