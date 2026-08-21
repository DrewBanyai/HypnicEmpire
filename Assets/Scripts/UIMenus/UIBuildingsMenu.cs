using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;

namespace HypnicEmpire
{
    public class UIBuildingsMenu : MonoBehaviour
    {
        //  Section headers are authored with the count they start on ("Commercial (0)"), so the name a
        //  header is rewritten from is whatever precedes that.
        private static readonly Regex AuthoredSectionCount = new(@"\s*\(\d+\)\s*$");

        //  A top level group of the scrollable list: its header and the building buttons below it. A
        //  section with no revealed building would otherwise show as a lone header, and its count is the
        //  buildings standing across the whole group.
        private class BuildingSection
        {
            public GameObject Root { get; }
            public List<UIBuildingButton> Buttons { get; } = new();

            private readonly TextMeshProUGUI TitleText;
            private readonly string TitleName;

            public BuildingSection(GameObject root)
            {
                Root = root;
                TitleText = FindTitleText(root);
                TitleName = TitleText == null ? null : AuthoredSectionCount.Replace(TitleText.text, string.Empty);
            }

            public bool HasRevealedButton => Buttons.Exists(button => button != null && button.gameObject.activeSelf);

            public void RefreshCount()
            {
                if (TitleText == null) return;

                int count = 0;
                foreach (var button in Buttons)
                    if (button != null) count += ModifierValueSystem.GetBuildingCount(button.BuildingName);

                TitleText.SetText(Localization.DisplayText_BuildingSectionTitle(TitleName, count));
            }

            //  The header is the only text in a section that is not part of a building button. Inactive
            //  children are included so a section hidden for want of a revealed building still resolves.
            private static TextMeshProUGUI FindTitleText(GameObject root)
            {
                foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (text.GetComponentInParent<UIBuildingButton>(true) == null)
                        return text;

                return null;
            }
        }

        private readonly List<UIBuildingButton> BuildingButtons = new();
        private readonly Dictionary<UIBuildingButton, string> RevealUnlockOfButton = new();

        //  Sections holding no building button at all (land ownership) never enter this map and are left
        //  untouched.
        private readonly Dictionary<GameObject, BuildingSection> SectionOfRoot = new();

        //  Buttons naming no known building. They have no cost, effect or reveal unlock to work from,
        //  so they are misconfigured rather than ungated and stay hidden.
        private readonly HashSet<UIBuildingButton> ButtonsMissingData = new();

        public void InitializeMenu()
        {
            //  Inactive children are included: a button hidden by its reveal unlock must still be
            //  found on later passes.
            GetComponentsInChildren(true, BuildingButtons);
            RevealUnlockOfButton.Clear();
            SectionOfRoot.Clear();
            ButtonsMissingData.Clear();

            Transform content = GetComponent<ScrollRect>()?.content;

            foreach (var button in BuildingButtons)
            {
                if (button == null) continue;

                var data = BuildingDataSystem.GetBuildingData(button.BuildingName);
                if (data == null)
                {
                    ButtonsMissingData.Add(button);
                    Debug.LogWarning($"Building button '{button.name}' names no known building ('{button.BuildingName}') and stays hidden.", button);
                }

                button.SetBuildingData(data);
                button.InitializeRuntime();
                MapButtonToSection(button, content);

                string revealUnlock = data?.RevealUnlock;
                RevealUnlockOfButton[button] = revealUnlock;
                if (string.IsNullOrEmpty(revealUnlock)) continue;

                GameUnlockSystem.AddGameUnlockAction(revealUnlock, true, (bool unlocked) =>
                {
                    button.SetRevealed(unlocked);
                    RefreshSectionVisibility();
                });
            }

            //  Section counts follow the buildings that stand, which the modifier system owns and
            //  recomputes whenever one is built, a save is loaded or the game is reset.
            ModifierValueSystem.OnValuesRecomputed -= RefreshSectionCounts;
            ModifierValueSystem.OnValuesRecomputed += RefreshSectionCounts;

            ApplyRevealState();
        }

        private void OnDestroy()
        {
            ModifierValueSystem.OnValuesRecomputed -= RefreshSectionCounts;
        }

        //  Registered unlock actions never apply an initial state, and loading or resetting replaces
        //  the unlock list wholesale, so the current state has to be applied directly.
        public void ApplyRevealState()
        {
            foreach (var button in BuildingButtons)
            {
                if (button == null) continue;
                if (ButtonsMissingData.Contains(button))
                {
                    button.SetRevealed(false);
                    continue;
                }

                string revealUnlock = RevealUnlockOfButton.TryGetValue(button, out var unlock) ? unlock : null;
                button.SetRevealed(string.IsNullOrEmpty(revealUnlock) || GameUnlockSystem.IsUnlocked(revealUnlock));
            }

            RefreshSectionVisibility();
            RefreshSectionCounts();
        }

        private void RefreshSectionVisibility()
        {
            foreach (var section in SectionOfRoot.Values)
                section.Root.SetActive(section.HasRevealedButton);
        }

        private void RefreshSectionCounts()
        {
            foreach (var section in SectionOfRoot.Values)
                section.RefreshCount();
        }

        //  A section is a top level group of the scrollable list, so it is the ancestor of the button
        //  that sits directly under the scroll content.
        private void MapButtonToSection(UIBuildingButton button, Transform content)
        {
            if (content == null) return;

            Transform section = button.transform;
            while (section != null && section.parent != content)
                section = section.parent;
            if (section == null) return;

            if (!SectionOfRoot.TryGetValue(section.gameObject, out var buildingSection))
            {
                buildingSection = new BuildingSection(section.gameObject);
                SectionOfRoot[section.gameObject] = buildingSection;
            }
            buildingSection.Buttons.Add(button);
        }

        public UIBuildingButton FindBuildingButton(string buildingName)
        {
            foreach (var button in BuildingButtons)
                if (button != null && button.BuildingName == buildingName)
                    return button;

            return null;
        }
    }
}
