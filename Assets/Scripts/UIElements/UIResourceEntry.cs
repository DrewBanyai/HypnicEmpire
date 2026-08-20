using System;
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

        //  Kept for a resource row so it can drop its subscriptions when the row goes: a load or a reset
        //  empties the list, and a row left subscribed would be asked to redraw text it no longer has.
        private string SubscribedResourceType;
        private Action<ResourceValue, ResourceValue> AmountChangedCallback;
        private Action<ResourceValue, ResourceValue> MaximumChangedCallback;

        //  The colour the total is authored in, taken from the row before anything is done to it so a row
        //  with room again can be put back the way it was drawn rather than to a colour named here.
        private Color? AuthoredAmountColor;

        public void SetContent(string resourceType)
        {
            SetResourceIconImage(resourceType);
            SetResourceNameText(resourceType);
            SetResourceAmountText(resourceType);

            SubscribedResourceType = resourceType;
            AmountChangedCallback = (amountChange, newAmount) => { SetResourceAmountText(resourceType); };
            MaximumChangedCallback = (maxChange, newMax) => { SetResourceAmountText(resourceType); };

            GameSubscriptionSystem.SubscribeToResourceAmount(resourceType, AmountChangedCallback);
            GameSubscriptionSystem.SubscribeToResourceMaximum(resourceType, MaximumChangedCallback);
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

            if (SubscribedResourceType == null) return;

            GameSubscriptionSystem.UnsubscribeToResourceAmount(SubscribedResourceType, AmountChangedCallback);
            GameSubscriptionSystem.UnsubscribeToResourceMaximum(SubscribedResourceType, MaximumChangedCallback);
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
            SetResourceAmountColor(resourceType);
        }

        //  A full store is saying the same thing here as it does on a reward line whose gain cannot land, so
        //  it is greyed in the same colour: the row still reads, but no longer as a total on its way up.
        private void SetResourceAmountColor(string resourceType)
        {
            if (ResourceAmountText == null) return;

            AuthoredAmountColor ??= ResourceAmountText.color;

            ResourceAmountText.SetOverrideColorTags(true);
            ResourceAmountText.SetColor(GameController.CurrentGameState.ResourceStorageIsFull(resourceType)
                ? UIResourceColors.StorageFull : AuthoredAmountColor.Value);
        }

        private void SetDerivedValueAmountText()
        {
            ResourceAmountText?.SetText(Localization.DisplayText_ResourceCountTotal(AlterableValueSystem.GetAlterableValueCurrentVal(DerivedValueName)));
        }
    }
}
