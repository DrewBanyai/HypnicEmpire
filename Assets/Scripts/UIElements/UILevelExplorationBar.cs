using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HypnicEmpire
{
    public class UILevelExplorationBar : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI LevelExplorationText;
        [SerializeField] public Image ProgressImage;

        //  Worn while the bar is full and waiting on the player rather than still filling. The fill alone
        //  cannot say as much: a bar stopped at its end looks exactly like one about to turn over.
        [SerializeField] public Color HeldProgressColor = new Color(0.5803922f, 0.47450984f, 0.6156863f, 1f);

        //  The authored fill colour, taken from the bar itself so that it is only ever stated in one place
        //  and can be given back once the bar is no longer held.
        private Color ProgressColor = Color.white;

        private void Awake()
        {
            if (ProgressImage != null) ProgressColor = ProgressImage.color;
        }

        public void SetProgress(float percentage, bool held)
        {
            float progress = Mathf.Clamp01(percentage);

            LevelExplorationText?.SetText(Localization.DisplayText_LevelExplorationPercent(Mathf.CeilToInt(progress * 100f)));
            ProgressImage?.SetFillAmount(progress);
            ProgressImage?.SetColor(held ? HeldProgressColor : ProgressColor);
        }
    }
}
