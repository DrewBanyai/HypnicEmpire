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

    public class AlterableValue
    {
        public string Name;
        public int CurrentValue;
        public int MinimumValue;
        public int MaximumValue;
        public List<AlterableValueUnlockCombo> ValueUnlocks;
        public List<AlterableValueRemapValue> RemappingValues;

        public int GetCurrentValueRemap()
        {
            return RemappingValues.Any(rv => rv.Value == CurrentValue) ? RemappingValues.Find(rv => rv.Value == CurrentValue).Value : -1;
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