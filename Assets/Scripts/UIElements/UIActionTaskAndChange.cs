using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    public class UIActionTaskAndChange : MonoBehaviour
    {
        [SerializeField] public GameObject ResourceChangeUIPrefab;
        [SerializeField] public UITaskProcessButton ProcessButton;
        [SerializeField] public Transform ResourceChangeEntriesLossParent;
        [SerializeField] public Transform ResourceChangeEntriesGainParent;

        private List<ResourceAmount> ResourceChange = new();

        public void SetContent(PlayerActionType actionType, PlayerActionData playerActionData)
        {

            if (ResourceChangeUIPrefab == null) return;
            if (ResourceChangeEntriesLossParent == null) return;
            if (ResourceChangeEntriesGainParent == null) return;

            //  TODO: Subscribe to any changes in Player Action data so that we can alter resources as needed and call SetResourceChangeUI on any changes
            SetResourceChangeUI(playerActionData.ResourceChange);

            //  Look up the resource change values for this action (TODO: Pull based on developments/upgrades!!!)

            List<ResourceAmount> gainChange = playerActionData.ResourceChange.Where(rc => rc.Amount > 0).ToList();
            List<ResourceAmount> lossChange = playerActionData.ResourceChange.Where(rc => rc.Amount < 0).ToList();
            ProcessButton?.SetEnabled(gainChange.CheckCanChangeAny() && lossChange.CheckCanChangeAll());

            GameController.GameSubscriptions.SubscribeToGenericResourceAmountChange((ResourceType rType, int amount, int maxAmount) =>
            {
                List<ResourceAmount> gainChange = playerActionData.ResourceChange.Where(rc => rc.Amount > 0).ToList();
                List<ResourceAmount> lossChange = playerActionData.ResourceChange.Where(rc => rc.Amount < 0).ToList();
                ProcessButton?.SetEnabled(gainChange.CheckCanChangeAny() && lossChange.CheckCanChangeAll());
            });

            ProcessButton?.SetContents(actionType, 20f, 100f, () =>
            {
                GameController.CurrentGameState.AddToResources(ResourceChange);
            });
        }

        public void SetResourceChangeUI(List<ResourceAmount> resourceChange)
        {
            if (resourceChange.IsIdentical(ResourceChange)) return;

            ResourceChange.Clear();
            foreach (var entry in resourceChange) ResourceChange.Add(new ResourceAmount(entry.ResourceType, entry.Amount));

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
        
        private void AddResourceChangeUI(List<ResourceAmount> resourceChange)
        {
            for (int i = 0; i < resourceChange.Count; ++i)
            {
                var rAmount = resourceChange[i];
                var entryObject = Instantiate(ResourceChangeUIPrefab, (rAmount.Amount >= 0) ? ResourceChangeEntriesGainParent : ResourceChangeEntriesLossParent);
                var entryComponent = entryObject.GetComponent<UIResourceChangeEntry>();
                entryComponent.SetContent(rAmount.ResourceType, rAmount.Amount);
            }
        }
    }
}