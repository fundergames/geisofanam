using UnityEngine;
using UnityEngine.SceneManagement;

namespace RogueDeal.Combat
{
    public class RuntimeSceneInspector : MonoBehaviour
    {
        [ContextMenu("List All Scenes and Objects")]
        public void ListAllScenesAndObjects()
        {
            Debug.Log("=== RUNTIME SCENE INSPECTOR ===");
            
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                Debug.Log($"Scene {i}: {scene.name} (loaded: {scene.isLoaded})");
                
                if (scene.isLoaded)
                {
                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    Debug.Log($"  Root objects: {rootObjects.Length}");
                    
                    foreach (GameObject rootObj in rootObjects)
                    {
                        LogGameObjectHierarchy(rootObj, 2);
                    }
                }
            }
            
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Debug.Log($"\nTotal cameras in all scenes: {cameras.Length}");
            foreach (Camera cam in cameras)
            {
                Debug.Log($"  Camera: {cam.name}, tag: {cam.tag}, position: {cam.transform.position}");
            }
            
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            Debug.Log($"\nTotal GameObjects: {allObjects.Length}");
            
            var clones = System.Array.FindAll(allObjects, obj => obj.name.Contains("Clone"));
            Debug.Log($"Objects with 'Clone' in name: {clones.Length}");
            foreach (var clone in clones)
            {
                MeshRenderer mr = clone.GetComponent<MeshRenderer>();
                Debug.Log($"  {clone.name} - active: {clone.activeSelf}, parent: {(clone.transform.parent != null ? clone.transform.parent.name : "ROOT")}, pos: {clone.transform.position}, MeshRenderer: {mr != null && mr.enabled}");
            }
            
            Debug.Log("=== END INSPECTOR ===");
        }
        
        private void LogGameObjectHierarchy(GameObject obj, int indent)
        {
            string indentStr = new string(' ', indent);
            
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            string rendererInfo = meshRenderer != null ? $"MeshRenderer: {meshRenderer.enabled}" : "";
            
            Debug.Log($"{indentStr}- {obj.name} (active: {obj.activeSelf}, pos: {obj.transform.position}) {rendererInfo}");
            
            foreach (Transform child in obj.transform)
            {
                LogGameObjectHierarchy(child.gameObject, indent + 2);
            }
        }
        
        private void Start()
        {
            Invoke(nameof(ListAllScenesAndObjects), 1f);
        }
    }
}
