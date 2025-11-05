using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HypnicEmpire
{
    public class UIRadioButtonControlEntry : MonoBehaviour
    {
        [SerializeField] public UIRadioButton RadioButton;
        [SerializeField] public TextMeshProUGUI OptionNameText;

        void Start()
        {
            RadioButton?.GetComponent<Button>().onClick.AddListener(() =>
            {
                RadioButton.SetSelected(!RadioButton.GetSelected());
            });
        }

        public void AddListener(UnityEngine.Events.UnityAction action)
        {
            RadioButton?.GetComponent<Button>().onClick.AddListener(action);
        }

        public void SetDisplayText(string optionName)
        {
            OptionNameText?.SetText(optionName);
        }
    }
}