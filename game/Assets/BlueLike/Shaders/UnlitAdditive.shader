// Minimal additive unlit shader, used for bullet tracers and effect lines.
// Written by hand because the built-in legacy unlit shaders render magenta under URP,
// and URP's own particle shaders need keyword setup this does not warrant.
Shader "BlueLike/UnlitAdditive"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One   // additive, premultiplied by vertex alpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                // LineRenderer writes its gradient into vertex colour, so the fade lives there.
                return half4(_BaseColor.rgb * input.color.rgb, _BaseColor.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
