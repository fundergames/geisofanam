/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using UnityEngine;

namespace RogueDeal.Combat.Targeting
{
    /// <summary>
    /// Visual indicator for lock-on targeting. Renders a procedural ring + cross above the target and
    /// billboards toward the player camera so the active lock stays readable while strafing.
    /// </summary>
    public class LockOnIndicator : MonoBehaviour
    {
        [Header("Appearance")]
        [Tooltip("Radius of the lock-on ring.")]
        [SerializeField] private float indicatorRadius = 0.6f;

        [Tooltip("Color of the indicator.")]
        [SerializeField] private Color indicatorColor = new Color(1f, 0f, 0f, 0.8f);

        [Tooltip("Width of the ring and cross lines.")]
        [SerializeField] private float lineWidth = 0.03f;

        [Tooltip("Vertical lift applied above the lock-on anchor position.")]
        [SerializeField] private float anchorHeightOffset = 0.15f;

        [Tooltip("How far to pull the indicator toward the camera so it sits in front of the target.")]
        [SerializeField] private float cameraFacingOffset = 0.35f;

        [Tooltip("Additional pulse applied to the indicator alpha.")]
        [SerializeField] private float pulseSpeed = 3f;

        [Tooltip("How much the indicator alpha pulses over time.")]
        [Range(0f, 1f)]
        [SerializeField] private float pulseAmount = 0.18f;

        [Header("Editor Tuning")]
        [Tooltip("When enabled, inspector changes rebuild the indicator immediately so you can tune it while playing.")]
        [SerializeField] private bool liveUpdateInEditor = true;

        [Header("Visual Settings")]
        [SerializeField] private LineRenderer circleRenderer;
        [SerializeField] private LineRenderer crossRenderer;

        private Transform targetAnchor;
        private bool isActive;
        private Camera mainCamera;
        private float pulseTime;
        private float _lastIndicatorRadius = -1f;
        private float _lastLineWidth = -1f;
        private float _lastAnchorHeightOffset = -1f;
        private float _lastCameraFacingOffset = -1f;
        private float _lastPulseSpeed = -1f;
        private float _lastPulseAmount = -1f;
        private Color _lastIndicatorColor = default;
        private bool _hasAppliedTunableSettings;
        private const int CircleSegments = 48;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }

            CreateVisuals();
            ApplyTunableSettings();
            SetActive(false);
        }

        private void OnValidate()
        {
            if (!liveUpdateInEditor)
                return;

            ApplyTunableSettings();

            if (isActive)
            {
                UpdatePosition();
                BillboardToCamera();
                UpdateVisuals();
                SetActive(true);
            }
        }

        private void CreateVisuals()
        {
            if (circleRenderer == null)
            {
                GameObject circleObj = new GameObject("Circle");
                circleObj.transform.SetParent(transform, false);
                circleObj.transform.localPosition = Vector3.zero;
                circleRenderer = circleObj.AddComponent<LineRenderer>();
                circleRenderer.useWorldSpace = false;
                circleRenderer.loop = true;
                circleRenderer.widthMultiplier = lineWidth;
                circleRenderer.material = new Material(Shader.Find("Sprites/Default"));
                circleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                circleRenderer.receiveShadows = false;
                circleRenderer.alignment = LineAlignment.TransformZ;
                BuildCircle(circleRenderer, indicatorRadius);
                circleRenderer.enabled = false;
            }

            if (crossRenderer == null)
            {
                GameObject crossObj = new GameObject("Cross");
                crossObj.transform.SetParent(transform, false);
                crossObj.transform.localPosition = Vector3.zero;
                crossRenderer = crossObj.AddComponent<LineRenderer>();
                crossRenderer.useWorldSpace = false;
                crossRenderer.loop = false;
                crossRenderer.widthMultiplier = lineWidth * 0.9f;
                crossRenderer.material = new Material(Shader.Find("Sprites/Default"));
                crossRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                crossRenderer.receiveShadows = false;
                crossRenderer.alignment = LineAlignment.TransformZ;
                BuildCross(crossRenderer, indicatorRadius * 1.15f);
                crossRenderer.enabled = false;
            }
        }

        private void ApplyTunableSettings()
        {
            indicatorRadius = Mathf.Max(0.05f, indicatorRadius);
            lineWidth = Mathf.Max(0.001f, lineWidth);
            anchorHeightOffset = Mathf.Max(0f, anchorHeightOffset);
            cameraFacingOffset = Mathf.Max(0f, cameraFacingOffset);
            pulseSpeed = Mathf.Max(0f, pulseSpeed);
            pulseAmount = Mathf.Clamp01(pulseAmount);

            if (circleRenderer != null)
            {
                circleRenderer.widthMultiplier = lineWidth;
                BuildCircle(circleRenderer, indicatorRadius);
            }

            if (crossRenderer != null)
            {
                crossRenderer.widthMultiplier = lineWidth * 0.9f;
                BuildCross(crossRenderer, indicatorRadius * 1.15f);
            }

            _lastIndicatorRadius = indicatorRadius;
            _lastLineWidth = lineWidth;
            _lastAnchorHeightOffset = anchorHeightOffset;
            _lastCameraFacingOffset = cameraFacingOffset;
            _lastPulseSpeed = pulseSpeed;
            _lastPulseAmount = pulseAmount;
            _lastIndicatorColor = indicatorColor;
            _hasAppliedTunableSettings = true;
        }

        private void BuildCircle(LineRenderer lr, float radius)
        {
            lr.positionCount = CircleSegments;
            float step = Mathf.PI * 2f / CircleSegments;
            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = step * i;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void BuildCross(LineRenderer lr, float radius)
        {
            float half = radius * 0.55f;
            lr.positionCount = 5;
            lr.SetPosition(0, new Vector3(-half, 0f, 0f));
            lr.SetPosition(1, new Vector3(half, 0f, 0f));
            lr.SetPosition(2, Vector3.zero);
            lr.SetPosition(3, new Vector3(0f, -half, 0f));
            lr.SetPosition(4, new Vector3(0f, half, 0f));
        }

        private void LateUpdate()
        {
            if (liveUpdateInEditor && HaveTunableSettingsChanged())
            {
                ApplyTunableSettings();

                if (isActive)
                {
                    UpdatePosition();
                    BillboardToCamera();
                    UpdateVisuals();
                    SetActive(true);
                }
            }

            if (isActive)
            {
                UpdatePosition();
                BillboardToCamera();
                UpdateVisuals();
            }
        }

        private bool HaveTunableSettingsChanged()
        {
            if (!_hasAppliedTunableSettings)
                return true;

            return !Mathf.Approximately(_lastIndicatorRadius, indicatorRadius)
                || !Mathf.Approximately(_lastLineWidth, lineWidth)
                || !Mathf.Approximately(_lastAnchorHeightOffset, anchorHeightOffset)
                || !Mathf.Approximately(_lastCameraFacingOffset, cameraFacingOffset)
                || !Mathf.Approximately(_lastPulseSpeed, pulseSpeed)
                || !Mathf.Approximately(_lastPulseAmount, pulseAmount)
                || _lastIndicatorColor != indicatorColor;
        }

        /// <summary>
        /// Follows a world-space anchor (e.g. lock-on socket, aim point, or external helper transform).
        /// </summary>
        public void SetTarget(Transform anchor)
        {
            targetAnchor = anchor;
            isActive = targetAnchor != null;

            if (isActive)
            {
                SetActive(true);
                UpdatePosition();
                BillboardToCamera();
                UpdateVisuals();
            }
            else
            {
                SetActive(false);
            }
        }

        /// <summary>
        /// Sets a dedicated world anchor to follow (same as <see cref="SetTarget"/>).
        /// </summary>
        public void SetAnchorTarget(Transform anchor) => SetTarget(anchor);

        /// <summary>
        /// Clears the lock-on target
        /// </summary>
        public void ClearTarget()
        {
            targetAnchor = null;
            isActive = false;
            SetActive(false);
        }
        
        /// <summary>
        /// Sets the ground position for AOE targeting
        /// </summary>
        public void SetGroundPosition(Vector3 position)
        {
            targetAnchor = null;
            isActive = true;

            transform.position = position;
            BillboardToCamera();
            SetActive(true);
            UpdateVisuals();
        }

        private void UpdatePosition()
        {
            if (targetAnchor != null)
                transform.position = GetIndicatorPosition();
        }

        /// <summary>
        /// Positions the indicator at the anchor with optional vertical lift for readability.
        /// </summary>
        private Vector3 GetIndicatorPosition()
        {
            if (targetAnchor == null)
                return Vector3.zero;

            return OffsetTowardCamera(targetAnchor.position + Vector3.up * anchorHeightOffset);
        }

        private Vector3 OffsetTowardCamera(Vector3 basePosition)
        {
            if (mainCamera == null)
                mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();

            if (mainCamera == null || cameraFacingOffset <= 0f)
                return basePosition;

            Vector3 towardCamera = mainCamera.transform.position - basePosition;
            towardCamera.y = 0f;

            if (towardCamera.sqrMagnitude <= 0.0001f)
                return basePosition;

            return basePosition + towardCamera.normalized * cameraFacingOffset;
        }

        private void BillboardToCamera()
        {
            if (mainCamera == null)
                mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();

            if (mainCamera == null)
                return;

            Vector3 toCamera = transform.position - mainCamera.transform.position;
            if (toCamera.sqrMagnitude <= 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }

        private void UpdateVisuals()
        {
            pulseTime += Time.deltaTime * pulseSpeed;
            float alphaScale = 1f - pulseAmount + Mathf.Abs(Mathf.Sin(pulseTime)) * pulseAmount;
            Color currentColor = indicatorColor;
            currentColor.a *= alphaScale;

            if (circleRenderer != null)
                circleRenderer.startColor = circleRenderer.endColor = currentColor;

            if (crossRenderer != null)
                crossRenderer.startColor = crossRenderer.endColor = currentColor;
        }

        private void SetActive(bool active)
        {
            if (circleRenderer != null)
                circleRenderer.enabled = active;

            if (crossRenderer != null)
                crossRenderer.enabled = active;
        }
    }
}
