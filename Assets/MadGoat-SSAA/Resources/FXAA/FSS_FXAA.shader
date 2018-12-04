Shader "Hidden/SSAA/FSS"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
	}

		CGINCLUDE
#include "UnityCG.cginc"
#pragma fragmentoption ARB_precision_hint_fastest

#if defined(SHADER_API_PS3)
#define FXAA_PS3 1

		// Shaves off 2 cycles from the shader
#define FXAA_EARLY_EXIT 0
#elif defined(SHADER_API_XBOX360)
#define FXAA_360 1

		// Shaves off 10ms from the shader's execution time
#define FXAA_EARLY_EXIT 1
#else
#define FXAA_PC 1
#endif

#define FXAA_HLSL_3 1
#define FXAA_QUALITY__PRESET 39

#define FXAA_GREEN_AS_LUMA 1

#pragma target 3.0
#include "FSS_FXAA3.hlsl"
        sampler2D _MainTex;
		float4 _MainTex_ST;
        float4 _MainTex_TexelSize;

        float3 _QualitySettings;
        float4 _ConsoleSettings;
		float _Intensity;
        struct Input
        {
            float4 position : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varying
        {
            float4 position : SV_POSITION;
            float2 uv : TEXCOORD0;
			float2 uvSPR : TEXCOORD1; // Single Pass Stereo UVs
        };

        Varying vert(Input input)
        {
            Varying o;
			o.position = UnityObjectToClipPos(input.position);
			o.uv = input.uv.xy;
			o.uvSPR = UnityStereoScreenSpaceUVAdjust(input.position.xy, _MainTex_ST);
			return o;
        }

        float calculateLuma(float4 color)
        {
            return color.g * 1.963211 + color.r;
        }

        fixed4 fragment(Varying input) : SV_Target
        {
            const float4 consoleUV = input.uv.xyxy + .5 * float4(-_MainTex_TexelSize.xy, _MainTex_TexelSize.xy);
            const float4 consoleSubpixelFrame = _ConsoleSettings.x * float4(-1., -1., 1., 1.) *
                _MainTex_TexelSize.xyxy;

            const float4 consoleSubpixelFramePS3 = float4(-2., -2., 2., 2.) * _MainTex_TexelSize.xyxy;
            const float4 consoleSubpixelFrameXBOX = float4(8., 8., -4., -4.) * _MainTex_TexelSize.xyxy;

            #if defined(SHADER_API_XBOX360)
                const float4 consoleConstants = float4(1., -1., .25, -.25);
            #else
                const float4 consoleConstants = float4(0., 0., 0., 0.);
            #endif
				float4 main = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(input.uv, _MainTex_ST)).rgba;
				half4 color = FxaaPixelShader(
                UnityStereoScreenSpaceUVAdjust(input.uv, _MainTex_ST),
                UnityStereoScreenSpaceUVAdjust(consoleUV, _MainTex_ST),
                _MainTex, _MainTex, _MainTex, _MainTex_TexelSize.xy,
                consoleSubpixelFrame, consoleSubpixelFramePS3, consoleSubpixelFrameXBOX,
                _QualitySettings.x, _QualitySettings.y, _QualitySettings.z,
                _ConsoleSettings.y, _ConsoleSettings.z, _ConsoleSettings.w, consoleConstants);

				float4 final =  lerp(main , color ,_Intensity);
				return float4(final.r, final.g, final.b, main.a);
        }
	
		
    ENDCG

    SubShader
    {
        ZTest Always Cull Off ZWrite Off
        Fog { Mode off }

			Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment fragment
			ENDCG
		} 
    }
}
