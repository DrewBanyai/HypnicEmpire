using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HypnicEmpire
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public class AchievementsCollection : MonoBehaviour
    {
        [SerializeField] public GameObject AchievementDisplayPrefab;
        [SerializeField] public Transform DisplayParent;

        public void LinkAchievementUI()
        {
            //  Clear existing displays
            Transform parent = DisplayParent != null ? DisplayParent : transform;
            foreach (Transform child in parent)
            {
                Destroy(child.gameObject);
            }

            //  Instantiate a display for every achievement in the data map
            foreach (var data in AchievementsSystem.AchievementDataMap.Values)
            {
                if (AchievementDisplayPrefab != null)
                {
                    var entryObject = Instantiate(AchievementDisplayPrefab, parent);
                    var display = entryObject.GetComponent<UIAchievementDisplay>();
                    if (display != null)
                    {
                        display.SetData(data);
                        display.RefreshUnlockState();
                    }
                }
            }
        }
    }
}
