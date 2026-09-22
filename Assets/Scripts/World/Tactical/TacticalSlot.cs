using Data;
using Data.Effects;
using UnityEngine;
using World.Actors.Player;

namespace World.Tactical{
    /// Represents a discrete standing spot and tactical anchor in the world used during turn-based Crisis combat.
    public class TacticalSlot : AreaSlot{
        [Header("Usability Phase")]
        [SerializeField] private TacticalUsabilityMode usabilityMode = TacticalUsabilityMode.AlwaysAvailable;

        [Header("Tactical Identity & Features")]
        [SerializeField] private bool isManuallyFeatured;

        [Header("Tactical Benefits (Commit Stance)")]
        [SerializeField] private Mastery grantedMasteryOnCommit;

        [Header("Dynamic Effects (Optional)")]
        [SerializeField] private EffectNode onCommitEffect;
        [SerializeField] private EffectNode onVacateEffect;

        public TacticalZone ParentZone{ get; internal set; }

        public TacticalUsabilityMode UsabilityMode => usabilityMode;
        public Mastery GrantedMasteryOnCommit => grantedMasteryOnCommit;
        public bool IsFeatured => isManuallyFeatured || grantedMasteryOnCommit != null || linkedInteractable != null || onCommitEffect != null;

        /// Evaluates whether this slot is available in the current game state.
        public bool IsUsableInCurrentState(bool isCrisis) => usabilityMode switch{
            TacticalUsabilityMode.CombatOnly      => isCrisis,
            TacticalUsabilityMode.ExplorationOnly => !isCrisis,
            _                                     => true
        };

        /// Applies defensive masteries and effects when passing the turn on this slot.
        public void CommitTurn(Character character){
            if (grantedMasteryOnCommit != null)
                character.sheet.TemporaryConditions.TryAdd(grantedMasteryOnCommit);

            if (onCommitEffect != null)
                onCommitEffect.Run(new EffectContext(character.sheet));
        }

        /// Reverts defensive masteries and executes vacate effects when leaving this slot.
        public void Vacate(Character character){
            if (grantedMasteryOnCommit != null)
                character.sheet.TemporaryConditions.TryRemove(grantedMasteryOnCommit);

            if (onVacateEffect != null)
                onVacateEffect.Run(new EffectContext(character.sheet));

            Release();
        }

        /// Draws editor gizmo representing tactical slot position and facing direction.
        protected override void OnDrawGizmos(){
            Gizmos.color = IsFeatured ? Color.cyan : Color.white;
            Vector3 pos = Position;
            Gizmos.DrawWireSphere(pos, 0.35f);
            Gizmos.DrawRay(pos, Rotation * Vector3.forward * 0.7f);
        }
    }
}
