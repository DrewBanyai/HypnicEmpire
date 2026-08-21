using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace HypnicEmpire
{
    public enum GameAutomationPrerequisiteType
    {
        ResourceAmount,
        CurrentLevelDelveCount,
        TotalDelveCount,
        CurrentLevel,
        DeepestLevelReached
    }

    //  How the tracked amount is weighed against the authored figure. AtLeast is first so that
    //  prerequisites authored before there was a choice keep the meaning they were written with.
    public enum GameAutomationComparison
    {
        AtLeast,
        AtMost,
        Exactly
    }

    [Serializable]
    public sealed class GameAutomationPrerequisite
    {
        [SerializeField] private GameAutomationPrerequisiteType type;
        [SerializeField] private string resourceId;
        [SerializeField] private GameAutomationComparison comparison;
        [FormerlySerializedAs("minimumValue")]
        [SerializeField, Min(0f)] private double targetValue;

        public GameAutomationPrerequisiteType Type => type;
        public string ResourceId => resourceId;
        public GameAutomationComparison Comparison => comparison;
        public double TargetValue => targetValue;
    }

    [Serializable]
    public sealed class GameAutomationStep
    {
        [Tooltip("Stable button ID, such as Forage, Delve, Actions, or Developments.")]
        [SerializeField] private string buttonId;
        [SerializeField] private List<GameAutomationPrerequisite> prerequisites = new();

        public string ButtonId => buttonId;
        public IReadOnlyList<GameAutomationPrerequisite> Prerequisites
        {
            get
            {
                if (prerequisites != null)
                    return prerequisites;

                return Array.Empty<GameAutomationPrerequisite>();
            }
        }
    }

    [CreateAssetMenu(
        fileName = "GameAutomationSequence",
        menuName = "ScriptableObjects/Game Automation Sequence",
        order = 2)]
    public sealed class GameAutomationSequence : ScriptableObject
    {
        [SerializeField] private bool active;
        [SerializeField, Min(0f)]
        [Tooltip("Seconds to wait after a step's prerequisites are met, before clicking its button. Also used between retries when the button is not yet available.")]
        private float stepAttemptInterval = 0.5f;
        [SerializeField] private List<GameAutomationStep> steps = new();

        public bool Active => active;
        public float StepAttemptInterval => Mathf.Max(0f, stepAttemptInterval);
        public IReadOnlyList<GameAutomationStep> Steps
        {
            get
            {
                if (steps != null)
                    return steps;

                return Array.Empty<GameAutomationStep>();
            }
        }
    }
}
