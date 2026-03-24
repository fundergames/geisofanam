using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using RogueDeal.Player;

namespace RogueDeal.Editor
{
    /// <summary>
    /// Creates an Animator Controller for TestPlayer1 and combat prefabs using KayKit Rig_Medium animations.
    /// Compatible with PolygonCombatController / CombatExecutor, CombatAnimationController, and GeisPlayerAnimationController.
    /// </summary>
    public static class KayKitRigMediumControllerBuilder
    {
        private const string RIG_MEDIUM_BASE = "Assets/KayKit/Characters/Animations/Animations/Rig_Medium";
        private const string OUTPUT_PATH = "Assets/RogueDeal/Combat/Animations/KayKit_RigMedium_Controller.controller";

        [MenuItem("Tools/Combat Setup/Build KayKit RigMedium Controller")]
        public static void BuildController()
        {
            AnimationClip idle = LoadClip($"{RIG_MEDIUM_BASE}/General/Idle_A.anim");
            AnimationClip walk = LoadClip($"{RIG_MEDIUM_BASE}/Movement Basic/Walking_A.anim");
            AnimationClip run = LoadClip($"{RIG_MEDIUM_BASE}/Movement Basic/Running_A.anim");
            AnimationClip attack1 = LoadClip($"{RIG_MEDIUM_BASE}/Combat Melee/Melee_1H_Attack_Chop.anim");
            AnimationClip attack2 = LoadClip($"{RIG_MEDIUM_BASE}/Combat Melee/Melee_1H_Attack_Slice_Horizontal.anim");
            AnimationClip attack3 = LoadClip($"{RIG_MEDIUM_BASE}/Combat Melee/Melee_2H_Attack_Chop.anim");
            AnimationClip dodge = LoadClip($"{RIG_MEDIUM_BASE}/Movement Advanced/Dodge_Forward.anim");
            AnimationClip hit = LoadClip($"{RIG_MEDIUM_BASE}/General/Hit_A.anim");
            AnimationClip block = LoadClip($"{RIG_MEDIUM_BASE}/Combat Melee/Melee_Block.anim");
            AnimationClip death = LoadClip($"{RIG_MEDIUM_BASE}/General/Death_A.anim");
            AnimationClip spawn = LoadClip($"{RIG_MEDIUM_BASE}/General/Spawn_Ground.anim");

            if (idle == null || walk == null || run == null)
            {
                Debug.LogError("[KayKitRigMediumControllerBuilder] Failed to load required animations. Check KayKit Rig_Medium folder exists.");
                return;
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(OUTPUT_PATH);

            // Parameters expected by combat system
            AddParamIfMissing(controller, "Speed", AnimatorControllerParameterType.Float);
            AddParamIfMissing(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
            AddParamIfMissing(controller, "IsRunning", AnimatorControllerParameterType.Bool);
            AddParamIfMissing(controller, "Run", AnimatorControllerParameterType.Bool);
            AddParamIfMissing(controller, "TakeAction", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "ActionIndex", AnimatorControllerParameterType.Int);
            AddParamIfMissing(controller, "IsAction", AnimatorControllerParameterType.Bool);
            AddParamIfMissing(controller, "Attack_1", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "Attack_2", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "Attack_3", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "TakeDamage", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "Dodge", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "Block", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "Die", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "Death", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "Spawn", AnimatorControllerParameterType.Trigger);
            AddParamIfMissing(controller, "Move", AnimatorControllerParameterType.Trigger);

            var root = controller.layers[0].stateMachine;
            root.entryPosition = new Vector3(50, 0, 0);
            root.exitPosition = new Vector3(50, -60, 0);
            root.anyStatePosition = new Vector3(50, 120, 0);

            // Idle (default)
            var idleState = root.AddState("Idle", new Vector3(50, 0, 0));
            idleState.motion = idle;
            root.defaultState = idleState;

            // Locomotion blend tree (Speed: 0=idle, 0.5=walk, 1=run)
            BlendTree locomotionBlend = new BlendTree();
            locomotionBlend.blendParameter = "Speed";
            locomotionBlend.blendType = BlendTreeType.Simple1D;
            locomotionBlend.AddChild(idle, 0f);
            locomotionBlend.AddChild(walk, 0.5f);
            locomotionBlend.AddChild(run, 1f);
            locomotionBlend.name = "Locomotion";
            AssetDatabase.AddObjectToAsset(locomotionBlend, controller);
            var locomotionState = root.AddState("Locomotion", new Vector3(50, 20, 0));
            locomotionState.motion = locomotionBlend;

            // Transitions: Idle <-> Locomotion based on Speed
            var idleToLocomotion = idleState.AddTransition(locomotionState);
            idleToLocomotion.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");
            idleToLocomotion.hasExitTime = false;
            idleToLocomotion.duration = 0.1f;

            var locomotionToIdle = locomotionState.AddTransition(idleState);
            locomotionToIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");
            locomotionToIdle.hasExitTime = false;
            locomotionToIdle.duration = 0.1f;

            // Dodge (forward)
            if (dodge != null)
            {
                var dodgeState = root.AddState("Dodge", new Vector3(50, 40, 0));
                dodgeState.motion = dodge;
                var dodgeTrans = dodgeState.AddTransition(idleState);
                dodgeTrans.hasExitTime = true;
                dodgeTrans.exitTime = 0.9f;
                dodgeTrans.duration = 0.1f;

                var anyToDodge = root.AddAnyStateTransition(dodgeState);
                anyToDodge.AddCondition(AnimatorConditionMode.If, 0, "Dodge");
                anyToDodge.duration = 0.05f;
            }

            // Attack states (Attack_1, Attack_2, Attack_3)
            if (attack1 != null)
            {
                var att1 = root.AddState("Attack_1", new Vector3(250, 0, 0));
                att1.motion = attack1;
                var t1 = att1.AddTransition(idleState);
                t1.hasExitTime = true;
                t1.exitTime = 0.9f;
                t1.duration = 0.1f;
                var any1 = root.AddAnyStateTransition(att1);
                any1.AddCondition(AnimatorConditionMode.If, 0, "Attack_1");
                any1.duration = 0.05f;
            }
            if (attack2 != null)
            {
                var att2 = root.AddState("Attack_2", new Vector3(250, 20, 0));
                att2.motion = attack2;
                var t2 = att2.AddTransition(idleState);
                t2.hasExitTime = true;
                t2.exitTime = 0.9f;
                t2.duration = 0.1f;
                var any2 = root.AddAnyStateTransition(att2);
                any2.AddCondition(AnimatorConditionMode.If, 0, "Attack_2");
                any2.duration = 0.05f;
            }
            if (attack3 != null)
            {
                var att3 = root.AddState("Attack_3", new Vector3(250, 40, 0));
                att3.motion = attack3;
                var t3 = att3.AddTransition(idleState);
                t3.hasExitTime = true;
                t3.exitTime = 0.9f;
                t3.duration = 0.1f;
                var any3 = root.AddAnyStateTransition(att3);
                any3.AddCondition(AnimatorConditionMode.If, 0, "Attack_3");
                any3.duration = 0.05f;
            }

            // TakeAction (ActionIndex-based) - use Attack_1 by default
            if (attack1 != null)
            {
                var takeActionState = root.AddState("TakeAction", new Vector3(250, 60, 0));
                takeActionState.motion = attack1;
                var t = takeActionState.AddTransition(idleState);
                t.hasExitTime = true;
                t.exitTime = 0.9f;
                t.duration = 0.1f;
                var anyTake = root.AddAnyStateTransition(takeActionState);
                anyTake.AddCondition(AnimatorConditionMode.If, 0, "TakeAction");
                anyTake.duration = 0.05f;
            }

            // TakeDamage (hit reaction)
            if (hit != null)
            {
                var hitState = root.AddState("TakeDamage", new Vector3(250, 80, 0));
                hitState.motion = hit;
                var t = hitState.AddTransition(idleState);
                t.hasExitTime = true;
                t.exitTime = 0.9f;
                t.duration = 0.1f;
                var anyHit = root.AddAnyStateTransition(hitState);
                anyHit.AddCondition(AnimatorConditionMode.If, 0, "TakeDamage");
                anyHit.duration = 0.05f;
            }

            // Dodge
            if (dodge != null)
            {
                var dodgeState = root.AddState("Dodge", new Vector3(250, 100, 0));
                dodgeState.motion = dodge;
                var t = dodgeState.AddTransition(idleState);
                t.hasExitTime = true;
                t.exitTime = 0.9f;
                t.duration = 0.1f;
                var anyDodge = root.AddAnyStateTransition(dodgeState);
                anyDodge.AddCondition(AnimatorConditionMode.If, 0, "Dodge");
                anyDodge.duration = 0.05f;
            }

            // Block
            if (block != null)
            {
                var blockState = root.AddState("Block", new Vector3(250, 120, 0));
                blockState.motion = block;
                var t = blockState.AddTransition(idleState);
                t.hasExitTime = true;
                t.exitTime = 0.5f;
                t.duration = 0.1f;
                var anyBlock = root.AddAnyStateTransition(blockState);
                anyBlock.AddCondition(AnimatorConditionMode.If, 0, "Block");
                anyBlock.duration = 0.05f;
            }

            // Death
            if (death != null)
            {
                var deathState = root.AddState("Death", new Vector3(450, 0, 0));
                deathState.motion = death;
                var anyDeath = root.AddAnyStateTransition(deathState);
                anyDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
                var anyDeath2 = root.AddAnyStateTransition(deathState);
                anyDeath2.AddCondition(AnimatorConditionMode.If, 0, "Death");
            }

            // Spawn
            if (spawn != null)
            {
                var spawnState = root.AddState("Spawn", new Vector3(450, 20, 0));
                spawnState.motion = spawn;
                var t = spawnState.AddTransition(idleState);
                t.hasExitTime = true;
                t.exitTime = 0.9f;
                t.duration = 0.1f;
                var anySpawn = root.AddAnyStateTransition(spawnState);
                anySpawn.AddCondition(AnimatorConditionMode.If, 0, "Spawn");
                anySpawn.duration = 0.05f;
            }

            // Move (used by some combat flows - map to run animation)
            var moveState = root.AddState("Move", new Vector3(50, 60, 0));
            moveState.motion = run != null ? run : idle;
            var moveTrans = moveState.AddTransition(idleState);
            moveTrans.hasExitTime = true;
            moveTrans.exitTime = 0.9f;
            moveTrans.duration = 0.1f;
            var anyMove = root.AddAnyStateTransition(moveState);
            anyMove.AddCondition(AnimatorConditionMode.If, 0, "Move");
            anyMove.duration = 0.05f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[KayKitRigMediumControllerBuilder] Created controller at {OUTPUT_PATH}");
        }

        [MenuItem("Tools/Combat Setup/Apply KayKit RigMedium Controller to TestPlayer1")]
        public static void ApplyToTestPlayer1()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OUTPUT_PATH);
            if (controller == null)
            {
                Debug.LogWarning("[KayKitRigMediumControllerBuilder] Controller not found. Run 'Build KayKit RigMedium Controller' first.");
                return;
            }

            string prefabPath = "Assets/RogueDeal/Combat/Prefabs/TestPlayer1.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[KayKitRigMediumControllerBuilder] TestPlayer1 prefab not found at {prefabPath}");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            bool changed = false;

            foreach (var animator in prefabRoot.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController != null)
                {
                    var currentName = animator.runtimeAnimatorController.name;
                    if (currentName.Contains("BaseBattle") || currentName.Contains("CS_L") || currentName.Contains("CS_R"))
                    {
                        animator.runtimeAnimatorController = controller;
                        changed = true;
                        Debug.Log($"[KayKitRigMediumControllerBuilder] Applied KayKit controller to {animator.gameObject.name}");
                    }
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);

            if (changed)
            {
                Debug.Log("[KayKitRigMediumControllerBuilder] TestPlayer1 prefab updated.");
            }
        }

        [MenuItem("Tools/Combat Setup/Update AnimatorData Assets to KayKit RigMedium")]
        public static void UpdateAnimatorDataAssets()
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(OUTPUT_PATH);
            if (controller == null)
            {
                Debug.LogWarning("[KayKitRigMediumControllerBuilder] Controller not found. Run 'Build KayKit RigMedium Controller' first.");
                return;
            }

            string[] animatorDataPaths = AssetDatabase.FindAssets("t:ClassAnimatorData", new[] { "Assets/RogueDeal" });
            int updated = 0;
            foreach (string guid in animatorDataPaths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var animatorData = AssetDatabase.LoadAssetAtPath<ClassAnimatorData>(path);
                if (animatorData != null && animatorData.battleAnimator != controller)
                {
                    animatorData.battleAnimator = controller;
                    EditorUtility.SetDirty(animatorData);
                    updated++;
                    Debug.Log($"[KayKitRigMediumControllerBuilder] Updated {path}");
                }
            }
            if (updated > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[KayKitRigMediumControllerBuilder] Updated {updated} AnimatorData asset(s).");
            }
        }

        private static AnimationClip LoadClip(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static void AddParamIfMissing(AnimatorController c, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in c.parameters)
            {
                if (p.name == name) return;
            }
            c.AddParameter(name, type);
        }
    }
}
