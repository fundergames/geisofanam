using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RogueDeal.Combat;
using RogueDeal.Combat.Visual;
using RogueDeal.Items;

namespace RogueDeal.Editor
{
    /// <summary>
    /// Wizard that automatically extracts body parts from TestPlayer1.prefab and sets up
    /// the modular character system with all necessary prefabs and data assets.
    /// </summary>
    public class CharacterVisualSetupWizard : EditorWindow
    {
        private GameObject sourcePrefab;
        private string outputPath = "Assets/RogueDeal/Combat/Prefabs/ModularCharacters";
        private string bodyPartsPath = "Assets/RogueDeal/Combat/Prefabs/BodyParts";
        private string dataPath = "Assets/RogueDeal/Combat/Data/CharacterVisuals";
        
        private Vector2 scrollPosition;
        private bool showAdvanced = false;
        
        [MenuItem("Funder Games/Rogue Deal/Character Visual Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<CharacterVisualSetupWizard>("Character Visual Setup");
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Label("Character Visual System Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This wizard will extract body parts from your TestPlayer1.prefab and create " +
                "all necessary prefabs and data assets for the modular character system.",
                MessageType.Info);
            
            GUILayout.Space(10);
            
            // Source prefab
            EditorGUILayout.LabelField("Source Prefab", EditorStyles.boldLabel);
            sourcePrefab = (GameObject)EditorGUILayout.ObjectField(
                "TestPlayer1 Prefab",
                sourcePrefab,
                typeof(GameObject),
                false);
            
            if (sourcePrefab == null)
            {
                // Try to auto-find it
                string[] guids = AssetDatabase.FindAssets("TestPlayer1 t:Prefab");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
            
            GUILayout.Space(10);
            
            // Output paths
            EditorGUILayout.LabelField("Output Paths", EditorStyles.boldLabel);
            outputPath = EditorGUILayout.TextField("Base Prefab Path", outputPath);
            bodyPartsPath = EditorGUILayout.TextField("Body Parts Path", bodyPartsPath);
            dataPath = EditorGUILayout.TextField("Data Assets Path", dataPath);
            
            GUILayout.Space(10);
            
            // Advanced options
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Options");
            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "Advanced options for fine-tuning the extraction process.",
                    MessageType.None);
                EditorGUI.indentLevel--;
            }
            
            GUILayout.Space(20);
            
            // Buttons
            EditorGUI.BeginDisabledGroup(sourcePrefab == null);
            
            if (GUILayout.Button("🚀 Setup Complete Modular System", GUILayout.Height(40)))
            {
                SetupCompleteSystem();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("📦 Extract Body Parts Only", GUILayout.Height(30)))
            {
                ExtractBodyPartsStep();
            }
            
            if (GUILayout.Button("🎨 Create Base Character Prefab", GUILayout.Height(30)))
            {
                CreateBaseCharacterPrefabStep();
            }
            
            if (GUILayout.Button("📋 Create All Data Assets", GUILayout.Height(30)))
            {
                CreateDataAssets();
            }
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void SetupCompleteSystem()
        {
            if (sourcePrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign the source prefab!", "OK");
                return;
            }
            
            if (!EditorUtility.DisplayDialog(
                "Setup Complete System",
                "This will:\n" +
                "1. Extract all body parts from TestPlayer1.prefab\n" +
                "2. Create BodyPartData assets\n" +
                "3. Create BaseCharacter prefab\n" +
                "4. Add attachment points\n" +
                "5. Create CharacterVisualData asset\n\n" +
                "Continue?",
                "Yes", "Cancel"))
            {
                return;
            }
            
            EditorUtility.DisplayProgressBar("Setting up...", "Creating directories...", 0f);
            
            try
            {
                // Create directories
                CreateDirectories();
                
                EditorUtility.DisplayProgressBar("Setting up...", "Extracting body parts...", 0.2f);
                
                // Extract body parts
                List<BodyPartInfo> bodyParts = ExtractBodyParts();
                
                EditorUtility.DisplayProgressBar("Setting up...", "Creating body part data assets...", 0.4f);
                
                // Create BodyPartData assets
                List<CharacterBodyPartData> bodyPartDataAssets = CreateBodyPartDataAssets(bodyParts);
                
                EditorUtility.DisplayProgressBar("Setting up...", "Creating base character prefab...", 0.6f);
                
                // Create base character prefab
                GameObject basePrefab = CreateBaseCharacterPrefab();
                
                EditorUtility.DisplayProgressBar("Setting up...", "Adding attachment points...", 0.7f);
                
                // Add attachment points
                AddAttachmentPoints(basePrefab);
                
                EditorUtility.DisplayProgressBar("Setting up...", "Creating character visual data...", 0.9f);
                
                // Create CharacterVisualData
                CreateCharacterVisualDataAsset(basePrefab, bodyPartDataAssets);
                
                EditorUtility.DisplayProgressBar("Setting up...", "Finalizing...", 1f);
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog(
                    "Success!",
                    "Modular character system setup complete!\n\n" +
                    "Created:\n" +
                    $"- {bodyParts.Count} body part prefabs\n" +
                    $"- {bodyPartDataAssets.Count} BodyPartData assets\n" +
                    "- BaseCharacter prefab\n" +
                    "- CharacterVisualData asset\n\n" +
                    "Check the output folders for all assets.",
                    "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Setup failed: {e.Message}", "OK");
                Debug.LogError($"[CharacterVisualSetupWizard] Error: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
        
        private void CreateDirectories()
        {
            if (!AssetDatabase.IsValidFolder(outputPath))
            {
                string parent = System.IO.Path.GetDirectoryName(outputPath).Replace('\\', '/');
                string folderName = System.IO.Path.GetFileName(outputPath);
                AssetDatabase.CreateFolder(parent, folderName);
            }
            
            if (!AssetDatabase.IsValidFolder(bodyPartsPath))
            {
                string parent = System.IO.Path.GetDirectoryName(bodyPartsPath).Replace('\\', '/');
                string folderName = System.IO.Path.GetFileName(bodyPartsPath);
                AssetDatabase.CreateFolder(parent, folderName);
            }
            
            if (!AssetDatabase.IsValidFolder(dataPath))
            {
                string parent = System.IO.Path.GetDirectoryName(dataPath).Replace('\\', '/');
                string folderName = System.IO.Path.GetFileName(dataPath);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
        
        private class BodyPartInfo
        {
            public GameObject gameObject;
            public string name;
            public SkinnedMeshRenderer renderer;
            public BodyPartCategory category;
        }
        
        private List<BodyPartInfo> ExtractBodyParts()
        {
            List<BodyPartInfo> bodyParts = new List<BodyPartInfo>();
            
            // Open prefab for editing
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(
                AssetDatabase.GetAssetPath(sourcePrefab));
            
            // Find all SkinnedMeshRenderer components
            SkinnedMeshRenderer[] renderers = prefabInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            
            foreach (var renderer in renderers)
            {
                GameObject obj = renderer.gameObject;
                string objName = obj.name;
                
                // Skip if it's a bone or attachment point
                if (objName.Contains("weapon") || 
                    objName.Contains("bone") ||
                    objName.Contains("Bone") ||
                    objName.Contains("root") ||
                    objName.Contains("Root") ||
                    objName.Contains("VFX") ||
                    objName.Contains("hitPoint") ||
                    objName.StartsWith("Cloak"))
                {
                    continue;
                }
                
                // Check if it looks like a body part (Body## pattern or common body part names)
                if (objName.StartsWith("Body") || 
                    IsBodyPartName(objName))
                {
                    BodyPartInfo info = new BodyPartInfo
                    {
                        gameObject = obj,
                        name = objName,
                        renderer = renderer,
                        category = GuessBodyPartCategory(objName)
                    };
                    
                    bodyParts.Add(info);
                }
            }
            
            // Create body part prefabs
            foreach (var info in bodyParts)
            {
                // Create a copy of the GameObject
                GameObject bodyPartCopy = Instantiate(info.gameObject);
                bodyPartCopy.name = $"BodyPart_{info.name}";
                
                // Remove all children (we only want the mesh)
                // Note: We keep children as they might be needed for the SkinnedMeshRenderer bones
                // Only remove if they're not part of the mesh structure
                
                // Save as prefab
                string prefabPath = $"{bodyPartsPath}/BodyPart_{info.name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(bodyPartCopy, prefabPath);
                DestroyImmediate(bodyPartCopy);
                
                Debug.Log($"[CharacterVisualSetupWizard] Created body part: {prefabPath}");
            }
            
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            
            return bodyParts;
        }
        
        private bool IsBodyPartName(string name)
        {
            string lower = name.ToLower();
            return lower.Contains("head") ||
                   lower.Contains("torso") ||
                   lower.Contains("arm") ||
                   lower.Contains("leg") ||
                   lower.Contains("hand") ||
                   lower.Contains("foot") ||
                   lower.Contains("body");
        }
        
        private BodyPartCategory GuessBodyPartCategory(string name)
        {
            string lower = name.ToLower();
            
            if (lower.Contains("head")) return BodyPartCategory.Head;
            if (lower.Contains("torso") || lower.Contains("chest") || lower.Contains("body") && !lower.Contains("arm") && !lower.Contains("leg")) 
                return BodyPartCategory.Torso;
            if (lower.Contains("arm")) return BodyPartCategory.Arms;
            if (lower.Contains("leg")) return BodyPartCategory.Legs;
            if (lower.Contains("hand")) return BodyPartCategory.Hands;
            if (lower.Contains("foot")) return BodyPartCategory.Feet;
            
            return BodyPartCategory.Other;
        }
        
        private List<CharacterBodyPartData> CreateBodyPartDataAssets(List<BodyPartInfo> bodyParts)
        {
            List<CharacterBodyPartData> assets = new List<CharacterBodyPartData>();
            
            foreach (var info in bodyParts)
            {
                CharacterBodyPartData data = ScriptableObject.CreateInstance<CharacterBodyPartData>();
                data.bodyPartName = info.name;
                data.category = info.category;
                
                // Load the prefab we just created
                string prefabPath = $"{bodyPartsPath}/BodyPart_{info.name}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                data.bodyPartPrefab = prefab;
                
                data.visibleByDefault = true;
                data.canBeHiddenByEquipment = true;
                
                string assetPath = $"{dataPath}/BodyPartData_{info.name}.asset";
                AssetDatabase.CreateAsset(data, assetPath);
                assets.Add(data);
                
                Debug.Log($"[CharacterVisualSetupWizard] Created BodyPartData: {assetPath}");
            }
            
            return assets;
        }
        
        private GameObject CreateBaseCharacterPrefab()
        {
            // Load the source prefab
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(
                AssetDatabase.GetAssetPath(sourcePrefab));
            
            // Remove body part meshes
            List<GameObject> toDestroy = new List<GameObject>();
            
            SkinnedMeshRenderer[] renderers = prefabInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                GameObject obj = renderer.gameObject;
                string objName = obj.name;
                
                // Keep skeleton bones, remove body part meshes
                if (objName.StartsWith("Body") || IsBodyPartName(objName))
                {
                    toDestroy.Add(obj);
                }
            }
            
            // Remove equipment meshes (they'll be attached at runtime)
            MeshRenderer[] meshRenderers = prefabInstance.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in meshRenderers)
            {
                GameObject obj = renderer.gameObject;
                // Keep attachment points, remove equipment
                if (obj.name.Contains("THS") || obj.name.Contains("Sword") || 
                    obj.name.Contains("Cloak") || obj.name.Contains("Armor"))
                {
                    toDestroy.Add(obj);
                }
            }
            
            // Destroy marked objects
            foreach (var obj in toDestroy)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            
            // Add CharacterVisualManager if not present
            CharacterVisualManager visualManager = prefabInstance.GetComponent<CharacterVisualManager>();
            if (visualManager == null)
            {
                visualManager = prefabInstance.AddComponent<CharacterVisualManager>();
            }
            
            // Save as new prefab
            string prefabPath = $"{outputPath}/BaseCharacter.prefab";
            GameObject basePrefab = PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            
            PrefabUtility.UnloadPrefabContents(prefabInstance);
            
            Debug.Log($"[CharacterVisualSetupWizard] Created base character prefab: {prefabPath}");
            
            return basePrefab;
        }
        
        private void AddAttachmentPoints(GameObject basePrefab)
        {
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(
                AssetDatabase.GetAssetPath(basePrefab));
            
            // Find common attachment bones
            Dictionary<string, EquipmentSlot> attachmentPoints = new Dictionary<string, EquipmentSlot>
            {
                { "weapon_r", EquipmentSlot.Weapon },
                { "weapon_l", EquipmentSlot.Weapon },
                { "hand_r", EquipmentSlot.Weapon },
                { "hand_l", EquipmentSlot.Weapon },
                { "neck_01", EquipmentSlot.Helmet },
                { "head", EquipmentSlot.Helmet },
            };
            
            // Find bones and add attachment points
            Transform[] allTransforms = prefabInstance.GetComponentsInChildren<Transform>(true);
            foreach (var transform in allTransforms)
            {
                string name = transform.name;
                
                foreach (var kvp in attachmentPoints)
                {
                    if (name.Contains(kvp.Key) || name == kvp.Key)
                    {
                        // Check if already has component
                        if (transform.GetComponent<EquipmentAttachmentPoint>() == null)
                        {
                            EquipmentAttachmentPoint point = transform.gameObject.AddComponent<EquipmentAttachmentPoint>();
                            point.attachmentPointName = kvp.Key;
                            point.slot = kvp.Value;
                            point.hideBodyPartsWhenEquipped = kvp.Value == EquipmentSlot.Weapon;
                            
                            if (kvp.Value == EquipmentSlot.Weapon)
                            {
                                point.bodyPartsToHide = new BodyPartCategory[] { BodyPartCategory.Hands };
                            }
                            
                            Debug.Log($"[CharacterVisualSetupWizard] Added attachment point: {name} -> {kvp.Value}");
                        }
                    }
                }
            }
            
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, AssetDatabase.GetAssetPath(basePrefab));
            PrefabUtility.UnloadPrefabContents(prefabInstance);
        }
        
        private void CreateCharacterVisualDataAsset(GameObject basePrefab, List<CharacterBodyPartData> bodyPartDataAssets)
        {
            CharacterVisualData visualData = ScriptableObject.CreateInstance<CharacterVisualData>();
            visualData.characterName = "Warrior";
            visualData.description = "Modular warrior character created from TestPlayer1";
            visualData.baseCharacterPrefab = basePrefab;
            visualData.bodyParts = bodyPartDataAssets;
            visualData.scaleMultiplier = 1f;
            
            string assetPath = $"{dataPath}/CharacterVisual_Warrior.asset";
            AssetDatabase.CreateAsset(visualData, assetPath);
            
            Debug.Log($"[CharacterVisualSetupWizard] Created CharacterVisualData: {assetPath}");
        }
        
        // Individual methods for step-by-step setup
        private void ExtractBodyPartsStep()
        {
            if (sourcePrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign the source prefab!", "OK");
                return;
            }
            
            CreateDirectories();
            List<BodyPartInfo> bodyParts = ExtractBodyParts();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", $"Extracted {bodyParts.Count} body parts!", "OK");
        }
        
        private void CreateBaseCharacterPrefabStep()
        {
            if (sourcePrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign the source prefab!", "OK");
                return;
            }
            
            CreateDirectories();
            GameObject basePrefab = CreateBaseCharacterPrefab();
            AddAttachmentPoints(basePrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", "Created BaseCharacter prefab with attachment points!", "OK");
        }
        
        private void CreateDataAssets()
        {
            // Find all body part prefabs
            string[] guids = AssetDatabase.FindAssets("BodyPart_ t:Prefab", new[] { bodyPartsPath });
            
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No body part prefabs found! Extract body parts first.", "OK");
                return;
            }
            
            CreateDirectories();
            List<BodyPartInfo> bodyParts = new List<BodyPartInfo>();
            
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                BodyPartInfo info = new BodyPartInfo
                {
                    gameObject = prefab,
                    name = prefab.name.Replace("BodyPart_", ""),
                    renderer = prefab.GetComponent<SkinnedMeshRenderer>(),
                    category = GuessBodyPartCategory(prefab.name)
                };
                
                bodyParts.Add(info);
            }
            
            List<CharacterBodyPartData> bodyPartDataAssets = CreateBodyPartDataAssets(bodyParts);
            
            // Create base prefab if it doesn't exist
            string basePrefabPath = $"{outputPath}/BaseCharacter.prefab";
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
            
            if (basePrefab == null)
            {
                if (sourcePrefab != null)
                {
                    basePrefab = CreateBaseCharacterPrefab();
                    AddAttachmentPoints(basePrefab);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "BaseCharacter prefab not found and no source prefab assigned!", "OK");
                    return;
                }
            }
            
            CreateCharacterVisualDataAsset(basePrefab, bodyPartDataAssets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", $"Created {bodyPartDataAssets.Count} BodyPartData assets and CharacterVisualData!", "OK");
        }
    }
}

