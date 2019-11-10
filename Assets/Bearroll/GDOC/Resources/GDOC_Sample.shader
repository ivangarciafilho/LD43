// Upgrade NOTE: replaced '_LightMatrix0' with 'unity_WorldToLight'

// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/GDOC/Sample" {
	SubShader {
		Pass {

			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite Off
			ZTest Always
			Cull Off
			Blend Off		

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5
			#include "UnityCG.cginc"

			struct appdata {
			};

			struct v2f {
				float4 pos: SV_POSITION;
			};

			float4x4 unity_WorldToLight;
			RWBuffer<float> GDOC_sampled: register(u0);
			float GDOC_ShadowPassIndex;

			v2f vert(appdata v) {

				v2f o;

				o.pos = float4(0.9,0.9,1,1);

				return o;
			}

			void frag(v2f i) {

				int s = (int) GDOC_ShadowPassIndex * 16;

				float4 m0 = UNITY_MATRIX_VP[0];
				float4 m1 = UNITY_MATRIX_VP[1];
				float4 m2 = UNITY_MATRIX_VP[2];
				float4 m3 = UNITY_MATRIX_VP[3];

				GDOC_sampled[s+0] = m0[0];
				GDOC_sampled[s+1] = m1[0];
				GDOC_sampled[s+2] = m2[0];
				GDOC_sampled[s+3] = m3[0];
							 
				GDOC_sampled[s+4] = m0[1];
				GDOC_sampled[s+5] = m1[1];
				GDOC_sampled[s+6] = m2[1];
				GDOC_sampled[s+7] = m3[1];
							 
				GDOC_sampled[s+8] = m0[2];
				GDOC_sampled[s+9] = m1[2];
				GDOC_sampled[s+10] = m2[2];
				GDOC_sampled[s+11] = m3[2];
							 
				GDOC_sampled[s+12] = m0[3];
				GDOC_sampled[s+13] = m1[3];
				GDOC_sampled[s+14] = m2[3];
				GDOC_sampled[s+15] = m3[3];

			}


			ENDCG
		}

	}
	Fallback Off
}
