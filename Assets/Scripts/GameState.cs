using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HypnicEmpire
{
    [Serializable]
    public class GameState
    {
        //  How many levels we've reached at maximum (note, we can move backwards in the list)
        public int LevelReached = 0;

        //  Which level index we're currently sitting at
        public int LevelCurrent = 0;

        //  How many "delves" into the farthest level reached we have done
        public int LevelDelveCount = 0;

        //  The list of game unlocks that have occurred
        public List<GameUnlock> GameUnlockList = new();

        public SerializableDictionary<ResourceType, int> CurrentResourceCounts = new();
        public SerializableDictionary<ResourceType, int> CurrentResourceMaximum = new();
        public SerializableDictionary<ResourceType, bool> CurrentResourceUnlocked = new();
        private List<Action<int, int>> GenericResourceAmountSubscriptions = new();
        private List<Action<int, int>> GenericResourceMaximumSubscriptions = new();
        private SerializableDictionary<ResourceType, List<Action<int, int>>> ResourceAmountSubscriptions = new();
        private SerializableDictionary<ResourceType, List<Action<int, int>>> ResourceMaximumSubscriptions = new();

        // Subscriptions to add or remove (before next event response)
        private SerializableDictionary<ResourceType, List<Action<int, int>>> ResourceAmountSubscriptionsToAdd = new();
        private SerializableDictionary<ResourceType, List<Action<int, int>>> ResourceMaximumSubscriptionsToAdd = new();
        private SerializableDictionary<ResourceType, List<Action<int, int>>> ResourceAmountSubscriptionsToRemove = new();
        private SerializableDictionary<ResourceType, List<Action<int, int>>> ResourceMaximumSubscriptionsToRemove = new();

        public int MasterVolume = 50;
        public int SFXVolume = 50;
        public int MusicVolume = 50;
        public bool ActionSoundExcess = false;
        public bool Fullscreen = false;
        public bool WindowBorder = true;

        public void Initialize(GameStateScriptableObject gameState)
        {
            if (gameState == null) return;
            
            ClearAllSubscriptions();
            CopyGameState(gameState.GameState);
        }

        private void ClearAllSubscriptions()
        {
            GenericResourceAmountSubscriptions = new List<Action<int, int>>();
            GenericResourceMaximumSubscriptions = new List<Action<int, int>>();
            ResourceAmountSubscriptions = new SerializableDictionary<ResourceType, List<Action<int, int>>>();
            ResourceMaximumSubscriptions = new SerializableDictionary<ResourceType, List<Action<int, int>>>();
            ResourceAmountSubscriptionsToAdd = new SerializableDictionary<ResourceType, List<Action<int, int>>>();
            ResourceMaximumSubscriptionsToAdd = new SerializableDictionary<ResourceType, List<Action<int, int>>>();
            ResourceAmountSubscriptionsToRemove = new SerializableDictionary<ResourceType, List<Action<int, int>>>();
            ResourceMaximumSubscriptionsToRemove = new SerializableDictionary<ResourceType, List<Action<int, int>>>();
        }

        public void CopyGameState(GameState other)
        {
            LevelReached = other.LevelReached;
            LevelCurrent = other.LevelCurrent;
            LevelDelveCount = other.LevelDelveCount;

            GameUnlockList.Clear();
            foreach (var item in other.GameUnlockList)
                GameUnlockList.Add(item);

            CurrentResourceCounts = new SerializableDictionary<ResourceType, int>();
            foreach (var entry in other.CurrentResourceCounts)
                SetResourceAmount(entry.Key, entry.Value);

            CurrentResourceMaximum = new SerializableDictionary<ResourceType, int>();
            foreach (var entry in other.CurrentResourceMaximum)
                SetResourceMaximum(entry.Key, entry.Value);

            CurrentResourceUnlocked = new SerializableDictionary<ResourceType, bool>();
            foreach (var entry in other.CurrentResourceUnlocked)
                SetResourceUnlocked(entry.Key, entry.Value);
        }

        public int GetResourceAmount(ResourceType resourceType)
        {
            if (!CurrentResourceCounts.ContainsKey(resourceType))
                CurrentResourceCounts[resourceType] = 0;

            return CurrentResourceCounts[resourceType];
        }

        public int GetResourceMaxAmount(ResourceType resourceType)
        {
            if (!CurrentResourceMaximum.ContainsKey(resourceType))
                CurrentResourceMaximum[resourceType] = 0;

            return CurrentResourceMaximum[resourceType];
        }

        public void SubscribeToGenericResourceAmountChange(Action<int, int> callback)
        {
            GenericResourceAmountSubscriptions.Add(callback);
        }

        public void SubscribeToGenericResourceMaximumChange(Action<int, int> callback)
        {
            GenericResourceMaximumSubscriptions.Add(callback);
        }

        public void SubscribeToResourceAmount(ResourceType resourceType, Action<int, int> callback)
        {
            if (!ResourceAmountSubscriptions.ContainsKey(resourceType))
                ResourceAmountSubscriptions[resourceType] = new List<Action<int, int>>();
            if (!ResourceAmountSubscriptionsToAdd.ContainsKey(resourceType))
                ResourceAmountSubscriptionsToAdd[resourceType] = new List<Action<int, int>>();

            ResourceAmountSubscriptionsToAdd[resourceType].Add(callback);
        }

        public void UnsubscribeToResourceAmount(ResourceType resourceType, Action<int, int> callback)
        {
            if (!ResourceAmountSubscriptions.ContainsKey(resourceType))
                ResourceAmountSubscriptions[resourceType] = new List<Action<int, int>>();
            if (!ResourceAmountSubscriptionsToRemove.ContainsKey(resourceType))
                ResourceAmountSubscriptionsToRemove[resourceType] = new List<Action<int, int>>();

            if (ResourceAmountSubscriptions[resourceType].Contains(callback))
                ResourceAmountSubscriptionsToRemove[resourceType].Add(callback);
        }

        public void SubscribeToResourceMaximum(ResourceType resourceType, Action<int, int> callback)
        {
            if (!ResourceMaximumSubscriptions.ContainsKey(resourceType))
                ResourceMaximumSubscriptions[resourceType] = new List<Action<int, int>>();
            if (!ResourceMaximumSubscriptionsToAdd.ContainsKey(resourceType))
                ResourceMaximumSubscriptionsToAdd[resourceType] = new List<Action<int, int>>();

            ResourceMaximumSubscriptionsToAdd[resourceType].Add(callback);
        }

        public void UnsubscribeToResourceMaximum(ResourceType resourceType, Action<int, int> callback)
        {
            if (!ResourceMaximumSubscriptions.ContainsKey(resourceType))
                ResourceMaximumSubscriptions[resourceType] = new List<Action<int, int>>();
            if (!ResourceMaximumSubscriptionsToRemove.ContainsKey(resourceType))
                ResourceMaximumSubscriptionsToRemove[resourceType] = new List<Action<int, int>>();

            if (ResourceMaximumSubscriptions[resourceType].Contains(callback))
                ResourceMaximumSubscriptionsToRemove[resourceType].Add(callback);
        }

        public void SetResourceAmount(ResourceType resourceType, int amount)
        {
            if (!CurrentResourceCounts.ContainsKey(resourceType))
                CurrentResourceCounts[resourceType] = 0;

            AddToResource(resourceType, amount - GetResourceAmount(resourceType));
        }

        public void AddToResource(ResourceType resourceType, int amount)
        {
            SetResourceUnlocked(resourceType, true);
            ProcessSubscriptionsToAddAndRemove(resourceType);

            if (!CurrentResourceCounts.ContainsKey(resourceType))
                CurrentResourceCounts[resourceType] = 0;

            CurrentResourceCounts[resourceType] = Math.Min(GetResourceMaxAmount(resourceType), Math.Max(0, CurrentResourceCounts[resourceType] + amount));

            if (!ResourceAmountSubscriptions.ContainsKey(resourceType))
                ResourceAmountSubscriptions[resourceType] = new List<Action<int, int>>();

            foreach (var callback in ResourceAmountSubscriptions[resourceType])
                callback(amount, CurrentResourceCounts[resourceType]);

            foreach (var callback in GenericResourceAmountSubscriptions)
                callback(amount, CurrentResourceCounts[resourceType]);
        }

        public void SetResourceMaximum(ResourceType resourceType, int maxAmount)
        {
            AddToResourceMaximum(resourceType, maxAmount - GetResourceMaxAmount(resourceType));
        }

        private void AddToResourceMaximum(ResourceType resourceType, int maxAmount)
        {
            ProcessSubscriptionsToAddAndRemove(resourceType);

            if (!CurrentResourceMaximum.ContainsKey(resourceType))
                CurrentResourceMaximum[resourceType] = 0;
            CurrentResourceMaximum[resourceType] += maxAmount;

            if (!ResourceMaximumSubscriptions.ContainsKey(resourceType))
                ResourceMaximumSubscriptions[resourceType] = new List<Action<int, int>>();

            foreach (var callback in ResourceMaximumSubscriptions[resourceType])
                callback(maxAmount, CurrentResourceMaximum[resourceType]);

            foreach (var callback in GenericResourceMaximumSubscriptions)
                callback(maxAmount, CurrentResourceMaximum[resourceType]);
        }

        private void ProcessSubscriptionsToAddAndRemove(ResourceType resourceType)
        {
            if (ResourceAmountSubscriptionsToAdd.ContainsKey(resourceType))
            {
                foreach (var callback in ResourceAmountSubscriptionsToAdd[resourceType])
                {
                    if (!ResourceAmountSubscriptions[resourceType].Contains(callback))
                        ResourceAmountSubscriptions[resourceType].Add(callback);
                }
                ResourceAmountSubscriptionsToAdd[resourceType].Clear();
            }

            if (ResourceAmountSubscriptionsToRemove.ContainsKey(resourceType))
            {
                foreach (var callback in ResourceAmountSubscriptionsToRemove[resourceType])
                {
                    if (ResourceAmountSubscriptions[resourceType].Contains(callback))
                        ResourceAmountSubscriptions[resourceType].Remove(callback);
                }
                ResourceAmountSubscriptionsToRemove[resourceType].Clear();
            }

            if (ResourceMaximumSubscriptionsToAdd.ContainsKey(resourceType))
            {
                foreach (var callback in ResourceMaximumSubscriptionsToAdd[resourceType])
                {
                    if (!ResourceMaximumSubscriptions[resourceType].Contains(callback))
                        ResourceMaximumSubscriptions[resourceType].Add(callback);
                }
                ResourceMaximumSubscriptionsToAdd[resourceType].Clear();
            }
            
            if (ResourceMaximumSubscriptionsToRemove.ContainsKey(resourceType))
            {
                foreach (var callback in ResourceMaximumSubscriptionsToRemove[resourceType])
                {
                    if (ResourceMaximumSubscriptions[resourceType].Contains(callback))
                        ResourceMaximumSubscriptions[resourceType].Remove(callback);
                }
                ResourceMaximumSubscriptionsToRemove[resourceType].Clear();
            }
        }
        
        public void SetResourceUnlocked(ResourceType resourceType, bool unlocked)
        {
            if (!CurrentResourceCounts.ContainsKey(resourceType))
                CurrentResourceCounts[resourceType] = 0;
            if (!CurrentResourceMaximum.ContainsKey(resourceType))
                CurrentResourceMaximum[resourceType] = 999;

            CurrentResourceUnlocked[resourceType] = unlocked;
        }

        public int GetResourceCount(ResourceType resourceType)
        {
            if (!CurrentResourceCounts.ContainsKey(resourceType))
                CurrentResourceCounts[resourceType] = 0;
                
            return CurrentResourceCounts[resourceType];
        }
    }
}