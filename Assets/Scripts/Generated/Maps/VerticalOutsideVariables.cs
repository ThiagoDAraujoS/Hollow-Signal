using Core;
using Core.State;
using Narrative.Dialog;
using UnityEngine;

namespace Generated.Maps{
    /// Concrete map-wide dialogue variables for the 'VerticalOutside' level scene.
    public class VerticalOutsideVariables : MapDialogVariables{
        public Tracked<bool> security_gate_unlocked = new("security_gate_unlocked", false);
    }
}
