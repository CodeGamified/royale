// Copyright CodeGamified 2025-2026
// MIT License — Royale
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CodeGamified.Camera;
using CodeGamified.Time;
using CodeGamified.Settings;
using CodeGamified.Quality;
using CodeGamified.Bootstrap;
using Royale.Game;
using Royale.Scripting;
using Royale.AI;
using Royale.UI;

namespace Royale.Core
{
    /// <summary>
    /// Bootstrap for Royale — top-down battle royale.
    /// 1 code-controlled player + 15 AI bots on a 200×200 arena with shrinking zone.
    /// </summary>
    public class RoyaleBootstrap : GameBootstrap
    {
        protected override string LogTag => "ROYALE";

        // =================================================================
        // INSPECTOR
        // =================================================================

        [Header("Arena")]
        public float mapSize = 200f;
        public int crateCount = 80;

        [Header("Players")]
        public int botCount = 15;

        [Header("Match")]
        public bool autoRestart = true;
        public float restartDelay = 5f;

        [Header("Scripting")]
        public bool enableScripting = true;

        [Header("AI")]
        public bool enableAI = true;

        [Header("TUI")]
        public bool enableTUI = true;

        [Header("War Master")]
        public WarMasterDifficulty warMasterDifficulty = WarMasterDifficulty.Tactical;

        [Header("Camera")]
        public bool configureCamera = true;

        // =================================================================
        // RUNTIME REFERENCES
        // =================================================================

        private RoyaleArena _arena;
        private RoyaleZone _zone;
        private RoyaleMatchManager _match;
        private RoyaleRenderer _renderer;
        private RoyalePlayer _codePlayer;
        private RoyaleProgram _playerProgram;
        private RoyaleInputProvider _inputProvider;
        private readonly List<RoyaleAIController> _aiControllers = new List<RoyaleAIController>();
        private readonly List<RoyalePlayer> _allPlayers = new List<RoyalePlayer>();

        // War Master & TUI
        private RoyaleWarMaster _warMaster;
        private RoyaleTUIManager _tuiManager;

        // Camera
        private CameraAmbientMotion _cameraSway;
        private float _targetCameraSize;
        private float _currentCameraSize;

        // =================================================================
        // UPDATE
        // =================================================================

        private void Update()
        {
            UpdateCameraFollow();
            UpdateCameraZoom();
        }

        private void UpdateCameraFollow()
        {
            if (_codePlayer == null || !_codePlayer.IsAlive) return;

            var cam = UnityEngine.Camera.main;
            if (cam == null) return;

            Vector3 target = new Vector3(_codePlayer.posX, 50f, _codePlayer.posZ);
            cam.transform.position = Vector3.Lerp(
                cam.transform.position, target,
                Time.unscaledDeltaTime * 8f);
        }

        private void UpdateCameraZoom()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null || !cam.orthographic) return;

            // Target size based on player scope
            float scopeSize = _codePlayer != null
                ? RoyaleWeapon.GetCameraSize(_codePlayer.scope)
                : 30f;

            // Late-game tighter zoom
            float aliveZoom = scopeSize;
            if (_match != null && _match.AliveCount < 8)
            {
                float t = 1f - (_match.AliveCount - 1f) / 7f;
                aliveZoom = Mathf.Lerp(scopeSize, 15f, t * 0.3f);
            }

            _targetCameraSize = Mathf.Min(scopeSize, aliveZoom);
            _currentCameraSize = Mathf.Lerp(_currentCameraSize, _targetCameraSize,
                Time.unscaledDeltaTime * 2f);
            cam.orthographicSize = _currentCameraSize;
        }

        // =================================================================
        // BOOTSTRAP
        // =================================================================

        private void Start()
        {
            Log("ROYALE Bootstrap starting...");

            SettingsBridge.Load();
            QualityBridge.SetTier((QualityTier)SettingsBridge.QualityLevel);

            SetupSimulationTime();
            SetupCamera();
            CreateArena();
            CreateZone();
            SpawnCrates();
            CreatePlayers();
            CreateMatchManager();
            CreateRenderer();
            CreateInputProvider();

            if (enableScripting) CreatePlayerProgram();
            if (enableAI) CreateAIControllers();
            CreateWarMaster();
            if (enableTUI) CreateTUI();

            WireEvents();
            StartCoroutine(RunBootSequence());
        }

        // =================================================================
        // SIMULATION TIME
        // =================================================================

        private void SetupSimulationTime()
        {
            EnsureSimulationTime<RoyaleSimulationTime>();
        }

        // =================================================================
        // CAMERA — orthographic top-down following player
        // =================================================================

        private void SetupCamera()
        {
            if (!configureCamera) return;

            var cam = EnsureCamera();
            cam.orthographic = true;
            cam.orthographicSize = 30f;
            _currentCameraSize = 30f;
            _targetCameraSize = 30f;

            cam.transform.position = new Vector3(0f, 50f, 0f);
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // look straight down
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.05f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            _cameraSway = cam.gameObject.AddComponent<CameraAmbientMotion>();
            _cameraSway.lookAtTarget = Vector3.zero;

            // Post-processing: bloom for neon glow
            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null)
                camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var volumeGO = new GameObject("PostProcessVolume");
            var volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>();
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.8f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 1.0f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.5f;
            volume.profile = profile;

            Log("Camera: ortho top-down, follow player, scope-based zoom");
        }

        // =================================================================
        // ARENA
        // =================================================================

        private void CreateArena()
        {
            var go = new GameObject("Arena");
            _arena = go.AddComponent<RoyaleArena>();
            _arena.Initialize(mapSize);
            Log($"Created Arena ({mapSize}×{mapSize}) with buildings, rocks, trees");
        }

        // =================================================================
        // ZONE
        // =================================================================

        private void CreateZone()
        {
            var go = new GameObject("Zone");
            _zone = go.AddComponent<RoyaleZone>();
            _zone.Initialize(mapSize);
            Log("Created RoyaleZone (5 phases, shrinking circle)");
        }

        // =================================================================
        // CRATES
        // =================================================================

        private void SpawnCrates()
        {
            // Will be populated by match manager after players are created
            Log($"Will spawn {crateCount} loot crates at match start");
        }

        // =================================================================
        // PLAYERS
        // =================================================================

        private void CreatePlayers()
        {
            float half = mapSize / 2f;
            int total = 1 + botCount; // 1 code-controlled + N bots

            for (int i = 0; i < total; i++)
            {
                float sx = Random.Range(-half * 0.8f, half * 0.8f);
                float sz = Random.Range(-half * 0.8f, half * 0.8f);

                var go = new GameObject(i == 0 ? "Player (CODE)" : $"Bot_{i}");
                var player = go.AddComponent<RoyalePlayer>();
                player.Initialize(i, i == 0, sx, sz);

                _allPlayers.Add(player);
                if (i == 0) _codePlayer = player;
            }

            Log($"Created {total} players (1 code + {botCount} bots)");
        }

        // =================================================================
        // MATCH MANAGER
        // =================================================================

        private void CreateMatchManager()
        {
            var go = new GameObject("MatchManager");
            _match = go.AddComponent<RoyaleMatchManager>();
            _match.Initialize(_arena, _zone, autoRestart, restartDelay);

            foreach (var player in _allPlayers)
                _match.RegisterPlayer(player);

            // Spawn crates
            _match.SpawnCrates(crateCount);

            Log("Created RoyaleMatchManager (last alive wins)");
        }

        // =================================================================
        // RENDERER
        // =================================================================

        private void CreateRenderer()
        {
            var go = new GameObject("Renderer");
            _renderer = go.AddComponent<RoyaleRenderer>();
            _renderer.Initialize(_arena, _zone, _match);
            Log("Created RoyaleRenderer (top-down visuals)");
        }

        // =================================================================
        // INPUT
        // =================================================================

        private void CreateInputProvider()
        {
            var go = new GameObject("InputProvider");
            _inputProvider = go.AddComponent<RoyaleInputProvider>();
            Log("Created RoyaleInputProvider (WASD + mouse)");
        }

        // =================================================================
        // PLAYER SCRIPTING
        // =================================================================

        private void CreatePlayerProgram()
        {
            var go = new GameObject("PlayerProgram");
            _playerProgram = go.AddComponent<RoyaleProgram>();
            _playerProgram.Initialize(_codePlayer, _match, _zone);
            Log("Created RoyaleProgram (code-controlled player)");
        }

        // =================================================================
        // AI
        // =================================================================

        private void CreateAIControllers()
        {
            for (int i = 1; i < _allPlayers.Count; i++)
            {
                var player = _allPlayers[i];
                var go = new GameObject($"AI_{i}");
                var ai = go.AddComponent<RoyaleAIController>();

                // 5 easy, 5 medium, 5 hard
                RoyaleAIController.AIDifficulty diff;
                if (i <= 5)
                    diff = RoyaleAIController.AIDifficulty.Easy;
                else if (i <= 10)
                    diff = RoyaleAIController.AIDifficulty.Medium;
                else
                    diff = RoyaleAIController.AIDifficulty.Hard;

                ai.Initialize(player, _match, _zone, diff);
                _aiControllers.Add(ai);
            }

            Log($"Created {_aiControllers.Count} AI controllers (5 Easy, 5 Medium, 5 Hard)");
        }

        // =================================================================
        // WAR MASTER (fate controller — right panel)
        // =================================================================

        private void CreateWarMaster()
        {
            var go = new GameObject("WarMaster");
            _warMaster = go.AddComponent<RoyaleWarMaster>();
            _warMaster.Initialize(_match, _zone, warMasterDifficulty);
            Log($"Created RoyaleWarMaster (difficulty: {warMasterDifficulty})");
        }

        // =================================================================
        // TUI (dual-panel code debugger + status bar)
        // =================================================================

        private void CreateTUI()
        {
            var go = new GameObject("TUIManager");
            _tuiManager = go.AddComponent<RoyaleTUIManager>();
            _tuiManager.Initialize(_match, _playerProgram, _warMaster);
            Log("Created RoyaleTUIManager (left=SURVIVAL, right=WAR MASTER, bottom=STATUS)");
        }

        // =================================================================
        // EVENT WIRING
        // =================================================================

        private void WireEvents()
        {
            if (SimulationTime.Instance != null)
            {
                SimulationTime.Instance.OnTimeScaleChanged += s => Log($"Time scale → {s:F0}x");
                SimulationTime.Instance.OnPausedChanged += p => Log(p ? "PAUSED" : "RESUMED");
            }

            if (_match != null)
            {
                _match.OnPlayerEliminated += player =>
                    Log($"ELIMINATED: Player #{player.PlayerIndex} " +
                        $"(#{player.placement}, {_match.AliveCount} alive)");

                _match.OnVictory += winner =>
                    Log($"VICTORY! Player #{winner.PlayerIndex} " +
                        $"({winner.kills} kills, scope {winner.scope})");

                _match.OnAirdropSpawned += (x, z) =>
                    Log($"AIRDROP at ({x:F0}, {z:F0})");

                _match.OnAirstrikeSpawned += (x, z) =>
                    Log($"AIRSTRIKE at ({x:F0}, {z:F0})");
            }
        }

        // =================================================================
        // BOOT SEQUENCE
        // =================================================================

        private System.Collections.IEnumerator RunBootSequence()
        {
            yield return null;
            yield return null;

            LogDivider();
            Log("ROYALE — Code Your Way to the Last Survivor Standing");
            LogDivider();
            LogStatus("ARENA", $"{mapSize}×{mapSize} (buildings, rocks, trees)");
            LogStatus("PLAYERS", $"1 code + {botCount} AI bots = {_allPlayers.Count} total");
            LogStatus("CRATES", $"{crateCount} loot crates");
            LogStatus("ZONE", "5 phases, 30s→0, DPS 1→16");
            LogStatus("SCOPES", "None→2x→4x→8x→15x (view 30→100)");
            LogStatus("WEAPONS", "Fists, Pistol, Shotgun, SMG, AR, Sniper");
            LogStatus("TIME", SimulationTime.Instance?.GetFormattedTimeScale() ?? "1x");
            LogDivider();
            LogEnabled("Scripting", enableScripting);
            LogEnabled("AI       ", enableAI, $"{_aiControllers.Count} bots");
            LogEnabled("TUI      ", enableTUI);
            LogStatus("WAR MASTER", $"{warMasterDifficulty}");
            LogDivider();
            Log("Bootstrap complete. Write your survival code!");

            if (_match != null) _match.StartMatch();
        }

        // =================================================================
        // CLEANUP
        // =================================================================

        private void OnDestroy()
        {
            if (SimulationTime.Instance != null)
            {
                SimulationTime.Instance.OnTimeScaleChanged -= s => { };
                SimulationTime.Instance.OnPausedChanged -= p => { };
            }
        }
    }
}
