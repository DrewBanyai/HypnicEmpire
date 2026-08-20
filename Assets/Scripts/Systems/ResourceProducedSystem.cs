using System.Collections.Generic;

namespace HypnicEmpire
{
    //  How much of a resource the player has ever produced by an action, as opposed to how much they have
    //  merely acquired. Delving can hand over wood (and other finds) without producing them; those gains
    //  still count toward ResourceGained, but they must never walk a production threshold. The two are
    //  counted apart so a "produce your first X" unlock can wait for the action that actually makes it.
    public static class ResourceProducedSystem
    {
        private const string ValueNamePrefix = "ResourceProduced_";

        public static string GetValueName(string resourceType) => ValueNamePrefix + resourceType;

        public static void Publish(string resourceType, ResourceValue total)
        {
            if (total == null) return;
            if (!AlterableValueSystem.TryGetAlterableValue(GetValueName(resourceType), out var alterableValue)) return;

            alterableValue.SetValue((int)total.WholeValue);
        }

        public static void PublishAll(IDictionary<string, ResourceValue> totals)
        {
            foreach (var resourceType in ResourceTypeSystem.ResourceTypes)
                Publish(resourceType, totals != null && totals.TryGetValue(resourceType, out var total) ? total : 0);
        }
    }
}
