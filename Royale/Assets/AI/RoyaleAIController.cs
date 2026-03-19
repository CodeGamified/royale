// Copyright CodeGamified 2025-2026
// MIT License — Royale
using UnityEngine;
using Royale.Game;

namespace Royale.AI
{
    /// <summary>
    /// AI controller for battle royale bots — runs the same bytecode engine
    /// as the player. Each difficulty is a Python script.
    /// 5 easy, 5 medium, 5 hard = 15 bots.
    /// </summary>
    public class RoyaleAIController : MonoBehaviour
    {
        public enum AIDifficulty { Easy, Medium, Hard }

        private RoyalePlayer _player;
        private RoyaleMatchManager _match;
        private RoyaleZone _zone;
        private Scripting.RoyaleProgram _program;

        public AIDifficulty Difficulty { get; private set; }

        public void Initialize(RoyalePlayer player, RoyaleMatchManager match,
                               RoyaleZone zone, AIDifficulty difficulty)
        {
            _player = player;
            _match = match;
            _zone = zone;
            Difficulty = difficulty;

            _program = gameObject.AddComponent<Scripting.RoyaleProgram>();
            string code = GetAICode(difficulty);
            _program.Initialize(player, match, zone, code);
        }

        public static string GetAICode(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return @"# Easy bot — basic survival
while True:
    # Stay in zone
    if get_in_zone() == 0:
        move_to_zone()
        wait

    # Loot nearby crates
    cd = get_crate_dist()
    if cd > 0:
        if cd < 15:
            move_toward(get_crate_angle())
            if cd < 2:
                loot()
            wait

    # Shoot if enemy close
    ed = get_enemy_dist()
    if ed > 0:
        if ed < 15:
            set_facing(get_enemy_angle())
            shoot()
            if get_ammo() == 0:
                reload()
            wait

    # Wander toward zone center
    move_to_zone()
    wait
";

                case AIDifficulty.Medium:
                    return @"# Medium bot — loot first, then fight
while True:
    hp = get_health()

    # Heal when low
    if hp < 35:
        if get_has_heal() == 1:
            use_heal()

    # Zone awareness
    if get_in_zone() == 0:
        move_to_zone()
        wait

    # Loot until armed
    wep = get_weapon()
    if wep == 0:
        cd = get_crate_dist()
        if cd > 0:
            move_toward(get_crate_angle())
            if cd < 2:
                loot()
        wait

    # Engage enemies
    ed = get_enemy_dist()
    if ed > 0:
        angle = get_enemy_angle()
        set_facing(angle)
        rng = get_weapon_range()
        if ed < rng:
            shoot()
            if get_ammo() == 0:
                reload()
        else:
            move_toward(angle)
        wait

    # Loot more
    cd = get_crate_dist()
    if cd > 0:
        if cd < 25:
            move_toward(get_crate_angle())
            if cd < 2:
                loot()
    else:
        move_to_zone()
    wait
";

                case AIDifficulty.Hard:
                    return @"# Hard bot — aggressive, uses cover, scope-aware
while True:
    hp = get_health()
    alive = get_alive()

    # Heal if safe
    if hp < 50:
        if get_has_heal() == 1:
            ed = get_enemy_dist()
            if ed < 0:
                use_heal()
            if ed > 30:
                use_heal()

    # Zone priority
    if get_in_zone() == 0:
        move_to_zone()
        wait

    # Always try to improve scope
    scope = get_scope()
    if scope < 2:
        cd = get_crate_dist()
        if cd > 0:
            if cd < 30:
                move_toward(get_crate_angle())
                if cd < 2:
                    loot()
                wait

    # Combat
    ed = get_enemy_dist()
    if ed > 0:
        angle = get_enemy_angle()
        set_facing(angle)
        rng = get_weapon_range()
        if ed < rng:
            shoot()
            if get_ammo() == 0:
                reload()
            # Strafe while fighting
            move_toward(angle + 90)
        else:
            if ed < rng + 20:
                move_toward(angle)
        wait

    # Late game — play safe near cover
    if alive < 5:
        cvd = get_cover_dist()
        if cvd > 3:
            move_toward(get_cover_angle())
            wait

    # Loot or drift
    cd = get_crate_dist()
    if cd > 0:
        move_toward(get_crate_angle())
        if cd < 2:
            loot()
    else:
        move_to_zone()
    wait
";

                default:
                    return "move_to_zone()\nwait\n";
            }
        }
    }
}
