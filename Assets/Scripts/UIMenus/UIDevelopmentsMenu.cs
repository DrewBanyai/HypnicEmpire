using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace HypnicEmpire
{
    public class UIDevelopmentsMenu : MonoBehaviour
    {
        [SerializeField] public GameObject UIDevelopmentEntryPrefab;
        [SerializeField] public Transform OpenDevelopmentsListParent;
        [SerializeField] public Transform FinishedDevelopmentsListParent;
        [SerializeField] public GameObject FinishedDevelopmentsSection;

        public void ClearDevelopmentMenu()
        {
            if (OpenDevelopmentsListParent != null)
                foreach (Transform child in OpenDevelopmentsListParent)
                    Destroy(child.gameObject);

            if (FinishedDevelopmentsListParent != null)
                foreach (Transform child in FinishedDevelopmentsListParent)
                    Destroy(child.gameObject);
        }

        public void AddOpenDevelopment(string name, string description, string extraInfo, List<ResourceAmount> cost, GameUnlock unlock)
        {
            if (UIDevelopmentEntryPrefab != null && OpenDevelopmentsListParent != null)
            {
                var entryObject = Instantiate(UIDevelopmentEntryPrefab, OpenDevelopmentsListParent);
                var entryComponent = entryObject.GetComponent<UIDevelopmentEntry>();
                entryComponent?.SetContent(name, description, extraInfo, cost, unlock);
                GameUnlockSystem.AddGameUnlockAction(unlock, (bool unlocked) => { if (unlocked) TransferDevelopmentToFinished(entryObject); });
            }
        }
        
        private void TransferDevelopmentToFinished(GameObject entryObject)
        {
            FinishedDevelopmentsSection?.gameObject.SetActive(true);
            entryObject.transform.SetParent(FinishedDevelopmentsListParent, false);
            entryObject.GetComponent<UIDevelopmentEntry>()?.ShowStatusColor(true);
        }
    }
}