using System;
using System.Collections.Generic;
using System.Linq;
using Actors.Player;

namespace Actors.Brains{
    /// <summary>
    /// Pure C# data structure managing party unit selection using a HashSet of Character.
    /// Guarantees O(1) lookups, uniqueness, tracks the designated Lead character,
    /// and provides Baldur's Gate 3-style party selection behaviors.
    /// </summary>
    public class PartySelection{
        public Character Lead{ get; private set; }

        public HashSet<Character> Selected{ get; } = new();

        public int Count => Selected.Count;

        public event Action OnSelectionChanged;

        /// <summary>
        /// Selects a single unit, clearing any previous selection.
        /// </summary>
        public void SingleUnitSelect(Character selectedUnit) => Select(selectedUnit);

        /// <summary>
        /// Adds a unit to the current selection without wiping previous members (Shift/Append).
        /// </summary>
        public void AddUnitSelect(Character selectedUnit) => Append(selectedUnit);

        /// <summary>
        /// Toggles a unit's selection state (Shift/Ctrl + Click).
        /// If already selected, removes it. If not, adds it and sets it as the Lead.
        /// </summary>
        public void ToggleAddSelection(Character selectedUnit){
            if (selectedUnit == null) return;

            if (Selected.Contains(selectedUnit))
                Remove(selectedUnit);
            else
                Append(selectedUnit);
        }

        /// <summary>
        /// Marquee/Drag box selection: wipes current selection and selects all units inside the box.
        /// The first unit becomes the Lead.
        /// </summary>
        public void DragboxSelect(List<Character> listOfSelectedUnits) => Select(listOfSelectedUnits);

        /// <summary>
        /// Additive box selection (Shift + Drag box): appends all units inside the box
        /// to the existing selection without wiping previous members.
        /// </summary>
        public void AdditiveBoxSelect(List<Character> listOfSelectedUnits) => Append(listOfSelectedUnits);

        /// <summary>
        /// Selects all members of the active party (~ or hotkey).
        /// Retains current Lead if still in the party, otherwise defaults to the first party member.\n        /// </summary>
        public void SelectAll(List<Character> activePartyMembersList){
            Selected.Clear();

            if (activePartyMembersList.Count == 0){
                Lead = null;
                OnSelectionChanged?.Invoke();
                return;
            }

            foreach (Character member in activePartyMembersList)
                Selected.Add(member);

            if (!Selected.Contains(Lead))
                Lead = activePartyMembersList[0];

            OnSelectionChanged?.Invoke();
        }

        /// <summary>
        /// Cycles the Lead character among currently selected members (Tab key),
        /// preserving the canonical order defined in the active party members list.
        /// </summary>
        public void CycleLeader(List<Character> activePartyMembersList){
            List<Character> selectedPartyOrdered =
                activePartyMembersList.Where(t => Selected.Contains(t)).ToList();
            if (selectedPartyOrdered.Count <= 1) return;
            int currentIndex = selectedPartyOrdered.IndexOf(Lead);
            int nextIndex    = (currentIndex + 1) % selectedPartyOrdered.Count;
            Lead = selectedPartyOrdered[nextIndex];
            OnSelectionChanged?.Invoke();
        }

        /// <summary>
        /// Clears all characters from the selection (Escape key or click-to-empty).
        /// </summary>
        public void Clear(){
            if (Selected.Count == 0 && Lead == null) return;
            Selected.Clear();
            Lead = null;
            OnSelectionChanged?.Invoke();
        }

        /// <summary>
        /// Checks whether a character is currently selected.
        /// </summary>
        public bool Contains(Character character) => Selected.Contains(character);

        private void Select(Character character) => Select(new List<Character>{ character });

        private void Select(List<Character> characters){
            List<Character> clean = Sanitize(characters);
            Selected.Clear();
            Lead = null;

            if (clean.Count == 0){
                OnSelectionChanged?.Invoke();
                return;
            }

            Lead = clean[0];
            foreach (Character t in clean)
                Selected.Add(t);

            OnSelectionChanged?.Invoke();
        }

        private void Append(Character character) => Append(new List<Character>{ character });

        private void Append(List<Character> characters){
            List<Character> clean = Sanitize(characters);
            if (clean.Count == 0) return;

            if (Lead == null || !Selected.Contains(Lead))
                Lead = clean[0];

            foreach (Character t in clean)
                Selected.Add(t);

            OnSelectionChanged?.Invoke();
        }

        private void Remove(Character character) => Remove(new List<Character>{ character });

        private void Remove(List<Character> characters){
            List<Character> clean = Sanitize(characters);
            if (clean.Count == 0) return;

            foreach (Character t in clean)
                Selected.Remove(t);

            if (clean.Contains(Lead))
                Lead = GetAnyCharacter();

            OnSelectionChanged?.Invoke();
        }

        private static List<Character> Sanitize(List<Character> list) =>
            list == null ? new List<Character>() : list.Where(e => e != null).ToList();

        private Character GetAnyCharacter(){
            using HashSet<Character>.Enumerator enumerator = Selected.GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : null;
        }
    }
}
