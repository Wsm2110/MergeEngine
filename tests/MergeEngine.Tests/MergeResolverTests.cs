using MergeEngine;
using MergeEngine.Contracts;
using MergeEngine.Tests.Models;

namespace MergeEngine.Tests;

public class MergeResolverTests
{
    private sealed class TestResolver : IMergeResolver<AnnotatedState>
    {
        public void RegisterRules(MergeEngine<AnnotatedState> engine)
        {
            engine.SetRule(x => x.Score,
                MergeRules.MaxInt());
        }
    }

    [Fact]
    public void ResolverShouldInjectCustomRule()
    {
        var resolver = new TestResolver();
        var engine = new MergeEngine<AnnotatedState>(resolver);

        var a = new AnnotatedState { Score = 10 };
        var b = new AnnotatedState { Score = 20 };

        a.Clock.Increment("A");
        b.Clock.Increment("B"); // concurrent

        var result = engine.Merge(a, b);

        Assert.Equal(20, result.Score);
    }
}
