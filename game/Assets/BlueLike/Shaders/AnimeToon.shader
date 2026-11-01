// Anime-style cel shader for URP, built for mobile.
//
// What it does and why each piece matters:
//   * Ramp / stepped diffuse  - hard shadow terminator instead of smooth falloff. This is
//                               what makes a 3D model read as drawn rather than rendered.
//   * SDF face shadow         - a grayscale map drives the face shadow instead of the mesh
//                               normals, so the nose and eye shadows sweep correctly as the
//                               light rotates. Turn it on only for the face material.
//   * Back-face outline       - cheapest outline that works on mobile. Width is in pixels,
//                               so it stays constant as the camera pulls in and out.
//   * Rim light               - separates the character from the background without a
//                               post-process pass.
//
// Single directional light only. That is a deliberate restriction: anime shading looks
// wrong under many lights, and one light is all a phone should pay for.

Shader "BlueLike/AnimeToon"
{
    Properties
    {
        [Header(Base)]
        _BaseMap            ("Base Map", 2D) = "white" {}
        _BaseColor          ("Base Color", Color) = (1,1,1,1)

        [Header(Cel Shading)]
        _ShadowColor        ("Shadow Tint", Color) = (0.55,0.58,0.76,1)
        _ShadowRamp         ("Shadow Ramp (optional)", 2D) = "white" {}
        [Toggle] _UseRamp   ("Use Ramp Texture", Float) = 0
        _ShadowThreshold    ("Shadow Threshold", Range(0,1)) = 0.5
        _ShadowSoftness     ("Shadow Softness", Range(0.001,0.3)) = 0.03
        _ShadowStepCount    ("Shadow Steps", Range(1,4)) = 1
        _ReceivedShadowStrength ("Cast Shadow Strength", Range(0,1)) = 0.7

        [Header(Face SDF)]
        [Toggle] _IsFace    ("Is Face Material", Float) = 0
        _FaceSdfMap         ("Face SDF Map", 2D) = "white" {}
        _FaceSdfBias        ("Face SDF Bias", Range(-0.5,0.5)) = 0
        // Written every frame by FaceDirectionBinder.cs. Do not set these by hand.
        _FaceForward        ("Face Forward (auto)", Vector) = (0,0,1,0)
        _FaceRight          ("Face Right (auto)", Vector) = (1,0,0,0)

        [Header(Specular)]
        _SpecColor          ("Specular Color", Color) = (1,1,1,1)
        _SpecPower          ("Specular Sharpness", Range(1,200)) = 40
        _SpecThreshold      ("Specular Threshold", Range(0,1)) = 0.5
        _SpecIntensity      ("Specular Intensity", Range(0,3)) = 0.35

        [Header(Rim Light)]
        _RimColor           ("Rim Color", Color) = (1,1,1,1)
        _RimPower           ("Rim Falloff", Range(0.5,16)) = 5
        _RimIntensity       ("Rim Intensity", Range(0,3)) = 0.5

        [Header(Emission)]
        _EmissionColor      ("Emission", Color) = (0,0,0,0)

        [Header(Outline)]
        _OutlineColor       ("Outline Color", Color) = (0.12,0.10,0.16,1)
        _OutlineWidth       ("Outline Width (px)", Range(0,8)) = 1.2
        [Toggle] _OutlineTintByBase ("Tint Outline By Base Color", Float) = 1
        [Toggle] _UseVertexColorWidth ("Vertex Alpha Scales Width", Float) = 0

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // -----------------------------------------------------------------------------
        // Shared declarations
        // -----------------------------------------------------------------------------
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float4 _ShadowColor;
            float4 _ShadowRamp_ST;
            float  _UseRamp;
            float  _ShadowThreshold;
            float  _ShadowSoftness;
            float  _ShadowStepCount;
            float  _ReceivedShadowStrength;
            float  _IsFace;
            float4 _FaceSdfMap_ST;
            float  _FaceSdfBias;
            float4 _FaceForward;
            float4 _FaceRight;
            float4 _SpecColor;
            float  _SpecPower;
            float  _SpecThreshold;
            float  _SpecIntensity;
            float4 _RimColor;
            float  _RimPower;
            float  _RimIntensity;
            float4 _EmissionColor;
            float4 _OutlineColor;
            float  _OutlineWidth;
            float  _OutlineTintByBase;
            float  _UseVertexColorWidth;
            float  _Cull;
        CBUFFER_END
        ENDHLSL

        // -----------------------------------------------------------------------------
        // Pass 1: outline. Inverted hull - render back faces pushed outward along the
        // normal in clip space, which gives a pixel-constant width at any distance.
        // -----------------------------------------------------------------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile_instancing
            #pragma target 3.0

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);

                // Normal straight into clip space, so the push is screen aligned.
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS);

                float width = _OutlineWidth;
                width *= lerp(1.0, input.color.a, _UseVertexColorWidth);

                float2 offset = normalize(normalCS.xy + 1e-6);
                // positionCS.w keeps the offset perspective-correct; _ScreenParams turns
                // the width into actual pixels.
                offset *= width * 2.0 * positionCS.w / _ScreenParams.xy;

                positionCS.xy += offset;

                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                half4 outlineColor = _OutlineColor;

                // Tinting the outline toward the surface colour reads far better than a
                // flat black line, especially on skin and light hair.
                if (_OutlineTintByBase > 0.5)
                {
                    half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                    outlineColor.rgb = lerp(outlineColor.rgb, outlineColor.rgb * albedo.rgb * 1.6, 0.6);
                }
                return half4(outlineColor.rgb, 1.0);
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------------------
        // Pass 2: the actual cel shading.
        // -----------------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma vertex ToonVertex
            #pragma fragment ToonFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_ShadowRamp);     SAMPLER(sampler_ShadowRamp);
            TEXTURE2D(_FaceSdfMap);     SAMPLER(sampler_FaceSdfMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 viewDirWS  : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ToonVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(positions.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            // Quantises half-Lambert into N hard bands.
            float SteppedDiffuse(float halfLambert)
            {
                float steps = max(1.0, floor(_ShadowStepCount));
                if (steps <= 1.0)
                {
                    return smoothstep(_ShadowThreshold - _ShadowSoftness,
                                      _ShadowThreshold + _ShadowSoftness,
                                      halfLambert);
                }
                float scaled = saturate((halfLambert - _ShadowThreshold * 0.5) / max(0.001, 1.0 - _ShadowThreshold * 0.5));
                float quantised = floor(scaled * steps) / steps;
                // Soften only the band edge so it still reads as a hard terminator.
                float frac0 = frac(scaled * steps);
                float edge = smoothstep(0.0, _ShadowSoftness * steps, frac0);
                return saturate(quantised + edge / steps);
            }

            // The face path. Instead of asking the mesh where the shadow goes, we ask a
            // hand-authored map, indexed by where the light is relative to the head.
            float FaceLightTerm(float2 uv, float3 lightDirWS)
            {
                float3 flatLight = normalize(float3(lightDirWS.x, 0.0, lightDirWS.z) + 1e-6);
                float3 faceForward = normalize(float3(_FaceForward.x, 0.0, _FaceForward.z) + 1e-6);
                float3 faceRight = normalize(float3(_FaceRight.x, 0.0, _FaceRight.z) + 1e-6);

                float frontDot = dot(faceForward, flatLight);
                float sideDot = dot(faceRight, flatLight);

                // The map is authored for light coming from one side; mirror the lookup for
                // the other side so a single texture covers both.
                float2 sdfUv = TRANSFORM_TEX(uv, _FaceSdfMap);
                if (sideDot < 0.0) sdfUv.x = 1.0 - sdfUv.x;

                float sdf = SAMPLE_TEXTURE2D(_FaceSdfMap, sampler_FaceSdfMap, sdfUv).r;

                // frontDot  1 -> light in front -> threshold 0 -> fully lit
                // frontDot -1 -> light behind   -> threshold 1 -> fully shadowed
                float threshold = saturate((1.0 - frontDot) * 0.5 + _FaceSdfBias);
                return smoothstep(threshold - _ShadowSoftness, threshold + _ShadowSoftness, sdf);
            }

            half4 ToonFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 L = normalize(mainLight.direction);

                // --- diffuse term ---------------------------------------------------
                float lightTerm;
                if (_IsFace > 0.5)
                {
                    lightTerm = FaceLightTerm(input.uv, L);
                }
                else
                {
                    float halfLambert = dot(N, L) * 0.5 + 0.5;
                    if (_UseRamp > 0.5)
                    {
                        lightTerm = SAMPLE_TEXTURE2D(_ShadowRamp, sampler_ShadowRamp,
                                                     float2(halfLambert, 0.5)).r;
                    }
                    else
                    {
                        lightTerm = SteppedDiffuse(halfLambert);
                    }
                }

                // Cast shadows are folded in at reduced strength; at full strength they
                // punch ugly hard holes in a cel-shaded character.
                float castShadow = lerp(1.0, mainLight.shadowAttenuation, _ReceivedShadowStrength);
                lightTerm = min(lightTerm, castShadow);

                float3 shadowed = albedo.rgb * _ShadowColor.rgb;
                float3 color = lerp(shadowed, albedo.rgb, saturate(lightTerm));
                color *= mainLight.color;

                // --- ambient --------------------------------------------------------
                float3 ambient = SampleSH(N) * albedo.rgb * 0.35;
                color += ambient;

                // --- toon specular ---------------------------------------------------
                float3 H = normalize(L + V);
                float NdotH = saturate(dot(N, H));
                float spec = pow(NdotH, _SpecPower);
                spec = smoothstep(_SpecThreshold, _SpecThreshold + 0.03, spec);
                color += spec * _SpecIntensity * _SpecColor.rgb * mainLight.color * saturate(lightTerm);

                // --- rim light -------------------------------------------------------
                float fresnel = 1.0 - saturate(dot(N, V));
                float rim = pow(fresnel, _RimPower) * _RimIntensity;
                // Rim only on the lit side, otherwise the character glows in the dark.
                rim *= saturate(dot(N, L) * 0.5 + 0.5);
                color += rim * _RimColor.rgb;

                // --- emission --------------------------------------------------------
                color += _EmissionColor.rgb;

                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------------------
        // Pass 3: shadow caster. Hand written rather than pulled from URP's include so it
        // does not depend on URP's Lit property layout.
        // -----------------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

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

            half4 ShadowFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------------------
        // Pass 4: depth only, so depth-dependent effects and SSAO still work.
        // -----------------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing
            #pragma target 3.0

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
