using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GridKeeper.EditorTools
{
    public static class LowPolyMeshes
    {
        private const string Folder = "Assets/GridKeeper/Art/Meshes";

        public static Mesh Box => GetOrCreate("Box", BuildBox);
        public static Mesh Prism => GetOrCreate("Prism", BuildPrism);
        public static Mesh Cone6 => GetOrCreate("Cone6", () => BuildCone(6));
        public static Mesh Cylinder8 => GetOrCreate("Cylinder8", () => BuildCylinder(8));

        private static Mesh GetOrCreate(string name, System.Func<Mesh> build)
        {
            string path = Folder + "/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;

            Directory.CreateDirectory(Folder);
            mesh = build();
            mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Mesh BuildBox()
        {
            var b = new Builder();
            Vector3[] c =
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 1f, -0.5f), new Vector3(-0.5f, 1f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 1f, 0.5f), new Vector3(-0.5f, 1f, 0.5f)
            };
            b.Quad(c[0], c[3], c[2], c[1]);
            b.Quad(c[5], c[6], c[7], c[4]);
            b.Quad(c[4], c[7], c[3], c[0]);
            b.Quad(c[1], c[2], c[6], c[5]);
            b.Quad(c[3], c[7], c[6], c[2]);
            b.Quad(c[4], c[0], c[1], c[5]);
            return b.ToMesh();
        }

        private static Mesh BuildPrism()
        {
            var b = new Builder();
            Vector3 fl = new Vector3(-0.5f, 0f, -0.5f), fr = new Vector3(0.5f, 0f, -0.5f), ft = new Vector3(0f, 1f, -0.5f);
            Vector3 bl = new Vector3(-0.5f, 0f, 0.5f), br = new Vector3(0.5f, 0f, 0.5f), bt = new Vector3(0f, 1f, 0.5f);
            b.Tri(fl, ft, fr);
            b.Tri(br, bt, bl);
            b.Quad(bl, bt, ft, fl);
            b.Quad(fr, ft, bt, br);
            b.Quad(bl, fl, fr, br);
            return b.ToMesh();
        }

        private static Mesh BuildCone(int sides)
        {
            var b = new Builder();
            Vector3 apex = new Vector3(0f, 1f, 0f);
            for (int i = 0; i < sides; i++)
            {
                Vector3 a = Ring(i, sides, 0f);
                Vector3 c = Ring(i + 1, sides, 0f);
                b.Tri(a, apex, c);
                b.Tri(c, Vector3.zero, a);
            }
            return b.ToMesh();
        }

        private static Mesh BuildCylinder(int sides)
        {
            var b = new Builder();
            Vector3 top = Vector3.up;
            for (int i = 0; i < sides; i++)
            {
                Vector3 a0 = Ring(i, sides, 0f), a1 = Ring(i + 1, sides, 0f);
                Vector3 b0 = Ring(i, sides, 1f), b1 = Ring(i + 1, sides, 1f);
                b.Quad(a0, b0, b1, a1);
                b.Tri(b0, top, b1);
                b.Tri(a1, Vector3.zero, a0);
            }
            return b.ToMesh();
        }

        private static Vector3 Ring(int i, int sides, float y)
        {
            float angle = i * Mathf.PI * 2f / sides;
            return new Vector3(Mathf.Cos(angle) * 0.5f, y, Mathf.Sin(angle) * 0.5f);
        }

        private sealed class Builder
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<int> triangles = new List<int>();

            public void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                int start = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                Tri(a, b, c);
                Tri(a, c, d);
            }

            public Mesh ToMesh()
            {
                var mesh = new Mesh();
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mesh.UploadMeshData(false);
                return mesh;
            }
        }
    }
}
