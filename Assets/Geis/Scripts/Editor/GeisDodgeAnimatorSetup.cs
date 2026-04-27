/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Geis.EditorTools
{
    /// <summary>
    /// Adds Int DodgeDirection, Trigger Dodge, and four dodge motion states with Any-State transitions
    /// to <see cref="AC_Polygon_Masculine_Geis"/>. The controller keeps those states inside a <c>Dodge</c>
    /// sub-state machine (Entry routes by <c>DodgeDirection</c>; exit returns to <c>Idle_Standing</c>).
    /// Clips are loaded from <c>Assets/Geis/Animations/Dodge/</c>
    /// (copies of Grruzam root-motion FBXs) so you can tune import/clip settings without editing the pack.
    /// </summary>
    public static class GeisDodgeAnimatorSetup
    {
        private const string ControllerPath = "Assets/Geis/Animations/AC_Polygon_Masculine_Geis.controller";
        private const string IdleReturnStateName = "Idle_Standing";

        private static readonly string[] DodgeFbxPaths =
        {
            "Assets/Geis/Animations/Dodge/Anim_Knight@Dodge_Front_Root.FBX",
            "Assets/Geis/Animations/Dodge/Anim_Knight@Dodge_Back_Rolling_Root.FBX",
            "Assets/Geis/Animations/Dodge/Anim_Knight@Dodge_Left_Root.FBX",
            "Assets/Geis/Animations/Dodge/Anim_Knight@Dodge_Right_Root.FBX",
        };

        private static readonly string[] StateNames = { "Dodge_Front", "Dodge_Back", "Dodge_Left", "Dodge_Right" };

        private const string ForwardRollStateName = "Dodge_Forward_Roll";
        private const int ForwardRollDirection = 4;
        private const string ForwardRollGeneratedClipPath = "Assets/Geis/Animations/Dodge/Dodge_Forward_Roll_Generated.anim";

        [MenuItem("Geis/Animator/Setup Dodge Rolls (AC_Polygon_Masculine_Geis)")]
        public static void SetupDodgeOnGeisAnimator()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError($"[GeisDodgeAnimatorSetup] Missing controller at {ControllerPath}");
                return;
            }

            AddParameterIfMissing(controller, "DodgeDirection", AnimatorControllerParameterType.Int);
            AddParameterIfMissing(controller, "Dodge", AnimatorControllerParameterType.Trigger);

            var root = controller.layers[0].stateMachine;
            var idleState = FindStateRecursive(root, IdleReturnStateName);
            if (idleState == null)
            {
                Debug.LogError($"[GeisDodgeAnimatorSetup] Could not find state '{IdleReturnStateName}' to return to after dodge.");
                return;
            }

            for (int i = 0; i < StateNames.Length; i++)
            {
                if (FindStateRecursive(root, StateNames[i]) != null)
                {
                    Debug.Log($"[GeisDodgeAnimatorSetup] State '{StateNames[i]}' already exists — skipping.");
                    continue;
                }

                var clip = LoadFirstAnimationClip(DodgeFbxPaths[i]);
                if (clip == null)
                {
                    Debug.LogError($"[GeisDodgeAnimatorSetup] No AnimationClip in {DodgeFbxPaths[i]}");
                    continue;
                }

                var dodgeState = root.AddState(StateNames[i]);
                dodgeState.motion = clip;

                var toIdle = dodgeState.AddTransition(idleState);
                toIdle.hasExitTime = true;
                toIdle.exitTime = 0.92f;
                toIdle.duration = 0.12f;
                toIdle.hasFixedDuration = true;

                var any = root.AddAnyStateTransition(dodgeState);
                any.AddCondition(AnimatorConditionMode.If, 0, "Dodge");
                any.AddCondition(AnimatorConditionMode.Equals, i, "DodgeDirection");
                any.duration = 0.05f;
                any.hasExitTime = false;
                any.hasFixedDuration = true;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[GeisDodgeAnimatorSetup] Dodge parameters and states added. Save the scene if needed.");
        }

        private static void AddParameterIfMissing(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == name)
                    return;
            }

            controller.AddParameter(name, type);
        }

        private static AnimatorState FindStateRecursive(AnimatorStateMachine sm, string stateName)
        {
            foreach (var child in sm.states)
            {
                if (child.state.name == stateName)
                    return child.state;
            }

            foreach (var sub in sm.stateMachines)
            {
                var found = FindStateRecursive(sub.stateMachine, stateName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static AnimationClip LoadFirstAnimationClip(string assetPath)
        {
            var objs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var o in objs)
            {
                if (o is AnimationClip clip && !clip.name.Contains("__preview"))
                    return clip;
            }

            return null;
        }

        /// <summary>
        /// Bakes a reversed copy of <c>Dodge_Back</c>'s clip as a .anim asset and wires a new <c>Dodge_Forward_Roll</c>
        /// state into the animator, triggered when DodgeDirection == 4. Unity blocks <c>Animator.speed = -1</c> at
        /// runtime unless the recorder is enabled, so we pre-bake the reversed clip and just Play() it forward.
        /// </summary>
        [MenuItem("Geis/Animator/Setup Forward Roll (Reverse-baked Dodge_Back)")]
        public static void SetupForwardRoll()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError($"[GeisDodgeAnimatorSetup] Missing controller at {ControllerPath}");
                return;
            }

            var root = controller.layers[0].stateMachine;
            var backState = FindStateRecursive(root, "Dodge_Back");
            AnimationClip sourceClip = backState != null ? backState.motion as AnimationClip : null;
            if (sourceClip == null)
            {
                // Fall back to the FBX directly if the state isn't wired yet.
                sourceClip = LoadFirstAnimationClip(DodgeFbxPaths[1]);
            }

            if (sourceClip == null)
            {
                Debug.LogError(
                    "[GeisDodgeAnimatorSetup] Could not locate source Dodge_Back clip. Run 'Setup Dodge Rolls' first.");
                return;
            }

            var reversedClip = BuildReversedClip(sourceClip);
            reversedClip.name = "Dodge_Forward_Roll_Generated";

            // Overwrite any previously-generated asset so re-running the menu stays idempotent.
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(ForwardRollGeneratedClipPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(reversedClip, existing);
                reversedClip = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(reversedClip, ForwardRollGeneratedClipPath);
            }
            AssetDatabase.SaveAssets();
            reversedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ForwardRollGeneratedClipPath);

            AddParameterIfMissing(controller, "DodgeDirection", AnimatorControllerParameterType.Int);
            AddParameterIfMissing(controller, "Dodge", AnimatorControllerParameterType.Trigger);

            var idleState = FindStateRecursive(root, IdleReturnStateName);
            if (idleState == null)
            {
                Debug.LogError($"[GeisDodgeAnimatorSetup] Could not find state '{IdleReturnStateName}'.");
                return;
            }

            var forwardRoll = FindStateRecursive(root, ForwardRollStateName);
            if (forwardRoll == null)
            {
                forwardRoll = root.AddState(ForwardRollStateName);
            }
            forwardRoll.motion = reversedClip;

            // Idle return transition on clip completion (match the other dodge leaves).
            bool hasIdleReturn = false;
            foreach (var t in forwardRoll.transitions)
            {
                if (t.destinationState == idleState)
                {
                    hasIdleReturn = true;
                    break;
                }
            }
            if (!hasIdleReturn)
            {
                var toIdle = forwardRoll.AddTransition(idleState);
                toIdle.hasExitTime = true;
                toIdle.exitTime = 0.92f;
                toIdle.duration = 0.12f;
                toIdle.hasFixedDuration = true;
            }

            // AnyState transition: Dodge trigger + DodgeDirection == 4.
            bool hasAnyState = false;
            foreach (var t in root.anyStateTransitions)
            {
                if (t.destinationState == forwardRoll)
                {
                    hasAnyState = true;
                    break;
                }
            }
            if (!hasAnyState)
            {
                var any = root.AddAnyStateTransition(forwardRoll);
                any.AddCondition(AnimatorConditionMode.If, 0, "Dodge");
                any.AddCondition(AnimatorConditionMode.Equals, ForwardRollDirection, "DodgeDirection");
                any.duration = 0.05f;
                any.hasExitTime = false;
                any.hasFixedDuration = true;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[GeisDodgeAnimatorSetup] Forward roll set up. Clip: {ForwardRollGeneratedClipPath}, state: {ForwardRollStateName} (DodgeDirection == {ForwardRollDirection}).");
        }

        /// <summary>
        /// Produces a reversed <see cref="AnimationClip"/> by mirroring every float/quaternion/vector curve and every
        /// object-reference keyframe across the clip's length, negating tangents so tween shapes still match.
        /// Keeps the source clip's settings (loop, root-motion flags, etc.) in lock-step with the original.
        /// </summary>
        private static AnimationClip BuildReversedClip(AnimationClip source)
        {
            var reversed = new AnimationClip
            {
                frameRate = source.frameRate,
                legacy = source.legacy,
                wrapMode = source.wrapMode,
                localBounds = source.localBounds,
            };

            var settings = AnimationUtility.GetAnimationClipSettings(source);
            AnimationUtility.SetAnimationClipSettings(reversed, settings);

            float length = source.length;

            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                if (curve == null || curve.keys.Length == 0)
                    continue;

                var src = curve.keys;
                var rKeys = new Keyframe[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    var k = src[src.Length - 1 - i];
                    var nk = new Keyframe(length - k.time, k.value, -k.outTangent, -k.inTangent)
                    {
                        weightedMode = k.weightedMode,
                        inWeight = k.outWeight,
                        outWeight = k.inWeight,
                    };
                    rKeys[i] = nk;
                }
                AnimationUtility.SetEditorCurve(reversed, binding, new AnimationCurve(rKeys));
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(source, binding);
                if (keys == null || keys.Length == 0)
                    continue;

                var rKeys = new ObjectReferenceKeyframe[keys.Length];
                for (int i = 0; i < keys.Length; i++)
                {
                    var k = keys[keys.Length - 1 - i];
                    rKeys[i] = new ObjectReferenceKeyframe { time = length - k.time, value = k.value };
                }
                AnimationUtility.SetObjectReferenceCurve(reversed, binding, rKeys);
            }

            return reversed;
        }
    }
}
#endif
