using System;
using System.Collections.Generic;
using System.Linq;

namespace HypnicEmpire
{
    public class JournalEntryData
    {
        public string Trigger;
        public List<string> Text = new();
        public bool Repeatable = false;
    }
}