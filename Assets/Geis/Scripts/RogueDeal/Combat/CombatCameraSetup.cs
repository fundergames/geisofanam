using UnityEngine;

namespace RogueDeal.Combat
{
    public class CombatCameraSetup : MonoBehaviour
    {
        [Header("Camera Position")]
        [SerializeField] private Vector3 cameraPosition = new Vector3(0f, 5f, -8f);
        [SerializeField] private Vector3 lookAtPosition = new Vector3(0f, 1f, 0f);
        
        private void Start()
        {
            SetupCamera();
        }
        
        private void SetupCamera()
        {
            Camera mainCamera = Camera.main;
            
            if (mainCamera == null)
            {
                Debug.LogError("[CombatCameraSetup] Main Camera not found!");
                return;
            }
            
            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.LookAt(lookAtPosition);
            
            Debug.Log($"[CombatCameraSetup] Camera positioned at {cameraPosition}, looking at {lookAtPosition}");
        }
    }
}
