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

        public GameLevelData GetLevel(int index)
        {
            if (index < 0 || GameLevels == null || index >= GameLevels.Count) return null;
            return GameLevels[index];
        }
    }
}