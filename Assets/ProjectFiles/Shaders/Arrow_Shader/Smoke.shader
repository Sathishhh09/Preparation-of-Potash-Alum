Shader "Custom/ProceduralSmoke"
{
    Properties
    {
        _Color ("Smoke Color", Color) = (0.8, 0.8, 0.8, 1)

        _Opacity ("Opacity", Range(0, 1)) = 0.5

        _NoiseScale ("Noise Scale", Range(0.5, 10)) = 3

        _NoiseSpeed ("Rise Speed", Range(0, 2)) = 0.3

        _Distortion ("Distortion", Range(0, 1)) = 0.25

        _Softness ("Softness", Range(0.01, 1)) = 0.3

        _EdgeFade ("Edge Fade", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha

        ZWrite Off

        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)

            float4 _Color;

            float _Opacity;

            float _NoiseScale;

            float _NoiseSpeed;

            float _Distortion;

            float _Softness;

            float _EdgeFade;

            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));

                p += dot(p, p + 45.32);

                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);

                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);

                float b = hash21(i + float2(1, 0));

                float c = hash21(i + float2(0, 1));

                float d = hash21(i + float2(1, 1));

                return lerp(
                    lerp(a, b, f.x),
                    lerp(c, d, f.x),
                    f.y
                );
            }

            float fbm(float2 p)
            {
                float value = 0.0;

                float amplitude = 0.5;

                for (int i = 0; i < 5; i++)
                {
                    value += noise(p) * amplitude;

                    p *= 2.0;

                    amplitude *= 0.5;
                }

                return value;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv = IN.uv;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                float time = _Time.y * _NoiseSpeed;

                float2 smokeUV = uv;

                smokeUV.y -= time;

                float n1 = fbm(smokeUV * _NoiseScale);

                float2 distortionUV = uv * (_NoiseScale * 0.7);

                distortionUV.y -= time * 0.5;


                float distortion = fbm(distortionUV);


                smokeUV.x += (distortion - 0.5) * _Distortion;

                float smoke = fbm(smokeUV * _NoiseScale);

                smoke = smoothstep(0.35,0.7,smoke);

                float topFade = 1.0 - smoothstep(0.45,1.0,uv.y);

                float bottomFade = smoothstep(0.0,0.15,uv.y);


                float sideDistance = abs(uv.x - 0.5) * 2.0;

                float sideFade = 1.0 - smoothstep(0.5,1.0,sideDistance);

                float alpha = smoke * topFade * bottomFade * sideFade * _Opacity;

                float3 finalColor = _Color.rgb;


                return half4(finalColor,alpha);
            }

            ENDHLSL
        }
    }
}