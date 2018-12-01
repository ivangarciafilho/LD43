// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Lux/Anisotropic Lighting/Base" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		[Header(Basic Inputs)]
		[Space(3)]
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		[NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}

		// Shader does not handle Metalic = 0.0 correctly. So we simply clamp the property.
		[Gamma] _Metallic("Metallic", Range(0.00, 1.0)) = 0.0
		// Smoothness must not go up to 1.0! So we simply clamp the property.
		_Glossiness ("Smoothness", Range(0,0.975)) = 0.5

		[Toggle(_METALLICGLOSSMAP)] _EnableMetallGlossMap("Enable Metallic Gloss Map", Float) = 0.0
		_MetallicGlossMap("Metallic (R) Occlusion (G) Smoothness (A)", 2D) = "white" {}
		
		[Lux_FloatToggleDrawer] _Translucency("Enable Translucent Lighting", Float) = 0.0

		[Header(Tangent Direction)]
		[Space(3)]
		[NoScaleOffset] _TangentDir ("Tangent (RG)", 2D) = "bump" {}
		_BaseTangentDir ("Base Tangent Direction (UV)", Vector) = (0.0,1.0,0.0,0.0)
		_TangentDirStrength ("Strength", Range(0,1)) = 1

		[Header(Deferred Dithering)]
		[Space(3)]
		[Toggle(LUX_TRANSLUCENTLIGHTING)] _UseDither("Enable Dithering", Float) = 0.0
		[Lux_FloatToggleDrawer] _AnimateDither("     Animate Dithering", Float) = 0.0
		[Space(3)]
		_DitherSpread ("Spread", Range(0,0.1)) = 0.01
		_DitherDistance ("Start Distance", Range(0,30)) = 20
		_DitherRange ("Fade Range", Range(0,30)) = 5

		        
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf LuxAnisoMetallic fullforwardshadows vertex:vert 
		#pragma multi_compile __ LUX_AREALIGHTS
		#pragma shader_feature _METALLICGLOSSMAP

		// Shall the shader use dithering?
		#pragma shader_feature _ LUX_TRANSLUCENTLIGHTING

		// Include Lux Config
		#include "../Lux Core/Lux Config.cginc"
		// Include the dithering functions
		#include "../Lux Core/Lux Utils/LuxUtils.cginc"
		// Just to get access to _Main_Tex_ST
		#include "../Lux Core/Lux Setup/LuxStructs.cginc"
		// Finally include the lighting function
		#include "../Lux Core/Lux Lighting/LuxAnisoMetallicPBSLighting.cginc"
		#pragma target 3.0

		struct Input {
			float4 lux_uv_MainTex;			// We need float4 here to store screenPos
			float3 worldNormal;
			INTERNAL_DATA

			fixed4 color : COLOR0;
			//#if defined (UNITY_PASS_DEFERRED) // does not get handled correctly by the compiler
				float4 worldTangent;
				float4 worldBinormal_screenPos;
			//#endif
		};

		fixed4 _Color;
		sampler2D _MainTex;
		sampler2D _BumpMap;
		#if defined (_METALLICGLOSSMAP)
			sampler2D _MetallicGlossMap;
		#else
			half _Glossiness;
			half _Metallic;
		#endif

		#if defined	(LUX_TRANSLUCENTLIGHTING)
			half _AnimateDither;
			float _DitherSpread;
			half _DitherDistance;
			half _DitherRange;
		#endif

		void vert (inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input,o);

			// Unity's dynamic batching might break normals and tangents
			// v.normal = normalize(v.normal);
			// v.tangent.xyz = normalize(v.tangent.xyz);

			// Lux
			o.lux_uv_MainTex.xy = TRANSFORM_TEX(v.texcoord, _MainTex);

			#if defined	(LUX_TRANSLUCENTLIGHTING)
				float4 screenPos = ComputeScreenPos( UnityObjectToClipPos(v.vertex));
				o.lux_uv_MainTex.zw = screenPos.xy;
			#endif

			#if defined (UNITY_PASS_DEFERRED)
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
				fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
				
				o.worldTangent.xyz = worldTangent;
				fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
		  		o.worldBinormal_screenPos.xyz = cross(worldNormal, worldTangent) * tangentSign;
		  		// Store Dither blending state
				#if defined (LUX_TRANSLUCENTLIGHTING)
					o.worldBinormal_screenPos.w = screenPos.z;
					// In order to get more efficient branching we calculate the blend state per object and not per vertex here.
					float3 worldObjectPosition = mul(unity_ObjectToWorld, float4(0,0,0,1) );
					o.worldTangent.w = distance(_WorldSpaceCameraPos, worldObjectPosition);
					o.worldTangent.w = saturate( (_DitherDistance - o.worldTangent.w) / _DitherRange);
				#endif
			#endif
		}


		void surf (Input IN, inout SurfaceOutputLuxAnisoMetallic o) {

			fixed4 c = tex2D (_MainTex, IN.lux_uv_MainTex.xy) * _Color;
			o.Alpha = c.a;
			o.Albedo = c.rgb;

			#if defined (_METALLICGLOSSMAP)
				fixed4 metallicGloss = tex2D (_MetallicGlossMap, IN.lux_uv_MainTex.xy);
				o.Smoothness = metallicGloss.a;
				o.Metallic = metallicGloss.r; // That is how the standard shaders handles it...
				o.Occlusion = metallicGloss.g;
			#else
				o.Smoothness = _Glossiness;
				o.Metallic = _Metallic;
			#endif

		//	The shader has to write to o.Normal as otherwise the needed tranformation matrix parameters will not get compiled out
			o.Normal = UnpackNormal( tex2D(_BumpMap, IN.lux_uv_MainTex.xy));

		//	Lux: Anisotropic features
			// We simply turn on or off translucency. So it is either 0 or 1. Mask is derived from metallic.
			o.Translucency = _Translucency;
			o.TangentDir = lerp( _BaseTangentDir, UnpackNormal( tex2D(_TangentDir, IN.lux_uv_MainTex.xy)), _TangentDirStrength);
			// tangent space basis -> tangent = (1, 0, 0), bitangent = (0, 1, 0) and normal = (0, 0, 1).

		//	Lux: Deferred anisotropic lighting specials
			#if defined (UNITY_PASS_DEFERRED)
			//	Apply dithering
				#if defined(LUX_TRANSLUCENTLIGHTING)
					o.dither = 1;
					UNITY_BRANCH
					if (IN.worldTangent.w > 0) {
						float2 screenPos = floor( IN.lux_uv_MainTex.zw / IN.worldBinormal_screenPos.w * _ScreenParams.xy );


	screenPos = ( IN.lux_uv_MainTex.zw / IN.worldBinormal_screenPos.w * _ScreenParams.xy );
						// Call the desired dither function
						o.dither = Lux_nrand(screenPos.xy);

screenPos = ( IN.lux_uv_MainTex.zw * _ScreenParams.xy ) + frac(_Time.yy) * _AnimateDither;
o.dither = Lux_hash12(screenPos.xy);
//o.Albedo = o.dither;

						// Tweak the dither parameter
						o.dither = lerp(1, lerp(1 - _DitherSpread, 1 + _DitherSpread, o.dither), IN.worldTangent.w);

					}
					// o.Albedo += o.dither * 0.01; // compiler bug?
				#endif
			//	Calculate world tangent direction
					half3 n = WorldNormalVector(IN, half3(0, 0, 1) ); //o.Normal); // 
				half3x3 tangent2World = half3x3(IN.worldTangent.xyz, IN.worldBinormal_screenPos.xyz, n);
				o.worldTangentDir = mul( o.TangentDir, (tangent2World));
			#endif
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
