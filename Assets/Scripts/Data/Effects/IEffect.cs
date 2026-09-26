namespace Data.Effects{
    /// Contract for executable actions and effects.
    public interface IEffect{
        /// Executes effect logic using the provided context.
        void Run(EffectContext context);
    }
}
