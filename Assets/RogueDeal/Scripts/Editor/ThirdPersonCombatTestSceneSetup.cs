using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using RogueDeal.Combat.Presentation;
using RogueDeal.Combat.Training;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;
using RogueDeal.Combat.Core.Targeting;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Cooldowns;
using RogueDeal.Player;

namespace RogueDeal.Editor
{
    /// <summary>
    /// Creates a minimal 3rd person combat test scene with player, camera, and training dummies
    /// </summary>
    public class ThirdPersonCombatTestSceneSetup : EditorWindow
    {
        [MenuItem("Tools/Combat Setup/Create 3rd Person Combat Test Scene")]
        public static void CreateTestScene()
        {
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Clear default objects (we'll create our own)
            GameObject[] rootObjects = newScene.GetRootGameObjects();
            foreach (GameObject obj in rootObjects)
            {
                DestroyImmediate(obj);
            }
            
            // Create ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one * 10f;
            
            Material groundMat = new Material(Shader.Find("Standard"));
            groundMat.color = new Color(0.3f, 0.5f, 0.3f); // Green-ish ground
            ground.GetComponent<Renderer>().material = groundMat;
            
            // Create lighting
            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            
            // Create player
            GameObject player = CreatePlayer();
            
            // Create camera
            GameObject camera = CreateCamera(player.transform);
            
            // Create training dummies
            CreateTrainingDummies();
            
            // Save scene
            string scenePath = "Assets/RogueDeal/Scenes/ThirdPersonCombatTest.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            
            Debug.Log($"✅ Created 3rd Person Combat Test Scene at: {scenePath}");
            Debug.Log("Controls:");
            Debug.Log("  WASD - Move");
            Debug.Log("  Left Shift - Run");
            Debug.Log("  Space - Dash");
            Debug.Log("  Left Click - Attack");
        }
        
        private static GameObject CreatePlayer()
        {
            // Create player root
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0, 1, 0);
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Default");
            
            // Add CharacterController
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.5f;
            characterController.center = new Vector3(0, 1, 0);
            characterController.slopeLimit = 45f;
            characterController.stepOffset = 0.3f;
            
            // Create visual representation (capsule)
            GameObject playerVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerVisual.name = "PlayerVisual";
            playerVisual.transform.SetParent(player.transform);
            playerVisual.transform.localPosition = new Vector3(0, 1, 0);
            playerVisual.transform.localRotation = Quaternion.identity;
            playerVisual.transform.localScale = Vector3.one;
            
            // Remove collider from visual (CharacterController handles collision)
            DestroyImmediate(playerVisual.GetComponent<CapsuleCollider>());
            
            // Color player
            Material playerMat = new Material(Shader.Find("Standard"));
            playerMat.color = Color.blue;
            playerVisual.GetComponent<Renderer>().material = playerMat;
            
            // Add Animator (required for ThirdPersonCombatController)
            Animator animator = playerVisual.AddComponent<Animator>();
            
            // Try to use existing third-person controller, or create minimal one
            AnimatorController animatorController = GetOrCreateAnimatorController();
            animator.runtimeAnimatorController = animatorController;
            
            // Add CombatEntity
            CombatEntity combatEntity = player.AddComponent<CombatEntity>();
            combatEntity.ForceInitializeStats(100f, 10f, 5f);
            
            // Add CombatExecutor
            CombatExecutor combatExecutor = player.AddComponent<CombatExecutor>();
            
            // Add CombatEventReceiver (for animation events)
            player.AddComponent<CombatEventReceiver>();
            
            // Add ThirdPersonCombatController
            ThirdPersonCombatController controller = player.AddComponent<ThirdPersonCombatController>();
            
            // Create and assign basic combat actions
            CombatAction[] actions = CreateBasicCombatActions();
            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty actionsProperty = serializedController.FindProperty("combatActions");
            actionsProperty.ClearArray();
            actionsProperty.arraySize = actions.Length;
            for (int i = 0; i < actions.Length; i++)
            {
                actionsProperty.GetArrayElementAtIndex(i).objectReferenceValue = actions[i];
            }
            serializedController.ApplyModifiedProperties();
            
            // Set enemy layer mask
            SerializedProperty enemyLayerProperty = serializedController.FindProperty("enemyLayerMask");
            enemyLayerProperty.intValue = LayerMask.GetMask("Enemy", "Default"); // Include Default for dummies
            serializedController.ApplyModifiedProperties();
            
            Debug.Log("✅ Created Player with all required components");
            return player;
        }
        
        private static GameObject CreateCamera(Transform target)
        {
            GameObject cameraGO = new GameObject("Main Camera");
            Camera cam = cameraGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            
            // Add CombatCameraController
            CombatCameraController cameraController = cameraGO.AddComponent<CombatCameraController>();
            SerializedObject serializedCamera = new SerializedObject(cameraController);
            serializedCamera.FindProperty("target").objectReferenceValue = target;
            serializedCamera.FindProperty("offset").vector3Value = new Vector3(0f, 5f, -8f);
            serializedCamera.FindProperty("lookAtOffset").vector3Value = new Vector3(0f, 1f, 0f);
            serializedCamera.FindProperty("smoothTime").floatValue = 0.3f;
            serializedCamera.ApplyModifiedProperties();
            
            // Position camera initially
            cameraController.SnapToTarget();
            
            Debug.Log("✅ Created Camera with CombatCameraController");
            return cameraGO;
        }
        
        private static void CreateTrainingDummies()
        {
            // Create 3 dummies in a line
            for (int i = 0; i < 3; i++)
            {
                float xPos = (i - 1) * 3f; // -3, 0, 3
                CreateTrainingDummy(new Vector3(xPos, 1, 5));
            }
            
            Debug.Log("✅ Created 3 Training Dummies");
        }
        
        private static GameObject CreateTrainingDummy(Vector3 position)
        {
            // Create dummy GameObject
            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = "TrainingDummy";
            dummy.transform.position = position;
            dummy.tag = "Enemy";
            dummy.layer = LayerMask.NameToLayer("Default"); // Use Default layer
            
            // Color dummy
            Material dummyMat = new Material(Shader.Find("Standard"));
            dummyMat.color = Color.red;
            dummy.GetComponent<Renderer>().material = dummyMat;
            
            // Set up collider
            CapsuleCollider collider = dummy.GetComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            collider.isTrigger = false; // Keep as non-trigger for physics
            
            // Add CombatEntity
            CombatEntity combatEntity = dummy.AddComponent<CombatEntity>();
            combatEntity.ForceInitializeStats(1000f, 10f, 5f);
            
            // Add TrainingDummy component
            TrainingDummy trainingDummy = dummy.AddComponent<TrainingDummy>();
            SerializedObject serializedDummy = new SerializedObject(trainingDummy);
            serializedDummy.FindProperty("maxHealth").floatValue = 1000f;
            serializedDummy.FindProperty("infiniteHealth").boolValue = true;
            serializedDummy.FindProperty("behavior").enumValueIndex = 0; // Idle
            serializedDummy.ApplyModifiedProperties();
            
            return dummy;
        }
        
        private static CombatAction[] CreateBasicCombatActions()
        {
            string actionsPath = "Assets/RogueDeal/Resources/Combat/TestActions/";
            System.IO.Directory.CreateDirectory(actionsPath);
            
            // Create damage effect
            DamageEffect damageEffect = ScriptableObject.CreateInstance<DamageEffect>();
            damageEffect.effectName = "Basic Damage";
            damageEffect.baseDamage = 50f;
            damageEffect.damageType = DamageType.Physical;
            damageEffect.scalingStat = StatType.Attack;
            damageEffect.scalingMultiplier = 1f;
            damageEffect.canCrit = true;
            AssetDatabase.CreateAsset(damageEffect, $"{actionsPath}Effect_BasicDamage.asset");
            
            // Create targeting strategy
            SingleTargetSelector targeting = ScriptableObject.CreateInstance<SingleTargetSelector>();
            targeting.strategyName = "Single Target Melee";
            targeting.maxRange = 5f;
            targeting.targetLayers = LayerMask.GetMask("Enemy", "Default");
            AssetDatabase.CreateAsset(targeting, $"{actionsPath}Targeting_SingleTargetMelee.asset");
            
            // Create basic attack action
            CombatAction basicAttack = ScriptableObject.CreateInstance<CombatAction>();
            basicAttack.actionName = "Basic Attack";
            basicAttack.description = "A simple melee attack";
            basicAttack.animationTrigger = "Attack_1"; // Adjust based on your animator
            basicAttack.effects = new BaseEffect[] { damageEffect };
            basicAttack.targetingStrategy = targeting;
            basicAttack.cooldownConfig = new CooldownConfiguration
            {
                cooldownType = CooldownType.None
            };
            AssetDatabase.CreateAsset(basicAttack, $"{actionsPath}Action_BasicAttack.asset");
            
            // Create second attack action (for combo)
            DamageEffect damageEffect2 = ScriptableObject.CreateInstance<DamageEffect>();
            damageEffect2.effectName = "Combo Damage";
            damageEffect2.baseDamage = 60f;
            damageEffect2.damageType = DamageType.Physical;
            damageEffect2.scalingStat = StatType.Attack;
            damageEffect2.scalingMultiplier = 1f;
            damageEffect2.canCrit = true;
            AssetDatabase.CreateAsset(damageEffect2, $"{actionsPath}Effect_ComboDamage.asset");
            
            CombatAction comboAttack = ScriptableObject.CreateInstance<CombatAction>();
            comboAttack.actionName = "Combo Attack";
            comboAttack.description = "A follow-up attack";
            comboAttack.animationTrigger = "Attack_2"; // Adjust based on your animator
            comboAttack.effects = new BaseEffect[] { damageEffect2 };
            comboAttack.targetingStrategy = targeting;
            comboAttack.cooldownConfig = new CooldownConfiguration
            {
                cooldownType = CooldownType.None
            };
            AssetDatabase.CreateAsset(comboAttack, $"{actionsPath}Action_ComboAttack.asset");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("✅ Created basic CombatActions");
            
            return new CombatAction[] { basicAttack, comboAttack };
        }
        
        private static AnimatorController GetOrCreateAnimatorController()
        {
            // First, try to find the existing ThirdPerson_Controller
            string existingControllerPath = "Assets/RogueDeal/Combat/Animations/ThirdPerson_Controller.controller";
            AnimatorController existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(existingControllerPath);
            
            if (existingController != null)
            {
                Debug.Log($"✅ Found existing controller: {existingControllerPath}");
                // Add missing parameters to existing controller
                AddMissingParameters(existingController);
                return existingController;
            }
            
            // If not found, try alternative paths
            string[] alternativePaths = new string[]
            {
                "Assets/RogueDeal/Resources/Combat/Animations/ThirdPerson_Controller.controller",
                "Assets/RogueDeal/Scripts/Combat/Animations/ThirdPerson_Controller.controller"
            };
            
            foreach (string path in alternativePaths)
            {
                existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (existingController != null)
                {
                    Debug.Log($"✅ Found existing controller at: {path}");
                    AddMissingParameters(existingController);
                    return existingController;
                }
            }
            
            // If no existing controller found, create a minimal one
            Debug.Log("⚠ No existing ThirdPerson_Controller found, creating minimal controller");
            return CreateMinimalAnimatorController();
        }
        
        private static void AddMissingParameters(AnimatorController controller)
        {
            // Check what parameters already exist
            bool hasAttack1 = HasParameter(controller, "Attack_1");
            bool hasAttack2 = HasParameter(controller, "Attack_2");
            bool hasAttack3 = HasParameter(controller, "Attack_3");
            bool hasRun = HasParameter(controller, "Run");
            bool hasDash = HasParameter(controller, "Dash");
            bool hasSpeed = HasParameter(controller, "Speed");
            bool hasIsGrounded = HasParameter(controller, "IsGrounded");
            bool hasIsRunning = HasParameter(controller, "IsRunning");
            bool hasTakeAction = HasParameter(controller, "TakeAction");
            bool hasActionIndex = HasParameter(controller, "ActionIndex");
            bool hasIsAction = HasParameter(controller, "IsAction");
            
            int addedCount = 0;
            
            // If controller uses Attack_1/2/3 system, we need to add compatibility parameters
            // OR add the new system parameters
            // For now, let's add the new system parameters that the script expects
            // These will work alongside the existing Attack_1/2/3 parameters
            
            // Add Speed if missing (optional but recommended)
            if (!hasSpeed)
            {
                controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
                addedCount++;
                Debug.Log("   Added parameter: Speed (Float)");
            }
            
            // Add IsGrounded if missing (optional)
            if (!hasIsGrounded)
            {
                controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
                addedCount++;
                Debug.Log("   Added parameter: IsGrounded (Bool)");
            }
            
            // Add IsRunning if missing (controller already has Run, but script uses IsRunning)
            // We'll add IsRunning and the script can use it, or we could modify script to use Run
            // For compatibility, let's add IsRunning
            if (!hasIsRunning && !hasRun)
            {
                controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
                addedCount++;
                Debug.Log("   Added parameter: IsRunning (Bool)");
            }
            
            // Add Dash if missing (controller has dash states but might need trigger)
            if (!hasDash)
            {
                controller.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
                addedCount++;
                Debug.Log("   Added parameter: Dash (Trigger)");
            }
            
            // Add new action system parameters (TakeAction, ActionIndex, IsAction)
            // Note: These work alongside Attack_1/2/3 - the script uses the new system
            if (!hasTakeAction)
            {
                controller.AddParameter("TakeAction", AnimatorControllerParameterType.Trigger);
                addedCount++;
                Debug.Log("   Added parameter: TakeAction (Trigger)");
            }
            
            if (!hasActionIndex)
            {
                controller.AddParameter("ActionIndex", AnimatorControllerParameterType.Int);
                addedCount++;
                Debug.Log("   Added parameter: ActionIndex (Int)");
            }
            
            if (!hasIsAction)
            {
                controller.AddParameter("IsAction", AnimatorControllerParameterType.Bool);
                addedCount++;
                Debug.Log("   Added parameter: IsAction (Bool)");
            }
            
            if (addedCount > 0)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                Debug.Log($"✅ Added {addedCount} missing parameter(s) to existing controller");
                Debug.Log("   Note: Controller now supports both Attack_1/2/3 and TakeAction systems");
            }
            else
            {
                Debug.Log("✅ All compatibility parameters already exist in controller");
            }
        }
        
        private static bool HasParameter(AnimatorController controller, string paramName)
        {
            foreach (var param in controller.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }
        
        private static AnimatorController CreateMinimalAnimatorController()
        {
            string controllerPath = "Assets/RogueDeal/Resources/Combat/TestActions/ThirdPersonCombatTest.controller";
            
            // Check if controller already exists
            AnimatorController existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (existingController != null)
            {
                Debug.Log("✅ Using existing minimal animator controller");
                return existingController;
            }
            
            // Create new animator controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            
            // Add required parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("TakeAction", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("ActionIndex", AnimatorControllerParameterType.Int);
            controller.AddParameter("IsAction", AnimatorControllerParameterType.Bool);
            
            // Create a default empty state (required for animator controller)
            // The default state is automatically created, but we'll make sure it exists
            var rootStateMachine = controller.layers[0].stateMachine;
            var defaultState = rootStateMachine.defaultState;
            if (defaultState == null)
            {
                defaultState = rootStateMachine.AddState("Idle");
                rootStateMachine.defaultState = defaultState;
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"✅ Created minimal animator controller at: {controllerPath}");
            Debug.Log("   Note: This controller has parameters but no animation states.");
            Debug.Log("   For full functionality, assign animation clips and set up states manually.");
            
            return controller;
        }
    }
}

