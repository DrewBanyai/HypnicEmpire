using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace HypnicEmpire
{
    public class UIResourceChangeEntry : MonoBehaviour
    {
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
                ResourceNameText?.SetColor((changeAmount < 0) ? UIResourceColors.Loss : UIResourceColors.Gain);

                ResourceChangeText?.SetText(Localization.DisplayText_ResourceChangeDisplayAmount(changeAmount));
                ResourceChangeText?.SetOverrideColorTags(true);
                ResourceChangeText?.SetColor((changeAmount < 0) ? UIResourceColors.Loss : UIResourceColors.Gain);
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

        //  Colours a line whose affordability is no question about a held resource, and so cannot be
        //  weighed here: land is spent against what the buildings leave free rather than against a stock
        //  in the game state, and only the caller knows that.
        public void ShowAffordability(bool affordable)
        {
            Background?.SetActive(false);
            ResourceNameText?.SetColor(affordable ? UIResourceColors.Gain : UIResourceColors.Loss);
            ResourceChangeText?.SetColor(affordable ? UIResourceColors.Gain : UIResourceColors.Loss);
        }

        //  A reward line stands for something the player is about to receive, so once its store is full the
        //  number would be a promise the game cannot keep: the mark replaces it until there is room again.
        //  Cost lines are left alone, as a full store says nothing about whether a cost can be paid.
        public void ShowRewardStorageState()
        {
            if (ChangeResourceAmount == null || ChangeResourceAmount.ResourceValue <= 0) return;

            bool storageFull = ChangeResourceAmount.RewardStorageIsFull();

            ResourceNameText?.SetColor(storageFull ? UIResourceColors.StorageFull : UIResourceColors.Gain);

            ResourceChangeText?.SetColor(storageFull ? UIResourceColors.StorageFull : UIResourceColors.Gain);
            ResourceChangeText?.SetText(storageFull ? Localization.DisplayText_ResourceStorageFull()
                : Localization.DisplayText_ResourceChangeDisplayAmount(ChangeResourceAmount.ResourceValue));
        }

        public bool CheckCanChange(bool overrideNoBG = false, bool greenEvenNegative = false)
        {
            Background?.SetActive(!overrideNoBG);
            if (ChangeResourceAmount.ResourceValue == 0) return true;

            ResourceValue currentResourceAmount = GameController.CurrentGameState.GetResourceAmount(ChangeResourceAmount.ResourceType);
            ResourceValue maxResourceAmount = GameController.CurrentGameState.GetResourceMaxAmount(ChangeResourceAmount.ResourceType);

            if (ChangeResourceAmount.ResourceValue < 0)
            {
                if (currentResourceAmount >= ChangeResourceAmount.ResourceValue.Abs())
                {
                    ResourceNameText?.SetColor(greenEvenNegative ? UIResourceColors.Gain : UIResourceColors.Loss);
                    ResourceChangeText?.SetColor(greenEvenNegative ? UIResourceColors.Gain : UIResourceColors.Loss);
                    Background?.SetActive(false);
                    return true;
                }
            }
            else
            {
                if (maxResourceAmount - currentResourceAmount <= ChangeResourceAmount.ResourceValue)
                {
                    ResourceNameText?.SetColor(UIResourceColors.Gain);
                    ResourceChangeText?.SetColor(UIResourceColors.Gain);
                    Background?.SetActive(false);
                    return true;
                }
            }

            ResourceNameText?.SetColor(UIResourceColors.Loss);
            ResourceChangeText?.SetColor(UIResourceColors.Loss);
            Background?.SetActive(overrideNoBG ? false : true);
            return false;
        }
    }
}