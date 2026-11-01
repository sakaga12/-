// Hair variant of the cel shader.
//
// Hair is the one part of an anime character that must NOT use a normal-based highlight.
// Real hair is a bundle of strands, so its highlight runs as a band ACROSS the strand
// direction, not as a dot on the surface. That band is what sells anime hair, and it is
// what this shader adds on top of the base cel shading.
//
// Technique: Kajiya-Kay anisotropic specular, with two offset highlight layers and a
// jitter map so the band breaks up instead of looking like a plastic ring.

Shader "BlueLike/AnimeToonHair"
{
    Properties
    {
        [Header(Base)]
        _BaseMap            ("Base Map", 2D) = "white" {}
        _BaseColor          ("Base Color", Color) = (1,1,1,1)

        [Header(Cel Shading)]
        _ShadowColor        ("Shadow Tint", Color) = (0.52,0.54,0.74,1)
        _ShadowThreshold    ("Shadow Threshold", Range(0,1)) = 0.5
        _ShadowSoftness     ("Shadow Softness", Range(0.001,0.3)) = 0.04
        _ReceivedShadowStrength ("Cast Shadow Strength", Range(0,1)) = 0.5

        [Header(Anisotropic Highlight)]
        _HairSpecColor      ("Primary Highlight Color", Color) = (1,1,1,1)
        _HairSpecColor2     ("Secondary Highlight Color", Color) = (1,0.85,0.7,1)
        _HairJitterMap      ("Jitter / Shift Map", 2D) = "gray" {}
        _PrimaryShift       ("Primary Shift", Range(-1,1)) = 0.12
        _SecondaryShift     ("Secondary Shift", Range(-1,1)) = -0.18
        _PrimaryExponent    ("Primary Sharpness", Range(1,256)) = 90
        _SecondaryExponent  ("Secondary Sharpness", Range(1,256)) = 24
        _PrimaryIntensity   ("Primary Intensity", Range(0,4)) = 1.1
        _SecondaryIntensity ("Secondary Intensity", Range(0,4)) = 0.45
        _JitterStrength     ("Jitter Strength", Range(0,0.5)) = 0.12
        // Strand direction in tangent space. Rotate if your hair UVs run vertically.
        _StrandDirection    ("Strand Direction (UV)", Vector) = (0,1,0,0)

        [Header(Rim Light)]
        _RimColor           ("Rim Color", Color) = (1,1,1,1)
        _RimPower           ("Rim Falloff", Range(0.5,16)) = 6
        _RimIntensity       ("Rim Intensity", Range(0,3)) = 0.45

        [Header(Outline)]
        _OutlineColor       ("Outline Color", Color) = (0.14,0.10,0.14,1)
        _OutlineWidth       ("Outline Width (px)", Range(0,8)) = 1.0
        [Toggle] _OutlineTintByBase ("Tint Outline By Base Color", Float) = 1

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+10"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float4 _ShadowColor;
            float  _ShadowThreshold;
            float  _ShadowSoftness;
            float  _ReceivedShadowStrength;
            float4 _HairSpecColor;
            float4 _HairSpecColor2;
            float4 _HairJitterMap_ST;
            float  _PrimaryShift;
            float  _SecondaryShift;
            float  _PrimaryExponent;
            float  _SecondaryExponent;
            float  _PrimaryIntensity;
            float  _SecondaryIntensity;
            float  _JitterStrength;
            float4 _StrandDirection;
            float4 _RimColor;
            float  _RimPower;
            float  _RimIntensity;
            float4 _OutlineColor;
            float  _OutlineWidth;
            float  _OutlineTintByBase;
            float  _Cull;
        CBUFFER_END
        ENDHLSL

        // -----------------------------------------------------------------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma target 3.0

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output;
                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS);

                float2 offset = normalize(normalCS.xy + 1e-6);
                offset *= _OutlineWidth * 2.0 * positionCS.w / _ScreenParams.xy;
                positionCS.xy += offset;

                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                half4 c = _OutlineColor;
                if (_OutlineTintByBase > 0.5)
                {
                    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                    c.rgb = lerp(c.rgb, c.rgb * albedo.rgb * 1.6, 0.6);
                }
                return half4(c.rgb, 1.0);
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma vertex HairVertex
            #pragma fragment HairFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);       SAMPLER(sampler_BaseMap);
            TEXTURE2D(_HairJitterMap); SAMPLER(sampler_HairJitterMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 tangentWS  : TEXCOORD3;
                float3 viewDirWS  : TEXCOORD4;
            };

            Varyings HairVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.tangentWS = normals.tangentWS;
                output.viewDirWS = GetWorldSpaceViewDir(positions.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            // Kajiya-Kay: the highlight follows the strand tangent, shifted along the
            // normal so the band can be slid up and down the hair.
            float StrandSpecular(float3 T, float3 N, float3 H, float shift, float exponent)
            {
                float3 shiftedT = normalize(T + N * shift);
                float TdotH = dot(shiftedT, H);
                float sinTH = sqrt(max(0.0, 1.0 - TdotH * TdotH));
                float attenuation = smoothstep(-1.0, 0.0, TdotH);
                return attenuation * pow(sinTH, exponent);
            }

            half4 HairFragment(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 L = normalize(mainLight.direction);
                float3 H = normalize(L + V);

                // --- cel diffuse -----------------------------------------------------
                float halfLambert = dot(N, L) * 0.5 + 0.5;
                float lightTerm = smoothstep(_ShadowThreshold - _ShadowSoftness,
                                             _ShadowThreshold + _ShadowSoftness,
                                             halfLambert);
                float castShadow = lerp(1.0, mainLight.shadowAttenuation, _ReceivedShadowStrength);
                lightTerm = min(lightTerm, castShadow);

                float3 color = lerp(albedo.rgb * _ShadowColor.rgb, albedo.rgb, saturate(lightTerm));
                color *= mainLight.color;
                color += SampleSH(N) * albedo.rgb * 0.35;

                // --- strand tangent --------------------------------------------------
                float3 bitangentWS = normalize(cross(N, input.tangentWS));
                // Blend tangent and bitangent so the strand direction can be steered from
                // the material without re-authoring UVs.
                float3 T = normalize(input.tangentWS * _StrandDirection.x +
                                     bitangentWS * _StrandDirection.y + 1e-6);

                float jitter = SAMPLE_TEXTURE2D(_HairJitterMap, sampler_HairJitterMap,
                                                TRANSFORM_TEX(input.uv, _HairJitterMap)).r;
                float jitterOffset = (jitter - 0.5) * 2.0 * _JitterStrength;

                float primary = StrandSpecular(T, N, H, _PrimaryShift + jitterOffset, _PrimaryExponent);
                float secondary = StrandSpecular(T, N, H, _SecondaryShift + jitterOffset, _SecondaryExponent);

                // Hard-edge the primary band so it reads as drawn, keep the secondary soft.
                primary = smoothstep(0.25, 0.32, primary);

                float3 highlight = primary * _PrimaryIntensity * _HairSpecColor.rgb;
                highlight += secondary * _SecondaryIntensity * _HairSpecColor2.rgb * albedo.rgb;
                color += highlight * mainLight.color * saturate(lightTerm * 1.2);

                // --- rim --------------------------------------------------------------
                float fresnel = 1.0 - saturate(dot(N, V));
                float rim = pow(fresnel, _RimPower) * _RimIntensity;
                rim *= saturate(dot(N, L) * 0.5 + 0.5);
                color += rim * _RimColor.rgb;

                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings ShadowVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                positionWS = ApplyShadowBias(positionWS, normalWS, _LightDirection);

                float4 positionCS = TransformWorldToHClip(positionWS);
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowFragment(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
