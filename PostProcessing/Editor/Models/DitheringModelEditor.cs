using UnityEngine;
using UnityEngine.PostProcessing;

namespace UnityEditor.PostProcessing
{
    using Settings = DitheringModel.Settings;

    [PostProcessingModelEditor(typeof(DitheringModel))]
    public class DitheringModelEditor : DefaultPostFxModelEditor
    {
        SerializedProperty m_DepthSelectionMode;
        SerializedProperty m_BitsPerChannel_R;
        SerializedProperty m_BitsPerChannel_G;
        SerializedProperty m_BitsPerChannel_B;
        SerializedProperty m_DitheringMode;

        int lastR;

        public override void OnEnable()
        {
            m_DepthSelectionMode = FindSetting((Settings x) => x.depthSelectionMode);
            m_BitsPerChannel_R = FindSetting((Settings x) => x.bitsPerChannel_R);
            m_BitsPerChannel_G = FindSetting((Settings x) => x.bitsPerChannel_G);
            m_BitsPerChannel_B = FindSetting((Settings x) => x.bitsPerChannel_B);
            m_DitheringMode = FindSetting((Settings x) => x.ditheringMode);

            lastR = m_BitsPerChannel_R.intValue;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Dithering requires HDR to function properly.", MessageType.Info);

            EditorGUILayout.LabelField("Color Depth (bits per channel)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            int depthSelectionMode = m_DepthSelectionMode.intValue;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(15);
                if (GUILayout.Toggle(depthSelectionMode == 0, "Single Slider", EditorStyles.miniButtonLeft))
                    depthSelectionMode = 0;
                if (GUILayout.Toggle(depthSelectionMode == 1, "RGB Sliders", EditorStyles.miniButtonMid))
                    depthSelectionMode = 1;
                if (GUILayout.Toggle(depthSelectionMode == 2, "Automatic", EditorStyles.miniButtonRight))
                    depthSelectionMode = 2;
            }

            m_DepthSelectionMode.intValue = depthSelectionMode;

            switch (depthSelectionMode)
            {
                case 0:
                {
                    EditorGUILayout.IntSlider(m_BitsPerChannel_R, 1, 16, "Channel Depth");
                    if (m_BitsPerChannel_R.intValue != lastR)
                    {
                        m_BitsPerChannel_G.intValue = m_BitsPerChannel_R.intValue;
                        m_BitsPerChannel_B.intValue = m_BitsPerChannel_R.intValue;
                        lastR = m_BitsPerChannel_R.intValue;
                    }
                    break;
                }
                case 1:
                {
                    EditorGUILayout.IntSlider(m_BitsPerChannel_R, 1, 16, "Red");
                    EditorGUILayout.IntSlider(m_BitsPerChannel_G, 1, 16, "Green");
                    EditorGUILayout.IntSlider(m_BitsPerChannel_B, 1, 16, "Blue");
                    lastR = m_BitsPerChannel_R.intValue;
                    break;
                }
                case 2:
                {
                    EditorGUILayout.HelpBox("Automatic color depth detection is not currently implemented.", MessageType.Warning);
                    break;
                }
            }

            int depth = m_BitsPerChannel_R.intValue + 
                        m_BitsPerChannel_G.intValue + 
                        m_BitsPerChannel_B.intValue;
            int comma = 0;
            string numberOfColors = "" + System.Math.Pow(2d, depth);

            for (int i = numberOfColors.Length-1; i > 0; i--)
            {
                comma++;
                if (comma == 3)
                {
                    numberOfColors = numberOfColors.Insert(i, ",");
                    comma = 0;
                }
            }

            EditorGUILayout.HelpBox("Using " + depth + "-bit color.\nTotal number of colors: " + numberOfColors, MessageType.None);

            EditorGUI.indentLevel--;

            int ditheringMode = m_DitheringMode.intValue;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Dithering Mode");
                if (GUILayout.Toggle(ditheringMode == 0, "Off", EditorStyles.miniButtonLeft))
                    ditheringMode = 0;
                if (GUILayout.Toggle(ditheringMode == 1, "Static", EditorStyles.miniButtonMid))
                    ditheringMode = 1;
                if (GUILayout.Toggle(ditheringMode == 2, "Animated", EditorStyles.miniButtonRight))
                    ditheringMode = 2;
            }

            m_DitheringMode.intValue = ditheringMode;
        }
    }
}