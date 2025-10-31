using UnityEngine;
using System.Collections.Generic;
using System;

namespace HypnicEmpire
{
    // Use the CreateAssetMenu attribute to allow creating instances of this ScriptableObject from the Unity Editor.
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/GameLevelsScriptableObject", order = 1)]
    [Serializable]
    public class GameLevelsScriptableObject : ScriptableObject
    {
        public List<GameLevelData> GameLevels;
    }
}