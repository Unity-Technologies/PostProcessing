using System;

namespace UnityEngine.PostProcessing
{
	[Serializable]
	public class SharpenModel : PostProcessingModel
	{
		[Serializable]
		public struct Settings
		{
			[Range(0,1)]
			public float intensity;
			[Range(0.0f, 10.0f)]
			public float size;
			[Range(0,2)]
			public int downsample;
			[Range(1,4)]
			public int iterations;
			public SharpenType type;

			public static Settings defaultSettings
			{
				get
				{
					return new Settings
					{
						intensity = 1,
						size = 0.6f,
						downsample = 0,
						iterations = 1,
						type = SharpenType.StandardGauss
					};
				}
			}
		}

		[Serializable]
		public enum SharpenType
		{
			StandardGauss = 0,
			SgxGauss = 1,
		}

		[SerializeField]
		Settings m_Settings = Settings.defaultSettings;
		public Settings settings
		{
			get { return m_Settings; }
			set { m_Settings = value; }
		}

		public override void Reset()
		{
			m_Settings = Settings.defaultSettings;
		}
	}
}