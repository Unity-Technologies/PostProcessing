using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [PostProcessEditor(typeof(SunShafts))]
    internal sealed class SunShaftsEditor : PostProcessEffectEditor<SunShafts>
    {
        SerializedParameterOverride sunPosition;
        SerializedParameterOverride radialBlurIterations;
        SerializedParameterOverride shaftsColor;
        SerializedParameterOverride thresholdColor;
        SerializedParameterOverride blurRadius;
        SerializedParameterOverride intensity;
        SerializedParameterOverride resolution;
        SerializedParameterOverride screenBlendMode;
        SerializedParameterOverride distanceFalloff;

        public override void OnEnable()
        {
            sunPosition = FindParameterOverride(x => x.sunPosition);
            radialBlurIterations = FindParameterOverride(x => x.radialBlurIterations);
            shaftsColor = FindParameterOverride(x => x.shaftsColor);
            thresholdColor = FindParameterOverride(x => x.thresholdColor);
            blurRadius = FindParameterOverride(x => x.blurRadius);
            intensity = FindParameterOverride(x => x.intensity);
            resolution = FindParameterOverride(x => x.resolution);
            screenBlendMode = FindParameterOverride(x => x.screenBlendMode);
            distanceFalloff = FindParameterOverride(x => x.distanceFalloff);
        }

        public override void OnInspectorGUI()
        {
            PropertyField(sunPosition);
            PropertyField(resolution);
            PropertyField(screenBlendMode);
            PropertyField(thresholdColor);
            PropertyField(shaftsColor);
            PropertyField(distanceFalloff);

            PropertyField(blurRadius);
            PropertyField(radialBlurIterations);

            PropertyField(intensity);
        }
    }
}