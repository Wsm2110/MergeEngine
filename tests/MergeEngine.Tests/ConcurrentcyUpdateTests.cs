using Xunit;
using MergeEngine;
using MergeEngine.Extensions;

namespace MergeEngine.Tests;

public class ConcurrentUpdateConflictTests
{
    [Fact]
    public void ConcurrentUpdatesWithDifferentClockCounts_ShouldTriggerMergeRule()
    {
        // Arrange
        var engine = new MergeEngine<UnitState>();
        engine.SetRule(x => x.Speed, MergeRules.MaxDouble()); // handle concurrency

        var a = new UnitState { Speed = 10 };
        var b = new UnitState { Speed = 20 };

        // Node A updates 3 times
        a.Update(o => o.Speed = 10, "A"); // A:1
        a.Update(o => o.Speed = 10, "A"); // A:2
        a.Update(o => o.Speed = 10, "A"); // A:3

        // Node B updates 10 times
        for (int i = 0; i < 10; i++)
        {
            b.Update(o => o.Speed = 20, "B"); // B:10
        }
        // Act — merge the two objects
        var result = engine.Merge(a, b);

        // Assert behavior
        Assert.Equal(MergeEngine.Core.VectorClockRelation.Concurrent, a.Clock.Compare(b.Clock));     // ensure test condition
        Assert.Equal(20, result.Speed);     // max(10,20) via rule
    }
}