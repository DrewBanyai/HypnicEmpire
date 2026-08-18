using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HypnicEmpire
{
    //  One building in the buildings menu: what it is, how many stand, what the next one costs and the
    //  click that buys it. The whole box is the button, so pointer events are handled here rather than
    //  through a Button component on a child.
    public class UIBuildingButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] public string BuildingName;
        [SerializeField] public Image BuildingButtonBox;

        [SerializeField] public TextMeshProUGUI BuildingTitleText;

        [SerializeField] public TextMeshProUGUI BuildingCountText;
        [SerializeField] public Image BuildingIconImage;

        [SerializeField] public TextMeshProUGUI BuildingDescriptionText;

        [SerializeField] public TextMeshProUGUI BuildingEffectText;

        //  Optional. With either of these unassigned the button works without a cost breakdown.
        [SerializeField] public GameObject ResourceCostUIPrefab;
        [SerializeField] public Transform ResourceCostEntryParent;

        [SerializeField] public Color32 AvailableColor;
        [SerializeField] public Color32 AvailableMouseOverColor;
        [SerializeField] public Color32 AvailableCantAffordColor;
        [SerializeField] public Color32 UnavailableColor;

        private BuildingData Data;
        private bool PointerOver;
        private bool ShowsCost;

        //  Cost rows in the order the tier authored them, land included. A building's price moves with
        //  its count, so their contents are rewritten on every refresh.
        private readonly List<UIResourceChangeEntry> CostEntries = new();

        public void SetBuildingData(BuildingData data)
        {
            Data = data;
            if (data == null) return;

            if (BuildingTitleText != null) BuildingTitleText.text = data.Name;
            if (BuildingDescriptionText != null) BuildingDescriptionText.text = data.Text;

            //  An explicit BuildingIcon path wins; otherwise the icon is found by the naming
            //  convention of the BuildingIcons folder: BuildingIcon_<NameWithoutSpaces>.
            string spritePath = !string.IsNullOrEmpty(data.BuildingIcon) ? data.BuildingIcon : $"BuildingIcons/BuildingIcon_{data.Name.Replace(" ", "")}";
            Sprite sprite = Resources.Load<Sprite>(spritePath);
            if (sprite != null && BuildingIconImage != null) BuildingIconImage.sprite = sprite;

            // Format effects list
            string effectText = "";
            if (data.AlteredValues != null)
            {
                foreach (var av in data.AlteredValues)
                {
                    string sign = av.Amount >= 0 ? "+" : "";
                    effectText += $"{av.ValueName}: {sign}{av.Amount}\n";
                }
            }
            if (BuildingEffectText != null) BuildingEffectText.text = effectText.Trim();

            // Set the button box color to AvailableColor
            if (BuildingButtonBox != null) BuildingButtonBox.color = AvailableColor;
        }

        public void SetRevealed(bool revealed)
        {
            gameObject.SetActive(revealed);
        }

        //  Binds the button to the running game. Kept apart from SetBuildingData because that also runs
        //  in the editor, where there is no game state to read and nothing to subscribe to.
        public void InitializeRuntime()
        {
            ShowsCost = CanShowCost();

            //  What the next building costs is weighed against the resources held and the land free, and
            //  the count shown against the buildings that stand, so all three drive a refresh.
            GameSubscriptionSystem.SubscribeToGenericResourceAmountChange((string resourceType, ResourceValue amount, ResourceValue maximum) => { RefreshState(); });
            LandSystem.OnLandChanged -= RefreshState;
            LandSystem.OnLandChanged += RefreshState;
            ModifierValueSystem.OnValuesRecomputed -= RefreshState;
            ModifierValueSystem.OnValuesRecomputed += RefreshState;

            RefreshState();
        }

        private void OnDestroy()
        {
            LandSystem.OnLandChanged -= RefreshState;
            ModifierValueSystem.OnValuesRecomputed -= RefreshState;
        }

        public void RefreshState()
        {
            RefreshCount();
            RefreshCostEntries();
            RefreshColor();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Data == null) return;

            BuildingPurchaseSystem.Build(Data.Name);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointerOver = true;
            RefreshColor();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PointerOver = false;
            RefreshColor();
        }

        private void RefreshCount()
        {
            BuildingCountText?.SetText(Localization.DisplayText_BuildingCount(Data == null ? 0 : ModifierValueSystem.GetBuildingCount(Data.Name)));
        }

        //  Three shades of unbuyable, worth keeping apart. A button naming no known building has no price
        //  to read at all; a price above what the resources can ever hold cannot be saved towards, so it
        //  reads as unavailable too; short of that the player merely cannot afford it yet.
        private void RefreshColor()
        {
            if (BuildingButtonBox == null) return;

            if (Data == null) { BuildingButtonBox.SetColor(UnavailableColor); return; }
            if (BuildingPurchaseSystem.IsPermanentlyUnaffordable(Data.Name)) { BuildingButtonBox.SetColor(UnavailableColor); return; }
            if (!BuildingPurchaseSystem.CanBuild(Data.Name)) { BuildingButtonBox.SetColor(AvailableCantAffordColor); return; }

            BuildingButtonBox.SetColor(PointerOver ? AvailableMouseOverColor : AvailableColor);
        }

        //  Cost rows are optional, and a source without the entry component could only ever produce inert
        //  clones, so both are settled once rather than reported on every refresh.
        private bool CanShowCost()
        {
            if (ResourceCostUIPrefab == null || ResourceCostEntryParent == null) return false;

            if (ResourceCostUIPrefab.GetComponent<UIResourceChangeEntry>() == null)
            {
                Debug.LogWarning($"'{name}' cannot show building costs: '{ResourceCostUIPrefab.name}' has no {nameof(UIResourceChangeEntry)}.", this);
                return false;
            }

            return true;
        }

        private void RefreshCostEntries()
        {
            if (Data == null || !ShowsCost) return;

            var changes = BuildingPurchaseSystem.GetNextPurchaseChanges(Data.Name);

            //  Only the amounts move as a building's count climbs through its cost tiers, so the rows
            //  themselves are rebuilt only when the resources being asked for actually change.
            if (!MatchesDisplayedResources(changes)) RebuildCostEntries(changes);

            for (int i = 0; i < CostEntries.Count && i < changes.Count; i++)
            {
                CostEntries[i].SetContent(changes[i].ResourceType, changes[i].ResourceValue);

                //  Each line is coloured for what the player holds of that one resource. Whether the
                //  purchase as a whole is out of reach is the button box's job, so the rows are left to
                //  speak with their text alone.
                //
                //  Land is weighed against what the standing buildings leave free rather than against a
                //  stock in the game state, so the entry cannot work it out for itself.
                if (changes[i].ResourceType == LandSystem.LandValueName)
                    CostEntries[i].ShowAffordability(BuildingPurchaseSystem.GetNextPurchaseLandCost(Data.Name) <= LandSystem.LandFree);
                else
                    CostEntries[i].CheckCanChange(overrideNoBG: true, greenEvenNegative: true);
            }
        }

        private bool MatchesDisplayedResources(List<ResourceAmountData> changes)
        {
            if (CostEntries.Count != changes.Count) return false;

            for (int i = 0; i < changes.Count; i++)
                if (CostEntries[i] == null || CostEntries[i].GetResourceAmount().ResourceType != changes[i].ResourceType) return false;

            return true;
        }

        private void RebuildCostEntries(List<ResourceAmountData> changes)
        {
            foreach (Transform child in ResourceCostEntryParent)
                Destroy(child.gameObject);
            CostEntries.Clear();

            foreach (var change in changes)
            {
                var entryObject = Instantiate(ResourceCostUIPrefab, ResourceCostEntryParent);

                //  Entry prefabs carry an authored depth from the screen space camera canvas they were
                //  built in. Stacked onto the depth of this panel it puts the row behind the camera, so
                //  rows are pinned to the plane of the button. The layout group still places x and y.
                entryObject.transform.localPosition = Vector3.zero;

                var entryComponent = entryObject.GetComponent<UIResourceChangeEntry>();
                entryComponent.SetContent(change.ResourceType, change.ResourceValue);
                CostEntries.Add(entryComponent);
            }
        }
    }
}
