Shader "MummysScape/VFX/Stylized Skull Fire Pipeline Safe v3"
{
    /*
    Summary:
    - Shader transparente no iluminado para simular fuego estilizado alrededor de un cráneo.
    - Usa una textura 2D de llama como máscara principal, proyectada cilíndricamente sobre una esfera/casco.
    - La textura se anima hacia arriba, se distorsiona con ruido procedural y se mezcla en dos capas.
    - El vertex displacement NO samplea textura; usa ruido procedural para evitar errores D3D en vertex shader.
    - Las propiedades de material están dentro de UnityPerMaterial para evitar el warning de SRP Batcher.
    */

    Properties
    {
        [Header(Texture)]
        _FlameMaskTex ("Flame Mask", 2D) = "white" {}

        [Header(Colors)]
        _BaseColor ("Base Color", Color) = (0.20, 0.95, 0.15, 1.00)
        _CoreColor ("Core Color", Color) = (0.90, 1.00, 0.55, 1.00)
        _OuterGlowColor ("Outer Glow Color", Color) = (0.05, 0.90, 0.20, 1.00)
        _Brightness ("Brightness", Range(0, 8)) = 2.1
        _Alpha ("Alpha", Range(0, 1)) = 0.95
        _AlphaClip ("Alpha Clip", Range(0, 1)) = 0.04

        [Header(Vertical Mapping)]
        _ObjectBottomY ("Object Bottom Y", Float) = -0.5
        _ObjectTopY ("Object Top Y", Float) = 0.5
        _FlameHeight ("Flame Height", Range(0.1, 1.4)) = 1.0
        _TipSoftness ("Tip Softness", Range(0.01, 0.8)) = 0.25
        _BottomFill ("Bottom Fill", Range(0, 1)) = 0.55

        [Header(Motion)]
        _Speed ("Speed", Range(0, 4)) = 1.2
        _MaskTilingX ("Mask Tiling X", Range(1, 12)) = 3.5
        _MaskStretchY ("Mask Stretch Y", Range(0.5, 5)) = 1.85
        _DistortStrength ("Distort Strength", Range(0, 1)) = 0.20
        _DistortScale ("Distort Scale", Range(0.5, 8)) = 3.4
        _SecondaryContribution ("Secondary Layer Contribution", Range(0, 1)) = 0.48
        _SwayStrength ("Sway Strength", Range(0, 1)) = 0.08

        [Header(Shape)]
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.0
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.75
        _CoreFill ("Core Fill", Range(0, 1)) = 0.56
        _InnerNoiseContribution ("Inner Noise Contribution", Range(0, 1)) = 0.24
        _VertexDisplacement ("Vertex Displacement", Range(0, 0.2)) = 0.035
        _TopTaper ("Top Taper", Range(0, 1)) = 0.35

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Sphere"
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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _CoreColor;
                float4 _OuterGlowColor;
                float _Brightness;
                float _Alpha;
                float _AlphaClip;

                float _ObjectBottomY;
                float _ObjectTopY;
                float _FlameHeight;
                float _TipSoftness;
                float _BottomFill;

                float _Speed;
                float _MaskTilingX;
                float _MaskStretchY;
                float _DistortStrength;
                float _DistortScale;
                float _SecondaryContribution;
                float _SwayStrength;

                float _RimPower;
                float _RimStrength;
                float _CoreFill;
                float _InnerNoiseContribution;
                float _VertexDisplacement;
                float _TopTaper;
            CBUFFER_END

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

            float tipFade(float h)
            {
                float endH = min(1.4, _FlameHeight + max(0.01, _TipSoftness));
                return 1.0 - smoothstep(_FlameHeight, endH, h);
            }

            float2 cylindricalUV(float3 positionOS, float h)
            {
                float angle = atan2(positionOS.z, positionOS.x);
                float u = angle / TWO_PI + 0.5;
                return float2(u, h);
            }

            float sampleFlameMask(float2 cylUV, float time, float layerOffset)
            {
                float2 distortUV = float2(cylUV.x * _DistortScale + layerOffset, cylUV.y * (_DistortScale * 0.8) - time * 0.45);
                float distort = fbm(distortUV) * 2.0 - 1.0;
                float sway = sin((cylUV.y * 8.0 + layerOffset * 4.1) + time * 1.6) * _SwayStrength;
                float topNarrow = lerp(1.0, 1.0 - _TopTaper, cylUV.y);

                float2 uv;
                uv.x = frac(cylUV.x * _MaskTilingX * topNarrow + distort * _DistortStrength + sway + layerOffset);
                uv.y = frac(cylUV.y * _MaskStretchY - time + distort * 0.08);

                return tex2D(_FlameMaskTex, uv).r;
            }

            float proceduralVertexMask(float2 cuv, float h, float time)
            {
                float noise = fbm(float2(cuv.x * (_DistortScale + 1.5), h * (_DistortScale + 0.8) - time * 0.7));
                float tongues = 1.0 - abs(frac(cuv.x * _MaskTilingX + noise * 0.35) - 0.5) * 2.0;
                tongues = pow(saturate(tongues), 1.8);
                return saturate(noise * 0.6 + tongues * 0.4);
            }

            float getShape(float3 positionOS, float3 normalWS, float3 viewDirWS, float time)
            {
                float h = height01(positionOS.y);
                float2 cuv = cylindricalUV(positionOS, h);
                float tFade = tipFade(h);

                float primary = sampleFlameMask(cuv, time, 0.0);
                float secondary = sampleFlameMask(cuv, time * 1.13, 0.37) * _SecondaryContribution;
                float mask = max(primary, secondary);

                float innerNoise = fbm(float2(cuv.x * (_DistortScale + 1.8), cuv.y * (_DistortScale + 0.7) - time * 0.58));
                float core = saturate(_CoreFill + innerNoise * _InnerNoiseContribution - h * 0.18);
                float bottom = (1.0 - smoothstep(0.0, 0.26, h)) * _BottomFill;

                float rim = pow(1.0 - saturate(dot(normalize(normalWS), normalize(viewDirWS))), _RimPower);
                rim = lerp(1.0, rim, _RimStrength);

                float shape = max(mask, core * 0.55);
                shape *= tFade;
                shape *= rim;
                shape = max(shape, bottom);

                return saturate(shape);
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
                float displacementNoise = fbm(float2(cuv.x * (_DistortScale + 0.5), cuv.y * (_DistortScale + 0.5) - time * 0.8));
                float rim = pow(1.0 - saturate(dot(normalize(normalWS), normalize(viewDirWS))), _RimPower);
                rim = lerp(1.0, rim, _RimStrength);

                float displacementMask = mask * tipFade(h) * rim * smoothstep(0.08, 0.95, h);
                float displacement = _VertexDisplacement * lerp(0.35, 1.0, displacementNoise) * displacementMask;
                posOS += normalize(v.normal) * displacement;

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

                float hotMask = sampleFlameMask(cuv, time * 1.32, 0.19);
                float hotNoise = fbm(float2(cuv.x * (_DistortScale + 2.3), cuv.y * (_DistortScale + 1.4) - time * 0.88));
                hotMask = saturate((hotMask * 0.75 + hotNoise * 0.50) * tipFade(h)) * shape;

                float rim = pow(1.0 - saturate(dot(normalize(i.normalWS), normalize(i.viewDirWS))), max(0.4, _RimPower - 0.6));
                float3 color = lerp(_OuterGlowColor.rgb, _BaseColor.rgb, shape);
                color = lerp(color, _CoreColor.rgb, hotMask);
                color += _OuterGlowColor.rgb * rim * shape * 0.35;
                color *= _Brightness;

                float alpha = saturate(shape * _Alpha);
                clip(alpha - _AlphaClip);
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}
