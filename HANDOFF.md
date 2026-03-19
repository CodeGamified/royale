# HANDOFF — Bootstrapping Any CodeGamified Game

**For agentic coding agents.** This is the minimum context to implement a new game
or resume work on an existing one without re-reading the engine codebase.

---

## 1. Repo Structure

```
codegamified.github.io/
├── .engine/                  ← shared engine submodule (canonical copy)
├── pong/Pong/Assets/         ← GOLD STANDARD — reference for all patterns
│   ├── Engine/               ← .engine submodule (symlinked)
│   ├── Core/                 ← Bootstrap, SimulationTime
│   ├── Game/                 ← Domain objects (Ball, Paddle, Court, MatchManager)
│   ├── Scripting/            ← IOHandler, CompilerExtension, EditorExtension, Program
│   ├── AI/                   ← AI opponent
│   ├── Audio/                ← IAudioProvider impl
│   ├── UI/                   ← TUI windows (debugger, status)
│   ├── Persistence/          ← PersistenceManager, entity serializer
│   └── Procedural/           ← Blueprints for ProceduralAssembler
├── bootstrap/Bootstrap/Assets/ ← TEMPLATE — copy this for new games
│   ├── Engine/               ← .engine submodule
│   ├── Core/                 ← TemplateBootstrap, TemplateSimulationTime
│   ├── Game/                 ← TemplateGrid, TemplateMatchManager, TemplateRenderer
│   └── Scripting/            ← TemplateCompilerExtension, TemplateIOHandler, TemplateProgram,
│                               TemplateEditorExtension, TemplateInputProvider
├── tetris/Tetris/Assets/     ← first pass complete
├── snake/Snake/Assets/       ← first pass complete
├── bird/Bird/Assets/         ← first pass complete
├── asteroids/Asteroids/Assets/ ← first pass complete
├── breakout/Breakout/Assets/ ← first pass complete
├── minesweeper/Minesweeper/Assets/ ← first pass complete
├── connect/Connect/Assets/   ← first pass complete
├── checkers/Checkers/Assets/ ← first pass complete
├── chess/Chess/Assets/       ← first pass complete
├── pool/Pool/Assets/         ← first pass complete
├── blackjack/Blackjack/Assets/ ← first pass complete
├── poker/Poker/Assets/       ← first pass complete
├── racer/Racer/Assets/       ← first pass complete
├── fighter/Fighter/Assets/   ← first pass complete
├── dwarf/Dwarf/Assets/      ← first pass complete
└── tanks/                    ← (planned)
```

**Every game follows the same folder layout.** Copy Bootstrap for new games, or mirror Pong for the full pattern.

---

## 2. The 10 Files You Must Create

Every game needs exactly these files (namespace = `{Game}.{Folder}`):

| # | Folder | File | Extends/Implements | Purpose |
|---|--------|------|--------------------|---------|
| 1 | `Core/` | `{Game}SimulationTime.cs` | `SimulationTime` | MaxTimeScale, presets, time formatting |
| 2 | `Core/` | `{Game}Bootstrap.cs` | `GameBootstrap` | Instantiate everything, wire events, start match |
| 3 | `Game/` | *domain objects* | `MonoBehaviour` | Board/court/entities — the game's physical state |
| 4 | `Game/` | `{Game}MatchManager.cs` | `MonoBehaviour` | Scoring, spawning, game over, auto-restart |
| 5 | `Scripting/` | `{Game}CompilerExtension.cs` | `ICompilerExtension` | Opcode enum + `TryCompileCall` for each builtin |
| 6 | `Scripting/` | `{Game}IOHandler.cs` | `IGameIOHandler` | `ExecuteIO` switch on custom opcodes → game state |
| 7 | `Scripting/` | `{Game}Program.cs` | `ProgramBehaviour` | Tick-based execution loop + default starter code |
| 8 | `Scripting/` | `{Game}EditorExtension.cs` | `IEditorExtension` | Function list for tap-to-code editor |
| 9 | `Scripting/` | `{Game}InputProvider.cs` | `MonoBehaviour` | Keyboard/gamepad → single float for bytecode |
| 10 | `Game/` | `{Game}BoardRenderer.cs` | `MonoBehaviour` | Visual representation (3D cubes, procedural, etc.) |

**Files 1-9 are mandatory for any game. File 10 varies by game.**

---

## 3. Engine Module Cheat Sheet

DO NOT re-read the engine source. Use this reference.

### 3a. Namespaces & Key Types

```
CodeGamified.Bootstrap    → GameBootstrap (abstract MonoBehaviour)
CodeGamified.Engine       → CodeExecutor, MachineState, Instruction, OpCode, CompiledProgram,
                            IGameIOHandler, GameEvent, CpuFlags
CodeGamified.Engine.Compiler → PythonCompiler, CompilerContext, ICompilerExtension, AstNodes
CodeGamified.Engine.Runtime  → ProgramBehaviour (abstract MonoBehaviour)
CodeGamified.Time         → SimulationTime (abstract singleton MonoBehaviour), TimeWarpController
CodeGamified.TUI          → TerminalWindow, TUIColors, TUIGlyphs, TUIWidgets, TUIFormat, ...
CodeGamified.Editor       → CodeEditorWindow, IEditorExtension, EditorFuncInfo, EditorTypeInfo
CodeGamified.Persistence  → EntityStore<T>, IEntitySerializer<T>, IGitRepository, PlayerIdentity
CodeGamified.Persistence.Providers → LocalGitProvider, MemoryGitProvider, PublicRepoReader
CodeGamified.Audio        → AudioBridge, IAudioProvider, IHapticProvider, HapticBridge, Equalizer
CodeGamified.Camera       → CameraRig, CameraAmbientMotion, CameraFlashlight
CodeGamified.Quality      → QualityBridge (static), QualityTier, IQualityResponsive
CodeGamified.Settings     → SettingsBridge (static), ISettingsListener, SettingsSnapshot
CodeGamified.Procedural   → ProceduralAssembler, IProceduralBlueprint, ColorPalette, AssemblyResult
```

### 3b. MachineState

- 8 float registers: `R0..R7`
- 64-deep stack, 256-slot named memory
- PC, flags (Zero/Negative/Carry/Overflow), IsHalted, IsWaiting
- `GameData` dictionary for opaque game state
- `OutputEvents` queue for game events

### 3c. OpCode Layout

```
LOAD_CONST, LOAD_FLOAT, LOAD_MEM, STORE_MEM, MOV          ← data
ADD, SUB, MUL, DIV, MOD, INC, DEC, MIN, MAX               ← arithmetic
CMP, JMP, JEQ, JNE, JLT, JGT, JLE, JGE                   ← control flow
PUSH, POP, CALL, RET                                       ← stack
WAIT, NOP, HALT, BREAK                                     ← system
CUSTOM_0 .. CUSTOM_31                                      ← game I/O (yours)
```

### 3d. CompilerContext.Emit Signature

```csharp
ctx.Emit(OpCode op, int arg0 = 0, int arg1 = 0, int arg2 = 0,
         int sourceLine = -1, string comment = null, int tag = 0);
```

Emit a CUSTOM opcode: `ctx.Emit(OpCode.CUSTOM_0 + (int)MyOpCode.FOO, 0, 0, 0, sourceLine, "comment");`

### 3e. GameBootstrap Helpers

```csharp
protected abstract string LogTag { get; }
protected Camera EnsureCamera(Camera existing = null);
protected T EnsureSimulationTime<T>() where T : SimulationTime;
protected T FindOrCreate<T>() where T : Component;
protected T CreateManager<T>(string name = null) where T : Component;
protected void Log(string msg);
protected void LogDivider();
protected void LogStatus(string label, string value);
protected void LogEnabled(string label, bool enabled, string detail = null);
protected void RunAfterFrames(System.Action action, int frames = 2);
```

---

## 4. Scripting Pattern — Copy-Paste Template

### 4a. Opcode Enum

```csharp
public enum {Game}OpCode
{
    // Queries (read game state → R0)
    GET_FOO = 0,   // CUSTOM_0
    GET_BAR = 1,   // CUSTOM_1
    // Commands (act on game, result → R0: 1=success, 0=fail)
    DO_THING = 10, // CUSTOM_10
}
```

### 4b. CompilerExtension — One case per builtin

```csharp
public bool TryCompileCall(string fn, List<AstNodes.ExprNode> args, CompilerContext ctx, int line)
{
    switch (fn)
    {
        // Query — no args, result in R0
        case "get_foo":
            ctx.Emit(OpCode.CUSTOM_0 + (int){Game}OpCode.GET_FOO, 0, 0, 0, line, "get_foo → R0");
            return true;

        // Command — one arg compiled to R0 first
        case "do_thing":
            if (args != null && args.Count > 0)
                args[0].Compile(ctx); // arg → R0
            ctx.Emit(OpCode.CUSTOM_0 + (int){Game}OpCode.DO_THING, 0, 0, 0, line, "do_thing(R0)");
            return true;

        // Two-arg query — push/pop pattern to load R0 and R1
        case "get_cell":
            if (args != null && args.Count >= 2)
            {
                args[0].Compile(ctx);                    // arg0 → R0
                ctx.Emit(OpCode.PUSH, 0);                // save R0
                args[1].Compile(ctx);                    // arg1 → R0
                ctx.Emit(OpCode.MOV, 1, 0);              // R0 → R1
                ctx.Emit(OpCode.POP, 0);                 // restore arg0 → R0
            }
            ctx.Emit(OpCode.CUSTOM_0 + (int){Game}OpCode.GET_CELL, 0, 0, 0, line, "get_cell(R0,R1) → R0");
            return true;

        default: return false;
    }
}
```

### 4c. IOHandler — One case per opcode

```csharp
public void ExecuteIO(Instruction inst, MachineState state)
{
    switch (({Game}OpCode)((int)inst.Op - (int)OpCode.CUSTOM_0))
    {
        case {Game}OpCode.GET_FOO:
            state.SetRegister(0, _someValue);
            break;
        case {Game}OpCode.DO_THING:
            bool ok = _game.DoThing(state.GetRegister(0));
            state.SetRegister(0, ok ? 1f : 0f);
            break;
    }
}
// Always implement:
public bool PreExecute(Instruction inst, MachineState state) => true;
public float GetTimeScale() => SimulationTime.Instance?.timeScale ?? 1f;
public double GetSimulationTime() => SimulationTime.Instance?.simulationTime ?? 0.0;
```

### 4d. ProgramBehaviour — Tick Model

```csharp
public class {Game}Program : ProgramBehaviour
{
    public const float OPS_PER_SECOND = 20f;
    private float _opAccumulator;

    protected override void Update()
    {
        if (_executor == null || _program == null || _isPaused) return;
        float timeScale = SimulationTime.Instance?.timeScale ?? 1f;
        if (SimulationTime.Instance != null && SimulationTime.Instance.isPaused) return;

        float simDelta = UnityEngine.Time.deltaTime * timeScale;
        _opAccumulator += simDelta * OPS_PER_SECOND;

        int opsToRun = (int)_opAccumulator;
        _opAccumulator -= opsToRun;

        for (int i = 0; i < opsToRun; i++)
        {
            if (_executor.State.IsHalted)
            {
                _executor.State.PC = 0;
                _executor.State.IsHalted = false;
            }
            _executor.ExecuteOne();
        }
        if (opsToRun > 0) ProcessEvents();
    }

    protected override IGameIOHandler CreateIOHandler() => new {Game}IOHandler(_match, _board);
    protected override CompiledProgram CompileSource(string source, string name)
        => PythonCompiler.Compile(source, name, new {Game}CompilerExtension());
    protected override void ProcessEvents()
    {
        while (_executor.State.OutputEvents.Count > 0)
            _executor.State.OutputEvents.Dequeue();
    }
}
```

---

## 5. Bootstrap Wiring Order

Every `{Game}Bootstrap.Start()` follows this sequence:

```csharp
void Start()
{
    // 1. Settings + Quality
    SettingsBridge.Load();
    QualityBridge.SetTier((QualityTier)SettingsBridge.QualityLevel);
    QualityBridge.Register(this);

    // 2. Simulation time
    EnsureSimulationTime<{Game}SimulationTime>();

    // 3. Camera + post-processing
    SetupCamera();

    // 4. Game domain objects (board, court, entities)
    CreateBoard();

    // 5. Match manager (needs board)
    CreateMatchManager();

    // 6. Visual renderer (needs board + match)
    CreateBoardRenderer();

    // 7. Input provider
    CreateInputProvider();

    // 8. Player program (needs match + board)
    if (enableScripting) CreatePlayerProgram();

    // 9. Audio, Persistence, TUI (optional, order doesn't matter)
    CreateAudio();
    if (enablePersistence) CreatePersistence();
    if (enableTUI) CreateTUI();

    // 10. Wire events + start
    WireEvents();
    StartCoroutine(RunBootSequence());
}
```

---

## 6. SimulationTime — Minimal Subclass

```csharp
public class {Game}SimulationTime : SimulationTime
{
    protected override float MaxTimeScale => 100f; // Pong uses 1000
    protected override void OnInitialize()
    {
        timeScalePresets = new[] { 0f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 50f, 100f };
        currentPresetIndex = 3; // 1x
    }
    public override string GetFormattedTime()
    {
        int m = (int)(simulationTime / 60.0);
        int s = (int)(simulationTime % 60.0);
        return $"{m:D2}:{s:D2}";
    }
}
```

**Inherited for free:** `simulationTime`, `timeScale`, `isPaused`, P=pause, +/-=presets,
events `OnSimulationTimeChanged`, `OnTimeScaleChanged`, `OnPausedChanged`.

---

## 7. Camera Setup Template

```csharp
private void SetupCamera()
{
    var cam = EnsureCamera();
    cam.orthographic = false;
    cam.fieldOfView = 60f;
    cam.transform.position = /* look at board center from -Z */;
    cam.transform.LookAt(/* board center */);
    cam.clearFlags = CameraClearFlags.SolidColor;
    cam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);

    // Sway
    var sway = cam.gameObject.AddComponent<CameraAmbientMotion>();
    sway.lookAtTarget = /* board center */;

    // URP post-processing (bloom for neon glow)
    var camData = cam.GetComponent<UniversalAdditionalCameraData>()
        ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
    camData.renderPostProcessing = true;

    var vol = new GameObject("PostProcessVolume").AddComponent<Volume>();
    vol.isGlobal = true;
    vol.priority = 1;
    var profile = ScriptableObject.CreateInstance<VolumeProfile>();
    var bloom = profile.Add<Bloom>();
    bloom.threshold.Override(0.8f);
    bloom.intensity.Override(1.0f);
    bloom.scatter.Override(0.5f);
    vol.profile = profile;
}
```

Required usings: `UnityEngine.Rendering`, `UnityEngine.Rendering.Universal`, `CodeGamified.Camera`.

---

## 8. InputProvider Pattern

Encode all game input as a single float (or a few) readable by bytecode via `GET_INPUT` opcode:

```csharp
public class {Game}InputProvider : MonoBehaviour
{
    public static {Game}InputProvider Instance { get; private set; }
    public float CurrentInput { get; private set; }

    private void Awake() { Instance = this; /* create InputActions */ }
    private void Update() { /* read input → set CurrentInput */ }
    private void OnDestroy() { /* dispose actions, null Instance */ }
}
```

Use Unity InputSystem `InputAction` with explicit bindings (no asset dependency).

---

## 9. EditorExtension — Tap-to-Code Metadata

```csharp
public class {Game}EditorExtension : IEditorExtension
{
    public List<EditorTypeInfo> GetAvailableTypes() => new();
    public List<EditorFuncInfo> GetAvailableFunctions() => new()
    {
        new() { Name = "get_foo", Hint = "description", ArgCount = 0 },
        new() { Name = "do_bar", Hint = "description", ArgCount = 1 },
    };
    public List<EditorMethodInfo> GetMethodsForType(string t) => new();
    public List<string> GetVariableNameSuggestions() => new() { "x", "target" };
    public List<string> GetStringLiteralSuggestions() => new();
}
```

---

## 10. Conventions

| Convention | Rule |
|-----------|------|
| Namespaces | `{Game}.Core`, `{Game}.Game`, `{Game}.Scripting`, `{Game}.Audio`, `{Game}.UI`, `{Game}.Persistence`, `{Game}.AI` |
| File header | `// Copyright CodeGamified 2025-2026` + `// MIT License — {Game}` |
| Opcode enum | `{Game}OpCode`, values 0-based, queries first then commands |
| Query convention | Result always in `R0` |
| Command convention | Args in `R0`/`R1`, success/fail in `R0` (1/0) |
| Two-arg builtins | Push/pop pattern: compile arg0→R0, PUSH R0, compile arg1→R0, MOV R1 R0, POP R0 |
| Tick model | `OPS_PER_SECOND = 20f`, sim-time aware, PC resets to 0 on HALT |
| Determinism | Same sim-time = same ops = same results at any time scale |
| Scoring | NES-style when applicable (lines×multiplier×level) |
| Auto-restart | Configurable via Inspector bool, sim-time-aware delay coroutine |
| Color scheme | Dark background (`0.01, 0.01, 0.02`), neon shape colors, dim frame |

---

## 11. Existing Implementations

### Pong (COMPLETE — gold standard)

```
Core/       PongBootstrap, PongSimulationTime, PongCameraSway
Game/       PongBall, PongPaddle, PongCourt, PongMatchManager, PaddleSide, PongBallTrail, PongLeaderboard
Scripting/  PongIOHandler, PongCompilerExtension, PongEditorExtension, PaddleProgram, PongInputProvider
AI/         PongAIController, AIDifficulty
Audio/      PongAudioProvider, PongHapticProvider
UI/         PongTUIManager, PongStatusPanel, PongCodeDebugger, PongDebuggerData
Persistence/ PongPersistenceManager, PongScriptData
Procedural/ PongBallBlueprint, PongPaddleBlueprint, PongCourtBlueprint, PongGoalZoneBlueprint
```

Opcodes: `GET_BALL_X/Y`, `GET_BALL_VX/VY`, `GET_PADDLE_Y/X`, `GET_SCORE`, `GET_OPP_SCORE`,
`GET_OPP_Y`, `GET_INPUT_Y`, `SET_TARGET_Y`, `MOVE_TARGET_Y`, `GET_MOUSE_Y`,
`GET_COURT_H/W`, `WAIT_OPP_HIT`, `WAIT_WALL_HIT`

### Tetris (FIRST PASS — playable, no Audio/TUI/Persistence/Procedural yet)

```
Core/       TetrisBootstrap, TetrisSimulationTime
Game/       TetrisBoard, TetrisPiece, TetrisMatchManager, TetrisBoardRenderer, Tetrominos
Scripting/  TetrisIOHandler, TetrisCompilerExtension, TetrisEditorExtension, TetrisProgram, TetrisInputProvider
```

Opcodes: `GET_PIECE`, `GET_ROTATION`, `GET_PIECE_ROW/COL`, `GET_NEXT`, `GET_HELD`,
`GET_SCORE`, `GET_LEVEL`, `GET_LINES`, `GET_BOARD_CELL`, `GET_COL_HEIGHT`, `GET_HOLES`,
`GET_MAX_HEIGHT`, `GET_GHOST_ROW`, `GET_BOARD_WIDTH/HEIGHT`, `GET_DROP_TIMER`, `GET_INPUT`,
`MOVE_LEFT/RIGHT`, `SOFT_DROP`, `HARD_DROP`, `ROTATE_CW/CCW`, `HOLD`

### Snake, Bird, Asteroids, Breakout, Minesweeper, Connect 4, Checkers, Chess (FIRST PASS)

All follow the same 10-file pattern. See each game's `Scripting/{Game}CompilerExtension.cs`
for the opcode surface. Each ships with default starter code in `{Game}Program.cs`.

### Pool (FIRST PASS)

```
Core/       PoolBootstrap, PoolSimulationTime
Game/       PoolTable, BallGroup, PoolMatchManager, PoolRenderer
Scripting/  PoolCompilerExtension, PoolIOHandler, PoolProgram, PoolEditorExtension, PoolInputProvider
```

8-ball billiards with custom 2D physics, 16 balls, 6 pockets, 26 opcodes.
Player writes code to compute shot angle and power.

### Blackjack (FIRST PASS)

```
Core/       BlackjackBootstrap, BlackjackSimulationTime
Game/       BlackjackShoe, BlackjackMatchManager, BlackjackRenderer
Scripting/  BlackjackCompilerExtension, BlackjackIOHandler, BlackjackProgram,
            BlackjackEditorExtension, BlackjackInputProvider
```

Casino blackjack with 6-deck shoe, 28 opcodes. Player writes code for basic strategy,
card counting (hi-lo), bet sizing.

### Poker (FIRST PASS)

```
Core/       PokerBootstrap, PokerSimulationTime
Game/       PokerDeck, PokerHandEvaluator, PokerMatchManager, PokerRenderer
Scripting/  PokerCompilerExtension, PokerIOHandler, PokerProgram,
            PokerEditorExtension, PokerInputProvider
```

Texas Hold'em No-Limit, 6-player table (seat 0 = code-controlled, seats 1-5 = AI).
32 opcodes (27 queries + 5 commands). Full hand evaluator (high card → straight flush).
Player writes code for pre-flop selection, pot odds, bet sizing, opponent modeling.

### Racer (FIRST PASS)

```
Core/       RacerBootstrap, RacerSimulationTime
Game/       RacerTrack, RacerCar, RacerMatchManager, RacerRenderer
Scripting/  RacerCompilerExtension, RacerIOHandler, RacerProgram,
            RacerEditorExtension, RacerInputProvider
```

Top-down racing with procedural oval track (32 waypoints), car physics on XZ plane.
28 opcodes (24 queries + 2 commands + 2 utility). Off-road heavy drag penalty.
Player writes code for waypoint following, PID steering, speed/brake management.

### Fighter (FIRST PASS)

```
Core/       FighterBootstrap, FighterSimulationTime
Game/       FighterCharacter, FighterMatchManager, FighterRenderer
Scripting/  FighterCompilerExtension, FighterIOHandler, FighterProgram,
            FighterEditorExtension, FighterInputProvider
```

Street Fighter-style 2D fighting game on X axis, side-scrolling camera.
32 opcodes (22 queries + 10 commands). Two fighters: player (code-controlled) vs AI.
5 attack types (light/heavy punch, light/heavy kick, special), blocking, jumping, crouching.
Special meter builds on hit/getting hit, specials cost 50 meter. Best-of-3 rounds, 60s timer.
Player writes code for spacing, attack selection, blocking reads, combo logic.

### Dwarf (FIRST PASS)

```
Core/       DwarfBootstrap, DwarfSimulationTime
Game/       FortressGrid, DwarfMatchManager, FortressRenderer
Scripting/  DwarfCompilerExtension, DwarfIOHandler, DwarfProgram,
            DwarfEditorExtension, DwarfInputProvider
```

Dwarf Fortress-inspired management sim with 2D cross-section grid (X × Z-depth).
35 opcodes (22 queries + 13 commands). Season/year time system.
7 workshop types (Carpenter, Mason, Smelter, Forge, Still, Kitchen, Craftsdwarf).
Resource chain: dig→stone, dig deep→ore, smelt→bars, forge→goods.
Threats (ambush/siege), drawbridge defense, military recruitment.
Player writes code for excavation, production chains, threat response.

### Bootstrap/Template (TEMPLATE — copy for new games)

```
Core/       TemplateBootstrap, TemplateSimulationTime
Game/       TemplateGrid, TemplateMatchManager, TemplateRenderer
Scripting/  TemplateCompilerExtension, TemplateIOHandler, TemplateProgram,
            TemplateEditorExtension, TemplateInputProvider
```

Minimal "move marker to target" game. Every file has `// TEMPLATE: REPLACE` comments.
Search for `REPLACE` to find all customization points.

---

## 12. What "First Pass" Means

A first-pass game is **playable**: bootstrap → match runs → code controls gameplay → game over → restart.

First pass includes:
- [x] SimulationTime subclass
- [x] Bootstrap (create all objects, wire events, start match)
- [x] Domain objects (board/entities with collision/physics)
- [x] MatchManager (scoring, spawning, game over, auto-restart)
- [x] CompilerExtension + OpCode enum
- [x] IOHandler (all opcodes wired)
- [x] ProgramBehaviour subclass with tick model
- [x] EditorExtension (function metadata)
- [x] InputProvider (keyboard → bytecode-readable float)
- [x] Visual renderer (3D cubes or ProceduralAssembler)

First pass does NOT include (add in later passes):
- [ ] Audio (IAudioProvider impl + AudioBridge wiring)
- [ ] TUI windows (TerminalWindow subclass + status panel)
- [ ] Persistence (EntityStore + autosave)
- [ ] Procedural blueprints (IProceduralBlueprint for each visual)
- [ ] AI opponent
- [ ] Leaderboard
- [ ] Code debugger (CodeDebuggerWindow)
- [ ] Quality-responsive visuals (IQualityResponsive beyond stub)

---

## 13. Quick-Start: New Game in Under 5 Minutes

1. **Copy `bootstrap/Bootstrap/Assets/` folder structure** (it's the compilable template)
2. **Rename** all files/classes/namespaces from `Template` → `{Game}`
3. **Replace domain objects** (TemplateGrid → your board, TemplateMatchManager → your game flow)
4. **Define opcodes** — what can the player's script read and do?
5. **Wire IOHandler** — one switch case per opcode
6. **Wire CompilerExtension** — one case per builtin function name
7. **Write default starter code** — the simplest script that makes the game playable
8. **Adjust Bootstrap.Start()** — camera position, inspector knobs, event wiring
9. **Test** — Press Play, verify script runs, game plays, game over triggers restart

Every file in `bootstrap/Bootstrap/Assets/` has `// TEMPLATE:` comments marking
what to replace. Search for `REPLACE` to find all customization points.

---

## 14. File Paths

Engine submodule: `{game}/{Game}/Assets/Engine/`
Game code: `{game}/{Game}/Assets/{Folder}/{File}.cs`
Scene: `{game}/{Game}/Assets/Scenes/SampleScene.unity`

The engine is a git submodule. Do NOT edit files under `Assets/Engine/`.
All game-specific code lives outside the Engine folder.