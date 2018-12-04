Shader "Hidden/SSAA_Nearest" 
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

			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Sharpness;
			float _ResizeHeight;
			float _ResizeWidth;

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
			fixed4 frag(v2f i) : COLOR
			{
				float2 uv = float2(UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST).x * _ResizeWidth, UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST).y * _ResizeHeight);
				float2 f = frac(uv);
				uv = float2(floor(uv.x)/_ResizeWidth, floor(uv.y)/_ResizeHeight);

				return lerp(tex2D(_MainTex, uv).rgba,tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST)).rgba,_Sharpness);
			}
			ENDCG 

		}
	}
	Fallback Off 
}
