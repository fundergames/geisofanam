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
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;

namespace Geis.Rendering
{
    /// <summary>
    /// Optional fullscreen pass after post-processing; uses globals set by <see cref="SoulRealm.SoulRealmVisuals"/>:
    /// <c>_GeisSoulRealmBlend</c>, and during entry <c>_GeisShockwaveCenterUV</c> / <c>_GeisShockwaveData</c> for the radial pulse.
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
            private static readonly ProfilingSampler ProfilingSampler = new("GeisSoulRealmScreen");
            private Material _material;

            public void Setup(Material mat, RenderPassEvent evt)
            {
                _material = mat;
                renderPassEvent = evt;
                profilingSampler = ProfilingSampler;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_material == null)
                    return;

                var resourcesData = frameData.Get<UniversalResourceData>();
                if (!resourcesData.activeColorTexture.IsValid())
                    return;

                var blitParameters = new BlitMaterialParameters(
                    TextureHandle.nullHandle,
                    resourcesData.activeColorTexture,
                    _material,
                    0);
                renderGraph.AddBlitPass(blitParameters, passName: "GeisSoulRealmScreen");
            }
        }
    }
}
