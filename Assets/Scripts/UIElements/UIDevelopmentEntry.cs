using TMPro;
using UnityEngine;

public class UIDevelopmentEntry : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI NameText;
    [SerializeField] public TextMeshProUGUI DescriptionText;
    [SerializeField] public TextMeshProUGUI ExtraInfoText;

    public void SetContent(string name, string description, string extraInfo)
    {
        SetNameText(name);
        SetDescriptionText(description);
        SetExtraInfoText(extraInfo);
    }

    public void SetNameText(string name)
    {
        if (NameText != null)
            NameText.text = name;
    }

    public void SetDescriptionText(string description)
    {
        if (DescriptionText != null)
            DescriptionText.text = description;
    }

    public void SetExtraInfoText(string extraInfo)
    {
        if (ExtraInfoText != null)
            ExtraInfoText.text = extraInfo;
    }
}
