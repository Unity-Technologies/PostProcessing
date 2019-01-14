Shader "Hidden/PostProcessing/GlobalFog" 
{
	HLSLINCLUDE

	#include "../StdLib.hlsl"

	uniform sampler2D _MainTex;
	TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
	
	// x = fog height
	// y = FdotC (CameraY-FogHeight)
	// z = k (FdotC > 0.0)
	// w = a/2
	uniform float4 _HeightParams;
	
	// x = start distance
	uniform float4 _DistanceParams;
	
	int4 _SceneFogMode; // x = fog mode, y = use radial flag
	float4 _SceneFogParams;
	#ifndef UNITY_APPLY_FOG
	half4 unity_FogColor;
	half4 unity_FogDensity;
	#endif	
	half4 _CameraDepthTexture_ST;
	
	// for fast world space reconstruction
	uniform float4x4 _FrustumCornersWS;
	uniform float4 _CameraWS;
	
	struct v2f {
		float4 vertex : SV_POSITION;
		float2 texcoord : TEXCOORD0;
		float2 texcoordStereo : TEXCOORD1;
		float4 interpolatedRay : TEXCOORD2;
	};
	
	v2f vert (AttributesDefault v)
	{
		v2f o;
		o.vertex = float4(v.vertex.xy, 0.0, 1.0);
		o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);

#if UNITY_UV_STARTS_AT_TOP
		o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif		
		o.texcoordStereo = TransformStereoScreenSpaceTex(o.texcoord, 1.0);

		int frustumIndex = o.texcoord.x + (2 * o.texcoord.y);
		o.interpolatedRay = _FrustumCornersWS[frustumIndex];
		o.interpolatedRay.w = frustumIndex;
		
		return o;
	}
	
	// Applies one of standard fog formulas, given fog coordinate (i.e. distance)
	half ComputeFogFactor (float coord)
	{
		float fogFac = 0.0;
		if (_SceneFogMode.x == 1) // linear
		{
			// factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
			fogFac = coord * _SceneFogParams.z + _SceneFogParams.w;
		}
		if (_SceneFogMode.x == 2) // exp
		{
			// factor = exp(-density*z)
			fogFac = _SceneFogParams.y * coord; fogFac = exp2(-fogFac);
		}
		if (_SceneFogMode.x == 3) // exp2
		{
			// factor = exp(-(density*z)^2)
			fogFac = _SceneFogParams.x * coord; fogFac = exp2(-fogFac*fogFac);
		}
		return saturate(fogFac);
	}

	// Distance-based fog
	float ComputeDistance (float3 camDir, float zdepth)
	{
		float dist; 
		if (_SceneFogMode.y == 1)
			dist = length(camDir);
		else
			dist = zdepth * _ProjectionParams.z;
		// Built-in fog starts at near plane, so match that by
		// subtracting the near value. Not a perfect approximation
		// if near plane is very large, but good enough.
		dist -= _ProjectionParams.y;
		return dist;
	}

	// Linear half-space fog, from https://www.terathon.com/lengyel/Lengyel-UnifiedFog.pdf
	float ComputeHalfSpace (float3 wsDir)
	{
		float3 wpos = _CameraWS.xyz + wsDir;
		float FH = _HeightParams.x;
		float3 C = _CameraWS.xyz;
		float3 V = wsDir;
		float3 P = wpos;
		float3 aV = _HeightParams.w * V;
		float FdotC = _HeightParams.y;
		float k = _HeightParams.z;
		float FdotP = P.y-FH;
		float FdotV = wsDir.y;
		float c1 = k * (FdotP + FdotC);
		float c2 = (1-2*k) * FdotP;
		float g = min(c2, 0.0);
		g = -length(aV) * (c1 - g * g / abs(FdotV+1.0e-5f));
		return g;
	}

	half4 ComputeFog (v2f i, bool distance, bool height) : SV_Target
	{
		half4 sceneColor = tex2D(_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord));
		
		// Reconstruct world space position & direction
		// towards this screen pixel.
#if UNITY_UV_STARTS_AT_TOP
		float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoScreenSpaceUVAdjust(i.texcoordStereo.xy, _CameraDepthTexture_ST));
#else
		float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoScreenSpaceUVAdjust(i.texcoord.xy, _CameraDepthTexture_ST));
#endif
		float dpth = Linear01Depth(rawDepth);
		float4 wsDir = dpth * i.interpolatedRay;
		float4 wsPos = _CameraWS + wsDir;

		// Compute fog distance
		float g = _DistanceParams.x;
		if (distance)
			g += ComputeDistance (wsDir.xyz, dpth);
		if (height)
			g += ComputeHalfSpace (wsDir.xyz);

		// Compute fog amount
		half fogFac = ComputeFogFactor (max(0.0,g));
		// Do not fog skybox
		if (dpth == _DistanceParams.y)
			fogFac = 1.0;
		//return fogFac; // for debugging
		
		// Lerp between fog color & original scene color
		// by fog amount
		return lerp (unity_FogColor, sceneColor, fogFac);
	}

	ENDHLSL

	SubShader
	{
		ZTest Always Cull Off ZWrite Off Fog { Mode Off }

		// 0: distance + height
		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			half4 frag (v2f i) : SV_Target { return ComputeFog (i, true, true); }
			ENDHLSL
		}
		// 1: distance
		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			half4 frag (v2f i) : SV_Target { return ComputeFog (i, true, false); }
			ENDHLSL
		}
		// 2: height
		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			half4 frag (v2f i) : SV_Target { return ComputeFog (i, false, true); }
			ENDHLSL
		}
	}
	Fallback off
}
