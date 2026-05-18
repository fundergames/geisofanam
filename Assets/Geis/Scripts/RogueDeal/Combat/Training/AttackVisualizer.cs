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
using System.Collections.Generic;

namespace RogueDeal.Combat.Training
{
    public class AttackVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private bool showAttackRange = true;
        [SerializeField] private bool showHitboxes = true;
        [SerializeField] private bool showTrajectory = true;
        
        [Header("Visual Properties")]
        [SerializeField] private Color rangeColor = new Color(0f, 1f, 0f, 0.2f);
        [SerializeField] private Color hitboxColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private Color trajectoryColor = new Color(1f, 1f, 0f, 0.5f);
        [SerializeField] private float trajectoryLineWidth = 0.1f;
        [SerializeField] private int trajectoryPointCount = 20;
        
        [Header("Hitbox Settings")]
        [SerializeField] private float hitboxDisplayDuration = 0.5f;

        [Header("Melee probe comparison")]
        [Tooltip("When set, draws SimpleAttackHitDetector probe spheres on each connected strike for tuning vs WeaponHitbox.")]
        [SerializeField] private SimpleAttackHitDetector meleeProbeReference;
        [SerializeField] private bool showMeleeProbeOnHit = true;
        [SerializeField] private Color meleeProbeColor = new Color(0.2f, 0.6f, 1f, 0.25f);
        [SerializeField] private Color strikeMissedColor = new Color(1f, 0.85f, 0.2f, 0.35f);
        
        private List<HitboxVisualization> activeHitboxes = new List<HitboxVisualization>();
        private LineRenderer trajectoryLine;
        private List<Vector3> trajectoryPoints = new List<Vector3>();
        
        private void Awake()
        {
            if (showTrajectory)
            {
                CreateTrajectoryLine();
            }
        }
        
        private void OnEnable()
        {
            CombatEvents.OnAttackStarted += OnAttackStarted;
            CombatEvents.OnAttackConnected += OnAttackConnected;
            CombatEvents.OnStrikeMissed += OnStrikeMissed;
            if (meleeProbeReference == null)
                meleeProbeReference = FindFirstObjectByType<SimpleAttackHitDetector>();
        }
        
        private void OnDisable()
        {
            CombatEvents.OnAttackStarted -= OnAttackStarted;
            CombatEvents.OnAttackConnected -= OnAttackConnected;
            CombatEvents.OnStrikeMissed -= OnStrikeMissed;
        }
        
        private void Update()
        {
            UpdateHitboxVisualizations();
        }
        
        private void OnAttackStarted(CombatEventData data)
        {
            if (showAttackRange && data.ability != null)
            {
                DrawAttackRange(data.source.transform.position, data.ability.range);
            }
            
            if (showTrajectory && data.source != null && data.target != null)
            {
                DrawTrajectory(data.source.transform.position, data.target.transform.position);
            }
        }
        
        private void OnAttackConnected(CombatEventData data)
        {
            if (showHitboxes)
            {
                float size = data.strikeKind == CombatStrikeKind.Melee ? 1f : 0.75f;
                CreateHitboxVisualization(data.hitPosition, size, hitboxDisplayDuration, hitboxColor);
            }

            if (showMeleeProbeOnHit && meleeProbeReference != null && data.source != null)
            {
                DrawMeleeProbeSpheres(data.source.transform, meleeProbeColor, hitboxDisplayDuration);
            }
        }

        private void OnStrikeMissed(CombatEventData data)
        {
            if (!showHitboxes || data.target == null)
                return;

            float size = data.strikeOutcome == CombatStrikeOutcome.Miss_Dodged ? 1.2f : 0.9f;
            CreateHitboxVisualization(data.hitPosition, size, hitboxDisplayDuration, strikeMissedColor);
        }

        private void DrawMeleeProbeSpheres(Transform attacker, Color color, float duration)
        {
            if (meleeProbeReference == null || attacker == null)
                return;

            Vector3 origin = attacker.position;
            Vector3 forward = attacker.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 1e-6f)
                forward.Normalize();

            const float rangeOffset = 1.5f;
            const float hitRadius = 2f;
            Vector3 forwardCenter = origin + forward * rangeOffset + Vector3.up * 0.5f;
            Vector3 bodyCenter = origin + Vector3.up * 0.5f;

            CreateHitboxVisualization(forwardCenter, hitRadius * 2f, duration, color);
            CreateHitboxVisualization(bodyCenter, hitRadius * 2f, duration, color);
        }
        
        private void DrawAttackRange(Vector3 origin, float range)
        {
            Debug.DrawLine(origin, origin + Vector3.forward * range, rangeColor, 1f);
            Debug.DrawLine(origin, origin + Vector3.back * range, rangeColor, 1f);
            Debug.DrawLine(origin, origin + Vector3.left * range, rangeColor, 1f);
            Debug.DrawLine(origin, origin + Vector3.right * range, rangeColor, 1f);
        }
        
        private void DrawTrajectory(Vector3 start, Vector3 end)
        {
            if (trajectoryLine == null) return;
            
            trajectoryPoints.Clear();
            
            for (int i = 0; i <= trajectoryPointCount; i++)
            {
                float t = i / (float)trajectoryPointCount;
                Vector3 point = Vector3.Lerp(start, end, t);
                point.y += Mathf.Sin(t * Mathf.PI) * 1f;
                trajectoryPoints.Add(point);
            }
            
            trajectoryLine.positionCount = trajectoryPoints.Count;
            trajectoryLine.SetPositions(trajectoryPoints.ToArray());
        }
        
        private void CreateTrajectoryLine()
        {
            GameObject lineObj = new GameObject("TrajectoryLine");
            lineObj.transform.SetParent(transform);
            
            trajectoryLine = lineObj.AddComponent<LineRenderer>();
            trajectoryLine.startWidth = trajectoryLineWidth;
            trajectoryLine.endWidth = trajectoryLineWidth;
            trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
            trajectoryLine.startColor = trajectoryColor;
            trajectoryLine.endColor = trajectoryColor;
        }
        
        private void CreateHitboxVisualization(Vector3 position, float size, float duration, Color? colorOverride = null)
        {
            GameObject hitboxObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitboxObj.transform.position = position;
            hitboxObj.transform.localScale = Vector3.one * size;
            
            Renderer renderer = hitboxObj.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = colorOverride ?? hitboxColor;
            renderer.material = mat;
            
            Collider collider = hitboxObj.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
            
            HitboxVisualization viz = new HitboxVisualization
            {
                gameObject = hitboxObj,
                creationTime = Time.time,
                duration = duration,
                baseColor = mat.color
            };
            
            activeHitboxes.Add(viz);
        }
        
        private void UpdateHitboxVisualizations()
        {
            for (int i = activeHitboxes.Count - 1; i >= 0; i--)
            {
                HitboxVisualization viz = activeHitboxes[i];
                
                if (Time.time - viz.creationTime >= viz.duration)
                {
                    Destroy(viz.gameObject);
                    activeHitboxes.RemoveAt(i);
                }
                else
                {
                    float alpha = 1f - ((Time.time - viz.creationTime) / viz.duration);
                    Renderer renderer = viz.gameObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Color color = viz.baseColor;
                        color.a *= alpha;
                        renderer.material.color = color;
                    }
                }
            }
        }
        
        public void ToggleAttackRange(bool enabled)
        {
            showAttackRange = enabled;
        }
        
        public void ToggleHitboxes(bool enabled)
        {
            showHitboxes = enabled;
        }
        
        public void ToggleTrajectory(bool enabled)
        {
            showTrajectory = enabled;
            
            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = enabled;
            }
        }
        
        private struct HitboxVisualization
        {
            public GameObject gameObject;
            public float creationTime;
            public float duration;
            public Color baseColor;
        }
    }
}
