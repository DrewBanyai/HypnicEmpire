using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BreakInfinity;
using TMPro;
using System;

namespace HypnicEmpire
{
    public class UIView_MainGame : MonoBehaviour
    {
        private const string DiscordURL = "";
        private const string RedditURL = "";
        private const string ItchIoURL = "";

        [Header("UI Element Prefabs")]
        [SerializeField] public GameObject UIJournalEntryPrefab;
        [SerializeField] public GameObject UIDevelopmentEntryPrefab;
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
        [SerializeField] public ResourceListContainer ResourceList;
        
        [Header("UI List Display Parents")]
        [SerializeField] public Transform ResourceDisplayParent;
        [SerializeField] public Transform JournalDisplayParent;
        [SerializeField] public Transform OpenDevelopmentDisplayParent;
        [SerializeField] public Transform FinishedDevelopmentDisplayParent;
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

        [Header("Secondary Game Related UI Elements")]
        [SerializeField] public GameObject[] DevelopmentsTabGroup;

        //  Collections of elements to use in menu functionality
        private List<Button> CenterMenuButtons => new() { ExitButton, OptionsButton, AchievementsButton, ActionsButton, DevelopmentsButton };
        private List<GameObject> Menus => new() { ExitMenu, OptionsMenu, AchievementsMenu, ActionsMenu, DevelopmentsMenu };

        //  Button Actions
        public Action SaveAndExitButtonAction;
        public Action SaveButtonAction;
        public Action LoadButtonAction;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (ExitButton != null) ExitButton.onClick.AddListener(() => { ShowCenterMenu(ExitButton, ExitMenu); });
            if (OptionsButton != null) OptionsButton.onClick.AddListener(() => { ShowCenterMenu(OptionsButton, OptionsMenu); });
            if (AchievementsButton != null) AchievementsButton.onClick.AddListener(() => { ShowCenterMenu(AchievementsButton, AchievementsMenu); });
            if (ActionsButton != null) ActionsButton.onClick.AddListener(() => { ShowCenterMenu(ActionsButton, ActionsMenu); });
            if (DevelopmentsButton != null) DevelopmentsButton.onClick.AddListener(() => { ShowCenterMenu(DevelopmentsButton, DevelopmentsMenu); });

            if (DiscordButton != null) DiscordButton.onClick.AddListener(() => { Application.OpenURL(DiscordURL); });
            if (RedditButton != null) RedditButton.onClick.AddListener(() => { Application.OpenURL(RedditURL); });
            if (ItchIoButton != null) ItchIoButton.onClick.AddListener(() => { Application.OpenURL(ItchIoURL); });

            if (SaveAndExitButton != null) SaveAndExitButton.onClick.AddListener(() => { SaveAndExitButtonAction?.Invoke(); });
            if (SaveButton != null) SaveButton.onClick.AddListener(() => { SaveButtonAction?.Invoke(); });
            if (LoadButton != null) LoadButton.onClick.AddListener(() => { LoadButtonAction?.Invoke(); });
        }

        public void ResetUI()
        {
            if (ResourceList != null)
            {
                ResourceList.ClearAllResourceEntries();
                AddResourceEntry("Food");
            }

            if (DelveTaskButton != null) DelveTaskButton.Reset();

            SetResetButtonUnpacked(false);
            ShowCenterMenu(ActionsButton, ActionsMenu);
        }

        public void SetDelveResourceChange(ResourceAmountCollection resourceChangeAmounts)
        {
            ClearDelveResourceChanges();

            foreach (var rca in resourceChangeAmounts.ResourceAmounts)
                AddDelveResourceChange(rca.ResourceType.ToString(), rca.Amount);
        }

        void ShowCenterMenu(Button button, GameObject menuToShow)
        {
            foreach (var btn in CenterMenuButtons)
            {
                if (btn != null)
                    btn.interactable = btn != button;
            }

            foreach (var menu in Menus)
            {
                if (menu != null)
                    menu.SetActive(menu == menuToShow);
            }
        }

        public void SetDevelopmentsTabGroupHidden(bool hidden)
        {
            foreach (var obj in DevelopmentsTabGroup)
            {
                if (obj != null)
                    obj.SetActive(!hidden);
            }
        }

        public void SetResetButtonUnpacked(bool unpacked)
        {
            if (HardResetButton == null) return;
            if (HardResetConfirmButton == null) return;
            if (HardResetCancelButton == null) return;

            HardResetButton.interactable = !unpacked;
            HardResetConfirmButton.gameObject.SetActive(unpacked);
            HardResetCancelButton.gameObject.SetActive(unpacked);
        }

        public void AddResourceEntry(string resourceName)
        {
            if (ResourceList != null)
                ResourceList.AddResourceEntry(resourceName);
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

        public void AddDelveResourceChange(string resourceName, int resourceChange)
        {
            if (DelveResourceLossParent != null && DelveResourceGainParent != null && UIResourceChangeEntryPrefab != null)
            {
                var entry = Instantiate(UIResourceChangeEntryPrefab, (resourceChange < 0) ? DelveResourceLossParent : DelveResourceGainParent);
                entry.GetComponent<UIResourceChangeEntry>().SetContent(resourceName, resourceChange);
            }
            
        }

        public void AddJournalEntry(string text)
        {
            if (UIJournalEntryPrefab != null && JournalDisplayParent != null)
            {
                var entryObject = Instantiate(UIJournalEntryPrefab, JournalDisplayParent);
                var entryComponent = entryObject.GetComponent<UIJournalEntry>();
                if (entryComponent != null)
                    entryComponent.SetJournalEntryText(text);
            }
        }

        public void AddOpenDevelopment(string name, string description, string extraInfo)
        {
            SetDevelopmentsTabGroupHidden(false);
            if (UIDevelopmentEntryPrefab != null && DevelopmentsMenu != null && OpenDevelopmentDisplayParent != null)
            {
                var entryObject = Instantiate(UIDevelopmentEntryPrefab, OpenDevelopmentDisplayParent);
                var entryComponent = entryObject.GetComponent<UIDevelopmentEntry>();
                if (entryComponent != null)
                    entryComponent.SetContent(name, description, extraInfo);
            }
        }

        public void ResetDevelopmentMenu()
        {
            SetDevelopmentsTabGroupHidden(true);
            if (OpenDevelopmentDisplayParent != null)
                foreach (Transform child in OpenDevelopmentDisplayParent)
                    Destroy(child.gameObject);
        }
    }
}