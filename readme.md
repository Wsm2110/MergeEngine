# MergeEngine – Deterministic Distributed Object Merging with Vector Clocks

MergeEngine is a lightweight C# library for **merging distributed object state deterministically** using **Vector Clocks** and **per-property conflict resolution rules**.

It is designed for multi-node, multi-master, eventually consistent systems where objects are updated concurrently across replicas.

## Why MergeEngine?

Typical LWW (Last-Write-Wins) systems based on timestamps fail because clocks drift and messages reorder. Vector clocks solve this by tracking causal history across nodes.

## Core Features
- Per-property CRDT-style merge rules
- Vector clock causality detection
- Attribute-based merge rule injection
- Resolver module support
- In-place or immutable merge operations

## Attribute Example
```csharp
public class VehicleState : IMergeObject
{
    public VectorClock Clock { get; set; } = new();

    [MergeRule(typeof(SetUnionRule<string>))]
    public HashSet<string> ActiveSensors { get; set; }

    [MergeRule(typeof(OrBooleanRule))]
    public bool EngineOn { get; set; }

    public double Speed { get; set; }
}
```

## Resolver Example
```csharp
public class VehicleMergeResolver : IMergeResolver
{
    public void RegisterRules<T>(MergeEngine<T> engine)
        where T : IMergeObject, new()
    {
        if (typeof(T) != typeof(VehicleState))
            return;

        engine.SetRule(v => ((VehicleState)(object)v).Speed, MergeRules.MaxDouble());
    }
}
```
