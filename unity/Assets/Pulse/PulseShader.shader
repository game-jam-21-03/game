// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/PulseShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_DetailTex("Texture", 2D) = "white" {}
		_PulseDistance("Pulse Distance", float) = 0
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

		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct VertIn
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				// store a vector in each corner of the screen from the camera
				float4 ray : TEXCOORD1;
			};

			struct VertOut
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 uv_depth : TEXCOORD1;
				float4 interpolatedRay : TEXCOORD2;
			};

			float4 _MainTex_TexelSize;
			float4 _CameraWS;

			VertOut vert(VertIn v)
			{
				VertOut o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;
				o.uv_depth = v.uv.xy;

				#if UNITY_UV_STARTS_AT_TOP
				if(_MainTex_TexelSize.y < 0)
				{
					o.uv.y = 1 - o.uv.y;
				}
				#endif

				o.interpolatedRay = v.ray;

				return o;
			}

			
			sampler2D _MainTex;
			sampler2D _DetailTex;
			sampler2D_float _CameraDepthTexture;
			float4 _WorldSpacePulsePos;
			float _PulseDistance;
			float _PulseWidth;
			float _LeadSharp;
			float4 _LeadColor;
			float4 _MidColor;
			float4 _TrailColor;
			float4 _HBarColor;

			float4 horizBars(float2 p)
			{
				return 1 - saturate(round(abs(frac(p.y * 100) * 2)));
			}

			float4 horizTex(float2 p)
			{
				return tex2D(_DetailTex, float2(p.x * 30, p.y * 40));
			}

			half4 frag (VertOut i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.uv);

				// sample the depth value from given fragment
				float rawDepth = DecodeFloatRG(tex2D(_CameraDepthTexture, i.uv_depth));
				// Helper function from the UnityCG include file
				// gives us a val between 0 -> 1 for the depth
				float linearDepth = Linear01Depth(rawDepth);
				// Multiply linear depth value by interpolated ray will give us a direction
				// from the camera towards the far plane but with a magnitude = to the distance
				// to the sampled fragment.
				float4 wsDir = linearDepth * i.interpolatedRay;
				// WS pos value for every pixel in our image effect
				float3 wsPos = _WorldSpaceCameraPos + wsDir;
				half4 pulseCol = half4(0,0,0,0);

				// Feed position of a transform into the shader called _WorldSpacePulsePos
				float dist = distance(wsPos, _WorldSpacePulsePos);
				// checks if the distance is between the range of the distance and the width
				if (dist < _PulseDistance && dist > _PulseDistance - _PulseWidth && linearDepth < 1)
				{
					float diff = 1 - (_PulseDistance - dist) / (_PulseWidth);
					half4 edge = lerp(_MidColor, _LeadColor, pow(diff, _LeadSharp));
					pulseCol = lerp(_TrailColor, edge, diff) + horizBars(i.uv) * _HBarColor;
					pulseCol *= diff;
				}
				return col + pulseCol;
			}
			ENDCG
		}
	}
	//FallBack "Diffuse"
}
