using System;
using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    public static class GameSubscriptionSystem
    {
        //  Subscriptions to changes in Resource Amount or Maximum
        public static List<Action<string, int, int>> GenericResourceAmountSubscriptions = new();
        public static List<Action<int, int>> GenericResourceMaximumSubscriptions = new();
        public static SerializableDictionary<string, List<Action<int, int>>> ResourceAmountSubscriptions = new();
        public static SerializableDictionary<string, List<Action<int, int>>> ResourceMaximumSubscriptions = new();

        // Subscriptions to add or remove (before next event response)
        public static SerializableDictionary<string, List<Action<int, int>>> ResourceAmountSubscriptionsToAdd = new();
        public static SerializableDictionary<string, List<Action<int, int>>> ResourceMaximumSubscriptionsToAdd = new();
        public static SerializableDictionary<string, List<Action<int, int>>> ResourceAmountSubscriptionsToRemove = new();
        public static SerializableDictionary<string, List<Action<int, int>>> ResourceMaximumSubscriptionsToRemove = new();

        public static void ClearAllSubscriptions()
        {
            GenericResourceAmountSubscriptions = new();
            GenericResourceMaximumSubscriptions = new();
            ResourceAmountSubscriptions = new();
            ResourceMaximumSubscriptions = new();
            ResourceAmountSubscriptionsToAdd = new();
            ResourceMaximumSubscriptionsToAdd = new();
            ResourceAmountSubscriptionsToRemove = new();
            ResourceMaximumSubscriptionsToRemove = new();
        }

        public static void CreateResourceTypeSubscriptionMaps()
        {
            foreach (var rt in ResourceTypeSystem.ResourceData.ResourceTypes)
            {
                ResourceAmountSubscriptions[rt.Name] = new();
                ResourceMaximumSubscriptions[rt.Name] = new();
                ResourceAmountSubscriptionsToAdd[rt.Name] = new();
                ResourceMaximumSubscriptionsToAdd[rt.Name] = new();
                ResourceAmountSubscriptionsToRemove[rt.Name] = new();
                ResourceMaximumSubscriptionsToRemove[rt.Name] = new();
            }
        }

        public static void SubscribeToGenericResourceAmountChange(Action<string, int, int> callback) { GenericResourceAmountSubscriptions.Add(callback); }
        public static void SubscribeToGenericResourceMaximumChange(Action<int, int> callback) { GenericResourceMaximumSubscriptions.Add(callback); }
        public static void SubscribeToResourceAmount(string resourceType, Action<int, int> callback) { ResourceAmountSubscriptionsToAdd[resourceType].Add(callback); }
        public static void UnsubscribeToResourceAmount(string resourceType, Action<int, int> callback)
        {
            if (ResourceAmountSubscriptions[resourceType].Contains(callback)) ResourceAmountSubscriptionsToRemove[resourceType].Add(callback);
        }
        public static void SubscribeToResourceMaximum(string resourceType, Action<int, int> callback) { ResourceMaximumSubscriptionsToAdd[resourceType].Add(callback); }
        public static void UnsubscribeToResourceMaximum(string resourceType, Action<int, int> callback)
        {
            if (ResourceMaximumSubscriptions[resourceType].Contains(callback)) ResourceMaximumSubscriptionsToRemove[resourceType].Add(callback);
        }

        public static void ProcessSubscriptionsToAddAndRemove(string resourceType)
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

    }
}