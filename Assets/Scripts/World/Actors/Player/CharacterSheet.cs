using Core.Crisis;
using Core.State;
using Data;
using UnityEngine;

namespace World.Actors.Player{
    [DisallowMultipleComponent]
    public class CharacterSheet : Sheet{
        [Header("Progression")]
        public Tracked<int> level = new("level", 1);
        public Tracked<int> experience = new("experience", 0);

        [Header("Vitality Pools")]
        public VitalityPool flesh = new("flesh", 10);
        public VitalityPool composure = new("composure", 10);

        [Header("Tactical")]
        [SerializeField] private int baseMoveSpeed = 1;
        [SerializeField] private CrisisTurn crisisTurn = new("crisis_turn");

        [Header("Mastery Buckets")]
        [SerializeField] private MasteryDatabase database;
        [SerializeField] private MasteryCollection innateMasteries = new("innate_masteries");
        [SerializeField] private MasteryCollection equippedConditions = new("equipped_conditions");
        [SerializeField] private MasteryCollection chronicConditions = new("chronic_conditions");
        [SerializeField] private MasteryCollection temporaryConditions = new("temporary_conditions");

        public int Level => level;
        public int Experience => experience;
        public VitalityPool Flesh => flesh;
        public VitalityPool Composure => composure;
        public int MoveSpeed => baseMoveSpeed;
        public bool IsIncapacitated => flesh.IsDead;
        public CrisisTurn CrisisTurn => crisisTurn;

        public MasteryCollection InnateMasteries => innateMasteries;
        public MasteryCollection EquippedConditions => equippedConditions;
        public MasteryCollection ChronicConditions => chronicConditions;
        public MasteryCollection TemporaryConditions => temporaryConditions;

        protected override void OnAwake(){
            base.OnAwake();
            innateMasteries.SetDatabase(database);
            equippedConditions.SetDatabase(database);
            chronicConditions.SetDatabase(database);
            temporaryConditions.SetDatabase(database);
            RebuildAllSkills();
        }

        /// Returns the bounded effective skill bonus clamped between 0 and 4 across all active buckets.
        public int GetEffectiveSkill(Skill skill){
            int rawTotal = innateMasteries.GetRawSkillDelta(skill)
                         + equippedConditions.GetRawSkillDelta(skill)
                         + chronicConditions.GetRawSkillDelta(skill)
                         + temporaryConditions.GetRawSkillDelta(skill);
            return Mathf.Clamp(rawTotal, 0, 4);
        }

        /// Finds the primary active mastery contributing a positive bonus to the specified skill.
        public Mastery GetContributingMastery(Skill skill){
            return innateMasteries.GetContributingMastery(skill)
                ?? equippedConditions.GetContributingMastery(skill)
                ?? chronicConditions.GetContributingMastery(skill)
                ?? temporaryConditions.GetContributingMastery(skill);
        }

        /// Adds or removes maximum vitality adjustments from equipment.
        public void ModifyMaxVitality(int fleshDelta, int composureDelta){
            flesh.SetMax(flesh.Max + fleshDelta);
            composure.SetMax(composure.Max + composureDelta);
        }

        /// Restores composure and wipes volatile conditions during a safe room rest.
        public void RestAndRecover(){
            temporaryConditions.Clear();
            composure.HealAll();
            CrisisManager.Instance.Scheduler.CancelForTarget(GetComponent<UniqueId>().Id);
            RebuildAllSkills();
        }

        /// Rebuilds all skill values across all active collections.
        public void RebuildAllSkills(){
            innateMasteries.RebuildSkills();
            equippedConditions.RebuildSkills();
            chronicConditions.RebuildSkills();
            temporaryConditions.RebuildSkills();
        }

        /// Increments current experience points.
        public void AddExperience(int amount) => experience.Value += amount;
    }
}
