#ifndef LUX_UTILS_INCLUDED
#define LUX_UTILS_INCLUDED

//-------------------------------------------------------------------------------------

#define LUX_METALLIC_TO_SPECULAR \
    o.Specular = lerp (unity_ColorSpaceDielectricSpec.rgb, o.Albedo, o.Metallic); \
    half oneMinusDielectricSpec = unity_ColorSpaceDielectricSpec.a; \
    half oneMinusReflectivity = oneMinusDielectricSpec - o.Metallic * oneMinusDielectricSpec; \
    o.Albedo *= oneMinusReflectivity;

//-------------------------------------------------------------------------------------

// Some Copies from the original Standard Utils cginc - so we do not have to include it

half3 Lux_BlendNormals(half3 n1, half3 n2)
{
    return normalize(half3(n1.xy + n2.xy, n1.z*n2.z));
}

half3 Lux_UnpackScaleNormal(half2 packednormal, half bumpScale)
{
    
    half3 normal;
    normal.xy = (packednormal.xy * 2 - 1);
    #if (SHADER_TARGET >= 30)
        // SM2.0: instruction count limitation
        // SM2.0: normal scaler is not supported
        normal.xy *= bumpScale;
    #endif
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
    
}

//-------------------------------------------------------------------------------------

// Screen space dithering functions

// https://www.shadertoy.com/view/MslGR8
#define MOD3 float3(443.8975,397.2973, 491.1871)

// Input for these functions are the screen space coordinates as float2

// This function expects input p like this:
// @param p = screenPos = IN.computedScreenPos.xy * _ScreenParams.xy;
float Lux_hash12(float2 p)
{
    float3 p3  = frac(float3(p.xyx) * MOD3);
    p3 += dot(p3, p3.yzx + 19.19);
    return frac((p3.x + p3.y) * p3.z);
}

// This function expects input p like this:
// @param = screenPos = IN.computedScreenPos.xy * _ScreenParams.xy;
float3 Lux_hash32(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * MOD3);
    p3 += dot(p3, p3.yxz+19.19);
    return frac(float3((p3.x + p3.y)*p3.z, (p3.x+p3.z)*p3.y, (p3.y+p3.z)*p3.x));
}

// This function expects input p like this:
// @param = screenPos = IN.computedScreenPos.xy * _ScreenParams.xy;
float Lux_nrand(float2 p)
{
    return frac(sin(dot(p,float2(12.9898,78.233))) * 43758.5453);
}



//-------------------------------------------------------------------------------------

// Fix Unity's dynamic batching bug

#define LUX_FIX_BATCHINGBUG \
    v.normal = normalize(v.normal); \
    v.tangent = normalize(v.tangent);


#endif // LUX_UTILS_INCLUDED