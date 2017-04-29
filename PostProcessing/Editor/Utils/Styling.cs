using UnityEngine;
using UnityEngine.Experimental.PostProcessing;

namespace UnityEditor.Experimental.PostProcessing
{
    public static class Styling
    {
        public static readonly GUIStyle smallTickbox;
        public static readonly GUIStyle miniLabelButton;

        public static readonly Texture2D paneOptionsIconDark;
        public static readonly Texture2D paneOptionsIconLight;

        static Styling()
        {
            smallTickbox = new GUIStyle("ShurikenCheckMark");

            miniLabelButton = new GUIStyle(EditorStyles.miniLabel);
            miniLabelButton.normal = new GUIStyleState
            {
                background = RuntimeUtilities.transparentTexture,
                scaledBackgrounds = null,
                textColor = Color.grey
            };
            var activeState = new GUIStyleState
            {
                background = RuntimeUtilities.transparentTexture,
                scaledBackgrounds = null,
                textColor = Color.white
            };
            miniLabelButton.active = activeState;
            miniLabelButton.onNormal = activeState;
            miniLabelButton.onActive = activeState;

            paneOptionsIconDark = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/pane options.png");
            paneOptionsIconLight = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/pane options.png");
        }
    }
}
