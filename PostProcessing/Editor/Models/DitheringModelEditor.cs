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
        SerializedProperty m_AnimatedNoise;
        SerializedProperty m_Amount;

        int lastR;

        public override void OnEnable()
        {
            m_DepthSelectionMode = FindSetting((Settings x) => x.depthSelectionMode);
            m_BitsPerChannel_R = FindSetting((Settings x) => x.bitsPerChannel_R);
            m_BitsPerChannel_G = FindSetting((Settings x) => x.bitsPerChannel_G);
            m_BitsPerChannel_B = FindSetting((Settings x) => x.bitsPerChannel_B);
            m_AnimatedNoise = FindSetting((Settings x) => x.animatedNoise);
            m_Amount = FindSetting((Settings x) => x.amount);

            lastR = m_BitsPerChannel_R.intValue;
        }

        public override void OnInspectorGUI()
        {
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

            if (depthSelectionMode == 0)
            {
                EditorGUILayout.IntSlider(m_BitsPerChannel_R, 1, 16, "Channel Depth");
                if (m_BitsPerChannel_R.intValue != lastR)
                {
                    m_BitsPerChannel_G.intValue = m_BitsPerChannel_R.intValue;
                    m_BitsPerChannel_B.intValue = m_BitsPerChannel_R.intValue;
                    lastR = m_BitsPerChannel_R.intValue;
                }
            }
            if (depthSelectionMode == 1)
            {
                EditorGUILayout.IntSlider(m_BitsPerChannel_R, 1, 16, "Red");
                EditorGUILayout.IntSlider(m_BitsPerChannel_G, 1, 16, "Green");
                EditorGUILayout.IntSlider(m_BitsPerChannel_B, 1, 16, "Blue");
                lastR = m_BitsPerChannel_R.intValue;
            }
            if (depthSelectionMode == 2)
            {
                EditorGUILayout.HelpBox("Automatic color depth detection is not currently implemented.", MessageType.Warning);
            }

            int depth = m_BitsPerChannel_R.intValue + 
                        m_BitsPerChannel_G.intValue + 
                        m_BitsPerChannel_B.intValue;
            int comma = 0;
            string numberOfColors = "" + System.Math.Pow(2d, depth);

            for (int i = numberOfColors.Length-1; i > 0; i--)
            {
                comma++;
                if(comma == 3)
                {
                    numberOfColors = numberOfColors.Insert(i, ",");
                    comma = 0;
                }
            }

            EditorGUILayout.HelpBox("Using " + depth + "-bit color.\nTotal number of colors: " + numberOfColors, MessageType.None);

            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("Dithering Settings", EditorStyles.boldLabel);

            bool animated = m_AnimatedNoise.boolValue;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(15);
                EditorGUILayout.PrefixLabel("Noise Type");
                if (GUILayout.Toggle(!animated, "Static", EditorStyles.miniButtonLeft))  animated = false;
                if (GUILayout.Toggle(animated, "Animated", EditorStyles.miniButtonRight)) animated = true;
            }

            m_AnimatedNoise.boolValue = animated;

            EditorGUI.indentLevel++;
            EditorGUILayout.Slider(m_Amount, 0, 1);
        }
    }
}