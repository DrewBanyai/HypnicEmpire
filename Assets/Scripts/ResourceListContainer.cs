using System.Collections.Generic;
using UnityEngine;

namespace HypnicEmpire
{
    public class ResourceListContainer : MonoBehaviour
    {
        [SerializeField] public GameObject ResourceEntryPrefab;

        private List<ResourceType> ResourcesTracked = new();

        public void AddResourceEntry(ResourceType resourceType)
        {
            if (ResourcesTracked.Contains(resourceType)) return;
            ResourcesTracked.Add(resourceType);

            if (ResourceEntryPrefab != null && this.gameObject.transform != null)
            {
                var entryObject = Instantiate(ResourceEntryPrefab, this.gameObject.transform);
                var entryComponent = entryObject.GetComponent<UIResourceEntry>();
                if (entryComponent != null)
                    entryComponent.SetContent(resourceType);
            }
        }

        public void ClearAllResourceEntries()
        {
            ResourcesTracked = new();
            foreach (Transform child in this.gameObject.transform)
                Destroy(child.gameObject);
        }
    }
}