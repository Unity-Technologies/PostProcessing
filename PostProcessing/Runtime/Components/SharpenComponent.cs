namespace UnityEngine.PostProcessing
{
	public class SharpenComponent : PostProcessingComponentRenderTexture<SharpenModel>
	{
		private static class Uniforms
		{
			internal static readonly int _SharpenTex = Shader.PropertyToID("_SharpenTex");
			internal static readonly int _SharpenSettings = Shader.PropertyToID("_SharpenSettings");
		}

		public override bool active
		{
			get { return model.enabled && model.settings.intensity > 0; }
		}

		public void Prepare(RenderTexture source, Material uberMaterial, Texture autoExposure)
		{
			var fastBlurMaterial = context.materialFactory.Get("Hidden/Post FX/FastBlur");
			float widthMod = 1.0f / (1.0f * (1 << model.settings.downsample));

			fastBlurMaterial.SetVector("_Parameter", new Vector4(model.settings.size * widthMod, -model.settings.size * widthMod, 0.0f, 0.0f));
			source.filterMode = FilterMode.Bilinear;

			int rtW = source.width >> model.settings.downsample;
			int rtH = source.height >> model.settings.downsample;

			// downsample
			RenderTexture rt = context.renderTextureFactory.Get(rtW, rtH, 0, source.format);

			rt.filterMode = FilterMode.Bilinear;
			Graphics.Blit(source, rt, fastBlurMaterial, 0);

			var passOffs = model.settings.type == SharpenModel.SharpenType.StandardGauss ? 0 : 2;

			for (int i = 0; i < model.settings.iterations; i++)
			{
				float iterationOffs = (i * 1.0f);
				fastBlurMaterial.SetVector("_Parameter", new Vector4(model.settings.size * widthMod + iterationOffs, -model.settings.size * widthMod - iterationOffs, 0.0f, 0.0f));

				// vertical blur
				RenderTexture rt2 = context.renderTextureFactory.Get(rtW, rtH, 0, source.format);
				rt2.filterMode = FilterMode.Bilinear;
				Graphics.Blit(rt, rt2, fastBlurMaterial, 1 + passOffs);
				context.renderTextureFactory.Release(rt);
				rt = rt2;

				// horizontal blur
				rt2 = context.renderTextureFactory.Get(rtW, rtH, 0, source.format);
				rt2.filterMode = FilterMode.Bilinear;
				Graphics.Blit(rt, rt2, fastBlurMaterial, 2 + passOffs);
				context.renderTextureFactory.Release(rt);
				rt = rt2;
			}

			context.renderTextureFactory.Release(rt);

			uberMaterial.SetVector(Uniforms._SharpenSettings,new Vector4(model.settings.intensity,0));
			uberMaterial.SetTexture(Uniforms._SharpenTex, rt);
			uberMaterial.EnableKeyword("SHARPEN");
		}
	}
}