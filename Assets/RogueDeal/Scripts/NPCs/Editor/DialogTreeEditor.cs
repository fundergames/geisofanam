using UnityEditor;
using UnityEngine;

namespace RogueDeal.NPCs.Editor
{
    [CustomEditor(typeof(DialogTree))]
    public class DialogTreeEditor : UnityEditor.Editor
    {
        private SerializedProperty _dialogId;
        private SerializedProperty _displayName;
        private SerializedProperty _nodes;
        private SerializedProperty _entryNodeId;

        private void OnEnable()
        {
            _dialogId = serializedObject.FindProperty("dialogId");
            _displayName = serializedObject.FindProperty("displayName");
            _nodes = serializedObject.FindProperty("nodes");
            _entryNodeId = serializedObject.FindProperty("entryNodeId");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DialogTree dialogTree = (DialogTree)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dialog Tree", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_displayName);
            EditorGUILayout.PropertyField(_dialogId);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Entry Point", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_entryNodeId);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dialog Nodes", EditorStyles.boldLabel);

            if (GUILayout.Button("Add New Node", GUILayout.Height(30)))
            {
                AddNewNode(dialogTree);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_nodes, true);

            EditorGUILayout.Space();
            
            if (_nodes.arraySize > 0)
            {
                EditorGUILayout.HelpBox($"Total Nodes: {_nodes.arraySize}\nEntry Node: {_entryNodeId.stringValue}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No dialog nodes defined. Click 'Add New Node' to create your first node.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddNewNode(DialogTree dialogTree)
        {
            Undo.RecordObject(dialogTree, "Add Dialog Node");
            
            DialogNode newNode = new DialogNode
            {
                nodeId = $"node_{dialogTree.nodes.Count}",
                displayName = $"Node {dialogTree.nodes.Count}",
                speaker = "NPC",
                text = "Enter dialog text here...",
                isEndNode = false
            };

            dialogTree.nodes.Add(newNode);
            
            if (dialogTree.nodes.Count == 1)
            {
                dialogTree.entryNodeId = newNode.nodeId;
            }

            EditorUtility.SetDirty(dialogTree);
        }
    }
}
