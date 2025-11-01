using System.Collections.Generic;
using UnityEngine;

namespace HypnicEmpire
{
    public class UIResourceListMenu : MonoBehaviour
    {
        [SerializeField] public GameObject ResourceEntryPrefab;
        [SerializeField] public Transform ResourceDisplayParent;

        private List<ResourceType> ResourcesTracked = new();

        public void AddResourceEntry(ResourceType resourceType)
        {
            if (ResourcesTracked.Contains(resourceType)) return;
            ResourcesTracked.Add(resourceType);

            if (ResourceEntryPrefab != null && ResourceDisplayParent != null)
            {
                var entryObject = Instantiate(ResourceEntryPrefab, ResourceDisplayParent);
                var entryComponent = entryObject.GetComponent<UIResourceEntry>();
                if (entryComponent != null)
                    entryComponent.SetContent(resourceType);
            }
        }

        public void ClearAllResourceEntries()
        {
            ResourcesTracked = new();
            foreach (Transform child in ResourceDisplayParent)
                Destroy(child.gameObject);
        }
    }
}