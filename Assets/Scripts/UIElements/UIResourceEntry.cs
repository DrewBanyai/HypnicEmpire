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

        [SerializeField] public ResourceType ResourceType;

        public void SetContent(ResourceType resourceType)
        {
            try
            {
                ResourceType = resourceType;

                if (ResourceIconImage != null)
                    ResourceIconImage.sprite = Resources.Load<Sprite>($"ResourceIcons/{resourceType.ToString()}");

                if (ResourceNameText != null)
                    ResourceNameText.text = Localization.DisplayText_ResourceDisplayName(resourceType);

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
                Debug.LogError($"Invalid resource name: {resourceType.ToString()}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
