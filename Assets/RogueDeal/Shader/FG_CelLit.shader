Shader "FunderGames/CelLitURP"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        _ShadowColor("Shadow Color", Color) = (0.72,0.78,0.9,1)
        _ShadowThreshold("Shadow Threshold", Range(0,1)) = 0.5
        _ShadowSmoothness("Shadow Smoothness", Range(0.001,0.2)) = 0.03

        _HighlightColor("Highlight Color", Color) = (1,1,1,1)
        _SpecularThreshold("Spec Threshold", Range(0,1)) = 0.75
        _SpecularSmoothness("Spec Smoothness", Range(0.001,0.2)) = 0.03
        _SpecularStrength("Spec Strength", Range(0,2)) = 0.35

        _RimColor("Rim Color", Color) = (0.7,0.85,1,1)
        _RimPower("Rim Power", Range(0.1,8)) = 3
        _RimStrength("Rim Strength", Range(0,2)) = 0.35

        _AmbientStrength("Ambient Strength", Range(0,1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float4 _HighlightColor;
                float4 _RimColor;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float _SpecularThreshold;
                float _SpecularSmoothness;
                float _SpecularStrength;
                float _RimPower;
                float _RimStrength;
                float _AmbientStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS   : SV_POSITION;
                float2 uv            : TEXCOORD0;
                float3 positionWS    : TEXCOORD1;
                float3 normalWS      : TEXCOORD2;
                float4 shadowCoord   : TEXCOORD3;
                float3 viewDirWS     : TEXCOORD4;
                float fogCoord       : TEXCOORD5;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normalize(normalInputs.normalWS);
                OUT.uv = IN.uv;
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                OUT.shadowCoord = GetShadowCoord(posInputs);
                OUT.fogCoord = ComputeFogFactor(posInputs.positionCS.z);

                return OUT;
            }

            float ToonStep(float value, float threshold, float smoothness)
            {
                return smoothstep(threshold - smoothness, threshold + smoothness, value);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 albedo = baseTex.rgb * _BaseColor.rgb;

                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 lightDir = normalize(mainLight.direction);

                float NdotL = saturate(dot(normalWS, lightDir));
                float shadowAtten = mainLight.shadowAttenuation;
                float litAmountRaw = NdotL * shadowAtten;

                float toonLight = ToonStep(litAmountRaw, _ShadowThreshold, _ShadowSmoothness);

                float3 litColor = albedo;
                float3 shadowColor = albedo * _ShadowColor.rgb;
                float3 color = lerp(shadowColor, litColor, toonLight);

                float3 halfDir = normalize(lightDir + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specBand = ToonStep(NdotH, _SpecularThreshold, _SpecularSmoothness) * _SpecularStrength;
                color += specBand * _HighlightColor.rgb * mainLight.color;

                float rim = pow(1.0 - saturate(dot(viewDirWS, normalWS)), _RimPower);
                color += rim * _RimStrength * _RimColor.rgb;

                color += albedo * _AmbientStrength;

                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light light = GetAdditionalLight(i, IN.positionWS);
                    float3 addDir = normalize(light.direction);
                    float addNdotL = saturate(dot(normalWS, addDir));
                    float addBand = ToonStep(addNdotL * light.distanceAttenuation * light.shadowAttenuation,
                                             _ShadowThreshold, _ShadowSmoothness);
                    color += albedo * addBand * light.color * 0.35;
                }
                #endif

                color = MixFog(color, IN.fogCoord);

                return half4(color, baseTex.a * _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // LerpWhiteTo is used by Shadows.hlsl but ShadowCasterPass.hlsl doesn't include CommonMaterial.hlsl where it's defined
            real LerpWhiteTo(real b, real t) { return (1.0 - t) + b * t; }
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    FallBack Off
}