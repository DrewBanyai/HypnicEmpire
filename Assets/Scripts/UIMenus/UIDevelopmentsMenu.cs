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

        public void AddOpenDevelopment(string name, string description, string extraInfo, List<ResourceAmountData> cost, List<string> unlock)
        {
            if (UIDevelopmentEntryPrefab != null && OpenDevelopmentsListParent != null)
            {
                var entryObject = Instantiate(UIDevelopmentEntryPrefab, OpenDevelopmentsListParent);
                var entryComponent = entryObject.GetComponent<UIDevelopmentEntry>();
                entryComponent?.SetContent(name, description, extraInfo, cost, unlock);
                foreach (var u in unlock)
                    GameUnlockSystem.AddGameUnlockAction(u, true, (bool unlocked) => { if (unlocked) TransferDevelopmentToFinished(entryObject); });
            }
        }
        
        //  Entries are made as their developments are offered and moved between the two lists as they are
        //  bought, so the one asked for is looked for in both and may not be there at all.
        public Button FindPurchaseButton(string developmentTitle)
        {
            return FindPurchaseButtonIn(OpenDevelopmentsListParent, developmentTitle)
                ?? FindPurchaseButtonIn(FinishedDevelopmentsListParent, developmentTitle);
        }

        private static Button FindPurchaseButtonIn(Transform listParent, string developmentTitle)
        {
            if (listParent == null) return null;

            foreach (Transform child in listParent)
            {
                var entry = child.GetComponent<UIDevelopmentEntry>();
                if (entry != null && entry.DevelopmentTitle == developmentTitle)
                    return entry.PurchaseButton;
            }

            return null;
        }

        private void TransferDevelopmentToFinished(GameObject entryObject)
        {
            FinishedDevelopmentsSection?.gameObject.SetActive(true);
            entryObject.transform.SetParent(FinishedDevelopmentsListParent, false);
            entryObject.transform.SetSiblingIndex(0);
            entryObject.GetComponent<UIDevelopmentEntry>()?.ShowStatusColor(true);
        }
    }
}