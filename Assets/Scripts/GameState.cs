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

        public PlayerActionType CurrentPlayerAction = PlayerActionType.Inactive;
        public List<string> JournalEntries = new();

        //  The list of game unlocks that have occurred
        public SerializableDictionary<string, bool> GameUnlockList = new();
        public bool IsUnlocked(string unlock) { return GameUnlockList.ContainsKey(unlock) && GameUnlockList[unlock] == true; }

        public SerializableDictionary<ResourceType, int> CurrentResourceCounts = new();
        public SerializableDictionary<ResourceType, int> CurrentResourceMaximum = new();

        public Dictionary<PlayerActionType, List<ResourceAmount>> PlayerActionTypeResourceChange = new();
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
            GameController.GameSubscriptions.ClearAllSubscriptions();
            CopyGameState(gameState.GameState);
        }

        public void ClearAllResourceValues()
        {
            CurrentResourceCounts = new();
            CurrentResourceMaximum = new();
            foreach (var rt in Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>())
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

            ClearAllResourceValues();
            foreach (var entry in other.CurrentResourceCounts) CurrentResourceCounts[entry.Key] = entry.Value;
            foreach (var entry in other.CurrentResourceMaximum) CurrentResourceMaximum[entry.Key] = entry.Value;
        }

        public void SetPlayerActionResourceChange(PlayerActionType playerActionType, List<ResourceAmount> resourceChange)
        {
            PlayerActionTypeResourceChange[playerActionType] = new();
            foreach (var item in resourceChange) PlayerActionTypeResourceChange[playerActionType].Add(item);
        }

        public int GetResourceAmount(ResourceType resourceType) { return CurrentResourceCounts.ContainsKey(resourceType) ? CurrentResourceCounts[resourceType] : 0; }
        public int GetResourceMaxAmount(ResourceType resourceType) { return CurrentResourceMaximum.ContainsKey(resourceType) ? CurrentResourceMaximum[resourceType] : 0; }
        public void SetResourceAmount(ResourceType resourceType, int amount) { AddToResource(resourceType, amount - GetResourceAmount(resourceType)); }

        public void AddToResources(List<ResourceAmount> resourceChange)
        {
            foreach (var ra in resourceChange)
                AddToResource(ra.ResourceType, ra.Amount);
        }
        
        public void AddToResource(ResourceType resourceType, int amount)
        {
            GameController.GameSubscriptions.ProcessSubscriptionsToAddAndRemove(resourceType);

            CurrentResourceCounts[resourceType] = Math.Min(GetResourceMaxAmount(resourceType), Math.Max(0, CurrentResourceCounts[resourceType] + amount));

            foreach (var callback in GameController.GameSubscriptions.ResourceAmountSubscriptions[resourceType])
                callback(amount, CurrentResourceCounts[resourceType]);

            foreach (var callback in GameController.GameSubscriptions.GenericResourceAmountSubscriptions)
                callback(resourceType, amount, CurrentResourceCounts[resourceType]);
        }

        public void SetResourceMaximum(ResourceType resourceType, int maxAmount) { AddToResourceMaximum(resourceType, maxAmount - GetResourceMaxAmount(resourceType)); }
        private void AddToResourceMaximum(ResourceType resourceType, int maxAmount)
        {
            GameController.GameSubscriptions.ProcessSubscriptionsToAddAndRemove(resourceType);

            CurrentResourceMaximum[resourceType] += maxAmount;

            foreach (var callback in GameController.GameSubscriptions.ResourceMaximumSubscriptions[resourceType])
                callback(maxAmount, CurrentResourceMaximum[resourceType]);

            foreach (var callback in GameController.GameSubscriptions.GenericResourceMaximumSubscriptions)
                callback(maxAmount, CurrentResourceMaximum[resourceType]);
        }
        
        public void SetResourceUnlocked(ResourceType resourceType, bool unlocked)
        {
            string? resourceUnlock = ResourceGameUnlockUtility.GetUnlockFromResourceType(resourceType);
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