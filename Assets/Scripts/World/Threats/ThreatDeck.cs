using System;
using System.Collections.Generic;
using UnityEngine;

namespace World.Threats{
    /// Manages the deck of action cards available to a threat and tracks cooldowns across combat rounds.
    [Serializable]
    public class ThreatDeck{
        [Tooltip("The library of action cards available to this threat.")]
        [SerializeField] private List<ThreatActionCard> cards = new();

        private readonly Dictionary<ThreatActionCard, int> _cooldowns = new();

        public IReadOnlyList<ThreatActionCard> Cards => cards;

        /// Resets cooldowns and initializes deck state.
        public void Initialize() => _cooldowns.Clear();

        /// Ticks cooldown counters down at the start of a new round or phase.
        public void TickCooldowns(){
            foreach (ThreatActionCard card in new List<ThreatActionCard>(_cooldowns.Keys))
                if (--_cooldowns[card] <= 0)
                    _cooldowns.Remove(card);
        }

        /// Returns true if the specified card is currently waiting on cooldown.
        public bool IsOnCooldown(ThreatActionCard card) =>
            _cooldowns.TryGetValue(card, out int remaining) && remaining > 0;

        /// Puts a card on its defined cooldown turns.
        public void PutOnCooldown(ThreatActionCard card){
            if (card.CooldownTurns > 0)
                _cooldowns[card] = card.CooldownTurns;
        }

        /// Returns all cards in the deck that satisfy cost, escalation, and cooldown requirements.
        public List<ThreatActionCard> GetPlayableCards(int currentEscalation, int currentAp){
            List<ThreatActionCard> playable = new();
            foreach (ThreatActionCard card in cards)
                if (!IsOnCooldown(card) && card.CanPlay(currentEscalation, currentAp))
                    playable.Add(card);
            return playable;
        }

        /// Adds a dynamic card to the deck during runtime.
        public void AddCard(ThreatActionCard card) => cards.Add(card);

        /// Removes a card from the deck and clears its cooldown.
        public void RemoveCard(ThreatActionCard card){
            cards.Remove(card);
            _cooldowns.Remove(card);
        }
    }
}
