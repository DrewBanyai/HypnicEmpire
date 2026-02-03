using System;
using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    public static class GameSubscriptionSystem
    {
        //  Subscriptions to changes in Resource Value or Maximum
        public static List<Action<string, ResourceValue, ResourceValue>> GenericResourceAmountSubscriptions = new();
        public static List<Action<ResourceValue, ResourceValue>> GenericResourceMaximumSubscriptions = new();
        public static SerializableDictionary<string, List<Action<ResourceValue, ResourceValue>>> ResourceAmountSubscriptions = new();
        public static SerializableDictionary<string, List<Action<ResourceValue, ResourceValue>>> ResourceMaximumSubscriptions = new();

        // Subscriptions to add or remove (before next event response)
        public static SerializableDictionary<string, List<Action<ResourceValue, ResourceValue>>> ResourceAmountSubscriptionsToAdd = new();
        public static SerializableDictionary<string, List<Action<ResourceValue, ResourceValue>>> ResourceMaximumSubscriptionsToAdd = new();
        public static SerializableDictionary<string, List<Action<ResourceValue, ResourceValue>>> ResourceAmountSubscriptionsToRemove = new();
        public static SerializableDictionary<string, List<Action<ResourceValue, ResourceValue>>> ResourceMaximumSubscriptionsToRemove = new();

        public static void CreateResourceTypeSubscriptionMaps()
        {
            GenericResourceAmountSubscriptions = new();
            GenericResourceMaximumSubscriptions = new();
            ResourceAmountSubscriptions = new();
            ResourceMaximumSubscriptions = new();
            ResourceAmountSubscriptionsToAdd = new();
            ResourceMaximumSubscriptionsToAdd = new();
            ResourceAmountSubscriptionsToRemove = new();
            ResourceMaximumSubscriptionsToRemove = new();

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

        public static void SubscribeToGenericResourceAmountChange(Action<string, ResourceValue, ResourceValue> callback) { GenericResourceAmountSubscriptions.Add(callback); }
        public static void SubscribeToGenericResourceMaximumChange(Action<ResourceValue, ResourceValue> callback) { GenericResourceMaximumSubscriptions.Add(callback); }
        public static void SubscribeToResourceAmount(string resourceType, Action<ResourceValue, ResourceValue> callback) { ResourceAmountSubscriptionsToAdd[resourceType].Add(callback); }
        public static void UnsubscribeToResourceAmount(string resourceType, Action<ResourceValue, ResourceValue> callback)
        {
            if (ResourceAmountSubscriptions[resourceType].Contains(callback)) ResourceAmountSubscriptionsToRemove[resourceType].Add(callback);
        }
        public static void SubscribeToResourceMaximum(string resourceType, Action<ResourceValue, ResourceValue> callback) { ResourceMaximumSubscriptionsToAdd[resourceType].Add(callback); }
        public static void UnsubscribeToResourceMaximum(string resourceType, Action<ResourceValue, ResourceValue> callback)
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