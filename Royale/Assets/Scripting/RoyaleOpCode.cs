// Copyright CodeGamified 2025-2026
// MIT License — Royale
namespace Royale.Scripting
{
    /// <summary>
    /// Royale-specific opcodes mapped to CUSTOM_0..CUSTOM_N.
    /// 29 queries + 8 commands = 37 opcodes.
    /// </summary>
    public enum RoyaleOpCode
    {
        // === Queries (read → R0) ===
        GET_X              = 0,   // Player X position
        GET_Y              = 1,   // Player Y position (Z in world space)
        GET_HEALTH         = 2,   // Current HP (0-100)
        GET_ARMOR          = 3,   // Current armor points (0-50)
        GET_ALIVE_COUNT    = 4,   // Players still alive
        GET_KILL_COUNT     = 5,   // Player's elimination count

        // Zone
        GET_ZONE_X         = 6,   // Safe zone center X
        GET_ZONE_Y         = 7,   // Safe zone center Y
        GET_ZONE_RADIUS    = 8,   // Current safe zone radius
        GET_ZONE_SHRINKING = 9,   // 1 if zone is actively shrinking, 0 if paused
        GET_IN_ZONE        = 10,  // 1 if player inside safe zone, 0 if outside

        // Weapon
        GET_WEAPON         = 11,  // Current weapon type (0-5)
        GET_AMMO           = 12,  // Ammo in current weapon magazine
        GET_AMMO_RESERVE   = 13,  // Reserve ammo for current weapon
        GET_RELOADING      = 14,  // 1 if currently reloading, 0 otherwise
        GET_WEAPON_RANGE   = 15,  // Effective range of current weapon

        // Nearest enemy (line-of-sight + view range gated)
        GET_ENEMY_DIST     = 16,  // Distance to nearest visible enemy (-1 if none)
        GET_ENEMY_ANGLE    = 17,  // Angle to nearest visible enemy (degrees)
        GET_ENEMY_HEALTH   = 18,  // Approx health of nearest visible enemy

        // Nearest loot
        GET_CRATE_DIST     = 19,  // Distance to nearest unopened crate (-1 if none)
        GET_CRATE_ANGLE    = 20,  // Angle to nearest crate

        // Nearest cover
        GET_COVER_DIST     = 21,  // Distance to nearest cover (-1 if none)
        GET_COVER_ANGLE    = 22,  // Angle to nearest cover

        // Inventory
        GET_SLOT_WEAPON    = 23,  // Weapon type in slot R0 (0 or 1)
        GET_HAS_HEAL       = 24,  // 1 if player has a healing item
        GET_FACING         = 25,  // Current aim angle in degrees
        GET_SCOPE          = 26,  // Current scope level (0-4)
        GET_VIEW_RANGE     = 27,  // Current view/detection radius

        // Input (manual play)
        GET_INPUT          = 28,  // Raw manual input value

        // === Commands (act → R0 = 1 success / 0 fail) ===
        MOVE_TOWARD        = 29,  // Move in direction R0 (angle degrees)
        SET_FACING         = 30,  // Aim at angle R0 (degrees)
        SHOOT              = 31,  // Fire current weapon
        RELOAD             = 32,  // Start reload
        LOOT               = 33,  // Pick up nearest crate/item within 2 units
        SWAP_WEAPON        = 34,  // Switch to other weapon slot
        USE_HEAL           = 35,  // Use healing item (stand still 3s)
        MOVE_TO_ZONE       = 36,  // Convenience: move toward zone center

        // === War Master fate commands (right panel script) ===
        GET_MATCH_TIME     = 37,  // Elapsed match time in seconds
        GET_PHASE          = 38,  // Current zone phase (0-4)
        SPAWN_AIRDROP      = 39,  // Spawn rare crate at (R0=x, R1=y)
        CALL_AIRSTRIKE     = 40,  // AOE denial at (R0=x, R1=y), 15u radius, 20s
        SHIFT_ZONE         = 41,  // Bias next zone center toward (R0=x, R1=y)
    }
}
