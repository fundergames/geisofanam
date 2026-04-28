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
        public int unreadableMeshCount;

        // ──────────────────────────────────────────────────────────────────────

        private static T[] FindObjectsCompat<T>() where T : UnityEngine.Object
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return UnityEngine.Object.FindObjectsOfType<T>();
#endif
        }

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
            var allGOs = FindObjectsCompat<GameObject>();
            census.activeGameObjects = allGOs.Length;

            // Renderer types
            var renderers = FindObjectsCompat<Renderer>();
            census.activeRenderers      = renderers.Length;
            census.meshRenderers        = FindObjectsCompat<MeshRenderer>().Length;
            census.skinnedMeshRenderers = FindObjectsCompat<SkinnedMeshRenderer>().Length;

            census.particleSystems = FindObjectsCompat<ParticleSystem>().Length;

            // Lights
            var allLights = FindObjectsCompat<Light>();
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
            census.cameras    = FindObjectsCompat<Camera>().Length;
            census.canvases   = FindObjectsCompat<Canvas>().Length;
            census.rigidbodies = FindObjectsCompat<Rigidbody>().Length;
            census.colliders  = FindObjectsCompat<Collider>().Length;
            census.animators  = FindObjectsCompat<Animator>().Length;

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
            int unreadableMeshes = 0;
            foreach (var mf in FindObjectsCompat<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                triCount += TryGetTriangleCount(mf.sharedMesh, ref unreadableMeshes);
            }
            foreach (var smr in FindObjectsCompat<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh == null) continue;
                triCount += TryGetTriangleCount(smr.sharedMesh, ref unreadableMeshes);
            }
            census.estimatedTriangleCount = triCount;
            census.unreadableMeshCount = unreadableMeshes;

            return census;
        }

        private static int TryGetTriangleCount(Mesh mesh, ref int unreadableMeshes)
        {
            if (mesh == null) return 0;

            // Non-readable meshes are common in shipping content; skip without log spam.
            if (!mesh.isReadable)
            {
                unreadableMeshes++;
                return 0;
            }

            try
            {
                return mesh.triangles.Length / 3;
            }
            catch
            {
                unreadableMeshes++;
                return 0;
            }
        }
    }
}
