using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace HypnicEmpire
{
    public class UIResourceEntry : MonoBehaviour
    {
        [SerializeField] public Image ResourceIconImage;
        [SerializeField] public TextMeshProUGUI ResourceNameText;
        [SerializeField] public TextMeshProUGUI ResourceAmountText;

        [SerializeField] public string ResourceName;

        public void SetContent(string resourceName)
        {
            try
            {
                ResourceName = resourceName;
                ResourceType resourceType = (ResourceType)Enum.Parse(typeof(ResourceType), resourceName, true);

                if (ResourceIconImage != null)
                    ResourceIconImage.sprite = Resources.Load<Sprite>($"ResourceIcons/{resourceName}");

                if (ResourceNameText != null)
                    ResourceNameText.text = Localization.DisplayText_ResourceDisplayName(resourceName);

                if (ResourceAmountText != null)
                    ResourceAmountText.text = Localization.DisplayText_ResourceCountDivide(GameController.CurrentGameState.GetResourceAmount(resourceType), GameController.CurrentGameState.GetResourceMaxAmount(resourceType));

                GameController.CurrentGameState.SubscribeToResourceAmount(resourceType, (amountChange, newAmount) =>
                {
                    if (ResourceAmountText != null)
                        ResourceAmountText.text = Localization.DisplayText_ResourceCountDivide(newAmount, GameController.CurrentGameState.GetResourceMaxAmount(resourceType));
                });

                GameController.CurrentGameState.SubscribeToResourceMaximum(resourceType, (maxChange, newMax) =>
                {
                    if (ResourceAmountText != null)
                        ResourceAmountText.text = Localization.DisplayText_ResourceCountDivide(GameController.CurrentGameState.GetResourceAmount(resourceType), newMax);
                });
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
    }
}
