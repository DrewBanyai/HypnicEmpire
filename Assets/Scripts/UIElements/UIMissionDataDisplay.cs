using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace HypnicEmpire
{
    public class UIMissionDataDisplay : MonoBehaviour
    {
        [SerializeField] public Image LevelImage;
        [SerializeField] public TextMeshProUGUI LevelNameText;
        [SerializeField] public TextMeshProUGUI LevelDescriptionText;
        [SerializeField] public UINumberOptionControlEntry LevelIndexControl;

        public void SetContent(int currentLevel, int maxLevel, int level, Action upLevelCallback, Action downLevelCallback)
        {
            if (currentLevel < 0 || maxLevel < 0 || currentLevel > maxLevel) return;

            //  TODO: Decouple this (UI shouldn't know about Game Data)
            var levelGrouping = LevelDataSystem.GetGroupingByLevel(level);
            var levelData = LevelDataSystem.GetLevelData(level);
            if (levelGrouping == null || levelData == null) return;

            LevelImage?.SetSprite(levelGrouping?.ImageSprite);
            LevelNameText?.SetText(levelGrouping?.Name);
            LevelDescriptionText?.SetText(levelData?.Description);

            Action upLevelCallbackAdvanced = () =>
            {
                upLevelCallback?.Invoke();
                RefreshButtons();
            };
            Action downLevelCallbackAdvanced = () =>
            {
                downLevelCallback?.Invoke();
                RefreshButtons();
            };

            LevelIndexControl?.SetContent("", Localization.DisplayText_CurrentLevelAndMax(currentLevel, maxLevel), upLevelCallbackAdvanced, downLevelCallbackAdvanced);
            RefreshButtons();
        }

        public void RefreshButtons()
        {
            LevelIndexControl?.IncreaseButton?.SetInteractable(GameController.CurrentGameState.LevelCurrent.Value < GameController.CurrentGameState.LevelReached.Value);
            LevelIndexControl?.DecreaseButton?.SetInteractable(GameController.CurrentGameState.LevelCurrent.Value > 0);
        }
    }
}
