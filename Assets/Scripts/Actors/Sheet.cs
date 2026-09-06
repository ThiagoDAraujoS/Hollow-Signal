using Core;

namespace Actors{
    public abstract class Sheet : TrackedTransform{ }

    public interface ISelectable{
        public Sheet Sheet{ get; }
    }

}