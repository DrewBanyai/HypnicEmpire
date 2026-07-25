// Assets/Scripts/Editor/PathVizMenu.cs
//
// Incremental Path Visualizer — text bridge (Editor)
// --------------------------------------------------
// Runs the full data-layer pipeline (importer -> simulation) against the project's
// GameData and prints the resulting path to the Console. This is the pre-GraphView
// checkpoint: it proves the Core compiles and the path builds on real data. The
// graphical window (M6) will render the same ProgressionGraph.
//
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using HypnicEmpire.PathViz;

namespace HypnicEmpire.EditorTools
{
    public static class PathVizMenu
    {
        [MenuItem("HypnicEmpire/Print Path (text)")]
        public static void PrintPath()
        {
            var dir = Application.dataPath + "/GameData";
            var model = new HypnicEmpireDataSource(dir).Build();
            var graph = PathSimulation.Build(model);

            Debug.Log($"[PathViz] Loaded {model.Options.Count} options " +
                      $"({model.Resources.Count} resources, {model.AlterableValues.Count} alterable values).\n\n" +
                      graph.Summary());

            if (graph.Unreachable.Count > 0)
            {
                Debug.LogWarning($"[PathViz] {graph.Unreachable.Count} pending option(s) (blocked by an unresolved gate):");
                foreach (var o in graph.Unreachable)
                    Debug.LogWarning($"[PathViz]   {o}  requires [{string.Join(", ", o.RequiredUnlocks)}]");
            }

            if (graph.UnresolvedGates.Count > 0)
                Debug.LogWarning($"[PathViz] Unresolved gates (pending future data): " +
                                 string.Join(", ", graph.UnresolvedGates));

            if (graph.DeadEnds.Count > 0)
            {
                var byId = model.Options.ToDictionary(o => o.Id);
                Debug.Log($"[PathViz] {graph.DeadEnds.Count} dead end(s) (grant an unlock, but open no further option):");
                foreach (var id in graph.DeadEnds.OrderBy(i => graph.StageOf.TryGetValue(i, out var s) ? s : 0))
                    Debug.Log($"[PathViz]   stage {(graph.StageOf.TryGetValue(id, out var st) ? st : -1),2}  " +
                              $"{(byId.TryGetValue(id, out var o) ? o.Display : id)}");
            }
        }

        // Quantitative pass (M8): how deep the delve reaches as developments accumulate by stage, using
        // the economy model — recursive converter budgets (a converter drains only as far as its product
        // is absorbed), the land-buying gate (buildings are the unbounded loot/money sink, unlocked by
        // "People have come to work for you"), unlock-driven cap-raises, and a joint reach fixpoint. Before
        // land-buying the loot can't drain, so depth stalls shallow (~L11) — reaches sit past that stage.
        [MenuItem("HypnicEmpire/Print Delve Depth by Stage (text)")]
        public static void PrintDelveDepth()
        {
            var dir = Application.dataPath + "/GameData";
            var source = new HypnicEmpireDataSource(dir);
            var model = source.Build();
            var econ = source.BuildEconomy();
            var graph = PathSimulation.Build(model);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[PathViz] Delve depth by stage ({econ.DelveTrack.Count} levels, {econ.Buildings.Count} buildings):");

            var owned = new System.Collections.Generic.List<string>();
            for (int s = 0; s < graph.Stages.Count; s++)
            {
                foreach (var o in graph.Stages[s])
                    if (o.Kind == OptionKind.Development) owned.Add(o.Id);

                var sat = PathEconomy.SaturatedDepth(model, econ, owned);
                sb.AppendLine($"  stage {s,2}: L{sat.ClearedLevel,-3}   wall: {sat.WallReason}");
            }

            // Reach placement: structural stage (unlock graph) vs economy stage (where the delve reaches it).
            var ann = PathEconomy.Annotate(model, econ, graph);
            sb.AppendLine();
            sb.AppendLine("  reach placement — structural stage vs economy stage (level):");
            foreach (var o in model.Options.Where(o => o.Kind == OptionKind.Reach)
                                           .OrderBy(o => ann.ReachLevel.TryGetValue(o.Id, out var l) ? l : 0))
            {
                int structural = graph.StageOf.TryGetValue(o.Id, out var st) ? st : -1;
                string econ2 = ann.ReachEconomyStage.TryGetValue(o.Id, out var es) ? $"stage {es}" : "unreached";
                int lvl = ann.ReachLevel.TryGetValue(o.Id, out var lv) ? lv : -1;
                sb.AppendLine($"    {o.Display,-28} L{lvl,-3}  structural stage {structural,2}  ->  economy {econ2}");
            }
            Debug.Log(sb.ToString());
        }

        // Timing (M8 §6.4): grind EFFORT to arrive at each reach — delve-actions + faucet-actions to
        // supply what delving consumes, using the best available yields. Rate-agnostic action counts
        // plus a rough single-worker time; identifies the grind chokepoint (Food, which explodes with
        // depth). Real pacing scales with job caps + speed modifiers — this is a relative-effort sketch.
        [MenuItem("HypnicEmpire/Print Grind Effort by Reach (text)")]
        public static void PrintGrindEffort()
        {
            var dir = Application.dataPath + "/GameData";
            var source = new HypnicEmpireDataSource(dir);
            var model = source.Build();
            var econ = source.BuildEconomy();
            var graph = PathSimulation.Build(model);
            var ann = PathEconomy.Annotate(model, econ, graph);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[PathViz] Grind effort by reach (delve-actions + supply-actions; grind chokepoint):");
            foreach (var o in model.Options.Where(o => o.Kind == OptionKind.Reach)
                                           .OrderBy(o => ann.ReachLevel.TryGetValue(o.Id, out var l) ? l : 0))
            {
                if (!ann.ReachGrind.TryGetValue(o.Id, out var g)) continue;
                double hrs = PathEconomy.SecondsFor(g, 10, 5.0) / 3600.0; // 10 workers, timeScale 5 (very rough)
                sb.AppendLine($"  {o.Display,-24} {g}   ~{hrs:0.0}h @10w×5");
            }
            Debug.Log(sb.ToString());
        }
    }
}
#endif
