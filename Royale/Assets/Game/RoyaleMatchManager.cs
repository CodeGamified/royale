// Copyright CodeGamified 2025-2026
// MIT License — Royale
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Time;

namespace Royale.Game
{
    /// <summary>
    /// Match manager — spawning, elimination tracking, zone damage, victory detection.
    /// 1 code-controlled player + 15 AI bots = 16 total.
    /// </summary>
    public class RoyaleMatchManager : MonoBehaviour
    {
        private RoyaleArena _arena;
        private RoyaleZone _zone;
        private readonly List<RoyalePlayer> _players = new List<RoyalePlayer>();
        private readonly List<RoyaleProjectile> _projectiles = new List<RoyaleProjectile>();

        private bool _autoRestart;
        private float _restartDelay;

        // Airstrikes — temporary AOE damage zones from war master
        public struct Airstrike
        {
            public float X, Z, Radius, DPS, TimeRemaining;
        }
        private readonly List<Airstrike> _airstrikes = new List<Airstrike>();
        public IReadOnlyList<Airstrike> Airstrikes => _airstrikes;

        // Airdrop count for status display
        public int AirdropCount { get; private set; }
        public int AirstrikeCount { get; private set; }

        public bool MatchInProgress { get; private set; }
        public int AliveCount { get; private set; }
        public int MatchesPlayed { get; private set; }
        public int PlayerWins { get; private set; }

        public RoyalePlayer CodePlayer { get; private set; }
        public IReadOnlyList<RoyalePlayer> Players => _players;
        public RoyaleZone Zone => _zone;

        // Events
        public System.Action OnMatchStarted;
        public System.Action<RoyalePlayer> OnPlayerEliminated;
        public System.Action<RoyalePlayer> OnVictory;
        public System.Action<RoyaleProjectile> OnProjectileSpawned;
        public System.Action<float, float> OnAirdropSpawned;
        public System.Action<float, float> OnAirstrikeSpawned;

        public void Initialize(RoyaleArena arena, RoyaleZone zone,
                               bool autoRestart, float restartDelay)
        {
            _arena = arena;
            _zone = zone;
            _autoRestart = autoRestart;
            _restartDelay = restartDelay;
        }

        public void RegisterPlayer(RoyalePlayer player)
        {
            _players.Add(player);
            player.OnEliminated += HandleEliminated;
            if (player.IsCodeControlled)
                CodePlayer = player;
        }

        public void StartMatch()
        {
            AliveCount = _players.Count;
            MatchInProgress = true;

            // Zone starts after initial delay
            if (_zone != null)
                _zone.StartZone();

            OnMatchStarted?.Invoke();
        }

        private void Update()
        {
            if (!MatchInProgress) return;
            if (SimulationTime.Instance == null || SimulationTime.Instance.isPaused) return;

            float dt = Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);

            // Zone damage
            if (_zone != null)
            {
                foreach (var p in _players)
                {
                    if (p.IsAlive)
                        _zone.ApplyDamage(p, dt);
                }
            }

            // Collision resolution for all alive players
            foreach (var p in _players)
            {
                if (!p.IsAlive) continue;
                var resolved = _arena.ResolveCollision(p.posX, p.posZ,
                    RoyalePlayer.COLLISION_RADIUS);
                p.posX = resolved.x;
                p.posZ = resolved.y;
                p.SyncTransform();
            }

            // Clear shooting flag
            foreach (var p in _players)
                p.isShooting = false;

            // Airstrike damage
            for (int i = _airstrikes.Count - 1; i >= 0; i--)
            {
                var strike = _airstrikes[i];
                strike.TimeRemaining -= dt;
                _airstrikes[i] = strike;

                if (strike.TimeRemaining <= 0f)
                {
                    _airstrikes.RemoveAt(i);
                    continue;
                }

                // Damage all players within airstrike radius
                float rSq = strike.Radius * strike.Radius;
                foreach (var p in _players)
                {
                    if (!p.IsAlive) continue;
                    float dx = p.posX - strike.X;
                    float dz = p.posZ - strike.Z;
                    if (dx * dx + dz * dz <= rSq)
                        p.TakeDamage(strike.DPS * dt, null);
                }
            }
        }

        private void HandleEliminated(RoyalePlayer player)
        {
            if (!MatchInProgress) return;

            AliveCount--;
            player.placement = AliveCount + 1;

            OnPlayerEliminated?.Invoke(player);
            Debug.Log($"[ROYALE] Eliminated: Player #{player.PlayerIndex} " +
                      $"(placement #{player.placement}, {AliveCount} alive)");

            if (AliveCount <= 1)
            {
                RoyalePlayer winner = null;
                foreach (var p in _players)
                {
                    if (p.IsAlive) { winner = p; break; }
                }
                EndMatch(winner);
            }
        }

        private void EndMatch(RoyalePlayer winner)
        {
            MatchInProgress = false;
            MatchesPlayed++;

            if (winner != null)
            {
                winner.placement = 1;
                if (winner.IsCodeControlled) PlayerWins++;
                OnVictory?.Invoke(winner);
                Debug.Log($"[ROYALE] VICTORY! Player #{winner.PlayerIndex} " +
                          $"({winner.kills} kills)");
            }

            if (_autoRestart)
                StartCoroutine(RestartCoroutine());
        }

        private System.Collections.IEnumerator RestartCoroutine()
        {
            float waited = 0f;
            while (waited < _restartDelay)
            {
                if (SimulationTime.Instance != null && !SimulationTime.Instance.isPaused)
                    waited += Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);
                yield return null;
            }

            // Clean up projectiles
            foreach (var proj in _projectiles)
            {
                if (proj != null)
                    Destroy(proj.gameObject);
            }
            _projectiles.Clear();

            // Reset players
            float half = _arena != null ? _arena.HalfMap : 100f;
            foreach (var p in _players)
            {
                float sx = Random.Range(-half * 0.8f, half * 0.8f);
                float sz = Random.Range(-half * 0.8f, half * 0.8f);
                p.Initialize(p.PlayerIndex, p.IsCodeControlled, sx, sz);
            }

            // Reset zone
            if (_zone != null)
                _zone.Initialize(_arena.MapSize);

            StartMatch();
        }

        // ── Shooting ──

        /// <summary>Fire weapon for a player. Spawns projectile(s).</summary>
        public void FireWeapon(RoyalePlayer player)
        {
            if (!player.TryShoot()) return;

            var stats = player.ActiveStats;
            Vector2 playerPos = new Vector2(player.posX, player.posZ);
            float baseAngle = player.facing;

            for (int i = 0; i < stats.Pellets; i++)
            {
                float spread = Random.Range(-stats.SpreadDeg, stats.SpreadDeg);
                float angle = (baseAngle + spread) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                if (stats.Type == WeaponType.Fists)
                {
                    // Melee — instant hit check within range
                    MeleeHit(player, dir, stats.Range, stats.Damage);
                }
                else
                {
                    var go = new GameObject($"Bullet_{player.PlayerIndex}");
                    var proj = go.AddComponent<RoyaleProjectile>();
                    proj.Initialize(player, _arena,
                                    playerPos + dir * 0.6f, dir,
                                    stats.Damage, stats.Range);
                    _projectiles.Add(proj);
                    proj.OnExpired += p => _projectiles.Remove(p);
                    proj.OnHitPlayer += (p, _) => _projectiles.Remove(p);
                    OnProjectileSpawned?.Invoke(proj);
                }
            }
        }

        private void MeleeHit(RoyalePlayer attacker, Vector2 dir, float range, float damage)
        {
            foreach (var p in _players)
            {
                if (!p.IsAlive || p == attacker) continue;
                float dx = p.posX - attacker.posX;
                float dz = p.posZ - attacker.posZ;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist > range) continue;

                // Check if roughly in front (within ~90 degrees of aim)
                float dot = (dx * dir.x + dz * dir.y) / Mathf.Max(dist, 0.01f);
                if (dot > 0f)
                {
                    p.TakeDamage(damage, attacker);
                    break; // Only hit one target per punch
                }
            }
        }

        // ── Crate spawning ──

        public void SpawnCrates(int count)
        {
            if (_arena == null) return;
            float half = _arena.HalfMap;

            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(-half * 0.9f, half * 0.9f);
                float z = Random.Range(-half * 0.9f, half * 0.9f);

                // Determine rarity
                float roll = Random.value;
                CrateRarity rarity;
                if (roll < 0.03f)       rarity = CrateRarity.Legendary;
                else if (roll < 0.15f)  rarity = CrateRarity.Rare;
                else if (roll < 0.40f)  rarity = CrateRarity.Uncommon;
                else                    rarity = CrateRarity.Common;

                var go = new GameObject($"Crate_{i}");
                go.transform.SetParent(_arena.transform, false);
                var crate = go.AddComponent<RoyaleCrate>();
                crate.Initialize(x, z, rarity);
                _arena.Crates.Add(crate);
            }
        }

        // ── War Master fate events ──

        /// <summary>Spawn a rare+ airdrop crate at the given map position.</summary>
        public void SpawnAirdrop(float x, float z)
        {
            if (_arena == null) return;
            float half = _arena.HalfMap;
            x = Mathf.Clamp(x, -half, half);
            z = Mathf.Clamp(z, -half, half);

            // 50% Rare, 50% Legendary
            CrateRarity rarity = Random.value < 0.5f ? CrateRarity.Rare : CrateRarity.Legendary;

            var go = new GameObject($"Airdrop_{AirdropCount}");
            go.transform.SetParent(_arena.transform, false);
            var crate = go.AddComponent<RoyaleCrate>();
            crate.Initialize(x, z, rarity);
            _arena.Crates.Add(crate);
            AirdropCount++;

            OnAirdropSpawned?.Invoke(x, z);
            Debug.Log($"[ROYALE] AIRDROP at ({x:F0}, {z:F0}) — {rarity}");
        }

        /// <summary>Create a temporary AOE damage zone (15u radius, 8 DPS, 20s duration).</summary>
        public void CallAirstrike(float x, float z)
        {
            if (_arena == null) return;
            float half = _arena.HalfMap;
            x = Mathf.Clamp(x, -half, half);
            z = Mathf.Clamp(z, -half, half);

            _airstrikes.Add(new Airstrike
            {
                X = x, Z = z, Radius = 15f, DPS = 8f, TimeRemaining = 20f
            });
            AirstrikeCount++;

            OnAirstrikeSpawned?.Invoke(x, z);
            Debug.Log($"[ROYALE] AIRSTRIKE at ({x:F0}, {z:F0}) — 15u radius, 20s");
        }

        // ── Query helpers for IOHandler ──

        /// <summary>Find nearest visible enemy for a player (LOS + view range gated).</summary>
        public RoyalePlayer FindNearestVisibleEnemy(RoyalePlayer from)
        {
            RoyalePlayer nearest = null;
            float bestDist = float.MaxValue;
            float viewRange = from.ViewRadius;

            foreach (var p in _players)
            {
                if (!p.IsAlive || p == from) continue;
                float dist = from.DistanceTo(p);
                if (dist > viewRange) continue;
                if (!_arena.HasLineOfSight(from.posX, from.posZ, p.posX, p.posZ))
                    continue;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = p;
                }
            }
            return nearest;
        }

        /// <summary>Find nearest unopened crate.</summary>
        public RoyaleCrate FindNearestCrate(RoyalePlayer from)
        {
            RoyaleCrate nearest = null;
            float bestDist = float.MaxValue;

            foreach (var c in _arena.Crates)
            {
                if (c.IsOpened) continue;
                float dx = c.PosX - from.posX;
                float dz = c.PosZ - from.posZ;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = c;
                }
            }
            return nearest;
        }

        /// <summary>Find nearest cover (building or rock).</summary>
        public Vector3 FindNearestCover(RoyalePlayer from)
        {
            float bestDist = float.MaxValue;
            Vector3 best = Vector3.zero;

            foreach (var bld in _arena.Buildings)
            {
                float cx = (bld.MinX + bld.MaxX) / 2f;
                float cz = (bld.MinZ + bld.MaxZ) / 2f;
                float dx = cx - from.posX;
                float dz = cz - from.posZ;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = new Vector3(cx, cz, dist);
                }
            }

            foreach (var rock in _arena.Rocks)
            {
                float dx = rock.x - from.posX;
                float dz = rock.y - from.posZ;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = new Vector3(rock.x, rock.y, dist);
                }
            }

            return bestDist < float.MaxValue ? best : new Vector3(0, 0, -1);
        }

        /// <summary>Try to loot nearest crate for a player.</summary>
        public bool TryLootNearest(RoyalePlayer player)
        {
            var crate = FindNearestCrate(player);
            if (crate == null) return false;
            return crate.TryLoot(player);
        }

        private void OnDestroy()
        {
            foreach (var p in _players)
            {
                if (p != null)
                    p.OnEliminated -= HandleEliminated;
            }
        }
    }
}
