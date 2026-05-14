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
using System.IO;
using Geis.Combat;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Cooldowns;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;
using RogueDeal.Combat.Core.Targeting;
using RogueDeal.Combat.Presentation;
using RogueDeal.Player;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Geis.Enemies.Editor
{
    /// <summary>
    /// Creates the Phase 1 enemy AI data assets, prefab, and a dedicated test arena scene.
    /// </summary>
    public static class EnemyPhase1ArenaBuilder
    {
        private const string DataFolder = "Assets/Geis/Data/EnemyAIPhase1";
        private const string PrefabFolder = "Assets/Prefabs/Enemies";
        private const string PrefabPath = "Assets/Prefabs/Enemies/P_Enemy_Phase1Humanoid.prefab";
        private const string ScenePath = "Assets/Geis/Scenes/EnemyAITestArena.unity";
        private const string PlayerPrefabPath = "Assets/Geis/Combat/Prefabs/Player.prefab";

        [MenuItem("Funder Games/Geis/Enemies/Build Phase 1 Enemy Arena")]
        public static void BuildPhase1EnemyArena()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "Phase 1 Enemy AI Arena",
                    "This creates or overwrites:\n"
                    + PrefabPath + "\n"
                    + ScenePath + "\n\nContinue?",
                    "Build",
                    "Cancel"))
            {
                return;
            }

            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Geis/Scenes");

            EnemyCurrentTargetingStrategy targeting = GetOrCreateTargetingStrategy();
            DamageEffect damageEffect = GetOrCreateDamageEffect();
            Weapon weapon = GetOrCreateWeapon();
            CombatProfile combatProfile = GetOrCreateCombatProfile();
            CombatAction attackAction = GetOrCreateAttackAction(targeting, damageEffect);
            EnemyAiDefinition definition = GetOrCreateDefinition(weapon, combatProfile, attackAction);

            AssetDatabase.SaveAssets();

            GameObject prefab = BuildOrUpdatePrefab(definition);
            BuildOrUpdateScene(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Phase 1 Enemy AI Arena",
                    "Created data assets, enemy prefab, and test arena scene.\n\n"
                    + "Prefab: " + PrefabPath + "\n"
                    + "Scene: " + ScenePath,
                    "OK");
            }
        }

        private static EnemyCurrentTargetingStrategy GetOrCreateTargetingStrategy()
        {
            const string path = DataFolder + "/Targeting_EnemyCurrentTarget.asset";
            EnemyCurrentTargetingStrategy asset = AssetDatabase.LoadAssetAtPath<EnemyCurrentTargetingStrategy>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EnemyCurrentTargetingStrategy>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.strategyName = "Enemy Current Target";
            asset.description = "Uses EnemyPerception.CurrentTarget so enemy CombatActions do not target nearby allies.";
            asset.defaultRange = 2.5f;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static DamageEffect GetOrCreateDamageEffect()
        {
            const string path = DataFolder + "/Effect_Damage_Phase1EnemySwing.asset";
            DamageEffect asset = AssetDatabase.LoadAssetAtPath<DamageEffect>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DamageEffect>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.baseDamage = 4f;
            asset.damageType = DamageType.Physical;
            asset.scalingStat = StatType.Attack;
            asset.scalingMultiplier = 0.65f;
            asset.canCrit = false;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Weapon GetOrCreateWeapon()
        {
            const string path = DataFolder + "/Weapon_Phase1EnemyBlade.asset";
            Weapon asset = AssetDatabase.LoadAssetAtPath<Weapon>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<Weapon>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.weaponName = "Enemy Blade";
            asset.description = "Short melee weapon for the Phase 1 humanoid enemy.";
            asset.slotType = WeaponSlotType.SingleHand;
            asset.baseDamage = 2f;
            asset.maxRange = 2.2f;
            asset.damageTypeMultiplierArray = new[] { new DamageTypeMultiplier { damageType = DamageType.Physical, multiplier = 1f } };
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CombatProfile GetOrCreateCombatProfile()
        {
            const string path = DataFolder + "/CombatProfile_Phase1Humanoid.asset";
            CombatProfile asset = AssetDatabase.LoadAssetAtPath<CombatProfile>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CombatProfile>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.profileName = "Phase 1 Humanoid";
            asset.description = "Short-range melee profile for the first enemy AI archetype.";
            asset.combatRange = CombatRange.Melee;
            asset.engagementDistance = 1.9f;
            asset.requiresLineOfSight = true;
            asset.animatorOverrideController = null;
            asset.movementSpeedMultiplier = 1f;
            asset.returnToOriginAfterAttack = false;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CombatAction GetOrCreateAttackAction(TargetingStrategy targeting, DamageEffect damageEffect)
        {
            const string path = DataFolder + "/Action_Phase1EnemyLightSwing.asset";
            CombatAction asset = AssetDatabase.LoadAssetAtPath<CombatAction>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CombatAction>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.actionName = "Enemy Light Swing";
            asset.description = "Phase 1 humanoid enemy melee strike.";
            asset.weaponType = WeaponType.Other;
            asset.animationTrigger = string.Empty;
            asset.comboAnimations = null;
            asset.timelineAsset = null;
            asset.targetingStrategy = targeting;
            asset.effects = new BaseEffect[] { damageEffect };
            asset.isCombo = false;
            asset.comboHitCount = 1;
            asset.perHitEffects = null;
            asset.isProjectile = false;
            asset.projectilePrefab = null;
            asset.spawnsPersistentAOE = false;
            asset.persistentAOEPrefab = null;
            asset.cooldownConfig = new CooldownConfiguration
            {
                cooldownType = CooldownType.TimeBased,
                timeCooldown = 1.25f,
                triggersGlobalCooldown = false
            };
            asset.effectBindings = null;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static EnemyAiDefinition GetOrCreateDefinition(Weapon weapon, CombatProfile combatProfile, CombatAction attackAction)
        {
            const string path = DataFolder + "/EnemyAI_Phase1Humanoid.asset";
            EnemyAiDefinition asset = AssetDatabase.LoadAssetAtPath<EnemyAiDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EnemyAiDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.enemyId = "phase1_humanoid";
            asset.displayName = "Phase 1 Humanoid";
            asset.description = "Drop-in melee enemy that acquires the player, telegraphs, attacks, and resets cleanly.";
            asset.maxHealth = 120f;
            asset.attack = 14f;
            asset.defense = 4f;
            asset.equippedWeapon = weapon;
            asset.combatProfile = combatProfile;
            asset.animatorOverrideController = null;

            asset.perception.aggroRange = 14f;
            asset.perception.loseTargetRange = 20f;
            asset.perception.eyeHeight = 1.6f;
            asset.perception.requiresLineOfSight = true;
            asset.perception.lineOfSightBlockers = ~0;

            asset.movement.moveSpeed = 3.8f;
            asset.movement.angularSpeed = 540f;
            asset.movement.acceleration = 24f;
            asset.movement.stopDistance = 1.6f;
            asset.movement.preferredDistance = 1.85f;
            asset.movement.distanceTolerance = 0.35f;
            asset.movement.strafeDistance = 1.85f;
            asset.movement.strafeRepathInterval = 1.15f;
            asset.movement.directMoveFallbackSpeed = 3.25f;

            asset.reactions.staggerDurationOnHit = 0.2f;
            asset.reactions.deathDisableDelay = 0.65f;

            asset.defaultSquadId = "phase1_test";
            asset.defaultCombatRole = EnemyCombatRole.Frontliner;
            asset.publishLegacyDefeatEvent = false;
            asset.attacks = new[]
            {
                new EnemyAttackDefinition
                {
                    attackId = "light_swing",
                    action = attackAction,
                    minRange = 0f,
                    maxRange = weapon.maxRange,
                    telegraphDuration = 0.45f,
                    recoveryDuration = 0.8f,
                    cooldownSeconds = 1.25f,
                    facingToleranceDegrees = 35f,
                    requiresLineOfSight = true,
                    selectionWeight = 1,
                    telegraphTrigger = "Telegraph",
                    attackTriggerOverride = "Attack",
                    executionTimeout = 0.3f
                }
            };

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static GameObject BuildOrUpdatePrefab(EnemyAiDefinition definition)
        {
            if (definition == null)
                return null;

            var root = new GameObject("P_Enemy_Phase1Humanoid");
            TrySetTag(root, "Enemy");
            TrySetLayer(root, "Enemy");

            var capsuleCollider = root.AddComponent<CapsuleCollider>();
            capsuleCollider.height = 2f;
            capsuleCollider.radius = 0.42f;
            capsuleCollider.center = new Vector3(0f, 1f, 0f);

            var lockOnTrigger = root.AddComponent<SphereCollider>();
            lockOnTrigger.isTrigger = true;
            lockOnTrigger.radius = 6f;
            lockOnTrigger.center = new Vector3(0f, 1f, 0f);

            var navAgent = root.AddComponent<NavMeshAgent>();
            navAgent.speed = definition.movement.moveSpeed;
            navAgent.angularSpeed = definition.movement.angularSpeed;
            navAgent.acceleration = definition.movement.acceleration;
            navAgent.stoppingDistance = definition.movement.stopDistance;
            navAgent.radius = 0.42f;
            navAgent.height = 2f;

            var combatEntity = root.AddComponent<CombatEntity>();
            combatEntity.InitializeStatsWithoutHeroData(definition.maxHealth, definition.attack, definition.defense);

            root.AddComponent<CombatExecutor>();
            root.AddComponent<EnemyCoordinationContext>();
            root.AddComponent<EnemyPerception>();
            root.AddComponent<EnemyMotor>();
            root.AddComponent<EnemyAnimatorDriver>();
            root.AddComponent<EnemyAttackDriver>();
            root.AddComponent<EnemyBrain>();
            root.AddComponent<EnemyVisual>();
            root.AddComponent<GeisObjectLockOn>();
            var combatant = root.AddComponent<EnemyCombatant>();

            Transform model = CreateModelHierarchy(root.transform);
            Transform hitPoint = CreateMarker(root.transform, "HitPoint", new Vector3(0f, 1.35f, 0.35f));
            CreateTargetHighlight(root.transform);
            CreateWorldHealthBar(root.transform);

            combatEntity.hitPoint = hitPoint;
            combatEntity.vfxSpawnPoint = hitPoint;

            var combatantSo = new SerializedObject(combatant);
            combatantSo.FindProperty("definition").objectReferenceValue = definition;
            combatantSo.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildOrUpdateScene(GameObject enemyPrefab)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                Debug.LogError("[EnemyPhase1ArenaBuilder] Player prefab not found at " + PlayerPrefabPath);
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLighting();
            CreateGround();

            var player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            if (player != null)
            {
                player.name = "Player";
                player.transform.position = new Vector3(0f, 0f, -4f);
                player.transform.rotation = Quaternion.identity;
            }

            GameObject enemy = null;
            if (enemyPrefab != null)
            {
                enemy = PrefabUtility.InstantiatePrefab(enemyPrefab) as GameObject;
                if (enemy != null)
                {
                    enemy.transform.position = new Vector3(0f, 0f, 6f);
                    enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }
            }

            var encounterRoot = new GameObject("EnemyEncounter");
            var encounter = encounterRoot.AddComponent<EnemyEncounterController>();
            if (enemy != null)
            {
                var enemyCombatant = enemy.GetComponent<EnemyCombatant>();
                var soEncounter = new SerializedObject(encounter);
                var enemiesProp = soEncounter.FindProperty("managedEnemies");
                enemiesProp.arraySize = 1;
                enemiesProp.GetArrayElementAtIndex(0).objectReferenceValue = enemyCombatant;
                soEncounter.FindProperty("autoStartOnAwake").boolValue = true;
                soEncounter.FindProperty("autoLoopOnClear").boolValue = true;
                soEncounter.FindProperty("resetDelaySeconds").floatValue = 2.5f;
                soEncounter.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void SetupLighting()
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private static void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(24f, 1f, 24f);
            SetRendererColor(ground.GetComponent<Renderer>(), new Color(0.22f, 0.24f, 0.27f));
        }

        private static Transform CreateModelHierarchy(Transform parent)
        {
            var modelRoot = new GameObject("Model");
            modelRoot.transform.SetParent(parent, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(modelRoot.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.9f, 1f, 0.9f);
            RemoveCollider(body);
            SetRendererColor(body.GetComponent<Renderer>(), new Color(0.33f, 0.37f, 0.31f));

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(modelRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            head.transform.localScale = Vector3.one * 0.42f;
            RemoveCollider(head);
            SetRendererColor(head.GetComponent<Renderer>(), new Color(0.54f, 0.57f, 0.51f));

            var weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weapon.name = "WeaponProxy";
            weapon.transform.SetParent(modelRoot.transform, false);
            weapon.transform.localPosition = new Vector3(0.4f, 1.1f, 0.25f);
            weapon.transform.localRotation = Quaternion.Euler(0f, 0f, 55f);
            weapon.transform.localScale = new Vector3(0.08f, 0.55f, 0.08f);
            RemoveCollider(weapon);
            SetRendererColor(weapon.GetComponent<Renderer>(), new Color(0.75f, 0.79f, 0.84f));

            return modelRoot.transform;
        }

        private static Transform CreateMarker(Transform parent, string name, Vector3 localPosition)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            return marker.transform;
        }

        private static void CreateTargetHighlight(Transform parent)
        {
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "TargetHighlight";
            orb.transform.SetParent(parent, false);
            orb.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            orb.transform.localScale = Vector3.one * 0.18f;
            RemoveCollider(orb);
            SetRendererColor(orb.GetComponent<Renderer>(), new Color(0.15f, 0.82f, 0.92f));
            orb.SetActive(false);
        }

        private static void CreateWorldHealthBar(Transform parent)
        {
            var canvasGo = new GameObject("EnemyHealthBar");
            canvasGo.transform.SetParent(parent, false);
            canvasGo.transform.localPosition = new Vector3(0f, 2.25f, 0f);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140f, 28f);
            rect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(canvasGo.transform, false);
            var sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0.4f);
            sliderRect.anchorMax = new Vector2(1f, 1f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 100f;

            var background = new GameObject("Background");
            background.transform.SetParent(sliderGo.transform, false);
            var backgroundRect = background.AddComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0.6f);

            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0.01f, 0.1f);
            fillAreaRect.anchorMax = new Vector2(0.99f, 0.9f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.72f, 0.12f, 0.12f);

            slider.targetGraphic = fillImage;
            slider.fillRect = fillRect;

            var healthTextGo = new GameObject("HealthText");
            healthTextGo.transform.SetParent(canvasGo.transform, false);
            var textRect = healthTextGo.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0.45f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = healthTextGo.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 12f;
            text.text = "100 / 100";

            var enemyHealthBar = canvasGo.AddComponent<EnemyHealthBar>();
            var soHealthBar = new SerializedObject(enemyHealthBar);
            soHealthBar.FindProperty("healthBarSlider").objectReferenceValue = slider;
            soHealthBar.FindProperty("healthText").objectReferenceValue = text;
            soHealthBar.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveCollider(GameObject go)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
        }

        private static void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            var material = new Material(renderer.sharedMaterial);
            material.color = color;
            renderer.sharedMaterial = material;
        }

        private static void TrySetTag(GameObject gameObject, string tag)
        {
            try
            {
                gameObject.tag = tag;
            }
            catch
            {
                Debug.LogWarning("[EnemyPhase1ArenaBuilder] Missing tag '" + tag + "' for object " + gameObject.name);
            }
        }

        /// <summary>
        /// Matches <see cref="SimpleAttackHitDetector"/> default mask (Enemy layer only): hurtboxes must not stay on Default.
        /// </summary>
        private static void TrySetLayer(GameObject gameObject, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning("[EnemyPhase1ArenaBuilder] Missing layer '" + layerName + "' — player melee may not hit this prefab.");
                return;
            }

            gameObject.layer = layer;
        }
    }
}
#endif
