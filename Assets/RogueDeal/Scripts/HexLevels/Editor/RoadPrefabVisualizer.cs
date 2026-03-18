using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RogueDeal.HexLevels.Editor
{
    public class RoadPrefabVisualizer : EditorWindow
    {
        private List<GameObject> roadPrefabs = new List<GameObject>();
        private Vector2 scrollPos;
        private float spacing = 3f;
        
        [MenuItem("Tools/Hex Levels/Road Prefab Visualizer")]
        static void ShowWindow()
        {
            var window = GetWindow<RoadPrefabVisualizer>("Road Variants");
            window.Show();
        }

        void OnEnable()
        {
            LoadRoadPrefabs();
        }

        void LoadRoadPrefabs()
        {
            roadPrefabs.Clear();
            string[] prefabNames = new string[] 
            { 
                "hex_road_A", "hex_road_B", "hex_road_C", "hex_road_D", 
                "hex_road_E", "hex_road_F", "hex_road_G", "hex_road_H",
                "hex_road_I", "hex_road_J", "hex_road_K", "hex_road_L", "hex_road_M"
            };

            string basePath = "Assets/KayKit/Packs/KayKit - Medieval Hexagon Pack (for Unity)/Prefabs/tiles/roads/";
            
            foreach (string name in prefabNames)
            {
                string path = basePath + name + ".prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    roadPrefabs.Add(prefab);
                }
            }
            
            Debug.Log($"Loaded {roadPrefabs.Count} road prefabs");
        }

        void OnGUI()
        {
            GUILayout.Label("Road Prefab Variants", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This shows all road variants. Click 'Place in Scene' to test each one.\n" +
                "Place them in a line to see which directions they connect.\n\n" +
                "Hex directions: E(0°), NE(60°), NW(120°), W(180°), SW(240°), SE(300°)",
                MessageType.Info
            );

            spacing = EditorGUILayout.Slider("Spacing", spacing, 1f, 5f);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Reload Prefabs"))
            {
                LoadRoadPrefabs();
            }
            
            if (GUILayout.Button("Clear All Test Roads"))
            {
                ClearTestRoads();
            }
            
            EditorGUILayout.Space();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            for (int i = 0; i < roadPrefabs.Count; i++)
            {
                GameObject prefab = roadPrefabs[i];
                
                EditorGUILayout.BeginHorizontal("box");
                
                EditorGUILayout.LabelField(prefab.name, GUILayout.Width(120));
                
                if (GUILayout.Button("Place in Scene", GUILayout.Width(120)))
                {
                    PlacePrefabInScene(prefab, i);
                }
                
                if (GUILayout.Button("Select Asset", GUILayout.Width(100)))
                {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }

        void PlacePrefabInScene(GameObject prefab, int index)
        {
            Vector3 position = new Vector3(index * spacing, 0, 0);
            
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            instance.name = $"{prefab.name}_Test";
            instance.tag = "EditorOnly";
            
            Undo.RegisterCreatedObjectUndo(instance, "Place Test Road");
            Selection.activeGameObject = instance;
            
            SceneView.lastActiveSceneView.Frame(new Bounds(position, Vector3.one * 2f), false);
            
            Debug.Log($"Placed {prefab.name} at {position}");
        }

        void ClearTestRoads()
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            List<GameObject> toDelete = new List<GameObject>();
            
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("hex_road_") && obj.name.Contains("_Test"))
                {
                    toDelete.Add(obj);
                }
            }
            
            foreach (GameObject obj in toDelete)
            {
                Undo.DestroyObjectImmediate(obj);
            }
            
            Debug.Log($"Cleared {toDelete.Count} test road objects");
        }
    }
}
