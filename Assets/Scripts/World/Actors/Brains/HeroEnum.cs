using System;

namespace World.Actors.Brains{
    [Flags]
    public enum HeroEnum{
        None      = 0,
        Leader    = 1 << 0,
        Brute     = 1 << 1,
        Thief     = 1 << 2,
        Scientist = 1 << 3,
        Mage      = 1 << 4,
        All       = Leader | Brute | Thief | Scientist | Mage
    }
}
