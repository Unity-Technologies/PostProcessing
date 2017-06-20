using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.VR;

namespace UnityEngine.Experimental.PostProcessing
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

        // Use a custom blit method to draw a fullscreen triangle instead of a fullscreen quad
        // https://michaldrobot.com/2014/04/01/gcn-execution-patterns-in-full-screen-passes/
        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, bool clear = false)
        {
            cmd.SetGlobalTexture(Uniforms._MainTex, source);
            cmd.SetRenderTarget(destination);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, copyMaterial, 0, 0);
        }

        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, PropertySheet propertySheet, int pass, bool clear = false)
        {
            cmd.SetGlobalTexture(Uniforms._MainTex, source);
            cmd.SetRenderTarget(destination);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, propertySheet.material, 0, pass, propertySheet.properties);
        }

        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, RenderTargetIdentifier depth, PropertySheet propertySheet, int pass, bool clear = false)
        {
            cmd.SetGlobalTexture(Uniforms._MainTex, source);
            cmd.SetRenderTarget(destination, depth);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, propertySheet.material, 0, pass, propertySheet.properties);
        }

        public static void BlitFullscreenTriangle(this CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier[] destinations, RenderTargetIdentifier depth, PropertySheet propertySheet, int pass, bool clear = false)
        {
            cmd.SetGlobalTexture(Uniforms._MainTex, source);
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
                material.SetTexture(Uniforms._MainTex, source);

            Graphics.SetRenderTarget(destination);
            Graphics.DrawMeshNow(fullscreenTriangle, Matrix4x4.identity);
            RenderTexture.active = oldRt;
        }

        // Fast basic copy texture if available, falls back to blit copy if not
        // Assumes that both textures have the exact same type and format
        public static void CopyTexture(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination)
        {
            if (SystemInfo.copyTextureSupport > CopyTextureSupport.None)
                cmd.CopyTexture(source, destination);
            else
                cmd.BlitFullscreenTriangle(source, destination);
        }

        #endregion

        #region Unity specifics

        public static bool scriptableRenderPipelineActive
        {
            get { return GraphicsSettings.renderPipelineAsset != null; } // 5.6+ only
        }

        public static bool isSinglePassStereoEnabled
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.PlayerSettings.virtualRealitySupported
                    && UnityEditor.PlayerSettings.stereoRenderingPath == UnityEditor.StereoRenderingPath.SinglePass;
#else
                return false; // TODO: Check for SPSR support at runtime
#endif
            }
        }

        public static bool isVREnabled
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.PlayerSettings.virtualRealitySupported;
#else
                return VRSettings.enabled;
#endif
            }
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

        #endregion

        #region Maths

        public static float Exp2(float x)
        {
            return Mathf.Exp(x * 0.69314718055994530941723212145818f);
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
