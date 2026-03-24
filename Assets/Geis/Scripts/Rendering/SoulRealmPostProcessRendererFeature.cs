using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Geis.Rendering
{
    /// <summary>
    /// Optional fullscreen pass after post-processing; uses global <c>_GeisSoulRealmBlend</c> (set by <see cref="SoulRealm.SoulRealmVisuals"/>).
    /// Add to the URP Renderer asset (e.g. PC_Renderer) and assign a material using shader <c>Geis/Hidden/SoulRealmScreen</c>.
    /// </summary>
    public sealed class SoulRealmPostProcessRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Material material;
        [SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;

        private SoulRealmScreenPass _pass;

        /// <inheritdoc />
        public override void Create()
        {
            _pass = new SoulRealmScreenPass();
        }

        /// <inheritdoc />
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (material == null || renderingData.cameraData.cameraType == CameraType.Preview)
                return;
            _pass.Setup(material, injectionPoint);
            renderer.EnqueuePass(_pass);
        }

        private sealed class SoulRealmScreenPass : ScriptableRenderPass
        {
            private Material _material;

            public void Setup(Material mat, RenderPassEvent evt)
            {
                _material = mat;
                renderPassEvent = evt;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_material == null)
                    return;

                var cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, new ProfilingSampler("GeisSoulRealmScreen")))
                {
                    var handle = renderingData.cameraData.renderer.cameraColorTargetHandle;
                    Blitter.BlitCameraTexture(cmd, handle, handle, _material, 0);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}
