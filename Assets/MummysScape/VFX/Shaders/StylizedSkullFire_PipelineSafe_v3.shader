Shader "MummysScape/VFX/Stylized Skull Fire Pipeline Safe v6"
{
    Properties
    {
        _FlameMaskTex ("Flame Mask", 2D) = "white" {}

        [HDR]_BottomColor ("Bottom Color", Color) = (0.10, 0.90, 0.20, 1.0)
        [HDR]_MiddleColor ("Middle Color", Color) = (0.45, 1.20, 0.35, 1.0)
        [HDR]_TopColor ("Top Color", Color) = (0.90, 1.60, 0.70, 1.0)
        [HDR]_GlowColor ("Glow Color", Color) = (0.05, 0.70, 0.18, 1.0)

        _GlobalBrightness ("Global Brightness", Range(0, 8)) = 1.8
        _Alpha ("Alpha", Range(0, 1)) = 0.92
        _AlphaClip ("Alpha Clip", Range(0, 0.25)) = 0.01

        _ObjectBottomY ("Object Bottom Y", Float) = -0.5
        _ObjectTopY ("Object Top Y", Float) = 0.5

        _Speed ("Speed", Range(0, 4)) = 1.2
        _MaskTilingX ("Mask Tiling X", Range(1, 12)) = 4.2
        _MaskStretchY ("Mask Stretch Y", Range(0.5, 5)) = 2.15
        _DistortStrength ("Distort Strength", Range(0, 1)) = 0.20
        _DistortScale ("Distort Scale", Range(0.5, 8)) = 3.4
        _SwayStrength ("Sway Strength", Range(0, 1)) = 0.09

        _PrimaryContribution ("Primary Contribution", Range(0, 1)) = 1.0
        _SecondaryContribution ("Secondary Contribution", Range(0, 1)) = 0.60
        _ThirdContribution ("Third Contribution", Range(0, 1)) = 0.35

        _BaseHeight ("Base Height", Range(0.01, 0.6)) = 0.24
        _BaseSoftness ("Base Softness", Range(0.01, 0.6)) = 0.28
        _BaseStrength ("Base Strength", Range(0, 1)) = 0.60
        _BaseFlameDetail ("Base Flame Detail", Range(0, 8)) = 2.0
        _BaseTransparency ("Base Transparency", Range(0, 1)) = 0.70

        _FadeStart ("Fade Start", Range(0, 1.2)) = 0.56
        _FadeEnd ("Fade End", Range(0, 1.4)) = 0.96
        _FadePower ("Fade Power", Range(0.5, 4)) = 1.35
        _TopTaper ("Top Taper", Range(0, 1)) = 0.42

        _CoreFill ("Core Fill", Range(0, 1)) = 0.36
        _InnerNoiseContribution ("Inner Noise Contribution", Range(0, 1)) = 0.22

        _RimPower ("Rim Power", Range(0.5, 8)) = 2.0
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.72

        _VertexDisplacement ("Vertex Displacement", Range(0, 0.2)) = 0.04
        _XZMotionStrength ("XZ Motion Strength", Range(0, 0.2)) = 0.035
        _XZNoiseScale ("XZ Noise Scale", Range(0.5, 8)) = 2.6
        _XZNoiseSpeed ("XZ Noise Speed", Range(0, 4)) = 1.1

        _BottomEmission ("Bottom Emission", Range(0, 8)) = 1.2
        _MiddleEmission ("Middle Emission", Range(0, 8)) = 2.0
        _TopEmission ("Top Emission", Range(0, 8)) = 3.0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Sphere"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull [_Cull]

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _FlameMaskTex;

            float4 _BottomColor;
            float4 _MiddleColor;
            float4 _TopColor;
            float4 _GlowColor;

            float _GlobalBrightness;
            float _Alpha;
            float _AlphaClip;

            float _ObjectBottomY;
            float _ObjectTopY;

            float _Speed;
            float _MaskTilingX;
            float _MaskStretchY;
            float _DistortStrength;
            float _DistortScale;
            float _SwayStrength;

            float _PrimaryContribution;
            float _SecondaryContribution;
            float _ThirdContribution;

            float _BaseHeight;
            float _BaseSoftness;
            float _BaseStrength;
            float _BaseFlameDetail;
            float _BaseTransparency;

            float _FadeStart;
            float _FadeEnd;
            float _FadePower;
            float _TopTaper;

            float _CoreFill;
            float _InnerNoiseContribution;

            float _RimPower;
            float _RimStrength;

            float _VertexDisplacement;
            float _XZMotionStrength;
            float _XZNoiseScale;
            float _XZNoiseSpeed;

            float _BottomEmission;
            float _MiddleEmission;
            float _TopEmission;

            #define TWO_PI 6.28318530718

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash21(i + float2(0.0, 0.0));
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 uv)
            {
                float value = 0.0;
                float amplitude = 0.5;

                value += valueNoise(uv) * amplitude;
                uv *= 2.03;
                amplitude *= 0.5;

                value += valueNoise(uv) * amplitude;
                uv *= 2.01;
                amplitude *= 0.5;

                value += valueNoise(uv) * amplitude;
                uv *= 2.02;
                amplitude *= 0.5;

                value += valueNoise(uv) * amplitude;
                return saturate(value / 0.9375);
            }

            float height01(float y)
            {
                float rangeY = max(0.0001, _ObjectTopY - _ObjectBottomY);
                return saturate((y - _ObjectBottomY) / rangeY);
            }

            float topFade(float h)
            {
                float fade = 1.0 - smoothstep(_FadeStart, _FadeEnd, h);
                fade = pow(saturate(fade), _FadePower);
                return saturate(fade);
            }

            float baseMask(float h)
            {
                float endValue = _BaseHeight + _BaseSoftness;
                float mask = 1.0 - smoothstep(0.0, endValue, h);
                return saturate(mask * _BaseStrength);
            }

            float2 cylindricalUV(float3 positionOS, float h)
            {
                float angle = atan2(positionOS.z, positionOS.x);
                float u = angle / TWO_PI + 0.5;
                return float2(u, h);
            }

            float sampleFlameLayer(float2 cylUV, float time, float layerOffset, float tilingAdd, float stretchAdd)
            {
                float distortNoise = fbm(float2(
                    cylUV.x * _DistortScale + layerOffset,
                    cylUV.y * (_DistortScale * 0.8) - time * 0.45
                ));

                float distort = distortNoise * 2.0 - 1.0;
                float sway = sin((cylUV.y * 8.0 + layerOffset * 4.2) + time * 1.6) * _SwayStrength;
                float topNarrow = lerp(1.0, 1.0 - _TopTaper, cylUV.y);

                float2 uv;
                uv.x = frac(cylUV.x * (_MaskTilingX + tilingAdd) * topNarrow + distort * _DistortStrength + sway + layerOffset);
                uv.y = frac(cylUV.y * (_MaskStretchY + stretchAdd) - time + distort * 0.08);

                return tex2D(_FlameMaskTex, uv).r;
            }

            float2 randomXZMotion(float3 positionOS, float h, float time)
            {
                float noiseX = fbm(float2(positionOS.z * _XZNoiseScale, h * 2.0 - time * _XZNoiseSpeed));
                float noiseZ = fbm(float2(positionOS.x * _XZNoiseScale + 7.31, h * 2.4 - time * (_XZNoiseSpeed * 1.13)));

                float2 dir = float2(noiseX * 2.0 - 1.0, noiseZ * 2.0 - 1.0);
                dir = normalize(dir + float2(0.0001, 0.0001));

                float strength = smoothstep(0.06, 0.78, h) * topFade(h);
                return dir * (_XZMotionStrength * strength);
            }

            float proceduralVertexMask(float2 cuv, float h, float time)
            {
                float noise = fbm(float2(cuv.x * (_DistortScale + 1.5), h * (_DistortScale + 0.8) - time * 0.7));
                float tongues = 1.0 - abs(frac(cuv.x * _MaskTilingX + noise * 0.35) - 0.5) * 2.0;
                tongues = pow(saturate(tongues), 1.8);
                return saturate(noise * 0.6 + tongues * 0.4);
            }

            float getBaseContribution(float2 cuv, float h, float time)
            {
                float baseZone = baseMask(h);

                float baseA = sampleFlameLayer(cuv, time * 1.12, 0.17, _BaseFlameDetail, 0.20);
                float baseB = sampleFlameLayer(cuv, time * 1.31, 0.49, _BaseFlameDetail + 0.8, 0.32);

                float layered = max(baseA, baseB) * baseZone;
                float softFill = baseZone * _BaseTransparency;

                return saturate(softFill * 0.45 + layered * 0.85);
            }

            float getShape(float3 positionOS, float3 normalWS, float3 viewDirWS, float time)
            {
                float h = height01(positionOS.y);
                float2 cuv = cylindricalUV(positionOS, h);

                float primary = sampleFlameLayer(cuv, time, 0.00, 0.0, 0.0) * _PrimaryContribution;
                float secondary = sampleFlameLayer(cuv, time * 1.13, 0.37, 0.3, 0.08) * _SecondaryContribution;
                float third = sampleFlameLayer(cuv, time * 1.28, 0.61, 0.7, 0.14) * _ThirdContribution;

                float flames = saturate(max(primary, max(secondary, third)));

                float innerNoise = fbm(float2(
                    cuv.x * (_DistortScale + 1.8),
                    cuv.y * (_DistortScale + 0.7) - time * 0.58
                ));

                float core = saturate(_CoreFill + innerNoise * _InnerNoiseContribution - h * 0.24);
                float fade = topFade(h);
                float baseContribution = getBaseContribution(cuv, h, time);

                float rim = pow(1.0 - saturate(dot(normalize(normalWS), normalize(viewDirWS))), _RimPower);
                rim = lerp(1.0, rim, _RimStrength);

                float body = max(flames, core * 0.40);
                body *= fade;
                body *= rim;

                // mezcla suave con la base, no corte duro
                float shape = saturate(body + baseContribution * (1.0 - body) * 0.90);

                return saturate(shape);
            }

            float3 getHeightColor(float h)
            {
                float midLerp = smoothstep(0.08, 0.55, h);
                float topLerp = smoothstep(0.42, 0.95, h);

                float3 lowMid = lerp(_BottomColor.rgb, _MiddleColor.rgb, midLerp);
                return lerp(lowMid, _TopColor.rgb, topLerp);
            }

            float getHeightEmission(float h)
            {
                float midLerp = smoothstep(0.08, 0.55, h);
                float topLerp = smoothstep(0.42, 0.95, h);

                float lowMid = lerp(_BottomEmission, _MiddleEmission, midLerp);
                return lerp(lowMid, _TopEmission, topLerp);
            }

            v2f vert(appdata v)
            {
                v2f o;

                float3 posOS = v.vertex.xyz;
                float3 normalWS = UnityObjectToWorldNormal(v.normal);
                float3 worldPos = mul(unity_ObjectToWorld, float4(posOS, 1.0)).xyz;
                float3 viewDirWS = _WorldSpaceCameraPos.xyz - worldPos;

                float h = height01(posOS.y);
                float time = _Time.y * _Speed;
                float2 cuv = cylindricalUV(posOS, h);

                float mask = proceduralVertexMask(cuv, h, time);
                float displacementNoise = fbm(float2(
                    cuv.x * (_DistortScale + 0.5),
                    cuv.y * (_DistortScale + 0.5) - time * 0.8
                ));

                float rim = pow(1.0 - saturate(dot(normalize(normalWS), normalize(viewDirWS))), _RimPower);
                rim = lerp(1.0, rim, _RimStrength);

                float displacementMask = mask * topFade(h) * rim * smoothstep(0.08, 0.95, h);
                float displacement = _VertexDisplacement * lerp(0.35, 1.0, displacementNoise) * displacementMask;

                posOS += normalize(v.normal) * displacement;

                float2 lateralOffset = randomXZMotion(posOS, h, time);
                posOS.x += lateralOffset.x;
                posOS.z += lateralOffset.y;

                worldPos = mul(unity_ObjectToWorld, float4(posOS, 1.0)).xyz;

                o.pos = UnityObjectToClipPos(float4(posOS, 1.0));
                o.positionOS = posOS;
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.viewDirWS = _WorldSpaceCameraPos.xyz - worldPos;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float time = _Time.y * _Speed;
                float h = height01(i.positionOS.y);
                float2 cuv = cylindricalUV(i.positionOS, h);

                float shape = getShape(i.positionOS, i.normalWS, i.viewDirWS, time);
                float fade = topFade(h);

                float hotA = sampleFlameLayer(cuv, time * 1.32, 0.19, 0.2, 0.1);
                float hotB = sampleFlameLayer(cuv, time * 1.49, 0.53, 0.6, 0.18);
                float hotNoise = fbm(float2(
                    cuv.x * (_DistortScale + 2.3),
                    cuv.y * (_DistortScale + 1.4) - time * 0.88
                ));

                float hotMask = max(hotA, hotB * 0.7);
                hotMask = saturate((hotMask * 0.70 + hotNoise * 0.40) * fade) * shape;

                float rim = pow(
                    1.0 - saturate(dot(normalize(i.normalWS), normalize(i.viewDirWS))),
                    max(0.4, _RimPower - 0.6)
                );

                float3 heightColor = getHeightColor(h);
                float emission = getHeightEmission(h);

                float3 color = lerp(_GlowColor.rgb, heightColor, shape);
                color = lerp(color, heightColor * 1.25, hotMask);
                color += _GlowColor.rgb * rim * shape * 0.22;
                color *= emission * _GlobalBrightness;

                float baseAlpha = baseMask(h) * 0.18;
                float alpha = saturate((shape + baseAlpha) * _Alpha);

                // arriba cae más suave
                alpha *= lerp(0.82, 1.0, fade);

                clip(alpha - _AlphaClip);
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}