using UnityEngine;
using System;
using System.Collections.Generic;

namespace HypnicEmpire
{
    [Serializable]
    public class SubscribableInt
    {
        [SerializeField] public int Value;
        private List<Action<int>> ValueChangeActions;
        public SubscribableInt() { Reset(); }
        public SubscribableInt(int value) { Value = value; Reset(); }
        public void Reset() { ValueChangeActions = new List<Action<int>>(); }
        public void Subscribe(Action<int> action) { ValueChangeActions.Add(action); }
        public void SetValue(int newValue) { Value = newValue; foreach (var action in ValueChangeActions) { action?.Invoke(Value); } }
    }
}