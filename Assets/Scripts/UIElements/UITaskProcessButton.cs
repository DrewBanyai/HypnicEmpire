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

        //  The frame that marks this button as the one the player's own effort is going into. It is a graphic
        //  of its own rather than an effect on the button's background, because the button's colour transition
        //  tints everything drawn by that background and would drag the frame along with it. Only the chosen
        //  action wears it, so it is authored disabled and switched on from here alone.
        [SerializeField] public Image SelectionHighlight;

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

            //  Only one action can be the player's at a time, so a choice made on any button unmarks this one.
            TaskActionSystem.OnPrimaryTaskChanged -= HandlePrimaryTaskChanged;
            TaskActionSystem.OnPrimaryTaskChanged += HandlePrimaryTaskChanged;

            RefreshSelectionHighlight();
        }

        private void OnDestroy()
        {
            TaskActionSystem.OnPrimaryTaskChanged -= HandlePrimaryTaskChanged;
        }

        public void SetContents(string actionType, Action progressFinishAction = null)
        {
            PlayerAction = actionType;
            SetButtonText(Localization.DisplayText_ActionName(actionType));

            TaskActionSystem.SetTaskUpdateCallback(PlayerAction, (percent) => { UpdateProgressVisual(percent); });
            TaskActionSystem.SetTaskFinishCallback(PlayerAction, progressFinishAction);

            RefreshSelectionHighlight();
        }

        private void HandlePrimaryTaskChanged(string primaryTask) { RefreshSelectionHighlight(); }

        //  Which action this button stands for is settled by SetContents, which can land either side of
        //  Start, so the mark is re-read at both rather than only once.
        private void RefreshSelectionHighlight()
        {
            if (SelectionHighlight == null) return;

            SelectionHighlight.enabled = !string.IsNullOrEmpty(PlayerAction) && TaskActionSystem.PrimaryTask == PlayerAction;
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

            RefreshSelectionHighlight();
            UpdateProgressVisual(0);
        }
    }
}