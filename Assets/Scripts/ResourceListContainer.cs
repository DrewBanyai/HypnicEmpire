using System.Collections.Generic;
using UnityEngine;

namespace Sleepwalking
{
    public class ResourceListContainer : MonoBehaviour
    {
        [SerializeField] public GameObject ResourceEntryPrefab;

        private List<string> ResourcesTracked = new();

        public void AddResourceEntry(string resourceName)
        {
            if (ResourcesTracked.Contains(resourceName)) return;
            ResourcesTracked.Add(resourceName);

            if (ResourceEntryPrefab != null && this.gameObject.transform != null)
            {
                var entryObject = Instantiate(ResourceEntryPrefab, this.gameObject.transform);
                var entryComponent = entryObject.GetComponent<UIResourceEntry>();
                if (entryComponent != null)
                {
                    entryComponent.SetContent(resourceName);
                }
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