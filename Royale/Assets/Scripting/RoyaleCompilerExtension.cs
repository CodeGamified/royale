// Copyright CodeGamified 2025-2026
// MIT License — Royale
using System.Collections.Generic;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;

namespace Royale.Scripting
{
    /// <summary>
    /// Compiler extension for Royale — registers all 37 builtins.
    /// </summary>
    public class RoyaleCompilerExtension : ICompilerExtension
    {
        public void RegisterBuiltins(CompilerContext ctx) { }

        public bool TryCompileCall(string functionName, List<AstNodes.ExprNode> args,
                                   CompilerContext ctx, int sourceLine)
        {
            switch (functionName)
            {
                // ── Queries: no args, result in R0 ──
                case "get_x":
                    EmitCustom(ctx, RoyaleOpCode.GET_X, sourceLine, "get_x → R0");
                    return true;
                case "get_y":
                    EmitCustom(ctx, RoyaleOpCode.GET_Y, sourceLine, "get_y → R0");
                    return true;
                case "get_health":
                    EmitCustom(ctx, RoyaleOpCode.GET_HEALTH, sourceLine, "get_health → R0");
                    return true;
                case "get_armor":
                    EmitCustom(ctx, RoyaleOpCode.GET_ARMOR, sourceLine, "get_armor → R0");
                    return true;
                case "get_alive":
                    EmitCustom(ctx, RoyaleOpCode.GET_ALIVE_COUNT, sourceLine, "get_alive → R0");
                    return true;
                case "get_kills":
                    EmitCustom(ctx, RoyaleOpCode.GET_KILL_COUNT, sourceLine, "get_kills → R0");
                    return true;

                // Zone
                case "get_zone_x":
                    EmitCustom(ctx, RoyaleOpCode.GET_ZONE_X, sourceLine, "get_zone_x → R0");
                    return true;
                case "get_zone_y":
                    EmitCustom(ctx, RoyaleOpCode.GET_ZONE_Y, sourceLine, "get_zone_y → R0");
                    return true;
                case "get_zone_radius":
                    EmitCustom(ctx, RoyaleOpCode.GET_ZONE_RADIUS, sourceLine, "get_zone_radius → R0");
                    return true;
                case "get_zone_shrinking":
                    EmitCustom(ctx, RoyaleOpCode.GET_ZONE_SHRINKING, sourceLine, "get_zone_shrinking → R0");
                    return true;
                case "get_in_zone":
                    EmitCustom(ctx, RoyaleOpCode.GET_IN_ZONE, sourceLine, "get_in_zone → R0");
                    return true;

                // Weapon
                case "get_weapon":
                    EmitCustom(ctx, RoyaleOpCode.GET_WEAPON, sourceLine, "get_weapon → R0");
                    return true;
                case "get_ammo":
                    EmitCustom(ctx, RoyaleOpCode.GET_AMMO, sourceLine, "get_ammo → R0");
                    return true;
                case "get_ammo_reserve":
                    EmitCustom(ctx, RoyaleOpCode.GET_AMMO_RESERVE, sourceLine, "get_ammo_reserve → R0");
                    return true;
                case "get_reloading":
                    EmitCustom(ctx, RoyaleOpCode.GET_RELOADING, sourceLine, "get_reloading → R0");
                    return true;
                case "get_weapon_range":
                    EmitCustom(ctx, RoyaleOpCode.GET_WEAPON_RANGE, sourceLine, "get_weapon_range → R0");
                    return true;

                // Enemy
                case "get_enemy_dist":
                    EmitCustom(ctx, RoyaleOpCode.GET_ENEMY_DIST, sourceLine, "get_enemy_dist → R0");
                    return true;
                case "get_enemy_angle":
                    EmitCustom(ctx, RoyaleOpCode.GET_ENEMY_ANGLE, sourceLine, "get_enemy_angle → R0");
                    return true;
                case "get_enemy_health":
                    EmitCustom(ctx, RoyaleOpCode.GET_ENEMY_HEALTH, sourceLine, "get_enemy_health → R0");
                    return true;

                // Crate
                case "get_crate_dist":
                    EmitCustom(ctx, RoyaleOpCode.GET_CRATE_DIST, sourceLine, "get_crate_dist → R0");
                    return true;
                case "get_crate_angle":
                    EmitCustom(ctx, RoyaleOpCode.GET_CRATE_ANGLE, sourceLine, "get_crate_angle → R0");
                    return true;

                // Cover
                case "get_cover_dist":
                    EmitCustom(ctx, RoyaleOpCode.GET_COVER_DIST, sourceLine, "get_cover_dist → R0");
                    return true;
                case "get_cover_angle":
                    EmitCustom(ctx, RoyaleOpCode.GET_COVER_ANGLE, sourceLine, "get_cover_angle → R0");
                    return true;

                // Inventory
                case "get_slot_weapon":
                    if (args != null && args.Count > 0) args[0].Compile(ctx);
                    EmitCustom(ctx, RoyaleOpCode.GET_SLOT_WEAPON, sourceLine, "get_slot_weapon(R0) → R0");
                    return true;
                case "get_has_heal":
                    EmitCustom(ctx, RoyaleOpCode.GET_HAS_HEAL, sourceLine, "get_has_heal → R0");
                    return true;
                case "get_facing":
                    EmitCustom(ctx, RoyaleOpCode.GET_FACING, sourceLine, "get_facing → R0");
                    return true;
                case "get_scope":
                    EmitCustom(ctx, RoyaleOpCode.GET_SCOPE, sourceLine, "get_scope → R0");
                    return true;
                case "get_view_range":
                    EmitCustom(ctx, RoyaleOpCode.GET_VIEW_RANGE, sourceLine, "get_view_range → R0");
                    return true;
                case "get_input":
                    EmitCustom(ctx, RoyaleOpCode.GET_INPUT, sourceLine, "get_input → R0");
                    return true;

                // ── Commands: arg from R0 ──
                case "move_toward":
                    if (args != null && args.Count > 0) args[0].Compile(ctx);
                    EmitCustom(ctx, RoyaleOpCode.MOVE_TOWARD, sourceLine, "move_toward(R0)");
                    return true;
                case "set_facing":
                    if (args != null && args.Count > 0) args[0].Compile(ctx);
                    EmitCustom(ctx, RoyaleOpCode.SET_FACING, sourceLine, "set_facing(R0)");
                    return true;
                case "shoot":
                    EmitCustom(ctx, RoyaleOpCode.SHOOT, sourceLine, "shoot()");
                    return true;
                case "reload":
                    EmitCustom(ctx, RoyaleOpCode.RELOAD, sourceLine, "reload()");
                    return true;
                case "loot":
                    EmitCustom(ctx, RoyaleOpCode.LOOT, sourceLine, "loot()");
                    return true;
                case "swap_weapon":
                    EmitCustom(ctx, RoyaleOpCode.SWAP_WEAPON, sourceLine, "swap_weapon()");
                    return true;
                case "use_heal":
                    EmitCustom(ctx, RoyaleOpCode.USE_HEAL, sourceLine, "use_heal()");
                    return true;
                case "move_to_zone":
                    EmitCustom(ctx, RoyaleOpCode.MOVE_TO_ZONE, sourceLine, "move_to_zone()");
                    return true;

                // ── War Master fate commands ──
                case "get_match_time":
                    EmitCustom(ctx, RoyaleOpCode.GET_MATCH_TIME, sourceLine, "get_match_time → R0");
                    return true;
                case "get_phase":
                    EmitCustom(ctx, RoyaleOpCode.GET_PHASE, sourceLine, "get_phase → R0");
                    return true;
                case "spawn_airdrop":
                    if (args != null && args.Count >= 2)
                    {
                        args[0].Compile(ctx);
                        ctx.Emit(OpCode.PUSH, 0, 0, 0, sourceLine, "push R0 (x)");
                        args[1].Compile(ctx);
                        ctx.Emit(OpCode.MOV, 1, 0, 0, sourceLine, "R1 ← R0 (y)");
                        ctx.Emit(OpCode.POP, 0, 0, 0, sourceLine, "pop R0 (x)");
                    }
                    EmitCustom(ctx, RoyaleOpCode.SPAWN_AIRDROP, sourceLine, "spawn_airdrop(R0,R1)");
                    return true;
                case "call_airstrike":
                    if (args != null && args.Count >= 2)
                    {
                        args[0].Compile(ctx);
                        ctx.Emit(OpCode.PUSH, 0, 0, 0, sourceLine, "push R0 (x)");
                        args[1].Compile(ctx);
                        ctx.Emit(OpCode.MOV, 1, 0, 0, sourceLine, "R1 ← R0 (y)");
                        ctx.Emit(OpCode.POP, 0, 0, 0, sourceLine, "pop R0 (x)");
                    }
                    EmitCustom(ctx, RoyaleOpCode.CALL_AIRSTRIKE, sourceLine, "call_airstrike(R0,R1)");
                    return true;
                case "shift_zone":
                    if (args != null && args.Count >= 2)
                    {
                        args[0].Compile(ctx);
                        ctx.Emit(OpCode.PUSH, 0, 0, 0, sourceLine, "push R0 (x)");
                        args[1].Compile(ctx);
                        ctx.Emit(OpCode.MOV, 1, 0, 0, sourceLine, "R1 ← R0 (y)");
                        ctx.Emit(OpCode.POP, 0, 0, 0, sourceLine, "pop R0 (x)");
                    }
                    EmitCustom(ctx, RoyaleOpCode.SHIFT_ZONE, sourceLine, "shift_zone(R0,R1)");
                    return true;

                default:
                    return false;
            }
        }

        public bool TryCompileMethodCall(string objectName, string methodName,
                                         List<AstNodes.ExprNode> args,
                                         CompilerContext ctx, int sourceLine)
        {
            return false;
        }

        public bool TryCompileObjectDecl(string typeName, string varName,
                                         List<AstNodes.ExprNode> constructorArgs,
                                         CompilerContext ctx, int sourceLine)
        {
            return false;
        }

        private static void EmitCustom(CompilerContext ctx, RoyaleOpCode op,
                                        int sourceLine, string comment)
        {
            ctx.Emit(OpCode.CUSTOM_0 + (int)op, 0, 0, 0, sourceLine, comment);
        }
    }
}
