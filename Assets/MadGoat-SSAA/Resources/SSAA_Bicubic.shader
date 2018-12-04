Shader "Hidden/SSAA_Bicubic" 
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

			#pragma vertex vert //_img
			#pragma fragment frag
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _ResizeHeight;
			float _ResizeWidth;
			float _Sharpness;
			float _SampleDistance;
			#define stereoInput.texcoord UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST)
			
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
				float4 mid = tex2D(_MainTex,  UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST));
				float4 tex = float4(1 / _ResizeWidth, 1 / _ResizeHeight, _ResizeWidth, _ResizeHeight);
				
				float2 uv = UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST) * tex.zw + 0.5;

				float2 iuv = floor(uv);
				float2 fuv = frac(uv);

				float ampl0x = ampl0(fuv.x);
				float ampl1x = ampl1(fuv.x);
				float off0x = off0(fuv.x);
				float off1x = off1(fuv.x);
				float off0y = off0(fuv.y);
				float off1y = off1(fuv.y);

				float2 pixel0 = (float2(iuv.x + off0x, iuv.y + off0y) - 0.5) * tex.xy;
				float2 pixel1 = (float2(iuv.x + off1x, iuv.y + off0y) - 0.5) * tex.xy;
				float2 pixel2 = (float2(iuv.x + off0x, iuv.y + off1y) - 0.5) * tex.xy;
				float2 pixel3 = (float2(iuv.x + off1x, iuv.y + off1y) - 0.5) * tex.xy;

				float4 col = ampl0(fuv.y) * (ampl0x * tex2D(_MainTex, pixel0) +
					ampl1x * tex2D(_MainTex, pixel1)) +
					ampl1(fuv.y) * (ampl0x * tex2D(_MainTex, pixel2) +
					ampl1x * tex2D(_MainTex, pixel3));
				return lerp(col,mid,_Sharpness);
			}
			ENDCG 
		}
	}
	Fallback Off 
}
