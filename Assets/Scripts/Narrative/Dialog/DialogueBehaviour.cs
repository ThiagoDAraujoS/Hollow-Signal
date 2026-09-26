using System.Collections.Generic;
using Core.State;
using Data.Effects;
using Narrative.Localization;
using UnityEngine;
using World;
using World.Actors.Brains;
using World.Actors.Player;

namespace Narrative.Dialog{
    /// Base class for all auto-generated dialogue state machines.
    /// Inherits from TrackedBehaviour and implements IEffect to execute dialogue interactions.
    [DisallowMultipleComponent]
    public abstract class DialogueBehaviour : TrackedBehaviour, IEffect{
        [Header("Dialogue Interaction")]
        [SerializeField] private string startingKnot = "Main";

        /// Collection of choice IDs that have already been chosen and cannot be repeated.
        public Tracked<List<string>> consumedChoices = new("dlg_consumed_choices", new List<string>());

        /// Active character currently participating in this dialogue session.
        public Character CurrentUser{ get; private set; }

        /// Indicates whether this dialogue is currently engaged by a character.
        public bool IsInUse => CurrentUser != null;

        /// Optional localization table name containing strings for this dialogue.
        protected virtual string LocTableName => null;

        public string StartingKnot{
            get => startingKnot;
            set => startingKnot = value;
        }

        /// Executes this dialogue as an IEffect.
        public void Run(EffectContext context) => Use(context.Character != null ? context.Character : PlayerBrain.Lead.sheet);

        /// Starts dialogue session on the interacting character if not already in use by another character.
        public virtual void Use(CharacterSheet whosUsing){
            Character character = whosUsing ? whosUsing.GetComponent<Character>() : null;
            if (CurrentUser != null && CurrentUser != character)
                return;
            CurrentUser = character;
            if (character != null && character.dialogueSession != null)
                character.dialogueSession.StartDialogue(this, startingKnot);
        }

        /// Releases the currently occupying user from this dialogue.
        public void ReleaseUser(Character character){
            if (CurrentUser == character)
                CurrentUser = null;
        }

        /// Resolves and builds the DialogueNode definition for the requested knot name.
        public abstract DialogueNode GetNode(string knotId);

        /// Returns true if a one-shot choice has already been selected and consumed.
        protected bool IsChoiceConsumed(string choiceId) => consumedChoices.Value != null && consumedChoices.Value.Contains(choiceId);

        /// Records that a one-shot choice was selected, persisting it to this entity's state.
        public void ConsumeChoice(string choiceId){
            consumedChoices.Value ??= new List<string>();
            if (!consumedChoices.Value.Contains(choiceId))
                consumedChoices.Value.Add(choiceId);
        }

        /// Resolves a localized string for this dialogue, prioritizing this dialogue's specific table.
        public string GetLocalizedString(string key, params object[] args) =>
            string.IsNullOrEmpty(key) ? string.Empty : LocalizationManager.Get(LocTableName, key, args);
    }
}
