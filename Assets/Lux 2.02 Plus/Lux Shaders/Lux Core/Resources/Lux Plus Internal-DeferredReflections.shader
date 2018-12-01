Shader "Hidden/Lux Plus Internal-DeferredReflections" {
Properties {
	_SrcBlend ("", Float) = 1
	_DstBlend ("", Float) = 1
}
SubShader {

// Calculates reflection contribution from a single probe (rendered as cubes) or default reflection (rendered as full screen quad)
Pass {
	ZWrite Off
	ZTest LEqual
	Blend [_SrcBlend] [_DstBlend]
CGPROGRAM
#pragma target 3.0
#pragma vertex vert_deferred
#pragma fragment frag

#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityPBSLighting.cginc"

#include "../Lux Config.cginc"
#include "../Lux Utils/LuxUtilsDeferred.cginc"

sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;

half3 distanceFromAABB(half3 p, half3 aabbMin, half3 aabbMax)
{
	return max(max(p - aabbMax, aabbMin - p), half3(0.0, 0.0, 0.0));
}

// Ref: Donald Revie - Implementing Fur Using Deferred Shading (GPU Pro 2)
// The grain direction (e.g. hair or brush direction) is assumed to be orthogonal to the normal.
// The returned normal is NOT normalized.
float3 ComputeGrainNormal(float3 grainDir, float3 V)
{
	float3 B = cross(-V, grainDir);
	return cross(B, grainDir);
}

// Fake anisotropic by distorting the normal.
// The grain direction (e.g. hair or brush direction) is assumed to be orthogonal to N.
// Anisotropic ratio (0->no isotropic; 1->full anisotropy in tangent direction)
float3 GetAnisotropicModifiedNormal(float3 grainDir, float3 N, float3 V, float anisotropy)
{
	float3 grainNormal = ComputeGrainNormal(grainDir, V);
	return lerp(N, grainNormal, anisotropy);
}


half4 frag (unity_v2f_deferred i) : SV_Target
{
	// Stripped from UnityDeferredCalculateLightParams, refactor into function ?
	i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
	float2 uv = i.uv.xy / i.uv.w;

	// read depth and reconstruct world position
	float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
	depth = Linear01Depth (depth);
	float4 viewPos = float4(i.ray * depth,1);
	float3 worldPos = mul (unity_CameraToWorld, viewPos).xyz;

	half4 gbuffer0 = tex2D (_CameraGBufferTexture0, uv);
	half4 gbuffer1 = tex2D (_CameraGBufferTexture1, uv);
	half4 gbuffer2 = tex2D (_CameraGBufferTexture2, uv);

//	///////////////////////////////////////
//	Lux: Check material - Index is stored in gbuffer2.a
	const half materialIndex  	= floor(gbuffer2.a * 3 + 0.5f); 
	const half isRegularMat 	= (materialIndex == 3) ? 1 : 0; // 1    --> 3 – all regular shaders
	const half isTransMat 		= (materialIndex == 2) ? 1 : 0; // 0.66 --> 2
	const half isAnisoMat 		= (materialIndex == 1) ? 1 : 0; // 0.33 --> 1
	const half isSkinMat 		= (materialIndex == 0) ? 1 : 0;	// 0

	half3 worldNormal = gbuffer2.rgb * 2 - 1;
	half3 worldNormalRefl;
	half oneMinusRoughness = gbuffer1.a;
	half3 specColor = gbuffer1.rgb;

	float3 eyeVec = normalize(worldPos - _WorldSpaceCameraPos);

	//UNITY_BRANCH
	half isGrass = 0;
	half specMask = 1;
	if (isTransMat) {
	//	Pick grass: spec color r is 1
		isGrass = (gbuffer1.r == 1) ? 1 : 0;
		specColor.rgb = (isGrass) ? unity_ColorSpaceDielectricSpec.rgb : gbuffer1.rrr;
		specMask = (isGrass) ? saturate(gbuffer1.g + 0.5) : 1;
	}
	//UNITY_BRANCH
	else if (isSkinMat) {
		worldNormal = DecodeOctahedronNormal(worldNormal.xy);
		specColor = unity_ColorSpaceDielectricSpec.rgb * 0.7; // < -- Debug, correct is: * 0.7;
	}
	//UNITY_BRANCH
	else if (isAnisoMat) {
		half metallic = frac(gbuffer1.b * 1.9); //99); // does not handle 0.0
		specColor = lerp (unity_ColorSpaceDielectricSpec.rgb, gbuffer0.rgb, metallic);
	//	new
		fixed3 tangentWS = DecodeOctahedronNormal(gbuffer1.rg * 2.0 - 1.0);
		fixed3 bitangentWS = cross(worldNormal, tangentWS);
		worldNormal = GetAnisotropicModifiedNormal(bitangentWS, worldNormal, -eyeVec, .5f );
	//	To simulate the streching of highlight at grazing angle for IBL we shrink the roughness which allow to fake an anisotropic specular lobe.
	//	Ref: http://www.frostbite.com/2015/08/stochastic-screen-space-reflections/ - slide 84
		float roughness = 1.0f - oneMinusRoughness;
		oneMinusRoughness = 1.0 - lerp(roughness, 1.0, saturate(dot(worldNormal, eyeVec) * 2.0) );
	}
	// The only way to make glcore handle it correctly...
	// worldNormalRefl = reflect(eyeVec, normalize (worldNormal + DecodeOctahedronNormal(gbuffer1.rg * 2 - 1) * 0.25 * isAnisoMat) );
	worldNormalRefl = reflect(eyeVec, normalize(worldNormal));
	half oneMinusReflectivity = 1 - SpecularStrength(specColor.rgb);
	half occlusion = gbuffer0.a * specMask;

//	/////////

	// Unused member don't need to be initialized
	UnityGIInput d;
	d.worldPos = worldPos;
	d.worldViewDir = -eyeVec;
	d.probeHDR[0] = unity_SpecCube0_HDR;
		d.boxMin[0].w = 1; // 1 in .w allow to disable blending in UnityGI_IndirectSpecular call since it doesn't work in Deferred

	float blendDistance = unity_SpecCube1_ProbePosition.w; // will be set to blend distance for this probe
	#ifdef UNITY_SPECCUBE_BOX_PROJECTION
		d.probePosition[0]	= unity_SpecCube0_ProbePosition;
		d.boxMin[0].xyz		= unity_SpecCube0_BoxMin - float4(blendDistance,blendDistance,blendDistance,0);
		d.boxMax[0].xyz		= unity_SpecCube0_BoxMax + float4(blendDistance,blendDistance,blendDistance,0);
	#endif

	Unity_GlossyEnvironmentData g;
	g.roughness		= 1.0 - oneMinusRoughness;
	g.reflUVW		= worldNormalRefl;

	half3 env0 = UnityGI_IndirectSpecular(d, occlusion, g);

	UnityLight light;
	light.color = half3(0, 0, 0);
	light.dir = half3(0, 1, 0);

	UnityIndirect ind;
	ind.diffuse = 0;
	ind.specular = env0 * occlusion;


#if LUX_LAZAROV_ENVIRONMENTAL_BRDF 
//	Lazarov 2013, "Getting More Physical in Call of Duty: Black Ops II", changed by EPIC
	half dotNV = DotClamped(worldNormal, -eyeVec);
	const half4 c0 = { -1, -0.0275, -0.572, 0.022 };
	const half4 c1 = { 1, 0.0425, 1.04, -0.04 };
	half4 r = (g.roughness) * c0 + c1;
	half a004 = min( r.x * r.x, exp2( -9.28 * dotNV ) ) * r.x + r.y;
	half2 AB = half2( -1.04, 1.04 ) * a004 + r.zw;
	half3 F_L = specColor * AB.x + AB.y;
	half3 rgb = ind.specular * F_L;
#else
	half3 rgb = UNITY_BRDF_PBS (0, specColor, oneMinusReflectivity, oneMinusRoughness, worldNormal, -eyeVec, light, ind).rgb;
#endif

	// Calculate falloff value, so reflections on the edges of the probe would gradually blend to previous reflection.
	// Also this ensures that pixels not located in the reflection probe AABB won't
	// accidentally pick up reflections from this probe.
	half3 distance = distanceFromAABB(worldPos, unity_SpecCube0_BoxMin.xyz, unity_SpecCube0_BoxMax.xyz);
	half falloff = saturate(1.0 - length(distance)/blendDistance);

	return half4(rgb, falloff);
}

ENDCG
}

// Adds reflection buffer to the lighting buffer
Pass
{
	ZWrite Off
	ZTest Always
	Blend [_SrcBlend] [_DstBlend]

	CGPROGRAM
		#pragma target 3.0
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile ___ UNITY_HDR_ON

		#include "UnityCG.cginc"

		sampler2D _CameraReflectionsTexture;

		struct v2f {
			float2 uv : TEXCOORD0;
			float4 pos : SV_POSITION;
		};

		v2f vert (float4 vertex : POSITION)
		{
			v2f o;
			o.pos = UnityObjectToClipPos(vertex);
			o.uv = ComputeScreenPos (o.pos).xy;
			return o;
		}

		half4 frag (v2f i) : SV_Target
		{
			half4 c = tex2D (_CameraReflectionsTexture, i.uv);
			#ifdef UNITY_HDR_ON
			return float4(c.rgb, 0.0f);
			#else
			return float4(exp2(-c.rgb), 0.0f);
			#endif

		}
	ENDCG
}

}
Fallback Off
}
