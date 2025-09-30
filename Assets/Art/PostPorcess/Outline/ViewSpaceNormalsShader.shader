Shader "Hidden/ViewSpaceNormalsShader"
{
    Properties { }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalRenderPipeline" }
        Pass
        {
            Name "NormalsVS"
            // Va a usarse como overrideMaterial
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            Cull Back
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalVS   : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                //directo a clip desde object
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Normal OS -> WS -> VS
                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 nVS = mul((float3x3)UNITY_MATRIX_V, nWS);
                OUT.normalVS = normalize(nVS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Remap [-1,1] -> [0,1]
                float3 n01 = IN.normalVS * 0.5f + 0.5f;
                return half4(n01, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}