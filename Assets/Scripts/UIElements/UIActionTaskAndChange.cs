using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace HypnicEmpire
{
    public class UIActionTaskAndChange : MonoBehaviour
    {
        //  Assigning workers to an action is meaningless before the settlement has any
        //  citizens, so the worker control's arrows stay hidden until the first one arrives.
        private const string CitizensUnlock = "Unlock_One_Villager";

        [SerializeField] public GameObject ResourceChangeUIPrefab;
        [SerializeField] public UITaskProcessButton ProcessButton;
        [SerializeField] public Transform ResourceChangeEntriesLossParent;
        [SerializeField] public Transform ResourceChangeEntriesGainParent;
        [SerializeField] public UINumberOptionControlEntry WorkerControl;

        private List<ResourceAmountData> ResourceChange = new();

        //  The task this group displays, kept so Refresh can re-render it without re-subscribing.
        private TaskActionState ActionState;

        //  First-run setup only: the subscriptions made here live for the whole session. Use Refresh to
        //  re-render against a newly loaded or reset game state.
        public void SetContent(string actionType, TaskActionState actionState)
        {
            ActionState = actionState;

            InitializeWorkerControlVisibility();

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
                    GameUnlockSystem.AddGameUnlockAction(unlockKey, false, (bool unlocked) => {
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

        //  Idempotent: brings the display back in line with the current game state, for after a load or reset
        //  replaces it. Deliberately does not subscribe to anything — see SetContent.
        public void Refresh()
        {
            ApplyWorkerControlVisibility();

            if (ActionState == null) return;
            if (ResourceChangeUIPrefab == null) return;
            if (ResourceChangeEntriesLossParent == null) return;
            if (ResourceChangeEntriesGainParent == null) return;

            RefreshUI(ActionState);
        }

        private void InitializeWorkerControlVisibility()
        {
            if (WorkerControl == null) return;

            ApplyWorkerControlVisibility();
            GameUnlockSystem.AddGameUnlockAction(CitizensUnlock, true, SetWorkerControlVisible);
        }

        private void ApplyWorkerControlVisibility()
        {
            if (WorkerControl == null) return;

            SetWorkerControlVisible(GameUnlockSystem.IsUnlocked(CitizensUnlock));
        }

        private void SetWorkerControlVisible(bool visible)
        {
            if (WorkerControl == null) return;
            WorkerControl.SetAdjustmentButtonsVisible(visible);
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
            foreach (var entry in resourceChange)
                if (entry.ResourceValue != 0.0)
                    ResourceChange.Add(new ResourceAmountData(entry.ResourceType, entry.ResourceValue));

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