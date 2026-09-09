using Core;
using Core.Dialog;

namespace Generated.Maps{
    /// Concrete map-wide dialogue variables for the 'MedicalBay' level scene.
    public class MedicalBayVariables : MapDialogVariables{
        public Tracked<bool> generator_power = new("generator_power", false);
    }
}
