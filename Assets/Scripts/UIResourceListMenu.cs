using System.Collections.Generic;
using UnityEngine;

namespace HypnicEmpire
{
    public class UIResourceListMenu : MonoBehaviour
    {
        [SerializeField] public GameObject ResourceEntryPrefab;
        [SerializeField] public Transform ResourceDisplayParent;

        private List<string> ResourcesTracked = new();

        //  Anything the list is asked to show is either an authored resource, held and spent out of the
        //  game state, or a tracked value accumulated from what has been built ("People" above all).
        //  The two read from different places and are displayed differently, so which one a name is has
        //  to be settled before the row is filled in - asking the game state for a value it has no entry
        //  for would otherwise fail outright.
        public void AddResourceEntry(string resourceType)
        {
            if (ResourcesTracked.Contains(resourceType)) return;

            bool isResource = ResourceTypeSystem.ResourceTypes.Contains(resourceType);
            bool isDerivedValue = !isResource && AlterableValueSystem.ValueMap.ContainsKey(resourceType);
            if (!isResource && !isDerivedValue)
            {
                Debug.LogWarning($"Resource list cannot show '{resourceType}': it is neither a resource type nor a tracked value.", this);
                return;
            }

            ResourcesTracked.Add(resourceType);

            if (ResourceEntryPrefab == null || ResourceDisplayParent == null) return;

            var entryObject = Instantiate(ResourceEntryPrefab, ResourceDisplayParent);
            var entryComponent = entryObject.GetComponent<UIResourceEntry>();
            if (entryComponent == null) return;

            if (isResource)
                entryComponent.SetContent(resourceType);
            else
                entryComponent.SetDerivedValueContent(resourceType);
        }

        public void ClearAllResourceEntries()
        {
            ResourcesTracked = new();
            foreach (Transform child in ResourceDisplayParent)
                Destroy(child.gameObject);
        }
    }
}
