using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using System.Security.AccessControl;

namespace HypnicEmpire
{
    public class UIResourceEntry : MonoBehaviour
    {
        [SerializeField] public Image ResourceIconImage;
        [SerializeField] public TextMeshProUGUI ResourceNameText;
        [SerializeField] public TextMeshProUGUI ResourceAmountText;

        public void SetContent(ResourceType resourceType)
        {
            SetResourceIconImage(resourceType);
            SetResourceNameText(resourceType);
            SetResourceAmountText(resourceType);
            
            GameController.CurrentGameState.SubscribeToResourceAmount(resourceType, (amountChange, newAmount) => { SetResourceAmountText(resourceType); });
            GameController.CurrentGameState.SubscribeToResourceMaximum(resourceType, (maxChange, newMax) => { SetResourceAmountText(resourceType); });
        }

        private void SetResourceIconImage(ResourceType resourceType)
        {
            if (ResourceIconImage != null)
                ResourceIconImage.sprite = Resources.Load<Sprite>($"ResourceIcons/{resourceType.ToString()}");
        }
        
        private void SetResourceNameText(ResourceType resourceType)
        {
            ResourceNameText?.SetText(Localization.DisplayText_ResourceDisplayName(resourceType));
        }
        
        private void SetResourceAmountText(ResourceType resourceType)
        {
            ResourceAmountText?.SetText(Localization.DisplayText_ResourceCountDivide(GameController.CurrentGameState.GetResourceAmount(resourceType), GameController.CurrentGameState.GetResourceMaxAmount(resourceType)));
        }
    }
}
