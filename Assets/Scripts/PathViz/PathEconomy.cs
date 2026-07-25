// Assets/Scripts/PathViz/PathEconomy.cs
//
// Incremental Path Visualizer — quantitative pass (engine-free core, M8)
// ---------------------------------------------------------------------
// The structural pass (PathSimulation) stages the unlock graph but cannot place the delve
// reaches, because delve depth is gated by the resource ECONOMY, not by unlock strings. This
// computes the deepest delve level reachable from a set of owned developments.
//
// The model (validated against the early-game ground truth; see the M8 design doc):
//   * Each delve at a level applies its grouping's per-delve resource change; a level takes
//     DelveCount delves to clear.
//   * A delve is permitted while every capped OUTPUT has room and every INPUT is suppliable.
//   * Output ROOM is a recursive BUDGET: a resource's room = its own (cap − init + spend), PLUS,
//     for each converter action that consumes it and produces P, budget(P)/rateOut × rateIn — so a
//     converter only drains as far as its product can be absorbed. Terminal (unbounded) sinks:
//     resources consumed by delving at a reached depth, and — only once land-buying is unlocked —
//     the resources buildings consume/cap-raise (buildings are the unbounded money/loot sink). This
//     is why, before "buying land", Market fills Money to its cap and then Treasure walls too.
//   * Caps include unlock-driven alterations (e.g. Unlock_Double_Max_Storage_Space ×2).
//   * SaturatedDepth is a JOINT fixpoint: developments apply their grants only when their triggers
//     are (economically) met, and REACH unlocks are granted by achieved depth — not by the too-early
//     structural staging. This stops reach-gated content (e.g. potions) from leaking in early.
//
// No UnityEngine / UnityEditor dependency.
//
using System;
using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire.PathViz
{
    public struct ResourceAlteration { public double MaxAdditive; public double MaxMultiplier; }

    /// <summary>A resource's starting amount, authored cap, and unlock-driven cap alterations.</summary>
    public sealed class EconomyResource
    {
        public string Id;
        public int InitialAmount;
        public int InitialMaximum;
        public string Group;
        public readonly Dictionary<string, ResourceAlteration> UnlockAlterations = new(); // unlock -> cap change
    }

    /// <summary>A repeatable action's per-completion resource deltas and the unlock that reveals it.</summary>
    public sealed class EconomyAction
    {
        public string Id;
        public string RevealUnlock;                               // null/empty => available from the start (e.g. Delve)
        public double DefaultSpeed = 300;                         // ValueDeterminant.DefaultSpeed; period ≈ ProgressMax/speed
        public readonly Dictionary<string, double> Changes = new(); // resource -> delta per completion (>0 produce, <0 consume)
    }

    /// <summary>One level of the delve track: the grouping's per-delve change and how many delves clear it.</summary>
    public sealed class DelveLevel
    {
        public int Level;
        public int DelveCount;
        public readonly Dictionary<string, double> Change = new(); // resource -> delta per delve at this depth
    }

    /// <summary>A building: base (Count 0) cost, the storage caps it raises per copy, and its reveal unlock.</summary>
    public sealed class EconomyBuilding
    {
        public string Id;
        public string RevealUnlock;
        public bool DirectlyBuildable;                            // false for transform-target sentinels (Land -1e7)
        public readonly Dictionary<string, double> BaseCost = new();     // resource -> amount (negative = spent); includes "Land"
        public readonly Dictionary<string, int> CapRaisePerCopy = new(); // resource -> cap increase per copy
    }

    /// <summary>An army unit: strength per copy, its cost, magical/non-magical school, and reveal unlock.
    /// Buying units raises ArmyStrength, which vs. a battle's enemy strength determines the win chance.</summary>
    public sealed class EconomyUnit
    {
        public string Id;
        public double Strength;
        public string School;                                    // "magical" | "nonmagical" (for cost-reduction upgrades)
        public string RevealUnlock;                              // Unlock_Army_<Name> (name convention)
        public readonly Dictionary<string, double> Cost = new(); // resource -> amount (negative = spent)
    }

    /// <summary>The economy primitives the quantitative pass reads (game-agnostic; built by a data source).</summary>
    public sealed class EconomyModel
    {
        public readonly List<EconomyResource> Resources = new();
        public readonly List<EconomyAction> Actions = new();
        public readonly List<DelveLevel> DelveTrack = new();      // ordered shallow -> deep
        public readonly List<EconomyBuilding> Buildings = new();
        public readonly Dictionary<string, int> ReachLevelByUnlock = new(); // reach unlock -> the level (grouping Min) it reaches
        public string SeedUnlock = "Unlock_Game_Start";
        public string LandBuyUnlock;                             // the unlock that makes Land (hence buildings) unbounded

        // ---- warfare spine (the second end-game track; terminal = defeating the Beast) ----
        public readonly List<EconomyUnit> Units = new();
        public readonly Dictionary<string, double> EnemyStrengthByGrant = new(); // battle-victory unlock -> enemy strength
        public string RoyalArmyUnlock;                           // grants a flat army-strength bonus (Remains of the Royal Army)
        public double RoyalArmyBonus = 100;
        public string WardingUnlock;                             // reduces enemy strength (Install Warding Stones)
        public double WardingEnemyMultiplier = 0.7;              // enemy strength ×0.7 once granted
        public readonly List<string> StrengthDoublingUnlocks = new(); // each doubles unit strength (Victories 6 & 12) — timing only
        public double GuaranteedWinRatio = 2.0;                  // own >= ratio × effectiveEnemy => deterministic clear (100% win)
    }

    /// <summary>How deep the delve reaches for a given owned set, and what stopped it.</summary>
    public struct DepthResult
    {
        public int ClearedLevel;    // deepest level fully cleared (0 = none)
        public int PartialDelves;   // delves completed into the next (walling) level
        public int PartialOf;       // that level's DelveCount
        public string WallReason;   // human text
        public string WallResource; // which resource walled (null at the end of the track)
        public bool WallIsInput;    // true = a missing input (a faucet is needed, not a building)

        public override string ToString()
        {
            var tail = PartialDelves > 0 ? $" +{PartialDelves}/{PartialOf} into L{ClearedLevel + 1}" : "";
            return $"cleared L{ClearedLevel}{tail}  ({WallReason})";
        }
    }

    /// <summary>Grind effort to reach a delve depth: action counts (portable) + a single-worker seconds estimate.</summary>
    public struct GrindEstimate
    {
        public int TargetLevel;
        public long DelveActions;
        public Dictionary<string, double> SupplyActions;
        public double TotalSupplyActions;
        public string DominantResource;
        public double DominantActions;
        public double RawSecondsSingleWorker;

        public override string ToString()
            => $"L{TargetLevel}: {DelveActions} delves + {(long)TotalSupplyActions} supply actions" +
               (DominantResource != null ? $" (grind: {DominantResource} ×{(long)DominantActions})" : "");
    }

    /// <summary>Economy-derived annotations the visualizer overlays on the structural graph.</summary>
    public sealed class EconomyAnnotation
    {
        public readonly Dictionary<int, DepthResult> DepthByStage = new();
        public readonly Dictionary<string, int> ReachLevel = new();
        public readonly Dictionary<string, int> ReachEconomyStage = new();
        public readonly Dictionary<string, GrindEstimate> ReachGrind = new();
        public readonly Dictionary<string, int> OptionEconomyStage = new(); // option id -> earliest stage it economically fires
        public readonly Dictionary<string, double> BattleEnemy = new();     // battle option id -> enemy strength (for display)
    }

    public static class PathEconomy
    {
        public const double ProgressMax = 1000.0;

        // ============================ core depth ============================

        /// <summary>Deepest delve level given granted unlocks, per-resource spend, a depth used for the
        /// "consumed-by-delving" sinks, and optional explicit building counts (deterministic mode).</summary>
        public static DepthResult MaxDepthCore(EconomyModel econ, ISet<string> granted,
                                               IReadOnlyDictionary<string, double> spent, int sinkDepth,
                                               IReadOnlyDictionary<string, int> buildingCounts = null)
        {
            var res = econ.Resources.ToDictionary(r => r.Id);
            var producible = Producible(econ, granted);
            var budget = ResourceBudgets(econ, granted, spent, sinkDepth, buildingCounts);

            int Init(string r) => res.TryGetValue(r, out var ri) ? ri.InitialAmount : 0;
            double OutBudget(string r) => budget.TryGetValue(r, out var b) ? b : 0;
            double InBudget(string r) => producible.Contains(r) ? double.PositiveInfinity : Init(r);

            var produced = new Dictionary<string, double>();
            var consumed = new Dictionary<string, double>();
            double Prod(string r) => produced.TryGetValue(r, out var v) ? v : 0.0;
            double Cons(string r) => consumed.TryGetValue(r, out var v) ? v : 0.0;

            foreach (var lvl in econ.DelveTrack.OrderBy(l => l.Level))
            {
                for (int d = 0; d < lvl.DelveCount; d++)
                {
                    foreach (var kv in lvl.Change)
                    {
                        string r = kv.Key; double v = kv.Value;
                        if (v < 0 && Cons(r) + (-v) > InBudget(r))
                            return Wall(lvl, d, r, true, $"{r} input exhausted (supply {Fmt(InBudget(r))})");
                        if (v > 0 && Prod(r) + v > OutBudget(r))
                            return Wall(lvl, d, r, false, $"{r} output capped (room {Fmt(OutBudget(r))})");
                    }
                    foreach (var kv in lvl.Change)
                    {
                        if (kv.Value > 0) produced[kv.Key] = Prod(kv.Key) + kv.Value;
                        else if (kv.Value < 0) consumed[kv.Key] = Cons(kv.Key) + (-kv.Value);
                    }
                }
            }
            int deepest = econ.DelveTrack.Count > 0 ? econ.DelveTrack.Max(l => l.Level) : 0;
            return new DepthResult { ClearedLevel = deepest, WallReason = "reached the end" };
        }

        /// <summary>Deterministic depth from an owned option set + explicit building counts (validation / queries).</summary>
        public static DepthResult MaxDepth(PathModel model, EconomyModel econ, IEnumerable<string> ownedOptionIds,
                                           IReadOnlyDictionary<string, int> buildingCounts = null)
        {
            OwnedToEconomy(model, econ, ownedOptionIds, out var granted, out var spent);
            return MaxDepthCore(econ, granted, spent, int.MaxValue, buildingCounts);
        }

        // ==================== saturated (joint fixpoint) ====================

        /// <summary>
        /// The correct economy depth for a set of owned developments. Joint fixpoint: developments apply
        /// their grants only when their triggers are met, non-purchase options (actions/thresholds) fire
        /// from unlocks, and REACH unlocks are granted by achieved depth (not the too-early structural
        /// staging). Output room is the recursive converter budget with the land-buying gate — so before
        /// land-buying the loot economy can't drain and the delve stalls shallow.
        /// </summary>
        public static DepthResult SaturatedDepth(PathModel model, EconomyModel econ, IEnumerable<string> ownedDevIds)
            => SaturatedDepth(model, econ, ownedDevIds, out _);

        /// <summary>As SaturatedDepth, also reporting which options economically FIRED (became available).
        /// A reach-gated development fires only once the reach is economically achieved — so its economy
        /// stage (earliest firing) is later than its too-early structural stage.</summary>
        public static DepthResult SaturatedDepth(PathModel model, EconomyModel econ, IEnumerable<string> ownedDevIds,
                                                 out HashSet<string> firedOptionIds)
        {
            var ownedDevs = new HashSet<string>(ownedDevIds);
            var granted = new HashSet<string>(model.SeedUnlocks) { econ.SeedUnlock };
            var spent = new Dictionary<string, double>();
            var fired = new HashSet<string>();
            var ctx = new ExpressionContext();
            DepthResult depth = default;
            int prevLevel = -1;

            for (int iter = 0; iter < 64; iter++)
            {
                // Fire every non-reach option whose requirements are met (developments only if owned).
                bool progressed = true;
                while (progressed)
                {
                    progressed = false;
                    foreach (var o in model.Options)
                    {
                        if (fired.Contains(o.Id) || o.Kind == OptionKind.Reach) continue;
                        if (o.Kind == OptionKind.Development && !ownedDevs.Contains(o.Id)) continue;
                        if (!o.RequiredUnlocks.All(granted.Contains)) continue;
                        fired.Add(o.Id);
                        foreach (var u in o.GrantedUnlocks) granted.Add(u);
                        foreach (var c in o.Costs)
                        {
                            double amt = c.Amount != null ? c.Amount.Evaluate(ctx) : 0.0;
                            if (amt < 0) spent[c.Resource] = (spent.TryGetValue(c.Resource, out var s) ? s : 0.0) + (-amt);
                        }
                        progressed = true;
                    }
                }

                depth = MaxDepthCore(econ, granted, spent, depth.ClearedLevel);

                // Fire reaches the delve has now reached (arrival = level-1).
                bool firedReach = false;
                foreach (var o in model.Options)
                {
                    if (o.Kind != OptionKind.Reach || fired.Contains(o.Id)) continue;
                    int level = ReachLevelOf(econ, o);
                    if (level > 0 && depth.ClearedLevel >= level - 1)
                    {
                        fired.Add(o.Id);
                        foreach (var u in o.GrantedUnlocks) granted.Add(u);
                        firedReach = true;
                    }
                }

                if (depth.ClearedLevel == prevLevel && !firedReach) break;
                prevLevel = depth.ClearedLevel;
            }
            firedOptionIds = fired;
            return depth;
        }

        // ==================== resource budgets ====================

        // Output ROOM per resource: own capacity + recursive converter pass-through, with unbounded sinks.
        private static Dictionary<string, double> ResourceBudgets(EconomyModel econ, ISet<string> granted,
            IReadOnlyDictionary<string, double> spent, int sinkDepth, IReadOnlyDictionary<string, int> buildingCounts)
        {
            var res = econ.Resources.ToDictionary(r => r.Id);
            var producible = Producible(econ, granted);
            var acts = econ.Actions.Where(a => string.IsNullOrEmpty(a.RevealUnlock) || granted.Contains(a.RevealUnlock)).ToList();

            // Unbounded sinks: consumed by delving at a reached depth; and (once land-buying is unlocked)
            // everything affordable revealed buildings consume or cap-raise — the unbounded loot/money sink.
            var unbounded = new HashSet<string>();
            foreach (var lvl in econ.DelveTrack)
                if (lvl.Level <= sinkDepth)
                    foreach (var kv in lvl.Change)
                        if (kv.Value < 0) unbounded.Add(kv.Key);

            bool landBuy = !string.IsNullOrEmpty(econ.LandBuyUnlock) && granted.Contains(econ.LandBuyUnlock);
            if (landBuy)
                foreach (var b in econ.Buildings)
                {
                    if (!b.DirectlyBuildable || string.IsNullOrEmpty(b.RevealUnlock) || !granted.Contains(b.RevealUnlock)) continue;
                    bool usable = b.BaseCost.All(kv => kv.Value >= 0 || kv.Key == "Land" || kv.Key == "Money" || producible.Contains(kv.Key));
                    if (!usable) continue;
                    foreach (var kv in b.BaseCost) if (kv.Value < 0 && kv.Key != "Land") unbounded.Add(kv.Key);
                    foreach (var kv in b.CapRaisePerCopy) unbounded.Add(kv.Key);
                }

            // Converter edges: action consuming R (rate rin) producing P (rate rout).
            var edges = new List<(string r, double rin, string p, double rout)>();
            foreach (var a in acts)
                foreach (var i in a.Changes) if (i.Value < 0)
                    foreach (var o in a.Changes) if (o.Value > 0)
                        edges.Add((i.Key, -i.Value, o.Key, o.Value));

            double Own(string r)
            {
                int cap = res.TryGetValue(r, out var ri) ? EffectiveCap(ri, granted, buildingCounts, econ) : 0;
                int init = ri != null ? ri.InitialAmount : 0;
                double s = spent.TryGetValue(r, out var sp) ? sp : 0.0;
                return (cap - init) + s;
            }

            var memo = new Dictionary<string, double>();
            double Budget(string r, HashSet<string> stack)
            {
                if (unbounded.Contains(r)) return double.PositiveInfinity;
                if (memo.TryGetValue(r, out var m)) return m;
                if (stack.Contains(r)) return Own(r);           // break cycles with own-capacity only
                stack.Add(r);
                double val = Own(r);
                foreach (var e in edges)
                {
                    if (e.r != r) continue;
                    double bp = Budget(e.p, stack);
                    if (double.IsPositiveInfinity(bp)) { val = double.PositiveInfinity; break; }
                    val += bp / e.rout * e.rin;
                }
                stack.Remove(r);
                memo[r] = val;
                return val;
            }

            var result = new Dictionary<string, double>();
            foreach (var r in econ.Resources) result[r.Id] = Budget(r.Id, new HashSet<string>());
            return result;
        }

        /// <summary>Effective cap = InitialMaximum, then active unlock alterations (add then mult), + building AddMax.</summary>
        public static int EffectiveCap(EconomyResource r, ISet<string> granted,
                                       IReadOnlyDictionary<string, int> buildingCounts, EconomyModel econ)
        {
            double m = r.InitialMaximum;
            foreach (var ua in r.UnlockAlterations)
                if (granted.Contains(ua.Key)) { m += ua.Value.MaxAdditive; m *= ua.Value.MaxMultiplier == 0 ? 1 : ua.Value.MaxMultiplier; }
            if (buildingCounts != null)
                foreach (var b in econ.Buildings)
                    if (buildingCounts.TryGetValue(b.Id, out var c) && c > 0 && b.CapRaisePerCopy.TryGetValue(r.Id, out var cr))
                        m += cr * c;
            return (int)m;
        }

        // ==================== annotation & chokepoints ====================

        /// <summary>
        /// Unified coarse-round economy staging. ONE fixpoint over the whole graph: each round fires every
        /// not-yet-fired non-reach option whose requirements are granted, plus every reach whose arrival
        /// level (level-1) the current delve depth has reached; applies their grants and spend; recomputes
        /// depth for the next round. Unlike per-structural-stage placement, this keeps early-game wave
        /// granularity AND sequences the deep game correctly (a reach can't fire before the development that
        /// deepens the delve enough to reach it). optionStage[id] = the round an option first fires;
        /// depthByRound[round] = the delve depth cleared after that round's grants.
        /// </summary>
        public static void EconomyStaging(PathModel model, EconomyModel econ,
                                          out Dictionary<string, int> optionStage,
                                          out Dictionary<int, int> depthByRound)
        {
            var granted = new HashSet<string>(model.SeedUnlocks) { econ.SeedUnlock };
            var spent = new Dictionary<string, double>();
            var fired = new HashSet<string>();
            optionStage = new Dictionary<string, int>();
            depthByRound = new Dictionary<int, int>();
            var ctx = new ExpressionContext();
            int depth = 0;

            for (int round = 0; round < 1024; round++)
            {
                double army = MaxArmyStrength(econ, granted);   // current fieldable army strength (∞ once a unit is buildable)
                var newly = new List<PathOption>();
                foreach (var o in model.Options)
                {
                    if (fired.Contains(o.Id)) continue;
                    if (o.Kind == OptionKind.Reach)
                    {
                        int lvl = ReachLevelOf(econ, o);
                        if (lvl > 0 && depth >= lvl - 1) newly.Add(o);
                    }
                    else if (o.Kind == OptionKind.Battle)
                    {
                        // Battle chain is in RequiredUnlocks (prev victory / Warfare); the quantitative gate is
                        // enough army strength to guarantee the win (own >= ratio × effectiveEnemy).
                        if (o.RequiredUnlocks.All(granted.Contains))
                        {
                            double enemy = BattleEnemyOf(econ, o);
                            if (enemy <= 0 || army >= BattleThreshold(econ, enemy, granted)) newly.Add(o);
                        }
                    }
                    else if (o.RequiredUnlocks.All(granted.Contains)) newly.Add(o);
                }
                if (newly.Count == 0) break;

                foreach (var o in newly) { fired.Add(o.Id); optionStage[o.Id] = round; }
                foreach (var o in newly)
                {
                    foreach (var u in o.GrantedUnlocks) granted.Add(u);
                    foreach (var c in o.Costs)
                    {
                        double amt = c.Amount != null ? c.Amount.Evaluate(ctx) : 0.0;
                        if (amt < 0) spent[c.Resource] = (spent.TryGetValue(c.Resource, out var s) ? s : 0.0) + (-amt);
                    }
                }
                depth = MaxDepthCore(econ, granted, spent, depth).ClearedLevel;
                depthByRound[round] = depth;
            }
        }

        public static EconomyAnnotation Annotate(PathModel model, EconomyModel econ, ProgressionGraph graph)
        {
            var ann = new EconomyAnnotation();
            EconomyStaging(model, econ, out var optionStage, out var depthByRound);
            foreach (var kv in optionStage) ann.OptionEconomyStage[kv.Key] = kv.Value;
            foreach (var kv in depthByRound) ann.DepthByStage[kv.Key] = new DepthResult { ClearedLevel = kv.Value };

            var fullGranted = new HashSet<string>(model.SeedUnlocks) { econ.SeedUnlock };
            foreach (var o in model.Options)
                if (graph.StageOf.ContainsKey(o.Id))
                    foreach (var u in o.GrantedUnlocks) fullGranted.Add(u);

            foreach (var o in model.Options)
            {
                if (o.Kind != OptionKind.Reach) continue;
                int level = ReachLevelOf(econ, o);
                if (level <= 0) continue;
                ann.ReachLevel[o.Id] = level;
                ann.ReachGrind[o.Id] = EstimateGrind(econ, fullGranted, level - 1);
                if (optionStage.TryGetValue(o.Id, out var rs)) ann.ReachEconomyStage[o.Id] = rs;
            }

            foreach (var o in model.Options)
                if (o.Kind == OptionKind.Battle)
                {
                    double e = BattleEnemyOf(econ, o);
                    if (e > 0) ann.BattleEnemy[o.Id] = e;
                }
            return ann;
        }

        /// <summary>Developments whose removal drops the final saturated depth.</summary>
        public static Dictionary<string, int> EconomicChokepoints(PathModel model, EconomyModel econ, ProgressionGraph graph)
        {
            var allDevs = model.Options.Where(o => o.Kind == OptionKind.Development && graph.StageOf.ContainsKey(o.Id))
                                       .Select(o => o.Id).ToList();
            int baseline = SaturatedDepth(model, econ, allDevs).ClearedLevel;
            var result = new Dictionary<string, int>();
            foreach (var id in allDevs)
            {
                int depth = SaturatedDepth(model, econ, allDevs.Where(x => x != id)).ClearedLevel;
                if (depth < baseline) result[id] = baseline - depth;
            }
            return result;
        }

        // ==================== timing (grind effort) ====================

        public static GrindEstimate EstimateGrind(EconomyModel econ, ISet<string> granted, int targetLevel)
        {
            var acts = econ.Actions.Where(a => string.IsNullOrEmpty(a.RevealUnlock) || granted.Contains(a.RevealUnlock)).ToList();
            var bestYield = new Dictionary<string, double>();
            var bestPeriod = new Dictionary<string, double>();
            foreach (var a in acts)
            {
                double period = ProgressMax / (a.DefaultSpeed > 0 ? a.DefaultSpeed : 300);
                foreach (var kv in a.Changes)
                    if (kv.Value > 0 && (!bestYield.TryGetValue(kv.Key, out var y) || kv.Value > y))
                    { bestYield[kv.Key] = kv.Value; bestPeriod[kv.Key] = period; }
            }
            double delveSpeed = acts.Where(a => a.Id == "Delve").Select(a => a.DefaultSpeed).FirstOrDefault();
            if (delveSpeed <= 0) delveSpeed = 300;
            double delvePeriod = ProgressMax / delveSpeed;

            long delveActions = 0;
            var consumed = new Dictionary<string, double>();
            foreach (var lvl in econ.DelveTrack.OrderBy(l => l.Level))
            {
                if (lvl.Level > targetLevel) break;
                delveActions += lvl.DelveCount;
                foreach (var kv in lvl.Change)
                    if (kv.Value < 0)
                        consumed[kv.Key] = (consumed.TryGetValue(kv.Key, out var c) ? c : 0) + lvl.DelveCount * (-kv.Value);
            }

            var supply = new Dictionary<string, double>();
            double totalActions = 0, domA = 0, rawSeconds = delveActions * delvePeriod;
            string domR = null;
            foreach (var kv in consumed)
            {
                double actions = bestYield.TryGetValue(kv.Key, out var y) && y > 0 ? kv.Value / y : double.PositiveInfinity;
                supply[kv.Key] = actions;
                if (!double.IsPositiveInfinity(actions))
                {
                    totalActions += actions;
                    rawSeconds += actions * (bestPeriod.TryGetValue(kv.Key, out var p) ? p : delvePeriod);
                    if (actions > domA) { domA = actions; domR = kv.Key; }
                }
            }
            return new GrindEstimate
            {
                TargetLevel = targetLevel, DelveActions = delveActions, SupplyActions = supply,
                TotalSupplyActions = totalActions, DominantResource = domR, DominantActions = domA,
                RawSecondsSingleWorker = rawSeconds,
            };
        }

        public static double SecondsFor(GrindEstimate g, int parallelWorkers = 10, double timeScale = 1.0)
            => g.RawSecondsSingleWorker / Math.Max(1, parallelWorkers) / Math.Max(0.0001, timeScale);

        // ==================== warfare (army strength vs enemy strength) ====================

        /// <summary>Max army strength given granted unlocks: the flat bonus (Royal Army) plus — once any REVEALED
        /// unit's cost resources are all producible — unbounded strength (units can be ground indefinitely, so any
        /// battle threshold is eventually met). Before a buildable unit exists, only the flat bonus is available
        /// (which alone clears the first few battles). Reachability parallels the delve's unbounded-sink logic;
        /// the win-chance curve (own/enemy: 2× ⇒ 100%, parity ⇒ 50%, ≤½ ⇒ 0%) is deterministic-cleared at the
        /// guaranteed-win ratio, and how EXPENSIVE that army is belongs to the timing layer, not reachability.</summary>
        public static double MaxArmyStrength(EconomyModel econ, ISet<string> granted)
        {
            double flat = 0;
            if (!string.IsNullOrEmpty(econ.RoyalArmyUnlock) && granted.Contains(econ.RoyalArmyUnlock))
                flat += econ.RoyalArmyBonus;

            var producible = Producible(econ, granted);
            foreach (var u in econ.Units)
            {
                if (string.IsNullOrEmpty(u.RevealUnlock) || !granted.Contains(u.RevealUnlock)) continue;
                if (u.Cost.Keys.All(producible.Contains)) return double.PositiveInfinity; // a buildable unit ⇒ unbounded strength
            }
            return flat;
        }

        /// <summary>Army strength that deterministically clears a battle: ratio × enemy × (Warding reduction if granted).</summary>
        public static double BattleThreshold(EconomyModel econ, double enemyStrength, ISet<string> granted)
        {
            double mult = (!string.IsNullOrEmpty(econ.WardingUnlock) && granted.Contains(econ.WardingUnlock))
                ? econ.WardingEnemyMultiplier : 1.0;
            return econ.GuaranteedWinRatio * enemyStrength * mult;
        }

        /// <summary>The enemy strength of a battle option, from the victory unlock it grants.</summary>
        private static double BattleEnemyOf(EconomyModel econ, PathOption battle)
        {
            foreach (var u in battle.GrantedUnlocks)
                if (econ.EnemyStrengthByGrant.TryGetValue(u, out var e)) return e;
            return 0;
        }

        // ==================== helpers ====================

        private static int ReachLevelOf(EconomyModel econ, PathOption reach)
        {
            foreach (var u in reach.GrantedUnlocks)
                if (econ.ReachLevelByUnlock.TryGetValue(u, out var lv)) return lv;
            return 0;
        }

        private static void OwnedToEconomy(PathModel model, EconomyModel econ, IEnumerable<string> ownedOptionIds,
                                           out HashSet<string> granted, out Dictionary<string, double> spent)
        {
            var byId = model.Options.ToDictionary(o => o.Id);
            granted = new HashSet<string>(model.SeedUnlocks) { econ.SeedUnlock };
            spent = new Dictionary<string, double>();
            var ctx = new ExpressionContext();
            foreach (var id in ownedOptionIds)
            {
                if (!byId.TryGetValue(id, out var opt)) continue;
                foreach (var u in opt.GrantedUnlocks) granted.Add(u);
                foreach (var c in opt.Costs)
                {
                    double amt = c.Amount != null ? c.Amount.Evaluate(ctx) : 0.0;
                    if (amt < 0) spent[c.Resource] = (spent.TryGetValue(c.Resource, out var s) ? s : 0.0) + (-amt);
                }
            }
        }

        private static HashSet<string> Producible(EconomyModel econ, ISet<string> granted)
        {
            var acts = econ.Actions.Where(a => string.IsNullOrEmpty(a.RevealUnlock) || granted.Contains(a.RevealUnlock)).ToList();
            var prod = new HashSet<string>();
            var first = econ.DelveTrack.OrderBy(l => l.Level).FirstOrDefault();
            if (first != null)
                foreach (var kv in first.Change)
                    if (kv.Value > 0) prod.Add(kv.Key);

            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var a in acts)
                {
                    if (!a.Changes.Where(kv => kv.Value < 0).All(kv => prod.Contains(kv.Key))) continue;
                    foreach (var kv in a.Changes)
                        if (kv.Value > 0 && prod.Add(kv.Key)) changed = true;
                }
            }
            return prod;
        }

        private static DepthResult Wall(DelveLevel lvl, int delvesDone, string resource, bool isInput, string reason)
            => new DepthResult
            {
                ClearedLevel = lvl.Level - 1,
                PartialDelves = delvesDone,
                PartialOf = lvl.DelveCount,
                WallReason = reason,
                WallResource = resource,
                WallIsInput = isInput,
            };

        private static string Fmt(double d) => double.IsPositiveInfinity(d) ? "unlimited" : ((int)d).ToString();
    }
}
