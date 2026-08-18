// Assets/Scripts/Editor/GameDataValidator.cs
//
// HypnicEmpire — Game Data Validator
// -----------------------------------
// A read-only integrity checker for the JSON files in Assets/GameData.
//
// It parses every data file generically (via Newtonsoft JToken, the same JSON
// stack the game already uses), builds a cross-reference index of the game's
// identifier namespaces — Unlocks, Resources, AlterableValues, Actions — and
// reports every reference that does not resolve. It also lints every embedded
// ComputedValue (Formula / Curve / Table): syntax, variable and accessor names,
// accessor cycles, and a sampled preview of each. It never writes to the game
// data; it only reads and reports.
//
// This is intended to be the first component of the Incremental Path
// Visualizer's data layer: the GameDataIndex it produces is exactly the index
// the path-graph builder will consume. The analyzer core (GameDataIndex,
// GameDataAnalyzer, ValidationIssue) deliberately references only System.IO and
// Newtonsoft — no UnityEngine / UnityEditor — so it can be unit-tested or reused
// outside the editor. Only the thin menu wrapper at the bottom touches Unity.
//
// Run it from the Unity menu:  HypnicEmpire ▸ Validate Game Data
//
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using HypnicEmpire;   // Expression, ComputedValue and friends (engine-free core)

namespace HypnicEmpire.EditorTools
{
    // ---------------------------------------------------------------------
    // Issue model
    // ---------------------------------------------------------------------
    public enum ValidationSeverity { Error, Warning, Info }

    public class ValidationIssue
    {
        public ValidationSeverity Severity;
        public string Category;   // short slug, e.g. "unknown-unlock"
        public string Message;    // human-readable description
        public string Location;   // file + entry + field, when known
        public int Count = 1;     // how many references collapsed into this issue

        public override string ToString()
        {
            var loc = string.IsNullOrEmpty(Location) ? "" : $"   @ {Location}";
            var cnt = Count > 1 ? $" (x{Count})" : "";
            return $"[{Severity}] ({Category}) {Message}{cnt}{loc}";
        }
    }

    // ---------------------------------------------------------------------
    // The cross-reference index the analyzer builds. Reusable by the visualizer.
    // ---------------------------------------------------------------------
    public class GameDataIndex
    {
        // Declared identifier namespaces (the "spelling authorities").
        public HashSet<string> DeclaredUnlocks   = new();
        public HashSet<string> Resources         = new();
        public HashSet<string> ResourceGroups    = new();
        public HashSet<string> AlterableValues   = new();
        public HashSet<string> Actions           = new();
        public HashSet<string> Buildings         = new();

        // Unlock flow: who can turn an unlock true (grants) vs who is gated by it (consumes).
        public HashSet<string> GrantedUnlocks    = new();   // set true by a data source (or the seed)
        public HashSet<string> ConsumedUnlocks   = new();   // used as a trigger / alteration key / reveal gate

        // Every reference actually seen, id -> sample locations (deduped, capped).
        public Dictionary<string, List<string>> UnlockRefs    = new();
        public Dictionary<string, List<string>> ResourceRefs  = new();
        public Dictionary<string, List<string>> ValueNameRefs = new();
        public Dictionary<string, List<string>> ActionRefs    = new();

        public IEnumerable<string> ReferencedUnlocks => UnlockRefs.Keys;
    }

    // ---------------------------------------------------------------------
    // The analyzer core — no Unity dependencies.
    // ---------------------------------------------------------------------
    public class GameDataAnalyzer
    {
        private readonly string _dir;                 // absolute path to the GameData folder
        private readonly List<ValidationIssue> _issues = new();
        private readonly GameDataIndex _index = new();

        // A couple of identifiers that legitimately live outside ResourceTypes:
        // Land/Buildings/People etc. are AlterableValues that are also spent as
        // "resource" costs. We treat any AlterableValue name as a valid resource id.
        // The seed unlock is set true by the engine at game start.
        private const string SeedUnlock = "Unlock_Game_Start";

        // Cache of parsed file roots, so the formula-lint pass can re-walk them
        // without re-reading (and without re-reporting missing/invalid-file issues).
        private readonly Dictionary<string, JToken> _roots = new();

        // Variables a formula may legitimately read from its use-site context.
        private static readonly HashSet<string> AllowedFormulaVars = new() { "count", "x", "value", "progress" };

        public GameDataAnalyzer(string gameDataDirectory) { _dir = gameDataDirectory; }

        public IReadOnlyList<ValidationIssue> Issues => _issues;
        public GameDataIndex Index => _index;

        // -----------------------------------------------------------------
        // Small JSON helpers (defensive: never throw on shape surprises).
        // -----------------------------------------------------------------
        private static IEnumerable<JToken> Arr(JToken node)
            => node is JArray a ? (IEnumerable<JToken>)a : Array.Empty<JToken>();

        private static string Str(JToken node, string field)
        {
            if (!(node is JObject obj)) return null;   // scalars/arrays have no named children
            var t = obj[field];
            if (t == null || t.Type == JTokenType.Null) return null;
            return t.Type == JTokenType.String ? t.Value<string>() : t.ToString();
        }

        private void Add(ValidationSeverity sev, string cat, string msg, string loc = null)
            => _issues.Add(new ValidationIssue { Severity = sev, Category = cat, Message = msg, Location = loc });

        private static void Record(Dictionary<string, List<string>> map, string id, string loc)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!map.TryGetValue(id, out var locs)) { locs = new List<string>(); map[id] = locs; }
            if (locs.Count < 5 && !locs.Contains(loc)) locs.Add(loc);
        }

        private void RefUnlockConsume(string id, string loc)
        { if (string.IsNullOrEmpty(id)) return; _index.ConsumedUnlocks.Add(id); Record(_index.UnlockRefs, id, loc); }

        private void RefUnlockGrant(string id, string loc)
        { if (string.IsNullOrEmpty(id)) return; _index.GrantedUnlocks.Add(id); Record(_index.UnlockRefs, id, loc); }

        private void RefResource(string id, string loc)   => Record(_index.ResourceRefs, id, loc);
        private void RefValueName(string id, string loc)  => Record(_index.ValueNameRefs, id, loc);
        private void RefAction(string id, string loc)     => Record(_index.ActionRefs, id, loc);

        private JToken LoadFile(string fileName)
        {
            if (_roots.TryGetValue(fileName, out var cached)) return cached;
            var path = Path.Combine(_dir, fileName);
            if (!File.Exists(path)) { Add(ValidationSeverity.Error, "missing-file", $"Data file not found: {fileName}", fileName); return null; }
            try { var t = JToken.Parse(File.ReadAllText(path)); _roots[fileName] = t; return t; }
            catch (Exception e) { Add(ValidationSeverity.Error, "invalid-json", $"Failed to parse {fileName}: {e.Message}", fileName); return null; }
        }

        private void DeclareUnique(HashSet<string> set, string id, string cat, string loc)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!set.Add(id)) Add(ValidationSeverity.Warning, cat, $"Duplicate declaration: '{id}'", loc);
        }

        // -----------------------------------------------------------------
        // Main entry point.
        // -----------------------------------------------------------------
        public GameDataIndex Run()
        {
            LoadUnlockIDs();
            LoadResources();
            LoadAlterableValues();
            LoadTaskActions();
            LoadDevelopments();
            LoadProjects();
            LoadBuildings();
            LoadArmies();
            LoadBattles();
            LoadTriggerList("Achievements.json", isArrayRoot: true, arrayField: null, triggerField: "Trigger", label: "achievement");
            LoadTriggerList("JournalEntries.json", isArrayRoot: true, arrayField: null, triggerField: "Trigger", label: "journal entry");

            _index.GrantedUnlocks.Add(SeedUnlock); // engine sets this true at start

            CrossCheck();
            LintComputedValues();
            return _index;
        }

        // ---- formula / ComputedValue linting ----------------------------
        //
        // Walks every parsed file for embedded ComputedValues (a JSON object with
        // a "Formula", "Table", or "Keys"/"Curve" key) and lints each:
        //   * Formula syntax        -> error
        //   * unknown context var   -> warning (likely a typo; count/x/value/progress are known)
        //   * unresolved accessor   -> warning (av/res/owned name not a declared value)
        //   * accessor cycles       -> error (av() between AlterableValues only)
        //   * sampled preview       -> info (so a human can eyeball the curve)
        // Reuses the runtime Expression / ComputedValue engine, so lint semantics
        // match runtime exactly. Today's data has no ComputedValues, so this pass
        // is silent until fields migrate to them (design doc §6.7/§7).

        private void LintComputedValues()
        {
            foreach (var kv in _roots)
                if (kv.Value != null) WalkComputed(kv.Value, kv.Key, "");
            CheckAccessorCycles();
        }

        private static bool IsComputedValue(JObject o)
            => o["Formula"] != null || o["Table"] != null || o["Keys"] != null || o["Curve"] != null;

        private void WalkComputed(JToken node, string file, string path)
        {
            if (node is JObject o)
            {
                if (IsComputedValue(o)) { LintOne(o, file, string.IsNullOrEmpty(path) ? "(root)" : path); return; }
                foreach (var p in o.Properties())
                    WalkComputed(p.Value, file, string.IsNullOrEmpty(path) ? p.Name : path + "." + p.Name);
            }
            else if (node is JArray a)
            {
                for (int i = 0; i < a.Count; i++) WalkComputed(a[i], file, $"{path}[{i}]");
            }
        }

        private void LintOne(JObject obj, string file, string path)
        {
            string loc = $"{file} > {path}";
            ComputedValue cv;
            try
            {
                if (obj["Formula"] != null)
                {
                    cv = ComputedValue.FromFormula(obj["Formula"].Value<string>());
                }
                else if (obj["Table"] != null)
                {
                    if (!(obj["Table"] is JArray ta) || ta.Count == 0)
                    { Add(ValidationSeverity.Error, "computed-shape", "'Table' must be a non-empty array.", loc); return; }
                    var tbl = new List<double>();
                    foreach (var e in ta) tbl.Add(e.Value<double>());
                    cv = ComputedValue.FromTable(tbl, Str(obj, "Index") ?? "count");
                }
                else // Keys / Curve
                {
                    var modeStr = Str(obj, "Curve") ?? "linear";
                    if (!modeStr.Equals("linear", StringComparison.OrdinalIgnoreCase) &&
                        !modeStr.Equals("step", StringComparison.OrdinalIgnoreCase))
                        Add(ValidationSeverity.Warning, "computed-shape", $"Unknown curve mode '{modeStr}', treating as linear.", loc);
                    var mode = modeStr.Equals("step", StringComparison.OrdinalIgnoreCase) ? CurveMode.Step : CurveMode.Linear;

                    if (!(obj["Keys"] is JArray keys) || keys.Count == 0)
                    { Add(ValidationSeverity.Error, "computed-shape", "Curve requires a non-empty 'Keys' array.", loc); return; }

                    var pts = new List<(double x, double y)>();
                    double prevX = double.NegativeInfinity; bool monotonic = true;
                    foreach (var k in keys)
                    {
                        if (k["x"] == null || k["y"] == null)
                        { Add(ValidationSeverity.Error, "computed-shape", "Curve key missing 'x' or 'y'.", loc); return; }
                        double kx = k["x"].Value<double>(), ky = k["y"].Value<double>();
                        if (kx <= prevX) monotonic = false;
                        prevX = kx; pts.Add((kx, ky));
                    }
                    if (!monotonic)
                        Add(ValidationSeverity.Warning, "computed-shape", "Curve 'Keys' are not strictly increasing in x (they will be sorted).", loc);
                    cv = ComputedValue.FromCurve(mode, Str(obj, "Input") ?? "x", pts);
                }
            }
            catch (ExpressionException e) { Add(ValidationSeverity.Error, "formula-syntax", $"Invalid formula: {e.Message}", loc); return; }
            catch (Exception e)           { Add(ValidationSeverity.Error, "computed-shape", $"Invalid ComputedValue: {e.Message}", loc); return; }

            // Unknown context variables (formulas only; Curve/Table 'Input'/'Index' may be custom).
            if (cv.Kind == ComputedValueKind.Formula)
                foreach (var v in cv.VariableReferences())
                    if (!AllowedFormulaVars.Contains(v))
                        Add(ValidationSeverity.Warning, "formula-variable",
                            $"Formula reads '{v}', which is not a known context variable ({string.Join("/", AllowedFormulaVars)}) — confirm it is provided at this use-site.", loc);

            // Accessor name resolution against the declared namespaces.
            foreach (var (kind, name) in cv.AccessorReferences())
            {
                bool known = kind switch
                {
                    "av"    => _index.AlterableValues.Contains(name),
                    "res"   => _index.Resources.Contains(name),
                    "owned" => _index.Buildings.Contains(name),
                    _       => true
                };
                if (!known)
                {
                    string realm = kind == "av" ? "AlterableValue" : kind == "res" ? "Resource" : "Building";
                    string note = kind == "owned" ? " (buildings are matched by Name until stable Ids are added)" : "";
                    Add(ValidationSeverity.Warning, "formula-accessor",
                        $"{kind}(\"{name}\") does not resolve to a known {realm}{note}.", loc);
                }
            }

            PreviewComputed(cv, loc);
        }

        // Sampled preview so a human can eyeball a curve/formula. Accessors and any
        // secondary variables are stubbed to 1.0 (noted), so this is a smoke check.
        private static readonly (string name, double[] samples)[] PreviewVars =
        {
            ("count",    new double[] { 0, 1, 2, 5, 10 }),
            ("x",        new double[] { 0, 25, 50, 75, 100 }),
            ("progress", new double[] { 0, 0.5, 1 }),
            ("value",    new double[] { 1, 10, 100 }),
        };

        private void PreviewComputed(ComputedValue cv, string loc)
        {
            var refs = new HashSet<string>(cv.VariableReferences());
            bool stubbedAccessors = cv.AccessorReferences().Count > 0;

            var ctx = new ExpressionContext { Av = _ => 1.0, Res = _ => 1.0, Owned = _ => 1.0 };

            string drivingName = null; double[] samples = null;
            foreach (var (name, s) in PreviewVars)
                if (refs.Contains(name)) { drivingName = name; samples = s; break; }
            // Curve/Table with a custom single variable: sample it with a default range.
            if (drivingName == null && cv.Kind != ComputedValueKind.Formula && refs.Count == 1)
            { drivingName = System.Linq.Enumerable.First(refs); samples = new double[] { 0, 1, 2, 5, 10 }; }

            try
            {
                if (drivingName == null)
                {
                    foreach (var v in refs) ctx.Set(v, 1.0);
                    double val = cv.Evaluate(ctx);
                    Add(ValidationSeverity.Info, "computed-preview",
                        $"preview: = {Fmt(val)}{(stubbedAccessors ? " (accessors=1)" : "")}", loc);
                    return;
                }

                foreach (var v in refs) if (v != drivingName) ctx.Set(v, 1.0);
                var parts = new List<string>();
                foreach (var sample in samples)
                {
                    ctx.Set(drivingName, sample);
                    parts.Add($"{Fmt(sample)}->{Fmt(cv.Evaluate(ctx))}");
                }
                string extra = (refs.Count > 1 ? " (other vars=1)" : "") + (stubbedAccessors ? " (accessors=1)" : "");
                Add(ValidationSeverity.Info, "computed-preview", $"preview {drivingName}: {string.Join(", ", parts)}{extra}", loc);
            }
            catch (Exception e)
            {
                Add(ValidationSeverity.Info, "computed-preview", $"preview unavailable: {e.Message}", loc);
            }
        }

        private static string Fmt(double v)
            => (!double.IsInfinity(v) && !double.IsNaN(v) && v == Math.Floor(v))
                ? ((long)v).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        // Accessor cycles can only occur through av() (the only accessor that reads a
        // data-defined value: an AlterableValue). Build a graph AlterableValue -> av
        // names read by any formula inside its definition, and DFS for a back-edge.
        private void CheckAccessorCycles()
        {
            if (!_roots.TryGetValue("AlterableValues.json", out var root) || root == null) return;

            var graph = new Dictionary<string, HashSet<string>>();
            foreach (var entry in Arr(root))
            {
                var name = Str(entry, "Name");
                if (string.IsNullOrEmpty(name)) continue;
                var deps = new HashSet<string>();
                CollectAvDeps(entry, deps);
                if (deps.Count > 0) graph[name] = deps;
            }

            var color = new Dictionary<string, int>(); // 0/undef = unvisited, 1 = on-stack, 2 = done
            var stack = new List<string>();
            foreach (var node in graph.Keys)
                if (DfsCycle(node, graph, color, stack)) break; // report the first cycle found
        }

        private bool DfsCycle(string node, Dictionary<string, HashSet<string>> g, Dictionary<string, int> color, List<string> stack)
        {
            color.TryGetValue(node, out var c);
            if (c == 1)
            {
                int i = stack.IndexOf(node);
                var cycle = System.Linq.Enumerable.Append(System.Linq.Enumerable.Skip(stack, i), node);
                Add(ValidationSeverity.Error, "formula-cycle",
                    $"Accessor cycle among AlterableValues: {string.Join(" -> ", cycle)}", "AlterableValues.json");
                return true;
            }
            if (c == 2) return false;

            color[node] = 1; stack.Add(node);
            if (g.TryGetValue(node, out var deps))
                foreach (var d in deps)
                    if (g.ContainsKey(d) && DfsCycle(d, g, color, stack)) return true;
            stack.RemoveAt(stack.Count - 1); color[node] = 2;
            return false;
        }

        private void CollectAvDeps(JToken node, HashSet<string> deps)
        {
            if (node is JObject o)
            {
                if (IsComputedValue(o))
                {
                    if (o["Formula"] != null)
                    {
                        try
                        {
                            var cv = ComputedValue.FromFormula(o["Formula"].Value<string>());
                            foreach (var (kind, name) in cv.AccessorReferences())
                                if (kind == "av") deps.Add(name);
                        }
                        catch { /* syntax errors are reported by the lint pass; ignore here */ }
                    }
                    return;
                }
                foreach (var p in o.Properties()) CollectAvDeps(p.Value, deps);
            }
            else if (node is JArray a)
            {
                foreach (var e in a) CollectAvDeps(e, deps);
            }
        }

        // ---- individual file readers ------------------------------------

        private void LoadUnlockIDs()
        {
            var root = LoadFile("UnlockIDs.json");
            foreach (var t in Arr(root))
                DeclareUnique(_index.DeclaredUnlocks, t?.ToString(), "duplicate-unlock", "UnlockIDs.json");
        }

        private void LoadResources()
        {
            var root = LoadFile("Resources.json");
            if (root == null) return;

            foreach (var g in Arr(root["ResourceGroups"]))
                _index.ResourceGroups.Add(g?.ToString());

            foreach (var r in Arr(root["ResourceTypes"]))
            {
                var name = Str(r, "Name");
                DeclareUnique(_index.Resources, name, "duplicate-resource", "Resources.json");

                var group = Str(r, "ResourceGroup");
                if (!string.IsNullOrEmpty(group) && !_index.ResourceGroups.Contains(group))
                    Add(ValidationSeverity.Warning, "unknown-resource-group",
                        $"Resource '{name}' uses ResourceGroup '{group}' which is not in ResourceGroups.", "Resources.json");

                // Threshold unlocks: reaching a value broadcasts (grants) an unlock.
                foreach (var u in Arr(r["Unlocks"]))
                    RefUnlockGrant(Str(u, "Unlock"), $"Resources.json > {name} > Unlocks");

                // UnlockAlterations: object keyed by unlock id (consumes those unlocks).
                if (r["UnlockAlterations"] is JObject alt)
                    foreach (var p in alt.Properties())
                        RefUnlockConsume(p.Name, $"Resources.json > {name} > UnlockAlterations");
            }

            // UnlockToResourceTypes: unlock -> reveals a resource.
            foreach (var m in Arr(root["UnlockToResourceTypes"]))
            {
                RefUnlockConsume(Str(m, "Unlock"), "Resources.json > UnlockToResourceTypes");
                RefResource(Str(m, "ResourceType"), "Resources.json > UnlockToResourceTypes");
            }
        }

        private void LoadAlterableValues()
        {
            var root = LoadFile("AlterableValues.json");
            foreach (var v in Arr(root))
            {
                var name = Str(v, "Name");
                DeclareUnique(_index.AlterableValues, name, "duplicate-alterable-value", "AlterableValues.json");

                foreach (var u in Arr(v["ValueUnlocks"]))
                    RefUnlockGrant(Str(u, "Unlock"), $"AlterableValues.json > {name} > ValueUnlocks");
            }
        }

        private void LoadTaskActions()
        {
            var root = LoadFile("TaskActions.json");
            if (root == null) return;

            // UnlockToActionMap: unlock -> action name.
            if (root["UnlockToActionMap"] is JObject map)
                foreach (var p in map.Properties())
                {
                    RefUnlockConsume(p.Name, "TaskActions.json > UnlockToActionMap");
                    RefAction(p.Value?.ToString(), $"TaskActions.json > UnlockToActionMap[{p.Name}]");
                }

            foreach (var a in Arr(root["ActionData"]))
            {
                var name = Str(a, "Name");
                DeclareUnique(_index.Actions, name, "duplicate-action", "TaskActions.json");

                foreach (var rc in Arr(a["ResourceChange"]))
                    RefResource(Str(rc, "ResourceType"), $"TaskActions.json > {name} > ResourceChange");

                var vd = a["ValueDeterminant"];
                if (vd?["UnlockAlterations"] is JObject ua)
                    foreach (var p in ua.Properties())
                    {
                        RefUnlockConsume(p.Name, $"TaskActions.json > {name} > UnlockAlterations");
                        foreach (var c in Arr(p.Value["CostChanges"]))
                            RefResource(Str(c, "ResourceType"), $"TaskActions.json > {name} > {p.Name} > CostChanges");
                        foreach (var c in Arr(p.Value["RewardChanges"]))
                            RefResource(Str(c, "ResourceType"), $"TaskActions.json > {name} > {p.Name} > RewardChanges");
                    }

                // AlterableValuePercentAdditions is an array of value-name strings.
                foreach (var pa in Arr(vd?["AlterableValuePercentAdditions"]))
                    RefValueName(pa?.ToString(), $"TaskActions.json > {name} > AlterableValuePercentAdditions");
            }
        }

        private void LoadDevelopments()
        {
            var root = LoadFile("Developments.json");
            if (root == null) return;

            foreach (var c in Arr(root["CostMultiplierUnlocks"]))
                RefUnlockConsume(Str(c, "Unlock"), "Developments.json > CostMultiplierUnlocks");

            int i = 0;
            foreach (var d in Arr(root["DevelopmentEntries"]))
            {
                var title = Str(d, "Title") ?? $"#{i}";
                foreach (var t in Arr(d["Trigger"]))
                    RefUnlockConsume(t?.ToString(), $"Developments.json > '{title}' > Trigger");
                foreach (var u in Arr(d["Unlock"]))
                    RefUnlockGrant(u?.ToString(), $"Developments.json > '{title}' > Unlock");
                foreach (var cost in Arr(d["Cost"]))
                    RefResource(Str(cost, "ResourceType"), $"Developments.json > '{title}' > Cost");
                foreach (var av in Arr(d["AlteredValues"]))
                {
                    RefValueName(Str(av, "ValueName"), $"Developments.json > '{title}' > AlteredValues");
                    RefUnlockConsume(Str(av, "Trigger"), $"Developments.json > '{title}' > AlteredValues.Trigger"); // optional/nullable
                }
                i++;
            }
        }

        private void LoadProjects()
        {
            var root = LoadFile("Projects.json");
            if (root == null) return;

            foreach (var g in Arr(root["GlobalEffects"]))
                RefUnlockConsume(Str(g, "Trigger"), "Projects.json > GlobalEffects");

            foreach (var p in Arr(root["Projects"]))
            {
                var name = Str(p, "Name") ?? "?";
                RefUnlockConsume(Str(p, "Trigger"), $"Projects.json > '{name}' > Trigger");
                RefUnlockGrant(Str(p, "Unlock"), $"Projects.json > '{name}' > Unlock");

                foreach (var av in Arr(p["AlteredValues"]))
                    RefValueName(Str(av, "ValueName"), $"Projects.json > '{name}' > AlteredValues");

                foreach (var lvl in Arr(p["Levels"]))
                    foreach (var pc in Arr(lvl["ProgressCosts"]))
                        RefResource(Str(pc, "ResourceType"), $"Projects.json > '{name}' > ProgressCosts");
            }
        }

        private void LoadBuildings()
        {
            var root = LoadFile("Buildings.json");
            if (root == null) return;

            foreach (var g in Arr(root["GlobalEffects"]))
            {
                RefUnlockConsume(Str(g, "Trigger"), "Buildings.json > GlobalEffects");

                //  A global effect only ever applies through its trigger, so a grant without one is
                //  authored land the player can never receive.
                if (g?["LandGranted"] != null && string.IsNullOrEmpty(Str(g, "Trigger")))
                    Add(ValidationSeverity.Warning, "unreachable-effect",
                        "Buildings.json > GlobalEffects grants land but has no Trigger, so it never applies.", "Buildings.json > GlobalEffects");
            }

            foreach (var lc in Arr(root["LandCost"]))
            {
                RefResource(Str(lc, "ResourceType"), "Buildings.json > LandCost");
                if (lc?["Amount"] != null && lc["ResourceValue"] == null)
                    Add(ValidationSeverity.Info, "field-name",
                        "Buildings.json > LandCost uses 'Amount' where the rest of the data uses 'ResourceValue'.", "Buildings.json > LandCost");
            }

            foreach (var b in Arr(root["BuildingTypes"]))
            {
                var name = Str(b, "Name") ?? "?";
                DeclareUnique(_index.Buildings, name, "duplicate-building", "Buildings.json");

                foreach (var tier in Arr(b["Costs"]))
                    foreach (var cost in Arr(tier["Cost"]))
                    {
                        RefResource(Str(cost, "ResourceType"), $"Buildings.json > {name} > Costs");
                        if (cost?["Amount"] != null && cost["ResourceValue"] == null)
                            Add(ValidationSeverity.Info, "field-name",
                                $"Buildings.json > {name} > Costs uses 'Amount' instead of 'ResourceValue'.", name);
                    }

                foreach (var av in Arr(b["AlteredValues"]))
                {
                    RefValueName(Str(av, "ValueName"), $"Buildings.json > {name} > AlteredValues");
                    RefUnlockConsume(Str(av, "Trigger"), $"Buildings.json > {name} > AlteredValues.Trigger"); // optional/nullable
                }

                foreach (var up in Arr(b["Upgrades"]))
                    RefUnlockConsume(Str(up, "Trigger"), $"Buildings.json > {name} > Upgrades");
            }
        }

        private void LoadArmies()
        {
            var root = LoadFile("Armies.json");
            if (root == null) return;

            foreach (var g in Arr(root["GlobalEffects"]))
                RefUnlockConsume(Str(g, "Trigger"), "Armies.json > GlobalEffects");

            foreach (var u in Arr(root["Units"]))
            {
                var name = Str(u, "Name") ?? "?";
                foreach (var cost in Arr(u["Cost"]))
                    RefResource(Str(cost, "ResourceType"), $"Armies.json > {name} > Cost");
                foreach (var up in Arr(u["Upgrades"]))
                    RefUnlockConsume(Str(up, "Trigger"), $"Armies.json > {name} > Upgrades");
            }
        }

        private void LoadBattles()
        {
            var root = LoadFile("Battles.json");
            if (root == null) return;

            foreach (var g in Arr(root["GlobalEffects"]))
                RefUnlockConsume(Str(g, "Trigger"), "Battles.json > GlobalEffects");

            foreach (var b in Arr(root["Battles"]))
                RefUnlockGrant(Str(b, "Unlock"), $"Battles.json > Battle {Str(b, "Battle")}");
        }

        // Generic reader for flat [{Trigger, ...}] files (Achievements, JournalEntries).
        private void LoadTriggerList(string file, bool isArrayRoot, string arrayField, string triggerField, string label)
        {
            var root = LoadFile(file);
            var list = isArrayRoot ? root : root?[arrayField];
            foreach (var e in Arr(list))
                RefUnlockConsume(Str(e, triggerField), $"{file} > {label}");
        }

        // ---- cross-reference resolution ---------------------------------

        private void CrossCheck()
        {
            // 1) Unknown unlocks: referenced anywhere but not declared in UnlockIDs.json.
            foreach (var kv in _index.UnlockRefs)
                if (!_index.DeclaredUnlocks.Contains(kv.Key))
                    _issues.Add(new ValidationIssue {
                        Severity = ValidationSeverity.Error, Category = "unknown-unlock",
                        Message = $"Unlock '{kv.Key}' is referenced but not declared in UnlockIDs.json.",
                        Location = string.Join(" ; ", kv.Value), Count = kv.Value.Count });

            // 2) Undefined alterable values: referenced ValueName not in AlterableValues.json.
            foreach (var kv in _index.ValueNameRefs)
                if (!string.IsNullOrEmpty(kv.Key) && !_index.AlterableValues.Contains(kv.Key))
                    _issues.Add(new ValidationIssue {
                        Severity = ValidationSeverity.Warning, Category = "undefined-alterable-value",
                        Message = $"AlteredValue '{kv.Key}' is not defined in AlterableValues.json (resolves to UNKNOWN/0 at runtime).",
                        Location = string.Join(" ; ", kv.Value), Count = kv.Value.Count });

            // 3) Unknown resources: a resource-cost id that is neither a ResourceType nor an AlterableValue.
            foreach (var kv in _index.ResourceRefs)
            {
                if (string.IsNullOrEmpty(kv.Key)) continue;
                if (_index.Resources.Contains(kv.Key)) continue;
                if (_index.AlterableValues.Contains(kv.Key))
                {
                    _issues.Add(new ValidationIssue {
                        Severity = ValidationSeverity.Info, Category = "resource-backed-by-value",
                        Message = $"'{kv.Key}' is used as a resource cost but is defined as an AlterableValue (expected for Land, etc.).",
                        Location = string.Join(" ; ", kv.Value), Count = kv.Value.Count });
                    continue;
                }
                _issues.Add(new ValidationIssue {
                    Severity = ValidationSeverity.Error, Category = "unknown-resource",
                    Message = $"Resource '{kv.Key}' is referenced but is neither a ResourceType nor an AlterableValue.",
                    Location = string.Join(" ; ", kv.Value), Count = kv.Value.Count });
            }

            // 4) Unknown actions: UnlockToActionMap points at an action not in ActionData.
            foreach (var kv in _index.ActionRefs)
                if (!string.IsNullOrEmpty(kv.Key) && !_index.Actions.Contains(kv.Key))
                    _issues.Add(new ValidationIssue {
                        Severity = ValidationSeverity.Error, Category = "unknown-action",
                        Message = $"Action '{kv.Key}' is referenced but not defined in TaskActions.json.",
                        Location = string.Join(" ; ", kv.Value), Count = kv.Value.Count });

            // 5) Ungranted gates: an unlock is consumed as a gate but nothing in the DATA grants it.
            //    Expected for unlocks the engine sets from events (delve depth, etc.) — hence a Warning.
            foreach (var id in _index.ConsumedUnlocks)
                if (_index.DeclaredUnlocks.Contains(id) && !_index.GrantedUnlocks.Contains(id))
                    _issues.Add(new ValidationIssue {
                        Severity = ValidationSeverity.Warning, Category = "gate-not-granted-by-data",
                        Message = $"Unlock '{id}' gates content but no data source grants it — confirm it is set by engine/event code.",
                        Location = _index.UnlockRefs.TryGetValue(id, out var l) ? string.Join(" ; ", l) : null });

            // 6) Orphan unlocks: declared but never referenced anywhere (dead entries / future work).
            foreach (var id in _index.DeclaredUnlocks)
                if (!_index.UnlockRefs.ContainsKey(id))
                    _issues.Add(new ValidationIssue {
                        Severity = ValidationSeverity.Info, Category = "orphan-unlock",
                        Message = $"Unlock '{id}' is declared in UnlockIDs.json but never referenced.",
                        Location = "UnlockIDs.json" });
        }

        // ---- report rendering -------------------------------------------

        public string BuildReport()
        {
            var sb = new StringBuilder();
            int err = _issues.Count(i => i.Severity == ValidationSeverity.Error);
            int warn = _issues.Count(i => i.Severity == ValidationSeverity.Warning);
            int info = _issues.Count(i => i.Severity == ValidationSeverity.Info);

            sb.AppendLine("HypnicEmpire — Game Data Validation Report");
            sb.AppendLine($"GameData: {_dir}");
            sb.AppendLine($"Declared: {_index.DeclaredUnlocks.Count} unlocks, {_index.Resources.Count} resources, " +
                          $"{_index.AlterableValues.Count} alterable values, {_index.Actions.Count} actions, {_index.Buildings.Count} buildings.");
            sb.AppendLine($"Result: {err} error(s), {warn} warning(s), {info} note(s).");
            sb.AppendLine(new string('-', 60));

            foreach (var grp in _issues
                         .OrderBy(i => i.Severity)
                         .ThenBy(i => i.Category)
                         .GroupBy(i => i.Severity))
            {
                sb.AppendLine();
                sb.AppendLine($"== {grp.Key.ToString().ToUpper()} ==");
                foreach (var issue in grp) sb.AppendLine("  " + issue);
            }
            return sb.ToString();
        }
    }

    // ---------------------------------------------------------------------
    // Editor menu wrapper — the only Unity-aware part.
    // ---------------------------------------------------------------------
    public static class GameDataValidatorMenu
    {
        private const string GameDataPath = "/GameData";              // under Application.dataPath (Assets)
        private const string ReportFileName = "GameDataValidationReport.txt";

        [MenuItem("HypnicEmpire/Validate Game Data")]
        public static void Validate()
        {
            var dir = Application.dataPath + GameDataPath;
            if (!Directory.Exists(dir))
            {
                Debug.LogError($"[GameDataValidator] GameData folder not found at {dir}");
                return;
            }

            var analyzer = new GameDataAnalyzer(dir);
            analyzer.Run();

            int err  = analyzer.Issues.Count(i => i.Severity == ValidationSeverity.Error);
            int warn = analyzer.Issues.Count(i => i.Severity == ValidationSeverity.Warning);
            int info = analyzer.Issues.Count(i => i.Severity == ValidationSeverity.Info);

            // Console summary + per-issue lines at matching log levels.
            Debug.Log($"[GameDataValidator] {err} error(s), {warn} warning(s), {info} note(s). " +
                      $"Declared {analyzer.Index.DeclaredUnlocks.Count} unlocks / {analyzer.Index.Resources.Count} resources / " +
                      $"{analyzer.Index.AlterableValues.Count} alterable values / {analyzer.Index.Actions.Count} actions.");

            foreach (var issue in analyzer.Issues.OrderBy(i => i.Severity).ThenBy(i => i.Category))
            {
                switch (issue.Severity)
                {
                    case ValidationSeverity.Error:   Debug.LogError("[GameData] " + issue);   break;
                    case ValidationSeverity.Warning: Debug.LogWarning("[GameData] " + issue); break;
                    default:                         Debug.Log("[GameData] " + issue);        break;
                }
            }

            // Write a full text report next to Assets/ for easy review/diffing.
            try
            {
                var reportPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ReportFileName);
                File.WriteAllText(reportPath, analyzer.BuildReport());
                Debug.Log($"[GameDataValidator] Full report written to {reportPath}");
            }
            catch (Exception e) { Debug.LogWarning($"[GameDataValidator] Could not write report file: {e.Message}"); }
        }
    }
}
#endif
