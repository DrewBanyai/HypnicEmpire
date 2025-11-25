using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HypnicEmpire
{
    public class AlterableValueUnlockCombo
    {
        public int Value;
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
        public string UnlockTarget;
        public List<AlterableValueUnlockCombo> ValueUnlocks;
        public List<AlterableValueRemapValue> RemappingValues;

        public int? GetCurrentValueRemap()
        {
            return RemappingValues.Find(rv => rv.Value == CurrentValue)?.Value;
        }

        public string? GetCurrentValueRemapNote()
        {
            return RemappingValues.Find(rv => rv.Value == CurrentValue)?.Note;
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
            switch (UnlockTarget)
            {
                case "==":
                    {
                        var vu = ValueUnlocks.Find(vu => CurrentValue == vu.Value);
                        if (vu != null)
                            GameUnlockSystem.SetUnlockValue(vu.Unlock, true);
                    }
                    break;
                case ">=":
                    {
                        var vu = ValueUnlocks.Find(vu => CurrentValue >= vu.Value );
                        if (vu != null)
                            GameUnlockSystem.SetUnlockValue(vu.Unlock, true);
                    }
                    break;
                case "<=":
                    {
                        var vu = ValueUnlocks.Find(vu => CurrentValue <= vu.Value);
                        if (vu != null)
                            GameUnlockSystem.SetUnlockValue(vu.Unlock, true);
                    }
                    break;
            }
        }
    }
}