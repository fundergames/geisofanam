Shader "Geis/Hidden/SoulRealmScreen"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off
        Pass
        {
            Name "SoulRealmScreen"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            SAMPLER(sampler_BlitTexture);

            float _GeisSoulRealmBlend;
            float4 _GeisShockwaveCenterUV;
            float4 _GeisShockwaveData;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float phase = _GeisShockwaveData.x;
                float intensity = _GeisShockwaveData.y;
                half4 c;

                if (intensity > 0.001f && phase > 0.001f)
                {
                    float2 center = _GeisShockwaveCenterUV.xy;
                    float2 delta = uv - center;
                    float dist = length(delta);
                    float2 dir = dist > 1e-5f ? delta / dist : float2(0.0f, 0.0f);
                    float travel = phase * 0.58f;
                    float edge = abs(dist - travel);
                    float ring = (1.0f - smoothstep(0.0f, 0.085f, edge)) * intensity;
                    float push = ring * 0.048f;
                    float chroma = ring * 0.014f;
                    float2 uvBase = uv + dir * push;
                    half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + dir * (push + chroma)).r;
                    half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uvBase).g;
                    half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + dir * (push - chroma)).b;
                    c = half4(r, g, b, 1.0h);
                }
                else
                {
                    c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
                }

                half lum = dot(c.rgb, half3(0.3h, 0.59h, 0.11h));
                half3 desat = lerp(half3(lum, lum, lum), c.rgb, 0.35h);
                half3 tinted = desat * half3(0.55h, 1.0h, 0.72h);
                c.rgb = lerp(c.rgb, tinted, (half)_GeisSoulRealmBlend);
                return c;
            }
            ENDHLSL
        }
    }
}
