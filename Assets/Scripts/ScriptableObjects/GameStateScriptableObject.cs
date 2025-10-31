using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Sleepwalking
{
    // Use the CreateAssetMenu attribute to allow creating instances of this ScriptableObject from the Unity Editor.

    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/GameStateScriptableObject", order = 1)]
    [Serializable]
    public class GameStateScriptableObject : ScriptableObject
    {
        public GameState GameState;
    }
}