using System;
using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    public class GameSubscriptionSystem
    {
        //  Subscriptions to changes in Resource Amount or Maximum
        public List<Action<ResourceType, int, int>> GenericResourceAmountSubscriptions = new();
        public List<Action<int, int>> GenericResourceMaximumSubscriptions = new();
        public SerializableDictionary<ResourceType, List<Action<int, int>>> ResourceAmountSubscriptions = new();
        public SerializableDictionary<ResourceType, List<Action<int, int>>> ResourceMaximumSubscriptions = new();

        // Subscriptions to add or remove (before next event response)
        public SerializableDictionary<ResourceType, List<Action<int, int>>> ResourceAmountSubscriptionsToAdd = new();
        public SerializableDictionary<ResourceType, List<Action<int, int>>> ResourceMaximumSubscriptionsToAdd = new();
        public SerializableDictionary<ResourceType, List<Action<int, int>>> ResourceAmountSubscriptionsToRemove = new();
        public SerializableDictionary<ResourceType, List<Action<int, int>>> ResourceMaximumSubscriptionsToRemove = new();

        public void ClearAllSubscriptions()
        {
            GenericResourceAmountSubscriptions = new();
            GenericResourceMaximumSubscriptions = new();
            ResourceAmountSubscriptions = new();
            ResourceMaximumSubscriptions = new();
            ResourceAmountSubscriptionsToAdd = new();
            ResourceMaximumSubscriptionsToAdd = new();
            ResourceAmountSubscriptionsToRemove = new();
            ResourceMaximumSubscriptionsToRemove = new();
            
            foreach (var rt in Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>())
            {
                ResourceAmountSubscriptions[rt] = new();
                ResourceMaximumSubscriptions[rt] = new();
                ResourceAmountSubscriptionsToAdd[rt] = new();
                ResourceMaximumSubscriptionsToAdd[rt] = new();
                ResourceAmountSubscriptionsToRemove[rt] = new();
                ResourceMaximumSubscriptionsToRemove[rt] = new();
            }
        }

        public void SubscribeToGenericResourceAmountChange(Action<ResourceType, int, int> callback) { GenericResourceAmountSubscriptions.Add(callback); }
        public void SubscribeToGenericResourceMaximumChange(Action<int, int> callback) { GenericResourceMaximumSubscriptions.Add(callback); }
        public void SubscribeToResourceAmount(ResourceType resourceType, Action<int, int> callback) { ResourceAmountSubscriptionsToAdd[resourceType].Add(callback); }
        public void UnsubscribeToResourceAmount(ResourceType resourceType, Action<int, int> callback)
        {
            if (ResourceAmountSubscriptions[resourceType].Contains(callback)) ResourceAmountSubscriptionsToRemove[resourceType].Add(callback);
        }
        public void SubscribeToResourceMaximum(ResourceType resourceType, Action<int, int> callback) { ResourceMaximumSubscriptionsToAdd[resourceType].Add(callback); }
        public void UnsubscribeToResourceMaximum(ResourceType resourceType, Action<int, int> callback)
        {
            if (ResourceMaximumSubscriptions[resourceType].Contains(callback)) ResourceMaximumSubscriptionsToRemove[resourceType].Add(callback);
        }

        public void ProcessSubscriptionsToAddAndRemove(ResourceType resourceType)
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