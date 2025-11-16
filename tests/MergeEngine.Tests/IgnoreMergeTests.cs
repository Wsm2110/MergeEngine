using MergeEngine;
using MergeEngine.Tests.Models;
using Xunit;

namespace MergeEngine.Tests;

public class IgnoreMergeTests
{
    [Fact]
    public void IgnoreMergeAttribute_ExcludesPropertyFromMerge()
    {
        var engine = new MergeEngine<UnitState>();

        var local = new UnitState
        {
            DebugInfo = "LOCAL",
            Speed = 10
        };

        var remote = new UnitState
        {
            DebugInfo = "REMOTE",
            Speed = 20
        };

        var result = engine.MergeInto(local, remote);

        Assert.Equal("LOCAL", result.DebugInfo); // untouched
        Assert.Equal(20, result.Speed);          // merged normally
    }

    [Fact]
    public void IgnoreMergeAttribute_ExcludesPropertyFromMerge_partTwo()
    {
        var engine = new MergeEngine<UnitState>();

        var local = new UnitState
        {
            DebugInfo = "LOCAL",
            Speed = 10
        };

        var remote = new UnitState
        {
            DebugInfo = "REMOTE",
            Speed = 20
        };

        var result = engine.Merge(local, remote);

        Assert.Equal("LOCAL", result.DebugInfo); // untouched
        Assert.Equal(20, result.Speed);          // merged normally
    }
}

