using System;
using System.Collections.Generic;

namespace HypnicEmpire
{
    //  Putting people to work. Two separate limits decide whether an action can take another worker.
    //
    //  The first is the settlement's population. People are accumulated from housing rather than held in
    //  the game state, and nobody works two jobs at once, so what is free to assign anywhere is the
    //  population less everyone already at work.
    //
    //  The second is the job section, which owns a number of jobs authored as MaxDelvingJobs,
    //  MaxCommercialJobs and the like and raised by the buildings that provide the workplaces. A section
    //  shares that cap between every action in it, so a villager sent to chop wood is a job that cutting
    //  stone can no longer fill. The section an action draws on is not always its ActionSection: see
    //  ModifierValueSystem.SectionForAction, which splits farming out of agriculture and gathers delving.
    public static class JobAssignmentSystem
    {
        //  The population, accumulated into this value from whatever housing stands.
        public const string PeopleValueName = "People";

        //  Raised after any assignment settles. A cap belongs to a section rather than an action, so a
        //  change to one action alters what its siblings are allowed and every worker display has to be
        //  re-read, not only the one that was clicked.
        public static event Action OnAssignmentsChanged;

        public static int PeopleTotal => AlterableValueSystem.GetAlterableValueCurrentVal(PeopleValueName);

        public static int PeopleAtWork
        {
            get
            {
                int atWork = 0;
                foreach (var taskAction in TaskActionSystem.TaskActionMap.Values)
                    atWork += taskAction.WorkersAssigned;

                return atWork;
            }
        }

        public static int PeopleIdle => Math.Max(0, PeopleTotal - PeopleAtWork);

        public static int AssignedToAction(string actionName) => TaskActionSystem.GetWorkersAssigned(actionName);

        //  The cap an action is measured against, which it shares with the rest of its job section. This is
        //  the figure worth showing beside the count: it is the ceiling the player is working towards.
        public static int JobCapOfAction(string actionName)
        {
            string jobSection = JobSectionOfAction(actionName);
            return jobSection == null ? 0 : ModifierValueSystem.GetJobCap(jobSection);
        }

        public static string JobSectionOfAction(string actionName)
        {
            if (!TaskActionSystem.TaskActionMap.ContainsKey(actionName)) return null;

            var taskAction = TaskActionSystem.TaskActionMap[actionName];
            return ModifierValueSystem.SectionForAction(taskAction.Name, taskAction.ActionSection);
        }

        public static int JobsFilledInSection(string jobSection)
        {
            int filled = 0;
            foreach (var taskAction in TaskActionSystem.TaskActionMap.Values)
                if (ModifierValueSystem.SectionForAction(taskAction.Name, taskAction.ActionSection) == jobSection)
                    filled += taskAction.WorkersAssigned;

            return filled;
        }

        public static bool CanAssign(string actionName)
        {
            string jobSection = JobSectionOfAction(actionName);
            if (jobSection == null) return false;
            if (PeopleIdle <= 0) return false;

            return JobsFilledInSection(jobSection) < ModifierValueSystem.GetJobCap(jobSection);
        }

        public static bool CanUnassign(string actionName) => AssignedToAction(actionName) > 0;

        public static bool Assign(string actionName)
        {
            if (!CanAssign(actionName)) return false;

            TaskActionSystem.SetWorkersAssigned(actionName, AssignedToAction(actionName) + 1);
            OnAssignmentsChanged?.Invoke();
            return true;
        }

        public static bool Unassign(string actionName)
        {
            if (!CanUnassign(actionName)) return false;

            TaskActionSystem.SetWorkersAssigned(actionName, AssignedToAction(actionName) - 1);
            OnAssignmentsChanged?.Invoke();
            return true;
        }

        //  Assignments can outlive what justified them: a save records the workers of a run whose
        //  population and workplaces are restored separately, and nothing guarantees the two agree. Rather
        //  than refuse such a state, the excess is laid off — over-full sections first, then any workers the
        //  population as a whole can no longer account for.
        public static void ClampToAvailable()
        {
            bool changed = false;

            foreach (string jobSection in JobSections())
            {
                int over = JobsFilledInSection(jobSection) - ModifierValueSystem.GetJobCap(jobSection);
                if (over > 0)
                    changed |= LayOff(over, taskAction => ModifierValueSystem.SectionForAction(taskAction.Name, taskAction.ActionSection) == jobSection);
            }

            int beyondPopulation = PeopleAtWork - PeopleTotal;
            if (beyondPopulation > 0)
                changed |= LayOff(beyondPopulation, taskAction => true);

            if (changed) OnAssignmentsChanged?.Invoke();
        }

        //  Which action gives up a worker is arbitrary; only the totals are being brought back in line.
        private static bool LayOff(int count, Func<TaskActionState, bool> matches)
        {
            bool changed = false;

            foreach (var taskAction in TaskActionSystem.TaskActionMap.Values)
            {
                if (count <= 0) break;
                if (taskAction.WorkersAssigned <= 0 || !matches(taskAction)) continue;

                int layOff = Math.Min(count, taskAction.WorkersAssigned);
                TaskActionSystem.SetWorkersAssigned(taskAction.Name, taskAction.WorkersAssigned - layOff);
                count -= layOff;
                changed = true;
            }

            return changed;
        }

        private static HashSet<string> JobSections()
        {
            var jobSections = new HashSet<string>();
            foreach (var taskAction in TaskActionSystem.TaskActionMap.Values)
                jobSections.Add(ModifierValueSystem.SectionForAction(taskAction.Name, taskAction.ActionSection));

            return jobSections;
        }
    }
}
