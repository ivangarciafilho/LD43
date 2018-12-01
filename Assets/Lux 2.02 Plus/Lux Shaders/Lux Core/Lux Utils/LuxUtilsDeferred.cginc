#ifndef LUX_UTILS_DEFERRED_INCLUDED
#define LUX_UTILS_DEFERRED_INCLUDED


//-------------------------------------------------------------------------------------

// Octahedron encoded normals
// https://knarkowicz.wordpress.com/2014/04/16/octahedron-normal-vector-encoding/
// http://jcgt.org/published/0003/02/01/paper.pdf

// Old functions prior Unity 2018 which break on OpenGLCore and Metal
/*
float2 EncodeOctahedronNormal(float3 n)
{
    float nrm = abs(n.x) + abs(n.y) + abs(n.z);
    float2 res = n.xy * (1.0 / nrm);
    float2 encN = 1.0 - abs(res.yx);
    encN = (n.zz < float2(0.0, 0.0) ? (res >= 0.0 ? encN : -encN) : res);
    return encN * 0.5 + 0.5;
}

float3 DecodeOctahedronNormal(float2 encN)
{
    //encN = encN * 2.0 - 1.0;
    float3 n = encN.xyy;
    n.z = 1.0 - abs(n.x) - abs(n.y);
    n.xy = (n.z >= 0.0) ? n.xy : ( (1.0 - abs( n.yx) ) * sign(n.xy) );
    return normalize(n);
}
*/

#define FLT_EPS  5.960464478e-8 

// Computes (FastSign(s) * x) using 2x VALU.
// See the comment about FastSign() below.
float FastMulBySignOf(float s, float x, bool ignoreNegZero = true)
{
#if !defined(SHADER_API_GLES)
    if (ignoreNegZero)
    {
        return (s >= 0) ? x : -x;
    }
    else
    {
        uint negZero = 0x80000000u;
        uint signBit = negZero & asuint(s);
        return asfloat(signBit ^ asuint(x));
    }
#else
    return (s >= 0) ? x : -x;
#endif
}

// Returns -1 for negative numbers and 1 for positive numbers.
// 0 can be handled in 2 different ways.
// The IEEE floating point standard defines 0 as signed: +0 and -0.
// However, mathematics typically treats 0 as unsigned.
// Therefore, we treat -0 as +0 by default: FastSign(+0) = FastSign(-0) = 1.
// If (ignoreNegZero = false), FastSign(-0, false) = -1.
// Note that the sign() function in HLSL implements signum, which returns 0 for 0.
float FastSign(float s, bool ignoreNegZero = true)
{
    return FastMulBySignOf(s, 1.0, ignoreNegZero);
}

float2 EncodeOctahedronNormal(float3 n)
{
    float nrm = abs(n.x) + abs(n.y) + abs(n.z);
    float2 res = n.xy * (1.0 / nrm);
    float2 encN = 1.0 - abs(res.yx);
//  This does not get compiled out properly in Unity 2018 on OpenGLCore and Metal
//  encN = (n.z < 0.0 ? (res >= float2(0.0, 0.0) ? encN : -encN) : res);
//  So we manually decompose the line:

//  This version saves 1 instruction on dx11
    float2 checkRes = res >= float2(0.0, 0.0);
    checkRes = checkRes * 2 - 1;
    encN *= checkRes;
    encN = (n.z < 0.0) ? encN : res;
    return encN * 0.5 + 0.5;
}

float3 DecodeOctahedronNormal(float2 encN)
{
    float3 n = encN.xyy;
    n.z = 1.0 - abs(n.x) - abs(n.y);
//  n.z = max(1.0 - abs(n.x) - abs(n.y), FLT_EPS); // EPS is absolutely crucial for anisotropy
    //n.xy = (n.z >= 0.0) ? n.xy : ( (1.0 - abs( n.yx) ) * sign(n.xy) );
//  Let's use FastSign
    float2 SignN;
    SignN.x = FastSign(n.x);
    SignN.y = FastSign(n.y);
    n.xy = (n.z >= 0.0) ? n.xy : ( (1.0 - abs( n.yx) ) * SignN );
    return normalize(n);
}

float Encode71(float Scalar, uint Mask)
{
    return 127.0f / 255.0f * saturate(Scalar) + 128.0f / 255.0f * Mask;
}

float Decode71(float Scalar, out uint Mask)
{
    Mask = (int)(Scalar > 0.5f);
    return (Scalar - 0.5f * Mask) * 2.0f;
}


//-------------------------------------------------------------------------------------

//  Lux Helper Functions for Cinemetic Image Effects: Screen space Reflections

half GetLuxMaterialIndexNormal(half input) {
    // return floor(input * 3 + 0.5f);
    // For now we can get away rather cheaply...
    return input;
}

half GetLuxMaterialIndexFull(half input) {
    return floor(input * 3 + 0.5f);
}

float3 GetLuxNormal (float4 normSample, half materialIndex, float2 uv) {
    // regular
    float3 normal = normSample.rgb * 2 - 1;
    // skin
    if (materialIndex == 0) {
        normal = DecodeOctahedronNormal(normal.xy); 
    }
    return normal;
}

float3 GetLuxSpecular (float3 specSample, float4 albedoAlphaSample, float metallic, half materialIndex) {
    // skin 
    if (materialIndex == 0) {
        return specSample.rrr; 
    }
    // aniso
    else if (materialIndex == 1.0) {
        metallic = frac(metallic * 1.9); //99);
        return lerp (unity_ColorSpaceDielectricSpec.rgb, albedoAlphaSample.rgb, metallic);
    }
    // regular
    else {
        return specSample;
    }
}

//-------------------------------------------------------------------------------------

#endif //LUX_UTILS_DEFERRED_INCLUDED
