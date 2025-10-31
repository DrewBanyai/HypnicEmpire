using UnityEngine;
using TMPro;

public class UIJournalEntry : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI JournalText;

    public void SetJournalEntryText(string text)
    {
        if (JournalText != null)
            JournalText.text = text;
    }
}
