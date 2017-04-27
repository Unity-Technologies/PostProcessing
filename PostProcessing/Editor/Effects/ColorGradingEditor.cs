using UnityEngine;
using UnityEngine.Experimental.PostProcessing;

namespace UnityEditor.Experimental.PostProcessing
{
    [PostProcessEditor(typeof(ColorGrading))]
    public sealed class ColorGradingEditor : PostProcessEffectEditor<ColorGrading>
    {
        SerializedParameterOverride m_LogLut;

        public override void OnEnable()
        {
            m_LogLut = FindParameterOverride(x => x.logLut);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Temporary UI for demo purposes.", MessageType.Info);

            PropertyField(m_LogLut);

            var lut = (target as ColorGrading).logLut.value;

            // Checks import settings on the LUT
            if (lut != null)
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(lut)) as TextureImporter;

                // Fails when using an internal texture as you can't change import settings on
                // builtin resources, thus the check for null
                if (importer != null)
                {
                    bool valid = importer.anisoLevel == 0
                        && importer.mipmapEnabled == false
                        && importer.sRGBTexture == false
                        && (importer.textureCompression == TextureImporterCompression.Uncompressed)
                        && importer.filterMode == FilterMode.Bilinear;

                    if (!valid)
                        EditorUtilities.DrawFixMeBox("Invalid LUT import settings.", () => SetLutImportSettings(importer));
                }
            }
        }

        void SetLutImportSettings(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.anisoLevel = 0;
            importer.sRGBTexture = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }
    }
}
