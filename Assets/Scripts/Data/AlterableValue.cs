using System;
using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    public class AlterableValueUnlockCombo
    {
        public int Value;
        public string Operator;
        public string Unlock;
    }

    public class AlterableValueRemapValue
    {
        public int Value;
        public int Remap;
        public string Note;
    }

    // One thing an application reaches: what kind of thing it is, and which one of them.
    public readonly struct ModifierTarget
    {
        public readonly string Kind;   // Resource | ResourceGroup | Section | Action | Value
        public readonly string Name;

        public ModifierTarget(string kind, string name) { Kind = kind; Name = name; }

        // "<Kind>:<Name>". A target with no separator is all kind and no name, which matches nothing.
        public static ModifierTarget Parse(string target)
        {
            int separator = target?.IndexOf(':') ?? -1;
            return separator >= 0
                ? new ModifierTarget(target.Substring(0, separator), target.Substring(separator + 1))
                : new ModifierTarget(target ?? "", "");
        }

        public override string ToString() => Kind + ":" + Name;
    }

    // An Application declares what a "Modifier" AlterableValue actually modifies.
    // Op + the thing(s) it reaches, authored either as one Target ("Resource:Money",
    // "ResourceGroup:Basic", "Section:Delving", "Action:*", "Action:Delve", "Value:Unrest") or as a
    // Targets list where one application reaches several — "ResourceGroup:Basic" together with
    // "Resource:Food", say, for a resource that is stored with the basics but grouped elsewhere in
    // the UI. Optional Resource narrows a GainPct to a single resource; Reduces flips an AddValue's
    // sign (e.g. lowering Unrest). See ModifierValueSystem for how these are consumed.
    public class ModifierApplication
    {
        public string Op;              // AddMax | MultMax | SpeedPct | GainPct | JobCap | AddValue | Counter
        public string Target;          // "<Kind>:<Name>"  Kind in {Resource,ResourceGroup,Section,Action,Value}
        public List<string> Targets;   // the same, for an application that reaches more than one thing
        public string Resource;        // optional extra filter for GainPct (e.g. FarmingFoodGainPercent -> only Food)
        public bool Reduces;           // AddValue only: subtract instead of add

        private List<ModifierTarget> _parsedTargets;

        // Everything this application reaches, however it was authored, so consumers never have to care
        // which of the two forms was used. Parsed once: the data loads once but is read on every
        // recompute and every storage query.
        public IReadOnlyList<ModifierTarget> ParsedTargets => _parsedTargets ??= ParseTargets();

        private List<ModifierTarget> ParseTargets()
        {
            var parsed = new List<ModifierTarget>();

            if (!string.IsNullOrEmpty(Target)) parsed.Add(ModifierTarget.Parse(Target));

            if (Targets != null)
                foreach (var target in Targets)
                    if (!string.IsNullOrEmpty(target)) parsed.Add(ModifierTarget.Parse(target));

            return parsed;
        }
    }

    public class AlterableValue
    {
        public string Name;
        public string Kind;      // null/"" for plain values; "Modifier" for computed modifier values
        public int CurrentValue;
        public int MinimumValue;
        public int MaximumValue;
        public List<AlterableValueUnlockCombo> ValueUnlocks;
        public List<AlterableValueRemapValue> RemappingValues;
        public List<ModifierApplication> Applications;   // only meaningful when Kind == "Modifier"

        public bool IsModifier => Kind == "Modifier";

        public int Clamp(int value) => Math.Max(Math.Min(value, MaximumValue), MinimumValue);

        public int GetCurrentValueRemap()
        {
            return RemappingValues.Any(rv => rv.Value == CurrentValue) ? RemappingValues.Find(rv => rv.Value == CurrentValue).Remap : -1;
        }

        public string GetCurrentValueRemapNote()
        {
            return RemappingValues.Any(rv => rv.Value == CurrentValue) ? RemappingValues.Find(rv => rv.Value == CurrentValue).Note : "UNKNOWN";
        }

        public string GetValueUnlock()
        {
            return ValueUnlocks.Find(vu => vu.Value == CurrentValue)?.Unlock;
        }


        
        public void SetValue(int value)
        {
            var newValue = Math.Max(Math.Min(value, MaximumValue), MinimumValue);
            if (CurrentValue == newValue) return;
            CurrentValue = newValue;
            BroadcastUnlock();
        }

        private void BroadcastUnlock()
        {
            foreach (var vu in ValueUnlocks)
            {
                switch (vu.Operator)
                {
                    case "==":
                        if (CurrentValue == vu.Value)
                            GameUnlockSystem.SetUnlockValue(vu.Unlock, true);
                        break;
                    case "<=":
                        if (CurrentValue <= vu.Value)
                            GameUnlockSystem.SetUnlockValue(vu.Unlock, true);
                        break;
                    case ">=":
                        if (CurrentValue >= vu.Value)
                            GameUnlockSystem.SetUnlockValue(vu.Unlock, true);
                        break;
                }
            }
        }
    }
}