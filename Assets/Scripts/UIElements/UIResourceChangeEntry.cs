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

        private ResourceAmount ChangeResourceAmount;

        public ResourceAmount GetResourceAmount() { return new ResourceAmount(ChangeResourceAmount.ResourceType, ChangeResourceAmount.Amount); }

        public void SetContent(string resourceName, int changeAmount)
        {
            try
            {
                ChangeResourceAmount = new ResourceAmount((ResourceType)Enum.Parse(typeof(ResourceType), resourceName, true), changeAmount);

                if (ResourceIconImage != null)
                    ResourceIconImage.sprite = Resources.Load<Sprite>($"ResourceIcons/{resourceName}");

                if (ResourceNameText != null)
                {
                    ResourceNameText.text = Localization.DisplayText_ResourceChangeDisplayName(resourceName);
                    ResourceNameText.overrideColorTags = true;
                    ResourceNameText.color = (changeAmount < 0) ? ResourceLossColor : ResourceGainColor;
                }

                if (ResourceChangeText != null)
                {
                    ResourceChangeText.text = Localization.DisplayText_ResourceChangeDisplayAmount(changeAmount);
                    ResourceChangeText.overrideColorTags = true;
                    ResourceChangeText.color = (changeAmount < 0) ? ResourceLossColor : ResourceGainColor;
                }
            }
            catch (ArgumentException)
            {
                Debug.LogError($"Invalid resource name: {resourceName}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public bool CheckCanChange()
        {
            if (Background != null) Background.SetActive(true);
            if (ChangeResourceAmount.Amount == 0) return true;

            int currentResourceAmount = GameController.CurrentGameState.GetResourceAmount(ChangeResourceAmount.ResourceType);
            if (ChangeResourceAmount.Amount < 0 && currentResourceAmount > Math.Abs(ChangeResourceAmount.Amount)) return true;

            int maxResourceAmount = GameController.CurrentGameState.GetResourceMaxAmount(ChangeResourceAmount.ResourceType);
            if (maxResourceAmount - currentResourceAmount <= ChangeResourceAmount.Amount) return true;

            if (Background != null) Background.SetActive(false);
            return false;
        }
    }
}