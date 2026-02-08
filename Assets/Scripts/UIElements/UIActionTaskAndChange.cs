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

            //  TODO: Subscribe to any changes in Player Action data so that we can alter resources as needed and call SetResourceChangeUI on any changes
            SetResourceChangeUI(actionState.GetResourceChange());

            //  Look up the resource change values for this action (TODO: Pull based on developments/upgrades!!!)
            var actionResourceChange = actionState.GetResourceChange();
            List<ResourceAmountData> gainChange = actionResourceChange.Where(rc => rc.ResourceValue > 0).ToList();
            List<ResourceAmountData> lossChange = actionResourceChange.Where(rc => rc.ResourceValue < 0).ToList();
            ProcessButton?.SetEnabled(gainChange.CheckCanChangeAny(true) && lossChange.CheckCanChangeAll());
            GameSubscriptionSystem.SubscribeToGenericResourceAmountChange((string resourceType, ResourceValue amount, ResourceValue maxAmount) => {
                List<ResourceAmountData> gainChange = actionResourceChange.Where(rc => rc.ResourceValue > 0).ToList();
                List<ResourceAmountData> lossChange = actionResourceChange.Where(rc => rc.ResourceValue < 0).ToList();
                ProcessButton?.SetEnabled(gainChange.CheckCanChangeAny(true) && lossChange.CheckCanChangeAll());
            });

            ProcessButton?.SetContents(actionType, 20f, 100f, () =>
            {
                GameController.CurrentGameState.AddToResources(actionState.GetResourceChange());
            });
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