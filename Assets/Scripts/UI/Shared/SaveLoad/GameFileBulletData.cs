using System;
using UnityEngine;

namespace UI.Shared.SaveLoad{
    /// Data container for populating a single save bullet on a carousel.
    [Serializable]
    public struct GameFileBulletData{
        public string slotName;
        public string characterName;
        public string location;
        public string timestamp;
        public Sprite snapshot;

        public GameFileBulletData(string slotName, string location, string timestamp, Sprite snapshot = null, string characterName = null){
            this.slotName      = slotName;
            this.characterName = characterName ?? slotName;
            this.location      = location;
            this.timestamp     = timestamp;
            this.snapshot      = snapshot;
        }
    }
}
