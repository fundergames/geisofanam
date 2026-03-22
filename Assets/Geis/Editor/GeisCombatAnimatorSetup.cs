using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace Geis.Editor
{
    /// <summary>
    /// Adds Attack_1 parameter and state to AC_Polygon_Masculine_Geis for Phase 1 combat.
    /// Run via menu: Tools/Geis/Add Attack to AC_Polygon_Masculine_Geis
    /// Requires Option B: Polygon character with retargeted or existing attack clips.
    /// </summary>
    public static class GeisCombatAnimatorSetup
    {
        private const string CONTROLLER_PATH = "Assets/Geis/Animations/AC_Polygon_Masculine_Geis.controller";
        private const string ROGUEDEAL_ANIM = "Assets/RogueDeal/Combat/Animations";
        private const string POLYGON_ANIM = "Assets/Synty/AnimationBaseLocomotion";

        [MenuItem("Tools/Geis/Add Attack_1 to AC_Polygon_Masculine_Geis")]
        public static void AddAttackToGeisAnimator()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
            if (controller == null)
            {
                Debug.LogError($"[GeisCombatAnimatorSetup] Controller not found at {CONTROLLER_PATH}");
                return;
            }

            AddParamIfMissing(controller, "Attack_1", AnimatorControllerParameterType.Trigger);

            var root = controller.layers[0].stateMachine;

            AnimationClip attack1 = LoadClip($"{ROGUEDEAL_ANIM}/Attack_1.anim")
                ?? LoadClipByGuid("5045d3b5bb344054db9d225cf48eea78")
                ?? LoadClipByGuid("5bf70b270fff4a24a8d5e4a0d118a872")
                ?? LoadClip($"{POLYGON_ANIM}/Samples/Animations/Polygon/Masculine/Combat/Melee_1H_Attack_Chop.anim")
                ?? GetFirstAttackClip();

            if (attack1 == null)
            {
                Debug.LogWarning("[GeisCombatAnimatorSetup] No attack clip found. Add one manually in the Animator.");
                attack1 = GetDefaultIdleClip();
            }

            AnimatorState idleState = FindState(root, "Idle_Standing") ?? FindState(root, "Idle") ?? root.defaultState;
            if (idleState == null) idleState = GetFirstState(root);

            AnimatorState att1State = FindState(root, "Attack_1");
            if (att1State == null)
            {
                att1State = root.AddState("Attack_1", new Vector3(450, 0, 0));
                att1State.motion = attack1;
                att1State.speed = 1f;

                var t1 = att1State.AddTransition(idleState);
                t1.hasExitTime = true;
                t1.exitTime = 0.9f;
                t1.duration = 0.1f;

                var any1 = root.AddAnyStateTransition(att1State);
                any1.AddCondition(AnimatorConditionMode.If, 0, "Attack_1");
                any1.duration = 0.05f;

                Debug.Log("[GeisCombatAnimatorSetup] Added Attack_1 parameter and state. Light Attack (LMB/E) will now play the attack.");
            }
            else
            {
                Debug.Log("[GeisCombatAnimatorSetup] Attack_1 state already exists.");
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static void AddParamIfMissing(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in ctrl.parameters)
                if (p.name == name) return;
            ctrl.AddParameter(name, type);
        }

        private static AnimatorState FindState(AnimatorStateMachine sm, string name)
        {
            foreach (var s in sm.states)
                if (s.state.name == name) return s.state;
            foreach (var sub in sm.stateMachines)
                if (sub.stateMachine != null)
                {
                    var found = FindState(sub.stateMachine, name);
                    if (found != null) return found;
                }
            return null;
        }

        private static AnimatorState GetFirstState(AnimatorStateMachine sm)
        {
            foreach (var s in sm.states)
                return s.state;
            return null;
        }

        private static AnimationClip LoadClip(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static AnimationClip LoadClipByGuid(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static AnimationClip GetFirstAttackClip()
        {
            var guids = AssetDatabase.FindAssets("t:AnimationClip Attack");
            foreach (var g in guids)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g));
                if (clip != null && (clip.name.IndexOf("Attack", System.StringComparison.OrdinalIgnoreCase) >= 0 || clip.name.IndexOf("Melee", System.StringComparison.OrdinalIgnoreCase) >= 0))
                    return clip;
            }
            return null;
        }

        private static AnimationClip GetDefaultIdleClip()
        {
            var guids = AssetDatabase.FindAssets("t:AnimationClip Idle");
            foreach (var g in guids)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g));
                if (clip != null && clip.name.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return clip;
            }
            return null;
        }
    }
}
