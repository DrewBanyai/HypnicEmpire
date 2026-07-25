// Assets/Scripts/PathViz/PathModel.cs
//
// Incremental Path Visualizer — normalized model (engine-free core)
// -----------------------------------------------------------------
// The game-agnostic representation the simulation and visualizer consume. A game
// plugs in by implementing IGameDataSource (see HypnicEmpireDataSource.cs); the
// simulation and UI never see any game's specific JSON schema. No UnityEngine /
// UnityEditor dependency, so this is reusable at runtime and portable.
//
using System.Collections.Generic;

namespace HypnicEmpire.PathViz
{
    /// <summary>What kind of thing a path option is (informational — for display/filtering).</summary>
    public enum OptionKind { Development, Project, Building, Battle, ArmyUnit, ThresholdUnlock, Action, Reach, Other }

    /// <summary>A resource (or alterable-value) delta attached to an option, as authored (negative = spent).</summary>
    public sealed class ResourceCost
    {
        public string Resource;        // ResourceType or AlterableValue id
        public ComputedValue Amount;   // full-precision; may be a literal or a formula

        public ResourceCost() { }
        public ResourceCost(string resource, ComputedValue amount) { Resource = resource; Amount = amount; }
    }

    /// <summary>
    /// A single acquirable/gating step in the game. Becomes available when ALL of
    /// RequiredUnlocks are granted (AND-semantics); acquiring it grants GrantedUnlocks.
    /// Costs are carried for display and the future quantitative pass; the structural
    /// pass uses only the unlock relationships.
    /// </summary>
    public sealed class PathOption
    {
        public string Id;                                       // unique within a model
        public OptionKind Kind;
        public string Display;
        public readonly List<string> RequiredUnlocks = new();   // AND: every one must be granted
        public readonly List<ResourceCost> Costs = new();
        public readonly List<string> GrantedUnlocks = new();
        public string SourceRef;                                // "file > entry", for tooltips / validation

        public override string ToString() => $"{Kind}:{Id} ({Display})";
    }

    /// <summary>
    /// The full normalized game. SeedUnlocks are what is true at game start (the true
    /// starting state). Unlocks that are consumed but granted by no option are resolved
    /// by the simulation as engine-provided (see PathSimulation), not here.
    /// </summary>
    public sealed class PathModel
    {
        public readonly List<PathOption> Options = new();
        public readonly HashSet<string> SeedUnlocks = new();     // the ONLY unlocks true at start (just Unlock_Game_Start)
        public readonly HashSet<string> Resources = new();       // declared ResourceType ids
        public readonly HashSet<string> AlterableValues = new(); // declared AlterableValue ids

        public IEnumerable<string> AllGrantedUnlocks()
        {
            foreach (var o in Options)
                foreach (var u in o.GrantedUnlocks)
                    yield return u;
        }

        public IEnumerable<string> AllRequiredUnlocks()
        {
            foreach (var o in Options)
                foreach (var u in o.RequiredUnlocks)
                    yield return u;
        }
    }

    /// <summary>The seam a game implements so the visualizer can load it. Build() is pure data → data.</summary>
    public interface IGameDataSource
    {
        PathModel Build();
    }
}
