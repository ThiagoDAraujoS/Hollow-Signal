using Core.State;
using UnityEngine;

namespace World.Actors{
    public abstract class Sheet : TrackedTransform{ }

    public interface ISelectable{
        public Sheet     Sheet          { get; }
        public Transform SelectionCircle{ get; }
        public void TurnSelectionCircleOn();
        public void TurnSelectionCircleOff();
    }

}