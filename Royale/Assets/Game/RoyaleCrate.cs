// Copyright CodeGamified 2025-2026
// MIT License — Royale
using UnityEngine;

namespace Royale.Game
{
    /// <summary>
    /// A loot crate on the map. Walking within 2 units and calling loot() opens it.
    /// Contains weapon, ammo, heal, armor, and/or scope based on rarity.
    /// </summary>
    public class RoyaleCrate : MonoBehaviour
    {
        public bool IsOpened { get; private set; }
        public CrateRarity Rarity { get; private set; }
        public float PosX { get; private set; }
        public float PosZ { get; private set; }

        // Loot contents
        public WeaponType LootWeapon { get; private set; }
        public int LootAmmo { get; private set; }
        public bool HasHeal { get; private set; }
        public bool HealIsMedkit { get; private set; }
        public float ArmorAmount { get; private set; }
        public ScopeLevel LootScope { get; private set; }

        // Visual
        private GameObject _visual;

        public System.Action<RoyaleCrate, RoyalePlayer> OnLooted;

        public void Initialize(float x, float z, CrateRarity rarity)
        {
            PosX = x;
            PosZ = z;
            Rarity = rarity;
            IsOpened = false;
            transform.position = new Vector3(x, 0.3f, z);

            GenerateLoot(rarity);
            BuildVisual();
        }

        private void GenerateLoot(CrateRarity rarity)
        {
            switch (rarity)
            {
                case CrateRarity.Common:
                    LootWeapon = WeaponType.Pistol;
                    LootAmmo = 30;
                    HasHeal = true;
                    HealIsMedkit = false;
                    ArmorAmount = 0f;
                    LootScope = Random.value < 0.3f ? ScopeLevel.X2 : ScopeLevel.None;
                    break;

                case CrateRarity.Uncommon:
                    LootWeapon = Random.value < 0.5f ? WeaponType.Shotgun : WeaponType.SMG;
                    LootAmmo = 20;
                    HasHeal = true;
                    HealIsMedkit = false;
                    ArmorAmount = 25f;
                    LootScope = ScopeLevel.X4;
                    break;

                case CrateRarity.Rare:
                    LootWeapon = WeaponType.AR;
                    LootAmmo = 60;
                    HasHeal = true;
                    HealIsMedkit = true;
                    ArmorAmount = 50f;
                    LootScope = ScopeLevel.X8;
                    break;

                case CrateRarity.Legendary:
                    LootWeapon = WeaponType.Sniper;
                    LootAmmo = 10;
                    HasHeal = true;
                    HealIsMedkit = true;
                    ArmorAmount = 50f;
                    LootScope = ScopeLevel.X15;
                    break;
            }
        }

        /// <summary>Try to loot this crate. Returns true if successfully looted.</summary>
        public bool TryLoot(RoyalePlayer player)
        {
            if (IsOpened || !player.IsAlive) return false;

            float dx = player.posX - PosX;
            float dz = player.posZ - PosZ;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist > 2f) return false;

            IsOpened = true;

            // Give loot to player
            if (LootWeapon != WeaponType.Fists)
                player.TryPickupWeapon(LootWeapon, LootAmmo);
            if (HasHeal)
                player.PickupHeal(HealIsMedkit);
            if (ArmorAmount > 0f)
                player.PickupArmor(ArmorAmount);
            if (LootScope != ScopeLevel.None)
                player.PickupScope(LootScope);

            OnLooted?.Invoke(this, player);

            // Hide visual
            if (_visual != null)
                _visual.SetActive(false);

            return true;
        }

        private void BuildVisual()
        {
            _visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _visual.transform.SetParent(transform, false);
            _visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            var col = _visual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = _visual.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Sprites/Default"));
                mat.color = GetCrateColor();
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", GetCrateColor() * 1.5f);
                rend.material = mat;
            }
        }

        private Color GetCrateColor()
        {
            switch (Rarity)
            {
                case CrateRarity.Uncommon:  return new Color(0.3f, 0.7f, 1.0f);
                case CrateRarity.Rare:      return new Color(0.8f, 0.3f, 1.0f);
                case CrateRarity.Legendary: return new Color(1.0f, 0.85f, 0.2f);
                default:                    return new Color(1.0f, 0.85f, 0.2f);
            }
        }
    }
}
