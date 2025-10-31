using UnityEngine;
using BasicAppUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Sleepwalking
{
    public class GameController : MonoBehaviour
    {
        public string SaveFilePath => Application.persistentDataPath + "/saveGame.dat";

        public static GameState GameState = new();
        public GameStateScriptableObject InitialGameState;
        public GameLevelsScriptableObject GameLevelsData;
        public UIView_MainGame MainGameUIView;

        private const float SaveInterval = 600.0f; // Save every 10 minutes
        private float SaveTimer = 0.0f;

        public void Start()
        {
            if (InitialGameState != null)
                GameState.Initialize(InitialGameState);

            SetupMainGameUI();
        }

        public void Update()
        {
            CheckForAutoSave();
        }

        private void CheckForAutoSave()
        {
            //  Iterate the SaveTimer and save the game if the interval is reached
            SaveTimer += Time.deltaTime;
            if (SaveTimer >= SaveInterval)
            {
                SaveGame();
                MainGameUIView.AddJournalEntry(Localization.JournalEntry_GameSaved());
                SaveTimer = 0.0f;
            }
        }

        private void SetupMainGameUI()
        {
            if (MainGameUIView != null)
            {
                // Assign button actions
                MainGameUIView.SaveAndExitButtonAction = SaveAndExitGame;
                MainGameUIView.SaveButtonAction = SaveGame;
                MainGameUIView.LoadButtonAction = LoadGame;

                if (MainGameUIView.MasterVolumeControlEntry != null) MainGameUIView.MasterVolumeControlEntry.SetContent(
                    "Master",
                    GameState.MasterVolume.ToString(),
                    () =>
                    {
                        GameState.MasterVolume = Mathf.Clamp(GameState.MasterVolume + 5, 0, 100);
                        MainGameUIView.MasterVolumeControlEntry.SetDisplayTexts("Master", GameState.MasterVolume.ToString());
                        MainGameUIView.MasterVolumeControlEntry.IncreaseButton.interactable = GameState.MasterVolume != 100;
                        MainGameUIView.MasterVolumeControlEntry.DecreaseButton.interactable = GameState.MasterVolume != 0;
                    },
                    () =>
                    {
                        GameState.MasterVolume = Mathf.Clamp(GameState.MasterVolume - 5, 0, 100);
                        MainGameUIView.MasterVolumeControlEntry.SetDisplayTexts("Master", GameState.MasterVolume.ToString());
                        MainGameUIView.MasterVolumeControlEntry.IncreaseButton.interactable = GameState.MasterVolume != 100;
                        MainGameUIView.MasterVolumeControlEntry.DecreaseButton.interactable = GameState.MasterVolume != 0;
                    });

                if (MainGameUIView.SFXVolumeControlEntry != null) MainGameUIView.SFXVolumeControlEntry.SetContent(
                    "Soundeffects",
                    GameState.SFXVolume.ToString(),
                    () =>
                    {
                        GameState.SFXVolume = Mathf.Clamp(GameState.SFXVolume + 5, 0, 100);
                        MainGameUIView.SFXVolumeControlEntry.SetDisplayTexts("Soundeffects", GameState.SFXVolume.ToString());
                        MainGameUIView.SFXVolumeControlEntry.IncreaseButton.interactable = GameState.SFXVolume != 100;
                        MainGameUIView.SFXVolumeControlEntry.DecreaseButton.interactable = GameState.SFXVolume != 0;
                    },
                    () =>
                    {
                        GameState.SFXVolume = Mathf.Clamp(GameState.SFXVolume - 5, 0, 100);
                        MainGameUIView.SFXVolumeControlEntry.SetDisplayTexts("Soundeffects", GameState.SFXVolume.ToString());
                        MainGameUIView.SFXVolumeControlEntry.IncreaseButton.interactable = GameState.SFXVolume != 100;
                        MainGameUIView.SFXVolumeControlEntry.DecreaseButton.interactable = GameState.SFXVolume != 0;
                    });

                if (MainGameUIView.MusicVolumeControlEntry != null) MainGameUIView.MusicVolumeControlEntry.SetContent(
                    "Music",
                    GameState.MusicVolume.ToString(),
                    () =>
                    {
                        GameState.MusicVolume = Mathf.Clamp(GameState.MusicVolume + 5, 0, 100);
                        MainGameUIView.MusicVolumeControlEntry.SetDisplayTexts("Music", GameState.MusicVolume.ToString());
                        MainGameUIView.MusicVolumeControlEntry.IncreaseButton.interactable = GameState.MusicVolume != 100;
                        MainGameUIView.MusicVolumeControlEntry.DecreaseButton.interactable = GameState.MusicVolume != 0;
                    },
                    () =>
                    {
                        GameState.MusicVolume = Mathf.Clamp(GameState.MusicVolume - 5, 0, 100);
                        MainGameUIView.MusicVolumeControlEntry.SetDisplayTexts("Music", GameState.MusicVolume.ToString());
                        MainGameUIView.MusicVolumeControlEntry.IncreaseButton.interactable = GameState.MusicVolume != 100;
                        MainGameUIView.MusicVolumeControlEntry.DecreaseButton.interactable = GameState.MusicVolume != 0;
                    });

                if (MainGameUIView.ActionSoundExcessControlEntry != null)
                {
                    MainGameUIView.ActionSoundExcessControlEntry.AddListener(() =>
                    {
                        GameState.ActionSoundExcess = !GameState.ActionSoundExcess;
                    });
                }

                if (MainGameUIView.FullscreenControlEntry != null)
                {
                    MainGameUIView.FullscreenControlEntry.AddListener(() =>
                    {
                        GameState.Fullscreen = !GameState.Fullscreen;
                        BasicAppUtilities.SetWindowFullscreen(GameState.Fullscreen);
                    });
                }

                if (MainGameUIView.HardResetButton != null)
                {
                    MainGameUIView.HardResetButton.onClick.AddListener(() =>
                    {
                        MainGameUIView.SetResetButtonUnpacked(true);
                    });
                }

                if (MainGameUIView.HardResetConfirmButton != null)
                {
                    MainGameUIView.HardResetConfirmButton.onClick.AddListener(() =>
                    {
                        ResetGame();
                        PostLoadInitialState();
                    });
                }

                if (MainGameUIView.HardResetCancelButton != null)
                {
                    MainGameUIView.HardResetCancelButton.onClick.AddListener(() =>
                    {
                        MainGameUIView.SetResetButtonUnpacked(false);
                    });
                }

                if (MainGameUIView.DelveTaskButton != null)
                {
                    MainGameUIView.DelveTaskButton.SetContents("Delve", 20f, 64f, () =>
                    {
                        // Action to perform when Delve task is completed
                        MainGameUIView.AddJournalEntry("You have completed a Delve task!");
                        // For example, spend some Food as a cost
                        var changes = GetCurrentDelveResourceChanges();
                        if (changes.ResourceAmounts.Any(ra => ra.ResourceType == ResourceType.Treasure)) GameState.SetResourceUnlocked(ResourceType.Treasure, true);
                        if (changes.ResourceAmounts.Any(ra => ra.ResourceType == ResourceType.Herbs)) GameState.SetResourceUnlocked(ResourceType.Herbs, true);
                        ChangeResources(changes);
                    });
                }

                //  Now initialize the UI
                PostLoadInitialState();
            }
        }

        public void PostLoadInitialState()
        {
            MainGameUIView.ResetUI();
            MainGameUIView.SetDelveResourceChange(GetCurrentDelveResourceChanges());
            MainGameUIView.DelveTaskButton.SetEnabled(GetCurrentDelveResourceChanges().CheckCanChange());

            GameState.SubscribeToGenericResourceAmountChange((int amount, int maxAmount) =>
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
                    GameState.GameUnlockList.Add(GameUnlock.Ran_Out_Of_Food);
                    MainGameUIView.AddOpenDevelopment("Initial Development", "You have run out of Food for the first time! This development has been unlocked as a result.", "No extra info.");
                    GameState.UnsubscribeToResourceAmount(ResourceType.Food, unhideDevelopmentsFunc);
                }
            };
            GameState.SubscribeToResourceAmount(ResourceType.Food, unhideDevelopmentsFunc);

            CheckGameUnlocks();
        }

        public void CheckGameUnlocks()
        {
            if (MainGameUIView == null) return;
            
            //  Unhide the Developments Tab and then re-hide it if our current game state has not unlocked the Ran_Out_Of_Food event that triggers it being shown
            MainGameUIView.SetDevelopmentsTabGroupHidden(false);
            if (!GameState.GameUnlockList.Contains(GameUnlock.Ran_Out_Of_Food))
                MainGameUIView.ResetDevelopmentMenu();

            //  Unlock resources visually in the left-most Resource Entry list on the main game UI
            foreach (var ru in GameState.CurrentResourceUnlocked) if (ru.Value == true) MainGameUIView.AddResourceEntry(ru.Key.ToString());
        }

        public void SaveAndExitGame()
        {
            SaveGame();
            BasicAppUtilities.ExitApplication();
        }

        public void SaveGame()
        {
            var gameStateJson = JsonSerialization.Serialize(GameState);
            System.IO.File.WriteAllText(SaveFilePath, gameStateJson);
        }

        public void LoadGame()
        {
            string gameStateJson = System.IO.File.ReadAllText(SaveFilePath);
            if (!string.IsNullOrEmpty(gameStateJson))
            {
                GameState deserializedState = JsonSerialization.Deserialize<GameState>(gameStateJson);
                GameState.CopyGameState(deserializedState);
            }

            PostLoadInitialState();
        }

        public void ResetGame()
        {
            if (InitialGameState == null) return;

            //  Set the game state to the Initial Game State, then immediately replace the existing save file with the new state
            GameState.Initialize(InitialGameState);
            SaveGame();
        }

        public ResourceAmountCollection GetCurrentDelveResourceChanges()
        {
            if (GameLevelsData == null) return new ResourceAmountCollection();
            if (GameState.LevelCurrent >= GameLevelsData.GameLevels.Count) return new ResourceAmountCollection();
            if (GameState.LevelCurrent < 0) return new ResourceAmountCollection();

            ResourceAmountCollection resourceChangeCollection = new ResourceAmountCollection();
            foreach (var rc in GameLevelsData.GameLevels[GameState.LevelCurrent].ResourceChanges) resourceChangeCollection.AddResourceAmount(new ResourceAmount(rc.Key, rc.Value));
            return resourceChangeCollection;
        }

        public void ChangeResources(ResourceAmountCollection resourceChanges)
        {
            if (resourceChanges == null) return;
            if (resourceChanges.ResourceAmounts == null) return;
            if (resourceChanges.ResourceAmounts.Count == 0) return;

            foreach (var rc in resourceChanges.ResourceAmounts)
            {
                GameState.AddToResource(rc.ResourceType, rc.Amount);
                MainGameUIView.AddResourceEntry(rc.ResourceType.ToString());
            }
        }
    }
}