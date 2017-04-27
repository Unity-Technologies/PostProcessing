using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.PostProcessing
{
    static class MeshUtilities
    {
        static Dictionary<PrimitiveType, Mesh> s_Primitives;
        static Dictionary<Type, PrimitiveType> s_ColliderPrimitives;

        static Mesh s_Quad;
        public static Mesh quad
        {
            get
            {
                if (s_Quad != null)
                    return s_Quad;

                var vertices = new[]
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3( 1f,  1f, 0f),
                    new Vector3( 1f, -1f, 0f),
                    new Vector3(-1f,  1f, 0f)
                };

                var uvs = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f)
                };

                var indices = new[] { 0, 1, 2, 1, 0, 3 };

                s_Quad = new Mesh
                {
                    vertices = vertices,
                    uv = uvs,
                    triangles = indices
                };
                s_Quad.RecalculateNormals();
                s_Quad.RecalculateBounds();

                return s_Quad;
            }
        }

        static MeshUtilities()
        {
            s_Primitives = new Dictionary<PrimitiveType, Mesh>();
            s_ColliderPrimitives = new Dictionary<Type, PrimitiveType>
            {
                { typeof(BoxCollider), PrimitiveType.Cube },
                { typeof(SphereCollider), PrimitiveType.Sphere },
                { typeof(CapsuleCollider), PrimitiveType.Capsule }
            };
        }

        internal static Mesh GetColliderMesh(Collider collider)
        {
            var type = collider.GetType();

            if (type == typeof(MeshCollider))
                return ((MeshCollider)collider).sharedMesh;

            Assert.IsTrue(s_ColliderPrimitives.ContainsKey(type), "Unknown collider");
            return GetPrimitive(s_ColliderPrimitives[type]);
        }

        internal static Mesh GetPrimitive(PrimitiveType primitiveType)
        {
            Mesh mesh;

            if (!s_Primitives.TryGetValue(primitiveType, out mesh))
            {
                mesh = GetBuiltinMesh(primitiveType);
                s_Primitives.Add(primitiveType, mesh);
            }

            return mesh;
        }

        // (Not pretty) hack to get meshes from `unity default resources` in user land
        // What it does is create a new GameObject using the CreatePrimitive utility, retrieve its
        // mesh and discard it...
        static Mesh GetBuiltinMesh(PrimitiveType primitiveType)
        {
            var gameObject = GameObject.CreatePrimitive(primitiveType);
            var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            RuntimeUtilities.Destroy(gameObject);
            return mesh;
        }
    }
}
