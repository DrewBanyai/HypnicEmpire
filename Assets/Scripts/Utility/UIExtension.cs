using TMPro;

namespace HypnicEmpire
{
    public static class UIExtension
    {
        //  TextMeshProUGUI
        public static void SetText(this TextMeshProUGUI textObj, string text) { textObj.text = text; }
        public static void SetOverrideColorTags(this TextMeshProUGUI textObj, bool overrideColorTags) { textObj.overrideColorTags = overrideColorTags; }
        public static void SetColor(this TextMeshProUGUI textObj, Color color) { textObj.color = color; }

        //  Image
        public static void SetFillAmount(this Image image, float fillAmount) { image.fillAmount = fillAmount; }
        public static void SetSprite(this Image image, Sprite sprite) { image.sprite = sprite; }

        //  Button
        public static void SetInteractable(this Button button, bool interactable) { button.interactable = interactable; }
    }
}