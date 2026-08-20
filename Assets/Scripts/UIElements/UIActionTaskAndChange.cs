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
        [SerializeField] public UINumberOptionControlEntry WorkerControl;

        private List<ResourceAmountData> ResourceChange = new();

        //  The reward rows currently on screen. They outlive a refresh that finds the same change set, so
        //  the full-storage mark has to be re-applied to them rather than left to their initial setup.
        private readonly List<UIResourceChangeEntry> GainEntries = new();

        //  The task this group displays, kept so Refresh can re-render it without re-subscribing.
        private TaskActionState ActionState;

        //  First-run setup only: the subscriptions made here live for the whole session. Use Refresh to
        //  re-render against a newly loaded or reset game state.
        public void SetContent(string actionType, TaskActionState actionState)
        {
            ActionState = actionState;

            InitializeWorkerControl();

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

            //  Raising a store's ceiling makes room without the amount held moving, so a reward marked as
            //  having nowhere to go has to be reconsidered on that too.
            GameSubscriptionSystem.SubscribeToGenericResourceMaximumChange((ResourceValue amount, ResourceValue maxAmount) => {
                RefreshUI(actionState);
            });

            //  Delving is priced by the level being delved rather than by the action's own data, so its rows
            //  have to be redrawn when the player moves to a level whose section asks and gives something else.
            TaskActionSystem.OnActionResourceChangeReplaced -= HandleActionResourceChangeReplaced;
            TaskActionSystem.OnActionResourceChangeReplaced += HandleActionResourceChangeReplaced;

            ProcessButton?.SetContents(actionType, () =>
            {
                GameController.CurrentGameState.AddToResources(actionState.GetResourceChange());
            });
        }

        //  Idempotent: brings the display back in line with the current game state, for after a load or reset
        //  replaces it. Deliberately does not subscribe to anything — see SetContent.
        public void Refresh()
        {
            RefreshWorkerControl();

            if (ActionState == null) return;
            if (ResourceChangeUIPrefab == null) return;
            if (ResourceChangeEntriesLossParent == null) return;
            if (ResourceChangeEntriesGainParent == null) return;

            RefreshUI(ActionState);
        }

        private void InitializeWorkerControl()
        {
            if (WorkerControl == null) return;

            //  The arrows keep these two handlers for the life of the control. Only the numbers beside them
            //  and whether either is allowed move as the game runs, which is RefreshWorkerControl's job.
            WorkerControl.SetContent(string.Empty, string.Empty, AssignWorker, UnassignWorker);

            //  Jobs are shared across a section, so an assignment made on any action can change what this
            //  one is allowed; the population and the jobs themselves both follow from what has been built.
            JobAssignmentSystem.OnAssignmentsChanged -= RefreshWorkerControl;
            JobAssignmentSystem.OnAssignmentsChanged += RefreshWorkerControl;
            ModifierValueSystem.OnValuesRecomputed -= RefreshWorkerControl;
            ModifierValueSystem.OnValuesRecomputed += RefreshWorkerControl;

            RefreshWorkerControl();
        }

        private void OnDestroy()
        {
            JobAssignmentSystem.OnAssignmentsChanged -= RefreshWorkerControl;
            ModifierValueSystem.OnValuesRecomputed -= RefreshWorkerControl;
            TaskActionSystem.OnActionResourceChangeReplaced -= HandleActionResourceChangeReplaced;
        }

        //  Every group hears every repricing, so each has to pick out its own.
        private void HandleActionResourceChangeReplaced(string actionName)
        {
            if (ActionState == null || actionName != ActionState.Name) return;

            RefreshUI(ActionState);
        }

        //  A group whose action never resolved has no worker to move; the arrows are wired before that is
        //  known, so the click is the place to check.
        private void AssignWorker()
        {
            if (ActionState != null) JobAssignmentSystem.Assign(ActionState.Name);
        }

        private void UnassignWorker()
        {
            if (ActionState != null) JobAssignmentSystem.Unassign(ActionState.Name);
        }

        //  Shows what this action has against the cap it shares with its section, and greys an arrow that
        //  has nothing left to do: no idle villager or no free job going up, nobody assigned coming down.
        //
        //  Job capacity is earned by building, so until the section has a single job to fill there is
        //  nothing worth saying: the control hides entirely rather than offer a 0/0 nobody can act on.
        private void RefreshWorkerControl()
        {
            if (WorkerControl == null || ActionState == null) return;

            int assigned = JobAssignmentSystem.AssignedToAction(ActionState.Name);
            int jobCap = JobAssignmentSystem.JobCapOfAction(ActionState.Name);

            WorkerControl.SetDisplayDetails(string.Empty, Localization.DisplayText_WorkersAssigned(assigned, jobCap),
                JobAssignmentSystem.CanAssign(ActionState.Name), JobAssignmentSystem.CanUnassign(ActionState.Name));

            WorkerControl.SetVisible(jobCap > 0);
        }

        private void RefreshUI(TaskActionState actionState)
        {
            var actionResourceChange = actionState.GetResourceChange();
            SetResourceChangeUI(actionResourceChange);

            foreach (var entry in GainEntries)
                if (entry != null) entry.ShowRewardStorageState();

            List<ResourceAmountData> gainChange = actionResourceChange.Where(rc => rc.ResourceValue > 0).ToList();
            List<ResourceAmountData> lossChange = actionResourceChange.Where(rc => rc.ResourceValue < 0).ToList();
            ProcessButton?.SetEnabled(gainChange.CheckCanChangeAny(true) && lossChange.CheckCanChangeAll());
        }

        public void SetResourceChangeUI(List<ResourceAmountData> resourceChange)
        {
            //  An authored 0 is a slot for a gain or cost that is not happening yet (Hunting's potions
            //  before Silent Hunter, Fishing's luxuries before the river). Those rows stay off until
            //  the amount is actually produced or taken.
            var visibleChange = new List<ResourceAmountData>();
            if (resourceChange != null)
                foreach (var entry in resourceChange)
                    if (entry != null && entry.ResourceValue != 0)
                        visibleChange.Add(new ResourceAmountData(entry.ResourceType, entry.ResourceValue));

            if (visibleChange.IsIdentical(ResourceChange)) return;

            ResourceChange.Clear();
            foreach (var entry in visibleChange)
                ResourceChange.Add(entry);

            ClearResourceChangeUI();
            AddResourceChangeUI(ResourceChange);
        }

        private void ClearResourceChangeUI()
        {
            GainEntries.Clear();

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
                bool isGain = ra.ResourceValue >= 0;
                var entryObject = Instantiate(ResourceChangeUIPrefab, isGain ? ResourceChangeEntriesGainParent : ResourceChangeEntriesLossParent);
                var entryComponent = entryObject.GetComponent<UIResourceChangeEntry>();
                entryComponent.SetContent(ra.ResourceType, ra.ResourceValue);

                if (isGain) GainEntries.Add(entryComponent);
            }
        }
    }
}