using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HypnicEmpire
{
    public class UIBuildingButton : MonoBehaviour
    {
        [SerializeField] public string BuildingName;
        [SerializeField] public Image BuildingButtonBox;

        [SerializeField] public TextMeshProUGUI BuildingTitleText;

        [SerializeField] public TextMeshProUGUI BuildingCountText;
        [SerializeField] public Image BuildingIconImage;

        [SerializeField] public TextMeshProUGUI BuildingDescriptionText;

        [SerializeField] public TextMeshProUGUI BuildingEffectText;

        public Color32 AvailableColor;
        public Color32 AvailableMouseOverColor;
        public Color32 AvailableCantAffordColor;
        public Color32 UnavailableColor;

        public void SetBuildingData(BuildingData data)
        {
            if (data == null) return;

            if (BuildingTitleText != null) BuildingTitleText.text = data.Name;
            if (BuildingDescriptionText != null) BuildingDescriptionText.text = data.Text;

            // Load icon from BuildingIcon path or fallback to name
            string spritePath = !string.IsNullOrEmpty(data.BuildingIcon) ? data.BuildingIcon : $"BuildingIcons/{data.Name.Replace(" ", "")}";
            Sprite sprite = Resources.Load<Sprite>(spritePath);
            if (sprite != null && BuildingIconImage != null) BuildingIconImage.sprite = sprite;

            // Format effects list
            string effectText = "";
            if (data.AlteredValues != null)
            {
                foreach (var av in data.AlteredValues)
                {
                    string sign = av.Amount >= 0 ? "+" : "";
                    effectText += $"{av.ValueName}: {sign}{av.Amount}\n";
                }
            }
            if (BuildingEffectText != null) BuildingEffectText.text = effectText.Trim();

            // Set the button box color to AvailableColor
            if (BuildingButtonBox != null) BuildingButtonBox.color = AvailableColor;
        }

        void Start()
        {
            //  TODO: Subscribe to changes in resource (value and maximum) as well as building count to update UI
        }
    }
}