Shader "MummysScape/VFX/Stylized Sphere Fire URP"
{
    Properties
    {
        [Header(Colors)]
        _BackgroundColor ("Background Color", Color) = (0.00, 0.12, 0.03, 1.00)
        _BaseColor ("Base Color", Color) = (0.10, 0.95, 0.20, 1.00)
        _HighlightsColor ("Highlights Color", Color) = (0.80, 1.00, 0.45, 1.00)
        _Brightness ("Brightness", Range(0, 5)) = 1.6
        _Alpha ("Alpha", Range(0, 1)) = 0.9
        _AlphaClipThreshold ("Alpha Clip Threshold", Range(0, 1)) = 0.035

        [Header(Vertical Shape)]
        _ObjectBottomY ("Object Bottom Y", Float) = -0.5
        _ObjectTopY ("Object Top Y", Float) = 0.5
        _FlameHeight ("Flame Height", Range(0, 1)) = 0.72
        _TipSoftness ("Tip Softness", Range(0.01, 0.8)) = 0.26
        _BaseWidth ("Base Width", Range(0.01, 0.7)) = 0.25
        _BaseStrength ("Base Strength", Range(0, 1)) = 0.85

        [Header(Flame Motion)]
        _Speed ("Speed", Range(0, 3)) = 0.65
        _HorizontalTiling ("Horizontal Tiling", Range(0.1, 12)) = 4.2
        _VerticalStretch ("Vertical Stretch", Range(0.1, 5)) = 1.55
        _SwayAmount ("Sway Amount", Range(0, 1)) = 0.16
        _FlameCut ("Flame Cut", Range(0, 1)) = 0.43
        _FlameSoftness ("Flame Softness", Range(0.01, 1)) = 0.25
        _NoiseScale ("Noise Scale", Range(0.1, 8)) = 1.15
        _DetailScale ("Detail Scale", Range(0.1, 12)) = 3.25

        [Header(Optional Silhouette)]
        _VertexDisplacement ("Vertex Displacement", Range(0, 0.25)) = 0.025

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "UniversalMaterialType" = "Unlit"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull [_Cull]

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BackgroundColor;
                float4 _BaseColor;
                float4 _HighlightsColor;
                float _Brightness;
                float _Alpha;
                float _AlphaClipThreshold;

                float _ObjectBottomY;
                float _ObjectTopY;
                float _FlameHeight;
                float _TipSoftness;
                float _BaseWidth;
                float _BaseStrength;

                float _Speed;
                float _HorizontalTiling;
                float _VerticalStretch;
                float _SwayAmount;
                float _FlameCut;
                float _FlameSoftness;
                float _NoiseScale;
                float _DetailScale;

                float _VertexDisplacement;
                float _Cull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i + float2(0.0, 0.0));
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 uv)
            {
                float value = 0.0;
                float amplitude = 0.5;

                value += ValueNoise(uv) * amplitude;
                uv *= 2.02;
                amplitude *= 0.5;

                value += ValueNoise(uv) * amplitude;
                uv *= 2.03;
                amplitude *= 0.5;

                value += ValueNoise(uv) * amplitude;
                uv *= 2.01;
                amplitude *= 0.5;

                value += ValueNoise(uv) * amplitude;

                return saturate(value / 0.9375);
            }

            float GetHeight01(float y)
            {
                float heightRange = max(0.0001, _ObjectTopY - _ObjectBottomY);
                return saturate((y - _ObjectBottomY) / heightRange);
            }

            float GetTipFade(float height01)
            {
                float tipEnd = min(1.0, _FlameHeight + max(0.01, _TipSoftness));
                return 1.0 - smoothstep(_FlameHeight, tipEnd, height01);
            }

            float GetBaseMask(float height01)
            {
                return (1.0 - smoothstep(0.0, _BaseWidth, height01)) * _BaseStrength;
            }

            float GetFlameShape(float3 positionOS, float height01, float time)
            {
                float swayNoise = Fbm(float2(positionOS.z * 2.25, height01 * 2.4 + time * 0.25));
                float sway = (swayNoise * 2.0 - 1.0) * _SwayAmount * smoothstep(0.03, 0.9, height01);

                float2 flameUV;
                flameUV.x = positionOS.x * _HorizontalTiling + sway;
                flameUV.y = height01 * _VerticalStretch - time;

                float mainNoise = Fbm(flameUV * _NoiseScale);
                float detailNoise = Fbm(flameUV * _DetailScale + float2(7.13, -time * 0.55));

                float verticalTongues = 1.0 - abs(frac(flameUV.x + mainNoise * 0.65) - 0.5) * 2.0;
                verticalTongues = pow(saturate(verticalTongues), 1.85);

                float bottomBoost = (1.0 - height01) * 0.16;
                float combinedNoise = mainNoise * 0.56 + detailNoise * 0.18 + verticalTongues * 0.48 + bottomBoost;
                float flame = smoothstep(_FlameCut, _FlameCut + _FlameSoftness, combinedNoise);

                return saturate(flame * GetTipFade(height01));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 positionOS = input.positionOS.xyz;
                float height01 = GetHeight01(positionOS.y);
                float time = _Time.y * _Speed;

                float shape = GetFlameShape(positionOS, height01, time);
                float displacementMask = smoothstep(0.04, _FlameHeight, height01) * GetTipFade(height01);
                float displacementNoise = Fbm(float2(positionOS.x * _HorizontalTiling, height01 * _VerticalStretch - time));
                float displacement = _VertexDisplacement * displacementMask * shape * lerp(0.35, 1.0, displacementNoise);

                positionOS += normalize(input.normalOS) * displacement;

                output.positionOS = positionOS;
                output.positionCS = TransformObjectToHClip(positionOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float height01 = GetHeight01(input.positionOS.y);
                float time = _Time.y * _Speed;

                float flames = GetFlameShape(input.positionOS, height01, time);
                float baseMask = GetBaseMask(height01);
                float finalShape = saturate(max(flames, baseMask));

                float2 hotUV = float2(input.positionOS.x * (_HorizontalTiling + 1.0), height01 * (_VerticalStretch + 1.0) - time * 1.2);
                float hotNoise = Fbm(hotUV * _DetailScale);
                float hotMask = smoothstep(0.55, 0.92, finalShape * 0.7 + hotNoise * 0.35) * finalShape;

                float3 bodyColor = lerp(_BackgroundColor.rgb, _BaseColor.rgb, finalShape);
                float3 fireColor = lerp(bodyColor, _HighlightsColor.rgb, hotMask);
                fireColor *= _Brightness;

                float finalAlpha = saturate(finalShape * _Alpha);
                clip(finalAlpha - _AlphaClipThreshold);

                return half4(fireColor, finalAlpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
