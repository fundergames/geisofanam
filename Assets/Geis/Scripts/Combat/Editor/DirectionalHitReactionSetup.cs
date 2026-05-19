/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Geis.Combat;
using Geis.Enemies;
using Geis.Locomotion;
using RogueDeal.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Geis.Combat.Editor
{
    /// <summary>
    /// Adds HitDirection + four hit-react states to the Geis Polygon player animator.
    /// </summary>
    public static class DirectionalHitReactionSetup
    {
        private const string ControllerPath = "Assets/Geis/Animations/AC_Polygon_Masculine_Geis.controller";
        private const string ReactionSetPath = "Assets/Geis/Combat/DirectionalHitReactionSet_PolygonPlayer.asset";
        private const string PlayerPrefabPath = "Assets/Geis/Combat/Prefabs/Player.prefab";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemies/P_Enemy_Phase1Humanoid.prefab";
        private const string HitReactionLayerName = "HitReaction";
        private const string HitReactionEmptyStateName = "HitReaction_Empty";
        private const string HitDirectionParameter = "HitDirection";
        private const string TakeDamageTrigger = "TakeDamage";

        private const float AnyStateTransitionDuration = 0.08f;
        private const float ExitTransitionDuration = 0.15f;
        private const float ExitTransitionNormalizedTime = 0.92f;

        private static readonly string[] PolygonHitReactFbxPaths =
        {
            "Assets/Synty/AnimationSwordCombat/Animations/Polygon/Hit/HitReact/A_Hit_F_React_Sword.fbx",
            "Assets/Synty/AnimationSwordCombat/Animations/Polygon/Hit/HitReact/A_Hit_B_React_Sword.fbx",
            "Assets/Synty/AnimationSwordCombat/Animations/Polygon/Hit/HitReact/A_Hit_L_React_Sword.fbx",
            "Assets/Synty/AnimationSwordCombat/Animations/Polygon/Hit/HitReact/A_Hit_R_React_Sword.fbx",
        };

        [MenuItem("Geis/Combat/Setup Directional Hit Reactions On Selected GeisPlayer")]
        public static void SetupOnSelectedGeisPlayer()
        {
            GameObject player = ResolveGeisPlayer();
            if (player == null)
            {
                Debug.LogWarning(
                    "[DirectionalHitReactionSetup] Select GeisPlayer in the Hierarchy (or open a scene containing GeisPlayer), then run this menu.");
                return;
            }

            DirectionalHitReactionSet set = LoadOrCreatePolygonReactionSet();
            if (set == null)
                return;

            if (!ApplyAnimatorStates(set))
                return;

            WireDirectionalHitReaction(player, set);
            Selection.activeGameObject = player;
            Debug.Log(
                $"[DirectionalHitReactionSetup] Directional hit reactions ready on '{player.name}' " +
                $"(animator layer '{HitReactionLayerName}', TakeDamage + HitDirection transitions).",
                player);
        }

        [MenuItem("Geis/Combat/Setup Directional Hit Reactions On Selected Enemy")]
        public static void SetupOnSelectedEnemy()
        {
            GameObject enemy = ResolveEnemyRoot();
            if (enemy == null)
            {
                Debug.LogWarning(
                    "[DirectionalHitReactionSetup] Select an enemy root (CombatEntity / EnemyCombatant), or open a scene with an enemy.");
                return;
            }

            DirectionalHitReactionSet set = LoadOrCreatePolygonReactionSet();
            if (set == null || !ApplyAnimatorStates(set))
                return;

            WireDirectionalHitReaction(enemy, set);
            Selection.activeGameObject = enemy;
            Debug.Log(
                $"[DirectionalHitReactionSetup] Directional hit reactions ready on '{enemy.name}'.",
                enemy);
        }

        [MenuItem("Geis/Combat/Add Directional Hit Reactions To Phase1 Enemy Prefab")]
        public static void AddToPhase1EnemyPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            if (root == null)
            {
                Debug.LogError($"[DirectionalHitReactionSetup] Prefab not found at {EnemyPrefabPath}");
                return;
            }

            DirectionalHitReactionSet set = LoadOrCreatePolygonReactionSet();
            if (set != null)
            {
                ApplyAnimatorStates(set);
                WireDirectionalHitReaction(root, set);
            }

            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[DirectionalHitReactionSetup] Updated P_Enemy_Phase1Humanoid with GeisDirectionalHitReaction.");
        }

        [MenuItem("Geis/Combat/Setup Directional Hit Reactions (Polygon Animator)")]
        public static void SetupPolygonAnimator()
        {
            var set = Selection.activeObject as DirectionalHitReactionSet ?? LoadOrCreatePolygonReactionSet();
            if (set == null)
            {
                Debug.LogWarning(
                    "[DirectionalHitReactionSetup] Select a DirectionalHitReactionSet asset in the Project window (with F/B/L/R clips assigned), then run this menu again.");
                return;
            }

            if (!ApplyAnimatorStates(set))
                return;

            Debug.Log($"[DirectionalHitReactionSetup] Updated '{HitReactionLayerName}' layer with TakeDamage + HitDirection transitions.");
        }

        [MenuItem("Geis/Combat/Add GeisDirectionalHitReaction To Player Prefab")]
        public static void AddComponentToPlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (root == null)
            {
                Debug.LogError($"[DirectionalHitReactionSetup] Prefab not found at {PlayerPrefabPath}");
                return;
            }

            DirectionalHitReactionSet set = LoadOrCreatePolygonReactionSet();
            if (set != null)
            {
                ApplyAnimatorStates(set);
                WireDirectionalHitReaction(root, set);
            }
            else if (root.GetComponent<GeisDirectionalHitReaction>() == null)
            {
                root.AddComponent<GeisDirectionalHitReaction>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[DirectionalHitReactionSetup] Updated Player prefab with GeisDirectionalHitReaction and wired references.");
        }

        private static GameObject ResolveEnemyRoot()
        {
            if (Selection.activeGameObject != null)
            {
                var combatant = Selection.activeGameObject.GetComponentInParent<EnemyCombatant>();
                if (combatant != null)
                    return combatant.gameObject;

                var entity = Selection.activeGameObject.GetComponentInParent<CombatEntity>();
                if (entity != null && entity.CompareTag("Enemy"))
                    return entity.gameObject;
            }

            var sceneCombatant = Object.FindFirstObjectByType<EnemyCombatant>();
            return sceneCombatant != null ? sceneCombatant.gameObject : null;
        }

        private static GameObject ResolveGeisPlayer()
        {
            if (Selection.activeGameObject != null)
            {
                if (Selection.activeGameObject.name == "GeisPlayer")
                    return Selection.activeGameObject;

                Transform found = Selection.activeGameObject.transform.root.Find("GeisPlayer");
                if (found != null)
                    return found.gameObject;
            }

            return GameObject.Find("GeisPlayer");
        }

        private static DirectionalHitReactionSet LoadOrCreatePolygonReactionSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<DirectionalHitReactionSet>(ReactionSetPath);
            if (set != null)
            {
                RefreshReactionSetClips(set);
                return set;
            }

            set = ScriptableObject.CreateInstance<DirectionalHitReactionSet>();
            set.front = LoadClipFromFbx(PolygonHitReactFbxPaths[0]);
            set.back = LoadClipFromFbx(PolygonHitReactFbxPaths[1]);
            set.left = LoadClipFromFbx(PolygonHitReactFbxPaths[2]);
            set.right = LoadClipFromFbx(PolygonHitReactFbxPaths[3]);

            if (set.front == null || set.back == null || set.left == null || set.right == null)
            {
                Debug.LogError(
                    "[DirectionalHitReactionSetup] Could not load one or more Synty Polygon hit-react clips. " +
                    "Expected FBX under Assets/Synty/AnimationSwordCombat/Animations/Polygon/Hit/HitReact/.");
                Object.DestroyImmediate(set);
                return null;
            }

            AssetDatabase.CreateAsset(set, ReactionSetPath);
            AssetDatabase.SaveAssets();
            return set;
        }

        private static AnimationClip LoadClipFromFbx(string fbxPath)
        {
            string preferredName = Path.GetFileNameWithoutExtension(fbxPath);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
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

        private static void RefreshReactionSetClips(DirectionalHitReactionSet set)
        {
            set.front = LoadClipFromFbx(PolygonHitReactFbxPaths[0]);
            set.back = LoadClipFromFbx(PolygonHitReactFbxPaths[1]);
            set.left = LoadClipFromFbx(PolygonHitReactFbxPaths[2]);
            set.right = LoadClipFromFbx(PolygonHitReactFbxPaths[3]);
            EditorUtility.SetDirty(set);
        }

        private static bool ApplyAnimatorStates(DirectionalHitReactionSet set)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError($"[DirectionalHitReactionSetup] Animator not found at {ControllerPath}");
                return false;
            }

            EnsureIntParameter(controller, HitDirectionParameter);
            EnsureTrigger(controller, TakeDamageTrigger);

            CleanupBaseLayerHitReactStates(controller);
            SetupHitReactionLayer(controller, set);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void CleanupBaseLayerHitReactStates(AnimatorController controller)
        {
            AnimatorStateMachine baseSm = controller.layers[0].stateMachine;
            RemoveAnyStateTransitionsTo(baseSm, "HitReact");
            RemoveStatesByName(baseSm, "HitReact_F", "HitReact_B", "HitReact_L", "HitReact_R");
        }

        private static void SetupHitReactionLayer(AnimatorController controller, DirectionalHitReactionSet set)
        {
            int layerIndex = GetOrCreateLayerIndex(controller, HitReactionLayerName);
            AnimatorControllerLayer layer = controller.layers[layerIndex];
            layer.defaultWeight = 0f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.avatarMask = null;
            controller.layers[layerIndex] = layer;

            AnimatorStateMachine sm = layer.stateMachine;
            AnimatorState empty = GetOrCreateEmptyState(sm);
            sm.defaultState = empty;

            AnimatorState front = AddOrUpdateState(sm, set.stateNameFront, set.front, new Vector3(520f, 160f, 0f));
            AnimatorState back = AddOrUpdateState(sm, set.stateNameBack, set.back, new Vector3(520f, 90f, 0f));
            AnimatorState left = AddOrUpdateState(sm, set.stateNameLeft, set.left, new Vector3(520f, 20f, 0f));
            AnimatorState right = AddOrUpdateState(sm, set.stateNameRight, set.right, new Vector3(520f, -50f, 0f));

            AssignMotion(front, set.front);
            AssignMotion(back, set.back);
            AssignMotion(left, set.left);
            AssignMotion(right, set.right);

            EnsureLayerWeightBehaviour(empty, 0f);
            EnsureLayerWeightBehaviour(front, 1f);
            EnsureLayerWeightBehaviour(back, 1f);
            EnsureLayerWeightBehaviour(left, 1f);
            EnsureLayerWeightBehaviour(right, 1f);

            RebuildDirectionalHitTransitions(sm, empty, front, back, left, right);
            SetLayerDefaultWeight(controller, HitReactionLayerName, 0f);
        }

        private static void EnsureLayerWeightBehaviour(AnimatorState state, float weight)
        {
            if (state == null)
                return;

            GeisHitReactionLayerWeightBehaviour behaviour = null;
            foreach (StateMachineBehaviour existing in state.behaviours)
            {
                if (existing is GeisHitReactionLayerWeightBehaviour layerWeightBehaviour)
                {
                    behaviour = layerWeightBehaviour;
                    break;
                }
            }

            if (behaviour == null)
                behaviour = state.AddStateMachineBehaviour<GeisHitReactionLayerWeightBehaviour>();

            behaviour.layerWeight = weight;
        }

        private static void AssignMotion(AnimatorState state, AnimationClip clip)
        {
            if (state != null && clip != null)
                state.motion = clip;
        }

        private static void SetLayerDefaultWeight(AnimatorController controller, string layerName, float weight)
        {
            SerializedObject so = new SerializedObject(controller);
            SerializedProperty layers = so.FindProperty("m_AnimatorLayers");
            for (int i = 0; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (layer.FindPropertyRelative("m_Name").stringValue != layerName)
                    continue;

                layer.FindPropertyRelative("m_DefaultWeight").floatValue = weight;
                so.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
        }

        private static int GetOrCreateLayerIndex(AnimatorController controller, string layerName)
        {
            for (int i = 0; i < controller.layers.Length; i++)
            {
                if (controller.layers[i].name == layerName)
                    return i;
            }

            controller.AddLayer(layerName);
            return controller.layers.Length - 1;
        }

        private static AnimatorState GetOrCreateEmptyState(AnimatorStateMachine sm)
        {
            foreach (ChildAnimatorState child in sm.states)
            {
                if (child.state.name != HitReactionEmptyStateName)
                    continue;

                child.state.motion = null;
                child.state.writeDefaultValues = false;
                return child.state;
            }

            AnimatorState empty = sm.AddState(HitReactionEmptyStateName, new Vector3(300f, 20f, 0f));
            empty.motion = null;
            empty.writeDefaultValues = false;
            return empty;
        }

        private static void RebuildDirectionalHitTransitions(
            AnimatorStateMachine sm,
            AnimatorState empty,
            AnimatorState front,
            AnimatorState back,
            AnimatorState left,
            AnimatorState right)
        {
            ClearAnyStateTransitions(sm);
            RemoveTransitionsToStates(sm, front, back, left, right);

            AddDirectionalAnyStateTransition(sm, front, CombatHitDirection.Front);
            AddDirectionalAnyStateTransition(sm, back, CombatHitDirection.Back);
            AddDirectionalAnyStateTransition(sm, left, CombatHitDirection.Left);
            AddDirectionalAnyStateTransition(sm, right, CombatHitDirection.Right);

            AddExitToEmptyTransition(front, empty);
            AddExitToEmptyTransition(back, empty);
            AddExitToEmptyTransition(left, empty);
            AddExitToEmptyTransition(right, empty);
        }

        private static void ClearAnyStateTransitions(AnimatorStateMachine sm)
        {
            for (int i = sm.anyStateTransitions.Length - 1; i >= 0; i--)
                sm.RemoveAnyStateTransition(sm.anyStateTransitions[i]);
        }

        private static void RemoveAnyStateTransitionsTo(AnimatorStateMachine sm, string destinationStateName)
        {
            for (int i = sm.anyStateTransitions.Length - 1; i >= 0; i--)
            {
                AnimatorStateTransition transition = sm.anyStateTransitions[i];
                if (transition.destinationState != null && transition.destinationState.name == destinationStateName)
                    sm.RemoveAnyStateTransition(transition);
            }
        }

        private static void RemoveStatesByName(AnimatorStateMachine sm, params string[] stateNames)
        {
            var names = new HashSet<string>(stateNames);
            for (int i = sm.states.Length - 1; i >= 0; i--)
            {
                if (names.Contains(sm.states[i].state.name))
                    sm.RemoveState(sm.states[i].state);
            }
        }

        private static void RemoveTransitionsToStates(AnimatorStateMachine sm, params AnimatorState[] states)
        {
            var targets = new HashSet<AnimatorState>(states);
            foreach (ChildAnimatorState child in sm.states)
            {
                for (int i = child.state.transitions.Length - 1; i >= 0; i--)
                {
                    AnimatorStateTransition transition = child.state.transitions[i];
                    if (transition.destinationState != null && targets.Contains(transition.destinationState))
                        child.state.RemoveTransition(transition);
                }
            }
        }

        private static void AddDirectionalAnyStateTransition(
            AnimatorStateMachine sm,
            AnimatorState destination,
            CombatHitDirection direction)
        {
            if (destination == null)
                return;

            AnimatorStateTransition transition = sm.AddAnyStateTransition(destination);
            transition.AddCondition(AnimatorConditionMode.If, 0f, TakeDamageTrigger);
            transition.AddCondition(AnimatorConditionMode.Equals, (int)direction, HitDirectionParameter);
            transition.duration = AnyStateTransitionDuration;
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.canTransitionToSelf = true;
            transition.interruptionSource = TransitionInterruptionSource.None;
        }

        private static void AddExitToEmptyTransition(AnimatorState from, AnimatorState empty)
        {
            if (from == null || empty == null)
                return;

            for (int i = from.transitions.Length - 1; i >= 0; i--)
            {
                AnimatorStateTransition existing = from.transitions[i];
                if (existing.destinationState == empty)
                    from.RemoveTransition(existing);
            }

            AnimatorStateTransition exit = from.AddTransition(empty);
            exit.hasExitTime = true;
            exit.exitTime = ExitTransitionNormalizedTime;
            exit.duration = ExitTransitionDuration;
            exit.hasFixedDuration = true;
            exit.offset = 0f;
        }

        private static void WireDirectionalHitReaction(GameObject root, DirectionalHitReactionSet set)
        {
            GeisDirectionalHitReaction reaction = root.GetComponent<GeisDirectionalHitReaction>();
            if (reaction == null)
                reaction = Undo.AddComponent<GeisDirectionalHitReaction>(root);

            Animator animator = root.GetComponent<Animator>();
            if (animator == null)
            {
                Transform character = root.transform.Find("GeisCharacter") ?? root.transform.Find("Visual");
                animator = character != null
                    ? character.GetComponent<Animator>()
                    : root.GetComponentInChildren<Animator>(true);
            }

            CombatEntity entity = root.GetComponent<CombatEntity>();

            SerializedObject so = new SerializedObject(reaction);
            so.FindProperty("reactionSet").objectReferenceValue = set;
            so.FindProperty("animator").objectReferenceValue = animator;
            so.FindProperty("combatEntity").objectReferenceValue = entity;
            so.FindProperty("crossFadeToState").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(root);
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

        private static AnimatorState AddOrUpdateState(AnimatorStateMachine root, string stateName, AnimationClip clip, Vector3 position)
        {
            if (string.IsNullOrEmpty(stateName) || clip == null)
                return null;

            foreach (ChildAnimatorState child in root.states)
            {
                if (child.state.name != stateName)
                    continue;

                child.state.motion = clip;
                return child.state;
            }

            AnimatorState state = root.AddState(stateName, position);
            state.motion = clip;
            return state;
        }
    }
}
#endif
