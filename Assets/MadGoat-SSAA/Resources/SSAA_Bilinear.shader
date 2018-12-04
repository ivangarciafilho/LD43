Shader "Hidden/SSAA_Bilinear" 
{
	Properties 
	{
	 _MainTex ("Texture", 2D) = "" {} 
	}
	SubShader {
		
		Pass {
 			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			
			#include "SSAA_Utils.cginc"
			#pragma vertex vert//_img
			#pragma fragment frag


			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _ResizeHeight;
			float _ResizeWidth;
			float _Sharpness;
			float _SampleDistance;
			
			struct Input
			{
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varying
			{
				float4 position : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float2 uvSPR : TEXCOORD1; // Single Pass Stereo UVs
			};

			Varying vert(Input input)
			{
				Varying o;
				o.position = UnityObjectToClipPos(input.position);
				o.texcoord = input.uv.xy;
				o.uvSPR = UnityStereoScreenSpaceUVAdjust(input.position.xy, _MainTex_ST);
				return o;
			}

			fixed4 frag(Varying i) : COLOR
			{
				float squareW = (_SampleDistance / _ResizeWidth);
				float squareH = (_SampleDistance / _ResizeHeight);
				
				// neighbor pixels
				float4 top = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST) + float2(0.0f, -squareH));
				float4 left = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST) + float2(-squareW, 0.0f));
				float4 mid = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST)  + float2(0.0f, 0.0f));
				float4 right = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST) + float2(squareW, 0.0f));
				float4 bot = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST) + float2(0.0f, squareH));
				
				// avg
				float4 sampleaverage = (top + left + right + bot) / 4;

				// lerp based on sharpness
				return lerp(sampleaverage, mid, _Sharpness);
			}
			ENDCG 

		}
	}
	Fallback Off 
}
