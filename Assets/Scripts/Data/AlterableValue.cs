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

    // An Application declares what a "Modifier" AlterableValue actually modifies.
    // Op + Target (e.g. "Resource:Money", "ResourceGroup:Basic", "Section:Delving",
    // "Action:*", "Action:Delve", "Value:Unrest"). Optional Resource narrows a GainPct
    // to a single resource; Reduces flips an AddValue's sign (e.g. lowering Unrest).
    // See ModifierValueSystem for how these are consumed.
    public class ModifierApplication
    {
        public string Op;        // AddMax | MultMax | SpeedPct | GainPct | JobCap | AddValue | Counter
        public string Target;    // "<Kind>:<Name>"  Kind in {Resource,ResourceGroup,Section,Action,Value}
        public string Resource;  // optional extra filter for GainPct (e.g. FarmingFoodGainPercent -> only Food)
        public bool Reduces;     // AddValue only: subtract instead of add

        // Parsed Target ---------------------------------------------------------
        public string TargetKind
        {
            get { int i = Target?.IndexOf(':') ?? -1; return i >= 0 ? Target.Substring(0, i) : (Target ?? ""); }
        }
        public string TargetName
        {
            get { int i = Target?.IndexOf(':') ?? -1; return i >= 0 ? Target.Substring(i + 1) : ""; }
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