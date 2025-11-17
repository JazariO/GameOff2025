Shader "Unlit/s_thermal"
{
	HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

		float _GridScale;
		TEXTURE2D(_ThermalGradient); SAMPLER(sampler_ThermalGradient);

		SamplerState point_clamp_sampler;

		float4 calc(Varyings input) : SV_TARGET
		{
			float2 screenResolution = _ScreenParams.xy;

			float2 scaledAspectRatioUV = screenResolution / _GridScale;

			float2 scaledTexCoord = input.texcoord * scaledAspectRatioUV;
			float2 id = floor(scaledTexCoord);
			float2 gridTexCoord = id / scaledAspectRatioUV;

			float4 blit = SAMPLE_TEXTURE2D_X(_BlitTexture, point_clamp_sampler, gridTexCoord);

			float greyScale = 0.299 * blit.r + 0.587 * blit.g + 0.114 * blit.b;

			float2 thermalGradUV = float2(greyScale, 0.5);
			float4 thermalGradient = SAMPLE_TEXTURE2D(_ThermalGradient, sampler_ThermalGradient, thermalGradUV);
			return float4(thermalGradient);
		}
	ENDHLSL

	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
		LOD 100
		ZWrite Off
		Cull Off

		Pass
		{
			Name "Fragment"
			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment calc
			ENDHLSL
		}
	}
}
