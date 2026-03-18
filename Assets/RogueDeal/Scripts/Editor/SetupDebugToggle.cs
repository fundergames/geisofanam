using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RogueDeal.Editor
{
    public class SetupDebugToggle : EditorWindow
    {
        [MenuItem("Rogue Deal/Setup Debug UI Toggle")]
        public static void Setup()
        {
            SetupInCombatScene();
            SetupInEntryScene();
            
            Debug.Log("Debug UI Toggle setup complete in both scenes!");
        }

        private static void SetupInCombatScene()
        {
            var combatScene = EditorSceneManager.OpenScene("Assets/RogueDeal/Scenes/Combat.unity", OpenSceneMode.Additive);
            
            GameObject debugToggle = GameObject.Find("DebugToggle");
            if (debugToggle == null)
            {
                debugToggle = new GameObject("DebugToggle");
            }

            var toggleScript = debugToggle.GetComponent<RogueDeal.UI.DebugUIToggle>();
            if (toggleScript == null)
            {
                toggleScript = debugToggle.AddComponent<RogueDeal.UI.DebugUIToggle>();
            }

            GameObject debugger = GameObject.Find("Debugger");
            if (debugger != null)
            {
                Transform combatFlowDebugger = debugger.transform.Find("CombatFlowDebugger");
                if (combatFlowDebugger != null)
                {
                    SerializedObject so = new SerializedObject(toggleScript);
                    so.FindProperty("combatFlowDebugger").objectReferenceValue = combatFlowDebugger.gameObject;
                    so.FindProperty("startHidden").boolValue = false;
                    so.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(combatScene);
            EditorSceneManager.SaveScene(combatScene);
            
            Debug.Log("Debug UI Toggle setup in Combat scene");
        }

        private static void SetupInEntryScene()
        {
            var entryScene = EditorSceneManager.OpenScene("Assets/RogueDeal/Scenes/Entry.unity", OpenSceneMode.Additive);
            
            GameObject debugToggle = GameObject.Find("DebugToggle");
            if (debugToggle == null)
            {
                debugToggle = new GameObject("DebugToggle");
            }

            var toggleScript = debugToggle.GetComponent<RogueDeal.UI.DebugUIToggle>();
            if (toggleScript == null)
            {
                toggleScript = debugToggle.AddComponent<RogueDeal.UI.DebugUIToggle>();
            }

            GameObject exampleServices = GameObject.Find("ExampleServices");
            if (exampleServices != null)
            {
                SerializedObject so = new SerializedObject(toggleScript);
                so.FindProperty("analyticsDemo").objectReferenceValue = exampleServices;
                so.FindProperty("startHidden").boolValue = false;
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(entryScene);
            EditorSceneManager.SaveScene(entryScene);
            
            Debug.Log("Debug UI Toggle setup in Entry scene");
        }
    }
}
