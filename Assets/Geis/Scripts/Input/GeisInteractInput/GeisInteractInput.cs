using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Geis.InteractInput
{
    /// <summary>
    /// Which realm(s) allow using the Interact action for a given object. Aligns with puzzle <c>PuzzleRealmMode</c>.
    /// </summary>
    public enum InteractRealmScope
    {
        SoulOnly,
        PhysicalOnly,
        BothRealms,
    }

    /// <summary>
    /// Reads <c>Player/Interact</c> from the generated controls asset (registered by GeisInputReader).
    /// Rebind in GeisControls.inputactions. When no action is registered, falls back to keyboard X/B and gamepad face buttons.
    /// Realm gating uses <see cref="SoulRealmIsActiveProvider"/> (set by GeisInputReader from SoulRealmManager).
    /// </summary>
    public static class GeisInteractInput
    {
        private static InputAction _interactAction;
        private static InputAction _moveAction;

        private static int _interactionMovementFreezeCount;

        /// <summary>
        /// When null, soul realm is treated as inactive (physical world). Set by GeisInputReader.
        /// </summary>
        public static Func<bool> SoulRealmIsActiveProvider { get; set; }

        /// <summary>
        /// World position for interaction range (ghost while soul realm is active). Set by GeisInputReader
        /// so assemblies that cannot reference SoulRealmManager still get correct proximity.
        /// </summary>
        public static Func<Vector3> InteractionWorldPositionProvider { get; set; }

        private static bool SoulRealmActive => SoulRealmIsActiveProvider != null && SoulRealmIsActiveProvider();

        /// <summary>
        /// Use for distance to puzzles/NPCs. Prefers <see cref="InteractionWorldPositionProvider"/>; otherwise first <c>Player</c> tag (unreliable when body + ghost both tagged Player).
        /// </summary>
        public static Vector3 GetInteractionWorldPositionOrFallback()
        {
            if (InteractionWorldPositionProvider != null)
                return InteractionWorldPositionProvider();

            var go = GameObject.FindGameObjectWithTag("Player");
            return go != null ? go.transform.position : Vector3.zero;
        }

        /// <summary>
        /// GeisInputReader assigns <c>Player/Interact</c> when the Player map is enabled.
        /// </summary>
        public static void SetInteractAction(InputAction interactAction) => _interactAction = interactAction;

        /// <summary>
        /// GeisInputReader assigns <c>Player/Move</c> so puzzles (e.g. alignment dial) can read strafe from the same WASD / stick bindings as locomotion.
        /// </summary>
        public static void SetMoveAction(InputAction moveAction) => _moveAction = moveAction;

        /// <summary>
        /// Ref-counted freeze: while &gt; 0, locomotion and ghost motor treat move input as zero (e.g. alignment dial).
        /// </summary>
        public static void PushInteractionMovementFreeze() => _interactionMovementFreezeCount++;

        public static void PopInteractionMovementFreeze() =>
            _interactionMovementFreezeCount = Mathf.Max(0, _interactionMovementFreezeCount - 1);

        public static bool IsMovementFrozenForInteraction => _interactionMovementFreezeCount > 0;

        /// <summary>
        /// Use when applying <see cref="GeisInputReader._moveComposite"/> so interaction freezes suppress walking.
        /// </summary>
        public static Vector2 GetEffectiveMoveCompositeForLocomotion(Vector2 rawComposite) =>
            IsMovementFrozenForInteraction ? Vector2.zero : rawComposite;

        /// <summary>Gamepad left stick X, or 0 if no gamepad.</summary>
        public static float GetGamepadLeftStickHorizontal()
        {
            var gp = GetGamepad();
            return gp != null ? gp.leftStick.x.ReadValue() : 0f;
        }

        /// <summary>Digital -1 / 0 / 1 from A/D and left/right arrows (keyboard).</summary>
        public static float GetKeyboardHorizontalDigital()
        {
            if (Keyboard.current == null)
                return 0f;
            float h = 0f;
            if (Keyboard.current[Key.A].isPressed || Keyboard.current[Key.LeftArrow].isPressed) h -= 1f;
            if (Keyboard.current[Key.D].isPressed || Keyboard.current[Key.RightArrow].isPressed) h += 1f;
            return Mathf.Clamp(h, -1f, 1f);
        }

        /// <summary>
        /// Horizontal component of <c>Player/Move</c> when the action is set and enabled; otherwise 0. Use <see cref="GetMoveHorizontalWithFallback"/> for raw keyboard/gamepad fallback.
        /// </summary>
        public static float GetMoveHorizontal()
        {
            if (_moveAction == null || !_moveAction.enabled)
                return 0f;
            return Mathf.Clamp(_moveAction.ReadValue<Vector2>().x, -1f, 1f);
        }

        /// <summary>
        /// Strafe input for dial-style minigames: prefers <c>Player/Move</c>.x, then keyboard A/D and arrows, then gamepad left stick X.
        /// </summary>
        public static float GetMoveHorizontalWithFallback()
        {
            float h = GetMoveHorizontal();
            if (Mathf.Abs(h) > 0.01f)
                return h;

            if (Keyboard.current != null)
            {
                if (Keyboard.current[Key.A].isPressed || Keyboard.current[Key.LeftArrow].isPressed) h -= 1f;
                if (Keyboard.current[Key.D].isPressed || Keyboard.current[Key.RightArrow].isPressed) h += 1f;
            }

            var gp = Gamepad.current;
            if (gp == null && Gamepad.all.Count > 0)
                gp = Gamepad.all[0];
            if (gp != null)
                h += gp.leftStick.x.ReadValue();

            return Mathf.Clamp(h, -1f, 1f);
        }

        /// <summary>
        /// Same rules as puzzle realm modes (SoulOnly → soul on, etc.). Used for NPCs when
        /// <see cref="SoulRealmIsActiveProvider"/> is set. Puzzle element visibility/dissolve uses SoulRealmManager directly.
        /// </summary>
        public static bool IsInteractRealmAllowed(InteractRealmScope scope)
        {
            return scope switch
            {
                InteractRealmScope.SoulOnly     => SoulRealmActive,
                InteractRealmScope.PhysicalOnly => !SoulRealmActive,
                InteractRealmScope.BothRealms   => true,
                _                             => false,
            };
        }

        public static bool WasInteractPressedThisFrame()
        {
            if (TryGetAction(out var action))
                return action.WasPressedThisFrame();

            return FallbackKeyboardPressed() || FallbackGamepadPressed();
        }

        /// <summary>
        /// True when interact registered a press this frame from the gamepad West face button
        /// (Xbox X / PlayStation Square). Keyboard-only interact does not set this.
        /// </summary>
        public static bool WasGamepadWestInteractPressedThisFrame()
        {
            if (!WasInteractPressedThisFrame())
                return false;
            var gp = GetGamepad();
            return gp != null && gp.buttonWest.wasPressedThisFrame;
        }

        public static bool WasInteractReleasedThisFrame()
        {
            if (TryGetAction(out var action))
                return action.WasReleasedThisFrame();

            return FallbackKeyboardReleased() || FallbackGamepadReleased();
        }

        public static bool IsInteractHeld()
        {
            if (TryGetAction(out var action))
                return action.IsPressed();

            return FallbackKeyboardHeld() || FallbackGamepadHeld();
        }

        public static bool WasInteractPressedThisFrameInRealm(InteractRealmScope scope) =>
            IsInteractRealmAllowed(scope) && WasInteractPressedThisFrame();

        public static bool WasInteractReleasedThisFrameInRealm(InteractRealmScope scope) =>
            IsInteractRealmAllowed(scope) && WasInteractReleasedThisFrame();

        public static bool IsInteractHeldInRealm(InteractRealmScope scope) =>
            IsInteractRealmAllowed(scope) && IsInteractHeld();

        private static bool TryGetAction(out InputAction action)
        {
            action = _interactAction;
            return action != null && action.enabled;
        }

        private static bool AnyKeyboard(System.Func<Keyboard, bool> predicate)
        {
            if (Keyboard.current != null && predicate(Keyboard.current))
                return true;

            for (var i = 0; i < InputSystem.devices.Count; i++)
            {
                if (InputSystem.devices[i] is not Keyboard kb)
                    continue;
                if (kb != Keyboard.current && predicate(kb))
                    return true;
            }

            return false;
        }

        private static Gamepad GetGamepad()
        {
            var gp = Gamepad.current;
            if (gp == null && Gamepad.all.Count > 0)
                gp = Gamepad.all[0];
            return gp;
        }

        private static bool FallbackKeyboardPressed()
        {
            return AnyKeyboard(kb =>
                kb[Key.X].wasPressedThisFrame
                || kb[Key.B].wasPressedThisFrame);
        }

        private static bool FallbackKeyboardReleased()
        {
            return AnyKeyboard(kb =>
                kb[Key.X].wasReleasedThisFrame
                || kb[Key.B].wasReleasedThisFrame);
        }

        private static bool FallbackKeyboardHeld()
        {
            return AnyKeyboard(kb =>
                kb[Key.X].isPressed
                || kb[Key.B].isPressed);
        }

        private static bool FallbackGamepadPressed()
        {
            var gp = GetGamepad();
            return gp != null && (gp.buttonWest.wasPressedThisFrame
                                    || gp.buttonNorth.wasPressedThisFrame
                                    || gp.buttonEast.wasPressedThisFrame);
        }

        private static bool FallbackGamepadReleased()
        {
            var gp = GetGamepad();
            return gp != null && (gp.buttonWest.wasReleasedThisFrame
                                  || gp.buttonNorth.wasReleasedThisFrame
                                  || gp.buttonEast.wasReleasedThisFrame);
        }

        private static bool FallbackGamepadHeld()
        {
            var gp = GetGamepad();
            return gp != null && (gp.buttonWest.isPressed || gp.buttonNorth.isPressed || gp.buttonEast.isPressed);
        }
    }
}
