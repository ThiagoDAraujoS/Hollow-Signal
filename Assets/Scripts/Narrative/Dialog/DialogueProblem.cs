using System;

namespace Narrative.Dialog{
    /// Represents a dynamic problem challenge within a dialogue knot, instantiated from a ProblemArchetype.
    [Serializable]
    public class DialogueProblem{
        /// Unique identifier of the ProblemArchetype blueprint to instantiate.
        public string archetypeId;

        /// Base difficulty level of the problem (Cypher-scale, DC = level * 3).
        public int baseLevel;

        /// Outcome branch executed when any approach succeeds.
        public DialogueOutcome onSuccess;

        /// Outcome branch executed when an approach fails.
        public DialogueOutcome onFailure;
    }
}
