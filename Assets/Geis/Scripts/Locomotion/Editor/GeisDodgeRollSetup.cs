/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Geis.Locomotion.Editor
{
    /// <summary>
    /// Wires sidestep clips under the <c>Dodge</c> sub-state machine and roll clips under a sibling <c>Roll</c> sub-state machine.
    /// </summary>
    public static class GeisDodgeRollSetup
    {
        private const string ControllerPath = "Assets/Geis/Animations/AC_Polygon_Masculine_Geis.controller";
        private const string DodgeSubStateMachineName = "Dodge";
        private const string RollSubStateMachineName = "Roll";
        private const string DodgeDirectionParameter = "DodgeDirection";
        private const string DodgeTrigger = "Dodge";
        private const string RollTrigger = "Roll";

        private const float AnyStateTransitionDuration = 0.05f;
        private const float LeafExitTransitionDuration = 0.12f;
        private const float LeafExitNormalizedTime = 0.92f;

        private static readonly (string stateName, string assetPath)[] SidestepClips =
        {
            ("Dodge_Front", "Assets/Geis/Animations/Dodge/Dodge_Front_Root.anim"),
            ("Dodge_Back", "Assets/Geis/Animations/Dodge/Anim_Knight@Dodge_Back_Root 1.FBX"),
            ("Dodge_Left", "Assets/Geis/Animations/Dodge/Dodge_Left_Root.anim"),
            ("Dodge_Right", "Assets/Geis/Animations/Dodge/Dodge_Right_Root.anim"),
        };

        private static readonly (string stateName, string assetPath)[] RollClips =
        {
            ("Dodge_Forward_Roll", "Assets/Geis/Animations/Dodge/Dodge_Forward_Roll_Generated.anim"),
            ("Dodge_Back_Rolling", "Assets/Geis/Animations/Dodge/Dodge_Back_Rolling_Root.anim"),
            ("A_DodgeRoll_L_Sword", "Assets/Synty/AnimationSwordCombat/Animations/Polygon/Dodge/A_DodgeRoll_L_RootMotion_Sword.fbx"),
            ("A_DodgeRoll_R_Sword", "Assets/Synty/AnimationSwordCombat/Animations/Polygon/Dodge/A_DodgeRoll_R_RootMotion_Sword.fbx"),
        };

        [MenuItem("Geis/Animator/Setup Directional Dodge & Roll Clips")]
        public static void SetupDirectionalDodgeAndRollClips()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError($"[GeisDodgeRollSetup] Animator not found at {ControllerPath}");
                return;
            }

            EnsureTrigger(controller, DodgeTrigger);
            EnsureTrigger(controller, RollTrigger);
            EnsureIntParameter(controller, DodgeDirectionParameter);

            AnimatorStateMachine baseSm = controller.layers[0].stateMachine;

            AnimatorStateMachine dodgeSm = FindSubStateMachine(baseSm, DodgeSubStateMachineName);
            if (dodgeSm == null)
            {
                Debug.LogError($"[GeisDodgeRollSetup] Sub-state machine '{DodgeSubStateMachineName}' not found.");
                return;
            }

            int sidesteps = AssignClipsToExistingStates(dodgeSm, SidestepClips);
            RebuildDirectionalEntryTransitions(dodgeSm, SidestepClips);
            EnsureLeafExitTransitions(dodgeSm, SidestepClips);

            RemoveOrphanRollStatesFromBaseLayer(baseSm);

            AnimatorStateMachine rollSm = GetOrCreateSubStateMachine(baseSm, RollSubStateMachineName, new Vector3(710f, 200f, 0f));
            int rolls = AssignClipsToExistingOrNewStates(rollSm, RollClips);
            RebuildDirectionalEntryTransitions(rollSm, RollClips);
            EnsureLeafExitTransitions(rollSm, RollClips);
            EnsureSubStateMachineExitToLocomotion(baseSm, rollSm);

            FixLegacyInPlaceRollAnims();

            RemoveObsoleteForwardRollAnyStateTransition(baseSm);
            RebuildRollAnyStateTransitions(baseSm, rollSm);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[GeisDodgeRollSetup] Updated {sidesteps} sidestep states under '{DodgeSubStateMachineName}', " +
                $"{rolls} roll states under '{RollSubStateMachineName}' (entry/exit + Any-State Roll transitions).",
                controller);
        }

        private static void RemoveOrphanRollStatesFromBaseLayer(AnimatorStateMachine baseSm)
        {
            var rollNames = new HashSet<string>();
            foreach ((string stateName, _) in RollClips)
                rollNames.Add(stateName);

            for (int i = baseSm.states.Length - 1; i >= 0; i--)
            {
                if (rollNames.Contains(baseSm.states[i].state.name))
                    baseSm.RemoveState(baseSm.states[i].state);
            }
        }

        private static void RemoveObsoleteForwardRollAnyStateTransition(AnimatorStateMachine baseSm)
        {
            for (int i = baseSm.anyStateTransitions.Length - 1; i >= 0; i--)
            {
                AnimatorStateTransition transition = baseSm.anyStateTransitions[i];
                if (TargetsForwardRollOnBaseLayer(transition))
                    baseSm.RemoveAnyStateTransition(transition);
            }
        }

        private static bool TargetsForwardRollOnBaseLayer(AnimatorStateTransition transition)
        {
            if (transition.destinationState == null || transition.destinationState.name != "Dodge_Forward_Roll")
                return false;

            bool hasDodgeDir4 = false;
            foreach (AnimatorCondition condition in transition.conditions)
            {
                if (condition.parameter == DodgeDirectionParameter
                    && condition.mode == AnimatorConditionMode.Equals
                    && Mathf.Approximately(condition.threshold, 4f))
                {
                    hasDodgeDir4 = true;
                    break;
                }
            }

            return hasDodgeDir4;
        }

        private static void RebuildRollAnyStateTransitions(AnimatorStateMachine baseSm, AnimatorStateMachine rollSm)
        {
            for (int i = baseSm.anyStateTransitions.Length - 1; i >= 0; i--)
            {
                AnimatorStateTransition transition = baseSm.anyStateTransitions[i];
                if (TargetsRollStateMachine(transition, rollSm))
                    baseSm.RemoveAnyStateTransition(transition);
            }

            AddRollAnyStateTransition(baseSm, rollSm, frontRange: true);
            AddRollAnyStateTransition(baseSm, rollSm, directionEquals: 1);
            AddRollAnyStateTransition(baseSm, rollSm, directionEquals: 2);
            AddRollAnyStateTransition(baseSm, rollSm, directionEquals: 3);
        }

        private static bool TargetsRollStateMachine(AnimatorStateTransition transition, AnimatorStateMachine rollSm)
        {
            if (transition.destinationStateMachine != rollSm)
                return false;

            foreach (AnimatorCondition condition in transition.conditions)
            {
                if (condition.parameter == RollTrigger)
                    return true;
            }

            return false;
        }

        private static void AddRollAnyStateTransition(
            AnimatorStateMachine baseSm,
            AnimatorStateMachine rollSm,
            bool frontRange = false,
            int directionEquals = -1)
        {
            AnimatorStateTransition transition = baseSm.AddAnyStateTransition(rollSm);
            transition.AddCondition(AnimatorConditionMode.If, 0f, RollTrigger);

            if (frontRange)
            {
                transition.AddCondition(AnimatorConditionMode.Greater, -1f, DodgeDirectionParameter);
                transition.AddCondition(AnimatorConditionMode.Less, 1f, DodgeDirectionParameter);
            }
            else
            {
                transition.AddCondition(AnimatorConditionMode.Equals, directionEquals, DodgeDirectionParameter);
            }

            transition.duration = AnyStateTransitionDuration;
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.canTransitionToSelf = true;
        }

        private static void RebuildDirectionalEntryTransitions(
            AnimatorStateMachine sm,
            (string stateName, string assetPath)[] clips)
        {
            while (sm.entryTransitions.Length > 0)
                sm.RemoveEntryTransition(sm.entryTransitions[0]);

            AnimatorState front = FindState(sm, clips[0].stateName);
            AnimatorState back = FindState(sm, clips[1].stateName);
            AnimatorState left = FindState(sm, clips[2].stateName);
            AnimatorState right = FindState(sm, clips[3].stateName);

            if (front != null)
            {
                AnimatorTransition entry = sm.AddEntryTransition(front);
                entry.AddCondition(AnimatorConditionMode.Greater, -1f, DodgeDirectionParameter);
                entry.AddCondition(AnimatorConditionMode.Less, 1f, DodgeDirectionParameter);
            }

            if (back != null)
            {
                AnimatorTransition entry = sm.AddEntryTransition(back);
                entry.AddCondition(AnimatorConditionMode.Equals, 1f, DodgeDirectionParameter);
            }

            if (left != null)
            {
                AnimatorTransition entry = sm.AddEntryTransition(left);
                entry.AddCondition(AnimatorConditionMode.Equals, 2f, DodgeDirectionParameter);
            }

            if (right != null)
            {
                AnimatorTransition entry = sm.AddEntryTransition(right);
                entry.AddCondition(AnimatorConditionMode.Equals, 3f, DodgeDirectionParameter);
            }
        }

        private static void EnsureLeafExitTransitions(
            AnimatorStateMachine sm,
            (string stateName, string assetPath)[] clips)
        {
            foreach ((string stateName, _) in clips)
            {
                AnimatorState state = FindState(sm, stateName);
                if (state == null)
                    continue;

                EnsureLeafExitTransition(state);
            }
        }

        private static void EnsureLeafExitTransition(AnimatorState state)
        {
            foreach (AnimatorStateTransition existing in state.transitions)
            {
                if (existing.isExit)
                    return;
            }

            AnimatorStateTransition exit = state.AddExitTransition();
            exit.hasExitTime = true;
            exit.exitTime = LeafExitNormalizedTime;
            exit.duration = LeafExitTransitionDuration;
            exit.hasFixedDuration = true;
        }

        private static void EnsureSubStateMachineExitToLocomotion(AnimatorStateMachine baseSm, AnimatorStateMachine rollSm)
        {
            AnimatorState idleStanding = FindStateRecursive(baseSm, "Idle_Standing");
            if (idleStanding == null)
            {
                Debug.LogWarning("[GeisDodgeRollSetup] Idle_Standing not found; Roll SM exit not wired.");
                return;
            }

            foreach (AnimatorTransition transition in baseSm.GetStateMachineTransitions(rollSm))
            {
                if (transition.destinationState == idleStanding)
                    return;
            }

            baseSm.AddStateMachineTransition(rollSm, idleStanding);
        }

        private static AnimatorState FindStateRecursive(AnimatorStateMachine sm, string stateName)
        {
            foreach (ChildAnimatorState child in sm.states)
            {
                if (child.state.name == stateName)
                    return child.state;
            }

            foreach (ChildAnimatorStateMachine childSm in sm.stateMachines)
            {
                AnimatorState nested = FindStateRecursive(childSm.stateMachine, stateName);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static AnimatorStateMachine GetOrCreateSubStateMachine(
            AnimatorStateMachine baseSm,
            string name,
            Vector3 position)
        {
            AnimatorStateMachine existing = FindSubStateMachine(baseSm, name);
            if (existing != null)
                return existing;

            return baseSm.AddStateMachine(name, position);
        }

        private static AnimatorStateMachine FindSubStateMachine(AnimatorStateMachine root, string name)
        {
            foreach (ChildAnimatorStateMachine child in root.stateMachines)
            {
                if (child.stateMachine.name == name)
                    return child.stateMachine;
            }

            return null;
        }

        private static AnimatorState FindState(AnimatorStateMachine sm, string stateName)
        {
            foreach (ChildAnimatorState child in sm.states)
            {
                if (child.state.name == stateName)
                    return child.state;
            }

            return null;
        }

        private static int AssignClipsToExistingStates(
            AnimatorStateMachine sm,
            (string stateName, string assetPath)[] clips)
        {
            int count = 0;
            foreach ((string stateName, string assetPath) in clips)
            {
                AnimationClip clip = LoadClip(assetPath);
                if (clip == null)
                {
                    Debug.LogWarning($"[GeisDodgeRollSetup] Missing clip: {assetPath}");
                    continue;
                }

                if (AssignMotionInStateMachine(sm, stateName, clip))
                    count++;
            }

            return count;
        }

        private static int AssignClipsToExistingOrNewStates(
            AnimatorStateMachine sm,
            (string stateName, string assetPath)[] clips)
        {
            int count = 0;
            float y = 50f;
            foreach ((string stateName, string assetPath) in clips)
            {
                AnimationClip clip = LoadClip(assetPath);
                if (clip == null)
                {
                    Debug.LogWarning($"[GeisDodgeRollSetup] Missing clip: {assetPath}");
                    continue;
                }

                EnsureHumanoidRootMotion(clip, assetPath);

                AnimatorState state = FindState(sm, stateName);
                if (state == null)
                {
                    state = sm.AddState(stateName, new Vector3(50f, y, 0f));
                    y += 70f;
                }

                state.motion = clip;
                count++;
            }

            return count;
        }

        private static bool AssignMotionInStateMachine(AnimatorStateMachine sm, string stateName, AnimationClip clip)
        {
            AnimatorState state = FindState(sm, stateName);
            if (state == null)
                return false;

            state.motion = clip;
            return true;
        }

        private static AnimationClip LoadClip(string assetPath)
        {
            if (assetPath.EndsWith(".anim"))
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

            string preferredName = Path.GetFileNameWithoutExtension(assetPath);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            AnimationClip fallback = null;

            foreach (Object asset in assets)
            {
                if (asset is not AnimationClip clip || clip.name.StartsWith("__preview__"))
                    continue;

                if (clip.name == preferredName)
                    return clip;

                fallback ??= clip;
            }

            return fallback;
        }

        /// <summary>
        /// Synty in-place sword rolls bake XZ into the skeleton unless root motion is enabled on the clip import.
        /// </summary>
        private static void EnsureHumanoidRootMotion(AnimationClip clip, string assetPath)
        {
            if (clip == null)
                return;

            if (assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                EnsureFbxRootMotionImport(assetPath);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.keepOriginalPositionXZ)
                return;

            settings.keepOriginalPositionXZ = false;
            settings.loopBlendPositionXZ = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        private static void EnsureFbxRootMotionImport(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
                return;

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;

            bool changed = false;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                if (!clip.keepOriginalPositionXZ)
                    continue;

                clip.keepOriginalPositionXZ = false;
                changed = true;
            }

            if (!changed)
                return;

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void FixLegacyInPlaceRollAnims()
        {
            string[] legacyPaths =
            {
                "Assets/Geis/Animations/Dodge/A_DodgeRoll_L_Sword.anim",
                "Assets/Geis/Animations/Dodge/A_DodgeRoll_R_Sword.anim",
            };

            foreach (string path in legacyPaths)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null)
                    continue;

                EnsureHumanoidRootMotion(clip, path);
            }
        }

        private static void EnsureIntParameter(AnimatorController controller, string name)
        {
            foreach (AnimatorControllerParameter p in controller.parameters)
            {
                if (p.name == name && p.type == AnimatorControllerParameterType.Int)
                    return;
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Int);
        }

        private static void EnsureTrigger(AnimatorController controller, string name)
        {
            foreach (AnimatorControllerParameter p in controller.parameters)
            {
                if (p.name == name && p.type == AnimatorControllerParameterType.Trigger)
                    return;
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
        }
    }
}
#endif
