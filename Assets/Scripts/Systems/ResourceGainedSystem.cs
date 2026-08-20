using System.Collections.Generic;

namespace HypnicEmpire
{
    //  How much of a resource the player has ever earned, as opposed to how much is held right now. Spending
    //  or capping out must never walk a lifetime threshold back, so the two are counted apart: the held amount
    //  drives ResourceTypeData.Unlocks, and the earned total drives the "ResourceGained_<Resource>"
    //  AlterableValue, where the thresholds that unlock on it are authored. A resource with no such value
    //  authored has nothing to announce and is simply passed over.
    public static class ResourceGainedSystem
    {
        private const string ValueNamePrefix = "ResourceGained_";

        public static string GetValueName(string resourceType) => ValueNamePrefix + resourceType;

        //  Only the whole part is published: the thresholds are authored as whole amounts, and the total keeps
        //  its fractions so that repeated part-resource gains still add up to the next one.
        public static void Publish(string resourceType, ResourceValue total)
        {
            if (total == null) return;
            if (!AlterableValueSystem.TryGetAlterableValue(GetValueName(resourceType), out var alterableValue)) return;

            alterableValue.SetValue((int)total.WholeValue);
        }

        //  Republishes every resource, including the ones the totals hold no entry for, so that a load or a
        //  reset cannot leave the previous run's tally standing in a value nothing has written to since.
        public static void PublishAll(IDictionary<string, ResourceValue> totals)
        {
            foreach (var resourceType in ResourceTypeSystem.ResourceTypes)
                Publish(resourceType, totals != null && totals.TryGetValue(resourceType, out var total) ? total : 0);
        }
    }
}
