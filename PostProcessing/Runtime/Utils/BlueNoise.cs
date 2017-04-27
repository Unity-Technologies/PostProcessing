using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Experimental.PostProcessing
{
    public sealed class BlueNoise
    {
        // 64 64x64 Alpha8 textures(256kb total)
        const int k_NoiseTextureCount = 64;
        Texture2D[] m_NoiseTextures;

        [IndexerName("texture")]
        public Texture2D this[int index]
        {
            get
            {
                if (index >= k_NoiseTextureCount)
                    throw new ArgumentOutOfRangeException();

                if (m_NoiseTextures == null || m_NoiseTextures.Length == 0)
                    LoadNoiseTextures();

                return m_NoiseTextures[index];
            }
        }

        public int count
        {
            get { return k_NoiseTextureCount; }
        }

        void LoadNoiseTextures()
        {
            m_NoiseTextures = new Texture2D[k_NoiseTextureCount];

            for (int i = 0; i < k_NoiseTextureCount; i++)
                m_NoiseTextures[i] = Resources.Load<Texture2D>("Bluenoise64/LDR_LLL1_" + i);
        }

        public void Release()
        {
            m_NoiseTextures = null;
        }
    }
}
