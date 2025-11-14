using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    public class UIActionMenuController : MonoBehaviour
    {
        [SerializeField] public PlayerActionScriptableObject PlayerActionsData;
        [SerializeField] public SerializableDictionary<PlayerActionType, UIActionTaskAndChange> ActionButtonGroupings = new();
        [SerializeField] public SerializableDictionary<PlayerActionSectionType, Transform> ActionSectionAreaMap = new();

        public void InitializeMenu()
        {
            //  Hide all sections and action button groups
            foreach (var section in ActionSectionAreaMap) { section.Value.gameObject.SetActive(false); }
            foreach (var actionButtonGroup in ActionButtonGroupings) { actionButtonGroup.Value.gameObject.SetActive(false); }

            //  Set all content on the action button groups, unhide them when valid, and subscribe to changes
            foreach (var actionType in Enum.GetValues(typeof(PlayerActionType)).Cast<PlayerActionType>())
                SetActionButtonGroupingData(actionType);
        }
        
        public void SetActionButtonGroupingData(PlayerActionType actionType)
        {
            if (!PlayerActionsData || !PlayerActionsData.UnlockToActionMap.Values.Contains(actionType)) return;
            if (!ActionButtonGroupings.ContainsKey(actionType)) return;

            UIActionTaskAndChange actionButtonGroup = ActionButtonGroupings[actionType];
            GameUnlock unlock = PlayerActionsData.UnlockToActionMap.FirstOrDefault(x => x.Value == actionType).Key;
            bool unlockStatus = GameController.CurrentGameState.GameUnlockList.ContainsKey(unlock) ? GameController.CurrentGameState.GameUnlockList[unlock]  : false;
            PlayerActionData playerActionData = PlayerActionsData?.PlayerActions?.Find(pa => pa.ActionType == actionType);
            actionButtonGroup.SetContent(actionType, playerActionData);

            SetActionActive(actionType, unlockStatus);
            GameUnlockSystem.AddGameUnlockAction(unlock, (bool unlocked) => {
                SetActionActive(actionType, unlocked);
            });
        }
        
        public void SetActionActive(PlayerActionType actionType, bool active)
        {
            Debug.Log($"SetActionActive({actionType.ToString()}, {active})");
            var actionButtonGroup = ActionButtonGroupings.ContainsKey(actionType) ? ActionButtonGroupings[actionType] : null;
            actionButtonGroup?.gameObject.SetActive(active);

            PlayerActionData playerActionData = PlayerActionsData?.PlayerActions?.Find(pa => pa.ActionType == actionType);
            if (active && ActionSectionAreaMap.ContainsKey(playerActionData.SectionType))
                ActionSectionAreaMap[playerActionData.SectionType].gameObject.SetActive(true);
        }
    }
}