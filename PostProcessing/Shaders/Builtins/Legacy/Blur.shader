Shader "Hidden/Legacy/Blur"
{
	HLSLINCLUDE

	#include "../../StdLib.hlsl"

	TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
	TEXTURE2D_SAMPLER2D(_BlurTex, sampler_BlurTex);

	uniform half4 _Parameter;
	uniform half4 _MainTex_TexelSize;
	half4 _MainTex_ST;

	// Weight Curves..
	static const half curve[7] = { 0.0205, 0.0855, 0.232, 0.324, 0.232, 0.0855, 0.0205 };
	static const half4 curve4[7] = { half4(0.0205,0.0205,0.0205,0), half4(0.0855,0.0855,0.0855,0), half4(0.232,0.232,0.232,0), half4(0.324,0.324,0.324,1), half4(0.232,0.232,0.232,0), half4(0.0855,0.0855,0.0855,0), half4(0.0205,0.0205,0.0205,0) };

	struct VaryingsDownsample
	{
		float4 vertex :SV_POSITION;
		half2 uv20    :TEXCOORD0;
		half2 uv21    :TEXCOORD1;
		half2 uv22    :TEXCOORD2;
		half2 uv23    :TEXCOORD3;
	};

	struct VaryingsBlurCoords8
	{
		float4 vertex :SV_POSITION;
		half2 uv      :TEXCOORD0;
		half2 offs    :TEXCOORD1;
	};

	struct VaryingsBlurCoordsSGX
	{
		float4 vertex :SV_POSITION;
		half2 uv      :TEXCOORD0;
		half4 offs[3] :TEXCOORD1;
	};

	struct VaryingsImg
	{
		float4 vertex :SV_POSITION;
		half2 uv      :TEXCOORD0;
	};

	VaryingsDownsample VertDownsample(AttributesDefault v)
	{
		VaryingsDownsample o;

		half2 texcoord = TransformTriangleVertexToUV(v.vertex.xy);

		#if UNITY_UV_STARTS_AT_TOP
			texcoord = texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
		#endif

		o.vertex = float4(v.vertex.xy, 0.0, 1.0);
		o.uv20 = UnityStereoScreenSpaceUVAdjust(texcoord + _MainTex_TexelSize.xy, _MainTex_ST);
		o.uv21 = UnityStereoScreenSpaceUVAdjust(texcoord + _MainTex_TexelSize.xy * half2(-0.5h, -0.5h), _MainTex_ST);
		o.uv22 = UnityStereoScreenSpaceUVAdjust(texcoord + _MainTex_TexelSize.xy * half2(0.5h, -0.5h), _MainTex_ST);
		o.uv23 = UnityStereoScreenSpaceUVAdjust(texcoord + _MainTex_TexelSize.xy * half2(-0.5h, 0.5h), _MainTex_ST);

		return o;
	}

	float4 FragDownsample(VaryingsDownsample i):SV_Target
	{
		float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv20);
		color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv21);
		color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv22);
		color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23);

		return color / 4;
	}

	VaryingsBlurCoords8 VertBlurHorizontal(AttributesDefault v)
	{
		VaryingsBlurCoords8 o;

		o.vertex = float4(v.vertex.xy, 0.0, 1.0);
		o.uv = TransformTriangleVertexToUV(v.vertex.xy);

		#if UNITY_UV_STARTS_AT_TOP
			o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
		#endif

		o.offs = _MainTex_TexelSize.xy * half2(1.0, 0.0) * _Parameter.x;

		return o;
	}

	VaryingsBlurCoords8 VertBlurVertical(AttributesDefault v)
	{
		VaryingsBlurCoords8 o;

		o.vertex = float4(v.vertex.xy, 0.0, 1.0);
		o.uv = TransformTriangleVertexToUV(v.vertex.xy);

		#if UNITY_UV_STARTS_AT_TOP
			o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
		#endif

		o.offs = _MainTex_TexelSize.xy * half2(0.0, 1.0) * _Parameter.x;

		return o;
	}

	float4 FragBlur8(VaryingsBlurCoords8 i):SV_Target
	{
		half2 uv = i.uv.xy;
		half2 netFilterWidth = i.offs;
		half2 coords = uv - netFilterWidth * 3.0;

		float4 color = 0;
		for(int l=0; l<7; l++)
		{
			half4 tap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoScreenSpaceUVAdjust(coords, _MainTex_ST));
			color += tap * curve4[l];
			coords += netFilterWidth;
		}

		return color;
	}

	VaryingsBlurCoordsSGX VertBlurHorizontalSGX(AttributesDefault v)
	{
		VaryingsBlurCoordsSGX o;

		o.vertex = float4(v.vertex.xy, 0.0, 1.0);
		o.uv = TransformTriangleVertexToUV(v.vertex.xy);

		#if UNITY_UV_STARTS_AT_TOP
			o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
		#endif

		half offsetMagnitude = _MainTex_TexelSize.x * _Parameter.x;
		o.offs[0] = UnityStereoScreenSpaceUVAdjust(o.uv.xyxy + offsetMagnitude * half4(-3.0h, 0.0h, 3.0h, 0.0h), _MainTex_ST);
		o.offs[1] = UnityStereoScreenSpaceUVAdjust(o.uv.xyxy + offsetMagnitude * half4(-2.0h, 0.0h, 2.0h, 0.0h), _MainTex_ST);
		o.offs[2] = UnityStereoScreenSpaceUVAdjust(o.uv.xyxy + offsetMagnitude * half4(-1.0h, 0.0h, 1.0h, 0.0h), _MainTex_ST);

		return o;
	}

	VaryingsBlurCoordsSGX VertBlurVerticalSGX(AttributesDefault v)
	{
		VaryingsBlurCoordsSGX o;

		o.vertex = float4(v.vertex.xy, 0.0, 1.0);
		o.uv = TransformTriangleVertexToUV(v.vertex.xy);

		#if UNITY_UV_STARTS_AT_TOP
			o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
		#endif

		half offsetMagnitude = _MainTex_TexelSize.y * _Parameter.x;
		o.offs[0] = UnityStereoScreenSpaceUVAdjust(o.uv.xyxy + offsetMagnitude * half4(0.0h, -3.0h, 0.0h, 3.0h), _MainTex_ST);
		o.offs[1] = UnityStereoScreenSpaceUVAdjust(o.uv.xyxy + offsetMagnitude * half4(0.0h, -2.0h, 0.0h, 2.0h), _MainTex_ST);
		o.offs[2] = UnityStereoScreenSpaceUVAdjust(o.uv.xyxy + offsetMagnitude * half4(0.0h, -1.0h, 0.0h, 1.0h), _MainTex_ST);

		return o;
	}

	float4 FragBlurSGX(VaryingsBlurCoordsSGX i):SV_Target
	{
		half2 uv = i.uv.xy;

		half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * curve4[3];

		for(int l=0; l<3; l++)
		{
			half4 tapA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.offs[l].xy);
			half4 tapB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.offs[l].zw);
			color += (tapA + tapB) * curve4[l];
		}

		return color;
	}

	ENDHLSL

	SubShader
	{
		Cull Off 
		ZWrite Off 
		ZTest Always

		Pass // 0
		{
			HLSLPROGRAM

			#pragma vertex VertDownsample
			#pragma fragment FragDownsample

			ENDHLSL
		}
		
		Pass // 1
		{
			HLSLPROGRAM

			#pragma vertex VertBlurVertical
			#pragma fragment FragBlur8

			ENDHLSL
		}

		Pass // 2
		{
			HLSLPROGRAM

			#pragma vertex VertBlurHorizontal
			#pragma fragment FragBlur8

			ENDHLSL
		}

		Pass // 3
		{
			HLSLPROGRAM

			#pragma vertex VertBlurVerticalSGX
			#pragma fragment FragBlurSGX

			ENDHLSL
		}

		Pass // 4
		{
			HLSLPROGRAM

			#pragma vertex VertBlurHorizontalSGX
			#pragma fragment FragBlurSGX

			ENDHLSL
		}
	}
}