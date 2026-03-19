// Copyright CodeGamified 2025-2026
// MIT License — Royale
using UnityEngine;
using CodeGamified.Time;

namespace Royale.Game
{
    /// <summary>
    /// A player or bot on the battlefield.
    /// Top-down 2D on XZ plane (Y is up/camera axis).
    /// All state exposed to scripts via IOHandler.
    /// </summary>
    public class RoyalePlayer : MonoBehaviour
    {
        // Identity
        public int PlayerIndex { get; private set; }
        public bool IsCodeControlled { get; private set; }
        public bool IsAlive { get; private set; } = true;

        // Position (XZ plane)
        public float posX;
        public float posZ;
        public float facing;  // degrees, 0=+X, CCW positive

        // Health
        public float health = 100f;
        public float armor;

        // Inventory: 2 weapon slots
        public WeaponType[] weaponSlots = new WeaponType[2];
        public int[] ammo = new int[2];           // magazine
        public int[] ammoReserve = new int[2];    // spare ammo
        public int activeSlot;

        // Scope
        public ScopeLevel scope = ScopeLevel.None;

        // Healing
        public int healCount;       // 0 or 1
        public bool healIsMedkit;   // false=bandage(30HP), true=medkit(full)

        // Weapon state
        public float reloadTimer;   // >0 means reloading
        public float fireCooldown;  // >0 means can't fire yet

        // Movement state
        public bool isShooting;
        public bool isHealing;
        public float healTimer;

        // Movement command (set by script each tick)
        public float moveAngle;
        public bool moveRequested;

        // Scoring
        public int kills;
        public float damageDealt;
        public int placement;

        // Invulnerability (grace period at match start)
        public float invulnTimer;

        // Config
        public const float BASE_SPEED = 5f;
        public const float SHOOT_SPEED = 3.5f;
        public const float COLLISION_RADIUS = 0.5f;
        public const float HEAL_TIME = 3f;

        // Events
        public System.Action<RoyalePlayer> OnEliminated;
        public System.Action<RoyalePlayer> OnDamaged;

        public Vector2 Position => new Vector2(posX, posZ);

        public WeaponType ActiveWeapon => weaponSlots[activeSlot];
        public RoyaleWeapon.WeaponStats ActiveStats => RoyaleWeapon.Get(ActiveWeapon);
        public bool IsReloading => reloadTimer > 0f;
        public float ViewRadius => RoyaleWeapon.GetViewRadius(scope);

        public void Initialize(int index, bool codeControlled, float startX, float startZ)
        {
            PlayerIndex = index;
            IsCodeControlled = codeControlled;
            IsAlive = true;
            posX = startX;
            posZ = startZ;
            facing = Random.Range(0f, 360f);
            health = 100f;
            armor = 0f;
            weaponSlots[0] = WeaponType.Fists;
            weaponSlots[1] = WeaponType.Fists;
            ammo[0] = int.MaxValue;
            ammo[1] = int.MaxValue;
            ammoReserve[0] = 0;
            ammoReserve[1] = 0;
            activeSlot = 0;
            scope = ScopeLevel.None;
            healCount = 0;
            healIsMedkit = false;
            reloadTimer = 0f;
            fireCooldown = 0f;
            isShooting = false;
            isHealing = false;
            healTimer = 0f;
            moveRequested = false;
            kills = 0;
            damageDealt = 0f;
            placement = 0;
            invulnTimer = 3f; // 3s grace

            SyncTransform();
        }

        private void Update()
        {
            if (!IsAlive) return;
            if (SimulationTime.Instance == null || SimulationTime.Instance.isPaused) return;

            float dt = Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);

            // Grace period
            if (invulnTimer > 0f)
                invulnTimer = Mathf.Max(0f, invulnTimer - dt);

            // Reload
            if (reloadTimer > 0f)
            {
                reloadTimer -= dt;
                if (reloadTimer <= 0f)
                    FinishReload();
            }

            // Fire cooldown
            if (fireCooldown > 0f)
                fireCooldown = Mathf.Max(0f, fireCooldown - dt);

            // Healing
            if (isHealing)
            {
                healTimer -= dt;
                if (healTimer <= 0f)
                    FinishHeal();
            }

            // Movement
            if (moveRequested && !isHealing)
            {
                float speed = isShooting ? SHOOT_SPEED : BASE_SPEED;
                float rad = moveAngle * Mathf.Deg2Rad;
                posX += Mathf.Cos(rad) * speed * dt;
                posZ += Mathf.Sin(rad) * speed * dt;
                moveRequested = false;
            }

            SyncTransform();
        }

        public void SyncTransform()
        {
            transform.position = new Vector3(posX, 0.25f, posZ);
        }

        // ── Weapon Actions ──

        public bool TryShoot()
        {
            if (!IsAlive || isHealing) return false;
            if (reloadTimer > 0f) return false;
            if (fireCooldown > 0f) return false;

            var stats = ActiveStats;
            if (ammo[activeSlot] <= 0) return false;

            if (stats.MagSize != int.MaxValue)
                ammo[activeSlot]--;

            fireCooldown = 1f / stats.FireRate;
            isShooting = true;
            return true;
        }

        public bool TryReload()
        {
            if (!IsAlive || reloadTimer > 0f) return false;
            var stats = ActiveStats;
            if (stats.Type == WeaponType.Fists) return false;
            if (ammo[activeSlot] >= stats.MagSize) return false;
            if (ammoReserve[activeSlot] <= 0) return false;

            reloadTimer = stats.ReloadTime;
            return true;
        }

        private void FinishReload()
        {
            reloadTimer = 0f;
            var stats = ActiveStats;
            int needed = stats.MagSize - ammo[activeSlot];
            int available = Mathf.Min(needed, ammoReserve[activeSlot]);
            ammo[activeSlot] += available;
            ammoReserve[activeSlot] -= available;
        }

        public bool TrySwapWeapon()
        {
            if (!IsAlive) return false;
            activeSlot = 1 - activeSlot;
            reloadTimer = 0f; // cancel reload on swap
            return true;
        }

        public bool TryUseHeal()
        {
            if (!IsAlive || isHealing) return false;
            if (healCount <= 0) return false;
            if (health >= 100f) return false;

            isHealing = true;
            healTimer = HEAL_TIME;
            return true;
        }

        private void FinishHeal()
        {
            isHealing = false;
            healTimer = 0f;
            if (healCount <= 0) return;
            healCount--;
            if (healIsMedkit)
                health = 100f;
            else
                health = Mathf.Min(100f, health + 30f);
        }

        // ── Damage ──

        public void TakeDamage(float damage, RoyalePlayer attacker)
        {
            if (!IsAlive) return;
            if (invulnTimer > 0f) return;

            float absorbed = 0f;
            if (armor > 0f)
            {
                absorbed = damage * 0.5f;
                if (absorbed > armor)
                    absorbed = armor;
                armor -= absorbed;
            }

            health -= (damage - absorbed);
            OnDamaged?.Invoke(this);

            if (attacker != null)
                attacker.damageDealt += damage;

            if (health <= 0f)
            {
                health = 0f;
                IsAlive = false;
                if (attacker != null)
                    attacker.kills++;
                OnEliminated?.Invoke(this);
            }
        }

        // ── Loot Pickup ──

        public bool TryPickupWeapon(WeaponType type, int ammoAmount)
        {
            if (!IsAlive) return false;

            // If we have fists in any slot, replace that
            for (int i = 0; i < 2; i++)
            {
                if (weaponSlots[i] == WeaponType.Fists)
                {
                    weaponSlots[i] = type;
                    var stats = RoyaleWeapon.Get(type);
                    ammo[i] = Mathf.Min(ammoAmount, stats.MagSize);
                    ammoReserve[i] = ammoAmount - ammo[i];
                    return true;
                }
            }

            // Both slots full — replace active slot if new weapon is better
            if ((int)type > (int)weaponSlots[activeSlot])
            {
                weaponSlots[activeSlot] = type;
                var stats = RoyaleWeapon.Get(type);
                ammo[activeSlot] = Mathf.Min(ammoAmount, stats.MagSize);
                ammoReserve[activeSlot] = ammoAmount - ammo[activeSlot];
                return true;
            }

            // Add ammo to matching weapon
            for (int i = 0; i < 2; i++)
            {
                if (weaponSlots[i] == type)
                {
                    ammoReserve[i] += ammoAmount;
                    return true;
                }
            }

            return false;
        }

        public void PickupArmor(float amount)
        {
            armor = Mathf.Min(50f, armor + amount);
        }

        public void PickupHeal(bool isMedkit)
        {
            healCount = 1;
            healIsMedkit = isMedkit;
        }

        public void PickupScope(ScopeLevel level)
        {
            if ((int)level > (int)scope)
                scope = level;
        }

        // ── Helpers ──

        public void ClearMoveCommand()
        {
            moveRequested = false;
            isShooting = false;
        }

        public float DistanceTo(RoyalePlayer other)
        {
            float dx = other.posX - posX;
            float dz = other.posZ - posZ;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public float AngleTo(float targetX, float targetZ)
        {
            float dx = targetX - posX;
            float dz = targetZ - posZ;
            return Mathf.Atan2(dz, dx) * Mathf.Rad2Deg;
        }
    }
}
