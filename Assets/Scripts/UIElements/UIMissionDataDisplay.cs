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

        public void SetContent(int currentLevel, int maxLevel, GameLevelData levelData, Action upLevelCallback, Action downLevelCallback)
        {
            if (currentLevel < 0 || maxLevel < 0 || currentLevel > maxLevel) return;
            if (levelData == null) return;

            if (LevelImage != null) LevelImage.sprite = levelData.Sprite;
            if (LevelNameText != null) LevelNameText.text = levelData.Name;
            if (LevelDescriptionText != null) LevelDescriptionText.text = levelData.Description;

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
            if (LevelIndexControl == null) return;
            if (LevelIndexControl.IncreaseButton != null) LevelIndexControl.IncreaseButton.interactable = GameController.CurrentGameState.LevelCurrent < GameController.CurrentGameState.LevelReached;
            if (LevelIndexControl.DecreaseButton != null) LevelIndexControl.DecreaseButton.interactable = GameController.CurrentGameState.LevelCurrent > 0;
        }
    }
}
