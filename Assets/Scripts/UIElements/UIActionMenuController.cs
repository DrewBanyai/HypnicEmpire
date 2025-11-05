using UnityEngine;
using System.Collections.Generic;

namespace HypnicEmpire
{
    public class UIActionMenuController : MonoBehaviour
    {
        [SerializeField] public PlayerActionScriptableObject PlayerActionDataMap;
        [SerializeField] public GameObject ActionEntryPrefab;
        [SerializeField] public SerializableDictionary<PlayerActionSectionType, Transform> ActionSectionParentMap = new();
        [SerializeField] public SerializableDictionary<PlayerActionSectionType, Transform> ActionSectionAreaMap = new();
        
        private Dictionary<PlayerActionType, UIActionTaskAndChange> ActionTaskAndChangeMap = new();

        public void SetActionEntry(PlayerActionType actionType, bool show)
        {
            //  TODO: Have this be replaced by a call that determines the action map on the fly using unlocks (costs lowered, rewards raised?)
            var playerActionData = PlayerActionDataMap.PlayerActions.Find(pa => pa.ActionType == actionType);
            if (playerActionData == null) return;

            if (ActionEntryPrefab == null) return;
            if (ActionSectionParentMap == null || ActionSectionParentMap.ContainsKey(playerActionData.SectionType) == false) return;
            if (ActionSectionAreaMap == null || ActionSectionAreaMap.ContainsKey(playerActionData.SectionType) == false) return;
            if (PlayerActionDataMap == null) return;

            if (!ActionTaskAndChangeMap.ContainsKey(actionType))
            {
                if (show == false) return;
                var entryObject = Instantiate(ActionEntryPrefab, ActionSectionParentMap[playerActionData.SectionType]);
                ActionTaskAndChangeMap[actionType] = entryObject.GetComponent<UIActionTaskAndChange>();
            }

            ActionSectionAreaMap[playerActionData.SectionType].gameObject.SetActive(true);
            ActionTaskAndChangeMap[actionType]?.SetContent(actionType, playerActionData, show);
        }
    }
}