using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

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

        private List<ResourceAmountData> Cost = new();
        private List<UIResourceChangeEntry> ResourceChangeEntries = new();
        private List<string> Unlock;

        public void SetContent(string title, string description, string extraInfo, List<ResourceAmountData> cost, List<string> unlock)
        {
            SetTitleText(title);
            SetDescriptionText(description);
            SetExtraInfoText(extraInfo);
            
            if (ResourceCostEntryParent != null) { SetResourceChangeEntries(cost); }

            SetCost(cost);
            Unlock = unlock;

            ShowStatusColor();
            GameSubscriptionSystem.SubscribeToGenericResourceAmountChange((string resourceType, ResourceValue amount, ResourceValue maximum) => { ShowStatusColor(); });

            PurchaseButton?.onClick.RemoveAllListeners();
            PurchaseButton?.onClick.AddListener(() =>
            {
                if (Cost.CheckCanChangeAll())
                {
                    GameController.CurrentGameState.AddToResources(Cost);
                    foreach (var u in Unlock)
                        GameController.CurrentGameState.SetUnlockValue(u, true);
                }
            });
        }

        public void SetResourceChangeEntries(List<ResourceAmountData> cost)
        {
            foreach (Transform child in ResourceCostEntryParent)
                Destroy(child.gameObject);
            ResourceChangeEntries.Clear();
            
            foreach (var ra in cost)
            {
                var entryObject = Instantiate(ResourceCostUIPrefab, ResourceCostEntryParent);
                var entryComponent = entryObject.GetComponent<UIResourceChangeEntry>();
                entryComponent.ResourceNameText?.SetText(ra.ResourceType);
                entryComponent.SetContent(ra.ResourceType, ra.ResourceValue);
                ResourceChangeEntries.Add(entryComponent);
            }
        }

        public void SetTitleText(string title) { TitleText?.SetText(title); }
        public void SetDescriptionText(string description) { DescriptionText?.SetText(description); }
        public void SetExtraInfoText(string extraInfo) { ExtraInfoText?.SetText(extraInfo); }

        public void SetCost(List<ResourceAmountData> cost)
        {
            Cost.Clear();
            foreach (var ra in cost) Cost.Add(new ResourceAmountData(ra.ResourceType, ra.ResourceValue));
        }

        public void ShowStatusColor(bool overrideFinished = false)
        {
            bool keyContained = Unlock.Select(u => GameController.CurrentGameState.GameUnlockList.ContainsKey(u)).ToList().All(uEntry => uEntry == true);
            if (keyContained || overrideFinished)
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