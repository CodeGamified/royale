// Copyright CodeGamified 2025-2026
// MIT License — Royale
using UnityEngine;

namespace Royale.Game
{
    /// <summary>
    /// Static weapon stats table. All weapon data in one place.
    /// </summary>
    public static class RoyaleWeapon
    {
        public struct WeaponStats
        {
            public WeaponType Type;
            public float Damage;
            public float FireRate;       // shots per second
            public float Range;
            public int MagSize;
            public float ReloadTime;
            public float SpreadDeg;
            public int Pellets;          // >1 for shotgun
            public string Name;
        }

        public static readonly WeaponStats[] Table = new WeaponStats[]
        {
            // Fists
            new WeaponStats
            {
                Type = WeaponType.Fists, Damage = 8f, FireRate = 2f,
                Range = 1.5f, MagSize = int.MaxValue, ReloadTime = 0f,
                SpreadDeg = 0f, Pellets = 1, Name = "Fists"
            },
            // Pistol
            new WeaponStats
            {
                Type = WeaponType.Pistol, Damage = 12f, FireRate = 6f,
                Range = 60f, MagSize = 15, ReloadTime = 1.5f,
                SpreadDeg = 4f, Pellets = 1, Name = "Pistol"
            },
            // Shotgun
            new WeaponStats
            {
                Type = WeaponType.Shotgun, Damage = 9f, FireRate = 1.5f,
                Range = 20f, MagSize = 2, ReloadTime = 2.5f,
                SpreadDeg = 12f, Pellets = 8, Name = "Shotgun"
            },
            // SMG
            new WeaponStats
            {
                Type = WeaponType.SMG, Damage = 9f, FireRate = 14f,
                Range = 40f, MagSize = 32, ReloadTime = 1.8f,
                SpreadDeg = 7f, Pellets = 1, Name = "SMG"
            },
            // AR
            new WeaponStats
            {
                Type = WeaponType.AR, Damage = 14f, FireRate = 10f,
                Range = 100f, MagSize = 30, ReloadTime = 2.2f,
                SpreadDeg = 3f, Pellets = 1, Name = "AR"
            },
            // Sniper
            new WeaponStats
            {
                Type = WeaponType.Sniper, Damage = 68f, FireRate = 0.9f,
                Range = 200f, MagSize = 5, ReloadTime = 3.0f,
                SpreadDeg = 0.5f, Pellets = 1, Name = "Sniper"
            },
        };

        public static WeaponStats Get(WeaponType type) => Table[(int)type];

        /// <summary>View radius for each scope level.</summary>
        public static float GetViewRadius(ScopeLevel scope)
        {
            switch (scope)
            {
                case ScopeLevel.X2:  return 40f;
                case ScopeLevel.X4:  return 55f;
                case ScopeLevel.X8:  return 75f;
                case ScopeLevel.X15: return 100f;
                default:             return 30f;
            }
        }

        /// <summary>Orthographic camera size for each scope level.</summary>
        public static float GetCameraSize(ScopeLevel scope)
        {
            switch (scope)
            {
                case ScopeLevel.X2:  return 22.5f;
                case ScopeLevel.X4:  return 16f;
                case ScopeLevel.X8:  return 11f;
                case ScopeLevel.X15: return 7.5f;
                default:             return 30f;
            }
        }
    }
}
