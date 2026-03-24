using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RogueDeal.NPCs;

namespace RogueDeal.NPCs.Editor
{
    public class DialogTreeEditorWindow : EditorWindow
    {
        private DialogTree _currentTree;
        private Vector2 _offset;
        private Vector2 _drag;
        private float _zoom = 1f;
        
        private DialogNode _selectedNode;
        private DialogNode _connectingNode;
        private int _connectingChoiceIndex = -1;
        
        private DialogNode _draggingNode;
        private Vector2 _dragNodeOffset;
        private bool _isDraggingCanvas;
        
        private Vector2 _rightPanelScroll;
        private const float RightPanelWidth = 350f;
        private const float NodeWidth = 200f;
        private const float NodeHeaderHeight = 30f;
        private const float ChoiceHeight = 25f;
        private const float MinZoom = 0.5f;
        private const float MaxZoom = 2f;
        
        private GUIStyle _nodeStyle;
        private GUIStyle _selectedNodeStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _choiceButtonStyle;
        
        [MenuItem("Tools/Rogue Deal/Dialog Tree Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DialogTreeEditorWindow>("Dialog Tree Editor");
            window.minSize = new Vector2(800, 600);
        }

        public void LoadDialogTree(DialogTree tree)
        {
            if (tree != null)
            {
                _currentTree = tree;
                _selectedNode = null;
                _connectingNode = null;
                _connectingChoiceIndex = -1;
                Repaint();
            }
        }
        
        private void OnEnable()
        {
            InitializeStyles();
        }
        
        private void InitializeStyles()
        {
            if (_nodeStyle != null)
                return; // Already initialized
            
            _nodeStyle = new GUIStyle();
            _nodeStyle.normal.background = MakeTexture(2, 2, new Color(0.2f, 0.2f, 0.25f));
            _nodeStyle.border = new RectOffset(12, 12, 12, 12);
            _nodeStyle.padding = new RectOffset(10, 10, 10, 10);
            
            _selectedNodeStyle = new GUIStyle();
            _selectedNodeStyle.normal.background = MakeTexture(2, 2, new Color(0.3f, 0.5f, 0.8f));
            _selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);
            _selectedNodeStyle.padding = new RectOffset(10, 10, 10, 10);
            
            _headerStyle = new GUIStyle();
            if (EditorStyles.boldLabel != null)
            {
                _headerStyle.font = EditorStyles.boldLabel.font;
                _headerStyle.fontSize = EditorStyles.boldLabel.fontSize;
            }
            _headerStyle.alignment = TextAnchor.MiddleCenter;
            _headerStyle.normal.textColor = Color.white;
            
            _choiceButtonStyle = new GUIStyle();
            if (GUI.skin != null && GUI.skin.button != null)
            {
                _choiceButtonStyle.font = GUI.skin.button.font;
                _choiceButtonStyle.fontSize = GUI.skin.button.fontSize;
            }
            _choiceButtonStyle.alignment = TextAnchor.MiddleLeft;
        }
        
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        
        private void OnGUI()
        {
            if (_nodeStyle == null)
                InitializeStyles();
            
            DrawToolbar();
            
            if (_currentTree == null)
            {
                DrawEmptyState();
                return;
            }
            
            ProcessEvents(Event.current);
            
            Rect canvasRect = new Rect(0, 30, position.width - RightPanelWidth, position.height - 30);
            Rect rightPanelRect = new Rect(position.width - RightPanelWidth, 30, RightPanelWidth, position.height - 30);
            
            BeginWindows();
            DrawCanvas(canvasRect);
            EndWindows();
            
            DrawRightPanel(rightPanelRect);
            
            if (GUI.changed)
                Repaint();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("New Tree", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                CreateNewDialogTree();
            }
            
            if (GUILayout.Button("Load Tree", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                LoadDialogTree();
            }
            
            if (_currentTree != null)
            {
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    SaveCurrentTree();
                }
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("Add Node", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    AddNewNode();
                }
                
                if (_selectedNode != null && GUILayout.Button("Delete Node", EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    DeleteNode(_selectedNode);
                }
                
                GUILayout.FlexibleSpace();
                
                GUILayout.Label(_currentTree.displayName, EditorStyles.toolbarButton);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawEmptyState()
        {
            GUILayout.BeginArea(new Rect(0, 50, position.width, position.height - 50));
            GUILayout.FlexibleSpace();
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUILayout.BeginVertical();
            GUILayout.Label("No Dialog Tree Loaded", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("Create New Dialog Tree", GUILayout.Width(200), GUILayout.Height(30)))
            {
                CreateNewDialogTree();
            }
            
            if (GUILayout.Button("Load Existing Dialog Tree", GUILayout.Width(200), GUILayout.Height(30)))
            {
                LoadDialogTree();
            }
            
            GUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            GUILayout.EndArea();
        }
        
        private void DrawCanvas(Rect canvasRect)
        {
            GUI.Box(canvasRect, "", new GUIStyle { normal = { background = MakeTexture(2, 2, new Color(0.15f, 0.15f, 0.15f)) } });
            
            // Apply zoom and pan transformations
            Vector2 center = new Vector2(canvasRect.x + canvasRect.width / 2, canvasRect.y + canvasRect.height / 2);
            
            // Create transformation matrix: translate to center, scale, translate back with offset
            Matrix4x4 oldMatrix = GUI.matrix;
            Matrix4x4 translation = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one);
            Matrix4x4 scale = Matrix4x4.Scale(new Vector3(_zoom, _zoom, 1f));
            Matrix4x4 translationBack = Matrix4x4.TRS(new Vector3(-center.x + _offset.x, -center.y + _offset.y, 0), Quaternion.identity, Vector3.one);
            
            GUI.matrix = translation * scale * translationBack * oldMatrix;
            
            DrawGrid(canvasRect);
            DrawConnections();
            DrawNodes();
            
            if (_connectingNode != null)
            {
                DrawConnectionLine();
            }
            
            // Restore original matrix
            GUI.matrix = oldMatrix;
            
            // Draw zoom indicator (outside of transformed space)
            GUI.Label(new Rect(canvasRect.x + 10, canvasRect.yMax - 30, 100, 20), 
                $"Zoom: {(_zoom * 100):F0}%", 
                EditorStyles.helpBox);
        }
        
        private void DrawGrid(Rect canvasRect)
        {
            Handles.BeginGUI();
            Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.2f);
            
            float gridSize = 50f;
            
            // Calculate the visible world space bounds
            Vector2 center = canvasRect.size / 2;
            Vector2 topLeft = (Vector2.zero - center) / _zoom - _offset + center;
            Vector2 bottomRight = (canvasRect.size - center) / _zoom - _offset + center;
            
            float startX = topLeft.x;
            float startY = topLeft.y;
            float endX = bottomRight.x;
            float endY = bottomRight.y;
            
            // Draw vertical lines
            int startGridX = Mathf.FloorToInt(startX / gridSize);
            int endGridX = Mathf.CeilToInt(endX / gridSize);
            
            for (int i = startGridX; i <= endGridX; i++)
            {
                float x = i * gridSize;
                Handles.DrawLine(
                    new Vector3(x, startY, 0),
                    new Vector3(x, endY, 0)
                );
            }
            
            // Draw horizontal lines
            int startGridY = Mathf.FloorToInt(startY / gridSize);
            int endGridY = Mathf.CeilToInt(endY / gridSize);
            
            for (int i = startGridY; i <= endGridY; i++)
            {
                float y = i * gridSize;
                Handles.DrawLine(
                    new Vector3(startX, y, 0),
                    new Vector3(endX, y, 0)
                );
            }
            
            Handles.color = Color.white;
            Handles.EndGUI();
        }
        
        private void DrawNodes()
        {
            if (_currentTree == null || _currentTree.nodes == null)
                return;
            
            for (int i = 0; i < _currentTree.nodes.Count; i++)
            {
                DrawNode(_currentTree.nodes[i], i);
            }
        }
        
        private void DrawNode(DialogNode node, int index)
        {
            if (node.editorPosition == default)
            {
                node.editorPosition = new Vector2(100 + index * 250, 100 + (index % 3) * 200);
            }
            
            Rect nodeRect = GetNodeRect(node);
            GUIStyle currentStyle = node == _selectedNode ? _selectedNodeStyle : _nodeStyle;
            
            // Highlight if dragging
            if (node == _draggingNode)
            {
                currentStyle = _selectedNodeStyle;
            }
            
            GUI.Box(nodeRect, "", currentStyle);
            
            GUILayout.BeginArea(nodeRect);
            
            Rect headerRect = new Rect(0, 0, NodeWidth, NodeHeaderHeight);
            GUI.Box(headerRect, "", new GUIStyle { normal = { background = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.15f)) } });
            GUI.Label(headerRect, node.displayName ?? node.nodeId ?? "Unnamed Node", _headerStyle);
            
            GUILayout.Space(NodeHeaderHeight + 5);
            
            GUILayout.Label($"Speaker: {node.speaker}", EditorStyles.miniLabel);
            
            string preview = node.text?.Length > 50 ? node.text.Substring(0, 50) + "..." : node.text;
            GUILayout.Label(preview, EditorStyles.wordWrappedMiniLabel);
            
            GUILayout.Space(10);
            
            if (node.choices != null && node.choices.Count > 0)
            {
                GUILayout.Label("Choices:", EditorStyles.miniBoldLabel);
                for (int i = 0; i < node.choices.Count; i++)
                {
                    if (GUILayout.Button($"→ {node.choices[i].text}", _choiceButtonStyle, GUILayout.Height(ChoiceHeight)))
                    {
                        StartConnection(node, i);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(node.nextNodeId))
            {
                if (GUILayout.Button("→ Continue", _choiceButtonStyle, GUILayout.Height(ChoiceHeight)))
                {
                    StartConnection(node, -1);
                }
            }
            else if (node.isEndNode)
            {
                GUILayout.Label("[ END ]", EditorStyles.centeredGreyMiniLabel);
            }
            
            GUILayout.EndArea();
        }
        
        private Rect GetNodeRect(DialogNode node)
        {
            float height = NodeHeaderHeight + 60;
            
            if (node.choices != null && node.choices.Count > 0)
            {
                height += 20 + (node.choices.Count * (ChoiceHeight + 2));
            }
            else if (!string.IsNullOrEmpty(node.nextNodeId))
            {
                height += ChoiceHeight + 5;
            }
            else if (node.isEndNode)
            {
                height += 20;
            }
            
            return new Rect(
                node.editorPosition.x,
                node.editorPosition.y,
                NodeWidth,
                height
            );
        }
        
        private void DrawConnections()
        {
            if (_currentTree == null || _currentTree.nodes == null)
                return;
            
            Handles.BeginGUI();
            
            foreach (var node in _currentTree.nodes)
            {
                if (node.choices != null)
                {
                    for (int i = 0; i < node.choices.Count; i++)
                    {
                        var choice = node.choices[i];
                        if (!string.IsNullOrEmpty(choice.nextNodeId))
                        {
                            var targetNode = _currentTree.GetNode(choice.nextNodeId);
                            if (targetNode != null)
                            {
                                DrawConnection(node, targetNode, i, new Color(0.5f, 0.8f, 1f));
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(node.nextNodeId))
                {
                    var targetNode = _currentTree.GetNode(node.nextNodeId);
                    if (targetNode != null)
                    {
                        DrawConnection(node, targetNode, -1, new Color(0.8f, 0.8f, 0.8f));
                    }
                }
            }
            
            Handles.EndGUI();
        }
        
        private void DrawConnection(DialogNode from, DialogNode to, int choiceIndex, Color color)
        {
            Rect fromRect = GetNodeRect(from);
            Rect toRect = GetNodeRect(to);
            
            Vector3 startPos = new Vector3(fromRect.xMax, fromRect.y + fromRect.height / 2);
            Vector3 endPos = new Vector3(toRect.x, toRect.y + toRect.height / 2);
            
            if (choiceIndex >= 0 && from.choices != null && choiceIndex < from.choices.Count)
            {
                float choiceYOffset = NodeHeaderHeight + 75 + (choiceIndex * (ChoiceHeight + 2)) + ChoiceHeight / 2;
                startPos.y = fromRect.y + choiceYOffset;
            }
            
            Handles.DrawBezier(
                startPos,
                endPos,
                startPos + Vector3.right * 50,
                endPos + Vector3.left * 50,
                color,
                null,
                3f
            );
            
            Vector3 arrowPos = endPos + Vector3.left * 10;
            Handles.color = color;
            Handles.DrawSolidDisc(arrowPos, Vector3.forward, 4f);
        }
        
        private void DrawConnectionLine()
        {
            if (_connectingNode == null)
                return;
            
            Rect fromRect = GetNodeRect(_connectingNode);
            Vector3 startPos = new Vector3(fromRect.xMax, fromRect.y + fromRect.height / 2, 0);
            
            // Get mouse position in world space (inverse of our transformation)
            Rect canvasRect = new Rect(0, 30, position.width - RightPanelWidth, position.height - 30);
            Vector2 center = canvasRect.size / 2;
            Vector2 localMouse = Event.current.mousePosition;
            Vector2 worldMouse = (localMouse - center) / _zoom - _offset + center;
            Vector3 endPos = new Vector3(worldMouse.x, worldMouse.y, 0);
            
            Handles.BeginGUI();
            Handles.DrawBezier(
                startPos,
                endPos,
                startPos + Vector3.right * 50,
                endPos + Vector3.left * 50,
                Color.yellow,
                null,
                2f
            );
            Handles.EndGUI();
            
            Repaint();
        }
        
        private void DrawRightPanel(Rect panelRect)
        {
            GUI.Box(panelRect, "", new GUIStyle { normal = { background = MakeTexture(2, 2, new Color(0.18f, 0.18f, 0.18f)) } });
            
            GUILayout.BeginArea(panelRect);
            _rightPanelScroll = GUILayout.BeginScrollView(_rightPanelScroll);
            
            GUILayout.Space(10);
            
            if (_selectedNode != null)
            {
                DrawNodeProperties();
            }
            else
            {
                DrawTreeProperties();
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        
        private void DrawTreeProperties()
        {
            GUILayout.Label("Dialog Tree Properties", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUI.BeginChangeCheck();
            
            _currentTree.displayName = EditorGUILayout.TextField("Display Name", _currentTree.displayName);
            _currentTree.dialogId = EditorGUILayout.TextField("Dialog ID", _currentTree.dialogId);
            _currentTree.entryNodeId = EditorGUILayout.TextField("Entry Node ID", _currentTree.entryNodeId);
            
            EditorGUILayout.Space();
            GUILayout.Label($"Total Nodes: {_currentTree.nodes?.Count ?? 0}", EditorStyles.helpBox);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_currentTree);
            }
        }
        
        private void DrawNodeProperties()
        {
            GUILayout.Label("Node Properties", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUI.BeginChangeCheck();
            
            _selectedNode.displayName = EditorGUILayout.TextField("Display Name", _selectedNode.displayName);
            _selectedNode.nodeId = EditorGUILayout.TextField("Node ID", _selectedNode.nodeId);
            
            EditorGUILayout.Space();
            _selectedNode.speaker = EditorGUILayout.TextField("Speaker", _selectedNode.speaker);
            _selectedNode.text = EditorGUILayout.TextArea(_selectedNode.text, GUILayout.Height(80));
            
            EditorGUILayout.Space();
            _selectedNode.speakerPortrait = (Sprite)EditorGUILayout.ObjectField("Portrait", _selectedNode.speakerPortrait, typeof(Sprite), false);
            
            EditorGUILayout.Space();
            GUILayout.Label("Navigation", EditorStyles.boldLabel);
            _selectedNode.isEndNode = EditorGUILayout.Toggle("Is End Node", _selectedNode.isEndNode);
            
            if (!_selectedNode.isEndNode && (_selectedNode.choices == null || _selectedNode.choices.Count == 0))
            {
                _selectedNode.nextNodeId = EditorGUILayout.TextField("Next Node ID", _selectedNode.nextNodeId);
            }
            
            EditorGUILayout.Space();
            DrawChoicesSection();
            
            EditorGUILayout.Space();
            DrawActionsSection();
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_currentTree);
            }
        }
        
        private void DrawChoicesSection()
        {
            GUILayout.Label("Choices", EditorStyles.boldLabel);
            
            if (_selectedNode.choices == null)
                _selectedNode.choices = new List<DialogChoice>();
            
            for (int i = 0; i < _selectedNode.choices.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Choice {i + 1}", EditorStyles.miniBoldLabel);
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    _selectedNode.choices.RemoveAt(i);
                    EditorUtility.SetDirty(_currentTree);
                    return;
                }
                EditorGUILayout.EndHorizontal();
                
                _selectedNode.choices[i].text = EditorGUILayout.TextField("Text", _selectedNode.choices[i].text);
                _selectedNode.choices[i].nextNodeId = EditorGUILayout.TextField("Next Node", _selectedNode.choices[i].nextNodeId);
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            if (GUILayout.Button("+ Add Choice"))
            {
                _selectedNode.choices.Add(new DialogChoice { text = "New Choice" });
                EditorUtility.SetDirty(_currentTree);
            }
        }
        
        private void DrawActionsSection()
        {
            GUILayout.Label("Actions", EditorStyles.boldLabel);
            
            if (_selectedNode.actions == null)
                _selectedNode.actions = new List<DialogAction>();
            
            for (int i = 0; i < _selectedNode.actions.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Action {i + 1}", EditorStyles.miniBoldLabel);
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    _selectedNode.actions.RemoveAt(i);
                    EditorUtility.SetDirty(_currentTree);
                    return;
                }
                EditorGUILayout.EndHorizontal();
                
                _selectedNode.actions[i].actionType = (DialogActionType)EditorGUILayout.EnumPopup("Type", _selectedNode.actions[i].actionType);
                
                switch (_selectedNode.actions[i].actionType)
                {
                    case DialogActionType.StartQuest:
                    case DialogActionType.CompleteQuest:
                        _selectedNode.actions[i].questId = EditorGUILayout.TextField("Quest ID", _selectedNode.actions[i].questId);
                        break;
                    case DialogActionType.GiveItem:
                    case DialogActionType.TakeItem:
                        _selectedNode.actions[i].itemId = EditorGUILayout.TextField("Item ID", _selectedNode.actions[i].itemId);
                        _selectedNode.actions[i].itemAmount = EditorGUILayout.IntField("Amount", _selectedNode.actions[i].itemAmount);
                        break;
                    case DialogActionType.GiveGold:
                    case DialogActionType.TakeGold:
                        _selectedNode.actions[i].goldAmount = EditorGUILayout.IntField("Gold Amount", _selectedNode.actions[i].goldAmount);
                        break;
                    case DialogActionType.SwitchDialogTree:
                        _selectedNode.actions[i].targetDialogTree = (DialogTree)EditorGUILayout.ObjectField(
                            "Target Dialog Tree", 
                            _selectedNode.actions[i].targetDialogTree, 
                            typeof(DialogTree), 
                            false
                        );
                        _selectedNode.actions[i].closeBeforeSwitch = EditorGUILayout.Toggle(
                            "Close Before Switch", 
                            _selectedNode.actions[i].closeBeforeSwitch
                        );
                        EditorGUILayout.HelpBox(
                            "If 'Close Before Switch' is true, the current dialog will end and the new tree will start fresh. " +
                            "If false, the dialog continues seamlessly into the new tree.",
                            MessageType.Info
                        );
                        break;
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            if (GUILayout.Button("+ Add Action"))
            {
                _selectedNode.actions.Add(new DialogAction());
                EditorUtility.SetDirty(_currentTree);
            }
        }
        
        private void ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0) // Left click
                    {
                        if (_connectingNode != null)
                        {
                            TryCompleteConnection();
                        }
                        else
                        {
                            // Check if clicking on a node
                            DialogNode clickedNode = GetNodeAtPosition(e.mousePosition);
                            if (clickedNode != null)
                            {
                                _selectedNode = clickedNode;
                                _draggingNode = clickedNode;
                                
                                Rect nodeRect = GetNodeRect(clickedNode);
                                _dragNodeOffset = e.mousePosition - nodeRect.position;
                                
                                e.Use();
                                GUI.changed = true;
                            }
                        }
                    }
                    else if (e.button == 1) // Right click
                    {
                        CancelConnection();
                    }
                    else if (e.button == 2) // Middle click
                    {
                        _isDraggingCanvas = true;
                        e.Use();
                    }
                    break;
                    
                case EventType.MouseUp:
                    if (e.button == 0)
                    {
                        _draggingNode = null;
                        e.Use();
                    }
                    else if (e.button == 2)
                    {
                        _isDraggingCanvas = false;
                        e.Use();
                    }
                    break;
                    
                case EventType.MouseDrag:
                    if (_draggingNode != null && e.button == 0)
                    {
                        OnDragNode(e.delta);
                        e.Use();
                    }
                    else if (_isDraggingCanvas || e.button == 2)
                    {
                        OnDrag(e.delta);
                        e.Use();
                    }
                    break;
                    
                case EventType.ScrollWheel:
                    OnZoom(-e.delta.y);
                    e.Use();
                    break;
            }
        }
        
        private DialogNode GetNodeAtPosition(Vector2 mousePos)
        {
            if (_currentTree == null || _currentTree.nodes == null)
                return null;
            
            // Convert screen position to world position
            Rect canvasRect = new Rect(0, 30, position.width - RightPanelWidth, position.height - 30);
            
            // Calculate the center of the canvas in screen space
            Vector2 center = new Vector2(canvasRect.x + canvasRect.width / 2, canvasRect.y + canvasRect.height / 2);
            
            // Inverse transform: reverse the matrix transformation
            // Original: translation * scale * translationBack
            // Inverse: apply translationBack inverse, scale inverse, translation inverse
            Vector2 worldPos = (mousePos - center) / _zoom - _offset + center;
            
            // Check in reverse order so top nodes are selected first
            for (int i = _currentTree.nodes.Count - 1; i >= 0; i--)
            {
                Rect nodeRect = GetNodeRect(_currentTree.nodes[i]);
                if (nodeRect.Contains(worldPos))
                {
                    return _currentTree.nodes[i];
                }
            }
            
            return null;
        }
        
        private void OnDragNode(Vector2 delta)
        {
            if (_draggingNode != null)
            {
                _draggingNode.editorPosition += delta / _zoom;
                EditorUtility.SetDirty(_currentTree);
                GUI.changed = true;
            }
        }
        
        private void OnDrag(Vector2 delta)
        {
            _offset += delta;
            GUI.changed = true;
        }
        
        private void OnZoom(float delta)
        {
            float oldZoom = _zoom;
            _zoom = Mathf.Clamp(_zoom + delta * 0.01f, MinZoom, MaxZoom);
            
            if (Mathf.Abs(oldZoom - _zoom) > 0.001f)
            {
                Repaint();
            }
        }
        
        private void StartConnection(DialogNode node, int choiceIndex)
        {
            _connectingNode = node;
            _connectingChoiceIndex = choiceIndex;
            Event.current.Use();
        }
        
        private void TryCompleteConnection()
        {
            if (_selectedNode != null && _selectedNode != _connectingNode)
            {
                if (_connectingChoiceIndex >= 0)
                {
                    _connectingNode.choices[_connectingChoiceIndex].nextNodeId = _selectedNode.nodeId;
                }
                else
                {
                    _connectingNode.nextNodeId = _selectedNode.nodeId;
                }
                EditorUtility.SetDirty(_currentTree);
            }
            CancelConnection();
        }
        
        private void CancelConnection()
        {
            _connectingNode = null;
            _connectingChoiceIndex = -1;
            GUI.changed = true;
        }
        
        private void CreateNewDialogTree()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Dialog Tree",
                "Dialog_NewDialog",
                "asset",
                "Choose a location for the new dialog tree",
                "Assets/RogueDeal/Resources/NPCs"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                DialogTree newTree = CreateInstance<DialogTree>();
                newTree.displayName = "New Dialog";
                newTree.dialogId = "new_dialog";
                
                DialogNode firstNode = new DialogNode
                {
                    nodeId = "start",
                    displayName = "Start",
                    speaker = "NPC",
                    text = "Hello! This is the first node.",
                    editorPosition = new Vector2(100, 100)
                };
                
                newTree.nodes = new List<DialogNode> { firstNode };
                newTree.entryNodeId = "start";
                
                AssetDatabase.CreateAsset(newTree, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                _currentTree = newTree;
                _selectedNode = firstNode;
            }
        }
        
        private void LoadDialogTree()
        {
            string path = EditorUtility.OpenFilePanel(
                "Load Dialog Tree",
                "Assets/RogueDeal/Resources/NPCs",
                "asset"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                int assetsIndex = path.IndexOf("Assets");
                if (assetsIndex >= 0)
                {
                    path = path.Substring(assetsIndex);
                    _currentTree = AssetDatabase.LoadAssetAtPath<DialogTree>(path);
                    _selectedNode = null;
                }
            }
        }
        
        private void SaveCurrentTree()
        {
            if (_currentTree != null)
            {
                EditorUtility.SetDirty(_currentTree);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Saved dialog tree: {_currentTree.displayName}");
            }
        }
        
        private void AddNewNode()
        {
            if (_currentTree == null)
                return;
            
            DialogNode newNode = new DialogNode
            {
                nodeId = $"node_{_currentTree.nodes.Count + 1}",
                displayName = $"Node {_currentTree.nodes.Count + 1}",
                speaker = "NPC",
                text = "New dialog text here...",
                editorPosition = new Vector2(
                    200 + (_currentTree.nodes.Count % 4) * 250,
                    100 + (_currentTree.nodes.Count / 4) * 200
                )
            };
            
            _currentTree.nodes.Add(newNode);
            _selectedNode = newNode;
            EditorUtility.SetDirty(_currentTree);
        }
        
        private void DeleteNode(DialogNode node)
        {
            if (_currentTree == null || node == null)
                return;
            
            if (EditorUtility.DisplayDialog(
                "Delete Node",
                $"Are you sure you want to delete '{node.displayName}'?",
                "Delete",
                "Cancel"))
            {
                _currentTree.nodes.Remove(node);
                
                foreach (var n in _currentTree.nodes)
                {
                    if (n.nextNodeId == node.nodeId)
                        n.nextNodeId = "";
                    
                    if (n.choices != null)
                    {
                        foreach (var choice in n.choices)
                        {
                            if (choice.nextNodeId == node.nodeId)
                                choice.nextNodeId = "";
                        }
                    }
                }
                
                if (_currentTree.entryNodeId == node.nodeId)
                {
                    _currentTree.entryNodeId = _currentTree.nodes.Count > 0 ? _currentTree.nodes[0].nodeId : "";
                }
                
                _selectedNode = null;
                EditorUtility.SetDirty(_currentTree);
            }
        }
    }
}
