using UnityEngine;

namespace RogueDeal.HexLevels
{
    /// <summary>
    /// Camera controller for hex level editor.
    /// Provides WASD movement, Q/E elevation, mouse scroll zoom, and middle mouse drag rotation.
    /// </summary>
    public class HexLevelCameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Movement speed (units per second)")]
        public float moveSpeed = 10f;
        
        [Tooltip("Fast movement multiplier (hold Shift)")]
        public float fastMoveMultiplier = 2f;
        
        [Tooltip("Elevation change speed (units per second)")]
        public float elevationSpeed = 10f;

        [Header("Rotation Settings")]
        [Tooltip("Mouse sensitivity for rotation")]
        public float rotationSensitivity = 0.5f;
        
        [Tooltip("Smooth rotation (0 = instant, higher = smoother)")]
        public float rotationSmoothing = 5f;

        [Header("Zoom Settings")]
        [Tooltip("Zoom speed")]
        public float zoomSpeed = 5f;
        
        [Tooltip("Minimum zoom distance")]
        public float minZoom = 5f;
        
        [Tooltip("Maximum zoom distance")]
        public float maxZoom = 100f;

        [Header("Constraints")]
        [Tooltip("Lock camera rotation (disable middle mouse rotation)")]
        public bool lockRotation = false;
        
        [Tooltip("Lock camera position to a plane (disable Q/E elevation)")]
        public bool lockElevation = false;
        
        [Tooltip("Fixed elevation if locked")]
        public float fixedElevation = 10f;

        private Camera _camera;
        private float _rotationX = 0f;
        private float _rotationY = 0f;
        private bool _isRotating = false;
        private Vector3 _lastMousePosition;
        private bool _hasInitializedRotation = false;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                Debug.LogWarning("HexLevelCameraController: No camera found! Add a Camera component or set Camera.main.");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            // Initialize rotation from current transform
            if (!_hasInitializedRotation)
            {
                Vector3 currentEuler = transform.eulerAngles;
                _rotationX = currentEuler.x;
                _rotationY = currentEuler.y;
                
                // Normalize rotation to -180 to 180 range for smoother handling
                if (_rotationX > 180f) _rotationX -= 360f;
                if (_rotationY > 180f) _rotationY -= 360f;
                
                _hasInitializedRotation = true;
            }
        }

        private void Update()
        {
            if (_camera == null)
                return;

            // Only process input if Game view is focused
            if (!IsGameViewFocused())
                return;

            HandleMovement();
            HandleRotation();
            HandleZoom();
        }

        /// <summary>
        /// Check if the Game view window is focused and mouse is over it.
        /// </summary>
        private bool IsGameViewFocused()
        {
            // Only work in play mode
            if (!Application.isPlaying)
                return false;

            // Check if application has focus
            if (!Application.isFocused)
                return false;

            // Check if mouse is over the Game view
            // We'll use the camera's viewport to determine if mouse is over the game window
            Vector3 mousePos = Input.mousePosition;
            
            // Check if mouse is within screen bounds
            if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
                return false;

            // Additional check: verify mouse is actually over the camera's viewport
            // This helps when Game view is docked or split with Scene view
            if (_camera != null)
            {
                // Convert mouse position to viewport coordinates
                Vector3 viewportPoint = _camera.ScreenToViewportPoint(mousePos);
                
                // Check if mouse is within camera viewport (0-1 range)
                if (viewportPoint.x < 0f || viewportPoint.x > 1f || 
                    viewportPoint.y < 0f || viewportPoint.y > 1f || 
                    viewportPoint.z < 0f)
                {
                    return false; // Mouse is outside camera viewport
                }
            }

            return true;
        }

        private void HandleMovement()
        {
            // Get input
            float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
            float vertical = Input.GetAxis("Vertical");     // W/S or Up/Down arrows
            float elevation = 0f;

            // Q/E for elevation
            if (!lockElevation)
            {
                if (Input.GetKey(KeyCode.Q))
                    elevation = -1f;
                else if (Input.GetKey(KeyCode.E))
                    elevation = 1f;
            }

            // Check for fast movement (Shift)
            float speedMultiplier = Input.GetKey(KeyCode.LeftShift) ? fastMoveMultiplier : 1f;
            float currentMoveSpeed = moveSpeed * speedMultiplier;
            float currentElevationSpeed = elevationSpeed * speedMultiplier;

            // Calculate movement direction in camera's local space
            Vector3 moveDirection = Vector3.zero;
            
            if (horizontal != 0f || vertical != 0f)
            {
                // Move relative to camera's forward/right, but keep Y movement separate
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;
                
                // Flatten forward and right to horizontal plane
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                
                moveDirection = (forward * vertical + right * horizontal) * currentMoveSpeed;
            }

            // Elevation movement (always in world Y)
            if (elevation != 0f)
            {
                moveDirection.y = elevation * currentElevationSpeed;
            }
            else if (lockElevation)
            {
                // Lock to fixed elevation
                float currentY = transform.position.y;
                float targetY = fixedElevation;
                if (Mathf.Abs(currentY - targetY) > 0.1f)
                {
                    moveDirection.y = (targetY - currentY) * currentMoveSpeed * 0.5f;
                }
            }

            // Apply movement
            if (moveDirection != Vector3.zero)
            {
                transform.position += moveDirection * Time.deltaTime;
            }
        }

        private void HandleRotation()
        {
            if (lockRotation)
                return;

            // Check for middle mouse button
            if (Input.GetMouseButtonDown(2)) // Middle mouse button
            {
                _isRotating = true;
                _lastMousePosition = Input.mousePosition;
                // Don't lock cursor - just capture mouse movement
            }
            else if (Input.GetMouseButtonUp(2))
            {
                _isRotating = false;
            }

            if (_isRotating)
            {
                // Get mouse delta (in pixels)
                Vector3 currentMousePos = Input.mousePosition;
                Vector3 mouseDelta = currentMousePos - _lastMousePosition;
                
                // Apply rotation (inverted Y for natural feel)
                _rotationY += mouseDelta.x * rotationSensitivity;
                _rotationX -= mouseDelta.y * rotationSensitivity;
                
                // Clamp vertical rotation to prevent flipping
                _rotationX = Mathf.Clamp(_rotationX, -89f, 89f);
                
                // Apply rotation immediately (no smoothing for responsiveness)
                transform.rotation = Quaternion.Euler(_rotationX, _rotationY, 0f);
                
                _lastMousePosition = currentMousePos;
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            
            if (scroll != 0f)
            {
                // Zoom by moving camera forward/back
                Vector3 zoomDirection = transform.forward;
                float zoomAmount = scroll * zoomSpeed;
                
                Vector3 newPosition = transform.position + zoomDirection * zoomAmount;
                
                // Clamp zoom distance (distance from origin, or use a reference point)
                // For simplicity, we'll clamp based on Y position (assuming looking down at hex grid)
                // You can adjust this based on your needs
                float distance = Vector3.Distance(newPosition, Vector3.zero);
                
                // Simple zoom clamp - adjust based on your scene
                if (distance >= minZoom && distance <= maxZoom)
                {
                    transform.position = newPosition;
                }
                else
                {
                    // Clamp to min/max
                    if (distance < minZoom)
                    {
                        Vector3 direction = (newPosition - Vector3.zero).normalized;
                        transform.position = Vector3.zero + direction * minZoom;
                    }
                    else if (distance > maxZoom)
                    {
                        Vector3 direction = (newPosition - Vector3.zero).normalized;
                        transform.position = Vector3.zero + direction * maxZoom;
                    }
                }
            }
        }

        /// <summary>
        /// Reset camera to default position and rotation.
        /// </summary>
        public void ResetCamera()
        {
            transform.position = new Vector3(0f, 15f, -15f);
            _rotationX = 45f; // 45 degree angle looking down
            _rotationY = 0f;
            transform.rotation = Quaternion.Euler(_rotationX, _rotationY, 0f);
        }

        /// <summary>
        /// Focus camera on a specific world position.
        /// </summary>
        public void FocusOn(Vector3 worldPosition, float distance = 20f, float angle = 45f)
        {
            // Position camera at angle looking at target
            float angleRad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                0f,
                distance * Mathf.Sin(angleRad),
                -distance * Mathf.Cos(angleRad)
            );
            
            transform.position = worldPosition + offset;
            transform.LookAt(worldPosition);
            
            // Update rotation values to match
            Vector3 euler = transform.eulerAngles;
            _rotationX = euler.x;
            _rotationY = euler.y;
            
            // Normalize to -180 to 180 range
            if (_rotationX > 180f) _rotationX -= 360f;
            if (_rotationY > 180f) _rotationY -= 360f;
        }
    }
}