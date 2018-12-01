Shader "Hidden/Lux Plus Internal-DeferredShading" {
Properties {
	_LightTexture0 ("", any) = "" {}
	_LightTextureB0 ("", 2D) = "" {}
	_ShadowMapTexture ("", any) = "" {}
	_SrcBlend ("", Float) = 1
	_DstBlend ("", Float) = 1
}
SubShader {

// Pass 1: Lighting pass
//  LDR case - Lighting encoded into a subtractive ARGB8 buffer
//  HDR case - Lighting additively blended into floating point buffer
Pass {
	ZWrite Off
	Blend [_SrcBlend] [_DstBlend]

CGPROGRAM
#pragma target 3.0
#pragma vertex vert_deferred
#pragma fragment frag
#pragma multi_compile_lightpass
#pragma multi_compile ___ UNITY_HDR_ON

#pragma multi_compile __ LUX_AREALIGHTS
#pragma multi_compile __ LUX_LIGHTINGFADE

#pragma exclude_renderers nomrt

#include "UnityCG.cginc"
#include "Lux Plus Deferred Library.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityStandardBRDF.cginc"

#include "../Lux Utils/LuxUtilsDeferred.cginc"
#include "../Lux Lighting/LuxAreaLights.cginc"
#include "../Lux BRDFs/LuxStandardBRDF.cginc"
#include "../Lux BRDFs/LuxSkinBRDF.cginc"
#include "../Lux BRDFs/LuxAnisoBRDF.cginc"

sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;

half4 _Lux_Tanslucent_Settings;
half _Lux_Transluclent_NdotL_Shadowstrength;
half4 _Lux_Anisotropic_Settings;

half4 CalculateLight (unity_v2f_deferred i)
{
	float3 wpos;
	float2 uv;
	float atten, fadeDist, shadow, transfade;
	UnityLight light;
	UNITY_INITIALIZE_OUTPUT(UnityLight, light);
	
//	///////////////////////////////////////	
//	Lux: Light attenuation and shadow attenuation will be returned separately	
	LuxDeferredCalculateLightParams (i, wpos, uv, light.dir, atten, fadeDist, shadow, transfade);

	half4 gbuffer0 = tex2D (_CameraGBufferTexture0, uv);
	half4 gbuffer1 = tex2D (_CameraGBufferTexture1, uv);
	half4 gbuffer2 = tex2D (_CameraGBufferTexture2, uv);

//	///////////////////////////////////////
//	Lux: Check material - Index is stored in gbuffer2.a
	const fixed materialIndex  	= floor(gbuffer2.a * 3 + 0.5f); 
	const fixed isRegularMat 	= (materialIndex == 3) ? 1 : 0; // 1    --> 3 – all regular shaders
	const fixed isTransMat 		= (materialIndex == 2) ? 1 : 0; // 0.66 --> 2
	const fixed isAnisoMat 		= (materialIndex == 1) ? 1 : 0; // 0.33 --> 1
	const fixed isSkinMat 		= (materialIndex == 0) ? 1 : 0;	// 0

	light.color = _LightColor.rgb * atten;

	half3 baseColor = gbuffer0.rgb;
	half3 specColor = gbuffer1.rgb;
	half oneMinusRoughness = gbuffer1.a;

	float3 eyeVec = normalize(wpos-_WorldSpaceCameraPos);
	half oneMinusReflectivity = 1 - SpecularStrength(specColor.rgb);
	
	UnityIndirect ind;
	UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
	ind.diffuse = 0;
	ind.specular = 0;

//	//////////////////////
//	Lux: Set up the needed variables for area lights and the different BRDFs
	half4 res = 1;
	half3 diffuseLightDir;
	half ndotlDiffuse = 1;
	half3 diffuseNormalWorld;
	diffuseLightDir = light.dir;

	half specularIntensity = 1;
	fixed curvature = gbuffer2.b;
	half translucency = gbuffer1.b * (isTransMat + isSkinMat);
	half power = 0;

	half3 normalWorld = gbuffer2.rgb * 2 - 1;
	if (isSkinMat) {
		normalWorld = DecodeOctahedronNormal(gbuffer1.rg * 2 - 1);
		diffuseNormalWorld = DecodeOctahedronNormal(gbuffer2.rg * 2 - 1);
		specColor = unity_ColorSpaceDielectricSpec.rgb * 0.7;
	}
	else {
		diffuseNormalWorld = normalWorld;
	}
	
	half isGrass = 0;
	half specMask = 1;
	if (isTransMat) {
	//	Pick grass: spec color r is 1
		isGrass = (gbuffer1.r == 1) ? 1 : 0;
		//power = specColor.g * 8.0;
		power = lerp(specColor.g * 8.0, 6.0f, isGrass);
		specColor.rgb = (isGrass) ? unity_ColorSpaceDielectricSpec.rgb : specColor.rrr;
		specMask = (isGrass) ? gbuffer1.g : 1;
	}

//	///////////////////////////////////////	
//	Lux: Important!
	normalWorld = normalize(normalWorld); // To avoid strange lighting artifacts on very smooth surfaces

//	/////////////
	half nl = saturate(dot(normalWorld, light.dir));
	ndotlDiffuse = nl;

//	///////////////////////////////////////	
//	Lux: Area lights
	#if defined(LUX_AREALIGHTS)
		// NOTE: Deferred needs other inputs than forward
		float3 lightPos = float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]);
		Lux_AreaLight (light, specularIntensity, diffuseLightDir, ndotlDiffuse, light.dir, _LightColor.a, lightPos, wpos, eyeVec, normalWorld, diffuseNormalWorld, 1.0 - oneMinusRoughness);
		nl = saturate(dot(normalWorld, light.dir));
	#else
		diffuseLightDir = light.dir;
		// If area lights are disabled we still have to reduce specular intensity
		#if !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE)
			specularIntensity = saturate(_LightColor.a);
		#endif
	#endif

//	///////////////////////////////////////	
//	Lux: Set up inputs shared by all BRDF - so if we branch we do it as effective as possible
	#define viewDir -eyeVec

	half3 halfDir = Unity_SafeNormalize (light.dir + viewDir);
	#define UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV 0
	#if UNITY_HANDLE_CORRECTLY_NEGATIVE_NDOTV
	    // The amount we shift the normal toward the view vector is defined by the dot product.
	    half shiftAmount = dot(normalWorld, viewDir);
	    normalWorld = shiftAmount < 0.0f ? normalWorld + viewDir * (-shiftAmount + 1e-5f) : normalWorld;
	    // A re-normalization should be applied here but as the shift is small we don't do it to save ALU.
	    //normalWorld = normalize(normalWorld);
	    half nv = saturate(dot(normalWorld, viewDir)); // TODO: this saturate should no be necessary here
	#else
	    half nv = abs(dot(normalWorld, viewDir));    // This abs allow to limit artifact
	#endif
	half nh = saturate(dot(normalWorld, halfDir));
	half lv = saturate(dot(light.dir, viewDir));
    half lh = saturate(dot(light.dir, halfDir));

//	/////////////////////
//	Lux: Standard Lighting
	//UNITY_BRANCH
	if (isRegularMat || isTransMat) {
	
	//	Add support for "real" lambert lighting and specular grass lighting
		specularIntensity = (specColor.r == 0.0) ? 0.0 : specularIntensity * specMask;

	//	Direct lighting
		res = Lux_BRDF1_PBS (baseColor, specColor, oneMinusReflectivity, oneMinusRoughness, normalWorld, -eyeVec,
			halfDir, nh, nv, lv, lh,
			nl,
			ndotlDiffuse,
		 	light,
		 	ind,
		 	specularIntensity,
		 	shadow);
	
	//	Add translucency
		half3 transLightDir = diffuseLightDir + diffuseNormalWorld * _Lux_Tanslucent_Settings.x;
		half transDot = dot( -transLightDir, viewDir );
		transDot = exp2(saturate(transDot) * power - power) * translucency;
		half shadowFactor = /*saturate(transDot) */ _Lux_Tanslucent_Settings.z * translucency;
		half3 lightScattering = transDot * light.color * lerp(shadow, 1, shadowFactor);	
		res.rgb += baseColor * lightScattering * _Lux_Tanslucent_Settings.w /* mask trans by spec */  * (1.0 - saturate(res.a));
	}

//	/////////////////////
//	Lux: Skin Lighting
	//UNITY_BRANCH
	if (isSkinMat) {
		res = LUX_SKIN_BRDF(baseColor, specColor, translucency, oneMinusReflectivity, oneMinusRoughness, normalWorld, diffuseNormalWorld, -eyeVec,
		  	halfDir, nh, nv, lv, lh,
		  	diffuseLightDir,
		  	nl,
		  	ndotlDiffuse,
		  	curvature,
		  	light, ind,
		  	specularIntensity,
		  	shadow,
		  	wpos);	
	}

//	/////////////////////
//	Lux: Anisotropic Lighting
	//UNITY_BRANCH
	if (isAnisoMat ) {
	//	low precision tangent direction per face is stored in spec buffer
		float3 worldTangentDir = DecodeOctahedronNormal( gbuffer1.rg * 2.0 - 1.0 );
		float3 worldBitangent = cross(normalWorld, worldTangentDir);
	//	get tanget per pixel and raise precision
		worldTangentDir = cross(worldBitangent, normalWorld);

		half translucency = 0;
		half metallic = Decode71(gbuffer1.b, translucency);

	//	DiffuseAndSpecularFromMetallic
		half3 origBaseColor = baseColor;
		specColor = lerp (unity_ColorSpaceDielectricSpec.rgb, baseColor, metallic);
		half oneMinusReflectivity = OneMinusReflectivityFromMetallic(metallic);
		baseColor *= oneMinusReflectivity;

	//	Direct lighting
		res = Lux_ANISO_BRDF (baseColor, specColor, oneMinusReflectivity, max(gbuffer1.b, gbuffer1.a), normalWorld, viewDir,
			halfDir, nh, nv, lv, lh,
			worldTangentDir,
			worldBitangent,
			1.0 - gbuffer1.a,
			1.0,
			nl,
			ndotlDiffuse,
			light,
			ind,
			specularIntensity,
			shadow
		);

	//	Add Translucency
		//UNITY_BRANCH
		if (translucency > 0) {
			half3 transLightDir = diffuseLightDir + diffuseNormalWorld * _Lux_Anisotropic_Settings.x;
			half transDot = dot( -transLightDir, viewDir );
			transDot = exp2(saturate(transDot) * _Lux_Anisotropic_Settings.y - _Lux_Anisotropic_Settings.y);
			half shadowFactor = saturate(transDot) * _Lux_Anisotropic_Settings.z;
			half3 lightScattering = transDot * light.color * lerp(shadow, 1, shadowFactor);
			res.rgb += lightScattering * baseColor * _Lux_Anisotropic_Settings.w * metallic;
		}	
	}
	return half4(res.rgb, 1);
}

#ifdef UNITY_HDR_ON
half4
#else
fixed4
#endif
frag (unity_v2f_deferred i) : SV_Target
{
	half4 c = CalculateLight(i);
	#ifdef UNITY_HDR_ON
	return c;
	#else
	return exp2(-c);
	#endif
}

ENDCG
}


// Pass 2: Final decode pass.
// Used only with HDR off, to decode the logarithmic buffer into the main RT
Pass {
	ZTest Always Cull Off ZWrite Off
	Stencil {
		ref [_StencilNonBackground]
		readmask [_StencilNonBackground]
		// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
		compback equal
		compfront equal
	}

CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#pragma exclude_renderers nomrt

sampler2D _LightBuffer;
struct v2f {
	float4 vertex : SV_POSITION;
	float2 texcoord : TEXCOORD0;
};

v2f vert (float4 vertex : POSITION, float2 texcoord : TEXCOORD0)
{
	v2f o;
	o.vertex = UnityObjectToClipPos(vertex);
	o.texcoord = texcoord.xy;
	#ifdef UNITY_SINGLE_PASS_STEREO
		o.texcoord = TransformStereoScreenSpaceTex(o.texcoord, 1.0f);
	#endif
	return o;
}

fixed4 frag (v2f i) : SV_Target
{
	return -log2(tex2D(_LightBuffer, i.texcoord));
}
ENDCG 
}

}
Fallback Off
}
