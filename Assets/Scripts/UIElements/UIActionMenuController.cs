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

        public void InitializeMenu()
        {
            //  Hide all sections and action button groups
            foreach (var section in ActionSectionAreaMap) { section.Value.gameObject.SetActive(false); }
            foreach (var actionButtonGroup in ActionButtonGroupings) { actionButtonGroup.Value.gameObject.SetActive(false); }

            //  Set all content on the action button groups, unhide them when valid, and subscribe to changes
            foreach (var actionType in TaskActionSystem.ActionsList)
                SetActionButtonGroupingData(actionType);
        }
        
        public void SetActionButtonGroupingData(string actionType)
        {
            if (!TaskActionSystem.UnlockToActionMap.Values.Contains(actionType)) return;
            if (!ActionButtonGroupings.ContainsKey(actionType)) return;

            UIActionTaskAndChange actionButtonGroup = ActionButtonGroupings[actionType];
            string unlock = TaskActionSystem.UnlockToActionMap.FirstOrDefault(x => x.Value == actionType).Key.ToString();
            bool unlockStatus = GameUnlockSystem.IsUnlocked(unlock);
            TaskActionState actionState = TaskActionSystem.TaskActionMap[actionType];
            actionButtonGroup.SetContent(actionType, actionState);

            SetActionActive(actionType, unlockStatus);
            GameUnlockSystem.AddGameUnlockAction(unlock, (bool unlocked) => {
                SetActionActive(actionType, unlocked);
            });
        }
        
        public void SetActionActive(string actionType, bool active)
        {
            Debug.Log($"SetActionActive({actionType}, {active})");
            var actionButtonGroup = ActionButtonGroupings.ContainsKey(actionType) ? ActionButtonGroupings[actionType] : null;
            actionButtonGroup?.gameObject.SetActive(active);

            TaskActionState actionState = TaskActionSystem.TaskActionMap[actionType];
            if (active && ActionSectionAreaMap.ContainsKey(actionState.ActionSection))
                ActionSectionAreaMap[actionState.ActionSection].gameObject.SetActive(true);
        }
    }
}