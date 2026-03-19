// Copyright CodeGamified 2025-2026
// MIT License — Royale
using CodeGamified.TUI;
using Royale.Scripting;

namespace Royale.UI
{
    /// <summary>
    /// Thin adapter — wires a RoyaleProgram into the engine's CodeDebuggerWindow
    /// via RoyaleDebuggerData (IDebuggerDataSource). All rendering lives in the engine.
    /// </summary>
    public class RoyaleCodeDebugger : CodeDebuggerWindow
    {
        protected override void Awake()
        {
            base.Awake();
            windowTitle = "CODE";
        }

        public void Bind(RoyaleProgram program)
        {
            SetDataSource(new RoyaleDebuggerData(program));
        }
    }
}
