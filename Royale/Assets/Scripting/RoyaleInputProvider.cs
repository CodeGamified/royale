// Copyright CodeGamified 2025-2026
// MIT License — Royale
using UnityEngine;
using UnityEngine.InputSystem;

namespace Royale.Scripting
{
    /// <summary>
    /// Input provider — WASD/arrows for movement angle, mouse for aim.
    /// Encodes as a single float readable by GET_INPUT opcode.
    /// </summary>
    public class RoyaleInputProvider : MonoBehaviour
    {
        public static RoyaleInputProvider Instance { get; private set; }

        /// <summary>Movement direction as angle in degrees, or -1 if no input.</summary>
        public float CurrentInput { get; private set; }

        /// <summary>Mouse aim angle in degrees.</summary>
        public float AimAngle { get; private set; }

        private InputAction _moveAction;

        private void Awake()
        {
            Instance = this;

            _moveAction = new InputAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _moveAction.Enable();
        }

        private void Update()
        {
            Vector2 move = _moveAction.ReadValue<Vector2>();
            if (move.sqrMagnitude > 0.01f)
                CurrentInput = Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg;
            else
                CurrentInput = -1f;
        }

        private void OnDestroy()
        {
            _moveAction?.Disable();
            _moveAction?.Dispose();
            if (Instance == this)
                Instance = null;
        }
    }
}
