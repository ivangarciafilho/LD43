Shader "Hidden/SSAA_Def" 
{
	Properties 
	{
		_MainTex ("Texture", 2D) = "" {} 
	}
	SubShader {
		
		Pass {
 			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			// include the unityCG.cginc
			#include "SSAA_Utils.cginc"

			#pragma vertex vert//_img
			#pragma fragment frag

			sampler2D _MainTex;
			float4 _MainTex_ST;
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

			fixed4 frag(v2f i, out float outDepth : SV_Depth) : COLOR
			{
				outDepth = 0;
				return tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord, _MainTex_ST)).rgba;
			}
			ENDCG 

		}
	}
	Fallback Off 
}
