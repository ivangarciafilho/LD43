Shader "Hidden/NGSS_FrustumShadows"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Cull Off ZWrite Off ZTest Always

		CGINCLUDE

		#pragma vertex vert
		#pragma fragment frag
		#pragma exclude_renderers gles d3d9
		#pragma target 3.0

		#include "UnityCG.cginc"
		half4 _MainTex_ST;
	/*
#if !defined(UNITY_SINGLE_PASS_STEREO)
#define UnityStereoTransformScreenSpaceTex(uv) (uv)
#endif*/
		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
			//float2 uv2 : TEXCOORD0;
		};

		//float2 _Jitter_Offset;

		v2f vert(appdata v)
		{
			v2f o = (v2f)0;

			o.vertex = UnityObjectToClipPos(v.vertex);
			//o.vertex.xy += _Jitter_Offset / _ScreenParams.xy;
			//o.uv = v.uv;//NGSS 2.0
			o.uv = ComputeNonStereoScreenPos(o.vertex).xy;//NGSS 2.0

			//o.uv = UnityStereoTransformScreenSpaceTex(v.uv);

			#if UNITY_UV_STARTS_AT_TOP
			//o.uv2 = UnityStereoTransformScreenSpaceTex(v.uv);
			if (_MainTex_ST.y < 0.0)
				o.uv.y = 1.0 - o.uv.y;
			#endif

			//o.uv += _Jitter_Offset;

			return o;
		}

		ENDCG

	Pass // clip edges
	{
		CGPROGRAM

		sampler2D _CameraDepthTexture;
		half4 _CameraDepthTexture_ST;

		fixed4 frag(v2f input) : SV_Target
		{
			float depth = tex2D(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(input.uv)).r;
			//float depth = tex2D(_CameraDepthTexture, UnityStereoScreenSpaceUVAdjust(input.uv, _CameraDepthTexture_ST)).r;

			if (input.uv.x < 0.001 || input.uv.x > 0.999 || input.uv.y < 0.001 || input.uv.y > 0.999)
			//if (input.vertex.x <= 1.0 || input.vertex.x >= _ScreenParams.x - 1.0 || input.vertex.y <= 1.0 || input.vertex.y >= _ScreenParams.y - 1.0)
			{
				#if defined(UNITY_REVERSED_Z)
					depth = 0.0;
				#else
					depth = 1.0;
				#endif
			}

			return depth;
		}
		ENDCG
	}

	Pass // render screen space rt shadows
	{
		CGPROGRAM
		#pragma shader_feature NGSS_USE_DITHERING
		//#pragma shader_feature NGSS_RAY_SCREEN_SCALE
		#pragma shader_feature NGSS_USE_LOCAL_SHADOWS
		#pragma shader_feature NGSS_DEFERRED_OPTIMIZATION

		sampler2D _MainTex;
		//sampler2D _CameraDepthTexture;
		sampler2D _CameraGBufferTexture2;
		//sampler2D ScreenSpaceMask;
		//half4 _MainTex_ST;

		//float4x4 unity_CameraInvProjection;
		//float4x4 InverseView;//camera to world
		//float4x4 InverseViewProj;//clip to world
		//float4x4 InverseProj;//clip to camera			
		float4x4 WorldToView;//world to camera

		float3 LightDir;
		float3 LightDirWorld;
		float3 LightPos;
		float ShadowsDistanceStart;
		float RaySamples;
		float RayThickness;
		float RayScale;
		float RayScreenScale;
		//float ShadowsFade;
		float ShadowsBias;
		//float RaySamplesScale;
		float BackfaceOpacity;

		#define ditherPattern float4x4(0.0,0.5,0.125,0.625, 0.75,0.22,0.875,0.375, 0.1875,0.6875,0.0625,0.5625, 0.9375,0.4375,0.8125,0.3125)

		fixed4 frag(v2f input) : SV_Target
		{
			float2 coord = input.uv;
			float shadow = 1.0;

			#if defined(NGSS_DEFERRED_OPTIMIZATION)
			float3 normal = tex2D(_CameraGBufferTexture2, coord.xy).xyz * 2 - 1;
			#if !defined(NGSS_USE_LOCAL_SHADOWS)
			//if(dot(mul(WorldToView, float4(normal.xyz, 0.0)), LightDir * float3(1.0, 1.0, -1.0)) < 0.0) { return 0.0; }
			if (dot(normal, LightDirWorld) <= 0.0) { return BackfaceOpacity; }
			#endif
			#endif

			float depth = SAMPLE_DEPTH_TEXTURE_LOD(_MainTex, float4(UnityStereoTransformScreenSpaceTex(coord.xy), 0, 0)).r;
			//float depth = tex2Dlod(_MainTex, float4(UnityStereoScreenSpaceUVAdjust(coord.xy, _MainTex_ST), 0, 0)).r;
			float linearDepth = Linear01Depth(depth) * _ProjectionParams.z;
			if (linearDepth < ShadowsDistanceStart) return 1.0;

			#if defined(UNITY_REVERSED_Z)
				depth = 1.0 - depth;
			#endif
			
			//clip(depth - 0.5);
			//if(UNITY_Z_0_FAR_FROM_CLIPSPACE(depth) < 0.25) return 1.0;

			float4 clipPos = float4(float3(coord.xy, depth) * 2.0 - 1.0, 1.0);

			float4 viewPos = mul(unity_CameraInvProjection, clipPos);//go from clip to view space | InverseProj
			viewPos.xyz /= viewPos.w;
			//viewPos.z *= -1;

			//if (-viewPos.z > 1.0 / _ProjectionParams.z * 0.025) return 1.0;

			float samplers = RaySamples;//lerp(RaySamples / -viewPos.z, RaySamples, RaySamplesScale);//reduce samplers over distance
			#if defined(NGSS_USE_LOCAL_SHADOWS)
			float3 lightDir = normalize(mul(WorldToView, float4(LightPos.xyz, 1.0)) - viewPos).xyz;//W == 1.0 treat as position | W == 0.0 treat as direction
			//NdotL backface check in deferred
			float3 rayDir = lightDir;
			#else
			float3 rayDir = LightDir * float3(1.0, 1.0, -1.0);//step size and length;
			#endif

			//#if defined(NGSS_RAY_SCREEN_SCALE)
			//rayDir = rayDir / samplers * -viewPos.z * RayScale;
			//#else
			//rayDir = rayDir * (RayScale / samplers);//fixed length
			//#endif

			rayDir = lerp(rayDir * (RayScale / samplers), rayDir / samplers * -viewPos.z * RayScale, RayScreenScale);

			#if defined(NGSS_USE_DITHERING)
				//float3 rayPos = viewPos.xyz + rayDir * saturate(frac(sin(dot(coord.xy, float2(12.9898, 78.223))) * 43758.5453));
				float3 rayPos = viewPos.xyz + rayDir * ditherPattern[input.uv.x * _ScreenParams.x % 4][input.uv.y * _ScreenParams.y % 4];
				//rayDir = normalize(rayDir * ditherPattern[input.uv.x * _ScreenParams.x % 4][input.uv.y * _ScreenParams.y % 4]) / samplers * -viewPos.z * RayScale;			
			#else
				float3 rayPos = viewPos.xyz;
			#endif

			float bias = (viewPos.z * ShadowsBias);

			//rayDir.xy /= float2(1.0 / _ScreenParams.z, 1.0 / _ScreenParams.w);

			for (float i = 0; i < samplers; i++)
			{
				rayPos += rayDir;

				float4 rayPosProj = mul(unity_CameraProjection, float4(rayPos.xyz, 0.0));
				rayPosProj.xy = rayPosProj.xy / rayPosProj.w * 0.5 + 0.5;//0-1 
				//rayPosProj.xy = (rayPosProj.xy / rayPosProj.w + 1.0) / 2.0;

				//if (rayPosProj.x < 0.001 || rayPosProj.x > 0.999 || rayPosProj.y < 0.001 || rayPosProj.y > 0.999) { return 1.0; }

				float lDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_LOD(_MainTex, float4(UnityStereoTransformScreenSpaceTex(rayPosProj.xy), 0, 0)).r);				
				//float lDepth = LinearEyeDepth(tex2Dlod(_MainTex, float4(UnityStereoScreenSpaceUVAdjust(rayPosProj.xy, _MainTex_ST), 0, 0)).r);

				float depthDiff = -rayPos.z - lDepth + bias;//0.02

				//shadow *= depthDiff > 0.0 && depthDiff < RayThickness? i / samplers * ShadowsFade : 1.0;
				if (depthDiff > 0.0 && depthDiff < RayThickness * -viewPos.z)
				{
					shadow = 0.0;
					//float fade = i / samplers;
					//shadow = pow(fade, 10);//fade
					break;
				}
			}

			//return shadow;//test
			float fadeDiff = smoothstep(ShadowsDistanceStart, max(0.0, ShadowsDistanceStart + 10), linearDepth);
			return lerp(1.0, shadow, fadeDiff);
		}
		ENDCG
	}

	Pass // simple and bilateral blur (taking into account screen depth)
	{
		CGPROGRAM
		#pragma shader_feature NGSS_FAST_BLUR

		sampler2D _CameraDepthTexture;

		float ShadowsSoftness;
		//float ShadowsOpacity;

		sampler2D _MainTex;
		//sampler2D NGSS_ContactShadowsTexture;
		//float4 NGSS_ContactShadowsTexture_ST;

		float2 ShadowsKernel;
		float ShadowsEdgeTolerance;

		fixed4 frag(v2f input) : COLOR0
		{
			float weights = 0.0;
			float result = 0.0;
			float2 offsets = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y) * ShadowsKernel.xy * ShadowsSoftness;

			#if defined(NGSS_FAST_BLUR)

				for (float i = -1; i <= 1; i++)
				{
					float2 offs = i * offsets;
					float shadow = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(input.uv + offs.xy)).r;
					result += shadow;
				}

				return result / 3;
				//return lerp(ShadowsOpacity, 1.0, result / 3);				

			#else

				float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(input.uv)));
				//offsets /= depth/depth;//adjust kernel size over distance

				for (float i = -1; i <= 1; i++)
				{
					float2 offs = i * offsets;
					float curDepth = LinearEyeDepth(tex2Dlod(_CameraDepthTexture, float4(input.uv + offs.xy, 0, 0)));

					float curWeight = saturate(1.0 - abs(depth - curDepth) / ShadowsEdgeTolerance);
					//float curWeight = saturate( ShadowsEdgeTolerance - abs(depth - curDepth));
					float shadow = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(input.uv + offs.xy)).r;
					result += shadow * curWeight;
					weights += curWeight;
				}

				return result / weights;
				//return lerp(ShadowsOpacity, 1.0, result / weights);

			#endif
		}

		ENDCG
	}

	Pass // temporal #1
	{
		CGPROGRAM

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;

		sampler2D NGSS_Temporal_Tex;
		sampler2D _CameraMotionVectorsTexture;

		//float4x4 _TemporalMatrix;
		float _TemporalScale;

		float2 _Jitter_Offset;

		fixed4 frag(v2f input) : SV_Target
		{
			input.uv += _Jitter_Offset;
			float2 motion = tex2D(_CameraMotionVectorsTexture, input.uv).xy;

			float current = tex2D(_MainTex, input.uv).r;
			float last = tex2D(NGSS_Temporal_Tex, input.uv - motion).r;

			return lerp(current, last, _TemporalScale);
		}
		ENDCG
	}

	}
		Fallback Off
}
