using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Partition = System.Collections.Generic.Dictionary<string, object>;

namespace Core.Data {
    [DisallowMultipleComponent]
    public class CharacterSheet : TrackedBehaviour {
        public Tracked<int> level = new("level", 1);
        public Tracked<int> experience = new("experience", 0);

        [SerializeField] private List<Mastery> activeMasteries = new();
        private readonly Dictionary<Skill, int> _skills = new();

        public int Level      => level;
        public int Experience => experience;
        public IReadOnlyList<Mastery> ActiveMasteries => activeMasteries;

        protected override void OnAwake() => RebuildAllSkills();
        
        public void RebuildAllSkills() {
            _skills.Clear();
            foreach (Mastery mastery in activeMasteries) 
                ApplyMasteryDelta(mastery, isAdding: true);
        }

        public int GetEffectiveSkill(Skill skill) => _skills.GetValueOrDefault(skill, 0);

        private void ApplyMasteryDelta(Mastery mastery, bool isAdding) {
            int changeMultiplier = isAdding ? 1 : -1;
            foreach (Skill skill in mastery.AssociatedSkills) {
                _skills.TryAdd(skill, 0);
                _skills[skill] += changeMultiplier;
                if (_skills[skill] == 0)
                    _skills.Remove(skill);
            }
            foreach (Skill skill in mastery.PenalizedSkills) {
                _skills.TryAdd(skill, 0);
                _skills[skill] -= changeMultiplier;
                if (_skills[skill] == 0)
                    _skills.Remove(skill);
            }
        }
        
        public bool HasMastery(Mastery mastery) => activeMasteries.Contains(mastery);
        
        public bool TryAddMastery(Mastery mastery) {
            if (HasMastery(mastery)) return false;
            activeMasteries.Add(mastery);
            ApplyMasteryDelta(mastery, isAdding: true);
            return true;
        }
        
        public bool TryRemoveMastery(Mastery mastery) {
            if (!HasMastery(mastery)) return false;
            activeMasteries.Remove(mastery);
            ApplyMasteryDelta(mastery, isAdding: false);
            return true;
        }

        public void AddExperience(int amount) => experience.Value += amount;
        
        public override void OnSaveState(Partition state) {
            base.OnSaveState(state);
            state["unlocked_masteries"] = activeMasteries.Select(m => m.Id).ToList();
        }

        public override void OnLoadState(Partition state) {
            base.OnLoadState(state);
            activeMasteries.Clear();
            if (state.TryGetValue("unlocked_masteries", out object value)){
                List<string> masteryIds = (List<string>)value;
                foreach (string id in masteryIds)
                    activeMasteries.Add(MasteryDatabase.Get(id));
            }
            RebuildAllSkills();
        }
    }
}
