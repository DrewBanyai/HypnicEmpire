using System.Collections.Generic;
using UnityEngine;

namespace HypnicEmpire
{
    public class UIResourceListMenu : MonoBehaviour
    {
        [SerializeField] public GameObject ResourceEntryPrefab;
        [SerializeField] public Transform ResourceDisplayParent;
        [SerializeField] public UIResourceBackingLines BackingLines;

        private readonly List<string> ResourcesTracked = new();

        //  Rows and section lines outlive every rebuild: a row carries subscriptions to the resource behind
        //  it, so the list is reordered by moving what it already has rather than by building it again.
        private readonly Dictionary<string, Transform> RowsShown = new();
        private readonly Dictionary<string, Transform> SectionLinesShown = new();
        private Transform ReservedTopLine;

        //  The striped backing is authored in the scene. Without it the rows still appear, but on lines of
        //  their own making with nothing behind them, which is worth saying once at startup rather than
        //  leaving to be spotted in the running game.
        private void Awake()
        {
            FindBackingLines();

            if (BackingLines == null)
                Debug.LogWarning("Resource list has no backing lines: it will not be striped, and its rows cannot be aligned to any.", this);

            AlignRowsToBackingLines();
        }

        //  The striped column belongs to this menu and is held within it, so it is looked for there rather
        //  than demanded: a list wired to a column elsewhere is honoured, but one left unwired still finds
        //  the lines it is drawn on instead of quietly going without them.
        private void FindBackingLines()
        {
            BackingLines ??= GetComponentInChildren<UIResourceBackingLines>(true);
        }

        //  The rows and the stripes behind them are two columns counting the same lines, and each column is
        //  filled from its own top edge, so any difference between those edges slides every row off the line
        //  it belongs to. The backing column is the one placed in the scene; the rows are laid over it.
        private void AlignRowsToBackingLines()
        {
            if (BackingLines == null || ResourceDisplayParent == null) return;

            var rows = ResourceDisplayParent as RectTransform;
            var backing = BackingLines.transform as RectTransform;
            if (rows == null || backing == null) return;

            if (rows.parent != backing.parent)
            {
                Debug.LogWarning("Resource rows and backing lines are held by different parents, so the rows cannot be laid over the lines they are drawn on.", this);
                return;
            }

            rows.anchorMin = backing.anchorMin;
            rows.anchorMax = backing.anchorMax;
            rows.pivot = backing.pivot;
            rows.anchoredPosition = backing.anchoredPosition;
            rows.sizeDelta = backing.sizeDelta;
        }

        //  Anything the list is asked to show is either an authored resource, held and spent out of the
        //  game state, or a tracked value accumulated from what has been built ("People" above all).
        //  The two read from different places and are displayed differently, so which one a name is has
        //  to be settled before the row is filled in - asking the game state for a value it has no entry
        //  for would otherwise fail outright.
        public void AddResourceEntry(string resourceType)
        {
            if (ResourcesTracked.Contains(resourceType)) return;

            bool isResource = ResourceTypeSystem.ResourceTypes.Contains(resourceType);
            bool isDerivedValue = !isResource && AlterableValueSystem.ValueMap.ContainsKey(resourceType);
            if (!isResource && !isDerivedValue)
            {
                Debug.LogWarning($"Resource list cannot show '{resourceType}': it is neither a resource type nor a tracked value.", this);
                return;
            }

            ResourcesTracked.Add(resourceType);

            if (ResourceEntryPrefab == null || ResourceDisplayParent == null) return;

            var entryObject = Instantiate(ResourceEntryPrefab, ResourceDisplayParent);
            var entryComponent = entryObject.GetComponent<UIResourceEntry>();
            if (entryComponent == null)
            {
                Debug.LogWarning($"Resource list cannot show '{resourceType}': its entry prefab has no {nameof(UIResourceEntry)}.", this);
                DiscardLine(entryObject.transform);
                return;
            }

            if (isResource)
                entryComponent.SetContent(resourceType);
            else
                entryComponent.SetDerivedValueContent(resourceType);

            RowsShown[resourceType] = entryObject.transform;

            ApplyLineOrder();
        }

        public void ClearAllResourceEntries()
        {
            ResourcesTracked.Clear();
            RowsShown.Clear();
            SectionLinesShown.Clear();
            ReservedTopLine = null;

            if (ResourceDisplayParent != null)
                for (int i = ResourceDisplayParent.childCount - 1; i >= 0; i--)
                    DiscardLine(ResourceDisplayParent.GetChild(i));

            FindBackingLines();
            BackingLines?.ResetToAuthoredLines();

            AlignRowsToBackingLines();

            //  An emptied list is still a list: the reserved line and the section lines beneath it are laid
            //  straight back, so it never appears as a bare gap between loads.
            ApplyLineOrder();
        }

        //  Lines are laid from the top down: the reserved line, then each section with its rows, opening on an
        //  empty line wherever the layout parts it from the section above. The backing is told how many lines
        //  that came to, so it stripes those and no more.
        private void ApplyLineOrder()
        {
            if (ResourceDisplayParent == null) return;

            var sections = ResourceListLayout.BuildSections(ResourcesTracked);

            int lineIndex = 0;
            ReservedTopLine ??= CreateEmptyLine();
            PlaceLine(ReservedTopLine, ref lineIndex);

            foreach (var section in sections)
            {
                if (ResourceListLayout.OpensWithEmptyLine(section))
                    PlaceLine(TakeSectionLine(section.Group), ref lineIndex);

                foreach (string member in section.Members)
                    if (RowsShown.TryGetValue(member, out var row))
                        PlaceLine(row, ref lineIndex);
            }

            BackingLines?.EnsureLineCount(ResourceListLayout.CountLines(sections));
        }

        private Transform TakeSectionLine(string group)
        {
            if (!SectionLinesShown.TryGetValue(group, out var sectionLine))
                SectionLinesShown[group] = sectionLine = CreateEmptyLine();

            return sectionLine;
        }

        //  A line the list leaves blank: the reserved line at the top and the line each section opens with.
        //  It is the same line the backing stripes, so the backing is what makes it.
        private Transform CreateEmptyLine()
        {
            if (BackingLines == null || ResourceDisplayParent == null) return null;

            var line = BackingLines.CreateEmptyLine(ResourceDisplayParent);
            return line == null ? null : line.transform;
        }

        private static void PlaceLine(Transform line, ref int lineIndex)
        {
            if (line == null) return;

            line.SetSiblingIndex(lineIndex);
            lineIndex++;
        }

        //  A destroyed object is not detached until the end of the frame, and the list is rebuilt within the
        //  same frame it is cleared, so a discarded line leaves the column at once: one left in place would
        //  still take up a sibling index the rebuilt lines are counting on.
        private static void DiscardLine(Transform line)
        {
            line.SetParent(null);
            Destroy(line.gameObject);
        }
    }
}
