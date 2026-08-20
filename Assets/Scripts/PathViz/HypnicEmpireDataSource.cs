// Assets/Scripts/PathViz/HypnicEmpireDataSource.cs
//
// Incremental Path Visualizer — HypnicEmpire importer (the one game-specific piece)
// --------------------------------------------------------------------------------
// Reads the HypnicEmpire GameData JSON into the game-agnostic PathModel. Only this
// file knows HypnicEmpire's schema. Engine-free (System.IO + Newtonsoft +
// ComputedValue).
//
// The path is the unlock graph, and it has four kinds of unlock-producing options:
//
//   * Developments  — Trigger -> Cost -> Unlock (the tech-tree spine).
//   * Projects      — Trigger -> ProgressCosts -> Unlock.
//   * Actions       — an action producing resource X reveals X. The action is gated
//                     by its enabling unlock (UnlockToActionMap), which a development
//                     grants; acquiring it grants Unlock_Resource_X for each produced
//                     resource. This is how resources become available (the engine
//                     reveals a resource when you first gain it).
//   * Delve reaches — reaching a depth grouping. You ARRIVE at a grouping by surviving
//                     the levels BEFORE it, so a reach requires the reveal-unlocks of
//                     resources consumed by SHALLOWER groupings, the previous reach, and
//                     the Delve activity marker (reaching is a delving consequence, so it
//                     lands after the Delve action). Arriving grants the reach unlock and
//                     reveals resources produced at that depth. (Getting the "before, not
//                     including" right is what breaks the reach<->potion cycle and matches
//                     the game: a depth teaches you what the NEXT depth needs.)
//
//   * Thresholds    — a threshold unlock that GATES a path option (only Unlock_Empty_Belly
//                     does, on this data) is modeled as an option gated on the measured
//                     resource being available AND, if you reach it by delving (a level-
//                     consumed resource), on the Delve activity marker — so it lands one
//                     stage after the Delve action (the action, then the state it leads
//                     to) rather than at stage 0 alongside it. Only Unlock_Game_Start is a
//                     true seed; every other unlock is reached through the graph. Threshold
//                     unlocks that gate nothing (achievement-only counts) are omitted.
//   * Buildings     — revealed by the build/transform unlock each one declares in its
//                     RevealUnlock field. Leaf nodes today: their AlteredValues raise stats
//                     but they grant NO unlocks, so nothing gates on them yet. When a
//                     "building grants an unlock" mechanism is added (e.g. the structure that
//                     will enable Refine), populate the building option's GrantedUnlocks and
//                     that dependency resolves.
//
// DATA GAP (worth fixing in the data): LevelData has no field linking a grouping to
// its reach unlock, so the grouping->reach map is hardcoded below. Ideally each
// LevelGrouping declares the reach unlock it grants (and, later, the resource
// prerequisites), so this importer needs no game knowledge.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace HypnicEmpire.PathViz
{
    public sealed class HypnicEmpireDataSource : IGameDataSource
    {
        private readonly string _dir;
        public const string SeedUnlock = "Unlock_Game_Start";

        // Synthetic marker the Delve action grants, representing "delving is happening".
        // A threshold you reach BY delving (a resource the levels consume, e.g. Food ->
        // Empty_Belly) depends on this, so it lands one stage AFTER the Delve action
        // rather than at stage 0 alongside it — the action, then the state it leads to.
        private const string DelveActivity = "__Activity_Delve";

        // resources the delve/level system consumes (drives their depletion thresholds)
        private readonly HashSet<string> _delveConsumed = new();

        // resource id -> the unlock that reveals it (from Resources.json UnlockToResourceTypes)
        private readonly Dictionary<string, string> _reveal = new();

        // threshold unlock -> the resource whose availability gates it
        // (Resources.ResourceType.Unlocks and AlterableValues "ResourceGained_X".ValueUnlocks)
        private readonly Dictionary<string, string> _thresholdResource = new();

        // Grouping name -> reach unlock. Lives here only because LevelData carries no
        // data linkage; move into LevelData to make this fully data-driven.
        private static readonly Dictionary<string, string> ReachByGrouping = new()
        {
            { "Subsurface Bog",       "Unlock_Reach_Subsurface_Bog" },
            { "Frostbound Catacombs", "Unlock_Reach_Frostbound_Catacombs" },
            { "Ironclad Fortress",    "Unlock_Reach_Ironclad_Fortress" },
            { "Oily Grottos",         "Unlock_Reach_Oily_Grottos" },
            { "The Last Chamber",     "Unlock_Reach_The_Last_Chamber" },
            { "End Of The Hole",      "Unlock_Reach_The_End" },
        };

        public HypnicEmpireDataSource(string gameDataDirectory) { _dir = gameDataDirectory; }

        public PathModel Build()
        {
            var model = new PathModel();
            model.SeedUnlocks.Add(SeedUnlock);

            LoadDeclarations(model);   // fills Resources, AlterableValues, _reveal, _thresholdResource
            LoadDevelopments(model);
            LoadProjects(model);
            LoadActions(model);        // resource reveals
            LoadDelveReaches(model);   // depth chain
            LoadBuildings(model);      // reveal-gated leaf nodes (+ costs)
            LoadBattles(model);        // warfare chain (terminal spine -> defeat the Beast)
            LoadThresholdOptions(model); // must be last: needs every requirement known
            return model;
        }

        // ---- Economy model (quantitative pass, M8) ----------------------
        // Normalizes the resource/action/delve data the economy sim needs. Caps here are the
        // AUTHORED InitialMaximum (before runtime storage modifiers); building cap-raises are
        // the documented next increment. Delve is an action revealed by the seed (always on).
        public EconomyModel BuildEconomy()
        {
            // LandBuyUnlock: buildings all cost Land, and Land beyond the starting 5 is only buyable once
            // this unlock is granted ("People have come to work for you"). Before it, the loot economy has
            // no unbounded sink and the delve stalls. DATA GAP: there's no field linking the Land-purchase
            // transaction to its unlock, so it's named here (like the reach / building-reveal maps).
            string timingPath = Path.Combine(_dir, "ActionTiming.json");
            var timing = JsonSerialization.Deserialize<ActionTimingConfiguration>(File.ReadAllText(timingPath));
            if (timing?.Actions == null)
                throw new InvalidDataException($"Invalid action timing configuration at {timingPath}");

            var timingByAction = timing.Actions.ToDictionary(action => action.Name);
            var econ = new EconomyModel
            {
                SeedUnlock = SeedUnlock,
                LandBuyUnlock = "Unlock_Buying_Land",
                Timing = timing
            };

            var res = Load("Resources.json");
            foreach (var r in Arr(res?["ResourceTypes"]))
            {
                var name = Str(r, "Name");
                if (string.IsNullOrEmpty(name)) continue;
                var er = new EconomyResource
                {
                    Id = name,
                    InitialAmount = (int)Dbl(r, "InitialValue"),
                    InitialMaximum = (int)Dbl(r, "InitialMaximum"),
                    Group = Str(r, "ResourceGroup"),
                };
                if ((r as JObject)?["UnlockAlterations"] is JObject ua)
                    foreach (var p in ua.Properties())
                        er.UnlockAlterations[p.Name] = new ResourceAlteration
                        {
                            MaxAdditive = Dbl(p.Value, "MaxAdditive"),
                            MaxMultiplier = Dbl(p.Value, "MaxMultiplier"),
                        };
                econ.Resources.Add(er);
            }

            var ta = Load("TaskActions.json");
            var revealOf = new Dictionary<string, string>();      // actionName -> enabling unlock
            if (ta?["UnlockToActionMap"] is JObject map)
                foreach (var p in map.Properties())
                {
                    var actionName = p.Value?.ToString();
                    if (!string.IsNullOrEmpty(actionName)) revealOf[actionName] = p.Name;
                }
            foreach (var a in Arr(ta?["ActionData"]))
            {
                var name = Str(a, "Name");
                if (string.IsNullOrEmpty(name)) continue;
                if (!timingByAction.TryGetValue(name, out ActionTimingData actionTiming))
                    throw new InvalidDataException($"Action '{name}' has no entry in ActionTiming.json");

                var ea = new EconomyAction
                {
                    Id = name,
                    RevealUnlock = revealOf.TryGetValue(name, out var u) ? u : null,
                    Timing = actionTiming
                };
                foreach (var c in Arr(a["ResourceChange"]))
                {
                    var rt = Str(c, "ResourceType");
                    if (!string.IsNullOrEmpty(rt)) ea.Changes[rt] = Dbl(c, "ResourceValue");
                }
                econ.Actions.Add(ea);
            }

            // Delve track: each level entry carries its grouping's per-delve change + DelveCount.
            var ld = Load("LevelData.json");
            var groups = Arr(ld?["LevelGroupings"]).ToList();
            Dictionary<string, double> ChangeForLevel(int level)
            {
                foreach (var g in groups)
                    if (level >= Dbl(g, "Min") && level <= Dbl(g, "Max"))
                    {
                        var ch = new Dictionary<string, double>();
                        foreach (var c in Arr(g["LevelResourceChange"]))
                        {
                            var rt = Str(c, "ResourceType");
                            if (!string.IsNullOrEmpty(rt)) ch[rt] = Dbl(c, "ResourceValue");
                        }
                        return ch;
                    }
                return new Dictionary<string, double>();
            }
            foreach (var e in Arr(ld?["LevelDataEntries"]))
            {
                int level = (int)Dbl(e, "Level");
                var dl = new DelveLevel { Level = level, DelveCount = (int)Dbl(e, "DelveCount") };
                foreach (var kv in ChangeForLevel(level)) dl.Change[kv.Key] = kv.Value;
                econ.DelveTrack.Add(dl);
            }
            econ.DelveTrack.Sort((x, y) => x.Level.CompareTo(y.Level));

            // Reach unlock -> the level (grouping Min) it reaches, so the economy pass can place a
            // reach at the earliest stage whose delve depth covers it (uses the same grouping->reach map).
            foreach (var g in groups)
            {
                var gname = Str(g, "Name");
                if (gname != null && ReachByGrouping.TryGetValue(gname, out var ru))
                    econ.ReachLevelByUnlock[ru] = (int)Dbl(g, "Min");
            }

            // Buildings: base cost (drains a resource) + storage cap-raises (its AlteredValues that
            // name a Modifier whose Applications AddMax a resource / resource-group). This is the
            // ModifierValueSystem AddMax mechanic resolved statically for the economy pass.
            var avRoot = Load("AlterableValues.json");
            var addMaxOf = new Dictionary<string, List<(string kind, string name)>>();
            foreach (var v in Arr(avRoot))
            {
                var vn = Str(v, "Name");
                if (string.IsNullOrEmpty(vn) || Str(v, "Kind") != "Modifier") continue;
                foreach (var app in Arr(v["Applications"]))
                {
                    if (Str(app, "Op") != "AddMax") continue;
                    var target = Str(app, "Target") ?? "";
                    int colon = target.IndexOf(':');
                    if (colon < 0) continue;
                    var entry = (target.Substring(0, colon), target.Substring(colon + 1));
                    if (!addMaxOf.TryGetValue(vn, out var list)) { list = new List<(string, string)>(); addMaxOf[vn] = list; }
                    list.Add(entry);
                }
            }
            var groupMembers = new Dictionary<string, List<string>>();
            foreach (var er in econ.Resources)
                if (!string.IsNullOrEmpty(er.Group))
                {
                    if (!groupMembers.TryGetValue(er.Group, out var m)) { m = new List<string>(); groupMembers[er.Group] = m; }
                    m.Add(er.Id);
                }

            var bRoot = Load("Buildings.json");
            foreach (var b in Arr(bRoot?["BuildingTypes"]))
            {
                var name = Str(b, "Name");
                if (string.IsNullOrEmpty(name)) continue;
                var eb = new EconomyBuilding { Id = name, RevealUnlock = Str(b, "RevealUnlock") };

                foreach (var tier in Arr(b["Costs"]))       // base cost = the Count 0 tier
                {
                    if (Dbl(tier, "Count") != 0) continue;
                    foreach (var c in Arr(tier["Cost"]))
                    {
                        var rt = Str(c, "ResourceType");
                        if (string.IsNullOrEmpty(rt)) continue;
                        double amt = Dbl(c, "ResourceValue"); if (amt == 0) amt = Dbl(c, "Amount");
                        eb.BaseCost[rt] = amt;
                    }
                    break;
                }
                double landCost = eb.BaseCost.TryGetValue("Land", out var lc) ? lc : 0;
                eb.DirectlyBuildable = landCost > -1000000; // skip the Land -1e7 transform-target sentinels

                foreach (var av in Arr(b["AlteredValues"]))
                {
                    if (!string.IsNullOrEmpty(Str(av, "Trigger"))) continue; // conditional; ignored until modelled
                    var vn = Str(av, "ValueName");
                    if (string.IsNullOrEmpty(vn) || !addMaxOf.TryGetValue(vn, out var apps)) continue;
                    int amount = (int)Dbl(av, "Amount");
                    foreach (var (kind, tname) in apps)
                    {
                        if (kind == "Resource")
                            eb.CapRaisePerCopy[tname] = (eb.CapRaisePerCopy.TryGetValue(tname, out var e) ? e : 0) + amount;
                        else if (kind == "ResourceGroup" && groupMembers.TryGetValue(tname, out var members))
                            foreach (var mr in members)
                                eb.CapRaisePerCopy[mr] = (eb.CapRaisePerCopy.TryGetValue(mr, out var e2) ? e2 : 0) + amount;
                    }
                }
                econ.Buildings.Add(eb);
            }

            // ---- Warfare spine: army units (Armies.json), the enemy curve (Battles.json), and the
            // reachability-critical modifiers. Units are revealed by name convention (Unlock_Army_<Name>),
            // matching how the game gates them off the development chain (garrison -> Footsoldier, etc.).
            econ.RoyalArmyUnlock = "Unlock_Remains_Of_The_Royal_Army";  // +100 flat army strength
            econ.WardingUnlock   = "Unlock_Install_Warding_Stones";     // enemy strength ×0.7
            econ.StrengthDoublingUnlocks.Add("Unlock_Battle_Victory_Six");    // +100% unit strength (timing)
            econ.StrengthDoublingUnlocks.Add("Unlock_Battle_Victory_Twelve"); // +100% unit strength (timing)

            var armies = Load("Armies.json");
            foreach (var un in Arr(armies?["Units"]))
            {
                var name = Str(un, "Name");
                if (string.IsNullOrEmpty(name)) continue;
                var eu = new EconomyUnit
                {
                    Id = name,
                    Strength = Dbl(un, "Strength"),
                    RevealUnlock = "Unlock_Army_" + name.Replace(' ', '_'),
                    School = UnitSchool(un),
                };
                foreach (var c in Arr(un["Cost"]))
                {
                    var rt = Str(c, "ResourceType");
                    if (!string.IsNullOrEmpty(rt)) eu.Cost[rt] = Dbl(c, "ResourceValue");
                }
                econ.Units.Add(eu);
            }

            var battles = Load("Battles.json");
            foreach (var b in Arr(battles?["Battles"]))
            {
                var grant = Str(b, "Unlock");
                if (!string.IsNullOrEmpty(grant)) econ.EnemyStrengthByGrant[grant] = Dbl(b, "EnemyStrength");
            }

            return econ;
        }

        // A unit's cost-reduction school, read from its Upgrades: units whose upgrades reference the Bastion
        // are non-magical; those referencing the Secret of the Elves are magical. (Affects grind cost, not
        // reachability — kept for the timing layer.)
        private static string UnitSchool(JToken unit)
        {
            foreach (var up in Arr(unit["Upgrades"]))
            {
                var t = Str(up, "Trigger") ?? "";
                if (t.Contains("Secret_Of_The_Elves")) return "magical";
                if (t.Contains("Gradiose_Bastion"))    return "nonmagical";
            }
            return "nonmagical";
        }

        // ---- helpers ----------------------------------------------------
        private JToken Load(string file)
        {
            var path = Path.Combine(_dir, file);
            if (!File.Exists(path)) return null;
            try { return JToken.Parse(File.ReadAllText(path)); }
            catch { return null; }
        }

        private static IEnumerable<JToken> Arr(JToken n) => n is JArray a ? (IEnumerable<JToken>)a : Array.Empty<JToken>();

        private static string Str(JToken n, string field)
        {
            if (!(n is JObject o)) return null;
            var t = o[field];
            if (t == null || t.Type == JTokenType.Null) return null;
            return t.Type == JTokenType.String ? t.Value<string>() : t.ToString();
        }

        private static double Dbl(JToken n, string field)
        {
            var t = (n as JObject)?[field];
            return (t != null && (t.Type == JTokenType.Integer || t.Type == JTokenType.Float)) ? t.Value<double>() : 0.0;
        }

        private static ComputedValue Amount(JToken n, string field)
        {
            var t = (n as JObject)?[field];
            if (t != null && (t.Type == JTokenType.Integer || t.Type == JTokenType.Float))
                return ComputedValue.FromLiteral(t.Value<double>());
            return ComputedValue.FromLiteral(0.0);
        }

        // Reveal unlock for a resource, unless it is the seed (Food) — those need no granter.
        private bool TryRevealGrant(string resource, out string revealUnlock)
        {
            revealUnlock = null;
            if (resource != null && _reveal.TryGetValue(resource, out var rev) && rev != SeedUnlock)
            { revealUnlock = rev; return true; }
            return false;
        }

        // ---- declarations ----------------------------------------------
        private void LoadDeclarations(PathModel model)
        {
            var res = Load("Resources.json");
            foreach (var r in Arr(res?["ResourceTypes"]))
            {
                var name = Str(r, "Name");
                if (string.IsNullOrEmpty(name)) continue;
                model.Resources.Add(name);
                // A threshold on this resource is gated by the resource being available.
                foreach (var u in Arr(r["Unlocks"]))
                {
                    var un = Str(u, "Unlock");
                    if (!string.IsNullOrEmpty(un)) _thresholdResource[un] = name;
                }
            }
            foreach (var m in Arr(res?["UnlockToResourceTypes"]))
            {
                var rt = Str(m, "ResourceType"); var ul = Str(m, "Unlock");
                if (!string.IsNullOrEmpty(rt) && !string.IsNullOrEmpty(ul)) _reveal[rt] = ul;
            }

            var av = Load("AlterableValues.json");
            foreach (var v in Arr(av))
            {
                var name = Str(v, "Name");
                if (string.IsNullOrEmpty(name)) continue;
                model.AlterableValues.Add(name);
                // "ResourceGained_X" thresholds are gated by resource X being available.
                if (name.StartsWith("ResourceGained_", StringComparison.Ordinal))
                {
                    var resource = name.Substring("ResourceGained_".Length);
                    foreach (var u in Arr(v["ValueUnlocks"]))
                    {
                        var un = Str(u, "Unlock");
                        if (!string.IsNullOrEmpty(un)) _thresholdResource[un] = resource;
                    }
                }
            }
        }

        // ---- Threshold unlocks that GATE an option --------------------
        // Modeled as options gated on the measured resource being available (you can
        // only cross a resource threshold once that resource is producible). Threshold
        // unlocks that gate nothing (achievement-only) are omitted to keep the graph
        // focused. On this data exactly one qualifies: Unlock_Empty_Belly (Food, which
        // is available from the seed), so "run out of food" is reachable at the start.
        private void LoadThresholdOptions(PathModel model)
        {
            var required = new HashSet<string>(model.AllRequiredUnlocks());
            var granted = new HashSet<string>(model.AllGrantedUnlocks());
            foreach (var u in required)
            {
                if (granted.Contains(u)) continue;
                if (!_thresholdResource.TryGetValue(u, out var resource)) continue; // not a resource threshold -> stays unresolved (pending)
                var opt = new PathOption { Id = $"thr:{u}", Kind = OptionKind.ThresholdUnlock, Display = $"Threshold: {u}", SourceRef = "Resources/AlterableValues thresholds" };
                if (_reveal.TryGetValue(resource, out var rev)) opt.RequiredUnlocks.Add(rev); // resource must be available
                if (_delveConsumed.Contains(resource)) opt.RequiredUnlocks.Add(DelveActivity); // reached BY delving -> one stage after Delve
                opt.GrantedUnlocks.Add(u);
                model.Options.Add(opt);
            }
        }

        // ---- Developments ----------------------------------------------
        private void LoadDevelopments(PathModel model)
        {
            var root = Load("Developments.json");
            int i = 0;
            foreach (var d in Arr(root?["DevelopmentEntries"]))
            {
                var title = Str(d, "Title") ?? $"Development {i}";
                var opt = new PathOption { Id = $"dev:{i}", Kind = OptionKind.Development, Display = title, SourceRef = $"Developments.json > '{title}'" };
                foreach (var t in Arr(d["Trigger"])) if (t != null) opt.RequiredUnlocks.Add(t.ToString());
                foreach (var u in Arr(d["Unlock"]))  if (u != null) opt.GrantedUnlocks.Add(u.ToString());
                foreach (var c in Arr(d["Cost"]))
                {
                    var rt = Str(c, "ResourceType");
                    if (!string.IsNullOrEmpty(rt)) opt.Costs.Add(new ResourceCost(rt, Amount(c, "ResourceValue")));
                }
                model.Options.Add(opt);
                i++;
            }
        }

        // ---- Projects ---------------------------------------------------
        private void LoadProjects(PathModel model)
        {
            var root = Load("Projects.json");
            int i = 0;
            foreach (var p in Arr(root?["Projects"]))
            {
                var name = Str(p, "Name") ?? $"Project {i}";
                var opt = new PathOption { Id = $"proj:{i}", Kind = OptionKind.Project, Display = name, SourceRef = $"Projects.json > '{name}'" };
                var trigger = Str(p, "Trigger");  if (!string.IsNullOrEmpty(trigger)) opt.RequiredUnlocks.Add(trigger);
                var unlock = Str(p, "Unlock");    if (!string.IsNullOrEmpty(unlock))  opt.GrantedUnlocks.Add(unlock);
                foreach (var lvl in Arr(p["Levels"]))
                    foreach (var pc in Arr(lvl["ProgressCosts"]))
                    {
                        var rt = Str(pc, "ResourceType");
                        if (!string.IsNullOrEmpty(rt)) opt.Costs.Add(new ResourceCost(rt, Amount(pc, "ResourceValue")));
                    }
                model.Options.Add(opt);
                i++;
            }
        }

        // ---- Battles: the warfare chain (terminal spine) ---------------
        // Each battle is gated by army strength vs. its enemy strength (the quantitative gate lives in
        // PathEconomy). Structurally the chain is by convention: Battle 1 requires Unlock_Warfare; Battle N
        // requires the previous battle's victory unlock. Winning grants the battle's Unlock (victory N, or
        // Unlock_Defeat_The_Beast for the final Battle 19, which ends the game). Enemy strengths feed the
        // economy model in BuildEconomy(). No data edit needed — the chain follows the victory-unlock naming.
        private void LoadBattles(PathModel model)
        {
            var root = Load("Battles.json");
            string prevVictory = null;
            foreach (var b in Arr(root?["Battles"]))
            {
                int num = (int)Dbl(b, "Battle");
                var name = Str(b, "Name") ?? $"Battle {num}";
                var grant = Str(b, "Unlock");
                var opt = new PathOption { Id = $"battle:{num}", Kind = OptionKind.Battle, Display = $"Battle {num}: {name}", SourceRef = $"Battles.json > {name}" };
                opt.RequiredUnlocks.Add(prevVictory ?? "Unlock_Warfare");
                if (!string.IsNullOrEmpty(grant)) opt.GrantedUnlocks.Add(grant);
                model.Options.Add(opt);
                prevVictory = grant;
            }
        }

        // ---- Actions: enabling unlock -> reveals produced resources -----
        private void LoadActions(PathModel model)
        {
            var ta = Load("TaskActions.json");

            // UnlockToActionMap is { enablingUnlock : actionName }; invert to action -> enabling unlock.
            var enableOf = new Dictionary<string, string>();
            if (ta?["UnlockToActionMap"] is JObject map)
                foreach (var p in map.Properties())
                {
                    var actionName = p.Value?.ToString();
                    if (!string.IsNullOrEmpty(actionName)) enableOf[actionName] = p.Name;
                }

            foreach (var a in Arr(ta?["ActionData"]))
            {
                var name = Str(a, "Name");
                if (name == null || !enableOf.TryGetValue(name, out var enable) || string.IsNullOrEmpty(enable)) continue;

                var opt = new PathOption { Id = $"act:{name}", Kind = OptionKind.Action, Display = $"Action: {name}", SourceRef = $"TaskActions.json > {name}" };
                opt.RequiredUnlocks.Add(enable);

                var grants = new HashSet<string>();
                foreach (var c in Arr(a["ResourceChange"]))
                    if (Dbl(c, "ResourceValue") > 0 && TryRevealGrant(Str(c, "ResourceType"), out var rev))
                        grants.Add(rev);
                foreach (var gr in grants) opt.GrantedUnlocks.Add(gr);

                if (name == "Delve") opt.GrantedUnlocks.Add(DelveActivity); // delving drives level-resource thresholds

                model.Options.Add(opt);
            }
        }

        // ---- Delve reaches: arrive by surviving SHALLOWER groupings -----
        private void LoadDelveReaches(PathModel model)
        {
            var ld = Load("LevelData.json");
            var groups = Arr(ld?["LevelGroupings"]).ToList();
            groups.Sort((x, y) => Dbl(x, "Min").CompareTo(Dbl(y, "Min")));

            var consumedShallower = new HashSet<string>();
            string prevReach = null;

            foreach (var grp in groups)
            {
                var gname = Str(grp, "Name");

                if (gname != null && ReachByGrouping.TryGetValue(gname, out var reachUnlock))
                {
                    var opt = new PathOption { Id = $"reach:{reachUnlock}", Kind = OptionKind.Reach, Display = $"Reach: {gname}", SourceRef = $"LevelData.json > {gname}" };

                    // Requires: the resources consumed by everything ABOVE this grouping.
                    foreach (var r in consumedShallower)
                        if (TryRevealGrant(r, out var rev) && !opt.RequiredUnlocks.Contains(rev))
                            opt.RequiredUnlocks.Add(rev);
                    if (prevReach != null) opt.RequiredUnlocks.Add(prevReach);
                    opt.RequiredUnlocks.Add(DelveActivity); // reaching a depth is a delving consequence -> after the Delve action

                    // Grants: the reach unlock + reveals of resources produced at this depth.
                    opt.GrantedUnlocks.Add(reachUnlock);
                    foreach (var c in Arr(grp["LevelResourceChange"]))
                        if (Dbl(c, "ResourceValue") > 0 && TryRevealGrant(Str(c, "ResourceType"), out var rev) && !opt.GrantedUnlocks.Contains(rev))
                            opt.GrantedUnlocks.Add(rev);

                    model.Options.Add(opt);
                    prevReach = reachUnlock;
                }

                // Now fold THIS grouping's consumption into the running shallower set.
                foreach (var c in Arr(grp["LevelResourceChange"]))
                    if (Dbl(c, "ResourceValue") < 0)
                    {
                        var rt = Str(c, "ResourceType");
                        if (!string.IsNullOrEmpty(rt)) { consumedShallower.Add(rt); _delveConsumed.Add(rt); }
                    }
            }
        }

        // ---- Buildings: revealed by a build/transform unlock ----------
        private void LoadBuildings(PathModel model)
        {
            var root = Load("Buildings.json");
            foreach (var b in Arr(root?["BuildingTypes"]))
            {
                var name = Str(b, "Name");
                if (string.IsNullOrEmpty(name)) continue;

                var opt = new PathOption { Id = $"bld:{name}", Kind = OptionKind.Building, Display = $"Building: {name}", SourceRef = $"Buildings.json > {name}" };
                var reveal = Str(b, "RevealUnlock");
                if (!string.IsNullOrEmpty(reveal))
                    opt.RequiredUnlocks.Add(reveal);
                // Buildings grant no unlocks in the current data (see header). Populate
                // opt.GrantedUnlocks when a building->unlock mechanism is added.

                foreach (var tier in Arr(b["Costs"]))     // base cost = the Count 0 tier
                {
                    if (Dbl(tier, "Count") != 0) continue;
                    foreach (var c in Arr(tier["Cost"]))
                    {
                        var rt = Str(c, "ResourceType");
                        if (!string.IsNullOrEmpty(rt)) opt.Costs.Add(new ResourceCost(rt, Amount(c, "ResourceValue")));
                    }
                    break;
                }
                model.Options.Add(opt);
            }
        }
    }
}
