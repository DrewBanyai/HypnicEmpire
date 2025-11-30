using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HypnicEmpire
{
    [Serializable]
    public class GameState
    {
        //  How many levels we've reached at maximum (note, we can move backwards in the list)
        public SubscribableInt LevelReached = new SubscribableInt(0);

        //  Which level index we're currently sitting at
        public SubscribableInt LevelCurrent = new SubscribableInt(0);

        //  How many "delves" into the farthest level reached we have done
        public SubscribableInt LevelDelveCount = new SubscribableInt(0);

        public List<string> JournalEntries = new();

        //  The list of game unlocks that have occurred
        public SerializableDictionary<string, bool> GameUnlockList = new();
        public bool IsUnlocked(string unlock) { return GameUnlockList.ContainsKey(unlock) && GameUnlockList[unlock] == true; }

        public SerializableDictionary<string, int> CurrentResourceCounts = new();
        public SerializableDictionary<string, int> CurrentResourceMaximum = new();
        
        public int ClickCount = 0;

        public int MasterVolume = 50;
        public int SFXVolume = 50;
        public int MusicVolume = 50;
        public bool ActionSoundExcess = false;
        public bool Fullscreen = false;
        public bool WindowBorder = true;

        public void Initialize(GameStateScriptableObject gameState)
        {
            if (gameState == null) return;

            ClearAllResourceValues();
            CopyGameState(gameState.GameState);
        }

        public void ClearAllResourceValues()
        {
            CurrentResourceCounts = new();
            CurrentResourceMaximum = new();
            foreach (var rt in ResourceTypeSystem.ResourceTypes)
            {
                CurrentResourceCounts[rt] = 0;
                CurrentResourceMaximum[rt] = 999;
            }
        }

        public void CopyGameState(GameState other)
        {
            LevelReached.SetValue(other.LevelReached.Value);
            LevelCurrent.SetValue(other.LevelCurrent.Value);
            LevelDelveCount.SetValue(other.LevelDelveCount.Value);

            GameUnlockList.Clear();
            foreach (var item in other.GameUnlockList) GameUnlockList[item.Key] = item.Value;

            //  Clear all resource current and maximum values and set them to their initial values
            ClearAllResourceValues();
            foreach (var entry in ResourceTypeSystem.ResourceData.ResourceTypes) CurrentResourceCounts[entry.Name] = entry.InitialValue;
            foreach (var entry in ResourceTypeSystem.ResourceData.ResourceTypes) CurrentResourceMaximum[entry.Name] = entry.InitialMaximum;
        }

        public int GetResourceAmount(string resourceType) { return CurrentResourceCounts.ContainsKey(resourceType) ? CurrentResourceCounts[resourceType] : 0; }
        public int GetResourceMaxAmount(string resourceType) { return CurrentResourceMaximum.ContainsKey(resourceType) ? CurrentResourceMaximum[resourceType] : 0; }
        public void SetResourceAmount(string resourceType, int amount) { AddToResource(resourceType, amount - GetResourceAmount(resourceType)); }

        public void AddToResources(List<ResourceAmountData> resourceChange)
        {
            foreach (var ra in resourceChange)
                AddToResource(ra.ResourceType, ra.Amount);
        }
        
        public void AddToResource(string resourceType, int amount)
        {
            GameSubscriptionSystem.ProcessSubscriptionsToAddAndRemove(resourceType);

            var previousAmount = CurrentResourceCounts[resourceType];
            CurrentResourceCounts[resourceType] = Math.Min(GetResourceMaxAmount(resourceType), Math.Max(0, CurrentResourceCounts[resourceType] + amount));

            if (CurrentResourceCounts[resourceType] != previousAmount)
            {
                foreach (var callback in GameSubscriptionSystem.ResourceAmountSubscriptions[resourceType])
                    callback(amount, CurrentResourceCounts[resourceType]);

                foreach (var callback in GameSubscriptionSystem.GenericResourceAmountSubscriptions)
                    callback(resourceType, amount, CurrentResourceCounts[resourceType]);
            }
        }

        public void SetResourceMaximum(string resourceType, int maxAmount) { AddToResourceMaximum(resourceType, maxAmount - GetResourceMaxAmount(resourceType)); }
        private void AddToResourceMaximum(string resourceType, int maxAmount)
        {
            GameSubscriptionSystem.ProcessSubscriptionsToAddAndRemove(resourceType);

            CurrentResourceMaximum[resourceType] += maxAmount;

            foreach (var callback in GameSubscriptionSystem.ResourceMaximumSubscriptions[resourceType])
                callback(maxAmount, CurrentResourceMaximum[resourceType]);

            foreach (var callback in GameSubscriptionSystem.GenericResourceMaximumSubscriptions)
                callback(maxAmount, CurrentResourceMaximum[resourceType]);
        }
        
        public void SetResourceUnlocked(string resourceType, bool unlocked)
        {
            string? resourceUnlock = ResourceTypeSystem.GetUnlockFromResourceType(resourceType);
            if (resourceUnlock != null) SetUnlockValue(resourceUnlock, unlocked);
        }

        public void SetUnlockValue(string unlock, bool unlocked)
        {
            GameUnlockSystem.SetUnlockValue(unlock, unlocked);
            GameUnlockList[unlock] = unlocked;
        }

        public bool GetUnlockValue(string unlock)
        {
            return GameUnlockList.ContainsKey(unlock) ? GameUnlockList[unlock] : false;
        }

        public void Click()
        {
            ClickCount += 1;
            var clickValue = AlterableValueSystem.GetAlterableValue("Clicks");
            clickValue?.SetValue(ClickCount);
        }
        
        public void ToggleActionSoundExcess()
        {
            ActionSoundExcess = !ActionSoundExcess;
        }
    }
}