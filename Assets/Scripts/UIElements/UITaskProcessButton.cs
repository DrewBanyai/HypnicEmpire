using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class UITaskProcessButton : MonoBehaviour
{
    [SerializeField] public Button Button;
    [SerializeField] public Image ProgressForeground;
    [SerializeField] public TextMeshProUGUI ButtonText;
    [SerializeField] public bool Activated = false;

    private float ButtonWidth;

    private float ProgressSpeed = 20f;
    private float ProgressCurrent = 0f;
    private float ProgressMaximum = 100f;
    private int ProgressPercent = 0;

    private Action ProgressFinishAction;

    public void Start()
    {
        ButtonWidth = ((RectTransform)Button.transform).rect.width;

        if (Button != null)
            Button.onClick.AddListener(() => { Activated = !Activated; });
    }

    public void Update()
    {
        if (!Activated) return;
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

    public void SetContents(string buttonText, float speed = 20f, float maximum = 100f, Action progressFinishAction = null)
    {
        SetButtonText(buttonText);
        ProgressFinishAction = progressFinishAction;
        ProgressSpeed = speed;
        ProgressMaximum = maximum;
    }

    private void SetButtonText(string buttonText)
    {
        if (ButtonText != null)
            ButtonText.text = buttonText;
    }

    private void UpdateProgressVisual()
    {
        float newWidth = ProgressCurrent / ProgressMaximum * ButtonWidth;
        ProgressForeground.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
    }

    public void SetEnabled(bool enabled)
    {
        Button.interactable = enabled;

        if (!enabled)
            ProgressForeground.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
    }

    public void Reset()
    {
        ProgressCurrent = 0f;
        ProgressPercent = 0;
        Activated = false;
        UpdateProgressVisual();
    }
}
