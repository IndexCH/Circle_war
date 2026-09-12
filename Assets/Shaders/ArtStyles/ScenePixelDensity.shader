Shader "CircleWar/ScenePixelDensity"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        [HideInInspector] _UseMeshGeometry("Mesh geometry", Float) = 0
        _PixelsPerUnit("Scene pixels per world unit", Float) = 54
        _ReferencePixelSize("Pixel size at 1080p (0 = use world density)", Float) = 0
        [NoScaleOffset] _OriginalColorLut("Original sprite palette", 3D) = "" {}
        _OriginalColorEnabled("Restore original palette", Float) = 0
        _SpringOutlineEnabled("Spring prop outline", Float) = 0
        _SpringOutlineColor("Spring outline color", Color) = (0.16,0.14,0.11,1)
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma target 3.0
            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                float2 pixelPositionWS : TEXCOORD4;
                COMMON_2D_LIT_OUTPUTS
                half4 color        : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _UseMeshGeometry;
                float _PixelsPerUnit;
                float _ReferencePixelSize;
                float _OriginalColorEnabled;
                float _SpringOutlineEnabled;
                half4 _SpringOutlineColor;
            CBUFFER_END
            #include "ScenePixelGrid.hlsl"
            #include "OriginalSpriteColor.hlsl"
            #include "ScenePixelOutline.hlsl"

            Varyings LitVertex(Attributes input)
            {
                if (_UseMeshGeometry < 0.5)
                {
                    UNITY_SKINNED_VERTEX_COMPUTE(input);
                    SetUpSpriteInstanceProperties();
                    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                }

                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color;
                if (_UseMeshGeometry < 0.5) o.color *= unity_SpriteColor;

                o.pixelPositionWS = TransformObjectToWorld(input.positionOS).xy;
                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                half4 spriteColor = SampleSceneSprite(input.uv, input.pixelPositionWS);
                input.uv = ScenePixelUV(input.uv, input.pixelPositionWS, _ReferencePixelSize > 0 ? 108.0 / _ReferencePixelSize : _PixelsPerUnit);
                input.lightingUV = ScenePixelUV(input.lightingUV, input.pixelPositionWS, _ReferencePixelSize > 0 ? 108.0 / _ReferencePixelSize : _PixelsPerUnit);
                half4 main = input.color * spriteColor;
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);
                #if defined(DEBUG_DISPLAY)
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                surfaceData.normalWS = input.normalWS;
                #endif
                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma target 3.0
            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                float2 pixelPositionWS : TEXCOORD4;
                COMMON_2D_NORMALS_OUTPUTS
                half4   color           : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
                float _UseMeshGeometry;
                float _PixelsPerUnit;
                float _ReferencePixelSize;
                float _OriginalColorEnabled;
                float _SpringOutlineEnabled;
                half4 _SpringOutlineColor;
            CBUFFER_END
            #include "ScenePixelGrid.hlsl"
            #include "OriginalSpriteColor.hlsl"
            #include "ScenePixelOutline.hlsl"

            Varyings NormalsRenderingVertex(Attributes input)
            {
                if (_UseMeshGeometry < 0.5)
                {
                    UNITY_SKINNED_VERTEX_COMPUTE(input);
                    SetUpSpriteInstanceProperties();
                    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                }

                Varyings o = CommonNormalsVertex(input);
                o.color = input.color * _Color;
                if (_UseMeshGeometry < 0.5) o.color *= unity_SpriteColor;

                o.pixelPositionWS = TransformObjectToWorld(input.positionOS).xy;
                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                input.uv = ScenePixelUV(input.uv, input.pixelPositionWS, _ReferencePixelSize > 0 ? 108.0 / _ReferencePixelSize : _PixelsPerUnit);
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma target 3.0
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                float2 pixelPositionWS : TEXCOORD4;
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
          
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _UseMeshGeometry;
                float _PixelsPerUnit;
                float _ReferencePixelSize;
                float _OriginalColorEnabled;
                float _SpringOutlineEnabled;
                half4 _SpringOutlineColor;
            CBUFFER_END
            #include "ScenePixelGrid.hlsl"
            #include "OriginalSpriteColor.hlsl"
            #include "ScenePixelOutline.hlsl"

            Varyings UnlitVertex(Attributes input)
            {
                if (_UseMeshGeometry < 0.5)
                {
                    UNITY_SKINNED_VERTEX_COMPUTE(input);
                    SetUpSpriteInstanceProperties();
                    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                }

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color;
                if (_UseMeshGeometry < 0.5) o.color *= unity_SpriteColor;
                o.pixelPositionWS = TransformObjectToWorld(input.positionOS).xy;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return input.color * SampleSceneSprite(input.uv, input.pixelPositionWS);
            }
            ENDHLSL
        }
    }
}
