using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.State;
using UnityEngine;

namespace Data{
    [Serializable]
    public class MasteryCollection : ITracked{
        [SerializeField] private string key;
        [SerializeField] private MasteryDatabase database;
        [SerializeField] private List<Mastery> activeMasteries = new();

        private readonly Dictionary<Skill, int> _skills = new();

        public event Action<Mastery> OnAdded;
        public event Action<Mastery> OnRemoved;

        public string Key => key;
        public IReadOnlyList<Mastery> List => activeMasteries;
        public MasteryDatabase Database => database;

        public MasteryCollection(string key, MasteryDatabase database = null){
            this.key = key;
            this.database = database;
        }

        /// Assigns the database reference used to deserialize masteries by id.
        public void SetDatabase(MasteryDatabase db) => database = db;

        /// Recalculates all skill bonuses from the active masteries list.
        public void RebuildSkills(){
            _skills.Clear();
            foreach (Mastery mastery in activeMasteries)
                ApplyMasteryDelta(mastery, isAdding: true);
        }

        /// Returns the bounded effective skill bonus clamped between 0 and 4.
        public int GetSkillBonus(Skill skill) => Mathf.Clamp(_skills.GetValueOrDefault(skill, 0), 0, 4);

        /// Returns the raw, unbounded skill delta tally for this collection.
        public int GetRawSkillDelta(Skill skill) => _skills.GetValueOrDefault(skill, 0);

        /// Returns the first active mastery that provides a positive bonus to the skill.
        public Mastery GetContributingMastery(Skill skill){
            foreach (Mastery mastery in activeMasteries)
                if (mastery.AssociatedSkills.Contains(skill))
                    return mastery;
            return null;
        }

        /// Checks if the mastery is present in the active collection.
        public bool Contains(Mastery mastery) => activeMasteries.Contains(mastery);

        /// Adds a mastery and updates the skill cache.
        public bool TryAdd(Mastery mastery){
            if (Contains(mastery)) return false;
            activeMasteries.Add(mastery);
            ApplyMasteryDelta(mastery, isAdding: true);
            OnAdded?.Invoke(mastery);
            return true;
        }

        /// Removes a mastery and updates the skill cache.
        public bool TryRemove(Mastery mastery){
            if (!Contains(mastery)) return false;
            activeMasteries.Remove(mastery);
            ApplyMasteryDelta(mastery, isAdding: false);
            OnRemoved?.Invoke(mastery);
            return true;
        }

        /// Clears all masteries from the collection and resets skill deltas.
        public void Clear(){
            activeMasteries.Clear();
            _skills.Clear();
        }

        /// Serializes active mastery string IDs into the Blackboard partition.
        public void Save(Dictionary<string, object> state) =>
            state[key] = activeMasteries.Select(m => m.Id).ToList();

        /// Deserializes mastery IDs from the Blackboard partition and rebuilds skills.
        public void Load(Dictionary<string, object> state){
            if (!state.TryGetValue(key, out object value) || value is not IEnumerable list) return;
            activeMasteries.Clear();
            foreach (object item in list)
                activeMasteries.Add(database.Get(item.ToString()));
            RebuildSkills();
        }

        /// Adjusts internal skill point tallies when adding or removing a mastery.
        private void ApplyMasteryDelta(Mastery mastery, bool isAdding){
            int multiplier = isAdding ? 1 : -1;
            foreach (Skill skill in mastery.AssociatedSkills){
                _skills.TryAdd(skill, 0);
                _skills[skill] += multiplier;
                if (_skills[skill] == 0)
                    _skills.Remove(skill);
            }

            foreach (Skill skill in mastery.PenalizedSkills){
                _skills.TryAdd(skill, 0);
                _skills[skill] -= multiplier;
                if (_skills[skill] == 0)
                    _skills.Remove(skill);
            }
        }
    }
}
