
# MergeEngine – Deterministic Distributed Object Merging with Vector Clocks

MergeEngine is a lightweight C# library for **merging distributed object state safely and deterministically**, without relying on unreliable timestamp-based Last-Write-Wins. It uses **Vector Clocks** to detect whether updates are causally ordered or occurred concurrently, and applies **custom merge rules only when necessary**.

## 🚀 Why Vector Clocks Instead of Last-Write-Wins?
Traditional LWW systems based on timestamps (`UpdatedAt`) are unreliable because:

- Machines do not share perfectly synchronized clocks (clock skew)
- Network delays and message reordering change event timing
- Two replicas may update without knowing about each other (true concurrency)

### Vector clocks fix this by tracking **causal history**, giving four clear relationships:

| Relation | Meaning | Automatic Resolution |
|---------|---------|----------------------|
| **Before** | Remote update happened after local | Remote overrides local |
| **After** | Local update happened after remote | Local overrides remote |
| **Equal** | Identical history | Deterministic overwrite using remote |
| **Concurrent** | Both changed independently | **Custom merge rule invoked** |

💡 **Custom merge rules are only applied on CONCURRENT updates.**  
Causally ordered updates require no conflict resolution.

## 🧠 Custom Merge Rules
Merge rules enable domain-specific conflict resolution:

```csharp
engine.SetRule(v => v.ActiveSensors, MergeRules.SetUnion<string>());
engine.SetRule(v => v.EngineOn, MergeRules.OrBoolean());
engine.SetRule(v => v.Speed, MergeRules.MaxDouble());
```

| Rule | Example | Behavior |
|------|--------|-----------|
| `LastWriteWins<T>` | default behavior | Uses vector clock ordering first |
| `SetUnion<T>` | sensor lists, groups | Combines both sets when concurrent |
| `OrBoolean` | flags like `EngineOn` | `true` dominates |
| `MaxDouble` | measurements like `Speed` | Highest number wins |

## 🧪 Example
```csharp
var engine = new MergeEngine<VehicleState>();

engine.SetRule(v => v.ActiveSensors, MergeRules.SetUnion<string>());
engine.SetRule(v => v.EngineOn, MergeRules.OrBoolean());

var local = new VehicleState { EngineOn = false, ActiveSensors = new HashSet<string> { "GPS" }, Speed = 40 };
var remote = new VehicleState { EngineOn = true, ActiveSensors = new HashSet<string> { "LIDAR" }, Speed = 50 };

local.Update(v => v.ActiveSensors.Add("GPS"), "Node-A");
remote.Update(v => v.ActiveSensors.Add("LIDAR"), "Node-B");

var merged = engine.Merge(local, remote);
```

### In this example
- Updates came from independent nodes → **Concurrent**
- The rules determine:
  - Sensors = `["GPS", "LIDAR"]`
  - EngineOn = `true`
  - Speed = chosen according to rule or LWW

## 📌 Key Features

| Feature | Description |
|---------|-------------|
| **Per-property merge** | Intelligent merging instead of overwriting whole object |
| **Custom rules** | Only triggered during concurrent conflicts |
| **Vector Clock causality** | Accurate update ordering without timestamps |
| **Pure functional merge** | `Merge()` returns a new object |
| **Optional in-place merge** | `MergeInto()` updates existing instance for speed |
| **CRDT-friendly** | Ideal for eventually consistent distributed replicas |

## 💡 Why not rely only on Last-Write-Wins?
LWW overwrites data even when both updates matter.  
Example: two vehicles independently reporting different sensors — with timestamp LWW, **one dataset is destroyed**.  
With MergeEngine CRDT rules → **sensors combine safely**.

## 📝 License
MIT
