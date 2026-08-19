using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HypnicEmpire
{
    //  The striped backing the resource list is read against: one line for every line the list occupies,
    //  alternating between an empty line and a drawn divider from the top down.
    //
    //  The lines authored in the scene are the ones the list starts with, and they are also what every later
    //  line is copied from: a line that draws something is a divider, a line that draws nothing is empty.
    //  Growth appends the opposite of whatever the column ends with, so the alternation carries on from the
    //  authored lines and a line that already exists keeps the colour its position gave it. Only the copies
    //  are discarded, when the list itself is emptied for a load or a reset; the authored lines stay.
    public class UIResourceBackingLines : MonoBehaviour
    {
        private readonly List<GameObject> LinesAdded = new();

        private Transform EmptyLineTemplate;
        private Transform DividerLineTemplate;

        //  A blank line the height of a striped one, for the column of rows drawn over these: it has to leave
        //  a line empty wherever this column stripes one, and what an empty line is stays here.
        public GameObject CreateEmptyLine(Transform parent)
        {
            FindTemplates();

            if (EmptyLineTemplate == null)
            {
                Debug.LogWarning("Resource backing lines have no empty line to copy, so a line cannot be left blank. One line drawing nothing has to be authored in the column.", this);
                return null;
            }

            return Instantiate(EmptyLineTemplate.gameObject, parent);
        }

        public void EnsureLineCount(int lineCount)
        {
            if (transform.childCount >= lineCount) return;

            FindTemplates();

            if (EmptyLineTemplate == null || DividerLineTemplate == null)
            {
                Debug.LogWarning("Resource backing lines cannot grow past the lines authored in the column: it needs one line drawing nothing and one divider to copy.", this);
                return;
            }

            while (transform.childCount < lineCount)
                LinesAdded.Add(Instantiate(NextLineTemplate().gameObject, transform));
        }

        //  Emptying the list returns the column to the lines it was authored with rather than to none: the
        //  list is never drawn with fewer, so there is nothing to gain by taking them away and putting them
        //  back, and the alternation is only ever right if it starts from them.
        public void ResetToAuthoredLines()
        {
            foreach (var line in LinesAdded)
            {
                if (line == null) continue;

                //  A destroyed object is not detached until the end of the frame, and the column is refilled
                //  within the same frame it is reset, so each copy leaves at once instead of being counted by
                //  the layout one last time.
                line.transform.SetParent(null);
                Destroy(line);
            }

            LinesAdded.Clear();
        }

        private Transform NextLineTemplate()
        {
            if (transform.childCount == 0) return EmptyLineTemplate;

            return IsDividerLine(transform.GetChild(transform.childCount - 1)) ? EmptyLineTemplate : DividerLineTemplate;
        }

        private void FindTemplates()
        {
            if (EmptyLineTemplate != null && DividerLineTemplate != null) return;

            for (int i = 0; i < transform.childCount; i++)
            {
                var line = transform.GetChild(i);

                if (IsDividerLine(line))
                    DividerLineTemplate ??= line;
                else
                    EmptyLineTemplate ??= line;
            }
        }

        private static bool IsDividerLine(Transform line)
        {
            return line.GetComponent<Graphic>() != null;
        }
    }
}
