using MergeEngine;
using MergeEngine.Core;
using MergeEngine.Extensions;
using MergeEngine.Samples;

namespace VehicleMergeExample
{
    /// <summary>
    /// Demonstrates causal merging of distributed state using vector clocks and customizable
    /// merge rules for resolving concurrent conflicts between replicas of <see cref="VehicleState"/>.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Entry point for the console demo, showing four example merge scenarios:
        /// BEFORE, AFTER, EQUAL, and CONCURRENT state updates.
        /// </summary>
        static void Main(string[] args)
        {
            var engine = new MergeEngine<VehicleState>();

            Console.WriteLine("=== MergeEngine Console Demo ===\n");

            CaseBefore(engine);
            CaseAfter(engine);
            CaseEqual(engine);
            CaseConcurrent(engine);

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Demonstrates a merge where the <b>remote</b> update happened causally after the local update.
        /// Vector clock relation: <see cref="VectorClockRelation.Before"/> — remote wins safely.
        /// </summary>
        static void CaseBefore(MergeEngine<VehicleState> engine)
        {
            Console.WriteLine("---- CASE: BEFORE ----");

            var local = new VehicleState { Speed = 50 };
            var remote = new VehicleState { Speed = 80 };

            // Remote is causally newer (remote clock > local clock)
            local.Update(v => v.Speed = 50, "Node-A");   // A:1
            remote.Clock = local.Clock.Clone();          // copy causal history
            remote.Update(v => v.Speed = 80, "Node-A");  // A:2  -> remote AFTER local

            var result = engine.Merge(local, remote);

            Print("Local", local);
            Print("Remote", remote);
            Print("Result", result);
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates a merge where the <b>local</b> replica is causally newer.
        /// Vector clock relation: <see cref="VectorClockRelation.After"/> — local wins.
        /// </summary>
        static void CaseAfter(MergeEngine<VehicleState> engine)
        {
            Console.WriteLine("---- CASE: AFTER ----");

            var local = new VehicleState { Speed = 40 };
            var remote = new VehicleState { Speed = 20 };

            // Local becomes newer (local clock > remote clock)
            remote.Update(v => v.Speed = 20, "Node-B");  // B:1
            local.Clock = remote.Clock.Clone();          // B:1
            local.Update(v => v.Speed = 40, "Node-B");   // B:2 -> local AFTER

            var result = engine.Merge(local, remote);

            Print("Local", local);
            Print("Remote", remote);
            Print("Result", result);
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates a merge where both versions share identical causal history.
        /// Vector clock relation: <see cref="VectorClockRelation.Equal"/> — remote is chosen for determinism.
        /// </summary>
        static void CaseEqual(MergeEngine<VehicleState> engine)
        {
            Console.WriteLine("---- CASE: EQUAL ----");

            var local = new VehicleState { Speed = 70 };
            var remote = new VehicleState { Speed = 90 };

            local.Update(v => v.Speed = 70, "Node-X");   // X:1
            remote.Clock = local.Clock.Clone();          // X:1
            remote.Update(v => v.Speed = 90, "Node-X");  // X:2
            local.Update(v => v.Speed = 70, "Node-X");   // X:2 => Equal history

            var result = engine.Merge(local, remote);

            Print("Local", local);
            Print("Remote", remote);
            Print("Result", result);
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates a merge where both replicas were updated independently.
        /// Vector clock relation: <see cref="VectorClockRelation.Concurrent"/> — custom merge rules resolve conflict.
        /// </summary>
        static void CaseConcurrent(MergeEngine<VehicleState> engine)
        {
            Console.WriteLine("---- CASE: CONCURRENT ----");

            // Add merge rules
            engine.SetRule(v => v.ActiveSensors, MergeRules.SetUnion<string>());
            engine.SetRule(v => v.EngineOn, MergeRules.OrBoolean());

            var local = new VehicleState { EngineOn = false, ActiveSensors = new HashSet<string> { "GPS" }, Speed = 40 };
            var remote = new VehicleState { EngineOn = true, ActiveSensors = new HashSet<string> { "LIDAR" }, Speed = 50 };

            // Node independence => concurrent
            local.Update(v => v.ActiveSensors.Add("GPS"), "Node-A");    // A:1
            remote.Update(v => v.ActiveSensors.Add("LIDAR"), "Node-B"); // B:1 => Concurrent

            var result = engine.Merge(local, remote);

            Print("Local", local);
            Print("Remote", remote);
            Print("Result", result);

            Console.WriteLine();
        }

        /// <summary>
        /// Prints a formatted view of a <see cref="VehicleState"/> including its vector clock state.
        /// </summary>
        static void Print(string label, VehicleState v)
        {
            Console.WriteLine($"{label}: Speed={v.Speed}, EngineOn={v.EngineOn}, Sensors=[{string.Join(",", v.ActiveSensors)}], Clock={FormatClock(v.Clock)}");
        }

        /// <summary>
        /// Formats the node:counter mapping of a vector clock into a readable string.
        /// </summary>
        static string FormatClock(VectorClock clock)
        {
            var list = new List<string>();
            foreach (var kv in clock.Versions)
                list.Add($"{kv.Key}:{kv.Value}");
            return "{" + string.Join(" ", list) + "}";
        }
    }
}
