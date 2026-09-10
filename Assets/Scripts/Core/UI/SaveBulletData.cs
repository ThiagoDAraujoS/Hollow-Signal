using System;
using UnityEngine;

namespace Core.UI
{
    /// Data container for populating a single save bullet on a carousel.
    [Serializable]
    public struct SaveBulletData
    {
        public string slotName;
        public string location;
        public string timestamp;
        public Sprite snapshot;

        public SaveBulletData(string slotName, string location, string timestamp, Sprite snapshot = null)
        {
            this.slotName = slotName;
            this.location = location;
            this.timestamp = timestamp;
            this.snapshot = snapshot;
        }
    }
}
