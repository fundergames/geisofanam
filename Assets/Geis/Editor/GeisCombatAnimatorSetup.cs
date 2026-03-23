using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Geis.Combat;

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
        private const int COMBO_BLEND_TREE_SLOTS = 32;

        [MenuItem("Tools/Geis/Add Data-Driven Attack (ComboState blend tree)")]
        public static void AddDataDrivenAttack()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
            if (controller == null)
            {
                Debug.LogError($"[GeisCombatAnimatorSetup] Controller not found at {CONTROLLER_PATH}");
                return;
            }

            AddParamIfMissing(controller, "Attack", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "ComboState", AnimatorControllerParameterType.Int);
            AddParamIfMissing(controller, "ComboStateBlend", AnimatorControllerParameterType.Float);
            AddParamIfMissing(controller, "EquippedWeaponIndex", AnimatorControllerParameterType.Int);

            var root = controller.layers[0].stateMachine;

            AnimationClip defaultClip = LoadClip($"{ROGUEDEAL_ANIM}/Attack_1.anim")
                ?? LoadClipByGuid("5045d3b5bb344054db9d225cf48eea78")
                ?? LoadClipByGuid("5bf70b270fff4a24a8d5e4a0d118a872")
                ?? LoadClip($"{POLYGON_ANIM}/Samples/Animations/Polygon/Masculine/Combat/Melee_1H_Attack_Chop.anim")
                ?? GetFirstAttackClip()
                ?? GetDefaultIdleClip();

            if (defaultClip == null)
            {
                Debug.LogError("[GeisCombatAnimatorSetup] No default clip found. Add an attack or idle clip to the project.");
                return;
            }

            AnimatorState idleState = FindState(root, "Idle_Standing") ?? FindState(root, "Idle") ?? root.defaultState;
            if (idleState == null) idleState = GetFirstState(root);

            AnimatorState attackState = FindState(root, "Attack");
            if (attackState == null)
            {
                BlendTree comboBlend = new BlendTree();
                comboBlend.name = "Attack_ComboBlend";
                comboBlend.blendParameter = "ComboStateBlend";
                comboBlend.blendType = BlendTreeType.Simple1D;

                for (int i = 0; i < COMBO_BLEND_TREE_SLOTS; i++)
                    comboBlend.AddChild(defaultClip, (float)i);

                AssetDatabase.AddObjectToAsset(comboBlend, controller);

                attackState = root.AddState("Attack", new Vector3(450, 0, 0));
                attackState.motion = comboBlend;
                attackState.speed = 1f;

                var toIdle = attackState.AddTransition(idleState);
                toIdle.hasExitTime = true;
                toIdle.exitTime = 0.9f;
                toIdle.duration = 0.1f;

                var anyToAttack = root.AddAnyStateTransition(attackState);
                anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                anyToAttack.duration = 0.05f;

                Debug.Log("[GeisCombatAnimatorSetup] Added data-driven Attack state with 32-slot ComboState blend tree.");
            }
            else
            {
                Debug.Log("[GeisCombatAnimatorSetup] Attack state already exists.");
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Geis/Create Combo Placeholder Clips (for runtime override)")]
        public static void CreateComboPlaceholderClips()
        {
            const string placeholdersPath = "Assets/Geis/Resources";
            const string placeholderFolder = "ComboPlaceholders";

            if (!AssetDatabase.IsValidFolder("Assets/Geis"))
                AssetDatabase.CreateFolder("Assets", "Geis");
            if (!AssetDatabase.IsValidFolder("Assets/Geis/Resources"))
                AssetDatabase.CreateFolder("Assets/Geis", "Resources");
            if (!AssetDatabase.IsValidFolder($"{placeholdersPath}/{placeholderFolder}"))
                AssetDatabase.CreateFolder(placeholdersPath, placeholderFolder);

            AnimationClip referenceClip = LoadClip($"{ROGUEDEAL_ANIM}/Attack_1.anim")
                ?? LoadClip($"{POLYGON_ANIM}/Samples/Animations/Polygon/Masculine/Combat/Melee_1H_Attack_Chop.anim")
                ?? GetDefaultIdleClip();
            if (referenceClip == null)
            {
                Debug.LogError("[GeisCombatAnimatorSetup] No reference clip for placeholders.");
                return;
            }

            var placeholders = new AnimationClip[COMBO_BLEND_TREE_SLOTS];
            for (int i = 0; i < COMBO_BLEND_TREE_SLOTS; i++)
            {
                string clipPath = $"{placeholdersPath}/{placeholderFolder}/ComboPlaceholder_{i}.anim";
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (existing != null)
                {
                    placeholders[i] = existing;
                    continue;
                }
                AnimationClip clip = Object.Instantiate(referenceClip);
                clip.name = $"ComboPlaceholder_{i}";
                AssetDatabase.CreateAsset(clip, clipPath);
                placeholders[i] = clip;
            }

            var holderPath = $"{placeholdersPath}/GeisComboPlaceholders.asset";
            var holder = AssetDatabase.LoadAssetAtPath<GeisComboPlaceholders>(holderPath);
            if (holder == null)
            {
                holder = ScriptableObject.CreateInstance<GeisComboPlaceholders>();
                AssetDatabase.CreateAsset(holder, holderPath);
            }
            SerializedObject so = new SerializedObject(holder);
            so.FindProperty("placeholders").ClearArray();
            for (int i = 0; i < placeholders.Length; i++)
            {
                so.FindProperty("placeholders").InsertArrayElementAtIndex(i);
                so.FindProperty("placeholders").GetArrayElementAtIndex(i).objectReferenceValue = placeholders[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(holder);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GeisCombatAnimatorSetup] Created {COMBO_BLEND_TREE_SLOTS} placeholder clips and GeisComboPlaceholders. Run 'Rebuild blend tree with placeholders' next.");
        }

        [MenuItem("Tools/Geis/Rebuild Attack blend tree with placeholders")]
        public static void RebuildBlendTreeWithPlaceholders()
        {
            var placeholders = AssetDatabase.LoadAssetAtPath<GeisComboPlaceholders>("Assets/Geis/Resources/GeisComboPlaceholders.asset");
            if (placeholders == null || placeholders.GetPlaceholder(0) == null)
            {
                Debug.LogError("[GeisCombatAnimatorSetup] Run 'Create Combo Placeholder Clips' first.");
                return;
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
            if (controller == null)
            {
                Debug.LogError($"[GeisCombatAnimatorSetup] Controller not found at {CONTROLLER_PATH}");
                return;
            }

            var root = controller.layers[0].stateMachine;
            var attackState = FindState(root, "Attack");
            if (attackState == null || attackState.motion == null)
            {
                Debug.LogError("[GeisCombatAnimatorSetup] Attack state not found. Run 'Add Data-Driven Attack' first.");
                return;
            }

            var blendTree = attackState.motion as BlendTree;
            if (blendTree == null)
            {
                Debug.LogError("[GeisCombatAnimatorSetup] Attack motion is not a BlendTree.");
                return;
            }

            var so = new SerializedObject(blendTree);
            var childrenProp = so.FindProperty("m_Childs") ?? so.FindProperty("m_Children");
            if (childrenProp != null)
            {
                for (int i = 0; i < childrenProp.arraySize && i < COMBO_BLEND_TREE_SLOTS; i++)
                {
                    var child = childrenProp.GetArrayElementAtIndex(i);
                    var motionProp = child.FindPropertyRelative("motion") ?? child.FindPropertyRelative("m_Motion");
                    if (motionProp != null)
                    {
                        motionProp.objectReferenceValue = placeholders.GetPlaceholder(i);
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[GeisCombatAnimatorSetup] Blend tree now uses placeholders. Clips will be applied at runtime from GeisComboData - no Sync needed.");
        }

        [MenuItem("Tools/Geis/Fix Combo blend parameter for normalized 0-1 range")]
        public static void FixComboBlendParameter()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
            if (controller == null)
            {
                Debug.LogError($"[GeisCombatAnimatorSetup] Controller not found at {CONTROLLER_PATH}");
                return;
            }

            AddParamIfMissing(controller, "ComboStateBlend", AnimatorControllerParameterType.Float);

            var root = controller.layers[0].stateMachine;
            var attackState = FindState(root, "Attack");
            if (attackState == null || attackState.motion == null)
            {
                Debug.LogError("[GeisCombatAnimatorSetup] Attack state not found.");
                return;
            }

            var blendTree = attackState.motion as BlendTree;
            if (blendTree != null)
            {
                var so = new SerializedObject(blendTree);
                var paramProp = so.FindProperty("m_BlendParameter");
                if (paramProp != null)
                {
                    paramProp.stringValue = "ComboStateBlend";
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[GeisCombatAnimatorSetup] Set Attack_ComboBlend to use ComboStateBlend (Float, 0-1). Use Create Combo Placeholder Clips + Rebuild for runtime override.");
        }

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
