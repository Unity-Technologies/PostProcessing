using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    public static class Styling
    {
        public static readonly GUIStyle smallTickbox;
        public static readonly GUIStyle miniLabelButton;

        public static readonly Texture2D paneOptionsIconDark;
        public static readonly Texture2D paneOptionsIconLight;

        public static readonly GUIStyle labelHeader;

        public static readonly GUIStyle wheelLabel;
        public static readonly GUIStyle wheelThumb;
        public static readonly Vector2 wheelThumbSize;

        public static readonly GUIStyle preLabel;

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

            labelHeader = new GUIStyle(EditorStyles.miniLabel);

            wheelThumb = new GUIStyle("ColorPicker2DThumb");

            wheelThumbSize = new Vector2(
                !Mathf.Approximately(wheelThumb.fixedWidth, 0f) ? wheelThumb.fixedWidth : wheelThumb.padding.horizontal,
                !Mathf.Approximately(wheelThumb.fixedHeight, 0f) ? wheelThumb.fixedHeight : wheelThumb.padding.vertical
            );

            wheelLabel = new GUIStyle(EditorStyles.miniLabel);

            preLabel = new GUIStyle("ShurikenLabel");
        }
    }
} 
