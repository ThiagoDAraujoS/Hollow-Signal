using UnityEngine;
using System.Collections.Generic;

namespace Core.Board{
    public class Area : MonoBehaviour{
        public Area[]      neighbors;
        public AreaManager manager;

#if UNITY_EDITOR
        private Color _gizmoColor;

        private void OnValidate() =>
            _gizmoColor = Color.HSVToRGB(Random.value, 1f, Random.Range(0.6f, 1f));

        private void OnDrawGizmos(){
            if (manager == null || manager.navmeshPolygon2D == null)
                return;

            Vector2 p = To2D(transform.position);

            // START WITH NAVMESH POLYGON
            List<Vector2> poly = new List<Vector2>(manager.navmeshPolygon2D);

            // CLIP AGAINST EACH NEIGHBOR
            foreach (Area area in neighbors){
                if (area == null) continue;
                Vector2 n = To2D(area.transform.position);
                poly = Clip(poly, p, n);
            }

            // DRAW FINAL POLYGON
            Gizmos.color = _gizmoColor;
            DrawPolygon(poly, transform.position.y + 0.05f);
        }

        private static Vector2 To2D(Vector3 pos) =>
            new Vector2(pos.x, pos.z);

        private static List<Vector2> Clip(List<Vector2> poly, Vector2 p, Vector2 n){
            List<Vector2> output = new List<Vector2>();

            Vector2 mid    = (p + n) * 0.5f;
            Vector2 normal = (p - n).normalized;

            for (int i = 0; i < poly.Count; i++){
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % poly.Count];

                bool aIn = Vector2.Dot(a - mid, normal) >= 0;
                bool bIn = Vector2.Dot(b - mid, normal) >= 0;

                if (aIn && bIn){
                    output.Add(b);
                }
                else if (aIn){
                    output.Add(Intersect(a, b, mid, normal));
                }
                else if (bIn){
                    output.Add(Intersect(a, b, mid, normal));
                    output.Add(b);
                }
            }

            return output;
        }

        private static Vector2 Intersect(Vector2 a, Vector2 b, Vector2 mid, Vector2 normal){
            Vector2 ab = b - a;
            float   t  = Vector2.Dot(mid - a, normal) / Vector2.Dot(ab, normal);
            return a + ab * t;
        }

        private static void DrawPolygon(List<Vector2> poly, float y){
            if (poly.Count < 2) return;

            for (int i = 0; i < poly.Count; i++){
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % poly.Count];

                Gizmos.DrawLine(
                    new Vector3(a.x, y, a.y),
                    new Vector3(b.x, y, b.y)
                );
            }
        }
#endif
    }
}