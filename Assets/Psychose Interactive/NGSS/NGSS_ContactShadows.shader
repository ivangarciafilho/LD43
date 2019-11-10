Shader "Hidden/NGSS_ContactShadows"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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

		v2f vert (appdata v)
		{
			v2f o = (v2f)0;

			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.uv;
			//o.uv = UnityStereoTransformScreenSpaceTex(v.uv);
			
			#if UNITY_UV_STARTS_AT_TOP
			//o.uv2 = UnityStereoTransformScreenSpaceTex(v.uv);
			if (_MainTex_ST.y < 0.0)
				o.uv.y = 1.0 - o.uv.y;
			#endif
			return o;
		}

		ENDCG
		
		Pass // clip edges
		{
			CGPROGRAM
			
			sampler2D _CameraDepthTexture;
			half4 _CameraDepthTexture_ST;

			fixed4 frag (v2f input) : SV_Target
			{
				float depth = tex2D(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(input.uv)).r;
				//float depth = tex2D(_CameraDepthTexture, UnityStereoScreenSpaceUVAdjust(input.uv, _CameraDepthTexture_ST)).r;

				if (input.vertex.x <= 1.0 || input.vertex.x >= _ScreenParams.x - 1.0 ||  input.vertex.y <= 1.0 || input.vertex.y >= _ScreenParams.y - 1.0)
				{
					#if defined(UNITY_REVERSED_Z)
						depth = 0.0;
					#else
						depth = 1.0;
					#endif
				}

				return depth.xxxx;
			}
			ENDCG
		}

		Pass // render screen space rt shadows
		{
			CGPROGRAM
			#pragma multi_compile _ NGSS_CONTACT_SHADOWS_USE_NOISE

			sampler2D _MainTex;
			//half4 _MainTex_ST;

			float3 LightDir;
			float ShadowsDistance;
			float RaySamples;
			float RayWidth;
			float ShadowsFade;
			float ShadowsBias;
			float RaySamplesScale;

			fixed4 frag (v2f input) : SV_Target
			{
				float2 coord = input.uv;
				float shadow = 1.0;
				float depth = tex2Dlod(_MainTex, float4(UnityStereoTransformScreenSpaceTex(coord.xy), 0, 0)).r;
				//float depth = tex2Dlod(_MainTex, float4(UnityStereoScreenSpaceUVAdjust(coord.xy, _MainTex_ST), 0, 0)).r;

				#if defined(UNITY_REVERSED_Z)
					depth = 1.0 - depth;
				#endif

				coord.xy = coord.xy * 2.0 - 1.0;
				float4 viewPos = mul(unity_CameraInvProjection, float4(coord.xy, depth * 2.0 - 1.0, 1.0));
				viewPos.xyz /= viewPos.w;				
				
				float samplers = lerp(RaySamples / -viewPos.z, RaySamples, RaySamplesScale);//reduce samplers over distance
				float3 rayDir = -LightDir * float3(1.0, 1.0, -1.0) * (ShadowsDistance / samplers);
				#if defined(NGSS_CONTACT_SHADOWS_USE_NOISE)
				float3 rayPos = viewPos + rayDir * saturate(frac(sin(dot(coord, float2(12.9898, 78.223))) * 43758.5453));
				#else
				float3 rayPos = viewPos + rayDir;
				#endif

				for (float i = 0; i < samplers; i++)
				{
					rayPos += rayDir;
					
					float4 rayPosProj = mul(unity_CameraProjection, float4(rayPos.xyz, 0.0));
					rayPosProj.xyz = rayPosProj.xyz / rayPosProj.w * 0.5 + 0.5;
					
					float lDepth = LinearEyeDepth(tex2Dlod(_MainTex, float4(UnityStereoTransformScreenSpaceTex(rayPosProj.xy), 0, 0)).r);
					//float lDepth = LinearEyeDepth(tex2Dlod(_MainTex, float4(UnityStereoScreenSpaceUVAdjust(rayPosProj.xy, _MainTex_ST), 0, 0)).r);

					float depthDiff = -rayPos.z - lDepth + (viewPos.z * ShadowsBias);//0.02
					shadow *= (depthDiff > 0.0 && depthDiff < RayWidth)? i / samplers * ShadowsFade : 1.0;
				}
				
				return shadow.rrrr;
			}
			ENDCG
		}
		/*
		Pass // poison blur
		{
			CGPROGRAM			

			sampler2D _MainTex;
			//half4 _MainTex_ST;

			//sampler2D _CameraDepthTexture;

			float ShadowsSoftness;
			float ShadowsOpacity;

			static float2 poissonDisk[9] =
			{
				float2 ( 0.4636456f, 0.3294131f),
				float2 ( 0.3153244f, 0.8371656f),
				float2 ( 0.7389247f, -0.3152522f),
				float2 ( -0.1819379f, -0.3826133f),
				float2 ( -0.38396f, 0.2479579f),
				float2 ( 0.1985026f, -0.8434925f),
				float2 ( -0.25466f, 0.9213307f),
				float2 ( -0.8729509f, -0.3795996f),
				float2 ( -0.8918442f, 0.3004266f)
			};

			float rand01(float2 seed)
			{
			   float dt = dot(seed, float2(12.9898,78.233));// project seed on random constant vector   
			   return frac(sin(dt) * 43758.5453);// return only fractional part
			}

			// returns random angle
			float randAngle(float2 seed)
			{
				return rand01(seed)*6.283285;
			}

			fixed4 frag(v2f input) : COLOR0
			{
				float result = 0.0;//tex2Dlod(_MainTex, float4(input.uv.xy, 0, 0)).r;
				ShadowsSoftness *= (_ScreenParams.zw - 1.0);
				//float angle = randAngle(input.uv.xy);
				//float s = sin(angle);
				//float c = cos(angle);

				//float lDepth = LinearEyeDepth(tex2Dlod(_CameraDepthTexture, float4(input.uv, 0, 0))) * 0.5;

				for(int i = 0; i < 9; ++i)
				{
					//float2 offs = float2(poissonDisk[i].x * c + poissonDisk[i].y * s, poissonDisk[i].x * -s + poissonDisk[i].y * c) * ShadowsSoftness;//rotated samples
					float2 offs = poissonDisk[i] * ShadowsSoftness;// / lDepth;//no rotation
					//result += tex2Dlod(_MainTex, float4(input.uv + offs.xy, 0, 0)).r;
					result += tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(input.uv + offs.xy)).r;
				}

				result /= 9.0;
				result += ShadowsOpacity;//faster opacity
				//result = lerp(ShadowsOpacity, 1.0, result);
				return result.xxxx;
			}

			ENDCG
		}*/

		Pass // bilateral blur (taking into account screen depth)
		{
			CGPROGRAM			

			sampler2D _CameraDepthTexture;

			float ShadowsSoftness;
			float ShadowsOpacity;

			sampler2D _MainTex;
			float2 ShadowsKernel;
			float ShadowsEdgeTolerance;

			fixed4 frag(v2f input) : COLOR0
			{
				float weights = 0.0;
				float result = 0.0;
				float2 offsets = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y) * ShadowsKernel.xy * ShadowsSoftness;

				float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(input.uv)));
				offsets /= depth;//adjust kernel size over distance

				for (float i = -1; i <= 1; i++)
				{
					float2 offs = i * offsets;
					float curDepth = LinearEyeDepth(tex2Dlod(_CameraDepthTexture, float4(input.uv + offs.xy, 0, 0)));

					float curWeight = saturate(1.0 - abs(depth - curDepth) / ShadowsEdgeTolerance);

					float blurSample = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(input.uv + offs.xy)).r;
					result += blurSample * curWeight;
					weights += curWeight;
				}

				result /= weights;//weights + 0.001

				return result.xxxx;
			}

			ENDCG
		}

		Pass // final mix
		{
			BlendOp Min
			Blend DstColor Zero

			CGPROGRAM
			
			sampler2D NGSS_ContactShadowsTexture;
			half4 NGSS_ContactShadowsTexture_ST;
			float ShadowsOpacity;

			fixed4 frag (v2f input) : SV_Target
			{
				//return tex2D(NGSS_ContactShadowsTexture, input.uv);
				return saturate(tex2D(NGSS_ContactShadowsTexture, UnityStereoScreenSpaceUVAdjust(input.uv, NGSS_ContactShadowsTexture_ST)) + ShadowsOpacity);
			}
			ENDCG
		}
	}
	Fallback Off
}
