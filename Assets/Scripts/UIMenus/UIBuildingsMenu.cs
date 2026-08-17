using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace HypnicEmpire
{
    public class UIBuildingsMenu : MonoBehaviour
    {
        private readonly List<UIBuildingButton> BuildingButtons = new();
        private readonly Dictionary<UIBuildingButton, string> RevealUnlockOfButton = new();

        //  Section root -> the building buttons below it. A section with no revealed building would
        //  otherwise show as a lone header. Sections holding no building button at all (land
        //  ownership) never enter this map and are left untouched.
        private readonly Dictionary<GameObject, List<UIBuildingButton>> ButtonsOfSection = new();

        //  Buttons naming no known building. They have no cost, effect or reveal unlock to work from,
        //  so they are misconfigured rather than ungated and stay hidden.
        private readonly HashSet<UIBuildingButton> ButtonsMissingData = new();

        public void InitializeMenu()
        {
            //  Inactive children are included: a button hidden by its reveal unlock must still be
            //  found on later passes.
            GetComponentsInChildren(true, BuildingButtons);
            RevealUnlockOfButton.Clear();
            ButtonsOfSection.Clear();
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

            ApplyRevealState();
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
        }

        private void RefreshSectionVisibility()
        {
            foreach (var section in ButtonsOfSection)
                section.Key.SetActive(section.Value.Exists(button => button != null && button.gameObject.activeSelf));
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

            if (!ButtonsOfSection.TryGetValue(section.gameObject, out var buttons))
            {
                buttons = new List<UIBuildingButton>();
                ButtonsOfSection[section.gameObject] = buttons;
            }
            buttons.Add(button);
        }
    }
}
