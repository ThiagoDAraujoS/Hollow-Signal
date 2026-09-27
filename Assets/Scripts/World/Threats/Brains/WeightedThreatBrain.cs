using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using World.Tactical;

namespace World.Threats.Brains{
    /// Threat brain drafting playable cards from its deck using weighted evaluation.
    public class WeightedThreatBrain : ThreatBrain{
        [Tooltip("Optional delay between actions in seconds for pacing/readability.")]
        [SerializeField] private float actionDelaySeconds = 0.25f;

        /// Executes enemy turn logic by ticking cooldowns and sequentially drafting actions.
        public override void ExecuteTurn(Action onTurnComplete){
            if (Threat.IsNeutralized){
                onTurnComplete();
                return;
            }

            Threat.Deck.TickCooldowns();
            ExecuteNextAction(Threat.ActionPoints, onTurnComplete);
        }

        /// Recursively drafts and executes playable action cards while AP remains.
        private void ExecuteNextAction(int remainingAp, Action onTurnComplete){
            if (remainingAp <= 0 || Threat.IsNeutralized){
                onTurnComplete();
                return;
            }

            List<ThreatActionCard> playable = Threat.Deck.GetPlayableCards(Threat.Escalation, remainingAp);
            if (playable.Count == 0){
                onTurnComplete();
                return;
            }

            ThreatActionCard chosenCard = SelectWeightedCard(playable);
            TacticalZone targetZone = ResolveTargetZone(chosenCard);
            Threat.Deck.PutOnCooldown(chosenCard);
            int apCost = chosenCard.ApCost;

            chosenCard.Execute(Threat, targetZone, () => {
                int nextAp = remainingAp - apCost;
                if (actionDelaySeconds > 0f)
                    StartCoroutine(DelayRoutine(actionDelaySeconds, () => ExecuteNextAction(nextAp, onTurnComplete)));
                else
                    ExecuteNextAction(nextAp, onTurnComplete);
            });
        }

        /// Selects a card from the playable list using roulette-wheel weighted probability.
        private ThreatActionCard SelectWeightedCard(List<ThreatActionCard> cards){
            float totalWeight = 0f;
            foreach (ThreatActionCard card in cards)
                totalWeight += Mathf.Max(0.01f, card.AiWeight);

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (ThreatActionCard card in cards){
                cumulative += Mathf.Max(0.01f, card.AiWeight);
                if (roll <= cumulative)
                    return card;
            }

            return cards[0];
        }

        /// Resolves the spatial tactical zone targeted by the given action card.
        private TacticalZone ResolveTargetZone(ThreatActionCard card){
            switch (card.TargetType){
                case ThreatTargetType.AdjacentZone:
                    if (Threat.CurrentZone.AdjacentZones.Count > 0){
                        foreach (TacticalZone adj in Threat.CurrentZone.AdjacentZones)
                            if (Threat.GetHeroesInZone(adj).Count > 0)
                                return adj;

                        int index = UnityEngine.Random.Range(0, Threat.CurrentZone.AdjacentZones.Count);
                        return Threat.CurrentZone.AdjacentZones[index];
                    }
                    return Threat.CurrentZone;

                case ThreatTargetType.ZoneWithMostHeroes:
                    TacticalZone bestZone = null;
                    int maxHeroes = -1;
                    foreach (TacticalZone zone in TacticalZone.AllZones){
                        int heroCount = Threat.GetHeroesInZone(zone).Count;
                        if (heroCount > maxHeroes){
                            maxHeroes = heroCount;
                            bestZone = zone;
                        }
                    }
                    return bestZone ?? Threat.CurrentZone;

                default:
                    return Threat.CurrentZone;
            }
        }

        /// Delays execution by seconds before invoking callback.
        private IEnumerator DelayRoutine(float seconds, Action callback){
            yield return new WaitForSeconds(seconds);
            callback();
        }
    }
}
