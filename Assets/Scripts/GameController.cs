using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BasicAppUtility;

namespace HypnicEmpire
{
    public class GameController : MonoBehaviour
    {
        private static GameController Instance;
        private static string SaveFilePath => Application.persistentDataPath + "/saveGame.dat";

        [SerializeField, Min(0f)]
        [Tooltip("Multiplies positive resource rewards. Resource costs are unaffected.")]
        private float resourceRewardMultiplier = 1f;

        public static double CurrentResourceRewardMultiplier =>
            Instance == null ? 1d : Mathf.Max(0f, Instance.resourceRewardMultiplier);

        [SerializeField] public GameStateScriptableObject InitialGameState;

        public static GameState CurrentGameState = new();
        public UIView_MainGame MainGameUIView;

        private UIActionMenuController ActionsMenuController =>
            MainGameUIView?.ActionsMenu?.GetComponent<UIActionMenuController>();

        private void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            Time.timeScale = 5f;

            GameUnlockSystem.LoadAllUnlockIDs(Application.dataPath + "/GameData/UnlockIDs.json");
            AchievementsSystem.LoadAllAchievementsData(Application.dataPath + "/GameData/Achievements.json");
            JournalEntrySystem.LoadAllJournalEntries(Application.dataPath + "/GameData/JournalEntries.json");
            ResourceTypeSystem.LoadAllResourceTypes(Application.dataPath + "/GameData/Resources.json");
            LevelDataSystem.LoadAllLevelData(Application.dataPath + "/GameData/LevelData.json");
            AlterableValueSystem.LoadAllAlterableValues(Application.dataPath + "/GameData/AlterableValues.json");
            DevelopmentSystem.LoadAllDevelopments(Application.dataPath + "/GameData/Developments.json");
            TaskActionSystem.LoadAllTaskActions(Application.dataPath + "/GameData/TaskActions.json");
            BuildingDataSystem.LoadAllBuildingsData(Application.dataPath + "/GameData/Buildings.json");

            CurrentGameState.Initialize(InitialGameState);

            // Make building/project "modifier" effects live: seeds building counts, accumulates
            // AlteredValues into modifier AlterableValues, and refreshes resource maxima. Must run
            // after AlterableValues + Buildings load and after the game state exists.
            ModifierValueSystem.Initialize();

            //  Used land is derived from building counts, so land initializes once those are seeded.
            LandSystem.Initialize();

            MainGameUIView.Initialize();
            
            JournalEntrySystem.OnJournalEntryAdded += (string text) => MainGameUIView?.JournalMenuControl?.AddJournalEntry(text);

            SetupMainGameUI();
        }

        public void Update()
        {
            SaveUtility.Update();
            TaskActionSystem.Update();
        }

        private void ChangeMasterVolume(int delta) {
            CurrentGameState.MasterVolume = Mathf.Clamp(CurrentGameState.MasterVolume + delta, 0, 100);
            MainGameUIView.MasterVolumeControlEntry?.SetDisplayDetails("Master", CurrentGameState.MasterVolume.ToString(), CurrentGameState.MasterVolume != 100, CurrentGameState.MasterVolume != 0);
        }

        private void ChangeSFXVolume(int delta)
        {
            CurrentGameState.SFXVolume = Mathf.Clamp(CurrentGameState.SFXVolume + delta, 0, 100);
            MainGameUIView.SFXVolumeControlEntry.SetDisplayDetails("Soundeffects", CurrentGameState.SFXVolume.ToString(), CurrentGameState.SFXVolume != 100, CurrentGameState.SFXVolume != 0);
        }
        
        private void ChangeMusicVolume(int delta)
        {
            CurrentGameState.MusicVolume = Mathf.Clamp(CurrentGameState.MusicVolume + delta, 0, 100);
            MainGameUIView.MusicVolumeControlEntry.SetDisplayDetails("Music", CurrentGameState.MusicVolume.ToString(), CurrentGameState.MusicVolume != 100, CurrentGameState.MusicVolume != 0);
        }

        private void SetupMainGameUI()
        {
            if (MainGameUIView == null) { Debug.LogError($"Failed to Setup Main Game UI: MainGameUIView is null!"); return; }

            //  Assign button actions from the Main Game UI View controller
            MainGameUIView.SaveAndExitButtonAction = SaveAndExitGame;
            MainGameUIView.SaveButtonAction = SaveGame;
            MainGameUIView.LoadButtonAction = LoadGame;
            MainGameUIView.HardResetButtonAction = ResetGame;
            MainGameUIView.ToggleFullscreenButtonAction = ToggleFullscreen;

            //  Define out the sound volume control entries
            MainGameUIView.MasterVolumeControlEntry?.SetContent("Master", CurrentGameState.MasterVolume.ToString(), () => ChangeMasterVolume(5), () => ChangeMasterVolume(-5));
            MainGameUIView.SFXVolumeControlEntry?.SetContent("Soundeffects", CurrentGameState.SFXVolume.ToString(), () => ChangeSFXVolume(5), () => ChangeSFXVolume(-5));
            MainGameUIView.MusicVolumeControlEntry?.SetContent("Music", CurrentGameState.MusicVolume.ToString(), () => ChangeMusicVolume(5), () => ChangeMusicVolume(-5));
            MainGameUIView.ActionSoundExcessControlEntry?.AddListener(CurrentGameState.ToggleActionSoundExcess);

            MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent.Value, CurrentGameState.LevelReached.Value, CurrentGameState.LevelCurrent.Value, CurrentLevelUp, CurrentLevelDown);

            MainGameUIView.LevelExplorationBar?.SetProgress((float)CurrentGameState.LevelDelveCount.Value / (float)LevelDataSystem.GetLevelData(CurrentGameState.LevelCurrent.Value).DelveCount);
            CurrentGameState.LevelDelveCount.Subscribe((newValue) =>
            {
                MainGameUIView.LevelExplorationBar?.SetProgress((float)CurrentGameState.LevelDelveCount.Value / (float)LevelDataSystem.GetLevelData(CurrentGameState.LevelCurrent.Value).DelveCount);
            });

            CurrentGameState.LevelCurrent.Subscribe((newValue) =>
            {
                MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent.Value, CurrentGameState.LevelReached.Value, CurrentGameState.LevelCurrent.Value, CurrentLevelUp, CurrentLevelDown);
            });

            CurrentGameState.LevelReached.Subscribe((newValue) =>
            {
                MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent.Value, CurrentGameState.LevelReached.Value, CurrentGameState.LevelCurrent.Value, CurrentLevelUp, CurrentLevelDown);
            });

            foreach (var development in DevelopmentSystem.DevelopmentEntries)
            {
                foreach (var trigger in development.Trigger)
                {
                    GameUnlockSystem.AddGameUnlockAction(trigger, true, (bool unlocked) =>
                    {
                        if (!unlocked) return;
                        
                        if (GameUnlockSystem.IsUnlocked(trigger))
                            return;
                            
                        List<string> listMinusTrigger = development.Trigger.Where(t => t != trigger).Select(t => t).ToList();
                        foreach (var t in listMinusTrigger)
                            if (!GameUnlockSystem.IsUnlocked(t))
                                return;

                        MainGameUIView.AddOpenDevelopment(development.Title, development.Description, development.EffectText, development.Cost, development.Unlock);
                    });
                }
            }

            SaveUtility.SaveCallback = () => { SaveGame(); };

            //  Now initialize the UI
            InitializeUnlockListeners();
            PostLoadInitialState();
        }

        //  Subscribing to unlocks is a first-run concern, not a per-load one: every listener below is driven by
        //  static game data and lives for the whole session, so registering them again on load or reset would
        //  simply stack a second set and make each unlock fire its listeners twice. They are registered before
        //  any unlock is applied so that nothing set during the first PostLoadInitialState is missed.
        private void InitializeUnlockListeners()
        {
            ActionsMenuController?.InitializeMenu();
            JournalEntrySystem.InitializeListeners();
            AchievementsSystem.InitializeListeners();
        }

        //  Re-applies state, and only state: everything here must be safe to run repeatedly, because a load or
        //  a hard reset replaces the game state mid-session and the UI has to be brought back in line with it.
        //  Registering listeners belongs in InitializeUnlockListeners instead.
        public void PostLoadInitialState()
        {
            //  Loading or resetting replaces the acquired land count, so land is brought back in line
            //  before the UI reads it.
            LandSystem.Refresh();

            MainGameUIView.ResetUI();

            JournalEntrySystem.ShownJournalEntries.Clear();
            MainGameUIView.JournalMenuControl?.ClearJournalEntries();
            foreach (string journalEntry in CurrentGameState.JournalEntries)
            {
                JournalEntrySystem.ShownJournalEntries.Add(journalEntry);
                MainGameUIView.JournalMenuControl?.AddJournalEntry(journalEntry);
            }

            ActionsMenuController?.RefreshMenu();
            MainGameUIView.DelveTaskButton?.SetContents("Delve", 20f, 64f, CompleteDelve);

            CheckDevelopments();

            //  If we haven't loaded a game state with the very first unlock, unlock it now
            if (!GameUnlockSystem.IsUnlocked("Unlock_Game_Start"))
                GameUnlockSystem.SetUnlockValue("Unlock_Game_Start", true);

            //  Link Achievement UI
            var achievementsCollection = MainGameUIView?.AchievementsMenu?.GetComponentInChildren<AchievementsCollection>();
            achievementsCollection?.LinkAchievementUI();
        }

        public void CheckDevelopments()
        {
            foreach (var development in DevelopmentSystem.DevelopmentEntries)
            {
                if (development.Trigger.Any(t => !GameUnlockSystem.IsUnlocked(t)))
                    continue;

                if (development.Unlock.Any(u => GameUnlockSystem.IsUnlocked(u)))
                    continue;

                MainGameUIView.AddOpenDevelopment(development.Title, development.Description, development.EffectText, development.Cost, development.Unlock);
            }
        }

        public void CurrentLevelUp()
        {
            SetCurrentLevel(CurrentGameState.LevelCurrent.Value + 1);
            MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent.Value, CurrentGameState.LevelReached.Value, CurrentGameState.LevelCurrent.Value, CurrentLevelUp, CurrentLevelDown);
        }

        public void CurrentLevelDown()
        {
            SetCurrentLevel(CurrentGameState.LevelCurrent.Value - 1);
            MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent.Value, CurrentGameState.LevelReached.Value, CurrentGameState.LevelCurrent.Value, CurrentLevelUp, CurrentLevelDown);
        }

        public void SetCurrentLevel(int level)
        {
            if (level < 0) return;
            if (level > CurrentGameState.LevelReached.Value) return;
            CurrentGameState.LevelCurrent.SetValue(level);
        }

        public void CompleteDelve()
        {
            //  Lose and gain all resources assigned to this level of the game, unlocking resources as needed
            var changes = GetCurrentDelveResourceChanges();
            CurrentGameState.AddToResources(changes);

            if (CurrentGameState.LevelCurrent.Value + 1 >= LevelDataSystem.GetLevelCount())
            {
                MainGameUIView?.DelveTaskButton.SetEnabled(false);
            }
            else
            {
                if (CurrentGameState.LevelDelveCount.Value + 1 >= LevelDataSystem.GetLevelData(CurrentGameState.LevelCurrent.Value)?.DelveCount)
                {
                    CurrentGameState.LevelReached.SetValue(CurrentGameState.LevelReached.Value + 1);
                    CurrentGameState.LevelDelveCount.SetValue(0);
                    CurrentGameState.LevelCurrent.SetValue(CurrentGameState.LevelReached.Value);
                }
                else
                {
                    CurrentGameState.LevelDelveCount.SetValue(CurrentGameState.LevelDelveCount.Value + 1);
                }
            }
        }

        public void SaveAndExitGame()
        {
            SaveGame();
            BasicAppUtilities.ExitApplication();
        }

        public void SaveGame()
        {
            GameSaveData saveData = new GameSaveData { 
                GameState = CurrentGameState, 
                GameUnlockList = GameUnlockSystem.GameUnlockList,
                BuildingCounts = ModifierValueSystem.GetAllBuildingCounts(),
                UnlockedAchievements = AchievementsSystem.UnlockedAchievements
            };
            File.WriteAllText(SaveFilePath, JsonSerialization.Serialize(saveData));
        }

        public void LoadGame()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.LogWarning($"Save file not found at {SaveFilePath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning($"Save file at {SaveFilePath} is empty");
                    return;
                }

                var saveData = JsonSerialization.Deserialize<GameSaveData>(json);
                if (!TryValidateSaveData(saveData, out string validationError))
                {
                    Debug.LogError($"Save file at {SaveFilePath} is invalid: {validationError}");
                    return;
                }

                CurrentGameState.CopyGameState(saveData.GameState);
                GameUnlockSystem.GameUnlockList = saveData.GameUnlockList;
                AchievementsSystem.UnlockedAchievements = saveData.UnlockedAchievements ?? new();

                // Building counts live outside GameState; restore them so Recompute refreshes
                // land usage, modifier values, and resource maxima before PostLoadInitialState.
                if (saveData.BuildingCounts != null)
                {
                    foreach (var entry in saveData.BuildingCounts)
                        ModifierValueSystem.SetBuildingCount(entry.Key, entry.Value);
                }

                PostLoadInitialState();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading save from {SaveFilePath}: {e.Message}");
            }
        }

        private static bool TryValidateSaveData(GameSaveData saveData, out string error)
        {
            if (saveData == null)
            {
                error = "deserialized save data is null";
                return false;
            }

            if (saveData.GameState == null)
            {
                error = "GameState is missing";
                return false;
            }

            if (saveData.GameUnlockList == null)
            {
                error = "GameUnlockList is missing";
                return false;
            }

            error = null;
            return true;
        }

        public void ResetGame()
        {
            if (InitialGameState == null) return;

            //  Set the game state to the Initial Game State, then immediately replace the existing save file with the new state
            CurrentGameState.Initialize(InitialGameState);

            //  Building counts live in the modifier system rather than the game state, so they have to be put
            //  back to their starting counts alongside it: land used is derived from them and would otherwise
            //  outlive the acquired land that paid for it. The recompute this triggers refreshes land in turn.
            ModifierValueSystem.Reset();

            SaveGame();

            PostLoadInitialState();
        }

        public void ToggleFullscreen()
        {
            BasicAppUtilities.SetWindowFullscreen(CurrentGameState.Fullscreen = !CurrentGameState.Fullscreen);
        }

        public List<ResourceAmountData> GetCurrentDelveResourceChanges()
        {
            if (CurrentGameState.LevelCurrent.Value >= LevelDataSystem.GetLevelCount() || CurrentGameState.LevelCurrent.Value < 0) return new List<ResourceAmountData>();

            List<ResourceAmountData> amountsList = new();
            foreach (var rc in LevelDataSystem.GetGroupingByLevel(CurrentGameState.LevelCurrent.Value).LevelResourceChange)
                amountsList.AddResourceAmount(new ResourceAmountData(rc.ResourceType, rc.ResourceValue));

            return amountsList;
        }
    }
}