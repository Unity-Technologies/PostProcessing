Shader "Hidden/PostProcessing/SunShaftsComposite" 
{
	HLSLINCLUDE

	#include "../StdLib.hlsl"

	// Converts color to luminance (grayscale)
	inline half Luminance(half3 rgb)
	{
		return dot(rgb, unity_ColorSpaceLuminance.rgb);
	}
	

	TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
	sampler2D _MainTex;

	uniform half4 _SunThreshold;
		
	uniform half4 _ScreenResultion;
	uniform half4 _BlurRadius4;
	uniform half4 _SunPosition;
	uniform half4 _MainTex_TexelSize;	
	half4 _MainTex_ST;
	half4 _CameraDepthTexture_ST;


	#define SAMPLES_FLOAT 6.0f
	#define SAMPLES_INT 6


	VaryingsDefault vert_radial(AttributesDefault v)
	{
		VaryingsDefault o;
		o.vertex = float4(v.vertex.xy, 0.0, 1.0);
		o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);
#if UNITY_UV_STARTS_AT_TOP
		o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif
		o.texcoordStereo = (_SunPosition.xy - o.texcoord.xy) * _BlurRadius4.xy;
		return o;
	}


	half4 frag_radial(VaryingsDefault i) : SV_Target
	{	
		half4 color = half4(0,0,0,0);
		for(int j = 0; j < SAMPLES_INT; j++)   
		{	
			half4 tmpColor = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord.xy, _MainTex_ST));
			color += tmpColor;
			i.texcoord.xy += i.texcoordStereo;
		}
		return color / SAMPLES_FLOAT;
	}				


	half TransformColor(half4 skyboxValue) {
		return dot(max(skyboxValue.rgb - _SunThreshold.rgb, half3(0, 0, 0)), half3(1, 1, 1)); // threshold and convert to greyscale
	}

	half4 frag_depth (VaryingsDefault i) : SV_Target {
		#if UNITY_UV_STARTS_AT_TOP
		float depthSample = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoScreenSpaceUVAdjust(i.texcoordStereo.xy, _CameraDepthTexture_ST));
		#else
		float depthSample = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoScreenSpaceUVAdjust(i.texcoord.xy, _CameraDepthTexture_ST));
		#endif
		depthSample = Linear01Depth(depthSample);
		
		half4 tex = tex2D (_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord.xy, _MainTex_ST));
		
		// consider maximum radius
		#if UNITY_UV_STARTS_AT_TOP
		half2 vec = _SunPosition.xy - i.texcoordStereo.xy;
		#else
		half2 vec = _SunPosition.xy - i.texcoord.xy;
		#endif
		half dist = saturate (_SunPosition.w - length (vec.xy));		
		
		half4 outColor = 0;

		// consider shafts blockers
		if (depthSample > 0.99)
			outColor = TransformColor(tex) * dist;

		// paint a small black border to get rid of clamping problems
		if (i.vertex.x == 0.5f || i.vertex.x == _ScreenResultion.x || i.vertex.y == 0.5f || i.vertex.y == _ScreenResultion.y)
			return half4(0, 0, 0, 0);

		return outColor;
	}
	ENDHLSL
	
	Subshader 
	{
		ZTest Always Cull Off ZWrite Off
  
		Pass { //  0
			HLSLPROGRAM
      
			#pragma vertex vert_radial
			#pragma fragment frag_radial
      
			ENDHLSL
		}
  
		Pass { //  1
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment frag_depth
      
			ENDHLSL
		}
	}
}
