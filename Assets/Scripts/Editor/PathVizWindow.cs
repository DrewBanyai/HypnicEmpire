// Assets/Scripts/Editor/PathVizWindow.cs
//
// Incremental Path Visualizer — GraphView window (Editor, M6)
// ----------------------------------------------------------
// Renders the ProgressionGraph as a node graph: one node per option, laid out in
// columns by stage, edges from the prerequisite DAG, nodes tinted by how much they
// gate (chokepoint weight). Toolbar has a Rebuild button and a live summary.
//
// Open from the Unity menu:  HypnicEmpire ▸ Path Visualizer
//
// Uses UnityEditor.Experimental.GraphView (the framework behind Shader Graph). If a
// future Unity relocates that namespace, only the two using lines below change.
//
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using HypnicEmpire.PathViz;

namespace HypnicEmpire.EditorTools
{
    public class PathVizWindow : EditorWindow
    {
        private PathGraphView _graph;
        private Label _summary;

        [MenuItem("HypnicEmpire/Path Visualizer")]
        public static void Open()
        {
            var w = GetWindow<PathVizWindow>();
            w.titleContent = new GUIContent("Path Visualizer");
            w.minSize = new Vector2(720, 480);
        }

        private void CreateGUI()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(Rebuild) { text = "Rebuild" });
            toolbar.Add(new ToolbarSpacer());
            _summary = new Label(" ") { style = { unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 6 } };
            toolbar.Add(_summary);
            rootVisualElement.Add(toolbar);

            _graph = new PathGraphView { style = { flexGrow = 1 } };
            rootVisualElement.Add(_graph);

            Rebuild();
        }

        private void Rebuild()
        {
            if (_graph == null) return;

            var dir = Application.dataPath + "/GameData";
            var source = new HypnicEmpireDataSource(dir);
            var model = source.Build();
            var econ = source.BuildEconomy();
            var pg = PathSimulation.Build(model);
            var ann = PathEconomy.Annotate(model, econ, pg);                 // saturated depth per stage + reach placement
            var econChokes = PathEconomy.EconomicChokepoints(model, econ, pg); // devs whose removal drops final depth
            _graph.Render(pg, ann, econChokes);

            int maxDepth = ann.DepthByStage.Count > 0 ? ann.DepthByStage.Values.Max(d => d.ClearedLevel) : 0;
            _summary.text = $"{pg.ReachableCount}/{pg.OptionCount} options · {pg.Stages.Count} stages · " +
                            $"{pg.Chokepoints.Count} chokepoints · {pg.DeadEnds.Count} dead ends · " +
                            $"delve → L{maxDepth} · {econChokes.Count} econ-gates" +
                            (pg.Unreachable.Count > 0 ? $" · {pg.Unreachable.Count} pending ({string.Join(", ", pg.UnresolvedGates)})" : "");
        }
    }

    // -------------------------------------------------------------------
    public class PathGraphView : GraphView
    {
        private const float ColW = 300f, RowH = 150f, MarginX = 40f, MarginY = 40f, NodeW = 240f;

        public PathGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }

        public void Render(ProgressionGraph pg, EconomyAnnotation ann, Dictionary<string, int> econChokes)
        {
            foreach (var e in graphElements.ToList()) RemoveElement(e);
            if (pg == null || pg.Stages.Count == 0) return;

            int maxGates = pg.Chokepoints.Count > 0 ? pg.Chokepoints.Values.Max() : 1;
            var nodes = new Dictionary<string, OptionNode>();

            // Display column = the unified coarse-round economy stage. That single fixpoint already keeps
            // early-game wave granularity (Delve 0, Empty_Belly 1, Look-around 2, ...) AND sequences the
            // deep game (a reach fires only once the delve is deep enough to reach it, after the development
            // that deepens it). The structural stage is only a fallback for any option the economy pass
            // never fires (unreached / gated on future data).
            var colOf = new Dictionary<string, int>();
            foreach (var kv in pg.StageOf) colOf[kv.Key] = kv.Value;
            foreach (var kv in ann.OptionEconomyStage) colOf[kv.Key] = kv.Value;

            var rowInCol = new Dictionary<int, int>();
            foreach (var stage in pg.Stages)
                foreach (var opt in stage)
                {
                    int col = colOf.TryGetValue(opt.Id, out var c) ? c : 0;
                    int row = rowInCol.TryGetValue(col, out var rr) ? rr : 0;
                    rowInCol[col] = row + 1;

                    int gates = pg.Chokepoints.TryGetValue(opt.Id, out var gv) ? gv : 0;
                    int econGate = econChokes.TryGetValue(opt.Id, out var eg) ? eg : 0;
                    Color header =
                        opt.Kind == OptionKind.Reach  ? new Color(0.30f, 0.22f, 0.42f)   // reaches: purple (depth-placed)
                      : opt.Kind == OptionKind.Battle ? new Color(0.46f, 0.16f, 0.16f)   // battles: crimson (strength-placed)
                      : ColorFor(gates, maxGates);
                    var node = new OptionNode(opt, col, gates, header, pg.DeadEnds.Contains(opt.Id),
                                              ExtraLine(opt, col, ann), econGate);
                    node.SetPosition(new Rect(MarginX + col * ColW, MarginY + row * RowH, NodeW, 120));
                    AddElement(node);
                    nodes[opt.Id] = node;
                }

            foreach (var edge in pg.Edges)
            {
                if (!nodes.TryGetValue(edge.FromId, out var from)) continue;
                if (!nodes.TryGetValue(edge.ToId, out var to)) continue;
                var e = from.Output.ConnectTo(to.Input);
                AddElement(e);
            }

            // Pending options (blocked by an unresolved gate) in a flagged column left of stage 0.
            for (int r = 0; r < pg.Unreachable.Count; r++)
            {
                var opt = pg.Unreachable[r];
                var node = new OptionNode(opt, -1, 0, new Color(0.36f, 0.20f, 0.22f));
                node.SetPosition(new Rect(MarginX - ColW, MarginY + r * RowH, NodeW, 120));
                AddElement(node);
            }
        }

        // The economy annotation shown under a node: a reach's level (+ "unreached" if the delve
        // never gets there), or the delve depth a development's stage reaches.
        private static string ExtraLine(PathOption opt, int col, EconomyAnnotation ann)
        {
            if (opt.Kind == OptionKind.Battle && ann.BattleEnemy.TryGetValue(opt.Id, out var en))
                return $"enemy {Compact(en)} · win ≥{Compact(en * 2)} str";
            if (opt.Kind == OptionKind.Reach && ann.ReachLevel.TryGetValue(opt.Id, out var lvl))
            {
                string s = ann.ReachEconomyStage.ContainsKey(opt.Id) ? $"reach L{lvl}" : $"reach L{lvl} · econ: unreached";
                if (ann.ReachGrind.TryGetValue(opt.Id, out var g) && g.DominantResource != null)
                    s += $" · grind {g.DominantResource}×{Compact(g.DominantActions)}";
                return s;
            }
            if (opt.Kind == OptionKind.Development && ann.DepthByStage.TryGetValue(col, out var dr))
                return $"→ delve L{dr.ClearedLevel}";
            return null;
        }

        private static string Compact(double n)
        {
            if (n >= 1_000_000) return $"{n / 1_000_000:0.#}M";
            if (n >= 1_000) return $"{n / 1_000:0.#}k";
            return ((long)n).ToString();
        }

        // Grey (not a gate) -> warm red (gates the most).
        private static Color ColorFor(int gates, int maxGates)
        {
            if (gates <= 0) return new Color(0.24f, 0.26f, 0.30f);
            float t = Mathf.Clamp01((float)gates / Mathf.Max(1, maxGates));
            return Color.Lerp(new Color(0.20f, 0.34f, 0.42f), new Color(0.62f, 0.20f, 0.18f), t);
        }

        // Let any port connect to any other (display graph; no editing semantics).
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter)
            => ports.ToList().Where(p => p.direction != startPort.direction && p.node != startPort.node).ToList();
    }

    // -------------------------------------------------------------------
    public class OptionNode : Node
    {
        public readonly Port Input, Output;

        public OptionNode(PathOption opt, int stage, int gates, Color header, bool deadEnd = false,
                          string extra = null, int econGate = 0)
        {
            title = opt.Display;
            tooltip = opt.SourceRef;

            // Amber left-border flags a structural dead end (opens no further option).
            if (deadEnd)
            {
                style.borderLeftWidth = 4;
                style.borderLeftColor = new Color(0.85f, 0.65f, 0.13f);
            }
            // Cyan right-border flags an economic chokepoint (its removal shortens the delve).
            if (econGate > 0)
            {
                style.borderRightWidth = 4;
                style.borderRightColor = new Color(0.20f, 0.70f, 0.75f);
            }

            Input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            Input.portName = "";
            inputContainer.Add(Input);

            Output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            Output.portName = "";
            outputContainer.Add(Output);

            titleContainer.style.backgroundColor = header;

            string line = stage < 0
                ? $"PENDING · {opt.Kind}"
                : $"stage {stage} · {opt.Kind}" + (gates > 0 ? $" · gates {gates}" : "")
                  + (deadEnd ? " · dead end" : "") + (econGate > 0 ? $" · econ-gate ↓{econGate}" : "");
            var info = new Label(line) { style = { marginLeft = 6, marginRight = 6, marginTop = 4, marginBottom = 4, whiteSpace = WhiteSpace.Normal } };
            extensionContainer.Add(info);

            if (!string.IsNullOrEmpty(extra))
                extensionContainer.Add(new Label(extra)
                    { style = { marginLeft = 6, marginRight = 6, marginBottom = 4, opacity = 0.85f, whiteSpace = WhiteSpace.Normal } });

            if (stage < 0 && opt.RequiredUnlocks.Count > 0)
                extensionContainer.Add(new Label("needs: " + string.Join(", ", opt.RequiredUnlocks))
                    { style = { marginLeft = 6, marginRight = 6, marginBottom = 4, opacity = 0.75f, whiteSpace = WhiteSpace.Normal } });

            if (opt.Costs.Count > 0)
            {
                var costs = string.Join(", ", opt.Costs.Take(4).Select(c => c.Resource));
                extensionContainer.Add(new Label($"cost: {costs}") { style = { marginLeft = 6, marginRight = 6, marginBottom = 4, opacity = 0.7f, whiteSpace = WhiteSpace.Normal } });
            }

            RefreshExpandedState();
            RefreshPorts();
        }
    }
}
#endif
