Shader "Custom/Pulse"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}

		_PulseWidth("Pulse Width", float) = 10
		_LeadSharp("Leading Edge Sharpness", float) = 10
		_LeadColor("Leading Edge Color", Color) = (1, 1, 1, 0)
		_MidColor("Mid Color", Color) = (1, 1, 1, 0)
		_TrailColor("Trail Color", Color) = (1, 1, 1, 0)
		_HBarColor("Horizontal Bar Color", Color) = (0.5, 0.5, 0.5, 0)
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

			struct VertIn
			{
				float4 positionOS : POSITION;
			};

			struct VertOut
			{
				float4 positionHCS : SV_POSITION;
			};

			// Rendering Resources
			float4 _MainTex_TexelSize;

			VertOut vert(VertIn i)
			{
				VertOut o;
				o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
				return o;
			}

			// Material Inspector Properties
			float _PulseWidth;
			float _LeadSharp;
			float4 _LeadColor;
			float4 _MidColor;
			float4 _TrailColor;
			float4 _HBarColor;

			// Programmatic Properties
			float _DistanceTravelled;
			float4 _StartPosition;

			// Rendering Resources
			sampler2D _MainTex;

			float4 horizBars(float2 p)
			{
				return 1 - saturate(round(abs(frac(p.y * 100) * 2)));
			}

			half4 frag(VertOut i) : SV_Target
			{
				float2 uv = i.positionHCS.xy / _ScaledScreenParams.xy;
				half4 currentColor = tex2D(_MainTex, uv);

				float depth = SampleSceneDepth(uv);
				#if !UNITY_REVERSED_Z
					// Adjust z to match NDC for OpenGL
					depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
				#endif

				float3 fragmentPositionWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
				float fragmentDistance = distance(fragmentPositionWS, _StartPosition.xyz);

				half4 pulseColor = half4(0, 0, 0, 0);
				if (fragmentDistance < _DistanceTravelled && fragmentDistance > _DistanceTravelled - _PulseWidth)
				{
					float diff = 1 - (_DistanceTravelled - fragmentDistance) / _PulseWidth;
					half4 edge = lerp(_MidColor, _LeadColor, pow(abs(diff), _LeadSharp));
					pulseColor = lerp(_TrailColor, edge, diff) + horizBars(uv) * _HBarColor;
					pulseColor *= diff;
				}
				return currentColor + pulseColor;
			}
			ENDHLSL
		}
	}
}
