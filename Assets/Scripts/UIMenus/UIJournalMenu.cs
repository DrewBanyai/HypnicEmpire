using UnityEngine;
using TMPro;

namespace HypnicEmpire
{
    public class UIJournalMenu : MonoBehaviour
    {
        [SerializeField] public GameObject JournalEntryPrefab;
        [SerializeField] public GameObject JournalEntryDividerPrefab;
        [SerializeField] public Transform JournalDisplayParent;

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