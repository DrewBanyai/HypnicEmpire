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

        private const string DelveActionName = "Delve";

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
            GameUnlockSystem.LoadAllUnlockIDs(Application.dataPath + "/GameData/UnlockIDs.json");
            AchievementsSystem.LoadAllAchievementsData(Application.dataPath + "/GameData/Achievements.json");
            JournalEntrySystem.LoadAllJournalEntries(Application.dataPath + "/GameData/JournalEntries.json");
            ResourceTypeSystem.LoadAllResourceTypes(Application.dataPath + "/GameData/Resources.json");
            LevelDataSystem.LoadAllLevelData(Application.dataPath + "/GameData/LevelData.json");
            AlterableValueSystem.LoadAllAlterableValues(Application.dataPath + "/GameData/AlterableValues.json");
            DevelopmentSystem.LoadAllDevelopments(Application.dataPath + "/GameData/Developments.json");
            if (!ActionTimingSystem.LoadConfiguration(Application.dataPath + "/GameData/ActionTiming.json"))
            {
                enabled = false;
                return;
            }
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

            void UpdateLevelExplorationBar()
            {
                var levelData = LevelDataSystem.GetLevelData(CurrentGameState.LevelCurrent.Value);
                if (levelData == null) return;
                MainGameUIView.LevelExplorationBar?.SetProgress((float)CurrentGameState.LevelDelveCount.Value / (float)levelData.DelveCount);
            }

            UpdateLevelExplorationBar();
            CurrentGameState.LevelDelveCount.Subscribe((newValue) =>
            {
                UpdateLevelExplorationBar();
            });

            CurrentGameState.LevelCurrent.Subscribe((newValue) =>
            {
                MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent.Value, CurrentGameState.LevelReached.Value, CurrentGameState.LevelCurrent.Value, CurrentLevelUp, CurrentLevelDown);
                RefreshDelveResourceChange();
            });

            CurrentGameState.LevelReached.Subscribe((newValue) =>
            {
                MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent.Value, CurrentGameState.LevelReached.Value, CurrentGameState.LevelCurrent.Value, CurrentLevelUp, CurrentLevelDown);
            });

            //  The state the game starts on was set before anything was listening for it, so the opening
            //  price is taken directly rather than waited on.
            RefreshDelveResourceChange();

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

            //  Likewise the lifetime resource totals: the state they were earned under has just been swapped
            //  out from under the values that carry their unlock thresholds.
            CurrentGameState.PublishResourceGainedTotals();

            MainGameUIView.ResetUI();

            JournalEntrySystem.ShownJournalEntries.Clear();
            MainGameUIView.JournalMenuControl?.ClearJournalEntries();
            foreach (string journalEntry in CurrentGameState.JournalEntries)
            {
                JournalEntrySystem.ShownJournalEntries.Add(journalEntry);
                MainGameUIView.JournalMenuControl?.AddJournalEntry(journalEntry);
            }

            ActionsMenuController?.RefreshMenu();
            MainGameUIView.DelveTaskButton?.SetContents("Delve", CompleteDelve);

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
            bool produced = TaskActionSystem.TaskActionMap.ContainsKey(DelveActionName)
                && TaskActionSystem.TaskActionMap[DelveActionName].ProducesResources;
            CurrentGameState.AddToResources(changes, produced);

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
                WorkerAssignments = TaskActionSystem.GetAllWorkersAssigned(),
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

                //  Workers are restored after the buildings, since the population housing them and the jobs
                //  they fill are both derived from those counts. A save older than the schema has none, and
                //  a save whose numbers no longer add up is clamped rather than refused.
                TaskActionSystem.ClearAllWorkersAssigned();
                if (saveData.WorkerAssignments != null)
                {
                    foreach (var entry in saveData.WorkerAssignments)
                        TaskActionSystem.SetWorkersAssigned(entry.Key, entry.Value);
                }
                JobAssignmentSystem.ClampToAvailable();

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

            //  Unlocks and earned achievements live outside the game state and are only ever added to, so they
            //  are emptied before the initial state is applied: Initialize adds the authored starter unlocks on
            //  top of whatever is already there, and anything left would survive into the new game and keep its
            //  progression-gated content visible. This runs before SaveGame so the file cannot capture the
            //  previous run's progression alongside a reset game state.
            GameUnlockSystem.ClearUnlockValues();
            AchievementsSystem.ClearUnlockedAchievements();

            //  Set the game state to the Initial Game State, then immediately replace the existing save file with the new state
            CurrentGameState.Initialize(InitialGameState);

            //  Building counts live in the modifier system rather than the game state, so they have to be put
            //  back to their starting counts alongside it: land used is derived from them and would otherwise
            //  outlive the acquired land that paid for it. The recompute this triggers refreshes land in turn.
            ModifierValueSystem.Reset();

            //  Workers live on the task states rather than the game state, so a reset would otherwise leave
            //  the previous run's villagers at work in a settlement that no longer houses them.
            TaskActionSystem.ClearAllWorkersAssigned();

            SaveGame();

            PostLoadInitialState();
        }

        public void ToggleFullscreen()
        {
            BasicAppUtilities.SetWindowFullscreen(CurrentGameState.Fullscreen = !CurrentGameState.Fullscreen);
        }

        //  The Delve action carries whatever the current level's section asks and gives, so what a delve costs
        //  and rewards is read back from there rather than from the level data a second time. That is what
        //  keeps the resources a delve moves identical to the ones shown beside the button, and earns both of
        //  them the unlock alterations and modifiers the player has bought.
        public List<ResourceAmountData> GetCurrentDelveResourceChanges()
        {
            return TaskActionSystem.TaskActionMap.ContainsKey(DelveActionName)
                ? TaskActionSystem.TaskActionMap[DelveActionName].GetResourceChange()
                : new List<ResourceAmountData>();
        }

        //  A delve is priced by the section its level belongs to, so every level within one costs and gives
        //  the same and only crossing into the next changes either. Pushing that price onto the action, rather
        //  than reading it at the moment of the delve, leaves the cost and reward rows, the check on whether
        //  the delve can be afforded, and the resources actually moved all drawing on one figure.
        private void RefreshDelveResourceChange()
        {
            TaskActionSystem.SetActionResourceChange(DelveActionName, GetLevelSectionResourceChange());
        }

        private static List<ResourceAmountData> GetLevelSectionResourceChange()
        {
            int level = CurrentGameState.LevelCurrent.Value;
            if (level < 0 || level >= LevelDataSystem.GetLevelCount()) return new List<ResourceAmountData>();

            var grouping = LevelDataSystem.GetGroupingByLevel(level);
            if (grouping?.LevelResourceChange == null) return new List<ResourceAmountData>();

            List<ResourceAmountData> amountsList = new();
            foreach (var rc in grouping.LevelResourceChange)
                amountsList.AddResourceAmount(new ResourceAmountData(rc.ResourceType, rc.ResourceValue));

            return amountsList;
        }
    }
}