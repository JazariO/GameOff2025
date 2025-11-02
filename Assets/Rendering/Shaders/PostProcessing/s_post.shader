Shader "Unlit/s_post"
{
	HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

		float _GridScale;

		SamplerState point_clamp_sampler;

		float4 calc(Varyings input) : SV_TARGET
		{
			float2 screenResolution = _ScreenParams.xy;

			float2 scaledAspectRatioUV = screenResolution / _GridScale;

			float2 scaledTexCoord = input.texcoord * scaledAspectRatioUV;
			float2 id = floor(scaledTexCoord);
			float2 gridTexCoord = id / scaledAspectRatioUV;

			float4 blit = SAMPLE_TEXTURE2D_X(_BlitTexture, point_clamp_sampler, gridTexCoord);
			return blit;
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
