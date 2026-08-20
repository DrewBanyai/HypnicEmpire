using UnityEngine;

namespace HypnicEmpire
{
    //  The colours the resource displays are drawn in. The rows of the resource list and the change lines
    //  that spend and reward against it both say something about the same stockpile, so what a colour means
    //  is settled in one place rather than once per display.
    public static class UIResourceColors
    {
        public static Color Loss => new Color32(169, 73, 73, 255);
        public static Color LossDisabledBackground => new Color32(53, 53, 64, 255);
        public static Color LossDisabled => new Color32(237, 228, 218, 255);
        public static Color Gain => new Color32(62, 85, 76, 255);
        public static Color StorageFull => new Color32(148, 122, 157, 255);
    }
}
