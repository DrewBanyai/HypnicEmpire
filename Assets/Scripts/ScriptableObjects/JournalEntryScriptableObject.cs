using UnityEngine;
using System;
using System.Collections.Generic;

namespace HypnicEmpire
{
    // Use the CreateAssetMenu attribute to allow creating instances of this ScriptableObject from the Unity Editor.

    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/JournalEntryScriptableObject", order = 1)]
    [Serializable]
    public class JournalEntryScriptableObject : ScriptableObject
    {
        public SerializableDictionary<GameUnlock, string> JournalEntries;
    }
}