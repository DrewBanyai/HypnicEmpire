using UnityEngine;
using System.Collections.Generic;

namespace HypnicEmpire
{
    public class UIBuildingsMenu : MonoBehaviour
    {
        [SerializeField] public List<UIBuildingButton> BuildingButtonObjects;

        public void InitializeMenu()
        {
            foreach (var button in BuildingButtonObjects)
            {
                if (button == null) continue;
                var data = BuildingDataSystem.GetBuildingData(button.BuildingName);
                button.SetBuildingData(data);
            }
        }
    }
}