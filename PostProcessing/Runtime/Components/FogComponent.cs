using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.PostProcessing
{
    public sealed class FogComponent : PostProcessingComponentCommandBuffer<FogModel>
    {
        static class Uniforms
        {
            // Fog shader uniforms
            internal static readonly int _FogColor             = Shader.PropertyToID("_FogColor");
            internal static readonly int _Density_Start_End    = Shader.PropertyToID("_Density_Start_End");
            internal static readonly int _TempRT               = Shader.PropertyToID("_TempRT");
            internal static readonly int _SkyCubemap           = Shader.PropertyToID("_SkyCubemap");
            internal static readonly int _SkyTint              = Shader.PropertyToID("_SkyTint");
            internal static readonly int _SkyExposure_Rotation = Shader.PropertyToID("_SkyExposure_Rotation");
            internal static readonly int _MainTex              = Shader.PropertyToID("_MainTex");

            // Skybox shader uniforms
            internal static readonly int _Tex                  = Shader.PropertyToID("_Tex");
            internal static readonly int _Tint                 = Shader.PropertyToID("_Tint");
            internal static readonly int _Exposure             = Shader.PropertyToID("_Exposure");
            internal static readonly int _Rotation             = Shader.PropertyToID("_Rotation");
        }

        const string k_ShaderString = "Hidden/Post FX/Fog";

        Mesh quad;
        List<Vector3> texcoord1 = new List<Vector3>(new Vector3[4]);

        public override bool active
        {
            get
            {
                return model.enabled
                       && context.isGBufferAvailable // In forward, fog is already done at shader level
                       && RenderSettings.fog
                       && !context.interrupted;
            }
        }

        public override string GetName()
        {
            return "Fog";
        }

        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth;
        }

        public override CameraEvent GetCameraEvent()
        {
            return CameraEvent.BeforeImageEffectsOpaque;
        }

        public override void PopulateCommandBuffer(CommandBuffer cb)
        {
            var settings = model.settings;

            var material = context.materialFactory.Get(k_ShaderString);
            material.shaderKeywords = null;
            material.SetColor(Uniforms._FogColor, RenderSettings.fogColor);
            material.SetVector(Uniforms._Density_Start_End,
                new Vector3(RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance));

            if (quad == null)
            {
                quad = new Mesh()
                {
                    vertices = new Vector3[]
                    {
                        new Vector3(-1f, -1f, 0f),
                        new Vector3( 1f, -1f, 0f),
                        new Vector3( 1f,  1f, 0f),
                        new Vector3(-1f,  1f, 0f)
                    },
                    uv = new Vector2[]
                    {
                        new Vector2(0, 1),
                        new Vector2(1, 1),
                        new Vector2(1, 0),
                        new Vector2(0, 0),
                    },
                };
                quad.SetIndices(new int[] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);
                quad.MarkDynamic();
            }

            Material skybox = RenderSettings.skybox;
            Texture cubemap = null;
            int pass = (int)model.settings.skyboxBehaviour;
            if (model.settings.skyboxBehaviour == FogModel.Settings.SkyboxBehaviour.FadeTo
                && skybox != null && (cubemap = skybox.GetTexture(Uniforms._Tex)) != null)
            {
                // Calculate vectors towards frustum corners.
                var cam = context.camera;
                var camtr = cam.transform;
                var camNear = cam.nearClipPlane;
                var camFar = cam.farClipPlane;

                var tanHalfFov = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad / 2);
                var toRight = camtr.right * camNear * tanHalfFov * cam.aspect;
                var toTop = camtr.up * camNear * tanHalfFov;

                var origin = camtr.forward * camNear;
                var v_tl = origin - toRight + toTop;
                var v_tr = origin + toRight + toTop;
                var v_br = origin + toRight - toTop;
                var v_bl = origin - toRight - toTop;

                var v_s = v_tl.magnitude * camFar / camNear;

                texcoord1[0] = v_tl.normalized * v_s;
                texcoord1[1] = v_tr.normalized * v_s;
                texcoord1[2] = v_br.normalized * v_s;
                texcoord1[3] = v_bl.normalized * v_s;

                quad.SetUVs(1, texcoord1);

                float exposure = skybox.GetFloat(Uniforms._Exposure);
                float rotation = Mathf.Deg2Rad * skybox.GetFloat(Uniforms._Rotation);
                material.SetVector(Uniforms._SkyExposure_Rotation, new Vector2(exposure, -rotation));
                material.SetColor(Uniforms._SkyTint, skybox.GetColor(Uniforms._Tint).linear);
                material.SetTexture(Uniforms._SkyCubemap, cubemap);
            }
            else if (pass == 2)
            {
                pass = 1; // default to exclude
            }

            switch (RenderSettings.fogMode)
            {
                case FogMode.Linear:
                    material.EnableKeyword("FOG_LINEAR");
                    break;
                case FogMode.Exponential:
                    material.EnableKeyword("FOG_EXP");
                    break;
                case FogMode.ExponentialSquared:
                    material.EnableKeyword("FOG_EXP2");
                    break;
            }

            var fbFormat = context.isHdr
                ? RenderTextureFormat.DefaultHDR
                : RenderTextureFormat.Default;

            cb.GetTemporaryRT(Uniforms._TempRT, context.width, context.height, 0, FilterMode.Point, fbFormat);
            cb.Blit(BuiltinRenderTextureType.CameraTarget, Uniforms._TempRT);
            cb.SetGlobalTexture(Uniforms._MainTex, Uniforms._TempRT);
            cb.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            cb.DrawMesh(quad, Matrix4x4.identity, material, 0, (int)settings.skyboxBehaviour);
            cb.ReleaseTemporaryRT(Uniforms._TempRT);
        }
    }
}
