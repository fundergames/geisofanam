using UnityEngine;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Shared defaults for soul-realm exit hold screen-center spectral particles (trails + noise).
    /// Used by <see cref="SoulRealmExitHoldVfx"/> at runtime and by the editor prefab builder.
    /// </summary>
    public static class SoulRealmExitHoldParticleSetup
    {
        public static void Apply(ParticleSystem ps)
        {
            if (ps == null)
                return;

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.45f, 0.92f, 0.78f, 0.95f),
                new Color(0.65f, 1f, 0.95f, 0.75f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 256;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 55f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            shape.randomDirectionAmount = 0.35f;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            noise.frequency = 0.65f;
            noise.scrollSpeed = 0.35f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(0.5f, 1f, 0.9f), 0f), new GradientColorKey(new Color(0.3f, 0.85f, 1f), 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve sz = new AnimationCurve(
                new Keyframe(0f, 0.4f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.2f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sz);

            var trails = ps.trails;
            trails.enabled = true;
            trails.mode = ParticleSystemTrailMode.Ribbon;
            trails.ratio = 1f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.45f);
            trails.minVertexDistance = 0.02f;
            trails.worldSpace = true;
            trails.dieWithParticles = false;
            trails.textureMode = ParticleSystemTrailTextureMode.Stretch;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingFudge = -2f;
        }
    }
}
