// Geis fork of URP Dissolve 2020-style property layout (matches ShaderGraph_Dissolve Dissolve_Metallic names).
// Dissolve noise UV = (mesh UV * _Tiling + _Offest) * _NoiseScale + _DissolveOffest.xy so SoulPhaseShiftPresentation
// can pulse _Dissolve (and optionally offset noise via _DissolveOffest / _Offest).
Shader "Geis/SoulRealm/PhaseShiftDissolve"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Tiling("Tiling", Vector) = (1, 1, 0, 0)
        [HideInInspector] _Offest("Offest", Vector) = (0, 0, 0, 0)
        _Dissolve("Dissolve", Range(0, 1)) = 0.5
        _NoiseScale("Noise Scale", Float) = 50
        [HideInInspector] _NoiseUVSpeed("Noise UV Speed", Vector) = (0, 0, 0, 0)
        [HideInInspector] _DissolveOffest("Dissolve Offest", Vector) = (0, 0, 0, 0)
        _EdgeWidth("Edge Width", Range(0, 1)) = 0.05
        [HDR] _EdgeColor("Edge Color", Color) = (0, 3.89, 4, 0)
        _EdgeColorIntensity("Edge Color Intensity", Float) = 1
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlitDissolve"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _Tiling;
                float4 _Offest;
                float _Dissolve;
                float _NoiseScale;
                float4 _DissolveOffest;
                float4 _NoiseUVSpeed;
                float _EdgeWidth;
                float4 _EdgeColor;
                float _EdgeColorIntensity;
                float _Metallic;
                float _Smoothness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInput.positionCS;
                output.positionWS = posInput.positionWS;
                output.normalWS = normInput.normalWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x),
                    lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 tiling = _Tiling.xy;
                float2 off = _Offest.xy;
                float2 uvMain = input.uv * tiling + off;

                float2 noiseUv = uvMain * _NoiseScale + _DissolveOffest.xy;
                float n = ValueNoise(noiseUv);
                clip(n - _Dissolve);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvMain) * half4(_BaseColor);

                float edge = abs(n - _Dissolve);
                float edgeMask = 1.0 - smoothstep(0.0, _EdgeWidth, edge);
                albedo.rgb += half3(_EdgeColor.rgb * (edgeMask * _EdgeColorIntensity));

                float3 nWs = normalize(input.normalWS);
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half nd = saturate(dot(nWs, mainLight.direction));
                half shadow = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                half3 diffuse = albedo.rgb * half3(mainLight.color) * (nd * shadow);

                half3 gi = SampleSH(nWs) * albedo.rgb;
                half smoothTerm = half(_Smoothness);
                half3 specTint = lerp(albedo.rgb, half3(0.04, 0.04, 0.04), half(_Metallic));
                half3 halfDir = normalize(mainLight.direction + viewDir);
                half spec = pow(saturate(dot(nWs, halfDir)), lerp(8.0h, 128.0h, smoothTerm)) * smoothTerm;
                half3 specular = specTint * half3(mainLight.color) * spec * shadow;

                half3 rgb = diffuse + gi * (1.0h - half(_Metallic) * 0.5h) + specular;
                half4 outCol = half4(rgb, albedo.a);
                outCol.rgb = MixFog(outCol.rgb, input.fogFactor);
                return outCol;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
