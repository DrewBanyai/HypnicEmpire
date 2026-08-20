using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

namespace HypnicEmpire
{
    public class TaskActionState
    {
        public string Name;
        public string DisplayName;
        public string ActionSection;
        public TaskActionValueDeterminant ValueDeterminant = null;
        public ActionTimingData Timing;
        public double ProgressSpeed = 0.0;
        public double ProgressCurrent = 0.0;
        public int ProgressPercent = 0;
        public List<ResourceAmountData> ResourceChange = new();
        public int WorkersAssigned = 0;
        public bool ProducesResources = true;

        public double ProgressMaximum => Timing?.ProgressMaximum ?? 1.0;

        public List<ResourceAmountData> GetResourceChange()
        {
            // Apply GainPct modifiers (building/project effects) to the reward. Positive
            // (gained) amounts are scaled; costs/losses are left untouched.
            // A missing ValueDeterminant (incomplete JSON) means no unlock alterations to apply.
            List<ResourceAmountData> baseChange = ValueDeterminant != null
                ? ValueDeterminant.GetResourceChange(ResourceChange)
                : TaskActionValueDeterminant.CopyResourceChange(ResourceChange);
            return ModifierValueSystem.ApplyGain(Name, ActionSection, baseChange);
        }

        public double GetSpeed()
        {
            return Timing?.CalculatePlayerProgressPerSecond(
                GameUnlockSystem.IsUnlocked,
                valueName => AlterableValueSystem.GetAlterableValueCurrentVal(valueName)) ?? 0.0;
        }
    }

    public static class TaskActionSystem
    {
        public static string PrimaryTask = "";
        public static List<string> ActionsList = new();
        public static SerializableDictionary<string, string> UnlockToActionMap = new();
        public static SerializableDictionary<string, TaskActionState> TaskActionMap = new();
        public static SerializableDictionary<string, Action<int>> TaskUpdateCallbackMap = new();
        public static SerializableDictionary<string, Action> TaskFinishCallbackMap = new();

        public static int GetWorkersAssigned(string taskName)
        {
            return TaskActionMap.ContainsKey(taskName) ? TaskActionMap[taskName].WorkersAssigned : 0;
        }

        //  Whether a task is allowed the workers asked for is JobAssignmentSystem's to judge, weighing the
        //  population and the section's shared jobs. All that is owed here is the count and the speed it
        //  buys, so this stores what it is given and never refuses.
        public static void SetWorkersAssigned(string taskName, int workers)
        {
            if (!TaskActionMap.ContainsKey(taskName)) return;

            TaskActionMap[taskName].WorkersAssigned = Math.Max(0, workers);
            UpdateTaskProgressSpeed(taskName);
        }

        public static SerializableDictionary<string, int> GetAllWorkersAssigned()
        {
            var assignments = new SerializableDictionary<string, int>();
            foreach (var taskAction in TaskActionMap.Values)
                if (taskAction.WorkersAssigned > 0)
                    assignments[taskAction.Name] = taskAction.WorkersAssigned;

            return assignments;
        }

        public static void ClearAllWorkersAssigned()
        {
            foreach (var taskAction in TaskActionMap.Values)
                taskAction.WorkersAssigned = 0;

            foreach (var taskAction in TaskActionMap.Values)
                UpdateTaskProgressSpeed(taskAction.Name);
        }

        //  Raised once an action's cost and reward have been replaced, carrying that action's name. Most
        //  actions are priced by their own authored data and never raise this; delving is priced by the level
        //  being delved and so is repriced as the player descends.
        public static event Action<string> OnActionResourceChangeReplaced;

        //  The authored ResourceChange is a starting price rather than a fixed one, so an action priced by
        //  something outside its own data can hand that price over here. Unlock alterations and modifiers are
        //  applied on top of whatever is set, exactly as they are for authored values.
        public static void SetActionResourceChange(string taskName, List<ResourceAmountData> resourceChange)
        {
            if (!TaskActionMap.ContainsKey(taskName)) return;

            TaskActionMap[taskName].ResourceChange = resourceChange ?? new List<ResourceAmountData>();
            OnActionResourceChangeReplaced?.Invoke(taskName);
        }

        //  Raised once the task the player is putting their own effort into settles, carrying the new task's
        //  name or an empty string when none is chosen. Choosing one action drops whatever was chosen
        //  before, so anything marking the choice has to hear about tasks other than its own.
        public static event Action<string> OnPrimaryTaskChanged;

        public static void SetPrimaryTask(string taskName)
        {
            if (PrimaryTask == taskName) return;

            string currentPrimary = PrimaryTask;
            PrimaryTask = "";
            //if (!string.IsNullOrEmpty(currentPrimary) && TaskActionMap.ContainsKey(currentPrimary))
            //    UpdateTaskProgressSpeed(currentPrimary);
            
            PrimaryTask = taskName;
            //if (!string.IsNullOrEmpty(taskName) && TaskActionMap.ContainsKey(taskName))
            //    UpdateTaskProgressSpeed(taskName);

            OnPrimaryTaskChanged?.Invoke(PrimaryTask);
        }

        public static void UpdateTaskProgressSpeed(string taskName)
        {
            if (!TaskActionMap.ContainsKey(taskName)) return;
            TaskActionState taskAction = TaskActionMap[taskName];
            taskAction.ProgressSpeed = 0.0;
            if (taskName == PrimaryTask)
                taskAction.ProgressSpeed = taskAction.GetSpeed();
            taskAction.ProgressSpeed += taskAction.WorkersAssigned * taskAction.Timing.ProgressPerWorkerPerSecond;
            // Apply SpeedPct modifiers (building/project effects) to the whole task speed.
            taskAction.ProgressSpeed *= ModifierValueSystem.GetActionSpeedMultiplier(taskAction.Name, taskAction.ActionSection);
        }

        public static void SetTaskUpdateCallback(string taskName, Action<int> updateCallback = null) { TaskUpdateCallbackMap[taskName] = updateCallback; }

        public static void SetTaskFinishCallback(string taskName, Action finishCallback = null) { TaskFinishCallbackMap[taskName] = finishCallback; }

        public static void Update()
        {
            if (ActionTimingSystem.Configuration == null) return;

            double deltaTime = Time.deltaTime * ActionTimingSystem.Configuration.TimeScale;

            foreach (var taskAction in TaskActionMap.Values)
            {
                UpdateTaskProgressSpeed(taskAction.Name);
                if (taskAction.ProgressSpeed == 0.0 && taskAction.ProgressCurrent == 0.0)
                    continue;

                taskAction.ProgressCurrent = Math.Clamp(taskAction.ProgressCurrent + taskAction.ProgressSpeed * deltaTime, 0, taskAction.ProgressMaximum);
                int percent = (int)(taskAction.ProgressCurrent / taskAction.ProgressMaximum * 100f);

                var actionResourceChange = taskAction.GetResourceChange();
                List<ResourceAmountData> gainChange = actionResourceChange.Where(rc => rc.ResourceValue > 0).ToList();
                List<ResourceAmountData> lossChange = actionResourceChange.Where(rc => rc.ResourceValue < 0).ToList();
                bool canChange = gainChange.CheckCanChangeAny(true) && lossChange.CheckCanChangeAll();
                if (!canChange)
                {
                    taskAction.ProgressCurrent = 0.0;
                    percent = 0;
                }

                if (percent != taskAction.ProgressPercent && TaskUpdateCallbackMap.ContainsKey(taskAction.Name))
                {
                    taskAction.ProgressPercent = percent;
                    if (taskAction.ProgressPercent == 100 && TaskFinishCallbackMap.ContainsKey(taskAction.Name))
                    {
                        TaskFinishCallbackMap[taskAction.Name]?.Invoke();
                        taskAction.ProgressPercent = 0;
                        taskAction.ProgressCurrent = 0.0;
                    }
                    else
                        TaskUpdateCallbackMap[taskAction.Name]?.Invoke(taskAction.ProgressPercent);
                }
            }
        }

        public static void LoadAllTaskActions(string jsonFilePath)
        {
            if (ActionTimingSystem.Configuration == null)
            {
                Debug.LogError("Cannot load task actions before the action timing configuration");
                return;
            }

            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    var taskData = JsonSerialization.Deserialize<TaskUnlockAndActionData>(jsonContent);
                    if (taskData == null)
                    {
                        Debug.LogError($"Failed to deserialize TaskActionStates from {jsonFilePath}");
                        return;
                    }

                    UnlockToActionMap.Clear();
                    if (taskData.UnlockToActionMap != null)
                        foreach (var uta in taskData.UnlockToActionMap) UnlockToActionMap[uta.Key] = uta.Value;

                    ActionsList.Clear();
                    TaskActionMap.Clear();
                    foreach (TaskActionData tad in taskData.ActionData ?? new List<TaskActionData>())
                    {
                        if (tad == null || string.IsNullOrEmpty(tad.Name))
                        {
                            Debug.LogWarning($"Skipping task action with no Name in {jsonFilePath}");
                            continue;
                        }

                        if (!ActionTimingSystem.TryGetAction(tad.Name, out ActionTimingData timing))
                        {
                            Debug.LogError($"Skipping task action '{tad.Name}' because it has no timing configuration");
                            continue;
                        }

                        ActionsList.Add(tad.Name);
                        TaskActionMap[tad.Name] = new TaskActionState()
                        {
                            Name = tad.Name,
                            DisplayName = tad.DisplayName,
                            ActionSection = tad.ActionSection,
                            ValueDeterminant = tad.ValueDeterminant,
                            Timing = timing,
                            ResourceChange = tad.ResourceChange ?? new List<ResourceAmountData>(),
                            ProducesResources = tad.ProducesResources
                        };
                    }

                    if (TaskActionMap.Count != ActionTimingSystem.Configuration.Actions.Count)
                        Debug.LogWarning(
                            $"Loaded {TaskActionMap.Count} task actions but found " +
                            $"{ActionTimingSystem.Configuration.Actions.Count} action timing entries");

                    Debug.Log($"Successfully loaded {TaskActionMap.Count} TaskActionStates from {jsonFilePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading TaskActionStates from {jsonFilePath}: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"TaskActions.json not found at {jsonFilePath}");
            }
        }
    }
}