using UnityEngine;
using System;
using System.Collections.Generic;

namespace HypnicEmpire
{
    // Use the CreateAssetMenu attribute to allow creating instances of this ScriptableObject from the Unity Editor.

    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/DevelopmentsScriptableObject", order = 1)]
    [Serializable]
    public class DevelopmentsScriptableObject : ScriptableObject
    {
        public List<DevelopmentData> Developments;
    }
}