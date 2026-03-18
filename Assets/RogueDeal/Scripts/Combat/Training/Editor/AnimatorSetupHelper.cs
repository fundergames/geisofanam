using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace RogueDeal.Combat.Training.Editor
{
    public class AnimatorSetupHelper : EditorWindow
    {
        private Animator targetAnimator;
        
        [MenuItem("RogueDeal/Combat/Setup Animator Parameters")]
        public static void ShowWindow()
        {
            GetWindow<AnimatorSetupHelper>("Animator Setup");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Combat Animator Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "This will add the required trigger parameters to your Animator Controller:\n" +
                "- Attack\n" +
                "- Hit\n" +
                "- Dodge\n" +
                "- Death",
                MessageType.Info
            );
            
            EditorGUILayout.Space();
            
            targetAnimator = EditorGUILayout.ObjectField("Target Animator", targetAnimator, typeof(Animator), true) as Animator;
            
            EditorGUILayout.Space();
            
            GUI.enabled = targetAnimator != null;
            
            if (GUILayout.Button("Add Combat Parameters", GUILayout.Height(30)))
            {
                AddCombatParameters();
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "After adding parameters, you need to:\n" +
                "1. Open the Animator Controller\n" +
                "2. Create transitions from 'Any State' to your attack/hit animations\n" +
                "3. Set conditions to use these triggers",
                MessageType.Warning
            );
        }
        
        private void AddCombatParameters()
        {
            if (targetAnimator == null || targetAnimator.runtimeAnimatorController == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an Animator with a valid controller!", "OK");
                return;
            }
            
            AnimatorController controller = targetAnimator.runtimeAnimatorController as AnimatorController;
            
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Error", "The animator controller is not editable!", "OK");
                return;
            }
            
            int addedCount = 0;
            
            if (!HasParameter(controller, "Attack"))
            {
                controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
                addedCount++;
            }
            
            if (!HasParameter(controller, "Hit"))
            {
                controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
                addedCount++;
            }
            
            if (!HasParameter(controller, "Dodge"))
            {
                controller.AddParameter("Dodge", AnimatorControllerParameterType.Trigger);
                addedCount++;
            }
            
            if (!HasParameter(controller, "Death"))
            {
                controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
                addedCount++;
            }
            
            if (addedCount > 0)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                
                EditorUtility.DisplayDialog(
                    "Success", 
                    $"Added {addedCount} parameter(s) to {controller.name}!\n\n" +
                    "Next steps:\n" +
                    "1. Open the Animator window\n" +
                    "2. Create transitions to your attack/hit animations\n" +
                    "3. The warnings should now be gone!",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog("Info", "All parameters already exist!", "OK");
            }
        }
        
        private bool HasParameter(AnimatorController controller, string paramName)
        {
            foreach (var param in controller.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }
    }
}
