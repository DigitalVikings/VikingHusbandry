using System.Collections.Generic;
using UnityEngine;

namespace VikingHusbandry.Features.Trough
{
    public static class TroughMeshBuilder
    {
        private static Mesh? s_troughMesh;
        private static Mesh? s_feedMesh;

        public static Mesh GetOrCreateTroughMesh()
        {
            if (s_troughMesh != null) return s_troughMesh;

            Mesh mesh = new Mesh { name = "VH_ProceduralTroughMesh" };

            List<Vector3> verts = new();
            List<Vector3> norms = new();
            List<Vector2> uvs = new();
            List<int> tris = new();

            // Dimensions:
            // Basin X = 1.20m, Z = 0.60m, Basin Y = 0.18m to 0.52m (height 0.34m)
            // 4 Corner Posts: width = 0.08m, depth = 0.08m, total height = 0.54m from ground (y=0)

            float postW = 0.08f;
            float postH = 0.54f;
            float postHalfX = 0.56f;
            float postHalfZ = 0.26f;

            // 1. Four Corner Posts
            AddBox(verts, norms, uvs, tris, new Vector3(-postHalfX, postH * 0.5f, -postHalfZ), new Vector3(postW, postH, postW));
            AddBox(verts, norms, uvs, tris, new Vector3(postHalfX, postH * 0.5f, -postHalfZ), new Vector3(postW, postH, postW));
            AddBox(verts, norms, uvs, tris, new Vector3(-postHalfX, postH * 0.5f, postHalfZ), new Vector3(postW, postH, postW));
            AddBox(verts, norms, uvs, tris, new Vector3(postHalfX, postH * 0.5f, postHalfZ), new Vector3(postW, postH, postW));

            // 2. Bottom Cross Runners / Skids under the posts (along Z at each end)
            float runnerH = 0.06f;
            float runnerW = 0.07f;
            float runnerL = (postHalfZ * 2f) + postW + 0.04f;
            AddBox(verts, norms, uvs, tris, new Vector3(-postHalfX, runnerH * 0.5f, 0f), new Vector3(runnerW, runnerH, runnerL));
            AddBox(verts, norms, uvs, tris, new Vector3(postHalfX, runnerH * 0.5f, 0f), new Vector3(runnerW, runnerH, runnerL));

            // 3. Trough Bottom Floor Plank (elevated at y = 0.18m)
            float floorY = 0.18f;
            float floorThick = 0.04f;
            float innerLength = (postHalfX * 2f) - postW;
            float innerWidth = (postHalfZ * 2f) - postW;
            AddBox(verts, norms, uvs, tris, new Vector3(0f, floorY + (floorThick * 0.5f), 0f), new Vector3(innerLength, floorThick, innerWidth));

            // 4. Front & Back Side Planks (slanted or straight vertical timber walls)
            float wallH = postH - floorY;
            float wallThick = 0.045f;
            float wallCenterY = floorY + (wallH * 0.5f);
            AddBox(verts, norms, uvs, tris, new Vector3(0f, wallCenterY, -postHalfZ), new Vector3(innerLength, wallH, wallThick));
            AddBox(verts, norms, uvs, tris, new Vector3(0f, wallCenterY, postHalfZ), new Vector3(innerLength, wallH, wallThick));

            // 5. Left & Right End Cap Boards
            AddBox(verts, norms, uvs, tris, new Vector3(-postHalfX, wallCenterY, 0f), new Vector3(wallThick, wallH, innerWidth));
            AddBox(verts, norms, uvs, tris, new Vector3(postHalfX, wallCenterY, 0f), new Vector3(wallThick, wallH, innerWidth));

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            s_troughMesh = mesh;
            return s_troughMesh;
        }

        public static Mesh GetOrCreateFeedMesh()
        {
            if (s_feedMesh != null) return s_feedMesh;

            Mesh mesh = new Mesh { name = "VH_ProceduralFeedMesh" };

            // A 7x4 grid surface with a gentle mound curve in the center
            int cols = 7;
            int rows = 4;
            float width = 0.98f;
            float depth = 0.44f;

            List<Vector3> verts = new();
            List<Vector3> norms = new();
            List<Vector2> uvs = new();
            List<int> tris = new();

            for (int r = 0; r <= rows; r++)
            {
                float tz = (float)r / rows;
                float z = Mathf.Lerp(-depth * 0.5f, depth * 0.5f, tz);

                for (int c = 0; c <= cols; c++)
                {
                    float tx = (float)c / cols;
                    float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, tx);

                    // Mound curve: peak in the middle, sloping down to rim
                    float curveX = Mathf.Sin(tx * Mathf.PI);
                    float curveZ = Mathf.Sin(tz * Mathf.PI);
                    float moundHeight = (curveX * curveZ) * 0.07f;

                    verts.Add(new Vector3(x, moundHeight, z));
                    norms.Add(Vector3.up);
                    uvs.Add(new Vector2(tx * 2.5f, tz * 1.5f));
                }
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int i0 = (r * (cols + 1)) + c;
                    int i1 = i0 + 1;
                    int i2 = ((r + 1) * (cols + 1)) + c;
                    int i3 = i2 + 1;

                    // First triangle
                    tris.Add(i0);
                    tris.Add(i2);
                    tris.Add(i1);

                    // Second triangle
                    tris.Add(i1);
                    tris.Add(i2);
                    tris.Add(i3);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            s_feedMesh = mesh;
            return s_feedMesh;
        }

        private static void AddBox(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris, Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;

            // 6 faces: Up, Down, Front, Back, Left, Right
            // Up (+Y)
            AddQuad(verts, norms, uvs, tris,
                center + new Vector3(-h.x, h.y, -h.z),
                center + new Vector3(-h.x, h.y, h.z),
                center + new Vector3(h.x, h.y, h.z),
                center + new Vector3(h.x, h.y, -h.z),
                Vector3.up, size.x, size.z);

            // Down (-Y)
            AddQuad(verts, norms, uvs, tris,
                center + new Vector3(-h.x, -h.y, h.z),
                center + new Vector3(-h.x, -h.y, -h.z),
                center + new Vector3(h.x, -h.y, -h.z),
                center + new Vector3(h.x, -h.y, h.z),
                Vector3.down, size.x, size.z);

            // Front (-Z)
            AddQuad(verts, norms, uvs, tris,
                center + new Vector3(-h.x, -h.y, -h.z),
                center + new Vector3(-h.x, h.y, -h.z),
                center + new Vector3(h.x, h.y, -h.z),
                center + new Vector3(h.x, -h.y, -h.z),
                Vector3.back, size.x, size.y);

            // Back (+Z)
            AddQuad(verts, norms, uvs, tris,
                center + new Vector3(h.x, -h.y, h.z),
                center + new Vector3(h.x, h.y, h.z),
                center + new Vector3(-h.x, h.y, h.z),
                center + new Vector3(-h.x, -h.y, h.z),
                Vector3.forward, size.x, size.y);

            // Left (-X)
            AddQuad(verts, norms, uvs, tris,
                center + new Vector3(-h.x, -h.y, h.z),
                center + new Vector3(-h.x, h.y, h.z),
                center + new Vector3(-h.x, h.y, -h.z),
                center + new Vector3(-h.x, -h.y, -h.z),
                Vector3.left, size.z, size.y);

            // Right (+X)
            AddQuad(verts, norms, uvs, tris,
                center + new Vector3(h.x, -h.y, -h.z),
                center + new Vector3(h.x, h.y, -h.z),
                center + new Vector3(h.x, h.y, h.z),
                center + new Vector3(h.x, -h.y, h.z),
                Vector3.right, size.z, size.y);
        }

        private static void AddQuad(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris,
            Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal, float uvWidth, float uvHeight)
        {
            int baseIdx = verts.Count;

            verts.Add(v0);
            verts.Add(v1);
            verts.Add(v2);
            verts.Add(v3);

            norms.Add(normal);
            norms.Add(normal);
            norms.Add(normal);
            norms.Add(normal);

            float scale = 1.0f; // 1 meter = 1 UV repeat
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(0f, uvHeight * scale));
            uvs.Add(new Vector2(uvWidth * scale, uvHeight * scale));
            uvs.Add(new Vector2(uvWidth * scale, 0f));

            tris.Add(baseIdx);
            tris.Add(baseIdx + 1);
            tris.Add(baseIdx + 2);

            tris.Add(baseIdx);
            tris.Add(baseIdx + 2);
            tris.Add(baseIdx + 3);
        }
    }
}
