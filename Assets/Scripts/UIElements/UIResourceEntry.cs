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

        public void SetContent(string resourceType)
        {
            SetResourceIconImage(resourceType);
            SetResourceNameText(resourceType);
            SetResourceAmountText(resourceType);
            
            GameController.GameSubscriptions.SubscribeToResourceAmount(resourceType, (amountChange, newAmount) => { SetResourceAmountText(resourceType); });
            GameController.GameSubscriptions.SubscribeToResourceMaximum(resourceType, (maxChange, newMax) => { SetResourceAmountText(resourceType); });
        }

        private void SetResourceIconImage(string resourceType)
        {
            if (ResourceIconImage != null)
                ResourceIconImage.sprite = Resources.Load<Sprite>($"ResourceIcons/{resourceType}");
        }
        
        private void SetResourceNameText(string resourceType)
        {
            ResourceNameText?.SetText(Localization.DisplayText_ResourceDisplayName(resourceType));
        }
        
        private void SetResourceAmountText(string resourceType)
        {
            ResourceAmountText?.SetText(Localization.DisplayText_ResourceCountDivide(GameController.CurrentGameState.GetResourceAmount(resourceType), GameController.CurrentGameState.GetResourceMaxAmount(resourceType)));
        }
    }
}
