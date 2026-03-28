#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Geis.SoulRealm.Editor
{
    /// <summary>
    /// Creates <c>Assets/Geis/Resources/VFX/SoulRealmExitHoldParticles.prefab</c> — a root <see cref="GameObject"/> with a default child
    /// <see cref="ParticleSystem"/> (loaded at runtime via Resources as <c>VFX/SoulRealmExitHoldParticles</c>). Add more particle children under the root in the editor.
    /// </summary>
    public static class SoulRealmExitHoldParticlesPrefabBuilder
    {
        const string PrefabPath = "Assets/Geis/Resources/VFX/SoulRealmExitHoldParticles.prefab";

        [MenuItem("Geis/VFX/Create Soul Realm Exit Hold Particle Prefab", false, 10)]
        public static void CreatePrefabFromMenu()
        {
            CreatePrefabInternal();
        }

        /// <summary>Batchmode entry: <c>-executeMethod Geis.SoulRealm.Editor.SoulRealmExitHoldParticlesPrefabBuilder.CreatePrefabBatch</c></summary>
        public static void CreatePrefabBatch()
        {
            CreatePrefabInternal();
        }

        static void CreatePrefabInternal()
        {
            string dir = System.IO.Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                string parent = "Assets";
                foreach (var segment in dir.Replace("Assets/", "").Split('/'))
                {
                    string next = parent + "/" + segment;
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(parent, segment);
                    parent = next;
                }
            }

            var root = new GameObject("SoulRealmExitHoldParticles");
            var child = new GameObject("Particles_Default");
            child.transform.SetParent(root.transform, false);
            var ps = child.AddComponent<ParticleSystem>();
            SoulRealmExitHoldParticleSetup.Apply(ps);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SoulRealmExitHoldParticlesPrefabBuilder] Wrote {PrefabPath}");
        }
    }
}
#endif
