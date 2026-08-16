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

        [Header("Primary Menu UI Buttons")]
        [SerializeField] public Button ExitButton;
        [SerializeField] public Button OptionsButton;
        [SerializeField] public Button AchievementsButton;
        [SerializeField] public Button ActionsButton;
        [SerializeField] public Button DevelopmentsButton;
        [SerializeField] public Button BuildingsButton;
        [SerializeField] public Button WarfareButton;
        [SerializeField] public Button DiscordButton;
        [SerializeField] public Button RedditButton;
        [SerializeField] public Button ItchIoButton;

        [Header("Primary Center Menus")]
        [SerializeField] public GameObject ExitMenu;
        [SerializeField] public GameObject OptionsMenu;
        [SerializeField] public GameObject AchievementsMenu;
        [SerializeField] public GameObject ActionsMenu;
        [SerializeField] public GameObject DevelopmentsMenu;
        [SerializeField] public GameObject BuildingsMenu;
        [SerializeField] public GameObject WarfareMenu;
        [SerializeField] public UIResourceListMenu ResourceListControl;
        
        [Header("UI List Display Parents")]
        [SerializeField] public UIJournalMenu JournalMenuControl;
        [SerializeField] public UIDevelopmentsMenu DevelopmentsMenuControl;
        [SerializeField] public UIBuildingsMenu BuildingsMenuControl;

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
        [SerializeField] public GameObject[] ProjectsTabGroup;
        [SerializeField] public GameObject[] WarfareTabGroup;

        //  Collections of elements to use in menu functionality
        [SerializeField] public List<Button> CenterMenuButtons;
        [SerializeField] public List<GameObject> Menus;

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
            BuildingsButton?.onClick.AddListener(() => { ShowCenterMenu(BuildingsButton, BuildingsMenu); });
            WarfareButton?.onClick.AddListener(() => { ShowCenterMenu(WarfareButton, WarfareMenu); });

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

            //  Define UI responses to game unlock events.
            //  Registered unlock actions fire only WHEN an unlock is set — they never apply an initial
            //  state — so both tab groups must start HIDDEN explicitly, otherwise their scene-default
            //  (active) leaves them visible at startup.
            foreach (var obj in DevelopmentsTabGroup) obj?.SetActive(false);
            foreach (var obj in BuildingsTabGroup)    obj?.SetActive(false);
            if (ProjectsTabGroup != null) foreach (var obj in ProjectsTabGroup) obj?.SetActive(false);
            if (WarfareTabGroup != null) foreach (var obj in WarfareTabGroup) obj?.SetActive(false);

            //  Developments tab: revealed once the first purchasable development is added to the menu
            //  (see AddOpenDevelopment). The earliest development triggers on Unlock_Empty_Belly, so the
            //  tab appears only AFTER Empty_Belly AND a development is actually available — never before.

            //  Buildings tab: revealed when the barony is visited (Unlock_Visit_The_Barony).
            GameUnlockSystem.AddGameUnlockAction("Unlock_Visit_The_Barony", true, (bool shown) => { foreach (var obj in BuildingsTabGroup) obj?.SetActive(shown); });

            //  Projects tab: revealed when the first project (Arcane Streetlamps) unlocks.
            GameUnlockSystem.AddGameUnlockAction("Unlock_Project_Arcane_Streetlamps", true, (bool shown) => { if (ProjectsTabGroup != null) foreach (var obj in ProjectsTabGroup) obj?.SetActive(shown); });

            //  Warfare tab: revealed when warfare unlocks (Unlock_Warfare).
            GameUnlockSystem.AddGameUnlockAction("Unlock_Warfare", true, (bool shown) => { if (WarfareTabGroup != null) foreach (var obj in WarfareTabGroup) obj?.SetActive(shown); });

            foreach (var gu in GameUnlockSystem.UnlockIDs)
            {
                string unlockedResource = ResourceTypeSystem.GetResourceTypeFromUnlock(gu);
                if (unlockedResource != null)
                    GameUnlockSystem.AddGameUnlockAction(gu.ToString(), true, (bool shown) => { if (shown) ResourceListControl?.AddResourceEntry(unlockedResource); }); 
            }

            //  Define UI responses to resource changes
            GameSubscriptionSystem.SubscribeToGenericResourceAmountChange((string resourceType, ResourceValue amount, ResourceValue maxAmount) => {
                if (amount > 0)
                    ResourceListControl?.AddResourceEntry(resourceType);
            });

            BuildingsMenuControl?.InitializeMenu();
        }

        public void ResetUI()
        {
            DelveTaskButton?.Reset();

            ResetDevelopmentMenu();
            ResourceListControl?.ClearAllResourceEntries();
            BuildingsMenuControl?.ApplyRevealState();

            SetResetButtonUnpacked(false);
            ShowCenterMenu(ActionsButton, ActionsMenu);
        }

        void ShowCenterMenu(Button button, GameObject menuToShow)
        {
            foreach (var btn in CenterMenuButtons)
                btn?.SetInteractable(btn != button);

            foreach (var menu in Menus)
                menu.SetScaleY(menu == menuToShow ? 1f : 0f);
        }

        public void SetResetButtonUnpacked(bool unpacked)
        {
            HardResetButton?.SetInteractable(!unpacked);
            HardResetConfirmButton?.gameObject.SetActive(unpacked);
            HardResetCancelButton?.gameObject.SetActive(unpacked);
        }

        public void AddOpenDevelopment(string name, string description, string extraInfo, List<ResourceAmountData> cost, List<string> unlock)
        {
            DevelopmentsMenuControl?.AddOpenDevelopment(name, description, extraInfo, cost, unlock);
            //  A purchasable development now exists — reveal the Developments tab (idempotent; also
            //  re-shows it on load, when saved unlocks are replayed and open developments are re-added).
            foreach (var obj in DevelopmentsTabGroup) obj?.SetActive(true);
        }

        public void ResetDevelopmentMenu()
        {
            DevelopmentsMenuControl?.ClearDevelopmentMenu();
        }
    }
}