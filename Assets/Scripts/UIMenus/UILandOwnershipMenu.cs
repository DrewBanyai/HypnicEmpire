using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HypnicEmpire
{
    public class UILandOwnershipMenu : MonoBehaviour
    {
        private const string BuyingLandUnlock = "Unlock_Buying_Land";

        [SerializeField] public GameObject ResourceChangeUIPrefab;
        [SerializeField] public Transform ResourceChangeEntryParent;

        [SerializeField] public TextMeshProUGUI LandOwnedText;
        [SerializeField] public TextMeshProUGUI LandUsedText;
        [SerializeField] public TextMeshProUGUI LandFreeText;

        [SerializeField] public Button BuyLandButton;

        //  The whole Land Ownership section (title + menu). Unlock actions never apply an initial
        //  state, so this starts hidden and is re-applied on load via ApplyRevealState.
        [SerializeField] public GameObject RevealRoot;

        //  Only the resources spent on a purchase can report affordability, so the land gained by it is
        //  displayed but never kept here.
        private readonly List<UIResourceChangeEntry> CostEntries = new();

        public void InitializeMenu()
        {
            BuildResourceChangeEntries();

            BuyLandButton?.onClick.RemoveAllListeners();
            BuyLandButton?.onClick.AddListener(LandSystem.BuyLand);

            //  Land is bought with resources, so the button's enabled state follows the player's money.
            GameSubscriptionSystem.SubscribeToGenericResourceAmountChange((string resourceType, ResourceValue amount, ResourceValue maximum) => { RefreshPurchaseState(); });
            LandSystem.OnLandChanged += RefreshDisplay;

            GameUnlockSystem.AddGameUnlockAction(BuyingLandUnlock, true, SetRevealed);
            ApplyRevealState();
            RefreshDisplay();
        }

        private void OnDestroy()
        {
            LandSystem.OnLandChanged -= RefreshDisplay;
        }

        public void ApplyRevealState()
        {
            SetRevealed(GameUnlockSystem.IsUnlocked(BuyingLandUnlock));
        }

        private void SetRevealed(bool revealed)
        {
            (RevealRoot != null ? RevealRoot : gameObject).SetActive(revealed);
        }

        public void RefreshDisplay()
        {
            RefreshLandCounts();
            RefreshPurchaseState();
        }

        private void RefreshLandCounts()
        {
            LandOwnedText?.SetText(Localization.DisplayText_LandAmount(LandSystem.LandOwned));
            LandUsedText?.SetText(Localization.DisplayText_LandAmount(LandSystem.LandUsed));
            LandFreeText?.SetText(Localization.DisplayText_LandAmount(LandSystem.LandFree));
        }

        private void RefreshPurchaseState()
        {
            BuyLandButton?.SetInteractable(LandSystem.CanBuyLand());

            foreach (var entry in CostEntries)
                entry.CheckCanChange();
        }

        private void BuildResourceChangeEntries()
        {
            if (ResourceChangeUIPrefab == null || ResourceChangeEntryParent == null) return;

            //  A source without the entry component can only ever produce inert clones, so it is
            //  reported instead of quietly filling the list with them.
            if (ResourceChangeUIPrefab.GetComponent<UIResourceChangeEntry>() == null)
            {
                Debug.LogWarning($"'{name}' cannot build land cost entries: '{ResourceChangeUIPrefab.name}' has no {nameof(UIResourceChangeEntry)}.", this);
                return;
            }

            foreach (Transform child in ResourceChangeEntryParent)
                Destroy(child.gameObject);
            CostEntries.Clear();

            foreach (var change in LandSystem.GetLandPurchaseChanges())
            {
                var entryObject = Instantiate(ResourceChangeUIPrefab, ResourceChangeEntryParent);

                //  Entry prefabs carry an authored depth from the screen space camera canvas they were
                //  built in. Stacked onto the depth of this panel it puts the row behind the camera, so
                //  rows are pinned to the plane of the panel. The layout group still places x and y.
                entryObject.transform.localPosition = Vector3.zero;

                var entryComponent = entryObject.GetComponent<UIResourceChangeEntry>();

                entryComponent.SetContent(change.ResourceType, change.ResourceValue);

                if (change.ResourceType != LandSystem.LandValueName)
                    CostEntries.Add(entryComponent);
            }
        }
    }
}
