using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HypnicEmpire
{
    public class UINumberOptionControlEntry : MonoBehaviour
    {
        public TextMeshProUGUI OptionNameText;
        public TextMeshProUGUI OptionValueText;
        public Button IncreaseButton;
        public Button DecreaseButton;
        public void SetContent(string optionName, string optionStartValue, Action onIncreaseClicked, Action onDecreaseClicked)
        {
            IncreaseButton?.onClick.RemoveAllListeners();
            IncreaseButton?.onClick.AddListener(() => onIncreaseClicked?.Invoke());
            DecreaseButton?.onClick.RemoveAllListeners();
            DecreaseButton?.onClick.AddListener(() => onDecreaseClicked?.Invoke());
            SetDisplayTexts(optionName, optionStartValue);
        }

        public void SetDisplayTexts(string optionName, string optionStartValue)
        {
            OptionNameText?.SetText(optionName);
            OptionValueText?.SetText(optionStartValue);
        }

        public void SetDisplayDetails(string optionName, string optionStartValue, bool upInteractable, bool downInteractable)
        {
            SetDisplayTexts(optionName, optionStartValue);
            IncreaseButton?.SetInteractable(upInteractable);
            DecreaseButton?.SetInteractable(downInteractable);
        }
    }
}