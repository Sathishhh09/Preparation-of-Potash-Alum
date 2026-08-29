Shader "Custom/ArrowFlow"
{
    Properties
    {
        [MainTexture] _MainTex ("Arrow Texture", 2D) = "white" {}

        [HDR]
        _ArrowColor ("Arrow Color", Color) = (0, 1, 1, 1)

        _Speed ("Animation Speed", Float) = 0.5

        _TilingX ("Arrows Per Row", Float) = 5

        _Rows ("Number Of Rows", Range(1, 20)) = 3

        _Glow ("Glow Intensity", Range(0, 10)) = 2

        _Alpha ("Transparency", Range(0, 1)) = 1

        [Enum(Right,0, Left,1, Up,2, Down,3)]
        _Direction ("Direction", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ArrowFlow"

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                float4 _ArrowColor;

                float _Speed;

                float _TilingX;

                float _Rows;

                float _Glow;

                float _Alpha;

                float _Direction;

            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                OUT.uv =
                    TRANSFORM_TEX(IN.uv, _MainTex);

                return OUT;
            }

            float2 GetDirection()
            {
                if (_Direction == 0)
                {
                    return float2(1, 0);
                }

                if (_Direction == 1)
                {
                    return float2(-1, 0);
                }

                if (_Direction == 2)
                {
                    return float2(0, 1);
                }

                return float2(0, -1);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                uv.x *= _TilingX;
                uv.y *= _Rows;

                float2 direction = GetDirection();

                uv += direction * _Time.y * _Speed;

                uv = frac(uv);

                float4 arrow = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    uv
                );

                float mask = arrow.a;

                float3 color = _ArrowColor.rgb;

                color *= _Glow;

                float alpha =
                    mask *
                    _Alpha *
                    _ArrowColor.a;

                return float4(color, alpha);
            }

            ENDHLSL
        }
    }
}