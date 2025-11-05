using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace HypnicEmpire
{
    [RequireComponent(typeof(Button))]
    public class UIDevelopmentEntry : MonoBehaviour
    {
        [SerializeField] public GameObject ResourceCostUIPrefab;
        [SerializeField] public TextMeshProUGUI TitleText;
        [SerializeField] public TextMeshProUGUI DescriptionText;
        [SerializeField] public TextMeshProUGUI ExtraInfoText;
        [SerializeField] public Transform ResourceCostEntryParent;
        [SerializeField] public Button PurchaseButton;

        private List<ResourceAmount> Cost = new();
        private GameUnlock Unlock;

        public void SetContent(string title, string description, string extraInfo, List<ResourceAmount> cost, GameUnlock unlock)
        {
            SetTitleText(title);
            SetDescriptionText(description);
            SetExtraInfoText(extraInfo);

            if (ResourceCostEntryParent != null)
            {
                foreach (Transform child in ResourceCostEntryParent)
                    Destroy(child.gameObject);
                foreach (var costAmount in cost)
                {
                    var entryObject = Instantiate(ResourceCostUIPrefab, ResourceCostEntryParent);
                    var entryComponent = entryObject.GetComponent<UIResourceChangeEntry>();
                    entryComponent.ResourceNameText?.SetText(costAmount.ResourceType.ToString());
                    entryComponent.SetContent(costAmount.ResourceType, costAmount.Amount);
                }
            }

            SetCost(cost);
            Unlock = unlock;

            PurchaseButton?.onClick.RemoveAllListeners();
            PurchaseButton?.onClick.AddListener(() =>
            {
                if (Cost.CheckCanChangeAll())
                {
                    GameController.CurrentGameState.AddToResources(Cost);
                    GameUnlockSystem.SetUnlockValue(Unlock, true);
                }
            });
        }

        public void SetTitleText(string title) { TitleText?.SetText(title); }
        public void SetDescriptionText(string description) { DescriptionText?.SetText(description); }
        public void SetExtraInfoText(string extraInfo) { ExtraInfoText?.SetText(extraInfo); }

        public void SetCost(List<ResourceAmount> cost)
        {
            Cost.Clear();
            foreach (var ra in cost) Cost.Add(new ResourceAmount(ra.ResourceType, ra.Amount));
        }
    }
}