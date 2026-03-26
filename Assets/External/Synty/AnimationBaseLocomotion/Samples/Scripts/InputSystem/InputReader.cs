// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// Sample scripts are included only as examples and are not intended as production-ready.

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Synty.AnimationBaseLocomotion.Samples.InputSystem
{
    public class InputReader : MonoBehaviour, Controls.IPlayerActions
    {
        public Vector2 _mouseDelta;
        public Vector2 _moveComposite;

        public float _movementInputDuration;
        public bool _movementInputDetected;

        private Controls _controls;

        public Action onAimActivated;
        public Action onAimDeactivated;

        public Action onCrouchActivated;
        public Action onCrouchDeactivated;

        public Action onJumpPerformed;

        public Action onLockOnToggled;

        public Action onSprintActivated;
        public Action onSprintDeactivated;

        public Action onWalkToggled;

        public Action onLightAttackPerformed;
        public Action onHeavyAttackPerformed;
        public Action onDodgePerformed;

        /// <inheritdoc cref="OnEnable" />
        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }

            if (Application.isFocused)
                _controls.Player.Enable();
            else
                _controls.Player.Disable();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_controls == null) return;

            if (hasFocus)
                _controls.Player.Enable();
            else
            {
                _controls.Player.Disable();
                _mouseDelta = Vector2.zero;
                _moveComposite = Vector2.zero;
                _movementInputDetected = false;
            }
        }

        /// <inheritdoc cref="OnDisable" />
        public void OnDisable()
        {
            _controls?.Player.Disable();
        }

        /// <summary>
        ///     Defines the action to perform when the OnLook callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnLook(InputAction.CallbackContext context)
        {
            _mouseDelta = context.ReadValue<Vector2>();
        }

        /// <summary>
        ///     Defines the action to perform when the OnMove callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnMove(InputAction.CallbackContext context)
        {
            _moveComposite = context.ReadValue<Vector2>();
            _movementInputDetected = _moveComposite.magnitude > 0;
        }

        /// <summary>
        ///     Defines the action to perform when the OnJump callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnJump(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            onJumpPerformed?.Invoke();
        }

        /// <summary>
        ///     Defines the action to perform when the OnToggleWalk callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnToggleWalk(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            onWalkToggled?.Invoke();
        }

        /// <summary>
        ///     Defines the action to perform when the OnSprint callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnSprint(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                onSprintActivated?.Invoke();
            }
            else if (context.canceled)
            {
                onSprintDeactivated?.Invoke();
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnCrouch callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                onCrouchActivated?.Invoke();
            }
            else if (context.canceled)
            {
                onCrouchDeactivated?.Invoke();
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnAim callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnAim(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                onAimActivated?.Invoke();
            }

            if (context.canceled)
            {
                onAimDeactivated?.Invoke();
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnLockOn callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnLockOn(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            onLockOnToggled?.Invoke();
            onSprintDeactivated?.Invoke();
        }

        /// <summary>
        ///     Defines the action to perform when the OnLightAttack callback is called.
        /// </summary>
        public void OnLightAttack(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            onLightAttackPerformed?.Invoke();
        }

        /// <summary>
        ///     Defines the action to perform when the OnHeavyAttack callback is called.
        /// </summary>
        public void OnHeavyAttack(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            onHeavyAttackPerformed?.Invoke();
        }

        /// <summary>
        ///     Defines the action to perform when the OnDodge callback is called.
        /// </summary>
        public void OnDodge(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            onDodgePerformed?.Invoke();
        }
    }
}
