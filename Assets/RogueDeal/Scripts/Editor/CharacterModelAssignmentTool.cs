using UnityEngine;
using UnityEditor;
using RogueDeal.Player;
using RogueDeal.Enemies;
using System.Collections.Generic;
using System.Linq;

namespace RogueDeal.Editor
{
    public class CharacterModelAssignmentTool : EditorWindow
    {
        private Vector2 heroScrollPosition;
        private Vector2 enemyScrollPosition;
        private Vector2 modelScrollPosition;
        
        private List<HeroVisualData> heroVisualDataList = new List<HeroVisualData>();
        private List<EnemyDefinition> enemyDefinitionsList = new List<EnemyDefinition>();
        private List<GameObject> availableModels = new List<GameObject>();
        
        private const string HERO_MODEL_PATH = "Assets/TEMP_REMOVE_WHEN_CONVERTED/FunderGames/RPG/Assets/Prefabs/Heroes";
        
        [MenuItem("Rogue Deal/Character Model Assignment Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<CharacterModelAssignmentTool>("Character Models");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            RefreshData();
        }
        
        private void RefreshData()
        {
            heroVisualDataList = FindAllAssetsOfType<HeroVisualData>();
            enemyDefinitionsList = FindAllAssetsOfType<EnemyDefinition>();
            LoadAvailableModels();
        }
        
        private void LoadAvailableModels()
        {
            availableModels.Clear();
            
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { HERO_MODEL_PATH });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    availableModels.Add(prefab);
                }
            }
        }
        
        private List<T> FindAllAssetsOfType<T>() where T : Object
        {
            List<T> assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }
            
            return assets;
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Character Model Assignment Tool", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Refresh Data", GUILayout.Width(150)))
            {
                RefreshData();
            }
            
            if (GUILayout.Button("Auto-Assign by Name", GUILayout.Width(150)))
            {
                AutoAssignModels();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            DrawHeroVisualDataSection();
            
            EditorGUILayout.Space(10);
            
            DrawEnemyDefinitionsSection();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            DrawAvailableModelsSection();
        }
        
        private void DrawHeroVisualDataSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(380));
            
            GUILayout.Label("Hero Visual Data", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            heroScrollPosition = EditorGUILayout.BeginScrollView(heroScrollPosition, GUILayout.Height(400));
            
            foreach (var heroVisual in heroVisualDataList)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                EditorGUILayout.LabelField(heroVisual.name, EditorStyles.boldLabel);
                
                EditorGUI.BeginChangeCheck();
                GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Character Prefab",
                    heroVisual.characterPrefab,
                    typeof(GameObject),
                    false
                );
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(heroVisual, "Change Character Prefab");
                    heroVisual.characterPrefab = newPrefab;
                    EditorUtility.SetDirty(heroVisual);
                }
                
                if (heroVisual.characterPrefab != null)
                {
                    EditorGUILayout.LabelField("✓ Assigned", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("⚠ Not Assigned", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawEnemyDefinitionsSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(380));
            
            GUILayout.Label("Enemy Definitions", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            enemyScrollPosition = EditorGUILayout.BeginScrollView(enemyScrollPosition, GUILayout.Height(400));
            
            foreach (var enemy in enemyDefinitionsList)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                EditorGUILayout.LabelField($"{enemy.displayName} ({enemy.name})", EditorStyles.boldLabel);
                
                EditorGUI.BeginChangeCheck();
                GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Model Prefab",
                    enemy.modelPrefab,
                    typeof(GameObject),
                    false
                );
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(enemy, "Change Model Prefab");
                    enemy.modelPrefab = newPrefab;
                    EditorUtility.SetDirty(enemy);
                }
                
                if (enemy.modelPrefab != null)
                {
                    EditorGUILayout.LabelField("✓ Assigned", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("⚠ Not Assigned", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAvailableModelsSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label($"Available Character Models ({availableModels.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            modelScrollPosition = EditorGUILayout.BeginScrollView(modelScrollPosition, GUILayout.Height(150));
            
            EditorGUILayout.BeginHorizontal();
            
            int columns = 4;
            for (int i = 0; i < availableModels.Count; i++)
            {
                if (i > 0 && i % columns == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
                
                GameObject model = availableModels[i];
                EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(180));
                
                EditorGUILayout.ObjectField(model, typeof(GameObject), false);
                EditorGUILayout.LabelField(model.name, EditorStyles.miniLabel);
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void AutoAssignModels()
        {
            int assignedCount = 0;
            
            foreach (var heroVisual in heroVisualDataList)
            {
                if (heroVisual.characterPrefab != null)
                    continue;
                
                string heroName = ExtractHeroName(heroVisual.name);
                GameObject matchingModel = FindModelByName(heroName);
                
                if (matchingModel != null)
                {
                    Undo.RecordObject(heroVisual, "Auto-assign Character Prefab");
                    heroVisual.characterPrefab = matchingModel;
                    EditorUtility.SetDirty(heroVisual);
                    assignedCount++;
                    Debug.Log($"[CharacterModelTool] Auto-assigned {matchingModel.name} to {heroVisual.name}");
                }
            }
            
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog(
                "Auto-Assignment Complete",
                $"Successfully auto-assigned {assignedCount} character model(s).",
                "OK"
            );
        }
        
        private string ExtractHeroName(string assetName)
        {
            string name = assetName.Replace("Hero_", "").Replace("_VisualData", "");
            return name;
        }
        
        private GameObject FindModelByName(string heroName)
        {
            return availableModels.FirstOrDefault(m => 
                m.name.Equals(heroName, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
