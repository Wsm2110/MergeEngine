using MergeEngine;
using MergeEngine.Contracts;
using MergeEngine.Core;
using MergeEngine.Tests.Models;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MergeEngine.Tests
{
    public class MergeRuleAttributeTests
    {
        [Fact]
        public void MergeEngine_ShouldApplyAttributeBasedRule()
        {
            var engine = new MergeEngine<AnnotatedState>();

            var a = new AnnotatedState { Score = 10, Name = "Alice" };
            var b = new AnnotatedState { Score = 50, Name = "Bob" };

            // Create concurrent updates so the rule executes
            a.Clock.Increment("A");  // A:1
            b.Clock.Increment("B");  // B:1 → concurrent with A:1

            var result = engine.Merge(a, b);

            // EXPECTATION 1 — Attribute rule (MaxInt) should execute
            Assert.Equal(50, result.Score);

            // EXPECTATION 2 — Name uses default LWW → remote wins in concurrent
            Assert.Equal("Bob", result.Name);
        }

        [Fact]
        public void Attribute_ShouldBeDiscoverableViaReflection()
        {
            var prop = typeof(AnnotatedState).GetProperty(nameof(AnnotatedState.Score));
            var attr = prop!.GetCustomAttribute<MergeRuleAttribute>();

            Assert.NotNull(attr);
            Assert.Equal(typeof(MaxIntMergeRule), attr!.RuleType);
        }

        [Fact]
        public void Engine_ShouldInstantiateAttributeRule()
        {
            var engine = new MergeEngine<AnnotatedState>();

            var prop = typeof(AnnotatedState).GetProperty(nameof(AnnotatedState.Score))!;
            var attr = prop.GetCustomAttribute<MergeRuleAttribute>()!;

            // Try creating rule instance
            var instance = Activator.CreateInstance(attr.RuleType);

            Assert.NotNull(instance);
            Assert.IsType<MaxIntMergeRule>(instance);
        }



        // ---------------------------------------------------------
        // TEST 1 — Attribute stores rule type
        // ---------------------------------------------------------
        [Fact]
        public void Constructor_ShouldStoreRuleType()
        {
            var attr = new MergeRuleAttribute(typeof(DummyRule));

            Assert.Equal(typeof(DummyRule), attr.RuleType);
        }

        // ---------------------------------------------------------
        // TEST 2 — Constructor throws on null
        // ---------------------------------------------------------
        [Fact]
        public void Constructor_ShouldThrowOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => new MergeRuleAttribute(null!));
        }

        // ---------------------------------------------------------
        // TEST 3 — Attribute can be applied to a property
        // ---------------------------------------------------------
        private class TestEntity
        {
            [MergeRule(typeof(DummyRule))]
            public int Value { get; set; }
        }

        [Fact]
        public void Attribute_ShouldBeRetrievableViaReflection()
        {
            var prop = typeof(TestEntity).GetProperty(nameof(TestEntity.Value))!;
            var attr = prop.GetCustomAttribute<MergeRuleAttribute>();

            Assert.NotNull(attr);
            Assert.Equal(typeof(DummyRule), attr!.RuleType);
        }

        // ---------------------------------------------------------
        // TEST 4 — Only property targets allowed
        // ---------------------------------------------------------
        [Fact]
        public void Attribute_ShouldOnlyTargetProperties()
        {
            var usage = typeof(MergeRuleAttribute)
                .GetCustomAttribute<AttributeUsageAttribute>()!;

            Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Property));
            Assert.False(usage.ValidOn.HasFlag(AttributeTargets.Field));
            Assert.False(usage.AllowMultiple);
        }

        // ---------------------------------------------------------
        // TEST 5 — Validate rule type implements IMergeRule<T>
        // (MergeEngine should do this check — we verify type validity)
        // ---------------------------------------------------------
        private class InvalidRule { }

        [Fact]
        public void RuleType_ShouldImplementIMergeRule()
        {
            var isValid = typeof(IMergeRule<>)
                .IsAssignableFromGeneric(typeof(DummyRule));

            Assert.True(isValid);

            var isInvalid = typeof(IMergeRule<>)
                .IsAssignableFromGeneric(typeof(InvalidRule));

            Assert.False(isInvalid);
        }

        // ----------------- SUPPORT TYPES ----------------------
        public class DummyRule : IMergeRule<int>
        {
            public int Merge(int l, int r, VectorClock lc, VectorClock rc) => r;
        }
    }

    // Helper to check open generic interface compatibility
    internal static class TypeExtensions
    {
        public static bool IsAssignableFromGeneric(this Type genericType, Type concrete)
        {
            return concrete.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericType);
        }
    }
}