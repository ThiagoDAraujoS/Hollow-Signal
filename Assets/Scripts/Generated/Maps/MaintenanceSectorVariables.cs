using Core;
using Core.State;
using Narrative.Dialog;
using UnityEngine;

namespace Generated.Maps{
    /// Concrete map-wide dialogue variables for the 'MaintenanceSector' level scene.
    public class MaintenanceSectorVariables : MapDialogVariables{
        public Tracked<bool> sector_lights_active = new("sector_lights_active", false);
        public Tracked<bool> sector_airlock_open = new("sector_airlock_open", false);
    }
}
