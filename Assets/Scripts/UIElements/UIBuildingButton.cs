using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIBuildingButton : MonoBehaviour
{
    [SerializeField] public string BuildingName;

    [SerializeField] public TextMeshProUGUI BuildingTitleText;

    [SerializeField] public TextMeshProUGUI BuildingCountText;
    [SerializeField] public Image BuildingIconImage;

    [SerializeField] public TextMeshProUGUI BuildingDescriptionText;

    [SerializeField] public TextMeshProUGUI BuildingEffectText;

    public Color32 AvailableColor;
    public Color32 AvailableMouseOverColor;
    public Color32 AvailableCantAffordColor;
    public Color32 UnavailableColor;

    void Start()
    {
        //  TODO: Subscribe to changes in resource (value and maximum) as well as building count to update UI
    }
}
