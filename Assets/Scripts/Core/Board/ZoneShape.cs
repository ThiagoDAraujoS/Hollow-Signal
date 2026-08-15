using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Core.Board {
    [RequireComponent(typeof(Area))]
    public class ZoneShape : MonoBehaviour {
        [SerializeField] [HideInInspector] private Area area;

        [Header("Baking Settings")] 
        
        [SerializeField] private int     rayCount         = 256;
        [SerializeField] private float   maxRadius        = 15f;
        [SerializeField] private float   stepSize         = 0.5f;
        [SerializeField] private float   segmentSize      = 0.5f;
        [SerializeField] public  Vector3 centerOffset     = Vector3.zero;
        [SerializeField] public  float   navMeshRayLength = 1f;
        [SerializeField] public  float   edgeTolerance    = 0.95f;

        [HideInInspector] public List<Edge> borderSegments = new();
        
        // --- THE BAKER ---
        [ContextMenu("Bake Geometry")]
        public void BakeGeometry(){
            area = GetComponent<Area>();
            Vector3 bakeOrigin = transform.position + centerOffset;
            borderSegments.Clear();

            Vertex[] points = NavMeshTracer.RadialTrace(bakeOrigin, area, maxRadius, stepSize, navMeshRayLength, rayCount);
            borderSegments = NavMeshTracer.ArbitraryPolygonToEdges(points, segmentSize, edgeTolerance);
            NavMeshTracer.SanitizeSelfNeighbors(borderSegments, area);
            borderSegments = NavMeshTracer.CompressEdges(borderSegments);
        }

        // --- VISUALIZATION ---
        private void OnDrawGizmosSelected() {
            if (borderSegments == null || borderSegments.Count == 0) return;

            Color[] colors ={ Color.cyan, Color.green, Color.blue, Color.magenta, Color.yellow };
            
            Dictionary<Area, Color> neighborColorMap = new();

            for (int i = 0; i < area.neighbors.Length; i++)
                neighborColorMap[area.neighbors[i]] = colors[i]; 
            neighborColorMap.Add(area, Color.black);
            
            
            foreach (Edge edge in borderSegments){
                Gizmos.color = edge.neighbor == null ? Color.red : neighborColorMap[edge.neighbor];
            
                // 1. Draw the Start Anchor (Corner)
                Gizmos.DrawSphere(edge.a, 0.1f); // Make corners distinct

                // 2. Handle the Line Drawing
                if (edge.segments == null || edge.segments.Count == 0) {
                    // Short edge: Just draw Start to End
                    Gizmos.DrawLine(edge.a, edge.b);
                } 
                else {
                    // A. Draw Start -> First Segment
                    Gizmos.DrawLine(edge.a, edge.segments[0]);

                    // B. Draw Internal Segments
                    for (int i = 1; i < edge.segments.Count; i++) {
                        Gizmos.DrawLine(edge.segments[i - 1], edge.segments[i]);
                        // Optional: Draw small dots for segment points
                        Gizmos.DrawSphere(edge.segments[i - 1], 0.02f); 
                    }

                    // C. Draw Last Segment -> End
                    Gizmos.DrawLine(edge.segments[^1], edge.b);
                }
            }
        }
    }
}