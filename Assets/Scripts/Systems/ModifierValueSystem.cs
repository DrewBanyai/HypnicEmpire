using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HypnicEmpire
{
    // A contribution a completed project (or any external source) makes to an AlterableValue.
    // Kept independent of the game's ProjectData type so this system has no hard dependency on it.
    public struct ModifierContribution
    {
        public string ValueName;
        public int Amount;
        public ModifierContribution(string valueName, int amount) { ValueName = valueName; Amount = amount; }
    }

    // ---------------------------------------------------------------------------
    // ModifierValueSystem
    //
    // Makes the "modifier" AlterableValues real (design doc: Building Effects & Modifier
    // Values). Buildings and completed projects CONTRIBUTE to AlterableValues via their
    // AlteredValues; those values, when Kind=="Modifier", carry Applications that say what
    // they modify (resource maxima, action speed/gain, job caps, or another tracked value).
    //
    // Flow:
    //   contribution:  value(M) = base(M)
    //                           + Σ_buildings count(b) × Σ(av.Amount for av in b.AlteredValues,
    //                                                       av.ValueName==M, trigger active)
    //                           + Σ_projects  completions(p) × Σ(c.Amount for c in p, c.ValueName==M)
    //   application:   AddValue ops fold one modifier's value into another AlterableValue;
    //                  AddMax / SpeedPct / GainPct / JobCap are pulled on demand by the consumers.
    //
    // This system OWNS the interim building-count and project-completion state (the game has
    // no purchase/completion runtime yet). When that is added, call SetBuildingCount / AddBuilding
    // / CompleteProject from it — everything downstream recomputes automatically.
    // ---------------------------------------------------------------------------
    public static class ModifierValueSystem
    {
        public const int BaseJobCap = 10;   // prior hard-coded worker cap per task (TaskActionSystem)

        // Raised after a recompute settles, for state derived from building/project counts (land usage).
        public static event Action OnValuesRecomputed;

        // Job-section vocabulary is NOT identical to an action's ActionSection (design §3.3):
        // Delve's ActionSection is "Unaffiliated" but it is a "Delving" job, and "Farming" is a
        // crop-farming subset of the Agricultural section. Bind those exceptions here; anything
        // not listed falls back to the action's own ActionSection.
        //   - Delve   -> Delving  (the only delving job)
        //   - Farming -> Farming  (crop farming; Forage/Hunting/Fishing/Cultivate stay Agricultural)
        // For worker caps this partitions the section: the Farming action is capped by
        // MaxFarmingJobs, the rest of Agricultural by MaxAgriculturalJobs. For speed/gain the
        // action still also matches its ActionSection, so AllAgriculturalGainPercent applies to
        // Farming too, while FarmingFoodGainPercent applies to Farming's Food only.
        // Add more actions here if others should count as Farming (or Delving) jobs.
        public static readonly Dictionary<string, string> JobSectionByAction = new()
        {
            { "Delve", "Delving" },
            { "Farming", "Farming" },
        };

        private static bool _initialized;
        private static bool _recomputing;

        private static readonly Dictionary<string, int> _bases = new();            // AV name -> authored base value
        private static readonly Dictionary<string, int> _buildingCounts = new();   // building name -> count built
        private static readonly Dictionary<string, int> _projectCounts = new();    // project name -> times completed
        private static readonly Dictionary<string, List<ModifierContribution>> _projectContribs = new();
        private static readonly HashSet<string> _drivenNames = new();              // AVs this system writes to
        private static readonly List<(string name, List<ModifierApplication> apps)> _modifiers = new();

        // -- lifecycle ----------------------------------------------------------

        public static void Initialize()
        {
            _bases.Clear(); _projectCounts.Clear();
            _projectContribs.Clear(); _drivenNames.Clear(); _modifiers.Clear();

            // Snapshot authored bases BEFORE we ever write, so recompute is idempotent.
            foreach (var kv in AlterableValueSystem.ValueMap)
                _bases[kv.Key] = kv.Value.CurrentValue;

            // Collect modifiers + the full set of AlterableValues we may drive.
            foreach (var kv in AlterableValueSystem.ValueMap)
            {
                var av = kv.Value;
                if (av.IsModifier && av.Applications != null && av.Applications.Count > 0)
                {
                    _modifiers.Add((av.Name, av.Applications));
                    foreach (var app in av.Applications)
                        if (app.Op == "AddValue" && app.TargetKind == "Value")
                            _drivenNames.Add(app.TargetName);
                }
            }

            SeedBuildingCounts();

            // Collect the values buildings drive and register their trigger unlocks.
            var triggers = new HashSet<string>();
            var buildings = BuildingDataSystem.Data?.BuildingTypes;
            if (buildings != null)
                foreach (var b in buildings)
                {
                    if (b.AlteredValues == null) continue;
                    foreach (var av in b.AlteredValues)
                    {
                        _drivenNames.Add(av.ValueName);
                        if (!string.IsNullOrEmpty(av.Trigger)) triggers.Add(av.Trigger);
                    }
                }

            // Recompute whenever a building's conditional trigger toggles.
            foreach (var t in triggers)
                GameUnlockSystem.AddGameUnlockAction(t, false, _ => Recompute());

            _initialized = true;
            Recompute();
            Debug.Log($"ModifierValueSystem initialized: {_modifiers.Count} modifier(s), driving {_drivenNames.Count} value(s).");
        }

        // What the player has built and completed is session state, not authored data, so a hard reset puts
        // it back to the counts the game starts on. The authored bases, the collected modifiers and the
        // trigger registrations live for the whole session and are deliberately left alone: rebuilding them
        // would stack a second set of unlock actions and re-snapshot bases this system has already written to.
        public static void Reset()
        {
            if (!_initialized) return;

            _projectCounts.Clear();
            SeedBuildingCounts();
            Recompute();
        }

        // Building counts always come from the authored StartingCount, whether at startup or after a reset,
        // so anything derived from them (land usage above all) can be recalculated rather than tracked.
        private static void SeedBuildingCounts()
        {
            _buildingCounts.Clear();

            var buildings = BuildingDataSystem.Data?.BuildingTypes;
            if (buildings == null) return;

            foreach (var b in buildings)
                _buildingCounts[b.Name] = b.StartingCount?.Amount ?? 0;
        }

        // -- mutation API (call these from purchase / project-completion code) ---

        public static void SetBuildingCount(string buildingName, int count)
        {
            _buildingCounts[buildingName] = Math.Max(0, count);
            Recompute();
        }

        public static void AddBuilding(string buildingName, int delta = 1)
        {
            _buildingCounts.TryGetValue(buildingName, out int c);
            _buildingCounts[buildingName] = Math.Max(0, c + delta);
            Recompute();
        }

        public static int GetBuildingCount(string buildingName)
            => _buildingCounts.TryGetValue(buildingName, out int c) ? c : 0;

        public static SerializableDictionary<string, int> GetAllBuildingCounts()
            => new SerializableDictionary<string, int>(_buildingCounts);

        // Register a project's contributions once (e.g. at load), then CompleteProject on completion.
        public static void RegisterProject(string projectName, IEnumerable<ModifierContribution> contributions)
        {
            _projectContribs[projectName] = contributions?.ToList() ?? new List<ModifierContribution>();
            foreach (var c in _projectContribs[projectName]) _drivenNames.Add(c.ValueName);
        }

        public static void CompleteProject(string projectName)
        {
            _projectCounts.TryGetValue(projectName, out int c);
            _projectCounts[projectName] = c + 1;
            Recompute();
        }

        // -- recompute ----------------------------------------------------------

        public static void Recompute()
        {
            if (!_initialized || _recomputing) return;
            _recomputing = true;
            try
            {
                // 1. Direct accumulation: base + Σ building/project contributions.
                var direct = new Dictionary<string, int>();
                foreach (var name in _drivenNames)
                    direct[name] = _bases.TryGetValue(name, out int b) ? b : 0;

                var buildings = BuildingDataSystem.Data?.BuildingTypes;
                if (buildings != null)
                    foreach (var bld in buildings)
                    {
                        int count = GetBuildingCount(bld.Name);
                        if (count == 0 || bld.AlteredValues == null) continue;
                        foreach (var av in bld.AlteredValues)
                        {
                            if (!string.IsNullOrEmpty(av.Trigger) && !GameUnlockSystem.IsUnlocked(av.Trigger)) continue;
                            if (!direct.ContainsKey(av.ValueName)) direct[av.ValueName] = _bases.TryGetValue(av.ValueName, out int bb) ? bb : 0;
                            direct[av.ValueName] += count * av.Amount;
                        }
                    }

                foreach (var kv in _projectContribs)
                {
                    int times = _projectCounts.TryGetValue(kv.Key, out int t) ? t : 0;
                    if (times == 0) continue;
                    foreach (var c in kv.Value)
                    {
                        if (!direct.ContainsKey(c.ValueName)) direct[c.ValueName] = _bases.TryGetValue(c.ValueName, out int bb) ? bb : 0;
                        direct[c.ValueName] += times * c.Amount;
                    }
                }

                // 2. Effective modifier value (clamped) for use by AddValue applications.
                int Effective(string name)
                {
                    int raw = direct.TryGetValue(name, out int d) ? d : (_bases.TryGetValue(name, out int b) ? b : 0);
                    return AlterableValueSystem.ValueMap.TryGetValue(name, out var av) ? av.Clamp(raw) : raw;
                }

                var adjust = new Dictionary<string, int>();
                foreach (var (modName, apps) in _modifiers)
                    foreach (var app in apps)
                        if (app.Op == "AddValue" && app.TargetKind == "Value")
                        {
                            int delta = Effective(modName) * (app.Reduces ? -1 : 1);
                            adjust.TryGetValue(app.TargetName, out int cur);
                            adjust[app.TargetName] = cur + delta;
                        }

                // 3. Write final values (SetValue clamps + fires ValueUnlocks thresholds).
                foreach (var name in _drivenNames)
                {
                    if (!AlterableValueSystem.ValueMap.TryGetValue(name, out var av)) continue;
                    int final = (direct.TryGetValue(name, out int d) ? d : (_bases.TryGetValue(name, out int b) ? b : 0))
                              + (adjust.TryGetValue(name, out int a) ? a : 0);
                    av.SetValue(final);
                }

                // 4. Storage modifiers changed resource maxima — push them into the game state.
                ResourceTypeSystem.RefreshAllResourceMaxima();
            }
            finally { _recomputing = false; }

            // Announced outside the guard so a listener that ends up unlocking something is still able
            // to drive a further recompute.
            OnValuesRecomputed?.Invoke();
        }

        // -- consumer queries (read the accumulated modifier values) ------------

        public static int Value(string modifierName) => AlterableValueSystem.GetAlterableValueCurrentVal(modifierName);

        // Additive storage bonus for a resource (its own AddMax modifiers + its group's).
        // Folded INTO GetMaximum before the unlock multipliers (design §3.1).
        public static int GetResourceMaxAdditive(string resourceName, string resourceGroup)
        {
            int sum = 0;
            foreach (var (name, apps) in _modifiers)
                foreach (var app in apps)
                    if (app.Op == "AddMax" &&
                        ((app.TargetKind == "Resource" && app.TargetName == resourceName) ||
                         (app.TargetKind == "ResourceGroup" && app.TargetName == resourceGroup)))
                        sum += Value(name);
            return sum;
        }

        // 1 + Σ SpeedPct% for a task (matched by action name, "*", or section).
        public static double GetActionSpeedMultiplier(string actionName, string actionSection)
        {
            int pct = 0;
            foreach (var (name, apps) in _modifiers)
                foreach (var app in apps)
                    if (app.Op == "SpeedPct" && MatchesActionOrSection(app, actionName, actionSection))
                        pct += Value(name);
            return 1.0 + pct / 100.0;
        }

        // 1 + Σ GainPct% for a task's reward of a specific resource.
        public static double GetActionGainMultiplier(string actionName, string actionSection, string resourceName)
        {
            int pct = 0;
            foreach (var (name, apps) in _modifiers)
                foreach (var app in apps)
                    if (app.Op == "GainPct" && MatchesGain(app, actionName, actionSection, resourceName))
                        pct += Value(name);
            return 1.0 + pct / 100.0;
        }

        // Scale an action's resource change by its GainPct modifiers. Only positive (gained)
        // amounts are scaled; costs/losses pass through unchanged. Returns the original list if
        // nothing changed (no allocation on the common no-modifier path).
        public static List<ResourceAmountData> ApplyGain(string actionName, string actionSection, List<ResourceAmountData> changes)
        {
            if (changes == null || changes.Count == 0) return changes;
            List<ResourceAmountData> result = null;
            for (int i = 0; i < changes.Count; i++)
            {
                var rc = changes[i];
                double mult = rc.ResourceValue > 0 ? GetActionGainMultiplier(actionName, actionSection, rc.ResourceType) : 1.0;
                if (mult != 1.0)
                {
                    if (result == null) result = new List<ResourceAmountData>(changes);
                    result[i] = new ResourceAmountData(rc.ResourceType, rc.ResourceValue * mult);
                }
            }
            return result ?? changes;
        }

        // Max parallel workers for a job section = base cap + Σ JobCap modifiers for that section.
        public static int GetJobCap(string jobSection)
        {
            int cap = BaseJobCap;
            foreach (var (name, apps) in _modifiers)
                foreach (var app in apps)
                    if (app.Op == "JobCap" && app.TargetKind == "Section" && app.TargetName == jobSection)
                        cap += Value(name);
            return cap;
        }

        // Job-section for an action: explicit binding first, else its ActionSection.
        public static string SectionForAction(string actionName, string actionSection)
            => JobSectionByAction.TryGetValue(actionName, out var s) ? s : actionSection;

        // -- matching helpers ---------------------------------------------------

        private static bool SectionMatches(string section, string actionName, string actionSection)
        {
            if (section == actionSection) return true;
            return JobSectionByAction.TryGetValue(actionName, out var js) && js == section;
        }

        private static bool MatchesActionOrSection(ModifierApplication app, string actionName, string actionSection)
        {
            switch (app.TargetKind)
            {
                case "Action":  return app.TargetName == "*" || app.TargetName == actionName;
                case "Section": return SectionMatches(app.TargetName, actionName, actionSection);
                default: return false;
            }
        }

        private static bool MatchesGain(ModifierApplication app, string actionName, string actionSection, string resourceName)
        {
            bool nameMatch;
            switch (app.TargetKind)
            {
                case "Action":   nameMatch = app.TargetName == "*" || app.TargetName == actionName; break;
                case "Section":  nameMatch = SectionMatches(app.TargetName, actionName, actionSection); break;
                case "Resource": nameMatch = app.TargetName == resourceName; break;
                default: return false;
            }
            bool resourceMatch = string.IsNullOrEmpty(app.Resource) || app.Resource == resourceName;
            return nameMatch && resourceMatch;
        }

        // -- diagnostics --------------------------------------------------------

        public static string DebugReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"ModifierValueSystem — {_modifiers.Count} modifiers");
            foreach (var (name, apps) in _modifiers)
            {
                string a = string.Join(", ", apps.Select(x => x.Op + "->" + x.Target + (x.Reduces ? "(reduces)" : "")));
                sb.AppendLine($"  {name,-28} = {Value(name),8}   [{a}]");
            }
            return sb.ToString();
        }
    }
}
