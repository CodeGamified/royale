// Copyright CodeGamified 2025-2026
// MIT License — Royale
using UnityEngine;
using CodeGamified.Time;

namespace Royale.Game
{
    /// <summary>
    /// Shrinking safe zone — circular, with phased delays and damage.
    /// Players outside the zone take escalating DPS.
    /// </summary>
    public class RoyaleZone : MonoBehaviour
    {
        // Current zone state
        public float CenterX { get; private set; }
        public float CenterZ { get; private set; }
        public float CurrentRadius { get; private set; }
        public bool IsShrinking { get; private set; }
        public int CurrentPhase { get; private set; }

        // Next zone target
        public float TargetCenterX { get; private set; }
        public float TargetCenterZ { get; private set; }
        public float TargetRadius { get; private set; }

        private float _phaseTimer;
        private float _shrinkTimer;
        private float _mapSize;

        public System.Action<int> OnPhaseStarted;

        // Zone phases: delay, shrinkTime, endRadius, dps
        private static readonly float[,] Phases = new float[,]
        {
            { 30f, 20f, 80f,  1f },
            { 25f, 18f, 50f,  2f },
            { 20f, 15f, 25f,  4f },
            { 15f, 12f,  5f,  8f },
            { 10f, 10f,  0f, 16f },
        };

        public float CurrentDPS
        {
            get
            {
                if (CurrentPhase < 0 || CurrentPhase >= Phases.GetLength(0))
                    return 0f;
                return Phases[CurrentPhase, 3];
            }
        }

        public void Initialize(float mapSize)
        {
            _mapSize = mapSize;
            CenterX = 0f;
            CenterZ = 0f;
            CurrentRadius = mapSize * 0.707f; // cover full map (diagonal/2)
            IsShrinking = false;
            CurrentPhase = -1; // hasn't started

            TargetCenterX = 0f;
            TargetCenterZ = 0f;
            TargetRadius = CurrentRadius;
        }

        public void StartZone()
        {
            CurrentPhase = 0;
            _phaseTimer = Phases[0, 0]; // delay before first shrink
            IsShrinking = false;
            PickNextZoneTarget();
            OnPhaseStarted?.Invoke(0);
        }

        private void Update()
        {
            if (CurrentPhase < 0 || CurrentPhase >= Phases.GetLength(0)) return;
            if (SimulationTime.Instance == null || SimulationTime.Instance.isPaused) return;

            float dt = Time.deltaTime * (SimulationTime.Instance?.timeScale ?? 1f);

            if (!IsShrinking)
            {
                // Waiting phase
                _phaseTimer -= dt;
                if (_phaseTimer <= 0f)
                {
                    IsShrinking = true;
                    _shrinkTimer = Phases[CurrentPhase, 1];
                }
            }
            else
            {
                // Shrinking
                _shrinkTimer -= dt;
                float t = 1f - Mathf.Max(0f, _shrinkTimer) / Phases[CurrentPhase, 1];
                float startRadius = CurrentPhase > 0
                    ? Phases[CurrentPhase - 1, 2]
                    : _mapSize * 0.707f;
                float endRadius = Phases[CurrentPhase, 2];

                CurrentRadius = Mathf.Lerp(startRadius, endRadius, t);
                CenterX = Mathf.Lerp(CenterX, TargetCenterX, t * 0.1f);
                CenterZ = Mathf.Lerp(CenterZ, TargetCenterZ, t * 0.1f);

                if (_shrinkTimer <= 0f)
                {
                    CurrentRadius = endRadius;
                    IsShrinking = false;
                    CurrentPhase++;

                    if (CurrentPhase < Phases.GetLength(0))
                    {
                        _phaseTimer = Phases[CurrentPhase, 0];
                        PickNextZoneTarget();
                        OnPhaseStarted?.Invoke(CurrentPhase);
                    }
                }
            }
        }

        private void PickNextZoneTarget()
        {
            if (CurrentPhase >= Phases.GetLength(0)) return;
            float endRadius = Phases[CurrentPhase, 2];
            float maxOffset = Mathf.Max(0f, CurrentRadius - endRadius) * 0.3f;
            TargetCenterX = CenterX + Random.Range(-maxOffset, maxOffset);
            TargetCenterZ = CenterZ + Random.Range(-maxOffset, maxOffset);
            TargetRadius = endRadius;
        }

        /// <summary>Is a position inside the current safe zone?</summary>
        public bool IsInside(float x, float z)
        {
            float dx = x - CenterX;
            float dz = z - CenterZ;
            return (dx * dx + dz * dz) <= CurrentRadius * CurrentRadius;
        }

        /// <summary>Nudge the next zone target toward the given position (war master command).</summary>
        public void NudgeTarget(float x, float z)
        {
            float maxOff = CurrentRadius * 0.3f;
            TargetCenterX = Mathf.Clamp(x, CenterX - maxOff, CenterX + maxOff);
            TargetCenterZ = Mathf.Clamp(z, CenterZ - maxOff, CenterZ + maxOff);
        }

        /// <summary>Apply zone damage to a player if outside.</summary>
        public void ApplyDamage(RoyalePlayer player, float dt)
        {
            if (!player.IsAlive) return;
            if (CurrentPhase < 0) return;
            if (IsInside(player.posX, player.posZ)) return;

            float dps = CurrentDPS;
            if (dps > 0f)
                player.TakeDamage(dps * dt, null);
        }
    }
}
