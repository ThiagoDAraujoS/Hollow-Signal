using System;
using Data;
using UI.Dice;
using World.Actors.Player;

namespace Narrative.Skills{
    public struct SkillEvaluationContext{
        public CharacterSheet actor;
        public ActionStyle style;
        public Skill bestSkill;
        public int skillBonus;
        public Mastery contributingMastery;
        public int targetDc;
    }

    public struct SkillEvaluationResult{
        public SkillEvaluationContext context;
        public bool passed;
        public int[] dice;
        public int diceTotal;
        public int finalTotal;
        public string selectedQuip;
    }

    /// Evaluates skill challenges against character masteries and broadcasts MVC viewer events.
    public static class SkillEvaluator{
        public static event Action<SkillEvaluationContext> OnEvaluationStarted;
        public static event Action<int[], int> OnDiceLanded;
        public static event Action<SkillEvaluationResult> OnEvaluationCompleted;

        /// Evaluates an action style check for the acting hero and reports the outcome via callback and events.
        public static void Evaluate(CharacterSheet actor, ActionStyle style, Action<SkillEvaluationResult> onComplete = null){
            Skill bestSkill = DetermineBestSkill(actor, style);
            int bonus = actor.GetEffectiveSkill(bestSkill);
            Mastery mastery = actor.GetContributingMastery(bestSkill);

            SkillEvaluationContext context = new(){
                actor = actor,
                style = style,
                bestSkill = bestSkill,
                skillBonus = bonus,
                contributingMastery = mastery,
                targetDc = style.targetDc
            };

            OnEvaluationStarted?.Invoke(context);

            Action<int[]> onDiceResolved = dice => {
                int diceSum = 0;
                foreach (int die in dice)
                    diceSum += die;

                OnDiceLanded?.Invoke(dice, diceSum);

                int finalTotal = diceSum + bonus;
                bool passed = finalTotal >= style.targetDc;
                string quip = passed ? style.GetRandomSuccessQuip() : style.GetRandomFailureQuip();

                SkillEvaluationResult result = new(){
                    context = context,
                    passed = passed,
                    dice = dice,
                    diceTotal = diceSum,
                    finalTotal = finalTotal,
                    selectedQuip = quip
                };

                OnEvaluationCompleted?.Invoke(result);
                onComplete?.Invoke(result);
            };

            DiceRollController.Roll(onDiceResolved);
        }

        /// Selects the applicable skill that yields the highest effective bonus on the character sheet.
        private static Skill DetermineBestSkill(CharacterSheet actor, ActionStyle style){
            Skill best = style.applicableSkills[0];
            int bestBonus = -1;
            foreach (Skill skill in style.applicableSkills){
                int bonus = actor.GetEffectiveSkill(skill);
                if (bonus > bestBonus){
                    bestBonus = bonus;
                    best = skill;
                }
            }
            return best;
        }
    }
}
