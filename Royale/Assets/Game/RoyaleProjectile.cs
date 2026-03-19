// Copyright CodeGamified 2025-2026
// MIT License — Royale
using UnityEngine;
using CodeGamified.Time;

namespace Royale.Game
{
    /// <summary>
    /// Bullet projectile — fast travel along XZ plane with hit detection.
    /// Blocked by buildings (AABB) and rocks. Trees give 50% block chance.
    /// </summary>
    public class RoyaleProjectile : MonoBehaviour
    {
        private RoyalePlayer _owner;
        private RoyaleArena _arena;
        private float _damage;
        private float _range;
        private float _speed;

        public Vector2 Position { get; private set; } // XZ
        public Vector2 Velocity { get; private set; }
        public bool IsActive { get; private set; }

        private float _distanceTraveled;
        private const float BULLET_SPEED = 120f;

        public System.Action<RoyaleProjectile, RoyalePlayer> OnHitPlayer;
        public System.Action<RoyaleProjectile> OnExpired;

        public void Initialize(RoyalePlayer owner, RoyaleArena arena,
                               Vector2 startPos, Vector2 direction,
                               float damage, float range)
        {
            _owner = owner;
            _arena = arena;
            _damage = damage;
            _range = range;
            _speed = BULLET_SPEED;

            Position = startPos;
            Velocity = direction.normalized * _speed;
            IsActive = true;
            _distanceTraveled = 0f;

            transform.position = new Vector3(startPos.x, 0.5f, startPos.y);

            // Small elongated visual
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(transform, false);
            quad.transform.localScale = new Vector3(0.08f, 0.3f, 1f);

            var col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = quad.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Sprites/Default"));
                mat.color = new Color(1f, 0.95f, 0.4f);
                rend.material = mat;
            }
        }

        private void Update()
        {
            if (!IsActive) return;
            if (SimulationTime.Instance == null || SimulationTime.Instance.isPaused) return;

            float dt = Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);
            float distance = _speed * dt;

            // Sub-step for accuracy
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / 0.5f));
            float subDist = distance / steps;

            for (int i = 0; i < steps && IsActive; i++)
            {
                Vector2 step = Velocity.normalized * subDist;
                Position += step;
                _distanceTraveled += subDist;

                // Range limit
                if (_distanceTraveled >= _range)
                {
                    Expire();
                    return;
                }

                // Arena bounds
                float halfMap = _arena != null ? _arena.MapSize / 2f : 100f;
                if (Mathf.Abs(Position.x) > halfMap || Mathf.Abs(Position.y) > halfMap)
                {
                    Expire();
                    return;
                }

                // Building collision (AABB)
                if (_arena != null)
                {
                    foreach (var bld in _arena.Buildings)
                    {
                        if (bld.BlocksPoint(Position.x, Position.y))
                        {
                            Expire();
                            return;
                        }
                    }

                    // Rock collision
                    foreach (var rock in _arena.Rocks)
                    {
                        float dx = Position.x - rock.x;
                        float dz = Position.y - rock.y;
                        if (dx * dx + dz * dz < rock.z * rock.z)
                        {
                            Expire();
                            return;
                        }
                    }

                    // Tree collision (50% block chance)
                    foreach (var tree in _arena.Trees)
                    {
                        float dx = Position.x - tree.x;
                        float dz = Position.y - tree.y;
                        if (dx * dx + dz * dz < 0.6f * 0.6f)
                        {
                            if (Random.value < 0.5f)
                            {
                                Expire();
                                return;
                            }
                        }
                    }
                }

                // Player hit detection
                var players = FindObjectsByType<RoyalePlayer>(FindObjectsSortMode.None);
                foreach (var p in players)
                {
                    if (!p.IsAlive || p == _owner) continue;
                    float pdx = Position.x - p.posX;
                    float pdz = Position.y - p.posZ;
                    float hitRadius = RoyalePlayer.COLLISION_RADIUS + 0.1f;
                    if (pdx * pdx + pdz * pdz <= hitRadius * hitRadius)
                    {
                        p.TakeDamage(_damage, _owner);
                        OnHitPlayer?.Invoke(this, p);
                        Expire();
                        return;
                    }
                }
            }

            if (IsActive)
                transform.position = new Vector3(Position.x, 0.5f, Position.y);
        }

        private void Expire()
        {
            IsActive = false;
            OnExpired?.Invoke(this);
            Destroy(gameObject, 0.05f);
        }
    }
}
