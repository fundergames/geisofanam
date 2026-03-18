using UnityEditor;
using UnityEngine;

namespace RogueDeal.NPCs.Editor
{
    public class NPCSetupWizard : EditorWindow
    {
        private string _npcName = "NewNPC";
        private NPCDefinition _npcDefinition;
        private GameObject _interactionPromptPrefab;
        private Vector3 _spawnPosition = Vector3.zero;
        private float _colliderRadius = 2.5f;

        [MenuItem("Tools/Rogue Deal/NPC Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<NPCSetupWizard>("NPC Setup Wizard");
            window.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("NPC Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Quickly create an interactable NPC in your scene.", MessageType.Info);
            EditorGUILayout.Space();

            _npcName = EditorGUILayout.TextField("NPC Name", _npcName);
            _npcDefinition = (NPCDefinition)EditorGUILayout.ObjectField("NPC Definition", _npcDefinition, typeof(NPCDefinition), false);
            _interactionPromptPrefab = (GameObject)EditorGUILayout.ObjectField("Interaction Prompt Prefab", _interactionPromptPrefab, typeof(GameObject), false);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
            _spawnPosition = EditorGUILayout.Vector3Field("Spawn Position", _spawnPosition);
            _colliderRadius = EditorGUILayout.FloatField("Interaction Radius", _colliderRadius);

            EditorGUILayout.Space();

            GUI.enabled = !string.IsNullOrEmpty(_npcName);
            
            if (GUILayout.Button("Create NPC", GUILayout.Height(40)))
            {
                CreateNPC();
            }

            GUI.enabled = true;

            EditorGUILayout.Space();
            
            if (GUILayout.Button("Create NPC Definition Asset"))
            {
                CreateNPCDefinition();
            }

            if (GUILayout.Button("Create Dialog Tree Asset"))
            {
                CreateDialogTree();
            }
        }

        private void CreateNPC()
        {
            GameObject npcObject = new GameObject(_npcName);
            npcObject.transform.position = _spawnPosition;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(npcObject.transform);
            visual.transform.localPosition = Vector3.zero;
            DestroyImmediate(visual.GetComponent<Collider>());

            CapsuleCollider trigger = npcObject.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.radius = _colliderRadius;
            trigger.height = 2f;

            NPCInteractable interactable = npcObject.AddComponent<NPCInteractable>();
            
            if (_npcDefinition != null)
            {
                interactable.SetNPCDefinition(_npcDefinition);
            }

            DialogController dialogController = npcObject.AddComponent<DialogController>();

            Selection.activeGameObject = npcObject;
            EditorGUIUtility.PingObject(npcObject);

            Debug.Log($"Created NPC: {_npcName} at position {_spawnPosition}");
        }

        private void CreateNPCDefinition()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create NPC Definition",
                $"NPC_{_npcName}",
                "asset",
                "Choose where to save the NPC Definition",
                "Assets/RogueDeal/Resources/NPCs"
            );

            if (!string.IsNullOrEmpty(path))
            {
                NPCDefinition asset = CreateInstance<NPCDefinition>();
                asset.displayName = _npcName;
                asset.npcId = _npcName.ToLower().Replace(" ", "_");
                asset.interactionRange = _colliderRadius;

                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                
                _npcDefinition = asset;
                
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
                
                Debug.Log($"Created NPC Definition at: {path}");
            }
        }

        private void CreateDialogTree()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Dialog Tree",
                $"Dialog_{_npcName}",
                "asset",
                "Choose where to save the Dialog Tree",
                "Assets/RogueDeal/Resources/NPCs"
            );

            if (!string.IsNullOrEmpty(path))
            {
                DialogTree asset = CreateInstance<DialogTree>();
                asset.displayName = $"{_npcName} Dialog";
                asset.dialogId = $"{_npcName.ToLower()}_dialog";

                DialogNode greetingNode = new DialogNode
                {
                    nodeId = "greeting",
                    displayName = "Greeting",
                    speaker = _npcName,
                    text = $"Hello! I am {_npcName}.",
                    isEndNode = true
                };

                asset.nodes.Add(greetingNode);
                asset.entryNodeId = "greeting";

                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
                
                Debug.Log($"Created Dialog Tree at: {path}");
            }
        }
    }
}
