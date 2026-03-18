Shader "FunderGames/OutlineURP"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0.05, 0.07, 0.1, 1)
        _OutlineWidth("Outline Width", Range(0.0005, 0.05)) = 0.01
        _ZOffset("Depth Offset", Range(0, 0.01)) = 0.001
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry+10"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _ZOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 normalOS = normalize(IN.normalOS);
                float3 expandedPosOS = IN.positionOS.xyz + normalOS * _OutlineWidth;

                VertexPositionInputs posInputs = GetVertexPositionInputs(expandedPosOS);
                OUT.positionCS = posInputs.positionCS;

                // Push slightly back to reduce z-fighting
                OUT.positionCS.z += _ZOffset;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}