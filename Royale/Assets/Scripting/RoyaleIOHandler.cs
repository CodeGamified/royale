// Copyright CodeGamified 2025-2026
// MIT License — Royale
using CodeGamified.Engine;
using CodeGamified.Time;
using Royale.Game;
using UnityEngine;

namespace Royale.Scripting
{
    /// <summary>
    /// Game I/O handler for Royale — bridges CUSTOM opcodes to player/match state.
    /// </summary>
    public class RoyaleIOHandler : IGameIOHandler
    {
        private readonly RoyalePlayer _player;
        private readonly RoyaleMatchManager _match;
        private readonly RoyaleZone _zone;

        public RoyaleIOHandler(RoyalePlayer player, RoyaleMatchManager match, RoyaleZone zone)
        {
            _player = player;
            _match = match;
            _zone = zone;
        }

        public bool PreExecute(Instruction inst, MachineState state) => true;

        public void ExecuteIO(Instruction inst, MachineState state)
        {
            int op = (int)inst.Op - (int)OpCode.CUSTOM_0;

            switch ((RoyaleOpCode)op)
            {
                // ── Player state ──
                case RoyaleOpCode.GET_X:
                    state.SetRegister(0, _player.posX);
                    break;
                case RoyaleOpCode.GET_Y:
                    state.SetRegister(0, _player.posZ);
                    break;
                case RoyaleOpCode.GET_HEALTH:
                    state.SetRegister(0, _player.health);
                    break;
                case RoyaleOpCode.GET_ARMOR:
                    state.SetRegister(0, _player.armor);
                    break;
                case RoyaleOpCode.GET_ALIVE_COUNT:
                    state.SetRegister(0, _match.AliveCount);
                    break;
                case RoyaleOpCode.GET_KILL_COUNT:
                    state.SetRegister(0, _player.kills);
                    break;

                // ── Zone ──
                case RoyaleOpCode.GET_ZONE_X:
                    state.SetRegister(0, _zone?.CenterX ?? 0f);
                    break;
                case RoyaleOpCode.GET_ZONE_Y:
                    state.SetRegister(0, _zone?.CenterZ ?? 0f);
                    break;
                case RoyaleOpCode.GET_ZONE_RADIUS:
                    state.SetRegister(0, _zone?.CurrentRadius ?? 999f);
                    break;
                case RoyaleOpCode.GET_ZONE_SHRINKING:
                    state.SetRegister(0, (_zone != null && _zone.IsShrinking) ? 1f : 0f);
                    break;
                case RoyaleOpCode.GET_IN_ZONE:
                    state.SetRegister(0,
                        (_zone != null && _zone.IsInside(_player.posX, _player.posZ)) ? 1f : 0f);
                    break;

                // ── Weapon ──
                case RoyaleOpCode.GET_WEAPON:
                    state.SetRegister(0, (int)_player.ActiveWeapon);
                    break;
                case RoyaleOpCode.GET_AMMO:
                    state.SetRegister(0, _player.ammo[_player.activeSlot] == int.MaxValue
                        ? 999f : _player.ammo[_player.activeSlot]);
                    break;
                case RoyaleOpCode.GET_AMMO_RESERVE:
                    state.SetRegister(0, _player.ammoReserve[_player.activeSlot]);
                    break;
                case RoyaleOpCode.GET_RELOADING:
                    state.SetRegister(0, _player.IsReloading ? 1f : 0f);
                    break;
                case RoyaleOpCode.GET_WEAPON_RANGE:
                    state.SetRegister(0, _player.ActiveStats.Range);
                    break;

                // ── Nearest enemy (LOS + view range gated) ──
                case RoyaleOpCode.GET_ENEMY_DIST:
                {
                    var enemy = _match.FindNearestVisibleEnemy(_player);
                    state.SetRegister(0, enemy != null ? _player.DistanceTo(enemy) : -1f);
                    break;
                }
                case RoyaleOpCode.GET_ENEMY_ANGLE:
                {
                    var enemy = _match.FindNearestVisibleEnemy(_player);
                    state.SetRegister(0, enemy != null
                        ? _player.AngleTo(enemy.posX, enemy.posZ) : 0f);
                    break;
                }
                case RoyaleOpCode.GET_ENEMY_HEALTH:
                {
                    var enemy = _match.FindNearestVisibleEnemy(_player);
                    state.SetRegister(0, enemy != null ? enemy.health : 0f);
                    break;
                }

                // ── Nearest crate ──
                case RoyaleOpCode.GET_CRATE_DIST:
                {
                    var crate = _match.FindNearestCrate(_player);
                    if (crate != null)
                    {
                        float dx = crate.PosX - _player.posX;
                        float dz = crate.PosZ - _player.posZ;
                        state.SetRegister(0, Mathf.Sqrt(dx * dx + dz * dz));
                    }
                    else
                    {
                        state.SetRegister(0, -1f);
                    }
                    break;
                }
                case RoyaleOpCode.GET_CRATE_ANGLE:
                {
                    var crate = _match.FindNearestCrate(_player);
                    state.SetRegister(0, crate != null
                        ? _player.AngleTo(crate.PosX, crate.PosZ) : 0f);
                    break;
                }

                // ── Nearest cover ──
                case RoyaleOpCode.GET_COVER_DIST:
                {
                    var cover = _match.FindNearestCover(_player);
                    state.SetRegister(0, cover.z >= 0 ? cover.z : -1f);
                    break;
                }
                case RoyaleOpCode.GET_COVER_ANGLE:
                {
                    var cover = _match.FindNearestCover(_player);
                    state.SetRegister(0, cover.z >= 0
                        ? _player.AngleTo(cover.x, cover.y) : 0f);
                    break;
                }

                // ── Inventory ──
                case RoyaleOpCode.GET_SLOT_WEAPON:
                {
                    int slot = Mathf.Clamp((int)state.Registers[0], 0, 1);
                    state.SetRegister(0, (int)_player.weaponSlots[slot]);
                    break;
                }
                case RoyaleOpCode.GET_HAS_HEAL:
                    state.SetRegister(0, _player.healCount > 0 ? 1f : 0f);
                    break;
                case RoyaleOpCode.GET_FACING:
                    state.SetRegister(0, _player.facing);
                    break;
                case RoyaleOpCode.GET_SCOPE:
                    state.SetRegister(0, (int)_player.scope);
                    break;
                case RoyaleOpCode.GET_VIEW_RANGE:
                    state.SetRegister(0, _player.ViewRadius);
                    break;
                case RoyaleOpCode.GET_INPUT:
                    state.SetRegister(0,
                        RoyaleInputProvider.Instance?.CurrentInput ?? 0f);
                    break;

                // ── Commands ──
                case RoyaleOpCode.MOVE_TOWARD:
                {
                    float angle = state.Registers[0];
                    _player.moveAngle = angle;
                    _player.moveRequested = true;
                    state.SetRegister(0, 1f);
                    break;
                }
                case RoyaleOpCode.SET_FACING:
                {
                    _player.facing = state.Registers[0];
                    state.SetRegister(0, 1f);
                    break;
                }
                case RoyaleOpCode.SHOOT:
                {
                    _match.FireWeapon(_player);
                    state.SetRegister(0, _player.isShooting ? 1f : 0f);
                    _player.isShooting = true;
                    break;
                }
                case RoyaleOpCode.RELOAD:
                {
                    bool ok = _player.TryReload();
                    state.SetRegister(0, ok ? 1f : 0f);
                    break;
                }
                case RoyaleOpCode.LOOT:
                {
                    bool ok = _match.TryLootNearest(_player);
                    state.SetRegister(0, ok ? 1f : 0f);
                    break;
                }
                case RoyaleOpCode.SWAP_WEAPON:
                {
                    bool ok = _player.TrySwapWeapon();
                    state.SetRegister(0, ok ? 1f : 0f);
                    break;
                }
                case RoyaleOpCode.USE_HEAL:
                {
                    bool ok = _player.TryUseHeal();
                    state.SetRegister(0, ok ? 1f : 0f);
                    break;
                }
                case RoyaleOpCode.MOVE_TO_ZONE:
                {
                    if (_zone != null)
                    {
                        float angle = _player.AngleTo(_zone.CenterX, _zone.CenterZ);
                        _player.moveAngle = angle;
                        _player.moveRequested = true;
                        state.SetRegister(0, 1f);
                    }
                    else
                    {
                        state.SetRegister(0, 0f);
                    }
                    break;
                }

                // ── War Master fate commands ──
                case RoyaleOpCode.GET_MATCH_TIME:
                    state.SetRegister(0, (float)(SimulationTime.Instance?.simulationTime ?? 0.0));
                    break;
                case RoyaleOpCode.GET_PHASE:
                    state.SetRegister(0, _zone?.CurrentPhase ?? 0);
                    break;
                case RoyaleOpCode.SPAWN_AIRDROP:
                {
                    float ax = state.Registers[0];
                    float ay = state.Registers[1];
                    _match.SpawnAirdrop(ax, ay);
                    state.SetRegister(0, 1f);
                    break;
                }
                case RoyaleOpCode.CALL_AIRSTRIKE:
                {
                    float sx = state.Registers[0];
                    float sy = state.Registers[1];
                    _match.CallAirstrike(sx, sy);
                    state.SetRegister(0, 1f);
                    break;
                }
                case RoyaleOpCode.SHIFT_ZONE:
                {
                    float zx = state.Registers[0];
                    float zy = state.Registers[1];
                    if (_zone != null)
                        _zone.NudgeTarget(zx, zy);
                    state.SetRegister(0, 1f);
                    break;
                }
            }
        }

        public float GetTimeScale()
        {
            return SimulationTime.Instance?.timeScale ?? 1f;
        }

        public double GetSimulationTime()
        {
            return SimulationTime.Instance?.simulationTime ?? 0.0;
        }
    }
}
