using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;
using System.Linq;

namespace HypnicEmpire
{
    public class UIView_MainGame : MonoBehaviour
    {
        private const string DiscordURL = "";
        private const string RedditURL = "";
        private const string ItchIoURL = "";

        [Header("UI Element Prefabs")]
        [SerializeField] public GameObject UIResourceChangeEntryPrefab;

        [Header("Primary Menu UI Buttons")]
        [SerializeField] public Button ExitButton;
        [SerializeField] public Button OptionsButton;
        [SerializeField] public Button AchievementsButton;
        [SerializeField] public Button ActionsButton;
        [SerializeField] public Button DevelopmentsButton;
        [SerializeField] public Button DiscordButton;
        [SerializeField] public Button RedditButton;
        [SerializeField] public Button ItchIoButton;

        [Header("Primary Center Menus")]
        [SerializeField] public GameObject ExitMenu;
        [SerializeField] public GameObject OptionsMenu;
        [SerializeField] public GameObject AchievementsMenu;
        [SerializeField] public GameObject ActionsMenu;
        [SerializeField] public GameObject DevelopmentsMenu;
        [SerializeField] public UIResourceListMenu ResourceListControl;
        
        [Header("UI List Display Parents")]
        [SerializeField] public UIJournalMenu JournalMenuControl;
        [SerializeField] public UIDevelopmentsMenu DevelopmentsMenuControl;
        [SerializeField] public Transform DelveResourceLossParent;
        [SerializeField] public Transform DelveResourceGainParent;

        [Header("Secondary Menu UI Elements")]
        [SerializeField] public Button SaveAndExitButton;
        [SerializeField] public UINumberOptionControlEntry MasterVolumeControlEntry;
        [SerializeField] public UINumberOptionControlEntry SFXVolumeControlEntry;
        [SerializeField] public UINumberOptionControlEntry MusicVolumeControlEntry;
        [SerializeField] public UIRadioButtonControlEntry ActionSoundExcessControlEntry;
        [SerializeField] public UIRadioButtonControlEntry FullscreenControlEntry;
        [SerializeField] public UIRadioButtonControlEntry WindowBorderControlEntry;
        [SerializeField] public Button SaveButton;
        [SerializeField] public Button LoadButton;
        [SerializeField] public Button HardResetButton;
        [SerializeField] public Button HardResetConfirmButton;
        [SerializeField] public Button HardResetCancelButton;

        [Header("Primary Game Related UI Elements")]
        [SerializeField] public UITaskProcessButton DelveTaskButton;
        [SerializeField] public UIMissionDataDisplay MissionDataDisplay;
        [SerializeField] public UILevelExplorationBar LevelExplorationBar;

        [Header("Secondary Game Related UI Elements")]
        [SerializeField] public GameObject[] DevelopmentsTabGroup;
        [SerializeField] public GameObject[] BuildingsTabGroup;

        //  Collections of elements to use in menu functionality
        private List<Button> CenterMenuButtons => new() { ExitButton, OptionsButton, AchievementsButton, ActionsButton, DevelopmentsButton };
        private List<GameObject> Menus => new() { ExitMenu, OptionsMenu, AchievementsMenu, ActionsMenu, DevelopmentsMenu };

        //  Button Actions
        public Action SaveAndExitButtonAction;
        public Action SaveButtonAction;
        public Action LoadButtonAction;
        public Action HardResetButtonAction;
        public Action ToggleFullscreenButtonAction;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public void Initialize()
        {
            ExitButton?.onClick.AddListener(() => { ShowCenterMenu(ExitButton, ExitMenu); });
            OptionsButton?.onClick.AddListener(() => { ShowCenterMenu(OptionsButton, OptionsMenu); });
            AchievementsButton?.onClick.AddListener(() => { ShowCenterMenu(AchievementsButton, AchievementsMenu); });
            ActionsButton?.onClick.AddListener(() => { ShowCenterMenu(ActionsButton, ActionsMenu); GameController.CurrentGameState.Click(); });
            DevelopmentsButton?.onClick.AddListener(() => { ShowCenterMenu(DevelopmentsButton, DevelopmentsMenu); });

            DiscordButton?.onClick.AddListener(() => { Application.OpenURL(DiscordURL); });
            RedditButton?.onClick.AddListener(() => { Application.OpenURL(RedditURL); });
            ItchIoButton?.onClick.AddListener(() => { Application.OpenURL(ItchIoURL); });

            SaveAndExitButton?.onClick.AddListener(() => { SaveAndExitButtonAction?.Invoke(); });
            SaveButton?.onClick.AddListener(() => { SaveButtonAction?.Invoke(); });
            LoadButton?.onClick.AddListener(() => { LoadButtonAction?.Invoke(); });
            HardResetConfirmButton?.onClick.AddListener(() => { HardResetButtonAction?.Invoke(); });
            FullscreenControlEntry?.AddListener(() => { ToggleFullscreenButtonAction?.Invoke(); });

            HardResetButton?.onClick.AddListener(() => { SetResetButtonUnpacked(true); });
            HardResetCancelButton?.onClick.AddListener(() => { SetResetButtonUnpacked(false); });

            //  Define UI responses to game unlock events
            GameUnlockSystem.AddGameUnlockAction("Unlock_Empty_Belly", (bool shown) => { foreach (var obj in DevelopmentsTabGroup) obj?.SetActive(shown); });
            GameUnlockSystem.AddGameUnlockAction("Unlock_Buildings", (bool shown) => { foreach (var obj in BuildingsTabGroup) obj?.SetActive(shown); });

            foreach (var gu in GameUnlockSystem.UnlockIDs)
            {
                string unlockedResource = ResourceTypeSystem.GetResourceTypeFromUnlock(gu);
                if (unlockedResource != null)
                    GameUnlockSystem.AddGameUnlockAction(gu.ToString(), (bool shown) => { if (shown) ResourceListControl?.AddResourceEntry(unlockedResource); }); 
            }

            //  Define UI responses to resource changes
            GameSubscriptionSystem.SubscribeToGenericResourceAmountChange((string resourceType, int amount, int maxAmount) => {
                if (amount > 0)
                    ResourceListControl?.AddResourceEntry(resourceType);
            });
        }

        public void ResetUI()
        {
            DelveTaskButton?.Reset();

            ResetDevelopmentMenu();
            ResourceListControl?.ClearAllResourceEntries();

            SetResetButtonUnpacked(false);
            ShowCenterMenu(ActionsButton, ActionsMenu);
        }

        public void SetDelveResourceChange(List<ResourceAmountData> amountList)
        {
            DelveTaskButton?.SetEnabled(amountList.CheckCanChangeAll());

            ClearDelveResourceChanges();
            foreach (var ra in amountList)
                AddDelveResourceChange(ra.ResourceType, ra.Amount);
        }

        void ShowCenterMenu(Button button, GameObject menuToShow)
        {
            foreach (var btn in CenterMenuButtons)
                btn?.SetInteractable(btn != button);

            foreach (var menu in Menus)
                menu?.SetActive(menu == menuToShow);
        }

        public void SetResetButtonUnpacked(bool unpacked)
        {
            HardResetButton?.SetInteractable(!unpacked);
            HardResetConfirmButton?.gameObject.SetActive(unpacked);
            HardResetCancelButton?.gameObject.SetActive(unpacked);
        }

        public void ClearDelveResourceChanges()
        {
            if (DelveResourceLossParent != null)
                foreach (Transform child in DelveResourceLossParent)
                    Destroy(child.gameObject);

            if (DelveResourceGainParent != null)
                foreach (Transform child in DelveResourceGainParent)
                    Destroy(child.gameObject);
        }

        public void AddDelveResourceChange(string resourceType, int resourceChange)
        {
            if (DelveResourceLossParent != null && DelveResourceGainParent != null && UIResourceChangeEntryPrefab != null)
            {
                var entry = Instantiate(UIResourceChangeEntryPrefab, (resourceChange < 0) ? DelveResourceLossParent : DelveResourceGainParent);
                entry.GetComponent<UIResourceChangeEntry>().SetContent(resourceType, resourceChange);
            }
            
        }

        public void AddOpenDevelopment(string name, string description, string extraInfo, List<ResourceAmountData> cost, List<string> unlock)
        {
            DevelopmentsMenuControl?.AddOpenDevelopment(name, description, extraInfo, cost, unlock);
        }

        public void ResetDevelopmentMenu()
        {
            DevelopmentsMenuControl?.ClearDevelopmentMenu();
        }
    }
}