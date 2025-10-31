using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRadioButtonControlEntry : MonoBehaviour
{
    [SerializeField] public UIRadioButton RadioButton;
    [SerializeField] public TextMeshProUGUI OptionNameText;

    void Start()
    {
        if (RadioButton != null)
        {
            RadioButton.GetComponent<Button>().onClick.AddListener(() => {
                RadioButton.SetSelected(!RadioButton.GetSelected());
            });
        }
    }

    public void AddListener(UnityEngine.Events.UnityAction action)
    {
        if (RadioButton != null)
        {
            RadioButton.GetComponent<Button>().onClick.AddListener(action);
        }
    }

    public void SetDisplayText(string optionName)
    {
        if (OptionNameText != null)
            OptionNameText.text = optionName;
    }
}
