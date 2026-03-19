// Copyright CodeGamified 2025-2026
// MIT License — Royale
using UnityEngine;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;
using CodeGamified.Engine.Runtime;
using CodeGamified.Time;
using Royale.Game;

namespace Royale.Scripting
{
    /// <summary>
    /// RoyaleProgram — tick-based bytecode execution for one player.
    /// Runs at 20 ops/sec sim-time. PC resets to 0 on HALT.
    /// </summary>
    public class RoyaleProgram : ProgramBehaviour
    {
        private RoyalePlayer _player;
        private RoyaleMatchManager _match;
        private RoyaleZone _zone;
        private float _opAccumulator;

        public const float OPS_PER_SECOND = 20f;

        private const string DEFAULT_CODE = @"# Royale — survive the shrinking zone
# Loot weapons, fight enemies, stay in the zone!

while True:
    hp = get_health()
    
    # Heal if low and have item
    if hp < 40:
        if get_has_heal() == 1:
            use_heal()
    
    # Stay inside the zone
    if get_in_zone() == 0:
        move_to_zone()
        wait
    
    # Got a weapon? Look for fights
    if get_weapon() > 0:
        dist = get_enemy_dist()
        if dist > 0:
            # Enemy visible — aim and shoot
            angle = get_enemy_angle()
            set_facing(angle)
            if dist < get_weapon_range():
                shoot()
                if get_ammo() == 0:
                    reload()
            else:
                move_toward(angle)
            wait
    
    # No enemies — go loot (scopes + weapons)
    cdist = get_crate_dist()
    if cdist > 0:
        move_toward(get_crate_angle())
        if cdist < 2:
            loot()
    else:
        # Nothing to do, drift toward zone center
        move_to_zone()
    
    wait
";

        public System.Action OnCodeChanged;

        public void Initialize(RoyalePlayer player, RoyaleMatchManager match, RoyaleZone zone,
                               string initialCode = null)
        {
            _player = player;
            _match = match;
            _zone = zone;
            _sourceCode = initialCode ?? DEFAULT_CODE;
            _programName = "RoyaleAI";
            _autoRun = false;

            LoadAndRun(_sourceCode);
        }

        protected override void Start()
        {
            // Don't call base — Initialize handles setup
        }

        public override bool LoadAndRun(string source)
        {
            _sourceCode = source;
            _executor = new CodeExecutor();
            var handler = new RoyaleIOHandler(_player, _match, _zone);
            _executor.SetIOHandler(handler);

            _program = PythonCompiler.Compile(source, _programName,
                new RoyaleCompilerExtension());

            if (!_program.IsValid)
            {
                Debug.LogWarning("[RoyaleProgram] Compile errors:");
                foreach (var err in _program.Errors)
                    Debug.LogWarning($"  {err}");
                return false;
            }

            _executor.LoadProgram(_program);
            _isPaused = false;
            _opAccumulator = 0f;
            return true;
        }

        protected override void Update()
        {
            if (_executor == null || _program == null || _isPaused) return;
            if (_player == null || !_player.IsAlive) return;

            float timeScale = SimulationTime.Instance?.timeScale ?? 1f;
            if (SimulationTime.Instance != null && SimulationTime.Instance.isPaused) return;

            float simDelta = Time.deltaTime * timeScale;
            _opAccumulator += simDelta * OPS_PER_SECOND;

            int opsToRun = (int)_opAccumulator;
            _opAccumulator -= opsToRun;

            _player.ClearMoveCommand();

            for (int i = 0; i < opsToRun; i++)
            {
                if (_executor.State.IsHalted)
                {
                    _executor.State.PC = 0;
                    _executor.State.IsHalted = false;
                }
                _executor.ExecuteOne();
            }

            if (opsToRun > 0)
                ProcessEvents();
        }

        protected override IGameIOHandler CreateIOHandler()
        {
            return new RoyaleIOHandler(_player, _match, _zone);
        }

        protected override CompiledProgram CompileSource(string source, string name)
        {
            return PythonCompiler.Compile(source, name, new RoyaleCompilerExtension());
        }

        protected override void ProcessEvents()
        {
            while (_executor.State.OutputEvents.Count > 0)
                _executor.State.OutputEvents.Dequeue();
        }

        public void UploadCode(string newSource)
        {
            LoadAndRun(newSource ?? DEFAULT_CODE);
            OnCodeChanged?.Invoke();
        }
    }
}
