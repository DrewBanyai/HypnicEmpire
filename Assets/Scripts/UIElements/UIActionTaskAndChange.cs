using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace HypnicEmpire
{
    public class UIActionTaskAndChange : MonoBehaviour
    {
        [SerializeField] public GameObject ResourceChangeUIPrefab;
        [SerializeField] public UITaskProcessButton ProcessButton;
        [SerializeField] public Transform ResourceChangeEntriesLossParent;
        [SerializeField] public Transform ResourceChangeEntriesGainParent;

        private List<ResourceAmountData> ResourceChange = new();

        public void SetContent(string actionType, TaskActionState actionState)
        {

            if (ResourceChangeUIPrefab == null) return;
            if (ResourceChangeEntriesLossParent == null) return;
            if (ResourceChangeEntriesGainParent == null) return;

            // Initial UI setup
            RefreshUI(actionState);

            // Subscribe to all unlocks that can affect this task's values
            if (actionState.ValueDeterminant != null && actionState.ValueDeterminant.UnlockAlterations != null)
            {
                foreach (var unlockKey in actionState.ValueDeterminant.UnlockAlterations.Keys)
                {
                    GameUnlockSystem.AddGameUnlockAction(unlockKey, (bool unlocked) => {
                        RefreshUI(actionState);
                    });
                }
            }

            // Subscribe to resource amount changes to update button enabled state
            GameSubscriptionSystem.SubscribeToGenericResourceAmountChange((string resourceType, ResourceValue amount, ResourceValue maxAmount) => {
                RefreshUI(actionState);
            });

            ProcessButton?.SetContents(actionType, 20f, 100f, () =>
            {
                GameController.CurrentGameState.AddToResources(actionState.GetResourceChange());
            });
        }

        private void RefreshUI(TaskActionState actionState)
        {
            var actionResourceChange = actionState.GetResourceChange();
            SetResourceChangeUI(actionResourceChange);

            List<ResourceAmountData> gainChange = actionResourceChange.Where(rc => rc.ResourceValue > 0).ToList();
            List<ResourceAmountData> lossChange = actionResourceChange.Where(rc => rc.ResourceValue < 0).ToList();
            ProcessButton?.SetEnabled(gainChange.CheckCanChangeAny(true) && lossChange.CheckCanChangeAll());
        }

        public void SetResourceChangeUI(List<ResourceAmountData> resourceChange)
        {
            if (resourceChange.IsIdentical(ResourceChange)) return;

            ResourceChange.Clear();
            foreach (var entry in resourceChange) ResourceChange.Add(new ResourceAmountData(entry.ResourceType, entry.ResourceValue));

            ClearResourceChangeUI();
            AddResourceChangeUI(resourceChange);
        }

        private void ClearResourceChangeUI()
        {
            foreach (Transform child in ResourceChangeEntriesLossParent)
                Destroy(child.gameObject);

            foreach (Transform child in ResourceChangeEntriesGainParent)
                Destroy(child.gameObject);
        }
        
        private void AddResourceChangeUI(List<ResourceAmountData> resourceChange)
        {
            for (int i = 0; i < resourceChange.Count; ++i)
            {
                var ra = resourceChange[i];
                var entryObject = Instantiate(ResourceChangeUIPrefab, (ra.ResourceValue >= 0) ? ResourceChangeEntriesGainParent : ResourceChangeEntriesLossParent);
                var entryComponent = entryObject.GetComponent<UIResourceChangeEntry>();
                entryComponent.SetContent(ra.ResourceType, ra.ResourceValue);
            }
        }
    }
}