using Geis.InputSystem;
using Geis.Locomotion;
using UnityEngine;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Spawns a spectral duplicate of the character mesh under the ghost root and applies transparent materials.
    /// </summary>
    public static class SoulSpectralGhostVisual
    {
        /// <summary>
        /// Finds a reasonable mesh root: first SkinnedMeshRenderer's transform, else first MeshRenderer's transform.
        /// </summary>
        public static Transform FindDefaultVisualRoot(Transform bodyRoot)
        {
            if (bodyRoot == null)
                return null;
            var smrs = bodyRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (smrs != null && smrs.Length > 0)
                return smrs[0].transform;
            var mrs = bodyRoot.GetComponentsInChildren<MeshRenderer>(true);
            if (mrs != null && mrs.Length > 0)
                return mrs[0].transform;
            return null;
        }

        public static GameObject Spawn(
            Transform ghostRoot,
            Transform bodyRoot,
            Transform explicitVisualRoot,
            Animator bodyAnimator,
            SoulGhostMotor ghostMotor,
            GeisInputReader inputReader,
            GeisPlayerAnimationController bodyLocomotion,
            Material spectralMaterialOverride,
            Material spectralDissolveMaterialTemplate,
            float spectralEnterDissolveDuration,
            bool invertDissolveForShader)
        {
            var source = explicitVisualRoot != null ? explicitVisualRoot : FindDefaultVisualRoot(bodyRoot);
            if (source == null || ghostRoot == null)
            {
                Debug.LogWarning(
                    "[SoulSpectralGhostVisual] No mesh root found. Assign Spectral Character Visual Root on SoulRealmManager.",
                    bodyRoot);
                return null;
            }

            var instance = Object.Instantiate(source.gameObject, ghostRoot.transform);
            instance.name = "SpectralCharacterVisual";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null)
                    r.enabled = true;
            }

            StripGameplayComponents(instance);

            var anim = instance.GetComponentInChildren<Animator>();
            if (anim != null && bodyAnimator != null)
            {
                anim.runtimeAnimatorController = bodyAnimator.runtimeAnimatorController;
                anim.avatar = bodyAnimator.avatar;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            if (spectralMaterialOverride != null)
            {
                ApplySpectralMaterials(instance, spectralMaterialOverride);
            }
            else if (spectralDissolveMaterialTemplate != null)
            {
                ApplySpectralDissolveMaterials(instance, spectralDissolveMaterialTemplate, invertDissolveForShader);
                var dissolveDriver = instance.AddComponent<SoulSpectralDissolveDriver>();
                dissolveDriver.Configure(spectralEnterDissolveDuration, invertDissolveForShader);
            }
            else
            {
                var mat = CreateDefaultSpectralMaterial();
                ApplySpectralMaterials(instance, mat);
            }

            var driver = instance.AddComponent<SoulSpectralAnimatorDriver>();
            driver.Configure(ghostMotor, inputReader, bodyLocomotion);

            return instance;
        }

        public static void Despawn(GameObject instance)
        {
            if (instance != null)
                Object.Destroy(instance);
        }

        private static void StripGameplayComponents(GameObject root)
        {
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null)
                    continue;
                if (mb is Animator || mb is SoulSpectralAnimatorDriver)
                    continue;
                Object.Destroy(mb);
            }

            foreach (var c in root.GetComponentsInChildren<CharacterController>(true))
                Object.Destroy(c);
            foreach (var r in root.GetComponentsInChildren<Rigidbody>(true))
                Object.Destroy(r);
            foreach (var c in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(c);
        }

        private static void ApplySpectralDissolveMaterials(GameObject root, Material dissolveTemplate, bool invertDissolveForShader)
        {
            if (dissolveTemplate == null)
                return;

            void ApplyToRenderer(Renderer r)
            {
                if (r == null) return;
                var orig = r.sharedMaterials;
                var mats = new Material[orig.Length];
                for (var i = 0; i < orig.Length; i++)
                {
                    var m = new Material(dissolveTemplate);
                    if (orig[i] != null)
                        m.CopyPropertiesFromMaterial(orig[i]);
                    ApplySpectralTintToDissolveMaterial(m);
                    m.SetFloat("_Dissolve", SoulSpectralDissolveDriver.ToShaderDissolve(1f, invertDissolveForShader));
                    mats[i] = m;
                }

                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                ApplyToRenderer(r);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
                ApplyToRenderer(r);
        }

        private static void ApplySpectralTintToDissolveMaterial(Material m)
        {
            if (m == null) return;
            var c = new Color(0.35f, 0.95f, 0.6f, 0.48f);
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", c);
            else if (m.HasProperty("_Color"))
                m.SetColor("_Color", c);
        }

        private static void ApplySpectralMaterials(GameObject root, Material template)
        {
            foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mats = r.materials;
                for (var i = 0; i < mats.Length; i++)
                    mats[i] = template;
                r.materials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = r.materials;
                for (var i = 0; i < mats.Length; i++)
                    mats[i] = template;
                r.materials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static Material CreateDefaultSpectralMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            var m = new Material(shader);
            if (Shader.Find("Universal Render Pipeline/Lit") != null)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetColor("_BaseColor", new Color(0.35f, 0.95f, 0.6f, 0.48f));
                m.SetFloat("_Metallic", 0.15f);
                m.SetFloat("_Smoothness", 0.65f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3000;
            }
            else
            {
                m.color = new Color(0.35f, 0.95f, 0.6f, 0.48f);
            }

            return m;
        }
    }
}
