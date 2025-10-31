using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UINumberOptionControlEntry : MonoBehaviour
{
    public TextMeshProUGUI OptionNameText;
    public TextMeshProUGUI OptionValueText;
    public Button IncreaseButton;
    public Button DecreaseButton;
    public void SetContent(string optionName, string optionStartValue, Action onIncreaseClicked, Action onDecreaseClicked)
    {
        if (IncreaseButton != null)
        {
            IncreaseButton.onClick.RemoveAllListeners();
            IncreaseButton.onClick.AddListener(() => onIncreaseClicked?.Invoke());
        }
        if (DecreaseButton != null)
        {
            DecreaseButton.onClick.RemoveAllListeners();
            DecreaseButton.onClick.AddListener(() => onDecreaseClicked?.Invoke());
        }
        SetDisplayTexts(optionName, optionStartValue);
    }

    public void SetDisplayTexts(string optionName, string optionStartValue)
    {
        if (OptionNameText != null)
            OptionNameText.text = optionName;
        if (OptionValueText != null)
            OptionValueText.text = optionStartValue;
    }

    public void SetDisplayDetails(string optionName, string optionStartValue, bool upInteractable, bool downInteractable)
    {
        SetDisplayTexts(optionName, optionStartValue);
        if (IncreaseButton != null) IncreaseButton.interactable = upInteractable;
        if (DecreaseButton != null) DecreaseButton.interactable = downInteractable;
    }
}
