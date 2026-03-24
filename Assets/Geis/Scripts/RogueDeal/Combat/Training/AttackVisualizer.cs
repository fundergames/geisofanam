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
        }
        
        private void OnDisable()
        {
            CombatEvents.OnAttackStarted -= OnAttackStarted;
            CombatEvents.OnAttackConnected -= OnAttackConnected;
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
                CreateHitboxVisualization(data.hitPosition, 1f, hitboxDisplayDuration);
            }
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
        
        private void CreateHitboxVisualization(Vector3 position, float size, float duration)
        {
            GameObject hitboxObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitboxObj.transform.position = position;
            hitboxObj.transform.localScale = Vector3.one * size;
            
            Renderer renderer = hitboxObj.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = hitboxColor;
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
                duration = duration
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
                        Color color = hitboxColor;
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
        }
    }
}
