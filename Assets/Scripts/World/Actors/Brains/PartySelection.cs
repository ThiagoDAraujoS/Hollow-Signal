using System;
using System.Collections.Generic;
using System.Linq;
using World.Actors.Player;

namespace World.Actors.Brains{
    /// Pure C# data structure managing party unit selection using a HashSet of Character.
    public class PartySelection{
        public Character Lead{ get; private set; }

        public HashSet<Character> Selected{ get; } = new();

        public int Count => Selected.Count;

        public event Action OnSelectionChanged;

        /// Selects a single unit, clearing any previous selection.
        public void SingleUnitSelect(Character selectedUnit) => Select(selectedUnit);

        /// Adds a unit to the current selection without wiping previous members.
        public void AddUnitSelect(Character selectedUnit) => Append(selectedUnit);

        /// Sets the designated lead character among selected units, or selects it if not selected.
        public void SetLead(Character character){
            if (character == null) return;
            if (Selected.Contains(character)){
                if (Lead != character){
                    Lead = character;
                    OnSelectionChanged?.Invoke();
                }
            }
            else{
                SingleUnitSelect(character);
            }
        }

        /// Toggles a unit's selection state between selected and unselected.
        public void ToggleAddSelection(Character selectedUnit){
            if (selectedUnit == null) return;

            if (Selected.Contains(selectedUnit))
                Remove(selectedUnit);
            else
                Append(selectedUnit);
        }

        /// Wipes current selection and selects all units inside the drag box.
        public void DragboxSelect(List<Character> listOfSelectedUnits) => Select(listOfSelectedUnits);

        /// Appends all units inside the box to existing selection without wiping previous members.
        public void AdditiveBoxSelect(List<Character> listOfSelectedUnits) => Append(listOfSelectedUnits);

        /// Selects all members of the active party roster.
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

        /// Cycles the designated lead character among currently selected members.
        public void CycleLeader(List<Character> activePartyMembersList){
            List<Character> selectedPartyOrdered =
                activePartyMembersList.Where(t => Selected.Contains(t)).ToList();
            if (selectedPartyOrdered.Count <= 1) return;
            int currentIndex = selectedPartyOrdered.IndexOf(Lead);
            int nextIndex    = (currentIndex + 1) % selectedPartyOrdered.Count;
            Lead = selectedPartyOrdered[nextIndex];
            OnSelectionChanged?.Invoke();
        }

        /// Clears all characters from the selection.
        public void Clear(){
            if (Selected.Count == 0 && Lead == null) return;
            Selected.Clear();
            Lead = null;
            OnSelectionChanged?.Invoke();
        }

        /// Checks whether a character is currently selected.
        public bool Contains(Character character) => Selected.Contains(character);

        /// Selects a single unit via sanitized collection assignment.
        private void Select(Character character) => Select(new List<Character>{ character });

        /// Selects a collection of units and resets the lead character to the first entry.
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

        /// Appends a single unit to the current selection.
        private void Append(Character character) => Append(new List<Character>{ character });

        /// Appends a collection of units to the current selection.
        private void Append(List<Character> characters){
            List<Character> clean = Sanitize(characters);
            if (clean.Count == 0) return;

            if (Lead == null || !Selected.Contains(Lead))
                Lead = clean[0];

            foreach (Character t in clean)
                Selected.Add(t);

            OnSelectionChanged?.Invoke();
        }

        /// Removes a single unit from current selection.
        private void Remove(Character character) => Remove(new List<Character>{ character });

        /// Removes a collection of units from current selection and reassigns lead if removed.
        private void Remove(List<Character> characters){
            List<Character> clean = Sanitize(characters);
            if (clean.Count == 0) return;

            foreach (Character t in clean)
                Selected.Remove(t);

            if (clean.Contains(Lead))
                Lead = GetAnyCharacter();

            OnSelectionChanged?.Invoke();
        }

        /// Filters null entries out of a character candidate list.
        private static List<Character> Sanitize(List<Character> list) =>
            list == null ? new List<Character>() : list.Where(e => e != null).ToList();

        /// Retrieves any active selected character to serve as fallback lead.
        private Character GetAnyCharacter(){
            using HashSet<Character>.Enumerator enumerator = Selected.GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : null;
        }
    }
}
