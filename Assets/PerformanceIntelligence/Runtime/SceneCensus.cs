using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PerformanceIntelligence
{
    /// <summary>
    /// Snapshot of scene composition: object counts, renderer types, lights, materials, and
    /// an estimated triangle count. Call <see cref="Capture"/> from the main thread at any time
    /// (edit mode or play mode).
    /// </summary>
    [Serializable]
    public sealed class SceneCensus
    {
        public string sceneName;

        /// <summary>Time.realtimeSinceStartupAsDouble at capture time.</summary>
        public double timestamp;

        // ── Object counts ──────────────────────────────────────────────────────
        public int activeGameObjects;
        public int activeRenderers;
        public int meshRenderers;
        public int skinnedMeshRenderers;
        public int particleSystems;

        // ── Lights ─────────────────────────────────────────────────────────────
        public int lights;
        public int realtimeLights;
        public int shadowCastingLights;

        // ── Scene components ───────────────────────────────────────────────────
        public int cameras;
        public int canvases;
        public int rigidbodies;
        public int colliders;
        public int animators;

        // ── Material / shader diversity ────────────────────────────────────────
        public int uniqueMaterials;
        public int uniqueShaders;

        /// <summary>
        /// Sum of triangleCount across all MeshFilter and SkinnedMeshRenderer shared meshes.
        /// Uses Mesh.triangles.Length / 3 — allocates a managed array copy per mesh; intended
        /// for one-shot editor/tool use, not per-frame runtime calls.
        /// </summary>
        public long estimatedTriangleCount;

        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Scans the currently active scene and returns a populated <see cref="SceneCensus"/>.
        /// Must be called from the main thread.
        /// </summary>
        public static SceneCensus Capture()
        {
            var census = new SceneCensus
            {
                sceneName = SceneManager.GetActiveScene().name,
                timestamp = Time.realtimeSinceStartupAsDouble,
            };

            // Active GameObjects (FindObjectsByType only returns active objects by default)
            var allGOs = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            census.activeGameObjects = allGOs.Length;

            // Renderer types
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            census.activeRenderers      = renderers.Length;
            census.meshRenderers        = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Length;
            census.skinnedMeshRenderers = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None).Length;

            census.particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;

            // Lights
            var allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            census.lights = allLights.Length;
            int realtimeCount = 0, shadowCount = 0;
            foreach (var light in allLights)
            {
                if (light.lightmapBakeType == LightmapBakeType.Realtime) realtimeCount++;
                if (light.shadows != LightShadows.None) shadowCount++;
            }
            census.realtimeLights      = realtimeCount;
            census.shadowCastingLights = shadowCount;

            // Scene components
            census.cameras    = FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
            census.canvases   = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length;
            census.rigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length;
            census.colliders  = FindObjectsByType<Collider>(FindObjectsSortMode.None).Length;
            census.animators  = FindObjectsByType<Animator>(FindObjectsSortMode.None).Length;

            // Unique materials and shaders
            var matSet = new HashSet<Material>();
            foreach (var r in renderers)
            {
                var shared = r.sharedMaterials;
                if (shared == null) continue;
                foreach (var m in shared)
                    if (m != null) matSet.Add(m);
            }
            census.uniqueMaterials = matSet.Count;

            var shaderSet = new HashSet<Shader>();
            foreach (var m in matSet)
                if (m.shader != null) shaderSet.Add(m.shader);
            census.uniqueShaders = shaderSet.Count;

            // Estimated triangle count from mesh data
            long triCount = 0;
            foreach (var mf in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                if (mf.sharedMesh != null)
                    triCount += mf.sharedMesh.triangles.Length / 3;
            foreach (var smr in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
                if (smr.sharedMesh != null)
                    triCount += smr.sharedMesh.triangles.Length / 3;
            census.estimatedTriangleCount = triCount;

            return census;
        }
    }
}
