using MergeEngine.Contracts;
using MergeEngine.Core;

namespace MergeEngine.Tests.Models
{
    public sealed class AnnotatedState : IMergeObject
    {
        public VectorClock Clock { get; set; } = new();

        // This property uses a CUSTOM merge rule
        [MergeRule(typeof(MaxIntMergeRule))]
        public int Score { get; set; }

        // This property uses the DEFAULT LWW rule
        public string Name { get; set; }

        public void Touch(string nodeId) => Clock.Increment(nodeId);
    }

    /// <summary>
    /// A simple custom merge rule: choose the MAX integer.
    /// </summary>
    public sealed class MaxIntMergeRule : IMergeRule<int>
    {
        public int Merge(int local, int remote, VectorClock lc, VectorClock rc)
            => Math.Max(local, remote);
    }
}
