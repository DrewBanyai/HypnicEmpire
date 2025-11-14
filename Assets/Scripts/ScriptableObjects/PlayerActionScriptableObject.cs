using UnityEngine;
using System;
using System.Collections.Generic;

namespace HypnicEmpire
{
    // Use the CreateAssetMenu attribute to allow creating instances of this ScriptableObject from the Unity Editor.

    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PlayerActionScriptableObject", order = 1)]
    [Serializable]
    public class PlayerActionScriptableObject : ScriptableObject
    {
        public List<PlayerActionData> PlayerActions = new();

        public SerializableDictionary<GameUnlock, PlayerActionType> UnlockToActionMap = new();
    }
}