Shader "Hidden/ScreenSpaceOutline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _Thickness("Thickness (px)", Float) = 1.0
        _Blend("Blend", Range(0,1)) = 1.0

        // Depth (Roberts + controles de estabilidad)
        _DepthThreshold("Depth Threshold", Float) = 1.0
        _DepthSmoothWidth("Depth Smooth Width", Range(0,1)) = 0.05
        _RobertsCrossMultiplier("Roberts Cross Multiplier", Float) = 1.0
        _DepthAttenuation("Depth Distance Attenuation", Range(0,2)) = 0.0
        _DepthRelativeScale("Depth Relative Scale", Range(0,4)) = 0.0

        // Normales (Roberts + umbral)
        _NormalsThreshold("Normals Threshold", Range(0,2)) = 0.2
        _NormalSensitivity("Normal Sensitivity", Range(0,4)) = 1.0

        // Canales (0=Off, 1=On)
        _UseDepth("Use Depth", Float) = 1
        _UseNormals("Use Normals", Float) = 1
    }
   
    SubShader
    {
        // Tags eliminados para evitar conflictos en un blit de post-proceso.
        Pass
        {
            Name "ScreenSpaceOutline"

            ZWrite Off
            ZTest Always
            Cull Off
            Blend One Zero // mezclo manual con lerp

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

TEXTURE2D(_BlitTexture);
SAMPLER(sampler_BlitTexture);
            
            // RT de normales VS generada por el primer pass
            TEXTURE2D(_SceneViewSpaceNormals);
            SAMPLER(sampler_SceneViewSpaceNormals);

            float4 _OutlineColor;
            float  _Thickness;
            float  _Blend;

            // Depth params
            float  _DepthThreshold;
            float  _DepthSmoothWidth;
            float  _RobertsCrossMultiplier;
            float  _DepthAttenuation;
            float  _DepthRelativeScale;
            
            // Normal params
            float  _NormalsThreshold;
            float  _NormalSensitivity;
            
            float  _UseDepth;
            float  _UseNormals;

            struct Attributes { float4 positionOS: POSITION; uint vertexID : SV_VertexID; };
            struct Varyings   { float4 positionCS: SV_POSITION; float2 uv: TEXCOORD0; };

            // #############################################################################
            // ##############               CORRECCIÓN APLICADA AQUÍ              ##############
            // #############################################################################
            // El vertex shader fue reemplazado por el estándar para un "blit" a pantalla 
            // completa en URP. El anterior usaba TransformObjectToHClip, que es para 
            // renderizar geometría de objetos, no un efecto de post-proceso.
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                OUT.uv = GetFullScreenTriangleTexCoord(IN.vertexID);
                return OUT;
            }

            inline float EyeDepth(float rawDepth){ return LinearEyeDepth(rawDepth, _ZBufferParams); }

            // Fresnel-like con normal en View Space (para relajar el umbral al mirar de canto)
            inline float AngleFactorAtUV(float2 uv){
                float3 n01 = SAMPLE_TEXTURE2D(_SceneViewSpaceNormals, sampler_SceneViewSpaceNormals, uv).xyz;
                float3 nVS = normalize(n01 * 2.0 - 1.0);
                float ndotv01 = saturate(abs(nVS.z)); // 1 frente, 0 canto
                float oneMinus = 1.0 - ndotv01; // 0..1 (canto)
                return (_UseDepth > 0.5) ? (1.0 + oneMinus) : 1.0;
            }

            // --- Roberts Cross en profundidad (gradiente absoluto) ---
            inline float RobertsDepth(float2 uv, float2 texel, float thickness){
                float2 d = texel * thickness;
                float zTR = EyeDepth(SampleSceneDepth(uv + d));
                float zBL = EyeDepth(SampleSceneDepth(uv - d));
                float zTL = EyeDepth(SampleSceneDepth(uv + float2(-d.x, d.y)));
                float zBR = EyeDepth(SampleSceneDepth(uv + float2( d.x,-d.y)));
                float g1 = zTR - zBL;
                float g2 = zTL - zBR;
                float gradSq = g1*g1 + g2*g2;
                return sqrt(max(gradSq, 1e-8));
            }

            // --- Roberts Cross en normales VS ---
            inline float RobertsNormals(float2 uv, float2 texel, float thickness){
                float2 d = texel * thickness;
                float3 nTR = SAMPLE_TEXTURE2D(_SceneViewSpaceNormals, sampler_SceneViewSpaceNormals, uv + d).xyz * 2.0 - 1.0;
                float3 nBL = SAMPLE_TEXTURE2D(_SceneViewSpaceNormals, sampler_SceneViewSpaceNormals, uv - d).xyz * 2.0 - 1.0;
                float3 nTL = SAMPLE_TEXTURE2D(_SceneViewSpaceNormals, sampler_SceneViewSpaceNormals, uv + float2(-d.x, d.y)).xyz * 2.0 - 1.0;
                float3 nBR = SAMPLE_TEXTURE2D(_SceneViewSpaceNormals, sampler_SceneViewSpaceNormals, uv + float2( d.x,-d.y)).xyz * 2.0 - 1.0;
                float3 g1 = nTR - nBL;
                float3 g2 = nTL - nBR;
                float gradSq = dot(g1,g1) + dot(g2,g2);
                return sqrt(max(gradSq, 1e-8));
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 texel = 1.0 / _ScreenParams.xy;
half4 src = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, IN.uv);                float angleFactor = AngleFactorAtUV(IN.uv);
                
                // --- Profundidad ---
                float depthMask = 0.0;
                if (_UseDepth > 0.5)
                {
                    float depthGrad = RobertsDepth(IN.uv, texel, _Thickness);
                    float zC = EyeDepth(SampleSceneDepth(IN.uv)); // z local para terminos dependientes de distancia
                    float distAtt = 1.0 / (1.0 + zC * _DepthAttenuation); // Atenuación por distancia (reduce “relleno” lejos)
                    float relTerm = (_DepthRelativeScale > 0.0) ? (depthGrad / max(zC, 1e-4)) * _DepthRelativeScale : 0.0; // Componente relativa (divide por z local)
                    float depthEdge = (depthGrad * distAtt + relTerm) * _RobertsCrossMultiplier;
                    float t0 = _DepthThreshold * angleFactor; // Umbral con suavizado y relajado por ángulo
                    float t1 = t0 + _DepthSmoothWidth;
                    depthMask = smoothstep(t0, t1, depthEdge);
                }

                // --- Normales ---
                float normalsMask = 0.0;
                if (_UseNormals > 0.5)
                {
                    float normalsGrad = RobertsNormals(IN.uv, texel, _Thickness);
                    normalsGrad *= _NormalSensitivity;
                    normalsMask = step(_NormalsThreshold, normalsGrad);
                }

                // Gate por cobertura de la RT de normales (alpha)
                float2 d = texel * _Thickness;
                float aC  = SAMPLE_TEXTURE2D(_SceneViewSpaceNormals, sampler_SceneViewSpaceNormals, IN.uv).a;
                float aTR = SAMPLE_TEXTURE2D(_SceneViewSpaceNormals, sampler_SceneViewSpaceNormals, IN.uv + d).a;
                float aBL = SAMPLE_TEXTURE2D(_SceneViewSpaceNormals, sampler_SceneViewSpaceNormals, IN.uv - d).a;
                float aTL = SAMPLE_TEXTURE2D(_SceneViewSpaceNormals, sampler_SceneViewSpaceNormals, IN.uv + float2(-d.x, d.y)).a;
                float aBR = SAMPLE_TEXTURE2D(_SceneViewSpaceNormals, sampler_SceneViewSpaceNormals, IN.uv + float2( d.x,-d.y)).a;
                float coverage = saturate(max(max(max(max(aC, aTR), aBL), aTL), aBR));
                
                // Combine solo depth/normals
                float mask = max(depthMask, normalsMask) * coverage;
                half3 outRgb = lerp(src.rgb, _OutlineColor.rgb, mask * _Blend);
                return half4(outRgb, src.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}