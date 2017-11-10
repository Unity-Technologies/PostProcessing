using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.PostProcessing
{
    using SceneManagement;
    using UnityObject = UnityEngine.Object;

    public static class RuntimeUtilities
    {
        #region Textures

        static Texture2D m_WhiteTexture;
        public static Texture2D whiteTexture
        {
            get
            {
                if (m_WhiteTexture == null)
                {
                    m_WhiteTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    m_WhiteTexture.SetPixel(0, 0, Color.white);
                    m_WhiteTexture.Apply();
                }

                return m_WhiteTexture;
            }
        }

        static Texture2D m_BlackTexture;
        public static Texture2D blackTexture
        {
            get
            {
                if (m_BlackTexture == null)
                {
                    m_BlackTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    m_BlackTexture.SetPixel(0, 0, Color.black);
                    m_BlackTexture.Apply();
                }

                return m_BlackTexture;
            }
        }

        static Texture2D m_TransparentTexture;
        public static Texture2D transparentTexture
        {
            get
            {
                if (m_TransparentTexture == null)
                {
                    m_TransparentTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    m_TransparentTexture.SetPixel(0, 0, Color.clear);
                    m_TransparentTexture.Apply();
                }

                return m_TransparentTexture;
            }
        }

        #endregion

        #region Rendering

        static Mesh s_FullscreenTriangle;
        public static Mesh fullscreenTriangle
        {
            get
            {
                if (s_FullscreenTriangle != null)
                    return s_FullscreenTriangle;

                s_FullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };

                // Because we have to support older platforms (GLES2/3, DX9 etc) we can't do all of
                // this directly in the vertex shader using vertex ids :(
                s_FullscreenTriangle.SetVertices(new List<Vector3>
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(-1f,  3f, 0f),
                    new Vector3( 3f, -1f, 0f)
                });
                s_FullscreenTriangle.SetIndices(new [] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
                s_FullscreenTriangle.UploadMeshData(false);

                return s_FullscreenTriangle;
            }
        }

        static Material s_CopyStdMaterial;
        public static Material copyStdMaterial
        {
            get
            {
                if (s_CopyStdMaterial != null)
                    return s_CopyStdMaterial;

                var shader = Shader.Find("Hidden/PostProcessing/CopyStd");
                s_CopyStdMaterial = new Material(shader)
                {
                    name = "PostProcess - CopyStd",
                    hideFlags = HideFlags.HideAndDontSave
                };

                return s_CopyStdMaterial;
            }
        }

        static Material s_CopyMaterial;
        public static Material copyMaterial
        {
            get
            {
                if (s_CopyMaterial != null)
                    return s_CopyMaterial;

                var shader = Shader.Find("Hidden/PostProcessing/Copy");
                s_CopyMaterial = new Material(shader)
                {
                    name = "PostProcess - Copy",
                    hideFlags = HideFlags.HideAndDontSave
                };

                return s_CopyMaterial;
            }
        }

        static PropertySheet s_CopySheet;
        public static PropertySheet copySheet
        {
            get
            {
                if (s_CopySheet == null)
                    s_CopySheet = new PropertySheet(copyMaterial);

                return s_CopySheet;
            }
        }

        // Use a custom blit method to draw a fullscreen triangle instead of a fullscreen quad
        // https://michaldrobot.com/2014/04/01/gcn-execution-patterns-in-full-screen-passes/
        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, bool clear = false)
        {
            cmd.SetGlobalTexture(ShaderIDs.MainTex, source);
            cmd.SetRenderTarget(destination);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, copyMaterial, 0, 0);
        }

        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, PropertySheet propertySheet, int pass, bool clear = false)
        {
            cmd.SetGlobalTexture(ShaderIDs.MainTex, source);
            cmd.SetRenderTarget(destination);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, propertySheet.material, 0, pass, propertySheet.properties);
        }

        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, RenderTargetIdentifier depth, PropertySheet propertySheet, int pass, bool clear = false)
        {
            cmd.SetGlobalTexture(ShaderIDs.MainTex, source);
            cmd.SetRenderTarget(destination, depth);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, propertySheet.material, 0, pass, propertySheet.properties);
        }

        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier[] destinations, RenderTargetIdentifier depth, PropertySheet propertySheet, int pass, bool clear = false)
        {
            cmd.SetGlobalTexture(ShaderIDs.MainTex, source);
            cmd.SetRenderTarget(destinations, depth);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, propertySheet.material, 0, pass, propertySheet.properties);
        }

        public static void BlitFullscreenTriangle(Texture source, RenderTexture destination, Material material, int pass)
        {
            var oldRt = RenderTexture.active;

            material.SetPass(pass);
            if (source != null)
                material.SetTexture(ShaderIDs.MainTex, source);

            Graphics.SetRenderTarget(destination);
            Graphics.DrawMeshNow(fullscreenTriangle, Matrix4x4.identity);
            RenderTexture.active = oldRt;
        }

        // Fast basic copy texture if available, falls back to blit copy if not
        // Assumes that both textures have the exact same type and format
        public static void CopyTexture(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination)
        {
            if (SystemInfo.copyTextureSupport > CopyTextureSupport.None)
            {
                cmd.CopyTexture(source, destination);
                return;
            }

            cmd.BlitFullscreenTriangle(source, destination);
        }

        // TODO: Generalize the GetTemporaryRT and Blit commands in order to support
        // RT Arrays for Stereo Instancing/MultiView

        #endregion

        #region Unity specifics & misc methods

        public static bool scriptableRenderPipelineActive
        {
            get { return GraphicsSettings.renderPipelineAsset != null; } // 5.6+ only
        }

#if UNITY_EDITOR
        public static bool isSinglePassStereoSelected
        {
            get
            {
                return UnityEditor.PlayerSettings.virtualRealitySupported
                    && UnityEditor.PlayerSettings.stereoRenderingPath == UnityEditor.StereoRenderingPath.SinglePass;
            }
        }
#endif

        // TODO: Check for SPSR support at runtime
        public static bool isSinglePassStereoEnabled
        {
            get
            {
#if UNITY_EDITOR
                return isSinglePassStereoSelected && Application.isPlaying;
#elif UNITY_2017_2_OR_NEWER
                return UnityEngine.XR.XRSettings.eyeTextureDesc.vrUsage == VRTextureUsage.TwoEyes;
#else
                return false;
#endif
            }
        }

        public static bool isVREnabled
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.PlayerSettings.virtualRealitySupported;
#elif UNITY_XBOXONE
                return false;
#elif UNITY_2017_2_OR_NEWER
                return UnityEngine.XR.XRSettings.enabled;
#elif UNITY_5_6_OR_NEWER
                return UnityEngine.VR.VRSettings.enabled;
#endif
            }
        }

        public static bool isAndroidOpenGL
        {
            get { return Application.platform == RuntimePlatform.Android && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan; }
        }

        public static void Destroy(UnityObject obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    UnityObject.Destroy(obj);
                else
                    UnityObject.DestroyImmediate(obj);
#else
                UnityObject.Destroy(obj);
#endif
            }
        }

        public static bool isLinearColorSpace
        {
            get { return QualitySettings.activeColorSpace == ColorSpace.Linear; }
        }

        public static bool IsResolvedDepthAvailable(Camera camera)
        {
            // AFAIK resolved depth is only available on D3D11/12 via BuiltinRenderTextureType.ResolvedDepth
            // TODO: Is there more proper way to determine this? What about SRPs?
            var gtype = SystemInfo.graphicsDeviceType;
            return camera.actualRenderingPath == RenderingPath.DeferredShading &&
                (gtype == GraphicsDeviceType.Direct3D11 || gtype == GraphicsDeviceType.Direct3D12 || gtype == GraphicsDeviceType.XboxOne);
        }

        public static void DestroyProfile(PostProcessProfile profile, bool destroyEffects)
        {
            if (destroyEffects)
            {
                foreach (var effect in profile.settings)
                    Destroy(effect);
            }

            Destroy(profile);
        }

        public static void DestroyVolume(PostProcessVolume volume, bool destroySharedProfile)
        {
            if (destroySharedProfile)
                DestroyProfile(volume.sharedProfile, true);

            Destroy(volume);
        }

        // Returns ALL scene objects in the hierarchy, included inactive objects
        // Beware, this method will be slow for big scenes
        public static IEnumerable<T> GetAllSceneObjects<T>()
            where T : Component
        {
            var queue = new Queue<Transform>();
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (var root in roots)
            {
                queue.Enqueue(root.transform);
                var comp = root.GetComponent<T>();

                if (comp != null)
                    yield return comp;
            }

            while (queue.Count > 0)
            {
                foreach (Transform child in queue.Dequeue())
                {
                    queue.Enqueue(child);
                    var comp = child.GetComponent<T>();

                    if (comp != null)
                        yield return comp;
                }
            }
        }

        public static void CreateIfNull<T>(ref T obj)
            where T : class, new()
        {
            if (obj == null)
                obj = new T();
        }

        #endregion

        #region Maths

        public static float Exp2(float x)
        {
            return Mathf.Exp(x * 0.69314718055994530941723212145818f);
        }

        // Adapted heavily from PlayDead's TAA code
        // https://github.com/playdeadgames/temporal/blob/master/Assets/Scripts/Extensions.cs
        public static Matrix4x4 GetJitteredPerspectiveProjectionMatrix(Camera camera, Vector2 offset)
        {
            float vertical = Mathf.Tan(0.5f * Mathf.Deg2Rad * camera.fieldOfView);
            float horizontal = vertical * camera.aspect;
            float near = camera.nearClipPlane;
            float far = camera.farClipPlane;

            offset.x *= horizontal / (0.5f * camera.pixelWidth);
            offset.y *= vertical / (0.5f * camera.pixelHeight);

            float left = (offset.x - horizontal) * near;
            float right = (offset.x + horizontal) * near;
            float top = (offset.y + vertical) * near;
            float bottom = (offset.y - vertical) * near;

            var matrix = new Matrix4x4();

            matrix[0, 0] = (2f * near) / (right - left);
            matrix[0, 1] = 0f;
            matrix[0, 2] = (right + left) / (right - left);
            matrix[0, 3] = 0f;

            matrix[1, 0] = 0f;
            matrix[1, 1] = (2f * near) / (top - bottom);
            matrix[1, 2] = (top + bottom) / (top - bottom);
            matrix[1, 3] = 0f;

            matrix[2, 0] = 0f;
            matrix[2, 1] = 0f;
            matrix[2, 2] = -(far + near) / (far - near);
            matrix[2, 3] = -(2f * far * near) / (far - near);

            matrix[3, 0] = 0f;
            matrix[3, 1] = 0f;
            matrix[3, 2] = -1f;
            matrix[3, 3] = 0f;

            return matrix;
        }

        public static Matrix4x4 GetJitteredOrthographicProjectionMatrix(Camera camera, Vector2 offset)
        {
            float vertical = camera.orthographicSize;
            float horizontal = vertical * camera.aspect;

            offset.x *= horizontal / (0.5f * camera.pixelWidth);
            offset.y *= vertical / (0.5f * camera.pixelHeight);

            float left = offset.x - horizontal;
            float right = offset.x + horizontal;
            float top = offset.y + vertical;
            float bottom = offset.y - vertical;

            return Matrix4x4.Ortho(left, right, bottom, top, camera.nearClipPlane, camera.farClipPlane);
        }

        public static Matrix4x4 GenerateJitteredProjectionMatrixFromOriginal(PostProcessRenderContext context, Matrix4x4 origProj, Vector2 jitter)
        {
#if UNITY_2017_2_OR_NEWER
            var planes = origProj.decomposeProjection;

            float vertFov = Math.Abs(planes.top) + Math.Abs(planes.bottom);
            float horizFov = Math.Abs(planes.left) + Math.Abs(planes.right);

            var planeJitter = new Vector2(jitter.x * horizFov / context.screenWidth,
                                          jitter.y * vertFov / context.screenHeight);

            planes.left += planeJitter.x;
            planes.right += planeJitter.x;
            planes.top += planeJitter.y;
            planes.bottom += planeJitter.y;

            var jitteredMatrix = Matrix4x4.Frustum(planes);

            return jitteredMatrix;
#else
            var rTan = (1.0f + origProj[0, 2]) / origProj[0, 0];
            var lTan = (-1.0f + origProj[0, 2]) / origProj[0, 0];

            var tTan = (1.0f + origProj[1, 2]) / origProj[1, 1];
            var bTan = (-1.0f + origProj[1, 2]) / origProj[1, 1];

            float tanVertFov = Math.Abs(tTan) + Math.Abs(bTan);
            float tanHorizFov = Math.Abs(lTan) + Math.Abs(rTan);

            jitter.x *= tanHorizFov / context.screenWidth;
            jitter.y *= tanVertFov / context.screenHeight;

            float left = jitter.x + lTan;
            float right = jitter.x + rTan;
            float top = jitter.y + tTan;
            float bottom = jitter.y + bTan;

            var jitteredMatrix = new Matrix4x4();

            jitteredMatrix[0, 0] = 2f / (right - left);
            jitteredMatrix[0, 1] = 0f;
            jitteredMatrix[0, 2] = (right + left) / (right - left);
            jitteredMatrix[0, 3] = 0f;

            jitteredMatrix[1, 0] = 0f;
            jitteredMatrix[1, 1] = 2f / (top - bottom);
            jitteredMatrix[1, 2] = (top + bottom) / (top - bottom);
            jitteredMatrix[1, 3] = 0f;

            jitteredMatrix[2, 0] = 0f;
            jitteredMatrix[2, 1] = 0f;
            jitteredMatrix[2, 2] = origProj[2, 2];
            jitteredMatrix[2, 3] = origProj[2, 3];

            jitteredMatrix[3, 0] = 0f;
            jitteredMatrix[3, 1] = 0f;
            jitteredMatrix[3, 2] = -1f;
            jitteredMatrix[3, 3] = 0f;

            return jitteredMatrix;
#endif
        }

        #endregion

        #region Reflection

        // Quick extension method to get the first attribute of type T on a given Type
        public static T GetAttribute<T>(this Type type) where T : Attribute
        {
            Assert.IsTrue(type.IsDefined(typeof(T), false), "Attribute not found");
            return (T)type.GetCustomAttributes(typeof(T), false)[0];
        }

        // Returns all attributes set on a specific member
        // Note: doesn't include inherited attributes, only explicit ones
        public static Attribute[] GetMemberAttributes<TType, TValue>(Expression<Func<TType, TValue>> expr)
        {
            Expression body = expr;

            if (body is LambdaExpression)
                body = ((LambdaExpression)body).Body;

            switch (body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    var fi = (FieldInfo)((MemberExpression)body).Member;
                    return fi.GetCustomAttributes(false).Cast<Attribute>().ToArray();
                default:
                    throw new InvalidOperationException();
            }
        }

        // Returns a string path from an expression - mostly used to retrieve serialized properties
        // without hardcoding the field path. Safer, and allows for proper refactoring.
        public static string GetFieldPath<TType, TValue>(Expression<Func<TType, TValue>> expr)
        {
            MemberExpression me;
            switch (expr.Body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    me = expr.Body as MemberExpression;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            var members = new List<string>();
            while (me != null)
            {
                members.Add(me.Member.Name);
                me = me.Expression as MemberExpression;
            }

            var sb = new StringBuilder();
            for (int i = members.Count - 1; i >= 0; i--)
            {
                sb.Append(members[i]);
                if (i > 0) sb.Append('.');
            }

            return sb.ToString();
        }

        public static object GetParentObject(string path, object obj)
        {
            var fields = path.Split('.');

            if (fields.Length == 1)
                return obj;

            var info = obj.GetType().GetField(fields[0], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            obj = info.GetValue(obj);

            return GetParentObject(string.Join(".", fields, 1, fields.Length - 1), obj);
        }

#endregion
    }
}
