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
        public Func<GameState, double> TaskSpeedFunc = null;
        public double ProgressSpeed = 0.0;
        public double ProgressCurrent = 0.0;
        public double ProgressMaximum = 1000.0;
        public int ProgressPercent = 0;
        public List<ResourceAmountData> ResourceChange = new();
        public int WorkersAssigned = 0;
    }

    public static class TaskActionSystem
    {
        public static string PrimaryTask = "";
        public static List<string> ActionsList = new();
        public static SerializableDictionary<string, string> UnlockToActionMap = new();
        public static SerializableDictionary<string, TaskActionState> TaskActionMap = new();
        public static SerializableDictionary<string, Action<int>> TaskUpdateCallbackMap = new();
        public static SerializableDictionary<string, Action> TaskFinishCallbackMap = new();
        private static GameState CurrentGameState = null;

        public static void SetGameState(GameState gameState) { CurrentGameState = gameState; }

        public static bool AddWorkerToTask(string taskName)
        {
            if (!TaskActionMap.ContainsKey(taskName))
                return false;

            var taskAction = TaskActionMap[taskName];
            taskAction.WorkersAssigned += 1;
            UpdateTaskProgressSpeed(taskName);
            return taskAction.WorkersAssigned < 10; // TODO: Pull from a system that determines the maximum number of workers on this task
        }

        public static bool RemoveWorkerFromTask(string taskName)
        {
            if (!TaskActionMap.ContainsKey(taskName))
                return false;

            var taskAction = TaskActionMap[taskName];
            taskAction.WorkersAssigned -= 1;
            UpdateTaskProgressSpeed(taskName);
            return taskAction.WorkersAssigned > 0;
        }

        public static void SetPrimaryTask(string taskName)
        {
            string currentPrimary = PrimaryTask;
            PrimaryTask = "";
            //if (!string.IsNullOrEmpty(currentPrimary) && TaskActionMap.ContainsKey(currentPrimary))
            //    UpdateTaskProgressSpeed(currentPrimary);
            
            PrimaryTask = taskName;
            //if (!string.IsNullOrEmpty(taskName) && TaskActionMap.ContainsKey(taskName))
            //    UpdateTaskProgressSpeed(taskName);
        }

        public static void UpdateTaskProgressSpeed(string taskName)
        {
            if (!TaskActionMap.ContainsKey(taskName)) return;
            TaskActionState taskAction = TaskActionMap[taskName];
            taskAction.ProgressSpeed = 0.0;
            if (taskName == PrimaryTask)
                taskAction.ProgressSpeed = taskAction.TaskSpeedFunc(CurrentGameState);
            taskAction.ProgressSpeed += ((double)taskAction.WorkersAssigned * 10.0);
        }

        public static void SetTaskUpdateCallback(string taskName, Action<int> updateCallback = null) { TaskUpdateCallbackMap[taskName] = updateCallback; }

        public static void SetTaskFinishCallback(string taskName, Action finishCallback = null) { TaskFinishCallbackMap[taskName] = finishCallback; }

        public static void Update()
        {
            foreach (var taskAction in TaskActionMap.Values)
            {
                UpdateTaskProgressSpeed(taskAction.Name);
                if (taskAction.ProgressSpeed == 0.0 && taskAction.ProgressCurrent == 0.0)
                    continue;

                taskAction.ProgressCurrent = Math.Clamp(taskAction.ProgressCurrent + taskAction.ProgressSpeed * Time.deltaTime, 0, taskAction.ProgressMaximum);
                int percent = (int)(taskAction.ProgressCurrent / taskAction.ProgressMaximum * 100f);

                List<ResourceAmountData> gainChange = taskAction.ResourceChange.Where(rc => rc.ResourceValue > 0).ToList();
                List<ResourceAmountData> lossChange = taskAction.ResourceChange.Where(rc => rc.ResourceValue < 0).ToList();
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
            if (File.Exists(jsonFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    var taskData = JsonSerialization.Deserialize<TaskUnlockAndActionData>(jsonContent);

                    UnlockToActionMap.Clear();
                    foreach (var uta in taskData.UnlockToActionMap) UnlockToActionMap[uta.Unlock] = uta.Action;

                    ActionsList.Clear();
                    TaskActionMap.Clear();
                    foreach (TaskActionData tad in taskData.ActionData)
                    {
                        ActionsList.Add(tad.Name);
                        TaskActionMap[tad.Name] = new TaskActionState()
                        {
                            Name = tad.Name,
                            DisplayName = tad.DisplayName,
                            ActionSection = tad.ActionSection,
                            TaskSpeedFunc = tad.ValueDeterminant.GetSpeed,
                            ResourceChange = tad.ResourceChange
                        };
                    }

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