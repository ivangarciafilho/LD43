#ifndef LUX_ANISO_METALLIC_PBS_LIGHTING_INCLUDED
#define LUX_ANISO_METALLIC_PBS_LIGHTING_INCLUDED

#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityLightingCommon.cginc"
#include "UnityGlobalIllumination.cginc"
#include "UnityStandardBRDF.cginc"

#include "../Lux Core/Lux Lighting/LuxAreaLights.cginc"
#include "../Lux Core/Lux BRDFs/LuxAnisoBRDF.cginc"

#include "../Lux Core/Lux Utils/LuxUtilsDeferred.cginc"

//-------------------------------------------------------------------------------------
// Default BRDF to use:
#if !defined (UNITY_BRDF_PBS) // allow to explicitly override BRDF in custom shader
	// still add safe net for low shader models, otherwise we might end up with shaders failing to compile
	// the only exception is WebGL in 5.3 - it will be built with shader target 2.0 but we want it to get rid of constraints, as it is effectively desktop
	#if SHADER_TARGET < 30 && !UNITY_53_SPECIFIC_TARGET_WEBGL
		#define UNITY_BRDF_PBS BRDF3_Unity_PBS
	#elif UNITY_PBS_USE_BRDF3
		#define UNITY_BRDF_PBS BRDF3_Unity_PBS
	#elif UNITY_PBS_USE_BRDF2
		#define UNITY_BRDF_PBS BRDF2_Unity_PBS
	#elif UNITY_PBS_USE_BRDF1
		#define UNITY_BRDF_PBS BRDF1_Unity_PBS
	#elif defined(SHADER_TARGET_SURFACE_ANALYSIS)
		// we do preprocess pass during shader analysis and we dont actually care about brdf as we need only inputs/outputs
		#define UNITY_BRDF_PBS BRDF1_Unity_PBS
	#else
		#error something broke in auto-choosing BRDF
	#endif
#endif

//-------------------------------------------------------------------------------------
// BRDF for lights extracted from *indirect* directional lightmaps (baked and realtime).
// Baked directional lightmap with *direct* light uses UNITY_BRDF_PBS.
// For better quality change to BRDF1_Unity_PBS.
// No directional lightmaps in SM2.0.

#if !defined(UNITY_BRDF_PBS_LIGHTMAP_INDIRECT)
	#define UNITY_BRDF_PBS_LIGHTMAP_INDIRECT BRDF2_Unity_PBS
#endif
#if !defined (UNITY_BRDF_GI)
	#define UNITY_BRDF_GI BRDF_Unity_Indirect
#endif

//-------------------------------------------------------------------------------------

inline half3 BRDF_Unity_Indirect (half3 baseColor, half3 specColor, half oneMinusReflectivity, half oneMinusRoughness, half3 normal, half3 viewDir, half occlusion, UnityGI gi)
{
	half3 c = 0;
	#if defined(DIRLIGHTMAP_SEPARATE)
		gi.indirect.diffuse = 0;
		gi.indirect.specular = 0;

		#ifdef LIGHTMAP_ON
			c += UNITY_BRDF_PBS_LIGHTMAP_INDIRECT (baseColor, specColor, oneMinusReflectivity, oneMinusRoughness, normal, viewDir, gi.light2, gi.indirect).rgb * occlusion;
		#endif
		#ifdef DYNAMICLIGHTMAP_ON
			c += UNITY_BRDF_PBS_LIGHTMAP_INDIRECT (baseColor, specColor, oneMinusReflectivity, oneMinusRoughness, normal, viewDir, gi.light3, gi.indirect).rgb * occlusion;
		#endif
	#endif
	return c;
}

// Horizon Occlusion for Normal Mapped Reflections: http://marmosetco.tumblr.com/post/81245981087
float GetHorizonOcclusion(float3 V, float3 normalWS, float3 vertexNormal, float horizonFade)
{
    float3 R = reflect(-V, normalWS);
    float specularOcclusion = saturate(1.0 + horizonFade * dot(R, vertexNormal));
    // smooth it
    return specularOcclusion; // * specularOcclusion;
}

//-------------------------------------------------------------------------------------

// little helpers for GI calculation

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


//-------------------------------------------------------------------------------------

// Surface shader output structure to be used with physically
// based shading model.

//-------------------------------------------------------------------------------------
// Metallic workflow

struct SurfaceOutputLuxAnisoMetallic
{
	fixed3 Albedo;			// diffuse color
	fixed3 Normal;			// tangent space normal, if written
	half3 Emission;
	half Metallic;			// 0=non-metal, 1=metal
	half Smoothness;		// 0=rough, 1=smooth
	half Occlusion;			// occlusion (default 1)
	fixed Alpha;			// alpha for transparencies
	
	fixed Shadow;
	fixed Translucency;		// as we use anisotropic lighting also for hair / it is either on (1.0) or off (0.0)
	float3 worldPosition;	// as it is needed by area lights
	fixed3 worldNormalFace;
	float3 worldTangentDir;	// as it is needed by aniso lighting
	half3 TangentDir;		// as it is needed by aniso lighting

	fixed4 SnowWorldNormal; // xyz = normal, w = blend factor

	// Shall the shader use dithering?
	#if defined(LUX_TRANSLUCENTLIGHTING)
		half dither;
	#endif
};

//half _SmoothnessX;
//half _SmoothnessY;

half4 _Lux_Anisotropic_Settings; // x: bump distortion, y: power, z: 1.0 - shadow Strength

#if !defined(LUX_STANDARD_CORE_INCLUDED)
	sampler2D _TangentDir;
	half _TangentDirStrength;
	half3 _BaseTangentDir;
	fixed _Translucency;
#endif

//	//////////////////////////////

inline half4 LightingLuxAnisoMetallic (SurfaceOutputLuxAnisoMetallic s, half3 viewDir, UnityGI gi)
{
	s.Normal = normalize(s.Normal);

	// energy conservation
	half oneMinusReflectivity;
	half3 specColor;
	s.Albedo = DiffuseAndSpecularFromMetallic (s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

// 	??? aber dann brauchen wir metallic nicht mehr als trans maske...
	fixed3 diffColor = s.Albedo;

	// shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
	// this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
	half outputAlpha;

	s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

//	///////////////////////////////////////	
//	Lux 
	half specularIntensity = 1.0;
	fixed3 diffuseNormal = s.Normal;
	half3 diffuseLightDir = 0;

	half nl = saturate(dot(s.Normal, gi.light.dir));
	half ndotlDiffuse = nl;

//	///////////////////////////////////////	
//	Lux Area lights
	#if defined(LUX_AREALIGHTS)
		// NOTE: Forward needs other inputs than deferred
		Lux_AreaLight (gi.light, specularIntensity, diffuseLightDir, ndotlDiffuse, gi.light.dir, _LightColor0.a, _WorldSpaceLightPos0.xyz, s.worldPosition, viewDir, s.Normal, diffuseNormal, 1.0 - s.Smoothness);
		nl = saturate(dot(s.Normal, gi.light.dir));
	#else
		diffuseLightDir = gi.light.dir;
		// If area lights are disabled we still have to reduce specular intensity
		#if !defined(DIRECTIONAL) && !defined(DIRECTIONAL_COOKIE)
			specularIntensity = saturate(_LightColor0.a);
		#endif
	#endif
	specularIntensity = (specColor.r == 0.0) ? 0.0 : specularIntensity;

//	Horizon Occlusion
	#if defined (UNITY_PASS_FORWARDBASE)
		#if LUX_HORIZON_OCCLUSION
			gi.indirect.specular *= GetHorizonOcclusion(viewDir, s.Normal, s.worldNormalFace, HORIZON_FADE);	
		#endif
	#endif

//	Make it fit deferred and take the per pixel normal into account
	float3 worldBitangent = cross(s.Normal, s.worldTangentDir);
	s.worldTangentDir = cross(worldBitangent, s.Normal);

//	///////////////////////////////////////	
//	Direct lighting uses the Lux BRDF
	half4 c = Lux_ANISO_BRDF (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir,
				half3(0, 0, 0), 0, 0, 0, 0,
				s.worldTangentDir,
				worldBitangent,
				1.0 - s.Smoothness,
				1.0,
				nl,
				ndotlDiffuse,
				gi.light, gi.indirect, specularIntensity, s.Shadow);
//	///////////////////////////////////////
	UNITY_BRANCH
	if (s.Translucency > 0) {
		half3 transLightDir = diffuseLightDir + diffuseNormal * _Lux_Anisotropic_Settings.x;
		//	get rid of the pow: https://seblagarde.wordpress.com/2012/06/03/spherical-gaussien-approximation-for-blinn-phong-phong-and-fresnel/
		half transDot = dot( -transLightDir, viewDir );
		transDot = exp2(saturate(transDot) * _Lux_Anisotropic_Settings.y - _Lux_Anisotropic_Settings.y);
		half shadowFactor = saturate(transDot) * _Lux_Anisotropic_Settings.z;
		half3 lightScattering = transDot * gi.light.color * lerp(s.Shadow, 1, shadowFactor);
		c.rgb += lightScattering * diffColor * _Lux_Anisotropic_Settings.w * s.Metallic;
	}
	c.rgb += UNITY_BRDF_GI (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, s.Occlusion, gi);
	c.a = outputAlpha;
	return c;
}

// ----------------------------------------------------------

inline half4 LightingLuxAnisoMetallic_Deferred (SurfaceOutputLuxAnisoMetallic s, half3 viewDir, UnityGI gi, out half4 outDiffuseOcclusion, out half4 outSpecSmoothness, out half4 outNormal)
{
	half oneMinusReflectivity;
	half3 specColor;

	half3 origAlbedo = s.Albedo;

	s.Albedo = DiffuseAndSpecularFromMetallic (s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

//	Horizon Occlusion – legacy reflections
	#if !UNITY_ENABLE_REFLECTION_BUFFERS
		#if LUX_HORIZON_OCCLUSION
			gi.indirect.specular *= GetHorizonOcclusion(viewDir, s.Normal, s.worldNormalFace, HORIZON_FADE);
		#endif
	#endif

	half4 c = UNITY_BRDF_PBS (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
	c.rgb += UNITY_BRDF_GI (s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, s.Occlusion, gi);

//	Horizon Occlusion – deferred reflections
	#if UNITY_ENABLE_REFLECTION_BUFFERS
		#if LUX_HORIZON_OCCLUSION
			s.Occlusion *= GetHorizonOcclusion(viewDir, s.Normal, s.worldNormalFace, HORIZON_FADE);
		#endif
	#endif

//	Write original Albedo and Occlusion
	outDiffuseOcclusion = half4(origAlbedo, s.Occlusion);
//	Write Tangent Dir, Metallic combined with Translucency and Smoothness
//	Dither
	#if defined(LUX_TRANSLUCENTLIGHTING)
		half2 encWTDir = EncodeOctahedronNormal(s.worldTangentDir);
		// encWTDir += (frac(encWTDir * 128) / 128) * s.dither;
		// encWTDir *= lerp(half2(0.985,0.985), half2(1,1), s.dither);
		encWTDir *= s.dither;
		// saturate(s.Translucency) is needed by dx9 to handle uint
		outSpecSmoothness = half4( encWTDir, Encode71(s.Metallic, saturate(s.Translucency)), s.Smoothness);
	#else
		// saturate(s.Translucency) is needed by dx9 to handle uint
		outSpecSmoothness = half4( EncodeOctahedronNormal(s.worldTangentDir), Encode71(s.Metallic, saturate(s.Translucency)), s.Smoothness);
	#endif
//	Normal and Material ID
	outNormal = half4(s.Normal * 0.5 + 0.5, 0.33);
	half4 emission = half4(s.Emission + c.rgb, 1);
	return emission;
}


inline void LightingLuxAnisoMetallic_GI (
	SurfaceOutputLuxAnisoMetallic s,
	UnityGIInput data,
	inout UnityGI gi)
{
	#if defined(UNITY_PASS_DEFERRED) && UNITY_ENABLE_REFLECTION_BUFFERS
	    gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal);
	#else
	    Unity_GlossyEnvironmentData g;
	//	To simulate the streching of highlight at grazing angle for IBL we shrink the roughness which allow to fake an anisotropic specular lobe.
	//	Ref: http://www.frostbite.com/2015/08/stochastic-screen-space-reflections/ - slide 84
		float roughness = 1.0f - s.Smoothness;
		s.Smoothness = 1.0 - lerp(roughness, 1.0, saturate(dot(s.Normal, -data.worldViewDir) * 2.0) );
	    g.roughness	= SmoothnessToPerceptualRoughness(s.Smoothness);									
		fixed3 bitangentWS = cross(s.Normal, s.worldTangentDir);			
		float3 anisoNormal = GetAnisotropicModifiedNormal(bitangentWS, s.Normal, data.worldViewDir, 0.5f ); 
		g.reflUVW = reflect( -data.worldViewDir, anisoNormal); 		
	    gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal, g);
	#endif
}

#endif