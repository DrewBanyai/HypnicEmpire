using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace HypnicEmpire
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class UITaskProcessButton : MonoBehaviour
    {
        [SerializeField] public string PlayerAction;
        [SerializeField] public Button Button;
        [SerializeField] public Image ProgressForeground;
        [SerializeField] public TextMeshProUGUI ButtonText;

        private float ButtonWidth;

        private Action ProgressFinishAction;

        public void Start()
        {
            ButtonWidth = ((RectTransform)Button.transform).rect.width;

            Button?.onClick.AddListener(() => {
                if (TaskActionSystem.PrimaryTask == PlayerAction)
                    TaskActionSystem.SetPrimaryTask("");
                else
                    TaskActionSystem.SetPrimaryTask(PlayerAction);
            });
        }

        public void SetContents(string actionType, float speed = 20f, float maximum = 100f, Action progressFinishAction = null)
        {
            PlayerAction = actionType;
            SetButtonText(Localization.DisplayText_ActionName(actionType));

            TaskActionSystem.SetTaskUpdateCallback(PlayerAction, (percent) => { UpdateProgressVisual(percent); });
            TaskActionSystem.SetTaskFinishCallback(PlayerAction, progressFinishAction);
        }

        private void SetButtonText(string buttonText) { ButtonText?.SetText(buttonText); }

        private void UpdateProgressVisual(int percent)
        {
            float newWidth = (float)percent / 100.0f * ButtonWidth;
            ProgressForeground.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        }

        public void SetEnabled(bool enabled)
        {
            Button?.SetInteractable(enabled);

            if (!enabled)
            {
                ProgressForeground.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
            }
        }

        public void Reset()
        {
            if (TaskActionSystem.PrimaryTask == PlayerAction)
                TaskActionSystem.SetPrimaryTask("");
                
            UpdateProgressVisual(0);
        }
    }
}