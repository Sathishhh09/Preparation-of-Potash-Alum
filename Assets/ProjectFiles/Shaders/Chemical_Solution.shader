Shader "Custom/Chemical_Solution_Turbid_Unity6"
{
    Properties
    {
        [Header(Color and Turbidity)]
        _LiquidBaseColor ("Liquid Tint", Color) = (0.88, 0.92, 0.94, 1.0)
        _TurbidWhiteColor ("Milky / Crust White", Color) = (0.95, 0.97, 0.98, 1.0)
        _SurfaceRimColor ("Surface Rim Color", Color) = (1, 1, 1, 0.9)
        _Transparency ("Overall Opacity", Range(0.1, 1.0)) = 0.88

        [Header(Continuous Fluid Drift Motion)]
        _DriftSpeed ("Drift Flow Speed", Range(0.01, 2.0)) = 0.35
        _SwirlStrength ("Internal Fluid Swirl", Range(0.0, 1.0)) = 0.4
        _PatternScale ("Pattern Scale", Range(1.0, 50.0)) = 18.0
        _PatternContrast ("Crust/Cloud Sharpness", Range(0.5, 6.0)) = 2.2
        _TurbidityAmount ("Milky Cloud Intensity", Range(0.0, 1.0)) = 0.7

        [Header(Container Gravity Level)]
        _FillHeight ("Liquid Level (Height above pivot)", Float) = 0.05

        [Header(Optical Effects)]
        _FresnelPower ("Surface Rim Power", Range(0.5, 8.0)) = 2.5
        _DepthDarkening ("Depth Thickness", Range(0.0, 5.0)) = 1.2
        _WaveStrength ("Surface Wave Ripple", Range(0.0, 0.01)) = 0.001
        _WaveSpeed ("Wave Ripple Speed", Range(0.0, 5.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+10"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct v2f
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _LiquidBaseColor;
                half4 _TurbidWhiteColor;
                half4 _SurfaceRimColor;
                float _FillHeight;
                float _DriftSpeed;
                float _SwirlStrength;
                float _PatternScale;
                float _PatternContrast;
                float _TurbidityAmount;
                float _FresnelPower;
                float _DepthDarkening;
                float _WaveStrength;
                float _WaveSpeed;
                half _Transparency;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float smoothNoise(float2 uv)
            {
                float2 lv = frac(uv);
                float2 id = floor(uv);
                lv = lv * lv * (3.0 - 2.0 * lv);

                float bl = hash21(id);
                float br = hash21(id + float2(1, 0));
                float tl = hash21(id + float2(0, 1));
                float tr = hash21(id + float2(1, 1));

                return lerp(lerp(bl, br, lv.x), lerp(tl, tr, lv.x), lv.y);
            }

            float fbmTurbidity(float2 uv)
            {
                float total = 0.0;
                float amp = 0.5;
                float freq = 1.0;

                for (int i = 0; i < 4; i++)
                {
                    total += smoothNoise(uv * freq) * amp;
                    freq *= 2.15;
                    amp *= 0.45;
                }
                return total;
            }

            v2f vert (appdata input)
            {
                v2f output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;

                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);

                return output;
            }

            half4 frag (v2f input, FRONT_FACE_TYPE facing : SV_IsFrontFace) : SV_Target
            {
                float3 gravityUp = float3(0, 1, 0);

                // Continuous multi-directional drift vectors
                float time = _Time.y * _DriftSpeed;
                float2 baseUV = input.positionWS.xz * _PatternScale;

                // Domain distortion / fluid swirling
                float2 swirlDistort = float2(
                    sin(baseUV.y * 0.4 + time * 0.8),
                    cos(baseUV.x * 0.4 + time * 0.6)
                ) * _SwirlStrength;

                // Two overlapping drifting layers moving at different angles
                float2 layer1 = baseUV + swirlDistort + float2(time * 0.15, time * 0.08);
                float2 layer2 = baseUV * 1.3 - swirlDistort + float2(-time * 0.10, time * 0.12);

                float noise1 = fbmTurbidity(layer1);
                float noise2 = fbmTurbidity(layer2);
                float blendedNoise = lerp(noise1, noise2, 0.5);

                // Pivot relative world-gravity height alignment
                float ripple = sin((input.positionWS.x + input.positionWS.z) * 4.0 + _Time.y * _WaveSpeed) * _WaveStrength;
                float3 containerWorldPivot = UNITY_MATRIX_M._m03_m13_m23;
                float heightRelPivot = dot(input.positionWS - containerWorldPivot, gravityUp);

                float surfaceLevel = _FillHeight + ripple;
                float distToSurface = surfaceLevel - heightRelPivot;

                // Discard pixels above water level
                clip(distToSurface);

                // Dynamic moving salt crust cloudiness
                float crustIntensity = pow(saturate(blendedNoise), _PatternContrast) * _TurbidityAmount;

                // Base liquid depth tint
                float depthFactor = saturate(distToSurface * _DepthDarkening);
                half3 baseLiquid = lerp(_LiquidBaseColor.rgb * 1.05, _LiquidBaseColor.rgb * 0.85, depthFactor);
                half3 mixedSolution = lerp(baseLiquid, _TurbidWhiteColor.rgb, crustIntensity);

                // Surface & Fresnel lighting
                float3 N = normalize(input.normalWS);
                N = facing ? N : -N;
                float3 V = normalize(input.viewDirWS);
                float NdotV = saturate(dot(N, V));
                half fresnel = pow(1.0 - NdotV, _FresnelPower);

                // Top boundary meniscus line
                float surfaceEdge = smoothstep(0.012, 0.00, distToSurface);
                half3 finalColor = lerp(mixedSolution + (fresnel * 0.25), _SurfaceRimColor.rgb, surfaceEdge);

                // Dynamic opacity with moving dense precipitate patches
                half alpha = max(_Transparency + (crustIntensity * 0.2), fresnel * 0.4);
                alpha = lerp(alpha, _SurfaceRimColor.a, surfaceEdge);

                return half4(finalColor, saturate(alpha));
            }
            ENDHLSL
        }
    }
}