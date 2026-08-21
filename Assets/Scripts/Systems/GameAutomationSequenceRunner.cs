using System;
using System.Collections.Generic;
using UnityEngine;

namespace HypnicEmpire
{
    public enum GameAutomationClickResult
    {
        Clicked,
        Unavailable,
        UnknownButton
    }

    public interface IGameAutomationButtonRouter
    {
        GameAutomationClickResult TryClick(string buttonId);
    }

    public interface IGameAutomationPrerequisiteEvaluator
    {
        bool TryIsMet(GameAutomationPrerequisite prerequisite, out bool isMet, out string error);
    }

    public sealed class GameAutomationPrerequisiteEvaluator : IGameAutomationPrerequisiteEvaluator
    {
        public bool TryIsMet(GameAutomationPrerequisite prerequisite, out bool isMet, out string error)
        {
            isMet = false;
            error = null;

            if (prerequisite == null)
            {
                error = "A prerequisite is null.";
                return false;
            }

            switch (prerequisite.Type)
            {
                case GameAutomationPrerequisiteType.ResourceAmount:
                    if (string.IsNullOrWhiteSpace(prerequisite.ResourceId))
                    {
                        error = "A resource prerequisite has no resource ID.";
                        return false;
                    }

                    //  Land is authored as a resource on buildings but never held in the resource list.
                    //  Free land is the amount that can still be spent, which is what a Land check means.
                    if (prerequisite.ResourceId == LandSystem.LandValueName)
                    {
                        return TryCompare(
                            prerequisite.Comparison,
                            new ResourceValue((long)LandSystem.LandFree),
                            new ResourceValue(prerequisite.TargetValue),
                            out isMet,
                            out error);
                    }

                    if (!ResourceTypeSystem.ResourceTypes.Contains(prerequisite.ResourceId))
                    {
                        error = $"Resource '{prerequisite.ResourceId}' does not exist.";
                        return false;
                    }

                    return TryCompare(
                        prerequisite.Comparison,
                        GameController.CurrentGameState.GetResourceAmount(prerequisite.ResourceId),
                        new ResourceValue(prerequisite.TargetValue),
                        out isMet,
                        out error);

                case GameAutomationPrerequisiteType.CurrentLevelDelveCount:
                    return TryCompare(prerequisite, GameController.CurrentGameState.LevelDelveCount.Value, out isMet, out error);

                case GameAutomationPrerequisiteType.TotalDelveCount:
                    return TryCompare(prerequisite, GameController.CurrentGameState.TotalDelves, out isMet, out error);

                case GameAutomationPrerequisiteType.CurrentLevel:
                    return TryCompare(prerequisite, GameController.CurrentGameState.LevelCurrent.Value, out isMet, out error);

                case GameAutomationPrerequisiteType.DeepestLevelReached:
                    return TryCompare(prerequisite, GameController.CurrentGameState.LevelReached.Value, out isMet, out error);

                default:
                    error = $"Unsupported prerequisite type '{prerequisite.Type}'.";
                    return false;
            }
        }

        //  Resources are held in hundredths, so they are weighed as ResourceValues rather than as doubles:
        //  an exact match has to answer to the stored figure rather than to a rounding of it.
        private static bool TryCompare(
            GameAutomationComparison comparison,
            ResourceValue actual,
            ResourceValue target,
            out bool isMet,
            out string error)
        {
            isMet = false;
            error = null;

            switch (comparison)
            {
                case GameAutomationComparison.AtLeast: isMet = actual >= target; return true;
                case GameAutomationComparison.AtMost: isMet = actual <= target; return true;
                case GameAutomationComparison.Exactly: isMet = actual == target; return true;
                default:
                    error = $"Unsupported comparison '{comparison}'.";
                    return false;
            }
        }

        private static bool TryCompare(
            GameAutomationPrerequisite prerequisite,
            double actual,
            out bool isMet,
            out string error)
        {
            isMet = false;
            error = null;

            switch (prerequisite.Comparison)
            {
                case GameAutomationComparison.AtLeast: isMet = actual >= prerequisite.TargetValue; return true;
                case GameAutomationComparison.AtMost: isMet = actual <= prerequisite.TargetValue; return true;
                case GameAutomationComparison.Exactly: isMet = actual == prerequisite.TargetValue; return true;
                default:
                    error = $"Unsupported comparison '{prerequisite.Comparison}'.";
                    return false;
            }
        }
    }

    public sealed class GameAutomationSequenceRunner
    {
        private readonly IReadOnlyList<GameAutomationStep> steps;
        private readonly IGameAutomationPrerequisiteEvaluator prerequisiteEvaluator;
        private readonly IGameAutomationButtonRouter buttonRouter;
        private readonly float stepAttemptInterval;
        private float waitRemaining;
        private bool waitingToClick;

        public int CurrentStepIndex { get; private set; }
        public bool IsRunning { get; private set; }
        public bool HasFailed { get; private set; }

        public GameAutomationSequenceRunner(
            IReadOnlyList<GameAutomationStep> steps,
            IGameAutomationPrerequisiteEvaluator prerequisiteEvaluator,
            IGameAutomationButtonRouter buttonRouter,
            float stepAttemptInterval)
        {
            this.steps = steps ?? throw new ArgumentNullException(nameof(steps));
            this.prerequisiteEvaluator = prerequisiteEvaluator ?? throw new ArgumentNullException(nameof(prerequisiteEvaluator));
            this.buttonRouter = buttonRouter ?? throw new ArgumentNullException(nameof(buttonRouter));
            this.stepAttemptInterval = Mathf.Max(0f, stepAttemptInterval);
            IsRunning = steps.Count > 0;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning || CurrentStepIndex >= steps.Count)
                return;

            if (!TryEvaluatePrerequisites(out bool prerequisitesMet))
                return;

            if (!prerequisitesMet)
            {
                waitingToClick = false;
                waitRemaining = 0f;
                return;
            }

            if (!waitingToClick)
            {
                waitingToClick = true;
                waitRemaining = stepAttemptInterval;
            }

            waitRemaining -= deltaTime;
            if (waitRemaining > 0f)
                return;

            waitRemaining = 0f;
            TryClickCurrentStep();
        }

        private bool TryEvaluatePrerequisites(out bool prerequisitesMet)
        {
            prerequisitesMet = false;

            GameAutomationStep step = steps[CurrentStepIndex];
            if (step == null)
            {
                Fail($"Step {CurrentStepIndex} is null.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(step.ButtonId))
            {
                Fail($"Step {CurrentStepIndex} has no button ID.");
                return false;
            }

            foreach (GameAutomationPrerequisite prerequisite in step.Prerequisites)
            {
                if (!prerequisiteEvaluator.TryIsMet(prerequisite, out bool isMet, out string error))
                {
                    Fail($"Step {CurrentStepIndex} ('{step.ButtonId}') has an invalid prerequisite: {error}");
                    return false;
                }

                if (!isMet)
                    return true;
            }

            prerequisitesMet = true;
            return true;
        }

        private void TryClickCurrentStep()
        {
            GameAutomationStep step = steps[CurrentStepIndex];

            switch (buttonRouter.TryClick(step.ButtonId))
            {
                case GameAutomationClickResult.Clicked:
                    Debug.Log($"Game automation completed step {CurrentStepIndex}: clicked '{step.ButtonId}'.");
                    CurrentStepIndex++;
                    waitingToClick = false;
                    waitRemaining = 0f;
                    if (CurrentStepIndex >= steps.Count)
                    {
                        IsRunning = false;
                        Debug.Log($"Game automation completed all {steps.Count} steps.");
                    }
                    break;

                case GameAutomationClickResult.Unavailable:
                    waitRemaining = stepAttemptInterval;
                    break;

                case GameAutomationClickResult.UnknownButton:
                    Fail($"Step {CurrentStepIndex} references unknown button ID '{step.ButtonId}'.");
                    break;
            }
        }

        private void Fail(string message)
        {
            HasFailed = true;
            IsRunning = false;
            Debug.LogError($"Game automation stopped: {message}");
        }
    }
}
