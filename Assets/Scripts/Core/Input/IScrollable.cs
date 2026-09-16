namespace Core.Input{
    /// Contract for objects that receive mouse scroll input from PlayerBrain.
    public interface IScrollable{
        /// Executes scroll behavior with the provided scroll delta value.
        void OnScroll(float delta);
    }
}
