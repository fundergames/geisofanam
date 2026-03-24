using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace RogueDeal.Editor
{
    /// <summary>
    /// Adds action parameters and states to AC_Polygon_Combat for sword/bow attacks.
    /// Run via menu: Tools/Combat Setup/Add Actions to AC_Polygon_Combat
    /// </summary>
    public static class PolygonCombatControllerSetup
    {
        private const string CONTROLLER_PATH = "Assets/Geis/Combat/Animations/ThirdPerson_Controller.controller";
        private const string POLYGON_ANIM_BASE = "Assets/Synty/AnimationBaseLocomotion";
        private const string THIRDPERSON_ANIM = "Assets/Geis/Combat/Animations";

        [MenuItem("Tools/Combat Setup/Add Actions to AC_Polygon_Combat")]
        public static void AddActionsToPolygonCombat()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
            if (controller == null)
            {
                Debug.LogError($"[PolygonCombatControllerSetup] Controller not found at {CONTROLLER_PATH}");
                return;
            }

            controller.name = "AC_Polygon_Combat";

            AddParamIfMissing(controller, "TakeAction", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "ActionIndex", AnimatorControllerParameterType.Int);
            AddParamIfMissing(controller, "IsAction", AnimatorControllerParameterType.Bool);
            AddParamIfMissing(controller, "Attack_1", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "Attack_2", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "Attack_3", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "Dodge", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "Speed", AnimatorControllerParameterType.Float);

            var root = controller.layers[0].stateMachine;

            AnimationClip attack1 = LoadClip($"{THIRDPERSON_ANIM}/Attack_1.anim")
                ?? LoadClipByGuid("5bf70b270fff4a24a8d5e4a0d118a872")
                ?? LoadClip($"{THIRDPERSON_ANIM}/Melee_1H_Attack_Chop.anim");
            AnimationClip attack2 = LoadClip($"{THIRDPERSON_ANIM}/Attack_2.anim")
                ?? LoadClipByGuid("bc8e528fde3a8c74ebc9f3e02470a5f7")
                ?? LoadClip($"{THIRDPERSON_ANIM}/Melee_1H_Attack_Slice_Horizontal.anim");
            AnimationClip attack3 = LoadClip($"{THIRDPERSON_ANIM}/Attack_3.anim")
                ?? LoadClip($"{THIRDPERSON_ANIM}/Melee_2H_Attack_Chop.anim");
            AnimationClip dodgeClip = LoadClip($"{THIRDPERSON_ANIM}/Dash_Forward.anim")
                ?? LoadClipByGuid("68531d144e80fec44b79f9a9279c7cbc")
                ?? LoadClip($"{POLYGON_ANIM_BASE}/Samples/Animations/Polygon/Masculine/Movement/Dash/AC_Dash_Forward.anim");

            if (attack1 == null) attack1 = GetDefaultClip();
            if (attack2 == null) attack2 = attack1;
            if (attack3 == null) attack3 = attack1;
            if (dodgeClip == null) dodgeClip = GetDefaultClip();

            AnimatorState idleState = FindState(root, "Idle_Standing") ?? FindState(root, "Idle") ?? root.defaultState;
            if (idleState == null) idleState = GetFirstState(root);

            AnimatorState att1State = FindState(root, "Attack_1");
            if (att1State == null)
            {
                att1State = root.AddState("Attack_1", new Vector3(400, 0, 0));
                att1State.motion = attack1;
                var t1 = att1State.AddTransition(idleState);
                t1.hasExitTime = true;
                t1.exitTime = 0.9f;
                t1.duration = 0.1f;

                var any1 = root.AddAnyStateTransition(att1State);
                any1.AddCondition(AnimatorConditionMode.If, 0, "Attack_1");
                any1.duration = 0.05f;
            }

            AnimatorState att2State = FindState(root, "Attack_2");
            if (att2State == null)
            {
                att2State = root.AddState("Attack_2", new Vector3(400, 30, 0));
                att2State.motion = attack2;
                var t2 = att2State.AddTransition(idleState);
                t2.hasExitTime = true;
                t2.exitTime = 0.9f;
                t2.duration = 0.1f;

                var any2 = root.AddAnyStateTransition(att2State);
                any2.AddCondition(AnimatorConditionMode.If, 0, "Attack_2");
                any2.duration = 0.05f;
            }

            AnimatorState att3State = FindState(root, "Attack_3");
            if (att3State == null)
            {
                att3State = root.AddState("Attack_3", new Vector3(400, 60, 0));
                att3State.motion = attack3;
                var t3 = att3State.AddTransition(idleState);
                t3.hasExitTime = true;
                t3.exitTime = 0.9f;
                t3.duration = 0.1f;

                var any3 = root.AddAnyStateTransition(att3State);
                any3.AddCondition(AnimatorConditionMode.If, 0, "Attack_3");
                any3.duration = 0.05f;
            }

            AnimatorState dodgeState = FindState(root, "Dodge");
            if (dodgeState == null && dodgeClip != null)
            {
                dodgeState = root.AddState("Dodge", new Vector3(400, 90, 0));
                dodgeState.motion = dodgeClip;
                var td = dodgeState.AddTransition(idleState);
                td.hasExitTime = true;
                td.exitTime = 0.9f;
                td.duration = 0.1f;

                var anyDodge = root.AddAnyStateTransition(dodgeState);
                anyDodge.AddCondition(AnimatorConditionMode.If, 0, "Dodge");
                anyDodge.duration = 0.05f;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[PolygonCombatControllerSetup] Added action parameters and states to AC_Polygon_Combat. Assign Attack_1/2/3 and Dodge animations in the Animator if needed.");
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

        private static AnimationClip GetDefaultClip()
        {
            var clips = AssetDatabase.FindAssets("t:AnimationClip Idle");
            foreach (var g in clips)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g));
                if (clip != null && clip.name.Contains("Idle")) return clip;
            }
            return null;
        }
    }
}
