Shader "Geis/SoulRealm/Taken"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Tint", Color) = (0.08, 0.12, 0.14, 1)
        _DarkColor("Dark Body", Color) = (0.02, 0.04, 0.06, 0.92)
        _VeinColor("Vein Glow", Color) = (0.75, 1.0, 0.92, 1)
        _VeinIntensity("Vein Intensity", Range(0, 4)) = 2.2
        _VeinThreshold("Vein Threshold", Range(0, 1)) = 0.52
        _VeinSoftness("Vein Softness", Range(0.01, 0.5)) = 0.14
        _FresnelColor("Fresnel Glow", Color) = (0.35, 0.95, 0.8, 1)
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 3.2
        _FresnelIntensity("Fresnel Intensity", Range(0, 4)) = 1.6
        _NoiseScale("Noise Scale", Range(0.1, 8)) = 1.35
        _NoiseSpeed("Noise Speed", Range(0, 2)) = 0.42
        _NoiseFlow("Noise Flow", Vector) = (0.12, 0.08, 0.18, 0)
        _StarScale("Star Scale", Range(1, 40)) = 14
        _StarBrightness("Star Brightness", Range(0, 1)) = 0.55
        _Dissolve("Dissolve", Range(0, 1)) = 0
        _EdgeColor("Dissolve Edge", Color) = (0.2, 0.95, 1.0, 1.5)
        _EdgeWidth("Dissolve Edge Width", Range(0, 0.5)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "TakenCommon.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _DarkColor;
                half4 _VeinColor;
                half4 _FresnelColor;
                half4 _EdgeColor;
                float _VeinIntensity;
                float _VeinThreshold;
                float _VeinSoftness;
                float _FresnelPower;
                float _FresnelIntensity;
                float _NoiseScale;
                float _NoiseSpeed;
                float4 _NoiseFlow;
                float _StarScale;
                float _StarBrightness;
                float _Dissolve;
                float _EdgeWidth;
            CBUFFER_END

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
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 viewDirWS = GetWorldSpaceViewDir(input.positionWS);

                half3 color;
                half alpha;
                GeisTakenEvaluateSurface(
                    input.positionOS,
                    input.normalWS,
                    viewDirWS,
                    input.uv,
                    _Time.y,
                    _NoiseScale,
                    _NoiseSpeed,
                    _NoiseFlow.xyz,
                    _VeinThreshold,
                    _VeinSoftness,
                    _VeinIntensity,
                    _StarScale,
                    _StarBrightness,
                    _FresnelPower,
                    _FresnelIntensity,
                    baseMap,
                    _DarkColor,
                    _VeinColor,
                    _FresnelColor,
                    color,
                    alpha);

                GeisTakenApplyDissolve(
                    input.positionOS,
                    _Dissolve,
                    _NoiseScale,
                    _EdgeColor,
                    _EdgeWidth,
                    color,
                    alpha);

                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(normalize(input.normalWS), mainLight.direction));
                color *= lerp(0.55h, 1.0h, ndotl * mainLight.distanceAttenuation);
                color += mainLight.color * ndotl * 0.08h;

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
