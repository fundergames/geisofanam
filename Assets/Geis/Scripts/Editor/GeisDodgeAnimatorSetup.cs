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
    }
}
#endif
