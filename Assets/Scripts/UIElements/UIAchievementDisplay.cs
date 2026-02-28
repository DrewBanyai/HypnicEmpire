using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace HypnicEmpire
{
    public class UIAchievementDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] public Image Icon;
        [SerializeField] public string AchievementName;
        
        private bool _unlocked;
        public bool Unlocked 
        { 
            get => _unlocked; 
            set 
            { 
                _unlocked = value; 
                UpdateDisplay(); 
            } 
        }

        private AchievementData _data;

        public void SetContent(AchievementData data, bool unlocked)
        {
            _data = data;
            AchievementName = data.Name;
            Unlocked = unlocked;
        }

        public void SetData(AchievementData data)
        {
            _data = data;
        }

        public void RefreshUnlockState()
        {
            if (_data == null) return;
            Unlocked = AchievementsSystem.UnlockedAchievements.Contains(_data.Trigger);
        }

        private void UpdateDisplay()
        {
            if (Icon != null)
            {
                //  Set the sprite
                if (Unlocked)
                {
                    if (_data != null && _data.ImageSprite != null)
                        Icon.sprite = _data.ImageSprite;
                }
                else
                {
                    //  Show the unknown icon if locked
                    Icon.sprite = Resources.Load<Sprite>("AchievementIcons/Achievement_Unknown");
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_data == null) return;
            RefreshUnlockState();

            string tooltipText = Unlocked ? _data.Text : _data.Hint;
            if (string.IsNullOrEmpty(tooltipText)) tooltipText = Unlocked ? "Achievement Unlocked!" : "Hidden Achievement";

            UITooltip.Show(_data.Name, tooltipText);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            UITooltip.Hide();
        }

        private void OnEnable()
        {
            AchievementsSystem.OnAchievementUnlocked += HandleAchievementUnlocked;
        }

        private void OnDisable()
        {
            AchievementsSystem.OnAchievementUnlocked -= HandleAchievementUnlocked;
        }

        private void HandleAchievementUnlocked(string trigger)
        {
            if (_data != null && _data.Trigger == trigger)
            {
                RefreshUnlockState();
            }
        }
    }
}
