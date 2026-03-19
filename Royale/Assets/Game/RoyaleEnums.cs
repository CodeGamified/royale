// Copyright CodeGamified 2025-2026
// MIT License — Royale
namespace Royale.Game
{
    public enum WeaponType
    {
        Fists    = 0,
        Pistol   = 1,
        Shotgun  = 2,
        SMG      = 3,
        AR       = 4,
        Sniper   = 5
    }

    public enum CrateRarity
    {
        Common    = 0,  // 60%
        Uncommon  = 1,  // 25%
        Rare      = 2,  // 12%
        Legendary = 3   //  3%
    }

    public enum ScopeLevel
    {
        None = 0,   // 30 view, size 30
        X2   = 1,   // 40 view, size 22.5
        X4   = 2,   // 55 view, size 16
        X8   = 3,   // 75 view, size 11
        X15  = 4    // 100 view, size 7.5
    }
}
