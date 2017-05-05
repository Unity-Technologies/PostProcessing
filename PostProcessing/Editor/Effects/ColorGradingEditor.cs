using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.PostProcessing;

namespace UnityEditor.Experimental.PostProcessing
{
    [PostProcessEditor(typeof(ColorGrading))]
    public sealed class ColorGradingEditor : PostProcessEffectEditor<ColorGrading>
    {
        SerializedParameterOverride m_GradingMode;

        static GUIContent[] s_Curves =
        {
            new GUIContent("Master"),
            new GUIContent("Red"),
            new GUIContent("Green"),
            new GUIContent("Blue"),
            new GUIContent("Hue VS Hue"),
            new GUIContent("Hue VS Sat"),
            new GUIContent("Sat VS Sat"),
            new GUIContent("Lum VS Sat")
        };

        sealed class CurveData
        {
            public SerializedParameterOverride master;
            public SerializedParameterOverride red;
            public SerializedParameterOverride green;
            public SerializedParameterOverride blue;

            public SerializedParameterOverride hueVsHue;
            public SerializedParameterOverride hueVsSat;
            public SerializedParameterOverride satVsSat;
            public SerializedParameterOverride lumVsSat;

            // Internal references to the actual animation curves
            // Needed for the curve editor
            public SerializedProperty rawMaster;
            public SerializedProperty rawRed;
            public SerializedProperty rawGreen;
            public SerializedProperty rawBlue;

            public SerializedProperty rawHueVsHue;
            public SerializedProperty rawHueVsSat;
            public SerializedProperty rawSatVsSat;
            public SerializedProperty rawLumVsSat;

            public SerializedProperty currentEditingCurve;

            public CurveEditor curveEditor;
            public Dictionary<SerializedProperty, Color> curveDict;

            public void SetupCurve(SerializedProperty prop, Color color, uint minPointCount, bool loop)
            {
                var state = CurveEditor.CurveState.defaultState;
                state.color = color;
                state.visible = false;
                state.minPointCount = minPointCount;
                state.onlyShowHandlesOnSelection = true;
                state.zeroKeyConstantValue = 0.5f;
                state.loopInBounds = loop;
                curveEditor.Add(prop, state);
                curveDict.Add(prop, color);
            }
        }

        // -----------------------------------------------------------------------------------------
        // LDR settings

        sealed class LdrSettings
        {
            public SerializedParameterOverride lut;

            public SerializedParameterOverride temperature;
            public SerializedParameterOverride tint;

            public SerializedParameterOverride colorFilter;
            public SerializedParameterOverride hueShift;
            public SerializedParameterOverride saturation;
            public SerializedParameterOverride brightness;
            public SerializedParameterOverride contrast;

            public SerializedParameterOverride mixerRedOutRedIn;
            public SerializedParameterOverride mixerRedOutGreenIn;
            public SerializedParameterOverride mixerRedOutBlueIn;

            public SerializedParameterOverride mixerGreenOutRedIn;
            public SerializedParameterOverride mixerGreenOutGreenIn;
            public SerializedParameterOverride mixerGreenOutBlueIn;

            public SerializedParameterOverride mixerBlueOutRedIn;
            public SerializedParameterOverride mixerBlueOutGreenIn;
            public SerializedParameterOverride mixerBlueOutBlueIn;

            public SerializedParameterOverride lift;
            public SerializedParameterOverride gamma;
            public SerializedParameterOverride gain;

            public SerializedProperty mixerChannel;

            public CurveData curveData;
        }

        LdrSettings ldr;

        // -----------------------------------------------------------------------------------------
        // HDR settings

        // -----------------------------------------------------------------------------------------
        // Custom HDR settings

        sealed class CustomSettings
        {
            public SerializedParameterOverride m_LogLut;
        }

        CustomSettings custom;

        public override void OnEnable()
        {
            m_GradingMode = FindParameterOverride(x => x.gradingMode);

            ldr = new LdrSettings
            {
                lut = FindParameterOverride(x => x.ldrLut),

                temperature = FindParameterOverride(x => x.ldrTemperature),
                tint = FindParameterOverride(x => x.ldrTint),

                colorFilter = FindParameterOverride(x => x.ldrColorFilter),
                hueShift = FindParameterOverride(x => x.ldrHueShift),
                saturation = FindParameterOverride(x => x.ldrSaturation),
                brightness = FindParameterOverride(x => x.ldrBrightness),
                contrast = FindParameterOverride(x => x.ldrContrast),

                mixerRedOutRedIn = FindParameterOverride(x => x.ldrMixerRedOutRedIn),
                mixerRedOutGreenIn = FindParameterOverride(x => x.ldrMixerRedOutGreenIn),
                mixerRedOutBlueIn = FindParameterOverride(x => x.ldrMixerRedOutBlueIn),

                mixerGreenOutRedIn = FindParameterOverride(x => x.ldrMixerGreenOutRedIn),
                mixerGreenOutGreenIn = FindParameterOverride(x => x.ldrMixerGreenOutGreenIn),
                mixerGreenOutBlueIn = FindParameterOverride(x => x.ldrMixerGreenOutBlueIn),

                mixerBlueOutRedIn = FindParameterOverride(x => x.ldrMixerBlueOutRedIn),
                mixerBlueOutGreenIn = FindParameterOverride(x => x.ldrMixerBlueOutGreenIn),
                mixerBlueOutBlueIn = FindParameterOverride(x => x.ldrMixerBlueOutBlueIn),
                mixerChannel = serializedObject.FindProperty("m_LdrMixerChannel"),

                lift = FindParameterOverride(x => x.ldrLift),
                gamma = FindParameterOverride(x => x.ldrGamma),
                gain = FindParameterOverride(x => x.ldrGain)
            };

            ldr.curveData = new CurveData
            {
                master = FindParameterOverride(x => x.ldrMaster),
                red = FindParameterOverride(x => x.ldrRed),
                green = FindParameterOverride(x => x.ldrGreen),
                blue = FindParameterOverride(x => x.ldrBlue),

                hueVsHue = FindParameterOverride(x => x.ldrHueVsHue),
                hueVsSat = FindParameterOverride(x => x.ldrHueVsSat),
                satVsSat = FindParameterOverride(x => x.ldrSatVsSat),
                lumVsSat = FindParameterOverride(x => x.ldrLumVsSat),

                rawMaster = FindProperty(x => x.ldrMaster.value.curve),
                rawRed = FindProperty(x => x.ldrRed.value.curve),
                rawGreen = FindProperty(x => x.ldrGreen.value.curve),
                rawBlue = FindProperty(x => x.ldrBlue.value.curve),

                rawHueVsHue = FindProperty(x => x.ldrHueVsHue.value.curve),
                rawHueVsSat = FindProperty(x => x.ldrHueVsSat.value.curve),
                rawSatVsSat = FindProperty(x => x.ldrSatVsSat.value.curve),
                rawLumVsSat = FindProperty(x => x.ldrLumVsSat.value.curve),

                currentEditingCurve = serializedObject.FindProperty("m_LdrCurrentEditingCurve"),

                curveEditor = new CurveEditor(),
                curveDict = new Dictionary<SerializedProperty, Color>()
            };

            // Prepare the curve editor
            ldr.curveData.SetupCurve(ldr.curveData.rawMaster, new Color(1f, 1f, 1f), 2, false);
            ldr.curveData.SetupCurve(ldr.curveData.rawRed, new Color(1f, 0f, 0f), 2, false);
            ldr.curveData.SetupCurve(ldr.curveData.rawGreen, new Color(0f, 1f, 0f), 2, false);
            ldr.curveData.SetupCurve(ldr.curveData.rawBlue, new Color(0f, 0.5f, 1f), 2, false);
            ldr.curveData.SetupCurve(ldr.curveData.rawHueVsHue, new Color(1f, 1f, 1f), 0, true);
            ldr.curveData.SetupCurve(ldr.curveData.rawHueVsSat, new Color(1f, 1f, 1f), 0, true);
            ldr.curveData.SetupCurve(ldr.curveData.rawSatVsSat, new Color(1f, 1f, 1f), 0, false);
            ldr.curveData.SetupCurve(ldr.curveData.rawLumVsSat, new Color(1f, 1f, 1f), 0, false);

            custom = new CustomSettings
            {
                m_LogLut = FindParameterOverride(x => x.logLut),
            };
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_GradingMode);

            var gradingMode = (GradingMode)m_GradingMode.value.intValue;
            if (gradingMode == GradingMode.LowDefinitionRange)
                DoLdrGUI();
            else if (gradingMode == GradingMode.HighDefinitionRange)
                DoHdrGUI();
            else
                DoCustomHdrGUI();
            
            EditorGUILayout.Space();
        }

        void DoLdrGUI()
        {
            EditorGUILayout.HelpBox("Only GradingMode.CustomLogLUT works right now.", MessageType.Error);
            PropertyField(ldr.lut);
            
            EditorGUILayout.Space();
            EditorUtilities.DrawHeaderLabel("White Balance");
            
            PropertyField(ldr.temperature);
            PropertyField(ldr.tint);

            EditorGUILayout.Space();
            EditorUtilities.DrawHeaderLabel("Tone");
            
            PropertyField(ldr.colorFilter);
            PropertyField(ldr.hueShift);
            PropertyField(ldr.saturation);
            PropertyField(ldr.brightness);
            PropertyField(ldr.contrast);

            EditorGUILayout.Space();
            int currentChannel = ldr.mixerChannel.intValue;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Channel Mixer", GUIStyle.none, Styling.labelHeader);

                EditorGUI.BeginChangeCheck();
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayoutUtility.GetRect(14f, 18f, GUILayout.ExpandWidth(false)); // Dirty hack to do proper right column alignement
                        if (GUILayout.Toggle(currentChannel == 0, EditorUtilities.GetContent("Red|Red output channel."), EditorStyles.miniButtonLeft)) currentChannel = 0;
                        if (GUILayout.Toggle(currentChannel == 1, EditorUtilities.GetContent("Green|Green output channel."), EditorStyles.miniButtonMid)) currentChannel = 1;
                        if (GUILayout.Toggle(currentChannel == 2, EditorUtilities.GetContent("Blue|Blue output channel."), EditorStyles.miniButtonRight)) currentChannel = 2;
                    }
                }
                if (EditorGUI.EndChangeCheck())
                    GUI.FocusControl(null);
            }

            ldr.mixerChannel.intValue = currentChannel;

            if (currentChannel == 0)
            {
                PropertyField(ldr.mixerRedOutRedIn);
                PropertyField(ldr.mixerRedOutGreenIn);
                PropertyField(ldr.mixerRedOutBlueIn);
            }
            else if (currentChannel == 1)
            {
                PropertyField(ldr.mixerGreenOutRedIn);
                PropertyField(ldr.mixerGreenOutGreenIn);
                PropertyField(ldr.mixerGreenOutBlueIn);
            }
            else
            {
                PropertyField(ldr.mixerBlueOutRedIn);
                PropertyField(ldr.mixerBlueOutGreenIn);
                PropertyField(ldr.mixerBlueOutBlueIn);
            }

            EditorGUILayout.Space();
            EditorUtilities.DrawHeaderLabel("Trackballs");

            using (new EditorGUILayout.HorizontalScope())
            {
                PropertyField(ldr.lift);
                GUILayout.Space(4f);
                PropertyField(ldr.gamma);
                GUILayout.Space(4f);
                PropertyField(ldr.gain);
            }

            EditorGUILayout.Space();
            EditorUtilities.DrawHeaderLabel("Grading Curves");

            DoCurvesGUI(ldr.curveData);
        }

        void DoHdrGUI()
        {
            EditorGUILayout.HelpBox("Only GradingMode.CustomLogLUT works right now.", MessageType.Error);
        }

        void DoCustomHdrGUI()
        {
            PropertyField(custom.m_LogLut);

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
                        EditorUtilities.DrawFixMeBox("Invalid LUT import settings.", () => SetLogLutImportSettings(importer));
                }
            }
        }

        void SetLogLutImportSettings(TextureImporter importer)
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

        void ResetVisibleCurves(CurveData curveData)
        {
            foreach (var curve in curveData.curveDict)
            {
                var state = curveData.curveEditor.GetCurveState(curve.Key);
                state.visible = false;
                curveData.curveEditor.SetCurveState(curve.Key, state);
            }
        }

        void SetCurveVisible(SerializedProperty rawProp, SerializedProperty overrideProp, CurveData curveData)
        {
            var state = curveData.curveEditor.GetCurveState(rawProp);
            state.visible = true;
            state.editable = overrideProp.boolValue;
            curveData.curveEditor.SetCurveState(rawProp, state);
        }

        void CurveOverrideToggle(SerializedProperty overrideProp)
        {
            overrideProp.boolValue = GUILayout.Toggle(overrideProp.boolValue, EditorUtilities.GetContent("Override"), EditorStyles.toolbarButton);
        }

        static Material s_MaterialGrid;

        void DoCurvesGUI(CurveData curveData)
        {
            EditorGUILayout.Space();
            ResetVisibleCurves(curveData);

            using (new EditorGUI.DisabledGroupScope(serializedObject.isEditingMultipleObjects))
            {
                int curveEditingId = 0;
                SerializedProperty currentCurveRawProp = null;

                // Top toolbar
                using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    curveEditingId = EditorGUILayout.Popup(curveData.currentEditingCurve.intValue, s_Curves, EditorStyles.toolbarPopup, GUILayout.MaxWidth(150f));
                    curveEditingId = Mathf.Clamp(curveEditingId, 0, 7);
                    EditorGUILayout.Space();

                    switch (curveEditingId)
                    {
                        case 0:
                            CurveOverrideToggle(curveData.master.overrideState);
                            SetCurveVisible(curveData.rawMaster, curveData.master.overrideState, curveData);
                            currentCurveRawProp = curveData.rawMaster;
                            break;
                        case 1:
                            CurveOverrideToggle(curveData.red.overrideState);
                            SetCurveVisible(curveData.rawRed, curveData.red.overrideState, curveData);
                            currentCurveRawProp = curveData.rawRed;
                            break;
                        case 2:
                            CurveOverrideToggle(curveData.green.overrideState);
                            SetCurveVisible(curveData.rawGreen, curveData.green.overrideState, curveData);
                            currentCurveRawProp = curveData.rawGreen;
                            break;
                        case 3:
                            CurveOverrideToggle(curveData.blue.overrideState);
                            SetCurveVisible(curveData.rawBlue, curveData.blue.overrideState, curveData);
                            currentCurveRawProp = curveData.rawBlue;
                            break;
                        case 4:
                            CurveOverrideToggle(curveData.hueVsHue.overrideState);
                            SetCurveVisible(curveData.rawHueVsHue, curveData.hueVsHue.overrideState, curveData);
                            currentCurveRawProp = curveData.rawHueVsHue;
                            break;
                        case 5:
                            CurveOverrideToggle(curveData.hueVsSat.overrideState);
                            SetCurveVisible(curveData.rawHueVsSat, curveData.hueVsSat.overrideState, curveData);
                            currentCurveRawProp = curveData.rawHueVsSat;
                            break;
                        case 6:
                            CurveOverrideToggle(curveData.satVsSat.overrideState);
                            SetCurveVisible(curveData.rawSatVsSat, curveData.satVsSat.overrideState, curveData);
                            currentCurveRawProp = curveData.rawSatVsSat;
                            break;
                        case 7:
                            CurveOverrideToggle(curveData.lumVsSat.overrideState);
                            SetCurveVisible(curveData.rawLumVsSat, curveData.lumVsSat.overrideState, curveData);
                            currentCurveRawProp = curveData.rawLumVsSat;
                            break;
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
                    {
                        switch (curveEditingId)
                        {
                            case 0: curveData.rawMaster.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                                break;
                            case 1: curveData.rawRed.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                                break;
                            case 2: curveData.rawGreen.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                                break;
                            case 3: curveData.rawBlue.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                                break;
                            case 4: curveData.rawHueVsHue.animationCurveValue = new AnimationCurve();
                                break;
                            case 5: curveData.rawHueVsSat.animationCurveValue = new AnimationCurve();
                                break;
                            case 6: curveData.rawSatVsSat.animationCurveValue = new AnimationCurve();
                                break;
                            case 7: curveData.rawLumVsSat.animationCurveValue = new AnimationCurve();
                                break;
                        }
                    }

                    curveData.currentEditingCurve.intValue = curveEditingId;
                }

                // Curve area
                var settings = curveData.curveEditor.settings;
                var rect = GUILayoutUtility.GetAspectRect(2f);
                var innerRect = settings.padding.Remove(rect);

                if (Event.current.type == EventType.Repaint)
                {
                    // Background
                    EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

                    if (curveEditingId == 4 || curveEditingId == 5)
                        DrawBackgroundTexture(innerRect, 0);
                    else if (curveEditingId == 6 || curveEditingId == 7)
                        DrawBackgroundTexture(innerRect, 1);

                    // Bounds
                    Handles.color = Color.white * (GUI.enabled ? 1f : 0.5f);
                    Handles.DrawSolidRectangleWithOutline(innerRect, Color.clear, new Color(0.8f, 0.8f, 0.8f, 0.5f));

                    // Grid setup
                    Handles.color = new Color(1f, 1f, 1f, 0.05f);
                    int hLines = (int)Mathf.Sqrt(innerRect.width);
                    int vLines = (int)(hLines / (innerRect.width / innerRect.height));

                    // Vertical grid
                    int gridOffset = Mathf.FloorToInt(innerRect.width / hLines);
                    int gridPadding = ((int)(innerRect.width) % hLines) / 2;

                    for (int i = 1; i < hLines; i++)
                    {
                        var offset = i * Vector2.right * gridOffset;
                        offset.x += gridPadding;
                        Handles.DrawLine(innerRect.position + offset, new Vector2(innerRect.x, innerRect.yMax - 1) + offset);
                    }

                    // Horizontal grid
                    gridOffset = Mathf.FloorToInt(innerRect.height / vLines);
                    gridPadding = ((int)(innerRect.height) % vLines) / 2;

                    for (int i = 1; i < vLines; i++)
                    {
                        var offset = i * Vector2.up * gridOffset;
                        offset.y += gridPadding;
                        Handles.DrawLine(innerRect.position + offset, new Vector2(innerRect.xMax - 1, innerRect.y) + offset);
                    }
                }

                // Curve editor
                if (curveData.curveEditor.OnGUI(rect))
                {
                    Repaint();
                    GUI.changed = true;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    // Borders
                    Handles.color = Color.black;
                    Handles.DrawLine(new Vector2(rect.x, rect.y - 18f), new Vector2(rect.xMax, rect.y - 18f));
                    Handles.DrawLine(new Vector2(rect.x, rect.y - 19f), new Vector2(rect.x, rect.yMax));
                    Handles.DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax));
                    Handles.DrawLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.y - 18f));

                    bool editable = curveData.curveEditor.GetCurveState(currentCurveRawProp).editable;
                    string editableString = editable ? string.Empty : "(Not Overriding)\n";

                    // Selection info
                    var selection = curveData.curveEditor.GetSelection();
                    var infoRect = innerRect;
                    infoRect.x += 5f;
                    infoRect.width = 100f;
                    infoRect.height = 30f;

                    if (selection.curve != null && selection.keyframeIndex > -1)
                    {
                        var key = selection.keyframe.Value;
                        GUI.Label(infoRect, string.Format("{0}\n{1}", key.time.ToString("F3"), key.value.ToString("F3")), Styling.preLabel);
                    }
                    else
                    {
                        GUI.Label(infoRect, editableString, Styling.preLabel);
                    }
                }
            }

            EditorGUILayout.Space();
        }

        void DrawBackgroundTexture(Rect rect, int pass)
        {
            if (s_MaterialGrid == null)
                s_MaterialGrid = new Material(Shader.Find("Hidden/PostProcessing/Editor/CurveGrid")) { hideFlags = HideFlags.HideAndDontSave };

            float scale = EditorGUIUtility.pixelsPerPoint;

            var oldRt = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(Mathf.CeilToInt(rect.width * scale), Mathf.CeilToInt(rect.height * scale), 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            s_MaterialGrid.SetFloat("_DisabledState", GUI.enabled ? 1f : 0.5f);
            s_MaterialGrid.SetFloat("_PixelScaling", EditorGUIUtility.pixelsPerPoint);

            Graphics.Blit(null, rt, s_MaterialGrid, pass);
            RenderTexture.active = oldRt;

            GUI.DrawTexture(rect, rt);
            RenderTexture.ReleaseTemporary(rt);
        }
    }
}
