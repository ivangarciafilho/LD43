// Collects cascaded shadows into screen space buffer
Shader "Hidden/NGSS_Directional" {
Properties {
    _ShadowMapTexture ("", any) = "" {}
    _ODSWorldTexture("", 2D) = "" {}
}

CGINCLUDE

UNITY_DECLARE_SHADOWMAP(_ShadowMapTexture);
#ifndef SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED
#define SHADOWMAPSAMPLER_AND_TEXELSIZE_DEFINED
float4 _ShadowMapTexture_TexelSize;		
#endif
sampler2D _ODSWorldTexture;

#include "UnityCG.cginc"
#include "UnityShadowLibrary.cginc"

// Configuration

// Should receiver plane bias be used? This estimates receiver slope using derivatives,
// and tries to tilt the PCF kernel along it. However, since we're doing it in screenspace
// from the depth texture, the derivatives are wrong on edges or intersections of objects,
// leading to possible shadow artifacts. So it's disabled by default.
// See also UnityGetReceiverPlaneDepthBias in UnityShadowLibrary.cginc.
//#define UNITY_USE_RECEIVER_PLANE_BIAS

struct appdata {
    float4 vertex : POSITION;
    float2 texcoord : TEXCOORD0;
#ifdef UNITY_STEREO_INSTANCING_ENABLED
    float3 ray0 : TEXCOORD1;
    float3 ray1 : TEXCOORD2;
#else
    float3 ray : TEXCOORD1;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f {

    float4 pos : SV_POSITION;

    // xy uv / zw screenpos
    float4 uv : TEXCOORD0;
    // View space ray, for perspective case
    float3 ray : TEXCOORD1;
    // Orthographic view space positions (need xy as well for oblique matrices)
    float3 orthoPosNear : TEXCOORD2;
    float3 orthoPosFar  : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert (appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float4 clipPos;
#if defined(STEREO_CUBEMAP_RENDER_ON)
    clipPos = mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, v.vertex));
#else
    clipPos = UnityObjectToClipPos(v.vertex);
#endif
    o.pos = clipPos;
    o.uv.xy = v.texcoord;

    // unity_CameraInvProjection at the PS level.
    o.uv.zw = ComputeNonStereoScreenPos(clipPos);

    // Perspective case
#ifdef UNITY_STEREO_INSTANCING_ENABLED
    o.ray = unity_StereoEyeIndex == 0 ? v.ray0 : v.ray1;
#else
    o.ray = v.ray;
#endif

    // To compute view space position from Z buffer for orthographic case,
    // we need different code than for perspective case. We want to avoid
    // doing matrix multiply in the pixel shader: less operations, and less
    // constant registers used. Particularly with constant registers, having
    // unity_CameraInvProjection in the pixel shader would push the PS over SM2.0
    // limits.
    clipPos.y *= _ProjectionParams.x;
    float3 orthoPosNear = mul(unity_CameraInvProjection, float4(clipPos.x,clipPos.y,-1,1)).xyz;
    float3 orthoPosFar  = mul(unity_CameraInvProjection, float4(clipPos.x,clipPos.y, 1,1)).xyz;
    orthoPosNear.z *= -1;
    orthoPosFar.z *= -1;
    o.orthoPosNear = orthoPosNear;
    o.orthoPosFar = orthoPosFar;

    return o;
}

// ------------------------------------------------------------------
//  Helpers
// ------------------------------------------------------------------
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
// sizes of cascade projections, relative to first one
float4 unity_ShadowCascadeScales;

//
// Keywords based defines
//
#if defined (SHADOWS_SPLIT_SPHERES)
    #define GET_CASCADE_WEIGHTS(wpos, z, dsqr)	getCascadeWeights_splitSpheres(wpos, dsqr) 
#else
    #define GET_CASCADE_WEIGHTS(wpos, z, dsqr)    getCascadeWeights( wpos, z )
#endif

#if defined (SHADOWS_SINGLE_CASCADE)
    #define GET_SHADOW_COORDINATES(wpos,cascadeWeights) getShadowCoord_SingleCascade(wpos)
#else
    #define GET_SHADOW_COORDINATES(wpos,cascadeWeights) getShadowCoord(wpos,cascadeWeights)
#endif

/**
 * Gets the cascade weights based on the world position of the fragment.
 * Returns a float4 with only one component set that corresponds to the appropriate cascade.
 */
inline fixed4 getCascadeWeights(float3 wpos, float z)
{
    fixed4 zNear = float4( z >= _LightSplitsNear );
    fixed4 zFar = float4( z < _LightSplitsFar );
    fixed4 weights = zNear * zFar;
    return weights;
}

/**
 * Gets the cascade weights based on the world position of the fragment and the poisitions of the split spheres for each cascade.
 * Returns a float4 with only one component set that corresponds to the appropriate cascade.
 */
inline fixed4 getCascadeWeights_splitSpheres(float3 wpos, out float4 dsqr)
{
    float3 fromCenter0 = wpos.xyz - unity_ShadowSplitSpheres[0].xyz;
    float3 fromCenter1 = wpos.xyz - unity_ShadowSplitSpheres[1].xyz;
    float3 fromCenter2 = wpos.xyz - unity_ShadowSplitSpheres[2].xyz;
    float3 fromCenter3 = wpos.xyz - unity_ShadowSplitSpheres[3].xyz;
    dsqr = float4(dot(fromCenter0,fromCenter0), dot(fromCenter1,fromCenter1), dot(fromCenter2,fromCenter2), dot(fromCenter3,fromCenter3));
	fixed4 weights = float4(dsqr < unity_ShadowSplitSqRadii);
	//dsqr = float4(length(fromCenter0), length(fromCenter1), length(fromCenter2), length(fromCenter3));
    //fixed4 weights = float4(dsqr < sqrt(unity_ShadowSplitSqRadii));
    weights.yzw = saturate(weights.yzw - weights.xyz);
    return weights;
}

/**
 * Returns the shadowmap coordinates for the given fragment based on the world position and z-depth.
 * These coordinates belong to the shadowmap atlas that contains the maps for all cascades.
 */
inline float4 getShadowCoord( float4 wpos, fixed4 cascadeWeights )
{
    float3 sc0 = mul (unity_WorldToShadow[0], wpos).xyz;
    float3 sc1 = mul (unity_WorldToShadow[1], wpos).xyz;
    float3 sc2 = mul (unity_WorldToShadow[2], wpos).xyz;
    float3 sc3 = mul (unity_WorldToShadow[3], wpos).xyz;
	
    float4 shadowMapCoordinate = float4((sc0 * cascadeWeights.x) + (sc1 * cascadeWeights.y) + (sc2 * cascadeWeights.z) + (sc3 * cascadeWeights.w), 1);
#if defined(UNITY_REVERSED_Z)
    float  noCascadeWeights = 1 - dot(cascadeWeights, float4(1, 1, 1, 1));
    shadowMapCoordinate.z += noCascadeWeights;
#endif
    return shadowMapCoordinate;
}

inline float4 getShadowCoordFinal( float3 sc0, float3 sc1, float3 sc2, float3 sc3, fixed4 cascadeWeights )
{
	//float3 sc0 = mul (unity_WorldToShadow[0], wpos).xyz;
    //float3 sc1 = mul (unity_WorldToShadow[1], wpos).xyz;
    //float3 sc2 = mul (unity_WorldToShadow[2], wpos).xyz;
    //float3 sc3 = mul (unity_WorldToShadow[3], wpos).xyz;
	
    float4 shadowMapCoordinate = float4((sc0 * cascadeWeights.x) + (sc1 * cascadeWeights.y) + (sc2 * cascadeWeights.z) + (sc3 * cascadeWeights.w), 1);
#if defined(UNITY_REVERSED_Z)
    float  noCascadeWeights = 1 - dot(cascadeWeights, float4(1, 1, 1, 1));
    shadowMapCoordinate.z += noCascadeWeights;
#endif
    return shadowMapCoordinate;
}

/**
 * Same as the getShadowCoord; but optimized for single cascade
 */
inline float4 getShadowCoord_SingleCascade( float4 wpos )
{
    return float4( mul (unity_WorldToShadow[0], wpos).xyz, 0);
}

/**
* Get camera space coord from depth and inv projection matrices
*/
inline float3 computeCameraSpacePosFromDepthAndInvProjMat(v2f i)
{
    float zdepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);

    #if defined(UNITY_REVERSED_Z)
        zdepth = 1 - zdepth;
    #endif

    // View position calculation for oblique clipped projection case.
    // this will not be as precise nor as fast as the other method
    // (which computes it from interpolated ray & depth) but will work
    // with funky projections.
    float4 clipPos = float4(i.uv.zw, zdepth, 1.0);
    clipPos.xyz = 2.0f * clipPos.xyz - 1.0f;
    float4 camPos = mul(unity_CameraInvProjection, clipPos);
    camPos.xyz /= camPos.w;
    camPos.z *= -1;
    return camPos.xyz;
}

/**
* Get camera space coord from depth and info from VS
*/
inline float3 computeCameraSpacePosFromDepthAndVSInfo(v2f i)
{
    float zdepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);

    // 0..1 linear depth, 0 at camera, 1 at far plane.
    float depth = lerp(Linear01Depth(zdepth), zdepth, unity_OrthoParams.w);
#if defined(UNITY_REVERSED_Z)
    zdepth = 1 - zdepth;
#endif

    // view position calculation for perspective & ortho cases
    float3 vposPersp = i.ray * depth;
    float3 vposOrtho = lerp(i.orthoPosNear, i.orthoPosFar, zdepth);
    // pick the perspective or ortho position as needed
    float3 camPos = lerp(vposPersp, vposOrtho, unity_OrthoParams.w);
    return camPos.xyz;
}

inline float3 computeCameraSpacePosFromDepth(v2f i);

//NGSS START--------------------------------------------------------------------------------------------------------------------------------------------------------------

// Note: Do not alter any of these or you risk shader compile errors

uniform float4 NGSS_CASCADES_SPLITS;
uniform float NGSS_CASCADES_COUNT = 4;
uniform float NGSS_CASCADE_BLEND_DISTANCE = 0.25;
uniform float NGSS_CASCADES_SOFTNESS_NORMALIZATION = 1;

uniform float NGSS_BANDING_TO_NOISE_RATIO_DIR = 1;

uniform float NGSS_FILTER_SAMPLERS_DIR = 32;
uniform float NGSS_TEST_SAMPLERS_DIR = 16;

//INLINE SAMPLING
#if (SHADER_TARGET < 30  || UNITY_VERSION <= 570 || defined(SHADER_API_D3D9) || defined(SHADER_API_GLES) || defined(SHADER_API_PSP2) || defined(SHADER_API_N3DS) || defined(SHADER_API_GLCORE))
	//#define NO_INLINE_SAMPLERS_SUPPORT
#elif defined(NGSS_PCSS_FILTER_DIR)
	#define NGSS_CAN_USE_PCSS_FILTER_DIR
	SamplerState my_point_clamp_smp2;
#endif

//uniform float NGSS_RECEIVER_PLANE_MIN_FRACTIONAL_ERROR = 0.01;

uniform float NGSS_GLOBAL_SOFTNESS = 0.01;
uniform float NGSS_PCSS_FILTER_DIR_MIN = 0.05;
uniform float NGSS_PCSS_FILTER_DIR_MAX = 0.25;
uniform float NGSS_BIAS_FADE_DIR = 0.001;

float2 VogelDiskSampleDir(int sampleIndex, int samplesCount, float phi)
{
	//float phi = 3.14159265359f;//UNITY_PI;
	float GoldenAngle = 2.4f;

	float r = sqrt(sampleIndex + 0.5f) / sqrt(samplesCount);
	float theta = sampleIndex * GoldenAngle + phi;

	float sine, cosine;
	sincos(theta, sine, cosine);
	
	return float2(r * cosine, r * sine);
}

float InterleavedGradientNoiseDir(float2 position_screen)
{
	float2 magic = position_screen * 10;
    return lerp(0, frac(sin(dot(magic, magic)) * 43758.5453f), NGSS_BANDING_TO_NOISE_RATIO_DIR) * UNITY_TWO_PI;
}


/********************************************************************************/

#if defined(NGSS_CAN_USE_PCSS_FILTER_DIR)
//BlockerSearch
float2 BlockerSearch(float2 uv, float receiver, float searchUV, float Sampler_Number, float randPied, float2 screenpos, uint cascadeIndex)
{
	float avgBlockerDepth = 0.0;
	float numBlockers = 0.0;
	float blockerSum = 0.0;

	int samplers = Sampler_Number;// / (cascadeIndex / 2);
	UNITY_LOOP
	for (int i = 0; i < samplers; i++)
	{
		float2 offset = VogelDiskSampleDir(i, samplers, randPied) * searchUV;
		float2 uvs = uv + offset;
		
		#if !defined(SHADOWS_SINGLE_CASCADE)
		uvs = clamp(uvs, 0, cascadeIndex / NGSS_CASCADES_COUNT * 4 * 0.499);//make sure we are not sampling outside the current cascade
		#endif
		float shadowMapDepth = _ShadowMapTexture.SampleLevel(my_point_clamp_smp2, uvs, 0);
		
		#if defined(UNITY_REVERSED_Z)
		blockerSum += shadowMapDepth >= receiver ? shadowMapDepth : 0;
		numBlockers += shadowMapDepth >= receiver ? 1.0 : 0.0;
		#else
		blockerSum += shadowMapDepth <= receiver ? shadowMapDepth : 0;
		numBlockers += shadowMapDepth <= receiver ? 1.0 : 0.0;
		#endif
	}

	avgBlockerDepth = blockerSum / numBlockers;

#if defined(UNITY_REVERSED_Z)
	avgBlockerDepth = 1.0 - avgBlockerDepth;
#endif

	return float2(avgBlockerDepth, numBlockers);
}
#endif//NGSS_CAN_USE_PCSS_FILTER_DIR

//PCF
float PCF_FilterDir(float2 uv, float receiver, float diskRadius, float Sampler_Number, float randPied, float2 screenpos, uint cascadeIndex)
{
	float sum = 0.0f;
	//if(cascadeIndex == 4)
		//return 0;
	int samplers = Sampler_Number;// / (cascadeIndex / 2);
	UNITY_LOOP
	for (int i = 0; i < samplers; i++)
	{
		float2 offset = VogelDiskSampleDir(i, samplers, randPied) * diskRadius;		
		float depthBiased = receiver;
		float2 uvs = uv + offset;
		
		#if !defined(SHADOWS_SINGLE_CASCADE)
		uvs = clamp(uvs, 0, cascadeIndex / NGSS_CASCADES_COUNT * 4 * 0.499);//make sure we are not sampling outside the current cascade
		#endif
		float value = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, float4(uvs, depthBiased, 0.0));
		
		sum += value;
	}

	return sum / samplers;
}

//Main Function
float NGSS_Main(float4 coord, float2 screenpos, uint cascadeIndex)
{
	float randPied = InterleavedGradientNoiseDir(screenpos);
	float shadowSoftness = NGSS_GLOBAL_SOFTNESS;//NGSS_GLOBAL_SOFTNESS default value 100

	float2 uv = coord.xy;//clamp(coord.xy, 0, clamp(coord.xy, 0, cascadeIndex / NGSS_CASCADES_COUNT * 4 * 0.499 - NGSS_GLOBAL_SOFTNESS * 0.199));
	float receiver = coord.z;
	float shadow = 1.0;
	float diskRadius = 0.01;
	
#if !defined(SHADOWS_SINGLE_CASCADE)
//reduce the softness of consecutive cascades (stiching)
#if defined(NGSS_CAN_USE_PCSS_FILTER_DIR)
	shadowSoftness /= lerp(cascadeIndex, clamp(NGSS_CASCADES_SPLITS[cascadeIndex - 1] / 0.1, 0, 3), NGSS_CASCADES_SOFTNESS_NORMALIZATION);//clamping to 4 will soft all cascades but its too hard on PCSS so skip 4th cascade
#else
	shadowSoftness /= lerp(cascadeIndex, NGSS_CASCADES_SPLITS[cascadeIndex - 1] / 0.1, NGSS_CASCADES_SOFTNESS_NORMALIZATION);
#endif
#endif//SHADOWS_SINGLE_CASCADE

#if defined(NGSS_CAN_USE_PCSS_FILTER_DIR)
	
	float2 blockerResults = BlockerSearch(uv, receiver, shadowSoftness * 0.25, NGSS_TEST_SAMPLERS_DIR, randPied, screenpos, cascadeIndex);

	if (blockerResults.y == 0.0)//There are no occluders so early out (this saves filtering)
		return 1.0;

#if defined(UNITY_REVERSED_Z)
	float penumbra = ((1.0 - receiver) - blockerResults.x);
#else
	float penumbra = (receiver - blockerResults.x);
#endif
	
	diskRadius = clamp(penumbra, NGSS_PCSS_FILTER_DIR_MIN, NGSS_PCSS_FILTER_DIR_MAX) * shadowSoftness;
#else//NO PCSS FILTERING
	diskRadius = shadowSoftness * 0.25;
#endif//NGSS_CAN_USE_PCSS_FILTER_DIR
	 
	shadow = PCF_FilterDir(uv, receiver, diskRadius, NGSS_TEST_SAMPLERS_DIR, randPied, screenpos, cascadeIndex);
	if (shadow == 1.0)//If all pixels are lit early bail out
		return 1.0;

	//float Sampler_Number = (int)clamp(Sampler_Number * (diskRadius / NGSS_GLOBAL_SOFTNESS), Sampler_Number * 0.5, Sampler_Number);
	shadow = PCF_FilterDir(uv, receiver, diskRadius, NGSS_FILTER_SAMPLERS_DIR, randPied, screenpos, cascadeIndex);	
	return shadow;	
}

//Hard shadow
fixed4 frag_hard (v2f i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); // required for sampling the correct slice of the shadow map render texture array
    float4 wpos;
    float3 vpos;

#if defined(STEREO_CUBEMAP_RENDER_ON)
    wpos.xyz = tex2D(_ODSWorldTexture, i.uv.xy).xyz;
    wpos.w = 1.0f;
    vpos = mul(unity_WorldToCamera, wpos).xyz;
#else
    vpos = computeCameraSpacePosFromDepth(i);
    wpos = mul (unity_CameraToWorld, float4(vpos,1));
#endif
    float4 dsqr;
    fixed4 cascadeWeights = GET_CASCADE_WEIGHTS(wpos, vpos.z, dsqr);
	//fixed4 cascadeWeights = GET_CASCADE_WEIGHTS (wpos, vpos.z);
    float4 coord = GET_SHADOW_COORDINATES(wpos, cascadeWeights);

    //1 tap hard shadow
    fixed shadow = UNITY_SAMPLE_SHADOW(_ShadowMapTexture, coord);
    //shadow = lerp(_LightShadowData.r, 1.0, shadow);
	shadow += _LightShadowData.r;

    fixed4 res = shadow;
    return res;
}

//Soft shadow
fixed4 frag_pcfSoft(v2f i) : SV_Target
{
#if defined(NGSS_HARD_SHADOWS_DIR)
	//Hard shadows from soft shadows? muhahaha ^^
	return frag_hard(i);
#endif
	
	//Return one if you want only ContactShadows, keep in mind that the cascaded depth are still rendered
	//return 1.0;
	
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); // required for sampling the correct slice of the shadow map render texture array
    float4 wpos;
    float3 vpos;

#if defined(STEREO_CUBEMAP_RENDER_ON)
    wpos.xyz = tex2D(_ODSWorldTexture, i.uv.xy).xyz;
    wpos.w = 1.0f;
    vpos = mul(unity_WorldToCamera, wpos).xyz;
#else
    vpos = computeCameraSpacePosFromDepth(i);

    // sample the cascade the pixel belongs to
    wpos = mul(unity_CameraToWorld, float4(vpos,1));
#endif
	
	float4 dsqr;
    fixed4 cascadeWeights = GET_CASCADE_WEIGHTS(wpos, vpos.z, dsqr);
	//fixed4 cascadeWeights = GET_CASCADE_WEIGHTS(wpos, vpos.z);//linear
    //float4 coord = GET_SHADOW_COORDINATES(wpos, cascadeWeights);
	float3 sc0, sc1, sc2, sc3;
#if defined (SHADOWS_SINGLE_CASCADE)
    float4 coord = getShadowCoord_SingleCascade(wpos);
#else
	sc0 = mul (unity_WorldToShadow[0], wpos).xyz;
    sc1 = mul (unity_WorldToShadow[1], wpos).xyz;
    sc2 = mul (unity_WorldToShadow[2], wpos).xyz;
    sc3 = mul (unity_WorldToShadow[3], wpos).xyz;	
    float4 coord = getShadowCoordFinal(sc0, sc1, sc2, sc3, cascadeWeights);
#endif
	
    float3 receiverPlaneDepthBias = 0.0;
	
	fixed cascadeIndex = 1;
#if !defined(SHADOWS_SINGLE_CASCADE)
	cascadeIndex += 4 - dot(cascadeWeights, half4(4, 3, 2, 1));
#endif

	//float shadow = UnitySampleShadowmap_PCF7x7Tent(coord, receiverPlaneDepthBias);
	float shadow = NGSS_Main(coord, i.uv.xy, cascadeIndex);

	// Blend between shadow cascades if enabled. No need when 1 cascade
#if defined(NGSS_USE_CASCADE_BLENDING) && !defined(SHADOWS_SINGLE_CASCADE)
	
#if defined(SHADOWS_SPLIT_SPHERES)

	float3 wdir = wpos - _WorldSpaceCameraPos;
    half4 z4 = dsqr / unity_ShadowSplitSqRadii.xyzw;
    z4 = ( unity_ShadowSplitSqRadii > 0 ) ? z4.xyzw : ( 0 ).xxxx;
    z4 *= dsqr < dot( wdir, wdir ).xxxx;	
#else
	half4 z4 = (float4(vpos.z, vpos.z, vpos.z, vpos.z) - _LightSplitsNear) / (_LightSplitsFar - _LightSplitsNear);	
#endif

	half alpha = saturate(dot(z4 * cascadeWeights, half4(1, 1, 1, 1)));
	//alpha = saturate(alpha);
		
	UNITY_BRANCH
	if (alpha > 1.0 - NGSS_CASCADE_BLEND_DISTANCE)
	{
		// get alpha to 0..1 range over the blend distance
		alpha = (alpha - (1.0 - NGSS_CASCADE_BLEND_DISTANCE)) / NGSS_CASCADE_BLEND_DISTANCE;
		
		// sample next cascade
		cascadeWeights = fixed4(0, cascadeWeights.xyz);
		//coord = GET_SHADOW_COORDINATES(wpos, cascadeWeights);
		coord = getShadowCoordFinal(sc0, sc1, sc2, sc3, cascadeWeights);

		//half shadowNextCascade = UnitySampleShadowmap_PCF3x3(coord, receiverPlaneDepthBias);
		half shadowNextCascade = NGSS_Main(coord, i.uv.xy, cascadeIndex + 1);
		
		//shadow = lerp(shadow, min(shadow, shadowNextCascade), alpha);//saturate(alpha)
		shadow = lerp(shadow, shadowNextCascade, alpha);//saturate(alpha)
	}
	
#endif
	
	//return lerp(_LightShadowData.r, 1.0, shadow);
	return shadow + _LightShadowData.r;
}
ENDCG

// ----------------------------------------------------------------------------------------
// Subshaders that does NGSS filterings while collecting shadows.
// Compatible with: DX11, DX12, GLCORE, PS4, XB1, GLES3.0, SWITCH, Metal, Vulkan and equivalent SM3.0+ APIs

//SM 3.0
Subshader
{
	Tags {"ShadowmapFilter" = "PCF_SOFT"}//Unity 2017
	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_pcfSoft
		#pragma shader_feature NGSS_PCSS_FILTER_DIR
		#pragma shader_feature NGSS_USE_CASCADE_BLENDING
		#pragma shader_feature NGSS_HARD_SHADOWS_DIR
		#pragma exclude_renderers gles d3d9
		#pragma multi_compile_shadowcollector
		#pragma target 3.0

		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndVSInfo(i);
		}
		ENDCG
	}
}
// This version does inv projection at the PS level, slower and less precise however more general.
Subshader
{
	Tags{ "ShadowmapFilter" = "PCF_SOFT_FORCE_INV_PROJECTION_IN_PS" }//Unity 2017
	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_pcfSoft
		#pragma shader_feature NGSS_PCSS_FILTER_DIR
		#pragma shader_feature NGSS_USE_CASCADE_BLENDING
		#pragma shader_feature NGSS_HARD_SHADOWS_DIR
		#pragma exclude_renderers gles d3d9
		#pragma multi_compile_shadowcollector
		#pragma target 3.0

		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndInvProjMat(i);
		}
		ENDCG
	}
}/*
//SM 2.0
SubShader
{
	Tags { "ShadowmapFilter" = "HardShadow" }
	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_hard
		#pragma multi_compile_shadowcollector
		#pragma exclude_renderers gles d3d9
		
		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndVSInfo(i);
		}
		ENDCG
	}
}
// This version does inv projection at the PS level, slower and less precise however more general.
SubShader
{
	Tags { "ShadowmapFilter" = "HardShadow_FORCE_INV_PROJECTION_IN_PS" }
	Pass
	{
		ZWrite Off ZTest Always Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag_hard
		#pragma multi_compile_shadowcollector
		#pragma exclude_renderers gles d3d9
		
		inline float3 computeCameraSpacePosFromDepth(v2f i)
		{
			return computeCameraSpacePosFromDepthAndInvProjMat(i);
		}
		ENDCG
	}
}*/
Fallback Off
}