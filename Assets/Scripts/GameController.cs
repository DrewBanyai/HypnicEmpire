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
        [SerializeField] public GameLevelsScriptableObject GameLevelsData;
        [SerializeField] public DevelopmentsScriptableObject DevelopmentsData;
        [SerializeField] public PlayerActionScriptableObject PlayerActionDataMap;

        public static GameState CurrentGameState = new();
        public UIView_MainGame MainGameUIView;

        public void Start()
        {
            //  TODO: Remove this when properly testing at a normal speed
            Time.timeScale = 3f;

            CurrentGameState.Initialize(InitialGameState);
            MainGameUIView.Initialize();
            SetupMainGameUI();
        }

        public void Update()
        {
            SaveUtility.Update();
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

            MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent.Value, CurrentGameState.LevelReached.Value, GameLevelsData.GetLevel(CurrentGameState.LevelCurrent.Value), CurrentLevelUp, CurrentLevelDown);
            MainGameUIView.DelveTaskButton?.SetContents(PlayerActionType.Delve, 20f, 64f, CompleteDelve);

            MainGameUIView.LevelExplorationBar?.SetProgress((float)CurrentGameState.LevelDelveCount.Value / (float)GameLevelsData.GetLevel(CurrentGameState.LevelCurrent.Value).DelveCount);
            CurrentGameState.LevelDelveCount.Subscribe((newValue) =>
            {
                MainGameUIView.LevelExplorationBar?.SetProgress((float)CurrentGameState.LevelDelveCount.Value / (float)GameLevelsData.GetLevel(CurrentGameState.LevelCurrent.Value).DelveCount);
            });

            CurrentGameState.LevelCurrent.Subscribe((newValue) =>
            {
                MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent.Value, CurrentGameState.LevelReached.Value, GameLevelsData.GetLevel(CurrentGameState.LevelCurrent.Value), CurrentLevelUp, CurrentLevelDown);
            });

            CurrentGameState.LevelReached.Subscribe((newValue) =>
            {
                MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent.Value, CurrentGameState.LevelReached.Value, GameLevelsData.GetLevel(CurrentGameState.LevelCurrent.Value), CurrentLevelUp, CurrentLevelDown);
            });

            if (DevelopmentsData != null)
            {
                foreach (var development in DevelopmentsData.Developments)
                {
                    GameUnlockSystem.AddGameUnlockAction(development.Trigger, (bool shown) =>
                    {
                        if (shown)
                            MainGameUIView.AddOpenDevelopment(development.Title, development.Description, development.EffectText, development.Cost, development.Unlock);
                    });

                }
            }

            foreach (var entry in PlayerActionDataMap.PlayerActions)
                CurrentGameState.SetPlayerActionResourceChange(entry.ActionType, entry.ResourceChange);

            SaveUtility.SaveCallback = () => { SaveGame(); };

            //  Now initialize the UI
            PostLoadInitialState();
        }

        public void PostLoadInitialState()
        {
            MainGameUIView.ResetUI();

            MainGameUIView.SetDelveResourceChange(GetCurrentDelveResourceChanges());
            CurrentGameState.SubscribeToGenericResourceAmountChange((ResourceType rType, int amount, int maxAmount) => {
                MainGameUIView.DelveTaskButton.SetEnabled(GetCurrentDelveResourceChanges().CheckCanChangeAll());
            });

            //  Custom game events 01: Unlock Developments when Food resource reaches zero for the first time
            Action<int, int> unhideDevelopmentsFunc = null;
            unhideDevelopmentsFunc = (oldAmount, newAmount) =>
            {
                if (newAmount != 0) return;
                CurrentGameState.GameUnlockList[GameUnlock.Unlocked_Developments] = true;
                GameUnlockSystem.SetUnlockValue(GameUnlock.Unlocked_Developments, true);
                CurrentGameState.UnsubscribeToResourceAmount(ResourceType.Food, unhideDevelopmentsFunc);
            };
            CurrentGameState.SubscribeToResourceAmount(ResourceType.Food, unhideDevelopmentsFunc);

            CheckDevelopments();
            CheckGameUnlocks();
        }

        public void CheckDevelopments()
        {

        }

        public void CheckGameUnlocks()
        {
            foreach (var gu in Enum.GetValues(typeof(GameUnlock)).Cast<GameUnlock>())
                GameUnlockSystem.SetUnlockValue(gu, CurrentGameState.IsUnlocked(gu));
        }

        public void CurrentLevelUp()
        {
            SetCurrentLevel(CurrentGameState.LevelCurrent.Value + 1);
            MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent.Value, CurrentGameState.LevelReached.Value, GameLevelsData.GetLevel(CurrentGameState.LevelCurrent.Value), CurrentLevelUp, CurrentLevelDown);
        }

        public void CurrentLevelDown()
        {
            SetCurrentLevel(CurrentGameState.LevelCurrent.Value - 1);
            MainGameUIView.MissionDataDisplay?.SetContent(CurrentGameState.LevelCurrent.Value, CurrentGameState.LevelReached.Value, GameLevelsData.GetLevel(CurrentGameState.LevelCurrent.Value), CurrentLevelUp, CurrentLevelDown);
        }

        public void SetCurrentLevel(int level)
        {
            if (level < 0) return;
            if (level > CurrentGameState.LevelReached.Value) return;
            CurrentGameState.LevelCurrent.SetValue(level);
        }

        public void CompleteDelve()
        {
            // TODO: Remove this journal entry. This is just to help debug
            MainGameUIView?.AddJournalEntry("You have completed a Delve task!");

            //  Lose and gain all resources assigned to this level of the game, unlocking resources as needed
            var changes = GetCurrentDelveResourceChanges();
            CurrentGameState.AddToResources(changes);

            if (CurrentGameState.LevelCurrent.Value + 1 >= GameLevelsData.GameLevels.Count)
            {
                MainGameUIView?.DelveTaskButton.SetEnabled(false);
            }
            else
            {
                if (CurrentGameState.LevelDelveCount.Value + 1 >= GameLevelsData.GetLevel(CurrentGameState.LevelCurrent.Value)?.DelveCount)
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

        public List<ResourceAmount> GetCurrentDelveResourceChanges()
        {
            if (GameLevelsData == null) return new List<ResourceAmount>();
            if (CurrentGameState.LevelCurrent.Value >= GameLevelsData.GameLevels.Count || CurrentGameState.LevelCurrent.Value < 0) return new List<ResourceAmount>();

            List<ResourceAmount> amountsList = new();
            foreach (var ra in GameLevelsData.GameLevels[CurrentGameState.LevelCurrent.Value].ResourceChanges) amountsList.AddResourceAmount(new ResourceAmount(ra.Key, ra.Value));
            return amountsList;
        }
    }
}