using System;
using System.Collections.Generic;

namespace HypnicEmpire
{
    //  One section of the resource list: the resource group it stands for, where that group is authored
    //  among the others, and the names currently shown under it, in the order they are authored. A section
    //  with no members yet is still a section, holding the place its group will fill.
    public sealed class ResourceListSection
    {
        public string Group;
        public int Index = ResourceListLayout.UngroupedSectionIndex;
        public readonly List<string> Members = new();
    }

    //  The resource list is a column of fixed-height lines rather than a plain stack of rows. One line is
    //  reserved empty at the top, and every group opens with an empty line of its own, which is what
    //  separates it from the group above.
    //
    //  Nothing is reserved at the foot of the list. The column stripes its lines in turn, so a line kept
    //  spare down there would be drawn as a divider as often as not, and a stripe below the last group reads
    //  as a row that failed to fill rather than as the margin it was meant to be. The list ends on the last
    //  group's line and lets the panel behind it give the margin.
    //
    //  Every authored group holds its place from the outset, locked or not, so the gaps that divide the list
    //  are there from the start rather than opening up one by one as groups come into play. A group with
    //  nothing to show yet is drawn as its opening line and no rows, which reads as the gap the group will
    //  grow into rather than as anything missing. The list still lengthens as resources unlock, but only
    //  beneath the group each one joins.
    //
    //  The one group without an opening line is the one authored first, which heads the list: the reserved
    //  line already stands between it and the title, so a line of its own would only double that gap.
    //
    //  Deciding all this here, apart from the objects that draw it, keeps the rows and the striped backing
    //  behind them counting the very same lines.
    public static class ResourceListLayout
    {
        //  The empty line above the list, present however little is shown.
        public const int ReservedTopLineCount = 1;

        //  The lines the backing column is authored with. The count only ever falls this low if the resource
        //  groups failed to load, and the list is better drawn at the size it was built at than as a sliver.
        public const int MinimumLineCount = 4;

        //  Stands in for a section a name was never authored into, so a data slip leaves a resource in an
        //  odd place rather than hiding it altogether.
        public const string UngroupedSectionName = "";
        public const int UngroupedSectionIndex = -1;

        //  The group authored first, which the list is headed by.
        public const int HeadingSectionIndex = 0;

        public static List<ResourceListSection> BuildSections(IEnumerable<string> shownNames)
        {
            var placed = new Dictionary<int, SortedDictionary<int, string>>();
            var unplaced = new List<string>();

            if (shownNames != null)
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

            var sections = new List<ResourceListSection>();

            for (int sectionIndex = 0; sectionIndex < ResourceTypeSystem.ResourceGroupCount; sectionIndex++)
            {
                var section = new ResourceListSection
                {
                    Group = ResourceTypeSystem.GetResourceGroupName(sectionIndex),
                    Index = sectionIndex
                };

                if (placed.TryGetValue(sectionIndex, out var members))
                    foreach (string member in members.Values)
                        section.Members.Add(member);

                sections.Add(section);
            }

            //  A name whose group was never authored has no place kept for it, so it is gathered into a
            //  section of its own after the rest: the list shows it out of place rather than dropping it.
            if (unplaced.Count > 0)
            {
                var section = new ResourceListSection { Group = UngroupedSectionName, Index = UngroupedSectionIndex };
                section.Members.AddRange(unplaced);
                sections.Add(section);
            }

            return sections;
        }

        public static int CountLines(IReadOnlyList<ResourceListSection> sections)
        {
            int lineCount = ReservedTopLineCount;

            if (sections != null)
                foreach (var section in sections)
                    lineCount += (OpensWithEmptyLine(section) ? 1 : 0) + section.Members.Count;

            return Math.Max(lineCount, MinimumLineCount);
        }

        //  Every section opens on an empty line but the one heading the list, which the reserved line above
        //  the list already stands apart from.
        public static bool OpensWithEmptyLine(ResourceListSection section)
        {
            return section == null || section.Index != HeadingSectionIndex;
        }
    }
}
