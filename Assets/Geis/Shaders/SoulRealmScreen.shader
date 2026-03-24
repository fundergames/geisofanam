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
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"

            float _GeisSoulRealmBlend;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
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
