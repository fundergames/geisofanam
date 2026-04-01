using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Lightweight one-shot burst used when no <see cref="SoulWeaponAbilityAsset"/> VFX prefab is assigned.
    /// </summary>
    public static class SoulAbilityProceduralBurst
    {
        public static void Spawn(Vector3 worldPosition, Vector3 forwardWorld, Color color, float destroyAfterSeconds = 1.35f)
        {
            if (!Application.isPlaying)
                return;

            var go = new GameObject("SoulAbilityBurst");
            go.transform.position = worldPosition;
            if (forwardWorld.sqrMagnitude > 1e-6f)
            {
                Vector3 f = forwardWorld.normalized;
                go.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
            }

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.35f;
            main.startLifetime = 0.45f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.35f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            main.gravityModifier = 0.15f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.35f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color * 0.4f, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
            Object.Destroy(go, destroyAfterSeconds);
        }
    }
}
