using MergeEngine;
using MergeEngine.Tests;
using Xunit;

namespace MergeEngine.Tests;

public class MergeEngineCausalityTests
{
    private MergeEngine<UnitState> CreateEngine() => new MergeEngine<UnitState>();

    [Fact]
    public void Merge_Before_RemoteShouldWin()
    {
        var engine = CreateEngine();

        var local = new UnitState { Speed = 10 };
        var remote = new UnitState { Speed = 20 };

        local.Clock.Increment("A");       // [A:1]
        remote.Clock.Increment("A");
        remote.Clock.Increment("A");      // [A:2] => remote AFTER

        var result = engine.Merge(local, remote);

        Assert.Equal(20, result.Speed);
    }

    [Fact]
    public void Merge_After_LocalShouldWin()
    {
        var engine = CreateEngine();

        var local = new UnitState { Speed = 15 };
        var remote = new UnitState { Speed = 30 };

        remote.Clock.Increment("B");     // [B:1]
        local.Clock.Increment("B");      // [B:1]
        local.Clock.Increment("B");      // [B:2] => local AFTER

        var result = engine.Merge(local, remote);

        Assert.Equal(15, result.Speed);
    }

    [Fact]
    public void Merge_Equal_RemoteOverridesForDeterminism()
    {
        var engine = CreateEngine();

        var local = new UnitState { Speed = 25 };
        var remote = new UnitState { Speed = 40 };

        local.Clock.Increment("C");      // [C:1]
        remote.Clock.Increment("C");     // [C:1] equal

        var result = engine.Merge(local, remote);

        Assert.Equal(40, result.Speed);
    }

    [Fact]
    public void Merge_Concurrent_UsesMergeRules()
    {
        var engine = CreateEngine();
        engine.SetRule(x => x.Speed, MergeRules.MaxDouble());

        var local = new UnitState { Speed = 60 };
        var remote = new UnitState { Speed = 80 };

        local.Clock.Increment("L");      // [L:1]
        remote.Clock.Increment("R");     // [R:1] concurrent

        var result = engine.Merge(local, remote);

        Assert.Equal(80, result.Speed);
    }
}

