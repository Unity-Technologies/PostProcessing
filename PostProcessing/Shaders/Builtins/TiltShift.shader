Shader "Hidden/PostProcessing/TiltShift" 
{
	HLSLINCLUDE

	#pragma multi_compile __ IRIS

	#include "../StdLib.hlsl"
	#include "../Colors.hlsl"

	sampler2D _MainTex;
	sampler2D _Blurred;

	float4 _MainTex_TexelSize;
	half4 _MainTex_ST;
	half4 _Blurred_ST;
	float _BlurSize;
	float _BlurArea;

	#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_METAL)
	#define SAMPLE_TEX(sampler, uv) tex2Dlod(sampler, float4(uv,0,1))
	#else
	#define SAMPLE_TEX(sampler, uv) tex2D(sampler, uv)
	#endif

	static const int SmallDiscKernelSamples = 12;		
	static const float2 SmallDiscKernel[SmallDiscKernelSamples] =
	{
		float2(-0.326212,-0.40581),
		float2(-0.840144,-0.07358),
		float2(-0.695914,0.457137),
		float2(-0.203345,0.620716),
		float2(0.96234,-0.194983),
		float2(0.473434,-0.480026),
		float2(0.519456,0.767022),
		float2(0.185461,-0.893124),
		float2(0.507431,0.064425),
		float2(0.89642,0.412458),
		float2(-0.32194,-0.932615),
		float2(-0.791559,-0.59771)
	};

	static const int NumDiscSamples = 28;
	static const float3 DiscKernel[NumDiscSamples] = 
	{
		float3(0.62463,0.54337,0.82790),
		float3(-0.13414,-0.94488,0.95435),
		float3(0.38772,-0.43475,0.58253),
		float3(0.12126,-0.19282,0.22778),
		float3(-0.20388,0.11133,0.23230),
		float3(0.83114,-0.29218,0.88100),
		float3(0.10759,-0.57839,0.58831),
		float3(0.28285,0.79036,0.83945),
		float3(-0.36622,0.39516,0.53876),
		float3(0.75591,0.21916,0.78704),
		float3(-0.52610,0.02386,0.52664),
		float3(-0.88216,-0.24471,0.91547),
		float3(-0.48888,-0.29330,0.57011),
		float3(0.44014,-0.08558,0.44838),
		float3(0.21179,0.51373,0.55567),
		float3(0.05483,0.95701,0.95858),
		float3(-0.59001,-0.70509,0.91938),
		float3(-0.80065,0.24631,0.83768),
		float3(-0.19424,-0.18402,0.26757),
		float3(-0.43667,0.76751,0.88304),
		float3(0.21666,0.11602,0.24577),
		float3(0.15696,-0.85600,0.87027),
		float3(-0.75821,0.58363,0.95682),
		float3(0.99284,-0.02904,0.99327),
		float3(-0.22234,-0.57907,0.62029),
		float3(0.55052,-0.66984,0.86704),
		float3(0.46431,0.28115,0.54280),
		float3(-0.07214,0.60554,0.60982),
	};	


	float Weight(float2 uv)
	{
		float2 tapCoord = uv * 2.0 - 1.0;
#if IRIS
		return dot(tapCoord, tapCoord) * _BlurArea;
#else
		return abs(tapCoord.y * _BlurArea);
#endif
	}

	float4 fragPreview(VaryingsDefault i) : SV_Target
	{
		return Weight(i.texcoord.xy) * 0.5;
	}

	float4 fragLow(VaryingsDefault i) : SV_Target
	{
		float4 centerTap = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord.xy, _MainTex_ST));
		float4 sum = centerTap;

		float w = clamp(Weight(i.texcoord.xy), 0, _BlurSize);

		float4 poissonScale = _MainTex_TexelSize.xyxy * w;

		if (w < 1e-2f)
			return sum;

		for (int l = 0; l < SmallDiscKernelSamples; l++)
		{
			float2 sampleUV = UnityStereoScreenSpaceUVAdjust(i.texcoord.xy + SmallDiscKernel[l].xy * poissonScale.xy, _MainTex_ST);
			float4 sample0 = SAMPLE_TEX(_MainTex, sampleUV.xy);
			sum += sample0;
		}
		return float4(sum.rgb / (1.0 + SmallDiscKernelSamples), w);
	}

	float4 frag(VaryingsDefault i) : SV_Target
	{
		float4 centerTap = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord.xy, _MainTex_ST));
		float4 sum = centerTap;

		float w = clamp(Weight(i.texcoord.xy), 0, _BlurSize);

		float4 poissonScale = _MainTex_TexelSize.xyxy * w;

		if (w < 1e-2f)
			return sum;

		for (int l = 0; l < NumDiscSamples; l++)
		{
			float2 sampleUV = UnityStereoScreenSpaceUVAdjust(i.texcoord.xy + DiscKernel[l].xy * poissonScale.xy, _MainTex_ST);
			float4 sample0 = SAMPLE_TEX(_MainTex, sampleUV.xy);
			sum += sample0;
		}
		return float4(sum.rgb / (1.0 + NumDiscSamples), w);
	}

	float4 fragHQ(VaryingsDefault i) : SV_Target
	{
		float4 centerTap = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.texcoord.xy, _MainTex_ST));
		float4 sum = centerTap;

		float w = clamp(Weight(i.texcoord.xy), 0, _BlurSize);

		float4 poissonScale = _MainTex_TexelSize.xyxy * float4(1,1,-1,-1) * w;

		if (w < 1e-2f)
			return sum;

		for (int l = 0; l < NumDiscSamples; l++)
		{
			float4 sampleUV = UnityStereoScreenSpaceUVAdjust(i.texcoord.xyxy + DiscKernel[l].xyxy * poissonScale, _MainTex_ST);
			float4 sample0 = SAMPLE_TEX(_MainTex, sampleUV.xy);
			float4 sample1 = SAMPLE_TEX(_MainTex, sampleUV.zw);

			sum += sample0 + sample1;
		}
		return float4(sum.rgb / (1.0 + 2.0 * NumDiscSamples), w);
	}
 
	ENDHLSL
	
	Subshader 
	{
		ZTest Always Cull Off ZWrite Off

		Pass{ // 0 
			HLSLPROGRAM

			#pragma target 3.0
			#pragma vertex VertDefault
			#pragma fragment fragPreview

			ENDHLSL
		}

		Pass{ // 1
			HLSLPROGRAM

			#pragma target 3.0
			#pragma vertex VertDefault
			#pragma fragment fragLow

			ENDHLSL
		}

		Pass{ // 2
			HLSLPROGRAM

			#pragma target 3.0
			#pragma vertex VertDefault
			#pragma fragment frag

			ENDHLSL
		}

		Pass{ // 3
			HLSLPROGRAM

			#pragma target 3.0
			#pragma vertex VertDefault
			#pragma fragment fragHQ

			ENDHLSL
		}
	}
}
