using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    public class UIActionMenuController : MonoBehaviour
    {
        [SerializeField] public SerializableDictionary<string, UIActionTaskAndChange> ActionButtonGroupings = new();
        [SerializeField] public SerializableDictionary<string, Transform> ActionSectionAreaMap = new();
        [SerializeField] private bool debugActionVisibility;

        //  First-run setup only: the subscriptions made here live for the whole session, so calling this a
        //  second time would leave every action group responding twice to the same unlock. Bringing the menu
        //  back in line with a newly loaded or reset game state is RefreshMenu's job.
        public void InitializeMenu()
        {
            foreach (var actionType in TaskActionSystem.ActionsList)
                SubscribeActionButtonGrouping(actionType);

            RefreshMenu();
        }

        //  Idempotent: re-derives every action group's visibility and displayed values from the current game
        //  state, without touching subscriptions. Safe to call after any load or reset.
        public void RefreshMenu()
        {
            //  Hide all sections and action button groups, then unhide only what the current state unlocks
            foreach (var section in ActionSectionAreaMap) { section.Value.gameObject.SetActive(false); }
            foreach (var actionButtonGroup in ActionButtonGroupings) { actionButtonGroup.Value.gameObject.SetActive(false); }

            foreach (var actionType in TaskActionSystem.ActionsList)
            {
                if (!TryGetActionUnlock(actionType, out string unlock)) continue;

                ActionButtonGroupings[actionType].Refresh();
                SetActionActive(actionType, GameUnlockSystem.IsUnlocked(unlock));
            }
        }

        private void SubscribeActionButtonGrouping(string actionType)
        {
            if (!TryGetActionUnlock(actionType, out string unlock)) return;

            TaskActionState actionState = TaskActionSystem.TaskActionMap[actionType];
            ActionButtonGroupings[actionType].SetContent(actionType, actionState);

            GameUnlockSystem.AddGameUnlockAction(unlock, true, (bool unlocked) => {
                SetActionActive(actionType, unlocked);
            });
        }

        private bool TryGetActionUnlock(string actionType, out string unlock)
        {
            unlock = null;

            if (!ActionButtonGroupings.ContainsKey(actionType)) return false;
            if (!TaskActionSystem.TaskActionMap.ContainsKey(actionType)) return false;
            if (!TaskActionSystem.UnlockToActionMap.Values.Contains(actionType)) return false;

            unlock = TaskActionSystem.UnlockToActionMap.FirstOrDefault(x => x.Value == actionType).Key;
            return unlock != null;
        }

        public void SetActionActive(string actionType, bool active)
        {
            if (debugActionVisibility)
                Debug.Log($"SetActionActive({actionType}, {active})");

            var actionButtonGroup = ActionButtonGroupings.ContainsKey(actionType) ? ActionButtonGroupings[actionType] : null;
            actionButtonGroup?.gameObject.SetActive(active);

            TaskActionState actionState = TaskActionSystem.TaskActionMap[actionType];
            if (active && ActionSectionAreaMap.ContainsKey(actionState.ActionSection))
                ActionSectionAreaMap[actionState.ActionSection].gameObject.SetActive(true);
        }
    }
}