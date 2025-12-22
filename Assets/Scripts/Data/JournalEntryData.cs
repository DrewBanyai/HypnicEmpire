using System.Collections.Generic;

namespace HypnicEmpire
{
    public class JournalEntryData
    {
        public string Trigger;
        public List<string> Text = new();
        public bool Repeatable = false;
    }
}