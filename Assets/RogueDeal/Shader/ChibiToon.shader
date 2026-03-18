Shader "Custom/ChibiToon"
{
    Properties
    {
        // --- Base ---
        _MainTex        ("Albedo (RGB)", 2D)            = "white" {}
        _Color          ("Base Color Tint", Color)      = (1,1,1,1)

        // --- Normal ---
        _NormalMap      ("Normal Map", 2D)              = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,2)) = 1.0

        // --- Toon Shading ---
        _ShadowColor    ("Shadow Color", Color)         = (0.55, 0.45, 0.55, 1)
        _ShadowThreshold("Shadow Threshold", Range(-1,1)) = 0.0
        _ShadowSmooth   ("Shadow Softness", Range(0,0.5)) = 0.05
        _MidShadowColor ("Mid-tone Shadow Color", Color) = (0.75, 0.65, 0.7, 1)
        _MidThreshold   ("Mid Shadow Threshold", Range(-1,1)) = 0.3
        _MidSmooth      ("Mid Shadow Softness", Range(0,0.5)) = 0.04

        // --- Specular (stylized) ---
        _SpecColor      ("Specular Color", Color)       = (1, 0.95, 0.85, 1)
        _SpecThreshold  ("Specular Threshold", Range(0,1)) = 0.92
        _SpecSmooth     ("Specular Softness", Range(0,0.1)) = 0.02
        _SpecIntensity  ("Specular Intensity", Range(0,2)) = 1.0

        // --- Rim Light ---
        _RimColor       ("Rim Color", Color)            = (0.7, 0.75, 1.0, 1)
        _RimThreshold   ("Rim Threshold", Range(0,1))   = 0.6
        _RimSmooth      ("Rim Softness", Range(0,0.2))  = 0.05
        _RimIntensity   ("Rim Intensity", Range(0,3))   = 1.2

        // --- Subsurface Scatter Fake ---
        _SSSColor       ("SSS Color (skin warmth)", Color) = (1.0, 0.4, 0.3, 1)
        _SSSStrength    ("SSS Strength", Range(0,1))    = 0.25
        _SSSThreshold   ("SSS Threshold", Range(-1,1))  = -0.2
        _SSSSmooth      ("SSS Softness", Range(0,0.5))  = 0.3

        // --- Outline ---
        _OutlineColor   ("Outline Color", Color)        = (0.1, 0.08, 0.12, 1)
        _OutlineWidth   ("Outline Width", Range(0,0.05))= 0.012

        // --- Ambient / GI ---
        _AmbientColor   ("Ambient Color", Color)        = (0.25, 0.22, 0.3, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // ─────────────────────────────────────────────
        // PASS 1 — Outline (back-face expanded)
        // ─────────────────────────────────────────────
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT;
                // Expand along normal in clip space for consistent outline thickness
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 posCS    = TransformObjectToHClip(IN.positionOS.xyz);
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS);
                float2 offset   = normalize(normalCS.xy) * (_OutlineWidth * posCS.w);
                posCS.xy       += offset;
                OUT.positionCS  = posCS;
                return OUT;
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                return half4(_OutlineColor.rgb, 1);
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────
        // PASS 2 — Main Toon Lit Pass
        // ─────────────────────────────────────────────
        Pass
        {
            Name "ToonLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex ToonVert
            #pragma fragment ToonFrag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ---- Textures ----
            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ShadowColor;
                float  _ShadowThreshold;
                float  _ShadowSmooth;
                float4 _MidShadowColor;
                float  _MidThreshold;
                float  _MidSmooth;
                float4 _SpecColor;
                float  _SpecThreshold;
                float  _SpecSmooth;
                float  _SpecIntensity;
                float4 _RimColor;
                float  _RimThreshold;
                float  _RimSmooth;
                float  _RimIntensity;
                float4 _SSSColor;
                float  _SSSStrength;
                float  _SSSThreshold;
                float  _SSSSmooth;
                float4 _AmbientColor;
                float  _NormalStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float3 tangentWS    : TEXCOORD3;
                float3 bitangentWS  : TEXCOORD4;
                float4 shadowCoord  : TEXCOORD5;
                float  fogFactor    : TEXCOORD6;
            };

            // Smooth step ramp for toon bands
            float ToonRamp(float value, float threshold, float smooth_)
            {
                return smoothstep(threshold - smooth_, threshold + smooth_, value);
            }

            Varyings ToonVert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS  = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS    = nrmInputs.normalWS;
                OUT.tangentWS   = nrmInputs.tangentWS;
                OUT.bitangentWS = nrmInputs.bitangentWS;
                OUT.shadowCoord = GetShadowCoord(posInputs);
                OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 ToonFrag(Varyings IN) : SV_Target
            {
                // ── 1. Sample textures ──────────────────────────────────────
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv),
                    _NormalStrength
                );
                float3x3 TBN = float3x3(
                    normalize(IN.tangentWS),
                    normalize(IN.bitangentWS),
                    normalize(IN.normalWS)
                );
                float3 N = normalize(mul(normalTS, TBN));

                // ── 2. Light & view dirs ────────────────────────────────────
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 L = normalize(mainLight.direction);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float3 H = normalize(L + V);

                float NdotL = dot(N, L);
                float NdotV = dot(N, V);
                float NdotH = dot(N, H);

                // ── 3. Shadow attenuation ───────────────────────────────────
                float shadowAtten = mainLight.shadowAttenuation;
                float lightVal    = NdotL * shadowAtten;

                // ── 4. Toon ramps (3 bands: lit / mid / shadow) ─────────────
                float litMask  = ToonRamp(lightVal, _MidThreshold,   _MidSmooth);
                float midMask  = ToonRamp(lightVal, _ShadowThreshold, _ShadowSmooth);

                // Blend colors across bands
                half3 toonColor  = lerp(_ShadowColor.rgb,    _MidShadowColor.rgb, midMask);
                toonColor        = lerp(toonColor,            half3(1,1,1),        litMask);

                // ── 5. Fake SSS (warm back-scatter on skin) ─────────────────
                float sssVal   = ToonRamp(-NdotL, _SSSThreshold, _SSSSmooth);
                half3 sssColor = _SSSColor.rgb * sssVal * _SSSStrength;

                // ── 6. Stylized specular ────────────────────────────────────
                float specVal   = ToonRamp(NdotH, _SpecThreshold, _SpecSmooth);
                half3 specColor = _SpecColor.rgb * specVal * _SpecIntensity * litMask;

                // ── 7. Rim light ────────────────────────────────────────────
                float rimVal   = 1.0 - saturate(NdotV);
                float rimMask  = ToonRamp(rimVal, _RimThreshold, _RimSmooth);
                rimMask       *= saturate(NdotL + 0.5); // no rim in deep shadow
                half3 rimColor = _RimColor.rgb * rimMask * _RimIntensity;

                // ── 8. Combine ──────────────────────────────────────────────
                half3 ambient  = _AmbientColor.rgb;
                half3 lighting = ambient + toonColor * mainLight.color;

                half3 finalColor = albedo.rgb * lighting
                                 + albedo.rgb * sssColor
                                 + specColor
                                 + rimColor;

                // ── 9. Fog ──────────────────────────────────────────────────
                finalColor = MixFog(finalColor, IN.fogFactor);

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }

        // Shadow caster pass (use URP built-in)
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }

    FallBack "Universal Render Pipeline/Lit"
    CustomEditor "UnityEditor.ShaderGUI"
}
