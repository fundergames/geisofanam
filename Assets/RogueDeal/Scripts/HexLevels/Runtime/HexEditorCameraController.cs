using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueDeal.HexLevels.Runtime
{
    public class HexEditorCameraController : MonoBehaviour
    {
        [Header("Movement")]
        public float panSpeed = 10f;
        public float zoomSpeed = 10f;
        public float rotationSpeed = 100f;
        
        [Header("Limits")]
        public float minZoom = 5f;
        public float maxZoom = 50f;
        public float minHeight = 2f;
        public float maxHeight = 100f;
        
        [Header("Input")]
        public InputActionReference panAction;
        public InputActionReference zoomAction;
        public InputActionReference rotateAction;
        public InputActionReference middleMouseButton;
        
        private Camera cam;
        private Vector3 targetPosition;
        private float targetHeight;
        private float targetRotationY;
        
        private void Awake()
        {
            cam = GetComponent<Camera>();
            targetPosition = transform.position;
            targetHeight = transform.position.y;
            targetRotationY = transform.eulerAngles.y;
        }
        
        private void OnEnable()
        {
            panAction?.action?.Enable();
            zoomAction?.action?.Enable();
            rotateAction?.action?.Enable();
            middleMouseButton?.action?.Enable();
        }
        
        private void OnDisable()
        {
            panAction?.action?.Disable();
            zoomAction?.action?.Disable();
            rotateAction?.action?.Disable();
            middleMouseButton?.action?.Disable();
        }
        
        private void Update()
        {
            HandlePan();
            HandleZoom();
            HandleRotation();
            
            SmoothMovement();
        }
        
        private void HandlePan()
        {
            if (panAction?.action == null)
                return;
            
            Vector2 input = panAction.action.ReadValue<Vector2>();
            
            if (input.magnitude > 0.1f)
            {
                Vector3 forward = transform.forward;
                forward.y = 0;
                forward.Normalize();
                
                Vector3 right = transform.right;
                right.y = 0;
                right.Normalize();
                
                Vector3 move = (forward * input.y + right * input.x) * panSpeed * Time.deltaTime;
                targetPosition += move;
            }
        }
        
        private void HandleZoom()
        {
            if (zoomAction?.action == null)
                return;
            
            float scrollValue = zoomAction.action.ReadValue<float>();
            
            if (Mathf.Abs(scrollValue) > 0.1f)
            {
                targetHeight -= scrollValue * zoomSpeed * Time.deltaTime;
                targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);
            }
        }
        
        private void HandleRotation()
        {
            if (rotateAction?.action == null || middleMouseButton?.action == null)
                return;
            
            if (middleMouseButton.action.IsPressed())
            {
                float rotationInput = rotateAction.action.ReadValue<float>();
                
                if (Mathf.Abs(rotationInput) > 0.1f)
                {
                    targetRotationY += rotationInput * rotationSpeed * Time.deltaTime;
                }
            }
        }
        
        private void SmoothMovement()
        {
            Vector3 newPos = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
            newPos.y = Mathf.Lerp(transform.position.y, targetHeight, Time.deltaTime * 10f);
            transform.position = newPos;
            
            float newRotY = Mathf.LerpAngle(transform.eulerAngles.y, targetRotationY, Time.deltaTime * 5f);
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, newRotY, 0);
        }
    }
}
