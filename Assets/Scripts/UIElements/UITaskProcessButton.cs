using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HypnicEmpire
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class UITaskProcessButton : MonoBehaviour
    {
        [SerializeField] public PlayerActionType PlayerAction;
        [SerializeField] public Button Button;
        [SerializeField] public Image ProgressForeground;
        [SerializeField] public TextMeshProUGUI ButtonText;

        private float ButtonWidth;

        private float ProgressSpeed = 20f;
        private float ProgressCurrent = 0f;
        private float ProgressMaximum = 100f;
        private int ProgressPercent = 0;

        private Action ProgressFinishAction;

        public void Start()
        {
            ButtonWidth = ((RectTransform)Button.transform).rect.width;

            Button?.onClick.AddListener(() => {
                if (GameController.CurrentGameState.CurrentPlayerAction == PlayerAction)
                    GameController.CurrentGameState.CurrentPlayerAction = PlayerActionType.Inactive;
                else
                    GameController.CurrentGameState.CurrentPlayerAction = PlayerAction;
            });
        }

        public void Update()
        {
            if (GameController.CurrentGameState.CurrentPlayerAction != PlayerAction) return;
            if (!Button.interactable) return;

            ProgressCurrent = Mathf.Clamp(ProgressCurrent + ProgressSpeed * Time.deltaTime, 0, ProgressMaximum);
            int percent = (int)(ProgressCurrent / ProgressMaximum * 100f);
            if (percent != ProgressPercent)
            {
                ProgressPercent = percent;
                UpdateProgressVisual();

                if (ProgressPercent >= 100)
                {
                    ProgressFinishAction?.Invoke();
                    ProgressPercent = 0;
                    ProgressCurrent = 0f;
                }
            }
        }

        public void SetContents(PlayerActionType actionType, float speed = 20f, float maximum = 100f, Action progressFinishAction = null)
        {
            PlayerAction = actionType;
            SetButtonText(Localization.DisplayText_ActionName(actionType));
            ProgressFinishAction = progressFinishAction;
            ProgressSpeed = speed;
            ProgressMaximum = maximum;
        }

        private void SetButtonText(string buttonText) { ButtonText?.SetText(buttonText); }

        private void UpdateProgressVisual()
        {
            float newWidth = ProgressCurrent / ProgressMaximum * ButtonWidth;
            ProgressForeground.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        }

        public void SetEnabled(bool enabled)
        {
            Button?.SetInteractable(enabled);

            if (!enabled)
            {
                ProgressCurrent = 0f;
                ProgressForeground.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
            }
        }

        public void Reset()
        {
            ProgressCurrent = 0f;
            ProgressPercent = 0;
            if (GameController.CurrentGameState.CurrentPlayerAction == PlayerAction)
                GameController.CurrentGameState.CurrentPlayerAction = PlayerActionType.Inactive;
                
            UpdateProgressVisual();
        }
    }
}