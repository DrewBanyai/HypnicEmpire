using UnityEditor;
using UnityEngine;
using HypnicEmpire;

namespace HypnicEmpire.Editor
{
    [CustomEditor(typeof(UIBuildingButton))]
    public class UIBuildingButtonEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            UIBuildingButton button = (UIBuildingButton)target;

            GUILayout.Space(10);
            if (GUILayout.Button("Load Building Data from JSON"))
            {
                AssignBuildingData(button);
            }
        }

        private void AssignBuildingData(UIBuildingButton button)
        {
            // Ensure data is loaded
            BuildingDataSystem.LoadAllBuildingsData(Application.dataPath + "/GameData/Buildings.json");

            BuildingData data = BuildingDataSystem.GetBuildingData(button.BuildingName);
            if (data != null)
            {
                Undo.RecordObject(button, "Assign Building Data");
                
                // We need to also record potentially modified child objects (TMP texts, Images)
                if (button.BuildingTitleText != null) Undo.RecordObject(button.BuildingTitleText, "Assign Building Data");
                if (button.BuildingDescriptionText != null) Undo.RecordObject(button.BuildingDescriptionText, "Assign Building Data");
                if (button.BuildingIconImage != null) Undo.RecordObject(button.BuildingIconImage, "Assign Building Data");
                if (button.BuildingEffectText != null) Undo.RecordObject(button.BuildingEffectText, "Assign Building Data");

                button.SetBuildingData(data);
                
                EditorUtility.SetDirty(button);
                if (button.BuildingTitleText != null) EditorUtility.SetDirty(button.BuildingTitleText);
                if (button.BuildingDescriptionText != null) EditorUtility.SetDirty(button.BuildingDescriptionText);
                if (button.BuildingIconImage != null) EditorUtility.SetDirty(button.BuildingIconImage);
                if (button.BuildingEffectText != null) EditorUtility.SetDirty(button.BuildingEffectText);

                Debug.Log($"Successfully assigned data for building: {button.BuildingName}");
            }
            else
            {
                Debug.LogError($"Could not find building data for: {button.BuildingName}");
            }
        }
    }
}
