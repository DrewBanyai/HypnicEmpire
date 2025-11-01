using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BasicAppUtility;
using System.Security.AccessControl;

namespace HypnicEmpire
{
    public class GameController : MonoBehaviour
    {
        private static string SaveFilePath => Application.persistentDataPath + "/saveGame.dat";
        public GameStateScriptableObject InitialGameState;
        public GameLevelsScriptableObject GameLevelsData;

        public static GameState CurrentGameState = new();
        public UIView_MainGame MainGameUIView;

        public void Start()
        {
            CurrentGameState.Initialize(InitialGameState);
            SetupMainGameUI();
        }

        public void Update()
        {
            SaveUtility.Update();
        }

        private void SetupMainGameUI()
        {
            if (MainGameUIView != null)
            {
                //  Assign button actions
                MainGameUIView.SaveAndExitButtonAction = SaveAndExitGame;
                MainGameUIView.SaveButtonAction = SaveGame;
                MainGameUIView.LoadButtonAction = LoadGame;

                //  Define out the UINumberOptionControlEntry menu instances
                MainGameUIView.MasterVolumeControlEntry?.SetContent(
                    "Master",
                    CurrentGameState.MasterVolume.ToString(),
                    () =>
                    {
                        CurrentGameState.MasterVolume = Mathf.Clamp(CurrentGameState.MasterVolume + 5, 0, 100);
                        MainGameUIView.MasterVolumeControlEntry.SetDisplayDetails("Master", CurrentGameState.MasterVolume.ToString(), CurrentGameState.MasterVolume != 100, CurrentGameState.MasterVolume != 0);
                    },
                    () =>
                    {
                        CurrentGameState.MasterVolume = Mathf.Clamp(CurrentGameState.MasterVolume - 5, 0, 100);
                        MainGameUIView.MasterVolumeControlEntry.SetDisplayDetails("Master", CurrentGameState.MasterVolume.ToString(), CurrentGameState.MasterVolume != 100, CurrentGameState.MasterVolume != 0);
                    });

                MainGameUIView.SFXVolumeControlEntry?.SetContent(
                    "Soundeffects",
                    CurrentGameState.SFXVolume.ToString(),
                    () =>
                    {
                        CurrentGameState.SFXVolume = Mathf.Clamp(CurrentGameState.SFXVolume + 5, 0, 100);
                        MainGameUIView.SFXVolumeControlEntry.SetDisplayDetails("Soundeffects", CurrentGameState.SFXVolume.ToString(), CurrentGameState.SFXVolume != 100, CurrentGameState.SFXVolume != 0);
                    },
                    () =>
                    {
                        CurrentGameState.SFXVolume = Mathf.Clamp(CurrentGameState.SFXVolume - 5, 0, 100);
                        MainGameUIView.SFXVolumeControlEntry.SetDisplayDetails("Soundeffects", CurrentGameState.SFXVolume.ToString(), CurrentGameState.SFXVolume != 100, CurrentGameState.SFXVolume != 0);
                    });

                MainGameUIView.MusicVolumeControlEntry?.SetContent(
                    "Music",
                    CurrentGameState.MusicVolume.ToString(),
                    () =>
                    {
                        CurrentGameState.MusicVolume = Mathf.Clamp(CurrentGameState.MusicVolume + 5, 0, 100);
                        MainGameUIView.MusicVolumeControlEntry.SetDisplayDetails("Music", CurrentGameState.MusicVolume.ToString(), CurrentGameState.MusicVolume != 100, CurrentGameState.MusicVolume != 0);
                    },
                    () =>
                    {
                        CurrentGameState.MusicVolume = Mathf.Clamp(CurrentGameState.MusicVolume - 5, 0, 100);
                        MainGameUIView.MusicVolumeControlEntry.SetDisplayDetails("Music", CurrentGameState.MusicVolume.ToString(), CurrentGameState.MusicVolume != 100, CurrentGameState.MusicVolume != 0);
                    });

                MainGameUIView.ActionSoundExcessControlEntry?.AddListener(() => { CurrentGameState.ActionSoundExcess = !CurrentGameState.ActionSoundExcess; });
                MainGameUIView.FullscreenControlEntry?.AddListener(() => { BasicAppUtilities.SetWindowFullscreen(CurrentGameState.Fullscreen = !CurrentGameState.Fullscreen); });
                MainGameUIView.HardResetButton?.onClick.AddListener(() => { MainGameUIView.SetResetButtonUnpacked(true); });
                MainGameUIView.HardResetConfirmButton?.onClick.AddListener(() => { ResetGame(); });
                MainGameUIView.HardResetCancelButton?.onClick.AddListener(() => { MainGameUIView.SetResetButtonUnpacked(false); });

                CurrentGameState.LevelReached = 7;
                MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent, CurrentGameState.LevelReached, GameLevelsData.GetLevel(CurrentGameState.LevelCurrent), CurrentLevelUp, CurrentLevelDown);
                MainGameUIView.DelveTaskButton?.SetContents("Delve", 20f, 64f, CompleteDelve);

                SaveUtility.SaveCallback = () => { SaveGame(); };

                //  Now initialize the UI
                PostLoadInitialState();
            }
        }

        public void PostLoadInitialState()
        {
            MainGameUIView.ResetUI();
            MainGameUIView.SetDelveResourceChange(GetCurrentDelveResourceChanges());

            CurrentGameState.SubscribeToGenericResourceAmountChange((int amount, int maxAmount) =>
            {
                if (!GetCurrentDelveResourceChanges().CheckCanChange())
                    MainGameUIView.DelveTaskButton.SetEnabled(false);
            });

            //  Custom game events 01: Unhide Developments tab group when Food resource reaches zero for the first time
            Action<int, int> unhideDevelopmentsFunc = null;
            unhideDevelopmentsFunc = (oldAmount, newAmount) =>
            {
                if (newAmount == 0)
                {
                    CurrentGameState.GameUnlockList[GameUnlock.Unlocked_Developments] = true;
                    MainGameUIView.AddOpenDevelopment("Initial Development", "You have run out of Food for the first time! This development has been unlocked as a result.", "No extra info.");
                    CurrentGameState.UnsubscribeToResourceAmount(ResourceType.Food, unhideDevelopmentsFunc);
                }
            };
            CurrentGameState.SubscribeToResourceAmount(ResourceType.Food, unhideDevelopmentsFunc);

            MainGameUIView.ResetDevelopmentMenu();
            CheckDevelopments();
            CheckGameUnlocks();
        }

        public void CheckDevelopments()
        {
            
        }

        public void CheckGameUnlocks()
        {
            if (MainGameUIView == null) return;

            //  Set the Developments tab to be visible if our current game state has not unlocked the Unlocked_Developments event that triggers it being shown
            MainGameUIView.ResourceListControl.ClearAllResourceEntries();
            foreach (var gu in Enum.GetValues(typeof(GameUnlock)).Cast<GameUnlock>())
            {
                bool unlocked = CurrentGameState.IsUnlocked(gu);
                GameUnlockSystem.SetUnlockValue(gu, unlocked);

                ResourceType? unlockedResource = ResourceGameUnlockUtility.GetResourceTypeFromUnlock(gu);
                if (unlockedResource != null && unlocked) MainGameUIView.ResourceListControl.AddResourceEntry(unlockedResource.Value);
            }
        }

        public void CurrentLevelUp()
        {
            SetCurrentLevel(CurrentGameState.LevelCurrent + 1);
            MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent, CurrentGameState.LevelReached, GameLevelsData.GetLevel(CurrentGameState.LevelCurrent), CurrentLevelUp, CurrentLevelDown);
        }

        public void CurrentLevelDown()
        {
            SetCurrentLevel(CurrentGameState.LevelCurrent - 1);
            MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent, CurrentGameState.LevelReached, GameLevelsData.GetLevel(CurrentGameState.LevelCurrent), CurrentLevelUp, CurrentLevelDown);
        }

        public void SetCurrentLevel(int level)
        {
            if (level < 0) return;
            if (level > CurrentGameState.LevelReached) return;
            CurrentGameState.LevelCurrent = level;
        }
        
        public void CompleteDelve()
        {
            // TODO: Remove this journal entry. This is just to help debug
            MainGameUIView?.AddJournalEntry("You have completed a Delve task!");

            //  Lose and gain all resources assigned to this level of the game, unlocking resources as needed
            var changes = GetCurrentDelveResourceChanges();
            ChangeResources(changes);
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

        public List<ResourceAmount> GetCurrentDelveResourceChanges()
        {
            if (GameLevelsData == null) return new List<ResourceAmount>();
            if (CurrentGameState.LevelCurrent >= GameLevelsData.GameLevels.Count || CurrentGameState.LevelCurrent < 0) return new List<ResourceAmount>();

            List<ResourceAmount> amountsList = new();
            foreach (var ra in GameLevelsData.GameLevels[CurrentGameState.LevelCurrent].ResourceChanges) amountsList.AddResourceAmount(new ResourceAmount(ra.Key, ra.Value));
            return amountsList;
        }

        public void ChangeResources(List<ResourceAmount> amountsList)
        {
            if (amountsList == null || amountsList.Count == 0) return;

            foreach (var ra in amountsList)
            {
                CurrentGameState.AddToResource(ra.ResourceType, ra.Amount);
                MainGameUIView.ResourceListControl?.AddResourceEntry(ra.ResourceType);
            }
        }
    }
}