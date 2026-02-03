using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace HypnicEmpire
{
    public class UIResourceChangeEntry : MonoBehaviour
    {
        //  Static colors for use with the UI
        public static Color ResourceLossColor => new Color32(169, 73, 73, 255);
        public static Color ResourceLossColorDisabledBG => new Color32(53, 53, 64, 255);
        public static Color ResourceLossColorDisabled => new Color32(237, 228, 218, 255);
        public static Color ResourceGainColor = new Color32(62, 85, 76, 255);

        [SerializeField] public GameObject Background;
        [SerializeField] public Image ResourceIconImage;
        [SerializeField] public TextMeshProUGUI ResourceNameText;
        [SerializeField] public TextMeshProUGUI ResourceChangeText;

        private ResourceAmountData ChangeResourceAmount;

        public ResourceAmountData GetResourceAmount() { return new ResourceAmountData(ChangeResourceAmount.ResourceType, ChangeResourceAmount.ResourceValue); }

        public void SetContent(string resourceType, ResourceValue changeAmount)
        {
            try
            {
                ChangeResourceAmount = new ResourceAmountData(resourceType, changeAmount);

                ResourceIconImage?.SetSprite(Resources.Load<Sprite>($"ResourceIcons/{resourceType}"));

                ResourceNameText?.SetText(Localization.DisplayText_ResourceChangeDisplayName(resourceType));
                ResourceNameText?.SetOverrideColorTags(true);
                ResourceNameText?.SetColor((changeAmount < 0) ? ResourceLossColor : ResourceGainColor);

                ResourceChangeText?.SetText(Localization.DisplayText_ResourceChangeDisplayAmount(changeAmount));
                ResourceChangeText?.SetOverrideColorTags(true);
                ResourceChangeText?.SetColor((changeAmount < 0) ? ResourceLossColor : ResourceGainColor);
            }
            catch (ArgumentException)
            {
                Debug.LogError($"Invalid resource name: {resourceType.ToString()}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public bool CheckCanChange(bool overrideNoBG = false, bool greenEvenNegative = false)
        {
            Background?.SetActive(true);
            if (ChangeResourceAmount.ResourceValue == 0) return true;

            ResourceValue currentResourceAmount = GameController.CurrentGameState.GetResourceAmount(ChangeResourceAmount.ResourceType);
            ResourceValue maxResourceAmount = GameController.CurrentGameState.GetResourceMaxAmount(ChangeResourceAmount.ResourceType);

            if (ChangeResourceAmount.ResourceValue < 0)
            {
                if (currentResourceAmount >= ChangeResourceAmount.ResourceValue.Abs())
                {
                    ResourceNameText?.SetColor(greenEvenNegative ? ResourceGainColor : ResourceLossColor);
                    ResourceChangeText?.SetColor(greenEvenNegative ? ResourceGainColor : ResourceLossColor);
                    Background?.SetActive(false);
                    return true;
                }
            }
            else
            {
                if (maxResourceAmount - currentResourceAmount <= ChangeResourceAmount.ResourceValue)
                {
                    ResourceNameText?.SetColor(ResourceGainColor);
                    ResourceChangeText?.SetColor(ResourceGainColor);
                    Background?.SetActive(false);
                    return true;
                }
            }

            ResourceNameText?.SetColor(ResourceLossColor);
            ResourceChangeText?.SetColor(ResourceLossColor);
            Background?.SetActive(overrideNoBG ? false : true);
            return false;
        }
    }
}