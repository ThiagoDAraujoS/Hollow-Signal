using System.Collections.Generic;
using Core.Localization;
using Core.Managers;
using UnityEngine;

namespace Core.Dialog{
    /// Base class for all auto-generated dialogue state machines.
    /// Inherits from TrackedBehaviour to synchronize dialogue variables
    /// and one-shot consumed choices directly with the save system and Blackboard.
    [DisallowMultipleComponent]
    public abstract class DialogueBehaviour : TrackedBehaviour{
        /// Collection of choice IDs that have already been chosen and cannot be repeated.
        public Tracked<List<string>> consumedChoices = new("dlg_consumed_choices", new List<string>());

        /// Optional localization table name containing strings for this dialogue.
        public virtual string LocTableName => null;

        /// Resolves and builds the DialogueNode definition for the requested knot name.
        public abstract DialogueNode GetNode(string knotId);

        /// Returns true if a one-shot choice has already been selected and consumed.
        public bool IsChoiceConsumed(string choiceId) => consumedChoices.Value != null && consumedChoices.Value.Contains(choiceId);

        /// Records that a one-shot choice was selected, persisting it to this entity's state.
        public void ConsumeChoice(string choiceId){
            consumedChoices.Value ??= new List<string>();

            if (!consumedChoices.Value.Contains(choiceId))
                consumedChoices.Value.Add(choiceId);
        }

        /// Resolves a localized string for this dialogue, prioritizing this dialogue's specific table.
        public string GetLocalizedString(string key, params object[] args){
            return LocalizationManager.Get(LocTableName, key, args);
        }
    }
}
