using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HypnicEmpire
{
    public class UILevelExplorationBar : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI LevelExplorationText;
        [SerializeField] public Image ProgressImage;
        
        public void SetProgress(float percentage)
        {
            LevelExplorationText?.SetText(Localization.DisplayText_LevelExplorationPercent(Mathf.CeilToInt(percentage * 100f)));
            ProgressImage?.SetFillAmount(percentage);
        }
    }
}