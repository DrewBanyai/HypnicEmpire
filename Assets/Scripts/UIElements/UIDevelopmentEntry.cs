using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace HypnicEmpire
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class UIDevelopmentEntry : MonoBehaviour
    {
        public static Color UnableToAffordColor => new Color32(223, 140, 110, 255);
        public static Color AbleToAffordColor => new Color32(174, 173, 151, 255);
        public static Color PurchasedColor => new Color32(135, 129, 133, 255);

        [SerializeField] public GameObject ResourceCostUIPrefab;
        [SerializeField] public TextMeshProUGUI TitleText;
        [SerializeField] public TextMeshProUGUI DescriptionText;
        [SerializeField] public TextMeshProUGUI ExtraInfoText;
        [SerializeField] public Transform ResourceCostEntryParent;
        [SerializeField] public Button PurchaseButton;
        [SerializeField] public Image ButtonBG;

        private List<ResourceAmount> Cost = new();
        private List<UIResourceChangeEntry> ResourceChangeEntries = new();
        private GameUnlock Unlock;

        public void SetContent(string title, string description, string extraInfo, List<ResourceAmount> cost, GameUnlock unlock)
        {
            SetTitleText(title);
            SetDescriptionText(description);
            SetExtraInfoText(extraInfo);

            if (ResourceCostEntryParent != null) { SetResourceChangeEntries(cost); }

            SetCost(cost);
            Unlock = unlock;

            ShowStatusColor();
            GameController.GameSubscriptions.SubscribeToGenericResourceAmountChange((ResourceType rType, int amount, int maximum) => { ShowStatusColor(); });

            PurchaseButton?.onClick.RemoveAllListeners();
            PurchaseButton?.onClick.AddListener(() =>
            {
                if (Cost.CheckCanChangeAll())
                {
                    GameController.CurrentGameState.AddToResources(Cost);
                    GameController.CurrentGameState.SetUnlockValue(Unlock, true);
                }
            });
        }

        public void SetResourceChangeEntries(List<ResourceAmount> cost)
        {
            foreach (Transform child in ResourceCostEntryParent)
                Destroy(child.gameObject);
            ResourceChangeEntries.Clear();
            
            foreach (var costAmount in cost)
            {
                var entryObject = Instantiate(ResourceCostUIPrefab, ResourceCostEntryParent);
                var entryComponent = entryObject.GetComponent<UIResourceChangeEntry>();
                entryComponent.ResourceNameText?.SetText(costAmount.ResourceType.ToString());
                entryComponent.SetContent(costAmount.ResourceType, costAmount.Amount);
                ResourceChangeEntries.Add(entryComponent);
            }
        }

        public void SetTitleText(string title) { TitleText?.SetText(title); }
        public void SetDescriptionText(string description) { DescriptionText?.SetText(description); }
        public void SetExtraInfoText(string extraInfo) { ExtraInfoText?.SetText(extraInfo); }

        public void SetCost(List<ResourceAmount> cost)
        {
            Cost.Clear();
            foreach (var ra in cost) Cost.Add(new ResourceAmount(ra.ResourceType, ra.Amount));
        }

        public void ShowStatusColor(bool overrideFinished = false)
        {
            if (GameController.CurrentGameState.GameUnlockList.ContainsKey(Unlock) || overrideFinished)
            {
                PurchaseButton?.SetInteractable(false);
                ButtonBG?.SetColor(PurchasedColor);
                ResourceCostEntryParent.gameObject.SetActive(false);
                return;
            }

            if (Cost.CheckCanChangeAll())
            {
                PurchaseButton?.SetInteractable(true);
                ButtonBG?.SetColor(AbleToAffordColor);
                ResourceCostEntryParent.gameObject.SetActive(true);
            }
            else
            {
                PurchaseButton?.SetInteractable(false);
                ButtonBG?.SetColor(UnableToAffordColor);
                ResourceCostEntryParent.gameObject.SetActive(true);
            }
            
            foreach (var entry in ResourceChangeEntries) { entry.CheckCanChange(true, true); }
        }
    }
}