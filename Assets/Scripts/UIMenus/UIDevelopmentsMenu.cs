using UnityEngine;

namespace HypnicEmpire
{
    public class UIDevelopmentsMenu : MonoBehaviour
    {
        [SerializeField] public GameObject UIDevelopmentEntryPrefab;
        [SerializeField] public Transform OpenDevelopmentsListParent;
        [SerializeField] public Transform FinishedDevelopmentsListParent;

        public void ClearDevelopmentMenu()
        {
            if (OpenDevelopmentsListParent != null)
                foreach (Transform child in OpenDevelopmentsListParent)
                    Destroy(child.gameObject);

            if (FinishedDevelopmentsListParent != null)
                foreach (Transform child in FinishedDevelopmentsListParent)
                    Destroy(child.gameObject);
        }

        public void AddOpenDevelopment(string name, string description, string extraInfo)
        {
            if (UIDevelopmentEntryPrefab != null && OpenDevelopmentsListParent != null)
            {
                var entryObject = Instantiate(UIDevelopmentEntryPrefab, OpenDevelopmentsListParent);
                var entryComponent = entryObject.GetComponent<UIDevelopmentEntry>();
                entryComponent?.SetContent(name, description, extraInfo);
            }
        }
    }
}