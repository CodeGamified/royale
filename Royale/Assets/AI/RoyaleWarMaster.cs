// Copyright CodeGamified 2025-2026
// MIT License — Royale
using UnityEngine;
using Royale.Game;
using Royale.Scripting;

namespace Royale.AI
{
    /// <summary>
    /// War Master — runs a second bytecode program (right panel) that controls
    /// random battlefield events: airdrops, airstrikes (AOE denial zones),
    /// and zone center shifts.
    ///
    /// Same architecture as PopVuj's FateController: each difficulty tier
    /// is a Python script compiled + executed by the same bytecode engine.
    /// The script reads match state and issues environmental commands.
    /// </summary>
    public class RoyaleWarMaster : MonoBehaviour
    {
        private RoyaleMatchManager _match;
        private RoyaleZone _zone;
        private WarMasterDifficulty _difficulty;
        private RoyaleProgram _program;

        public WarMasterDifficulty Difficulty => _difficulty;
        public RoyaleProgram Program => _program;

        public void Initialize(RoyaleMatchManager match, RoyaleZone zone,
                               WarMasterDifficulty difficulty)
        {
            _match = match;
            _zone = zone;

            // War master uses a "phantom" player (index -1) so the IOHandler
            // can read match queries. The phantom never appears on the map.
            var phantomGo = new GameObject("WarMasterPhantom");
            phantomGo.SetActive(false);
            var phantom = phantomGo.AddComponent<RoyalePlayer>();
            phantom.Initialize(-1, false, 0f, 0f);

            _program = gameObject.AddComponent<RoyaleProgram>();
            SetDifficulty(difficulty);
        }

        public void SetDifficulty(WarMasterDifficulty difficulty)
        {
            _difficulty = difficulty;
            string code = GetSampleCode(difficulty);

            // Re-initialize with a fresh phantom for the IO handler
            var phantom = GetComponentInChildren<RoyalePlayer>(true);
            if (phantom == null)
            {
                var go = new GameObject("WarMasterPhantom");
                go.SetActive(false);
                phantom = go.AddComponent<RoyalePlayer>();
                phantom.Initialize(-1, false, 0f, 0f);
            }

            _program.Initialize(phantom, _match, _zone, code);
            Debug.Log($"[WarMaster] Difficulty → {difficulty} (running bytecode)");
        }

        // =================================================================
        // WAR MASTER SCRIPTS — each tier reads match state and issues
        // environmental commands. The script IS the war.
        // =================================================================

        public static string GetSampleCode(WarMasterDifficulty difficulty)
        {
            switch (difficulty)
            {
                case WarMasterDifficulty.Calm:     return CALM;
                case WarMasterDifficulty.Tactical: return TACTICAL;
                case WarMasterDifficulty.Warzone:  return WARZONE;
                case WarMasterDifficulty.Chaos:    return CHAOS;
                default: return CALM;
            }
        }

        // ── Calm: occasional supply drops ────────────────────────
        private static readonly string CALM = @"# ═══════════════════════════════════════
# CALM — Supply Drops Only
# Occasional airdrops, no hostility.
# One drop every ~45 seconds.
# ═══════════════════════════════════════
while True:
    t = get_match_time()
    alive = get_alive()
    phase = get_phase()

    # Drop supplies at regular intervals
    # Use time-based seed for position variety
    seed = t * 7 + 13
    x = seed % 160 - 80
    y = (seed * 3 + 7) % 160 - 80

    # More drops when fewer players remain
    interval = 45
    if alive < 8:
        interval = 30

    # Only drop if enough time passed (checked via modular time)
    tick = t % interval
    if tick < 1:
        # Drop near the zone center so it's contestable
        zx = get_zone_x()
        zy = get_zone_y()
        zr = get_zone_radius()
        dx = x % zr - zr / 2
        dy = y % zr - zr / 2
        spawn_airdrop(zx + dx, zy + dy)

    wait
";

        // ── Tactical: airdrops + light bombardment ───────────────
        private static readonly string TACTICAL = @"# ═══════════════════════════════════════
# TACTICAL — Airdrops & Light Bombardment
# Periodic supply drops + occasional strikes
# to keep players moving.
# ═══════════════════════════════════════
while True:
    t = get_match_time()
    alive = get_alive()
    phase = get_phase()
    zx = get_zone_x()
    zy = get_zone_y()
    zr = get_zone_radius()

    seed = t * 13 + 31

    # Airdrop every ~30 seconds
    drop_tick = t % 30
    if drop_tick < 1:
        dx = seed % 60 - 30
        dy = (seed * 7) % 60 - 30
        spawn_airdrop(zx + dx, zy + dy)

    # Airstrike every ~50 seconds (after phase 1)
    if phase > 0:
        strike_tick = t % 50
        if strike_tick < 1:
            # Strike outside zone edge to push players in
            sx = zx + (seed % 40 - 20) * 2
            sy = zy + ((seed * 3) % 40 - 20) * 2
            call_airstrike(sx, sy)

    # Nudge zone toward action in late game
    if alive < 6:
        if phase > 2:
            shift_zone(zx + seed % 10 - 5, zy + seed % 10 - 5)

    wait
";

        // ── Warzone: heavy bombardment + zone manipulation ───────
        private static readonly string WARZONE = @"# ═══════════════════════════════════════
# WARZONE — Heavy Bombardment
# Frequent drops, frequent strikes,
# aggressive zone shifts. Nowhere is safe.
# ═══════════════════════════════════════
while True:
    t = get_match_time()
    alive = get_alive()
    phase = get_phase()
    zx = get_zone_x()
    zy = get_zone_y()
    zr = get_zone_radius()

    seed = t * 17 + 41

    # Airdrop every ~20 seconds
    drop_tick = t % 20
    if drop_tick < 1:
        dx = seed % 80 - 40
        dy = (seed * 11) % 80 - 40
        spawn_airdrop(zx + dx, zy + dy)

    # Airstrike every ~25 seconds
    strike_tick = t % 25
    if strike_tick < 1:
        sx = seed % 120 - 60
        sy = (seed * 7) % 120 - 60
        call_airstrike(sx, sy)

    # Second airstrike after phase 2
    if phase > 1:
        strike2 = (t + 12) % 25
        if strike2 < 1:
            sx = (seed * 3) % 100 - 50
            sy = (seed * 5) % 100 - 50
            call_airstrike(sx, sy)

    # Aggressive zone shifts
    if phase > 0:
        shift_tick = t % 35
        if shift_tick < 1:
            nx = seed % 30 - 15
            ny = (seed * 3) % 30 - 15
            shift_zone(zx + nx, zy + ny)

    wait
";

        // ── Chaos: constant bombardment ──────────────────────────
        private static readonly string CHAOS = @"# ═══════════════════════════════════════
# CHAOS — Total Warfare
# Relentless airdrops, constant strikes,
# erratic zone shifts. Pure entropy.
# ═══════════════════════════════════════
while True:
    t = get_match_time()
    alive = get_alive()
    phase = get_phase()
    zx = get_zone_x()
    zy = get_zone_y()
    zr = get_zone_radius()

    seed = t * 23 + 59

    # Airdrop every ~12 seconds
    drop_tick = t % 12
    if drop_tick < 1:
        dx = seed % 100 - 50
        dy = (seed * 13) % 100 - 50
        spawn_airdrop(zx + dx, zy + dy)

    # Double airstrike every ~15 seconds
    strike_tick = t % 15
    if strike_tick < 1:
        sx = seed % 140 - 70
        sy = (seed * 7) % 140 - 70
        call_airstrike(sx, sy)

        sx2 = (seed * 11) % 100 - 50
        sy2 = (seed * 3) % 100 - 50
        call_airstrike(sx2, sy2)

    # Constant zone drift
    shift_tick = t % 18
    if shift_tick < 1:
        nx = seed % 40 - 20
        ny = (seed * 5) % 40 - 20
        shift_zone(zx + nx, zy + ny)

    # Late game: triple strike barrage
    if alive < 5:
        barrage = t % 10
        if barrage < 1:
            call_airstrike(zx + 20, zy)
            call_airstrike(zx - 20, zy)
            call_airstrike(zx, zy + 20)

    wait
";
    }
}
