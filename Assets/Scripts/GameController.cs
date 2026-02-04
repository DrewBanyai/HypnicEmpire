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
        private static string SaveFilePath => Application.persistentDataPath + "/saveGame.dat";
        [SerializeField] public GameStateScriptableObject InitialGameState;

        public static GameState CurrentGameState = new();
        public UIView_MainGame MainGameUIView;

        public void Start()
        {
            GameUnlockSystem.LoadAllUnlockIDs(Application.dataPath + "/GameData/UnlockIDs.json");
            AchievementsSystem.LoadAllAchievementsData(Application.dataPath + "/GameData/Achievements.json");
            JournalEntrySystem.LoadAllJournalEntries(Application.dataPath + "/GameData/JournalEntries.json");
            ResourceTypeSystem.LoadAllResourceTypes(Application.dataPath + "/GameData/Resources.json");
            LevelDataSystem.LoadAllLevelData(Application.dataPath + "/GameData/LevelData.json");
            AlterableValueSystem.LoadAllAlterableValues(Application.dataPath + "/GameData/AlterableValues.json");
            DevelopmentSystem.LoadAllDevelopments(Application.dataPath + "/GameData/Developments.json");
            TaskActionSystem.LoadAllTaskActions(Application.dataPath + "/GameData/TaskActions.json");
            CurrentGameState.Initialize(InitialGameState);
            MainGameUIView.Initialize();

            TaskActionSystem.SetGameState(CurrentGameState);

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
                    GameUnlockSystem.AddGameUnlockAction(trigger, (bool unlocked) =>
                    {
                        if (!unlocked) return;
                        
                        if (CurrentGameState.GetUnlockValue(trigger) == true)
                            return;
                            
                        List<string> listMinusTrigger = development.Trigger.Where(t => t != trigger).Select(t => t).ToList();
                        foreach (var t in listMinusTrigger)
                            if (CurrentGameState.GetUnlockValue(t) == false)
                                return;

                        MainGameUIView.AddOpenDevelopment(development.Title, development.Description, development.EffectText, development.Cost, development.Unlock);
                    });
                }
            }

            SaveUtility.SaveCallback = () => { SaveGame(); };

            //  Now initialize the UI
            PostLoadInitialState();
        }

        public void PostLoadInitialState()
        {
            MainGameUIView.ResetUI();

            MainGameUIView.ActionsMenu.GetComponent<UIActionMenuController>()?.InitializeMenu();
            MainGameUIView.DelveTaskButton?.SetContents("Delve", 20f, 64f, CompleteDelve);

            CheckDevelopments();
            CheckGameUnlocks();
            SubscribeToJournalEntries();

            //  If we haven't loaded a game state with the very first unlock, unlock it now
            if (!CurrentGameState.GameUnlockList.Contains("Unlock_Game_Start"))
                CurrentGameState.SetUnlockValue("Unlock_Game_Start", true);
        }

        public void CheckDevelopments()
        {

        }

        public void CheckGameUnlocks()
        {
            foreach (var gu in GameUnlockSystem.UnlockIDs)
                GameUnlockSystem.SetUnlockValue(gu, CurrentGameState.IsUnlocked(gu));
        }

        public void SubscribeToJournalEntries()
        {
            foreach (var je in JournalEntrySystem.JournalEntryDataMap)
                GameUnlockSystem.AddGameUnlockAction(je.Key, (bool unlocked) => {
                    if (!unlocked) return;
                    if (!CurrentGameState.GetUnlockValue(je.Key))
                        MainGameUIView?.JournalMenuControl?.AddJournalEntry(je.Value.Text[UnityEngine.Random.Range(0, je.Value.Text.Count)]);
                });
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
            File.WriteAllText(SaveFilePath, JsonSerialization.Serialize(CurrentGameState));
        }

        public void LoadGame()
        {
            string gameStateJson = File.ReadAllText(SaveFilePath);
            if (!string.IsNullOrEmpty(gameStateJson))
                CurrentGameState.CopyGameState(JsonSerialization.Deserialize<GameState>(gameStateJson));

            PostLoadInitialState();
        }

        public void ResetGame()
        {
            if (InitialGameState == null) return;

            //  Set the game state to the Initial Game State, then immediately replace the existing save file with the new state
            CurrentGameState.Initialize(InitialGameState);
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
            foreach (var rc in LevelDataSystem.GetGroupingByLevel(CurrentGameState.LevelCurrent.Value).ResourceChange)
                amountsList.AddResourceAmount(new ResourceAmountData(rc.ResourceType, rc.ResourceValue));

            return amountsList;
        }
    }
}