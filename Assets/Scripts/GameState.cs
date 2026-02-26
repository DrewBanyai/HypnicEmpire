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


        public SerializableDictionary<string, ResourceValue> CurrentResourceCounts = new();
        public SerializableDictionary<string, ResourceValue> CurrentResourceMaximum = new();
        
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
            CopyGameUnlocks(gameState.GameUnlocks);
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


            //  Clear all resource current and maximum values and set them to their initial values
            ClearAllResourceValues();
            foreach (var entry in ResourceTypeSystem.ResourceData.ResourceTypes) CurrentResourceCounts[entry.Name] = entry.InitialValue;
            foreach (var entry in ResourceTypeSystem.ResourceData.ResourceTypes) CurrentResourceMaximum[entry.Name] = entry.GetMaximum();
        }

        public void CopyGameUnlocks(SerializableDictionary<string, bool> gameUnlocks)
        {
            foreach (var gu in gameUnlocks)
                GameUnlockSystem.SetUnlockValue(gu.Key, gu.Value);
        }

        public ResourceValue GetResourceAmount(string resourceType) { return CurrentResourceCounts.ContainsKey(resourceType) ? CurrentResourceCounts[resourceType] : 0; }
        public ResourceValue GetResourceMaxAmount(string resourceType) { return CurrentResourceMaximum.ContainsKey(resourceType) ? CurrentResourceMaximum[resourceType] : 0; }
        public void SetResourceAmount(string resourceType, ResourceValue resourceValue) { AddToResource(resourceType, resourceValue - GetResourceAmount(resourceType)); }

        public void AddToResources(List<ResourceAmountData> resourceChange)
        {
            foreach (var ra in resourceChange)
                AddToResource(ra.ResourceType, ra.ResourceValue);
        }
        
        public void AddToResource(string resourceType, ResourceValue resourceValue)
        {
            GameSubscriptionSystem.ProcessSubscriptionsToAddAndRemove(resourceType);

            ResourceValue previousAmount = CurrentResourceCounts[resourceType];
            CurrentResourceCounts[resourceType] = ResourceValue.Min(GetResourceMaxAmount(resourceType), ResourceValue.Max(0, CurrentResourceCounts[resourceType] + resourceValue));

            if (CurrentResourceCounts[resourceType] != previousAmount)
            {
                foreach (var callback in GameSubscriptionSystem.ResourceAmountSubscriptions[resourceType])
                    callback(resourceValue, CurrentResourceCounts[resourceType]);

                foreach (var callback in GameSubscriptionSystem.GenericResourceAmountSubscriptions)
                    callback(resourceType, resourceValue, CurrentResourceCounts[resourceType]);
            }
        }

        public void SetResourceMaximum(string resourceType, ResourceValue maxAmount) { AddToResourceMaximum(resourceType, maxAmount - GetResourceMaxAmount(resourceType)); }
        private void AddToResourceMaximum(string resourceType, ResourceValue maxAmount)
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
            string resourceUnlock = ResourceTypeSystem.GetUnlockFromResourceType(resourceType);
            if (resourceUnlock != default) GameUnlockSystem.SetUnlockValue(resourceUnlock, unlocked);
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