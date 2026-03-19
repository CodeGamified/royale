// Copyright CodeGamified 2025-2026
// MIT License — Royale
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Engine;
using CodeGamified.Engine.Runtime;
using CodeGamified.TUI;
using Royale.Scripting;
using static Royale.Scripting.RoyaleOpCode;

namespace Royale.UI
{
    /// <summary>
    /// Adapts a RoyaleProgram into the engine's IDebuggerDataSource contract.
    /// Fed to DebuggerSourcePanel, DebuggerMachinePanel, DebuggerStatePanel.
    /// </summary>
    public class RoyaleDebuggerData : IDebuggerDataSource
    {
        private readonly RoyaleProgram _program;
        private readonly string _label;

        public RoyaleDebuggerData(RoyaleProgram program, string label = null)
        {
            _program = program;
            _label = label;
        }

        // ── IDebuggerDataSource ─────────────────────────────────

        public string ProgramName => _label ?? _program?.ProgramName ?? "Royale";
        public string[] SourceLines => _program?.Program?.SourceLines;
        public bool HasLiveProgram =>
            _program != null && _program.Executor != null && _program.Program != null
            && _program.Program.Instructions != null && _program.Program.Instructions.Length > 0;
        public int PC
        {
            get
            {
                var s = _program?.State;
                if (s == null) return 0;
                return s.LastExecutedPC >= 0 ? s.LastExecutedPC : s.PC;
            }
        }
        public long CycleCount => _program?.State?.CycleCount ?? 0;

        public string StatusString
        {
            get
            {
                if (_program == null || _program.Executor == null)
                    return TUIColors.Dimmed("NO PROGRAM");
                var state = _program.State;
                if (state == null) return TUIColors.Dimmed("NO STATE");
                int instCount = _program.Program?.Instructions?.Length ?? 0;
                return TUIColors.Fg(TUIColors.BrightGreen, $"TICK {instCount} inst");
            }
        }

        public List<string> BuildSourceLines(int pc, int scrollOffset, int maxRows)
        {
            var lines = new List<string>();
            var src = SourceLines;
            if (src == null) return lines;

            int activeLine = -1;
            int activeEnd = -1;
            bool isHalt = false;
            Instruction activeInst = default;
            if (HasLiveProgram && _program.Program.Instructions.Length > 0
                && pc < _program.Program.Instructions.Length)
            {
                activeInst = _program.Program.Instructions[pc];
                activeLine = activeInst.SourceLine - 1;
                isHalt = activeInst.Op == OpCode.HALT;
                if (activeLine >= 0)
                    activeEnd = SourceHighlight.GetContinuationEnd(src, activeLine);
            }

            // Synthetic "while True:" at display row 0
            if (scrollOffset == 0 && lines.Count < maxRows)
            {
                string whileLine = "while True:";
                if (isHalt)
                    lines.Add(TUIColors.Fg(TUIColors.BrightGreen, $"  {TUIGlyphs.ArrowR}   {whileLine}"));
                else
                    lines.Add($"  {TUIColors.Dimmed(TUIGlyphs.ArrowR)}   {SynthwaveHighlighter.Highlight(whileLine)}");
            }

            // Find the ONE line that contains the active token
            int tokenLine = -1;
            if (activeLine >= 0)
            {
                string token = SourceHighlight.GetSourceToken(activeInst);
                if (token != null)
                {
                    for (int k = activeLine; k <= activeEnd; k++)
                    {
                        if (src[k].IndexOf(token) >= 0) { tokenLine = k; break; }
                    }
                }
                if (tokenLine < 0) tokenLine = activeLine;
            }

            // Auto-scroll to keep active source line visible
            int focusLine = tokenLine >= 0 ? tokenLine : activeLine;
            if (focusLine >= 0 && src.Length > maxRows)
                scrollOffset = Mathf.Clamp(focusLine - maxRows / 3, 0, src.Length - maxRows);

            for (int i = scrollOffset; i < src.Length && lines.Count < maxRows; i++)
            {
                if (i == tokenLine)
                {
                    lines.Add(SourceHighlight.HighlightActiveLine(
                        src[i], $" {i + 1:D3}      ", activeInst));
                }
                else
                {
                    string num = TUIColors.Dimmed($"{i + 1:D3}");
                    lines.Add($" {num}      {SynthwaveHighlighter.Highlight(src[i])}");
                }
            }
            return lines;
        }

        public List<string> BuildMachineLines(int pc, int maxRows)
        {
            var lines = new List<string>();
            if (!HasLiveProgram) return lines;

            var instructions = _program.Program.Instructions;
            int total = instructions.Length;

            int offset = 0;
            if (total > maxRows)
                offset = Mathf.Clamp(pc - maxRows / 3, 0, total - maxRows);
            int visibleCount = Mathf.Min(maxRows, total);

            for (int j = 0; j < visibleCount; j++)
            {
                int i = offset + j;
                var inst = instructions[i];
                bool isPC = (i == pc);
                string asm = inst.ToAssembly(FormatRoyaleOp);
                if (isPC)
                {
                    lines.Add(TUIColors.Fg(TUIColors.BrightGreen, $" {i:X3}  {asm}"));
                }
                else
                {
                    string addr = TUIColors.Dimmed($"{i:X3}");
                    lines.Add($" {addr}  {SynthwaveHighlighter.HighlightAsm(asm)}");
                }
            }
            return lines;
        }

        public List<string> BuildStateLines()
        {
            if (!HasLiveProgram) return new List<string>();
            var s = _program.State;
            int displayPC = s.LastExecutedPC >= 0 ? s.LastExecutedPC : s.PC;
            return TUIWidgets.BuildStateLines(
                s.Registers, s.LastRegisterModified,
                s.Flags, displayPC, s.Stack.Count,
                s.NameToAddress, s.Memory);
        }

        // ── Custom opcode formatting ────────────────────────────

        static string FormatRoyaleOp(Instruction inst)
        {
            int id = (int)inst.Op - (int)OpCode.CUSTOM_0;
            return (RoyaleOpCode)id switch
            {
                // Queries (no args → R0)
                GET_X              => "INP R0, POS_X",
                GET_Y              => "INP R0, POS_Y",
                GET_HEALTH         => "INP R0, HP",
                GET_ARMOR          => "INP R0, ARMOR",
                GET_ALIVE_COUNT    => "INP R0, ALIVE",
                GET_KILL_COUNT     => "INP R0, KILLS",
                GET_ZONE_X         => "INP R0, ZONE_X",
                GET_ZONE_Y         => "INP R0, ZONE_Y",
                GET_ZONE_RADIUS    => "INP R0, ZONE_R",
                GET_ZONE_SHRINKING => "INP R0, SHRINK",
                GET_IN_ZONE        => "INP R0, IN_ZONE",
                GET_WEAPON         => "INP R0, WEAPON",
                GET_AMMO           => "INP R0, AMMO",
                GET_AMMO_RESERVE   => "INP R0, AMMO_RSV",
                GET_RELOADING      => "INP R0, RELOAD?",
                GET_WEAPON_RANGE   => "INP R0, WPN_RNG",
                GET_ENEMY_DIST     => "INP R0, ENEMY_D",
                GET_ENEMY_ANGLE    => "INP R0, ENEMY_A",
                GET_ENEMY_HEALTH   => "INP R0, ENEMY_HP",
                GET_CRATE_DIST     => "INP R0, CRATE_D",
                GET_CRATE_ANGLE    => "INP R0, CRATE_A",
                GET_COVER_DIST     => "INP R0, COVER_D",
                GET_COVER_ANGLE    => "INP R0, COVER_A",
                GET_SLOT_WEAPON    => "INP R0, SLOT(R0)",
                GET_HAS_HEAL       => "INP R0, HAS_HEAL",
                GET_FACING         => "INP R0, FACING",
                GET_SCOPE          => "INP R0, SCOPE",
                GET_VIEW_RANGE     => "INP R0, VIEW_R",
                GET_INPUT          => "INP R0, INPUT",

                // Commands (act → R0)
                MOVE_TOWARD        => "OUT MOVE, R0",
                SET_FACING         => "OUT FACE, R0",
                SHOOT              => "OUT SHOOT",
                RELOAD             => "OUT RELOAD",
                LOOT               => "OUT LOOT",
                SWAP_WEAPON        => "OUT SWAP",
                USE_HEAL           => "OUT HEAL",
                MOVE_TO_ZONE       => "OUT ZONE_RUN",

                // War Master fate commands
                GET_MATCH_TIME     => "INP R0, MATCH_T",
                GET_PHASE          => "INP R0, PHASE",
                SPAWN_AIRDROP      => "OUT AIRDROP, R0, R1",
                CALL_AIRSTRIKE     => "OUT STRIKE, R0, R1",
                SHIFT_ZONE         => "OUT SHIFT, R0, R1",

                _                  => $"IO.{id,2} {inst.Arg0}, {inst.Arg1}"
            };
        }
    }
}
