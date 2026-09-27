using System;
using System.Collections.Generic;
using Data.Effects;
using UnityEngine;
using World.Actors.Player;
using World.Tactical;

namespace World.Threats{
    public enum ThreatTargetType{
        Self,
        CurrentZone,
        ControlledZones,
        AdjacentZone,
        ZoneWithMostHeroes,
        Global
    }

    /// Scriptable definition for an action card drafted and executed by a threat's deck.
    [CreateAssetMenu(fileName = "ThreatCard_Action", menuName = "CRPG/Threats/Action Card")]
    public class ThreatActionCard : ScriptableObject{
        [Header("Identity")]
        [SerializeField] private string cardName = "Threat Action";
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Cost & Conditions")]
        [SerializeField] private int apCost = 1;
        [SerializeField] private int minEscalation = 0;
        [SerializeField] private int cooldownTurns = 0;
        [SerializeField] private float aiWeight = 10f;

        [Header("Targeting & Hazard Dynamics")]
        [SerializeField] private ThreatTargetType targetType = ThreatTargetType.CurrentZone;
        [Tooltip("If true, this card damages or penalizes the threat itself (e.g. fire consuming own building, unstable backfire, infighting).")]
        [SerializeField] private bool isSelfHarm;

        [Header("Effects Payload")]
        [SerializeField] private List<EffectNode> effects = new();

        public string CardName => cardName;
        public string Description => description;
        public int ApCost => apCost;
        public int MinEscalation => minEscalation;
        public int CooldownTurns => cooldownTurns;
        public float AiWeight => aiWeight;
        public ThreatTargetType TargetType => targetType;
        public bool IsSelfHarm => isSelfHarm;
        public IReadOnlyList<EffectNode> Effects => effects;

        /// Evaluates if this card can currently be drafted and played based on escalation and AP.
        public bool CanPlay(int currentEscalation, int currentAp) =>
            currentAp >= apCost && currentEscalation >= minEscalation;

        /// Resolves the zones affected by this action card.
        private IEnumerable<TacticalZone> ResolveZones(ThreatSheet source, TacticalZone targetZone){
            if (targetType == ThreatTargetType.ControlledZones)
                return source.ControlledZones;
            if (targetType == ThreatTargetType.Global)
                return TacticalZone.AllZones;
            return targetZone ? new[] { targetZone } : Array.Empty<TacticalZone>();
        }

        /// Executes the action card logic against the resolved target zones and/or threat itself.
        public void Execute(ThreatSheet source, TacticalZone targetZone, Action onComplete = null){
            if (targetType == ThreatTargetType.Self || isSelfHarm){
                EffectContext selfContext = new(source, targetZone);
                foreach (EffectNode effect in effects)
                    effect.Run(selfContext);
            }

            if (targetType != ThreatTargetType.Self)
                foreach (TacticalZone zone in ResolveZones(source, targetZone))
                    foreach (TacticalSlot slot in zone.ChildSlots){
                        Character hero = slot.Occupant;
                        if (!hero || !hero.sheet)
                            continue;

                        EffectContext heroContext = new(hero.sheet, zone);
                        foreach (EffectNode effect in effects)
                            effect.Run(heroContext);
                    }

            onComplete?.Invoke();
        }
    }
}
