using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace Core.Board
{
    public class AreaManager : MonoBehaviour
    {
        public List<Vector2> navmeshPolygon2D;

        private void Awake()
        {
            // Assign manager references
            Area[] areas = GetComponentsInChildren<Area>();
            foreach (Area area in areas)
                area.manager = this;

            // Build navmesh polygon once
            navmeshPolygon2D = BuildNavmeshPolygon();
        }

        private List<Vector2> BuildNavmeshPolygon()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

            Vector3[] verts = triangulation.vertices;
            int[] tris = triangulation.indices;

            // --- VERY IMPORTANT ---
            // For clarity, I will first give you a "simple convex hull"
            // version. If your navmesh is concave, tell me and I will
            // give you the full polygon reconstruction.
            // -----------------------------------------

            // Convert verts to 2D
            List<Vector2> points2D = new List<Vector2>(verts.Length);
            foreach (Vector3 v in verts)
                points2D.Add(new Vector2(v.x, v.z));

            // Build convex hull (safe for clean navmesh islands)
            return ConvexHull(points2D);
        }

        // -------------------------
        // GRAHAM SCAN CONVEX HULL
        // -------------------------
        private List<Vector2> ConvexHull(List<Vector2> points)
        {
            if (points.Count <= 3)
                return new List<Vector2>(points);

            points.Sort((a, b) => Mathf.Approximately(a.x, b.x) ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            List<Vector2> hull = new List<Vector2>();

            // Lower
            foreach (var p in points)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }

            // Upper
            int lowerCount = hull.Count + 1;
            for (int i = points.Count - 1; i >= 0; i--)
            {
                var p = points[i];
                while (hull.Count >= lowerCount && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }

            hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        private float Cross(Vector2 o, Vector2 a, Vector2 b)
        {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        }
    }
}
