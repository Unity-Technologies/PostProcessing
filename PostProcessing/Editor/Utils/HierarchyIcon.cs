using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [InitializeOnLoad]
    sealed class HierarchyIcon
    {
        static Texture2D s_Icon;

        static Texture2D GetIcon()
        {
            if (s_Icon == null)
                s_Icon = Resources.Load<Texture2D>("PostProcess-Icon");

            if (s_Icon != null)
                s_Icon.hideFlags = HideFlags.DontSaveInEditor;

            return s_Icon;
        }

        static HierarchyIcon()
        {
            if (GetIcon() != null)
                EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            var inst = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (inst == null)
                return;

            if (inst.GetComponent<PostProcessVolume>() != null)
            {
                float size = selectionRect.height;
                var r = new Rect(selectionRect.xMax - size - 2, selectionRect.yMin, size, size);
                GUI.DrawTexture(r, GetIcon(), ScaleMode.ScaleAndCrop);
            }
        }
    }
}
