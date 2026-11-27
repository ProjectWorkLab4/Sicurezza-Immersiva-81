Shader "SI81/MachineLock/DamageOverlay"
{
    Properties
    {
        _Color ("Tint Color", Color) = (0.85, 0.04, 0.04, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0
        _InnerRadius ("Inner Clear Radius", Range(0.05, 0.9)) = 0.28
        _OuterRadius ("Outer Edge Radius", Range(0.2, 1.2)) = 0.72
        _FullScreenFade ("Full Screen Fade Mode", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "DamageOverlay"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _InnerRadius;
                half _OuterRadius;
                half _FullScreenFade;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half2 centered = input.uv - half2(0.5h, 0.5h);
                half dist = length(centered) * 1.41421356h;
                half vignette = smoothstep(_InnerRadius, _OuterRadius, dist);
                half mask = lerp(vignette, 1.0h, saturate(_FullScreenFade));
                half alpha = saturate(mask * _Intensity * _Color.a);
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
