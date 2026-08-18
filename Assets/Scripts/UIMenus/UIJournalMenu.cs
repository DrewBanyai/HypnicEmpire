using UnityEngine;
using TMPro;

namespace HypnicEmpire
{
    public class UIJournalMenu : MonoBehaviour
    {
        [SerializeField] public GameObject JournalEntryPrefab;
        [SerializeField] public GameObject JournalEntryDividerPrefab;
        [SerializeField] public Transform JournalDisplayParent;

        public void ClearJournalEntries()
        {
            if (JournalDisplayParent == null) return;

            for (int i = JournalDisplayParent.childCount - 1; i >= 0; i--)
            {
                Transform child = JournalDisplayParent.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }

        public void AddJournalEntry(string journalText)
        {
            if (JournalEntryPrefab != null && JournalEntryDividerPrefab != null && JournalDisplayParent != null)
            {
                if (JournalDisplayParent.childCount != 0)
                {
                    var dividerObject = Instantiate(JournalEntryDividerPrefab, JournalDisplayParent);
                    dividerObject.transform.SetSiblingIndex(0);
                }
                var entryObject = Instantiate(JournalEntryPrefab, JournalDisplayParent);
                entryObject.transform.SetSiblingIndex(0);
                var entryComponent = entryObject.GetComponent<TextMeshProUGUI>();
                entryComponent?.SetText(journalText);
            }
        }
    }
}