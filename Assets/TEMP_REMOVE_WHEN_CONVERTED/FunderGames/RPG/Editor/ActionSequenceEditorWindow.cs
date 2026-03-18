using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;

namespace FunderGames.RPG
{
    public class ActionSequenceEditorWindow : EditorWindow
    {
        private ActionSequence actionSequence;
        private List<ActionStep> availableSteps = new List<ActionStep>();
        private GameObject performerPrefab;  // Predefined asset (prefab) for the performer
        private GameObject targetPrefab;     // Predefined asset (prefab) for the target
        private GameObject performerInstance;
        private GameObject targetInstance;
        private bool isExecuting = false;
        private float executionProgress = 0f;
        private Camera previewCamera;
        private RenderTexture renderTexture;

        // Transform properties for performer and target
        private Vector3 performerPosition = new Vector3(-4, 0, 0);
        private Vector3 targetPosition = new Vector3(4, 0, 0);
        private Vector3 performerRotation = new Vector3(0, 90, 0);
        private Vector3 targetRotation = new Vector3(0, -90, 0);
        private Vector3 performerScale = Vector3.one * 2f;
        private Vector3 targetScale = Vector3.one * 2f;

        [MenuItem("FunderGames/Action Sequence Editor")]
        public static void ShowWindow()
        {
            GetWindow<ActionSequenceEditorWindow>("Action Sequence Editor");
        }

        private void OnEnable()
        {
            EditorApplication.update += UpdatePreview;
            FindAllActionSteps();
            InitializePreviewCamera();
        }

        private void OnDisable()
        {
            CleanupInstances();
            EditorApplication.update -= UpdatePreview;

            if (renderTexture != null)
            {
                renderTexture.Release();
                DestroyImmediate(renderTexture);
            }

            if (previewCamera != null)
            {
                DestroyImmediate(previewCamera.gameObject);
            }

            Debug.Log("Action Sequence Editor: Resources cleaned up.");
        }

        private void CleanupInstances()
        {
            if (performerInstance != null) DestroyImmediate(performerInstance);
            if (targetInstance != null) DestroyImmediate(targetInstance);
        }

        private void OnGUI()
        {
            GUILayout.Label("Action Sequence Editor", EditorStyles.boldLabel);

            // Select predefined character assets (prefabs)
            performerPrefab = (GameObject)EditorGUILayout.ObjectField("Performer Prefab", performerPrefab, typeof(GameObject), false);
            targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Prefab", targetPrefab, typeof(GameObject), false);

            // Position, Rotation, and Scale controls for the Performer
            GUILayout.Label("Performer Transform", EditorStyles.boldLabel);
            performerPosition = EditorGUILayout.Vector3Field("Position", performerPosition);
            performerRotation = EditorGUILayout.Vector3Field("Rotation", performerRotation);
            performerScale = EditorGUILayout.Vector3Field("Scale", performerScale);

            // Position, Rotation, and Scale controls for the Target
            GUILayout.Label("Target Transform", EditorStyles.boldLabel);
            targetPosition = EditorGUILayout.Vector3Field("Position", targetPosition);
            targetRotation = EditorGUILayout.Vector3Field("Rotation", targetRotation);
            targetScale = EditorGUILayout.Vector3Field("Scale", targetScale);

            // Show available steps and add them to the sequence
            GUILayout.Label("Available Action Steps", EditorStyles.boldLabel);
            foreach (var step in availableSteps)
            {
                if (GUILayout.Button($"Add {step.name}"))
                {
                    if (actionSequence == null)
                    {
                        actionSequence = ScriptableObject.CreateInstance<ActionSequence>();
                    }
                    actionSequence.steps.Add(step);
                    Debug.Log($"{step.name} added to sequence.");
                }
            }

            // Display the current action sequence
            GUILayout.Label("Current Action Sequence", EditorStyles.boldLabel);
            if (actionSequence != null)
            {
                for (int i = 0; i < actionSequence.steps.Count; i++)
                {
                    GUILayout.Label($"{i + 1}. {actionSequence.steps[i].name}");
                }

                if (GUILayout.Button("Clear Sequence"))
                {
                    actionSequence.steps.Clear();
                    Debug.Log("Action Sequence cleared.");
                }
            }

            // Execute the sequence
            if (!isExecuting && GUILayout.Button("Execute Sequence"))
            {
                if (performerPrefab != null && targetPrefab != null && actionSequence != null)
                {
                    InstantiateCharacters();
                    StartExecution();
                }
                else
                {
                    Debug.LogWarning("Please select performer prefab, target prefab, and create a sequence first.");
                }
            }

            // Render the preview area
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            RenderPreviewScene();

            // Show the progress of the execution
            if (isExecuting)
            {
                EditorGUILayout.LabelField("Execution Progress", $"{executionProgress * 100}%");
                Repaint();  // Repaint the window to update the progress bar

                if (GUILayout.Button("Stop Execution"))
                {
                    StopExecution();
                }
            }
        }

        private void FindAllActionSteps()
        {
            string[] guids = AssetDatabase.FindAssets("t:ActionStep");
            availableSteps.Clear();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ActionStep actionStep = AssetDatabase.LoadAssetAtPath<ActionStep>(path);
                if (actionStep != null)
                {
                    availableSteps.Add(actionStep);
                }
            }
            Debug.Log($"Found {availableSteps.Count} ActionSteps in the project.");
        }

        private void InitializePreviewCamera()
        {
            renderTexture = new RenderTexture(1024, 1024, 16);
            previewCamera = new GameObject("PreviewCamera").AddComponent<Camera>();
            previewCamera.targetTexture = renderTexture;
            previewCamera.transform.position = new Vector3(0, 5, -10);
            previewCamera.transform.LookAt(Vector3.zero);
        }

        private void RenderPreviewScene()
        {
            Rect previewRect = GUILayoutUtility.GetRect(position.width - 20, position.height - 200, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.DrawTexture(previewRect, renderTexture, ScaleMode.ScaleToFit, false);

            if (previewCamera != null && performerInstance != null && targetInstance != null)
            {
                previewCamera.Render();
            }
        }

        private void InstantiateCharacters()
        {
            CleanupInstances();  // Clean up any existing instances before instantiating new ones

            // Instantiate the performer and target with user-defined positions, rotations, and scales
            performerInstance = Instantiate(performerPrefab, performerPosition, Quaternion.Euler(performerRotation));
            performerInstance.transform.localScale = performerScale;

            targetInstance = Instantiate(targetPrefab, targetPosition, Quaternion.Euler(targetRotation));
            targetInstance.transform.localScale = targetScale;

            Debug.Log("Performer and Target instantiated in the preview scene.");
        }

        private void StartExecution()
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(ExecuteSequence());
        }

        private void StopExecution()
        {
            isExecuting = false;
            executionProgress = 0f;
            Debug.Log("Execution stopped.");
        }

        private IEnumerator ExecuteSequence()
        {
            isExecuting = true;
            executionProgress = 0f;

            for (int i = 0; i < actionSequence.steps.Count; i++)
            {
                ActionStep step = actionSequence.steps[i];
                Debug.Log($"Executing step {i + 1}: {step.name}");

                yield return step.Execute(performerInstance.GetComponent<Combatant>(), targetInstance.GetComponent<Combatant>());

                executionProgress = (float)(i + 1) / actionSequence.steps.Count;
            }

            Debug.Log("Action Sequence Execution Completed.");
            isExecuting = false;
        }

        private void UpdatePreview()
        {
            if (!Application.isPlaying && performerInstance != null)
            {
                Animator animator = performerInstance.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.Update(Time.deltaTime);
                }
            }

            Repaint();  // Repaint the editor window to keep visuals updated
        }
    }
}
