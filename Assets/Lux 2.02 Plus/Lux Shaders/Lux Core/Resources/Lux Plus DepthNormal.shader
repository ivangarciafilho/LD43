// We have to keep the original name as Unity's PostProcessing suit otherwise will pick up the built in shader 

Shader "Hidden/Internal-CombineDepthNormals" {
SubShader {
	
Pass {
	ZWrite Off ZTest Always Cull Off
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

#include "../Lux Utils/LuxUtilsDeferred.cginc"


struct appdata {
	float4 vertex : POSITION;
	float2 texcoord : TEXCOORD0;
};

struct v2f {
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
};
float4 _CameraNormalsTexture_ST;

v2f vert (appdata v)
{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = TRANSFORM_TEX(v.texcoord,_CameraNormalsTexture);
	return o;
}
sampler2D_float _CameraDepthTexture;
sampler2D _CameraNormalsTexture;
sampler2D _CameraGBufferTexture1;

fixed4 frag (v2f i) : SV_Target
{
	float d = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
	
	float4 nSample = tex2D (_CameraNormalsTexture, i.uv);
	float3 n = nSample.rgb * 2.0 - 1.0;

	// Handle Skin Normals
	if( nSample.a == 0) {
		n = DecodeOctahedronNormal(n.xy);
	}
	
	d = Linear01Depth (d);
	n = mul ((float3x3)unity_WorldToCamera, n);
	n.z = -n.z;
	return (d < (1.0-1.0/65025.0)) ? EncodeDepthNormal (d, n.xyz) : float4(0.5,0.5,1.0,1.0);
}
ENDCG
}

}
Fallback Off
}
