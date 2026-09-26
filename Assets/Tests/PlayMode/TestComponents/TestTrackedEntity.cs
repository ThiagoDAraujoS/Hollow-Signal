using Core.State;

namespace CRPG.Tests.PlayMode.TestComponents{
    /// Test component wrapping tracked integer and string properties.
    public class TestTrackedEntity : TrackedBehaviour{
        public Tracked<int>    Health = new("Health", 100);
        public Tracked<string> Status = new("Status", "Alive");
    }
}
