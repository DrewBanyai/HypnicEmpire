using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HypnicEmpire
{
    //  One row of the resource list. A row shows either a resource the player holds, which is counted
    //  against the storage available for it, or a value derived from what has been built, which is only
    //  ever a total and so has nothing to count against.
    public class UIResourceEntry : MonoBehaviour
    {
        [SerializeField] public Image ResourceIconImage;
        [SerializeField] public TextMeshProUGUI ResourceNameText;
        [SerializeField] public TextMeshProUGUI ResourceAmountText;

        //  Set for a derived row, so its total can be re-read whenever the buildings behind it change.
        private string DerivedValueName;

        public void SetContent(string resourceType)
        {
            SetResourceIconImage(resourceType);
            SetResourceNameText(resourceType);
            SetResourceAmountText(resourceType);
            
            GameSubscriptionSystem.SubscribeToResourceAmount(resourceType, (amountChange, newAmount) => { SetResourceAmountText(resourceType); });
            GameSubscriptionSystem.SubscribeToResourceMaximum(resourceType, (maxChange, newMax) => { SetResourceAmountText(resourceType); });
        }

        //  A tracked value the player never holds or spends: "People" and the like are accumulated from
        //  the buildings standing, so the row follows the recompute that produces them rather than the
        //  resource change events, which such a value never raises.
        public void SetDerivedValueContent(string valueName)
        {
            DerivedValueName = valueName;

            SetResourceIconImage(valueName);
            SetResourceNameText(valueName);
            SetDerivedValueAmountText();

            ModifierValueSystem.OnValuesRecomputed -= SetDerivedValueAmountText;
            ModifierValueSystem.OnValuesRecomputed += SetDerivedValueAmountText;
        }

        private void OnDestroy()
        {
            ModifierValueSystem.OnValuesRecomputed -= SetDerivedValueAmountText;
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

        private void SetDerivedValueAmountText()
        {
            ResourceAmountText?.SetText(Localization.DisplayText_ResourceCountTotal(AlterableValueSystem.GetAlterableValueCurrentVal(DerivedValueName)));
        }
    }
}
