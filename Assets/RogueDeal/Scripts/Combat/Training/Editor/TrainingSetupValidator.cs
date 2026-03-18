using UnityEngine;
using UnityEditor;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Combat.Training.Editor
{
    [InitializeOnLoad]
    public class TrainingSetupValidator
    {
        static TrainingSetupValidator()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ValidateTrainingSetup();
            }
        }
        
        [MenuItem("RogueDeal/Combat/Validate Training Setup")]
        public static void ValidateTrainingSetup()
        {
            bool allGood = true;
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            
            report.AppendLine("=== Training Mode Setup Validation ===\n");
            
            TrainingModeManager manager = Object.FindObjectOfType<TrainingModeManager>();
            if (manager != null)
            {
                report.AppendLine("✓ TrainingModeManager found");
            }
            else
            {
                report.AppendLine("✗ TrainingModeManager NOT found - Create one!");
                allGood = false;
            }
            
            TrainingAttackController attackController = Object.FindObjectOfType<TrainingAttackController>();
            if (attackController != null)
            {
                report.AppendLine("✓ TrainingAttackController found");
            }
            else
            {
                report.AppendLine("✗ TrainingAttackController NOT found - Will be auto-added");
            }
            
            // CombatExecutor (on player) is used for real-time combat
            // CombatExecutor is automatically added by CombatEntity, no need to check for it
            var combatExecutor = Object.FindObjectOfType<RogueDeal.Combat.Presentation.CombatExecutor>();
            if (combatExecutor != null)
            {
                report.AppendLine("✓ CombatExecutor found (new system)");
            }
            else
            {
                report.AppendLine("⚠ CombatExecutor not found - will be auto-added by CombatEntity");
            }
            
            CombatEntity[] entities = Object.FindObjectsOfType<CombatEntity>();
            if (entities.Length >= 2)
            {
                report.AppendLine($"✓ Found {entities.Length} CombatEntities");
                foreach (var entity in entities)
                {
                    report.AppendLine($"  - {entity.gameObject.name}");
                }
            }
            else if (entities.Length == 1)
            {
                report.AppendLine($"⚠ Only 1 CombatEntity found - Need at least 2 (player + dummy)");
                report.AppendLine($"  - {entities[0].gameObject.name}");
                allGood = false;
            }
            else
            {
                report.AppendLine("✗ No CombatEntities found - Add to player and dummy!");
                allGood = false;
            }
            
            TrainingDummy dummy = Object.FindObjectOfType<TrainingDummy>();
            if (dummy != null)
            {
                report.AppendLine($"✓ TrainingDummy found: {dummy.gameObject.name}");
            }
            else
            {
                report.AppendLine("⚠ TrainingDummy NOT found - Create one for best results");
            }
            
            CombatAction[] actions = Resources.LoadAll<CombatAction>("Combat/Actions");
            if (actions.Length > 0)
            {
                report.AppendLine($"✓ Found {actions.Length} actions in Resources/Combat/Actions");
                foreach (var action in actions)
                {
                    report.AppendLine($"  - {action.actionName}");
                }
            }
            else
            {
                report.AppendLine("⚠ No CombatAction assets found in Resources/Combat/Actions");
                string[] guids = AssetDatabase.FindAssets("t:CombatAction");
                if (guids.Length > 0)
                {
                    report.AppendLine($"  But found {guids.Length} CombatAction assets elsewhere");
                }
            }
            
            report.AppendLine();
            
            if (allGood)
            {
                report.AppendLine("=== Setup looks good! Press Play and F12 to start training ===");
                Debug.Log(report.ToString());
            }
            else
            {
                report.AppendLine("=== Some components are missing - see above for details ===");
                report.AppendLine("\nQuick Fix: Go to RogueDeal > Combat > Setup Training Mode");
                Debug.LogWarning(report.ToString());
            }
        }
    }
    
    [CustomEditor(typeof(TrainingAttackController))]
    public class TrainingAttackControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            TrainingAttackController controller = (TrainingAttackController)target;
            
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Quick Test Controls", EditorStyles.boldLabel);
                
                GUILayout.BeginVertical(EditorStyles.helpBox);
                
                if (GUILayout.Button("Test Quick Attack (SPACE)", GUILayout.Height(30)))
                {
                    controller.PerformQuickAttack();
                }
                
                EditorGUILayout.Space(5);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Ability 1", GUILayout.Height(25)))
                {
                    controller.PerformAbility(0);
                }
                if (GUILayout.Button("Ability 2", GUILayout.Height(25)))
                {
                    controller.PerformAbility(1);
                }
                if (GUILayout.Button("Ability 3", GUILayout.Height(25)))
                {
                    controller.PerformAbility(2);
                }
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Ability 4", GUILayout.Height(25)))
                {
                    controller.PerformAbility(3);
                }
                if (GUILayout.Button("Ability 5", GUILayout.Height(25)))
                {
                    controller.PerformAbility(4);
                }
                GUILayout.EndHorizontal();
                
                GUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test attacks from Inspector", MessageType.Info);
            }
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Print Current Setup"))
            {
                controller.PrintSetup();
            }
        }
    }
}
