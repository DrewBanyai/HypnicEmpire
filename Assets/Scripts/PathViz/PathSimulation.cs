// Assets/Scripts/PathViz/PathSimulation.cs
//
// Incremental Path Visualizer — reachability simulation (engine-free core)
// -----------------------------------------------------------------------
// The structural v1 fixpoint (architecture doc §4): from the seed, repeatedly
// fire every option whose RequiredUnlocks are all granted, granting its unlocks,
// bucketing options into stages by the round they first became reachable. Then
// derive the prerequisite edges, the unreachable set, and structural chokepoints.
//
// Seed: only the unlocks true at game start (just Unlock_Game_Start). Every other
// unlock must be granted by some option to be reachable. A required unlock that no
// option ever grants is UNRESOLVED, and the options behind it are reported as pending
// (Unreachable) rather than silently assumed available — that flags not-yet-wired data.
//
// No UnityEngine / UnityEditor dependency.
//
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HypnicEmpire.PathViz
{
    public sealed class PathEdge
    {
        public string FromId, ToId, ViaUnlock;
        public PathEdge(string from, string to, string via) { FromId = from; ToId = to; ViaUnlock = via; }
    }

    public sealed class ProgressionGraph
    {
        public readonly List<List<PathOption>> Stages = new();
        public readonly Dictionary<string, int> StageOf = new();          // option id -> stage index
        public readonly List<PathEdge> Edges = new();
        public readonly List<PathOption> Unreachable = new();             // pending: blocked by an unresolved gate
        public readonly HashSet<string> UnresolvedGates = new();          // required but never granted or engine-provided
        public readonly Dictionary<string, int> Chokepoints = new();      // option id -> downstream options it solely gates
        public readonly HashSet<string> DeadEnds = new();                 // reachable, grants an unlock, but opens no further option

        public int OptionCount => StageOf.Count + Unreachable.Count;
        public int ReachableCount => StageOf.Count;

        public string Summary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Path: {Stages.Count} stages, {ReachableCount}/{OptionCount} options reachable, " +
                          $"{Unreachable.Count} pending, {UnresolvedGates.Count} unresolved gate(s).");
            for (int i = 0; i < Stages.Count; i++)
            {
                var eg = string.Join(", ", Stages[i].Take(3).Select(o => Trunc(o.Display, 24)));
                sb.AppendLine($"  stage {i,2}: {Stages[i].Count,3} option(s)   e.g. {eg}");
            }
            if (Chokepoints.Count > 0)
            {
                sb.AppendLine("  top chokepoints:");
                foreach (var kv in Chokepoints.OrderByDescending(k => k.Value).Take(8))
                    sb.AppendLine($"    gates {kv.Value,3} downstream  {kv.Key}");
            }
            if (DeadEnds.Count > 0)
                sb.AppendLine($"  dead ends (open no further option): {DeadEnds.Count}");
            if (UnresolvedGates.Count > 0)
                sb.AppendLine("  unresolved gates (pending future data): " + string.Join(", ", UnresolvedGates));
            return sb.ToString();
        }

        private static string Trunc(string s, int n) => string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s.Substring(0, n));
    }

    public static class PathSimulation
    {
        /// <summary>Build the progression graph from a normalized model.</summary>
        public static ProgressionGraph Build(PathModel model, bool computeChokepoints = true)
        {
            var g = new ProgressionGraph();

            // Seed = only the unlocks true at start (just Unlock_Game_Start). Everything
            // else must be reached through the graph — resource thresholds are modeled as
            // options gated on the resource, so they're achieved during play, not seeded.
            // A required unlock that is never granted is UNRESOLVED: the options behind it
            // are pending (e.g. an action a not-yet-added building will enable).
            var effectiveSeed = new HashSet<string>(model.SeedUnlocks);

            // Coarse-round fixpoint: a round's grants apply only to the NEXT round,
            // so each stage reads as one clean wave of unlocks.
            var granted = new HashSet<string>(effectiveSeed);
            var fired = new HashSet<string>();
            while (true)
            {
                var newly = model.Options
                    .Where(o => !fired.Contains(o.Id) && o.RequiredUnlocks.All(granted.Contains))
                    .ToList();
                if (newly.Count == 0) break;

                int stageIndex = g.Stages.Count;
                g.Stages.Add(newly);
                foreach (var o in newly) { fired.Add(o.Id); g.StageOf[o.Id] = stageIndex; }
                foreach (var o in newly)
                    foreach (var u in o.GrantedUnlocks)
                        granted.Add(u);
            }

            foreach (var o in model.Options)
                if (!fired.Contains(o.Id)) g.Unreachable.Add(o);

            // Unresolved gates: required unlocks that never became true (block the pending options).
            foreach (var o in model.Options)
                foreach (var r in o.RequiredUnlocks)
                    if (!granted.Contains(r)) g.UnresolvedGates.Add(r);

            BuildEdges(model, g);
            ComputeDeadEnds(model, g);
            if (computeChokepoints) ComputeChokepoints(model, effectiveSeed, g);
            return g;
        }

        // A structural "dead end" — the walkthrough's "dead end stage entry": a Development you
        // can buy that opens no NEW development. It may boost a stat or enable an action, but it
        // presents no further decision on the path. Developments are the player's decision nodes,
        // so dead-end analysis is scoped to them; actions/buildings/reaches are consequences and
        // are categorised by Kind instead. That scoping is what makes "Sample the herbs" (opens
        // the Trade Herbs action but no dev) a dead end while "Buy an axe" (opens the "Setup a
        // chopping block" dev) is not. NOTE: this is a *structural* dead end; the walkthrough's
        // resource-economy dead ends ("stuck at level N") require the quantitative pass (M8).
        private static void ComputeDeadEnds(PathModel model, ProgressionGraph g)
        {
            var devRequired = new HashSet<string>();
            foreach (var o in model.Options)
                if (g.StageOf.ContainsKey(o.Id) && o.Kind == OptionKind.Development)
                    foreach (var r in o.RequiredUnlocks)
                        devRequired.Add(r);

            foreach (var o in model.Options)
                if (g.StageOf.ContainsKey(o.Id) && o.Kind == OptionKind.Development &&
                    o.GrantedUnlocks.Count > 0 && !o.GrantedUnlocks.Any(u => devRequired.Contains(u)))
                    g.DeadEnds.Add(o.Id);
        }

        // Edge A -> B when A grants an unlock B requires, both reachable, A no later than B.
        private static void BuildEdges(PathModel model, ProgressionGraph g)
        {
            var grantersOf = new Dictionary<string, List<PathOption>>();
            foreach (var o in model.Options)
                if (g.StageOf.ContainsKey(o.Id))
                    foreach (var u in o.GrantedUnlocks)
                    {
                        if (!grantersOf.TryGetValue(u, out var list)) { list = new List<PathOption>(); grantersOf[u] = list; }
                        list.Add(o);
                    }

            var seen = new HashSet<string>();
            foreach (var b in model.Options)
            {
                if (!g.StageOf.TryGetValue(b.Id, out var bStage)) continue;
                foreach (var u in b.RequiredUnlocks)
                {
                    if (!grantersOf.TryGetValue(u, out var granters)) continue; // engine-granted (external)
                    foreach (var a in granters)
                    {
                        if (a.Id == b.Id) continue;
                        if (g.StageOf[a.Id] > bStage) continue;
                        var key = a.Id + "->" + b.Id;
                        if (seen.Add(key)) g.Edges.Add(new PathEdge(a.Id, b.Id, u));
                    }
                }
            }
        }

        // Structural chokepoint = how many otherwise-reachable options become unreachable
        // if this option is removed. Remove-and-recount; O(N * fixpoint), fine for ~200 nodes.
        private static void ComputeChokepoints(PathModel model, HashSet<string> effectiveSeed, ProgressionGraph g)
        {
            int baseline = g.ReachableCount;
            foreach (var o in model.Options)
            {
                if (!g.StageOf.ContainsKey(o.Id)) continue;
                int reachedWithout = ReachCount(model, effectiveSeed, o.Id);
                int lost = baseline - reachedWithout - 1; // minus the removed option itself
                if (lost > 0) g.Chokepoints[o.Id] = lost;
            }
        }

        private static int ReachCount(PathModel model, HashSet<string> effectiveSeed, string skipId)
        {
            var granted = new HashSet<string>(effectiveSeed);
            var fired = new HashSet<string>();
            while (true)
            {
                bool progressed = false;
                foreach (var o in model.Options)
                {
                    if (o.Id == skipId || fired.Contains(o.Id)) continue;
                    if (!o.RequiredUnlocks.All(granted.Contains)) continue;
                    fired.Add(o.Id);
                    foreach (var u in o.GrantedUnlocks) granted.Add(u);
                    progressed = true;
                }
                if (!progressed) break;
            }
            return fired.Count;
        }
    }
}
