using System;
using System.Collections.Generic;

namespace HypnicEmpire
{
    //  One section of the resource list: the resource group it stands for and the names currently shown
    //  under it, in the order they are authored.
    public sealed class ResourceListSection
    {
        public string Group;
        public readonly List<string> Members = new();
    }

    //  The resource list is a column of fixed-height lines rather than a plain stack of rows. One line is
    //  reserved empty at the top and another at the bottom, and every section that has something to show
    //  opens with an empty line of its own, which is what separates it from the section above. Deciding
    //  that here, apart from the objects that draw it, keeps the rows and the striped backing behind them
    //  counting the very same lines.
    public static class ResourceListLayout
    {
        //  The empty line above the list and the empty line below it, both present however little is shown.
        public const int ReservedLineCount = 2;

        //  What the game starts with: the two reserved lines, the opening line of the one section unlocked
        //  at that point, and the single resource under it.
        public const int MinimumLineCount = 4;

        //  Stands in for a section a name was never authored into, so a data slip leaves a resource in an
        //  odd place rather than hiding it altogether.
        public const string UngroupedSectionName = "";

        public static List<ResourceListSection> BuildSections(IEnumerable<string> shownNames)
        {
            var sections = new List<ResourceListSection>();
            if (shownNames == null) return sections;

            var placed = new SortedDictionary<int, SortedDictionary<int, string>>();
            var unplaced = new List<string>();

            foreach (string name in shownNames)
            {
                if (!ResourceTypeSystem.TryGetDisplayPosition(name, out var position))
                {
                    unplaced.Add(name);
                    continue;
                }

                if (!placed.TryGetValue(position.SectionIndex, out var members))
                    placed[position.SectionIndex] = members = new SortedDictionary<int, string>();

                members[position.MemberIndex] = name;
            }

            foreach (var (sectionIndex, members) in placed)
            {
                var section = new ResourceListSection { Group = ResourceTypeSystem.GetResourceGroupName(sectionIndex) };
                foreach (string member in members.Values)
                    section.Members.Add(member);
                sections.Add(section);
            }

            if (unplaced.Count > 0)
            {
                var section = new ResourceListSection { Group = UngroupedSectionName };
                section.Members.AddRange(unplaced);
                sections.Add(section);
            }

            return sections;
        }

        public static int CountLines(IReadOnlyList<ResourceListSection> sections)
        {
            int lineCount = ReservedLineCount;

            if (sections != null)
                foreach (var section in sections)
                    lineCount += 1 + section.Members.Count;

            return Math.Max(lineCount, MinimumLineCount);
        }
    }
}
