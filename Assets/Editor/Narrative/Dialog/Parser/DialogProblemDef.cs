namespace Editor.Dialog.Parser{
    /// AST representation of an archetype problem declaration in a dialogue knot.
    public class DialogProblemDef{
        /// Unique identifier of the ProblemArchetype to instantiate.
        public string archetypeId;

        /// Base difficulty level of the problem.
        public int baseLevel;

        /// Outcome branch taken when any approach succeeds.
        public DialogOutcomeDef onSuccess;

        /// Outcome branch taken when an approach fails.
        public DialogOutcomeDef onFailure;
    }
}
