using UnityEngine;
using UnityEditor;

namespace RogueDeal.HexLevels.Editor
{
    public class RoadPrototypeVisualizer : EditorWindow
    {
        private const string PREFAB_BASE_PATH = "Assets/KayKit/Packs/KayKit - Medieval Hexagon Pack (for Unity)/Prefabs/tiles/roads/";
        private Vector3 startPosition = new Vector3(0, 0, 0);
        private float groupSpacing = 10f;
        private float rotationSpacing = 3f;
        
        [MenuItem("Tools/Hex Levels/Visualize Road Prototypes (All Rotations)")]
        public static void ShowWindow()
        {
            GetWindow<RoadPrototypeVisualizer>("Road Prototype Visualizer");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Road Prototype Visualizer", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool will place ALL road prefab variants (A-M) in the scene, " +
                "with each variant shown in all 6 rotations (0°, 60°, 120°, 180°, 240°, 300°).\n\n" +
                "Each instance will be labeled with:\n" +
                "- Variant name (A-M)\n" +
                "- Rotation angle\n" +
                "- Connection pattern (binary bits)",
                MessageType.Info);
            
            GUILayout.Space(10);
            
            startPosition = EditorGUILayout.Vector3Field("Start Position", startPosition);
            groupSpacing = EditorGUILayout.FloatField("Group Spacing", groupSpacing);
            rotationSpacing = EditorGUILayout.FloatField("Rotation Spacing", rotationSpacing);
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Place All Prototype Variations", GUILayout.Height(40)))
            {
                PlaceAllPrototypes();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Clear All Test Instances", GUILayout.Height(30)))
            {
                ClearTestInstances();
            }
        }
        
        private void PlaceAllPrototypes()
        {
            // Load database
            RoadPatternDatabase_New database = AssetDatabase.LoadAssetAtPath<RoadPatternDatabase_New>(
                "Assets/RogueDeal/Resources/Data/HexLevels/RoadPatternDatabaseV2.asset");
            
            if (database == null || database.prototypes.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", 
                    "Road Pattern Database V2 not found or empty.\n\nRun: Tools > Hex Levels > Generate Road Pattern Database V2", 
                    "OK");
                return;
            }
            
            // Create parent container
            GameObject container = new GameObject("RoadPrototype_Visualizer");
            Undo.RegisterCreatedObjectUndo(container, "Create Road Prototype Visualizer");
            
            Vector3 currentGroupPos = startPosition;
            
            // For each prototype
            for (int i = 0; i < database.prototypes.Count; i++)
            {
                PrefabPrototype prototype = database.prototypes[i];
                
                // Create group container
                GameObject groupContainer = new GameObject($"Group_{prototype.variantName}_{prototype.description}");
                groupContainer.transform.parent = container.transform;
                groupContainer.transform.position = currentGroupPos;
                Undo.RegisterCreatedObjectUndo(groupContainer, "Create Prototype Group");
                
                // Load the prefab
                string prefabPath = PREFAB_BASE_PATH + $"hex_road_{prototype.variantName.ToLower()}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                if (prefab == null)
                {
                    Debug.LogWarning($"Prefab not found at: {prefabPath}");
                    continue;
                }
                
                // Place 6 rotations
                for (int rot = 0; rot < 6; rot++)
                {
                    int rotationDegrees = rot * 60;
                    Vector3 instancePos = currentGroupPos + new Vector3(rot * rotationSpacing, 0, 0);
                    
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.transform.position = instancePos;
                    instance.transform.rotation = Quaternion.Euler(0, rotationDegrees, 0);
                    instance.transform.parent = groupContainer.transform;
                    
                    // Calculate rotated pattern
                    int rotatedPattern = RotatePattern(prototype.basePattern, rot);
                    string binaryPattern = System.Convert.ToString(rotatedPattern, 2).PadLeft(6, '0');
                    string connections = GetConnectionsString(rotatedPattern);
                    
                    // Name with all info
                    instance.name = $"{prototype.variantName}_{rotationDegrees}deg_Pattern{rotatedPattern}_{connections.Replace("+", "")}";
                    
                    Undo.RegisterCreatedObjectUndo(instance, "Create Road Prototype Instance");
                    
                    // Add label with connections
                    CreateLabel(instance, $"{prototype.variantName}\n{rotationDegrees}°\n{connections}", instancePos + Vector3.up * 2);
                }
                
                // Move to next group position (down Z axis)
                currentGroupPos.z += groupSpacing;
            }
            
            // Frame the container
            Selection.activeGameObject = container;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
            
            Debug.Log($"✓ Placed {database.prototypes.Count} prototype groups with 6 rotations each = {database.prototypes.Count * 6} instances");
        }
        
        private void CreateLabel(GameObject parent, string text, Vector3 position)
        {
            // Create a simple GameObject with a TextMesh for labeling
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.parent = parent.transform;
            labelObj.transform.position = position;
            labelObj.transform.rotation = Quaternion.Euler(90, 0, 0); // Face up
            
            TextMesh textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 24;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = Color.white;
            textMesh.characterSize = 0.1f;
            
            Undo.RegisterCreatedObjectUndo(labelObj, "Create Label");
        }
        
        private int RotatePattern(int pattern, int steps)
        {
            steps = steps % 6;
            if (steps == 0) return pattern;
            
            int result = 0;
            for (int i = 0; i < 6; i++)
            {
                if ((pattern & (1 << i)) != 0)
                {
                    // Rotate clockwise: E→SE→SW→W→NW→NE→E
                    // So subtract steps instead of add
                    int newPos = (i - steps + 6) % 6;
                    result |= (1 << newPos);
                }
            }
            return result;
        }
        
        private string GetConnectionsString(int pattern)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string[] dirs = { "E", "NE", "NW", "W", "SW", "SE" };
            
            for (int i = 0; i < 6; i++)
            {
                if ((pattern & (1 << i)) != 0)
                {
                    if (sb.Length > 0) sb.Append("+");
                    sb.Append(dirs[i]);
                }
            }
            
            return sb.Length > 0 ? sb.ToString() : "none";
        }
        
        private void ClearTestInstances()
        {
            GameObject container = GameObject.Find("RoadPrototype_Visualizer");
            if (container != null)
            {
                Undo.DestroyObjectImmediate(container);
                Debug.Log("✓ Cleared all test instances");
            }
            else
            {
                Debug.Log("No test instances found");
            }
        }
    }
}
