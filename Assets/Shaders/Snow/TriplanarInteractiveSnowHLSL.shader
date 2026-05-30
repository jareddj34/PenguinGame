Shader "Custom/Snow Interactive Normals" {
	Properties{
		[Header(Main)]
		_Noise("Snow Noise", 2D) = "gray" {}
		_NoiseScale("Noise Scale", Range(0,2)) = 0.1
		_NoiseWeight("Noise Weight", Range(0,2)) = 0.1
		[HDR]_ShadowColor("Shadow Color", Color) = (0.5,0.5,0.5,1)

		[Space]
		[Header(Tesselation)]
		_MaxTessDistance("Max Tessellation Distance", Range(10,100)) = 50
		_Tess("Tessellation", Range(1,500)) = 20

		[Space]
		[Header(Snow)]
		[HDR]_Color("Snow Color", Color) = (0.5,0.5,0.5,1)
		[HDR]_PathColorIn("Snow Path Color In", Color) = (0.5,0.5,0.7,1)
		[HDR]_PathColorOut("Snow Path Color Out", Color) = (0.5,0.5,0.7,1)
		_PathBlending("Snow Path Blending", Range(0,3)) = 0.3
		_PathBlendOffset("Snow Path Blending Offset", Range(-3,3)) = 0.3
		_PathCutoff("Snow Path Cutoff", Range(0,3)) = 0
		_PathSmooth("Snow Path Smooth", Range(0,3)) = 1
		_MainTex("Snow Texture", 2D) = "white" {}
		_SnowHeight("Snow Height", Range(0,2)) = 0.3
		_SnowDepth("Snow Path Depth", Range(-2,2)) = 0.3
		_SnowTextureOpacity("Snow Texture Opacity", Range(0,2)) = 0.3
		_SnowTextureScale("Snow Texture Scale", Range(0,2)) = 0.3

		[Space]
		[Header(Sparkles)]
		_SparkleScale("Sparkle Scale", Range(0,10)) = 10
		_SparkCutoff("Sparkle Cutoff", Range(0,10)) = 0.8
		_SparkleNoise("Sparkle Noise", 2D) = "gray" {}

		[Space]
		[Header(Rim)]
		_RimPower("Rim Power", Range(0,20)) = 20
		[HDR]_RimColor("Rim Color Snow", Color) = (0.5,0.5,0.5,1)

		[Header(Rendering Layer Decal)]
		_RenderingLayer("Rendering Layer", Range(0,10)) = 1
	}
	HLSLINCLUDE

	// Includes
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
	#include "SnowTessellation.hlsl"
	
	#pragma require tessellation tessHW
	#pragma instancing_options renderinglayer
	#pragma multi_compile_instancing
	#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
	#pragma vertex TessellationVertexProgram
	#pragma hull hull
	#pragma domain domain
	// Keywords
	
    #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
	#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
	#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
	#pragma multi_compile _ _SHADOWS_SOFT
	#pragma multi_compile _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
	#pragma multi_compile_fog
	#pragma target 5.0
	


	ControlPoint TessellationVertexProgram(Attributes2 v)
	{
		ControlPoint p;
		
		p.vertex = v.vertex;
		p.uv = v.uv;
		p.normal = v.normal;
		UNITY_SETUP_INSTANCE_ID(v);
		UNITY_TRANSFER_INSTANCE_ID(v, p);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(p);
		
		return p;
	}
	ENDHLSL

	SubShader{
		Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

		Pass{
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			// vertex happens in snowtessellation.hlsl
			#pragma fragment frag
			#pragma require tessellation tessHW
			#pragma target 2.0
			sampler2D _MainTex, _SparkleNoise;
			float4 _Color, _RimColor;
			float _RimPower;
			float4 _PathColorIn, _PathColorOut;
			float _PathBlending, _PathBlendOffset;
			float _SparkleScale, _SparkCutoff;
			float _SnowTextureOpacity, _SnowTextureScale;
			float4 _ShadowColor;
			
			half4 frag(Varyings2 IN) : SV_Target{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
			
				// Effects RenderTexture Reading
				float3 worldPosition = mul(unity_ObjectToWorld, IN.vertex).xyz;
				float2 uv = IN.worldPos.xz - _Position.xz;
				uv /= (_OrthographicCamSize * 2);
				uv += 0.5;

				// effects texture				
				float4 effect = tex2D(_GlobalEffectRT, uv);

				// mask to prevent bleeding
				effect *=  smoothstep(0.99, 0.9, uv.x) * smoothstep(0.99, 0.9,1- uv.x);
				effect *=  smoothstep(0.99, 0.9, uv.y) * smoothstep(0.99, 0.9,1- uv.y);

				// worldspace Noise texture
				float3 topdownNoise = tex2D(_Noise, IN.worldPos.xz * _NoiseScale).rgb;

				// worldspace Snow texture
				float3 snowtexture = tex2D(_MainTex, IN.worldPos.xz * _SnowTextureScale).rgb;
				
				//lerp between snow color and snow texture
				float3 snowTex = lerp(_Color.rgb,snowtexture * _Color.rgb, _SnowTextureOpacity);
				
				//lerp the colors using the RT effect path 
				float pathShape = smoothstep(_PathCutoff,_PathCutoff + _PathSmooth,effect.g);
				float blendPath = (effect.g * _PathBlending) + _PathBlendOffset;
				float3 path = lerp(_PathColorOut.rgb, _PathColorIn.rgb,  saturate(blendPath));
				float3 mainColors = lerp(snowTex,path, pathShape);

				// surface and inputdate are needed here to apply decals
				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = mainColors;
				surfaceData.alpha = 1;
				surfaceData.normalTS = IN.normal; 
				surfaceData.smoothness = 0.5;
				surfaceData.occlusion = 1;

				InputData inputData = (InputData)0;
				inputData.positionWS = IN.worldPos;
				inputData.positionCS = IN.vertex;
				
				inputData.normalWS = normalize(IN.normal);
				inputData.viewDirectionWS = normalize(IN.viewDir);
				inputData.shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
				inputData.fogCoord = IN.fogFactor;
				
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.vertex);

#if defined(_DBUFFER_MRT1) || defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3) || defined(_DBUFFER)
    			ApplyDecalToSurfaceData(inputData.positionCS, surfaceData, inputData);
	
#endif

				// main colors with decals applied
				mainColors = surfaceData.albedo;
				

				// lighting and shadow information
				float shadow = 0;
				half4 shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
				
				#if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
					Light mainLight = GetMainLight(shadowCoord);
					shadow = mainLight.shadowAttenuation;
				#else
					Light mainLight = GetMainLight();
				#endif

					float3 extraLights;
				// extra lights forward+
uint lightsCount = GetAdditionalLightsCount();
        		LIGHT_LOOP_BEGIN(lightsCount)
            
            		Light aLight = GetAdditionalLight(lightIndex, IN.worldPos, half4(1,1,1,1));
					float3 attenuatedLightColor = aLight.color * (aLight.distanceAttenuation * aLight.shadowAttenuation);
					extraLights += attenuatedLightColor;
					
        		LIGHT_LOOP_END

				// extra point lights support forward
			
				int pixelLightCount = GetAdditionalLightsCount();
				for (int j = 0; j < pixelLightCount; ++j) {
					Light light = GetAdditionalLight(j, IN.worldPos, half4(1, 1, 1, 1));
					float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
					extraLights += attenuatedLightColor;			
				}

				float4 litMainColors = float4(mainColors,1) ;
				extraLights *= litMainColors.rgb;
				// add in the sparkles
				float sparklesStatic = tex2D(_SparkleNoise, IN.worldPos.xz * _SparkleScale).r;
				float cutoffSparkles = step(_SparkCutoff,sparklesStatic);				
				litMainColors += cutoffSparkles  *saturate(1- (effect.g * 2)) * 4;
	
				// Rim using recalculated normal instead of vertex normal
				half rim = 1.0 - saturate(dot(inputData.viewDirectionWS, inputData.normalWS));
				litMainColors += _RimColor * pow(abs(rim), _RimPower);

				// ambient and mainlight colors added
				half4 extraColors;
				extraColors.rgb = litMainColors.rgb * mainLight.color.rgb * (shadow + unity_AmbientSky.rgb);
				extraColors.a = 1;
				
				// colored shadows
				float3 coloredShadows = (shadow + (_ShadowColor.rgb * (1-shadow)));
				litMainColors.rgb = litMainColors.rgb * mainLight.color * (coloredShadows);
				// everything together
				float4 final = litMainColors+ extraColors + float4(extraLights,0);
				
				// add in fog
				
				final.rgb = MixFog(final.rgb, IN.fogFactor);
				return final;

			}
			ENDHLSL

		}

Pass
{
	// pass needed for decals
    Name "DepthNormals"
    Tags { "LightMode" = "DepthNormals" }

    ZWrite On
    Cull Back

    HLSLPROGRAM
    #pragma target 5.0
	#pragma require tessellation tessHW
    #pragma fragment DepthNormalsFragment
	 // Universal Pipeline keywords
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
	
    struct DNAttributes
    {
        float4 positionOS : POSITION;
        float3 normalOS   : NORMAL;
    };

    struct DNVaryings
    {
        float4 positionCS : SV_POSITION;
        float3 normalWS   : TEXCOORD0;
    };

    DNVaryings DepthNormalsVertex(DNAttributes input)
    {
        DNVaryings output;
        float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
        output.positionCS = TransformWorldToHClip(positionWS);
        output.normalWS = TransformObjectToWorldNormal(input.normalOS);
        return output;
    }

	float _RenderingLayer;

    void DepthNormalsFragment(
            DNVaryings input
            , out half4 outNormalWS : SV_Target0,
            out float outDepth : SV_Depth
        #ifdef _WRITE_RENDERING_LAYERS
            , out float4 outRenderingLayers : SV_Target1
        #endif
        )
{
		 #ifdef _WRITE_RENDERING_LAYERS
                uint renderingLayers = GetMeshRenderingLayer();
                outRenderingLayers = _RenderingLayer;//float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
            #endif
			outNormalWS = half4(NormalizeNormalPerPixel(input.normalWS) * 0.5 + 0.5, 0);
			outDepth = input.positionCS.z;
       // return half4(NormalizeNormalPerPixel(input.normalWS) * 0.5 + 0.5, 0);
    }

    ENDHLSL
}
		// depth only pass to fix invisiblity when turning on Depth Priming mode
		   Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
			#pragma require tessellation tessHW
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages

            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
		// casting shadows is a little glitchy, I've turned it off, but maybe in future urp versions it works better?
		// Shadow Casting Pass
		Pass
		{
				Name "ShadowCaster"
				Tags { "LightMode" = "ShadowCaster" }
				ZWrite On
				ZTest LEqual
				Cull Off
			
				HLSLPROGRAM
				#pragma target 3.0
				#pragma require tessellation tessHW
				// support all the various lightypes and shadow paths
				#pragma multi_compile_shadowcaster
			
				
				#pragma fragment frag
				

				half4 frag(Varyings2 IN) : SV_Target{
						return 0;
				}
			
				ENDHLSL
		}
	}
}