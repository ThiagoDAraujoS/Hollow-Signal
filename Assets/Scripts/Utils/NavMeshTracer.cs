using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.Board;

namespace Utils{
    public static class NavMeshTracer{
        private static Vector3 Flatten(Vector3 v) => new(v.x, 0f, v.z);

        public static Area FlattenedVoronoiCheck(Vector3 position, Area area){
            Area closestArea = null;
            position = Flatten(position);
            float distToClosestArea = Vector3.Distance(position, Flatten(area.transform.position));

            foreach (Area n in area.neighbors){
                float distToNeighbor = Vector3.Distance(position, Flatten(n.transform.position));
                if (distToNeighbor >= distToClosestArea) continue;
                closestArea       = n;
                distToClosestArea = distToNeighbor;
            }

            return closestArea;
        }

        private const float MinCleanDist = 0.05f;
        public static Vertex[] CleanVertices(Vertex[] raw){
            if (raw.Length < 2) return raw;

            List<Vertex> clean = new(){ raw[0] };

            for (int i = 1; i < raw.Length; i++){
                if (Vector3.SqrMagnitude(raw[i].position - clean[^1].position) > MinCleanDist){
                    clean.Add(raw[i]);
                }
            }

            if (clean.Count > 1 && Vector3.SqrMagnitude(clean[^1].position - clean[0].position) < MinCleanDist)
                clean.RemoveAt(clean.Count - 1);

            return clean.ToArray();
        }
        public static void SanitizeSelfNeighbors(List<Edge> edges, Area currentArea){
            if (edges == null || currentArea == null) return;

            foreach (Edge edge in edges)
                if (edge.neighbor == currentArea)
                    edge.neighbor = null;
        }
        public static List<Edge> CompressEdges(List<Edge> inputEdges){
            if (inputEdges == null || inputEdges.Count == 0) 
                return new List<Edge>();

            List<Edge> result = new();
    
            // Initialize the first batch
            List<Edge> batch = new() { inputEdges[0] };

            // 1. Standard Linear Compression
            for (int i = 1; i < inputEdges.Count; i++){
                Edge currentEdge  = inputEdges[i];
                Edge previousEdge = batch[0]; // Representative of the current batch

                // Use == for object/null comparison
                if (currentEdge.neighbor == previousEdge.neighbor){
                    batch.Add(currentEdge);
                }
                else{
                    result.Add(Edge.Coalesce(batch));
                    batch.Clear();
                    batch.Add(currentEdge);
                }
            }

            // Add the final pending batch
            if (batch.Count > 0)
                result.Add(Edge.Coalesce(batch));

            // 2. Wraparound Logic (The "Seam" Fix)
            // If we have >1 groups, check if the Start and End groups share a neighbor.
            if (result.Count > 1 && result[0].neighbor == result[^1].neighbor){
        
                // We create a temporary list [LastEdge, FirstEdge]
                // Order matters: We want the path to flow from the Tail into the Head.
                List<Edge> wrapList = new List<Edge> { result[^1], result[0] };
        
                // Fuse them. This creates a new edge starting at the Tail's start 
                // and ending at the Head's end.
                Edge wrappedEdge = Edge.Coalesce(wrapList);
        
                // Replace the First edge with this new merged edge
                result[0] = wrappedEdge;
        
                // Remove the Last edge (since it's now merged into the first)
                result.RemoveAt(result.Count - 1);
            }

            return result;
        }
        
        public static Vertex[] RadialTrace(Vector3 o, Area area, float maxDistance, float stepSize, float nvMeshDist, int rayCount){
            Vertex[] vertices = new Vertex[rayCount];

            for (int i = 0; i < rayCount; i++){
                float   angle = i % rayCount * (360f / rayCount);
                Vector3 dir   = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                vertices[i] = Trace(o, dir, maxDistance, stepSize, nvMeshDist, area);
            }

            return CleanVertices(vertices);
        }

        public static Vertex Trace(Vector3 o, Vector3 dir, float maxDistance, float stepSize, float nvMeshDist, Area area){
            Vertex result = new(o, null);

            // 1. Initial Snap
            if (!NavMesh.SamplePosition(o, out NavMeshHit hit, nvMeshDist, NavMesh.AllAreas)){
                Debug.LogWarning("Trace failed to find navmesh at origin");
                return result;
            }

            result.position = hit.position;

            // Using a slightly looser drift for stability, but 0.0005f is fine if your mesh is clean.
            const float maxHorizontalDrift = 0.005f;

            float distTraveled = 0f;

            while (distTraveled < maxDistance){
                Vector3 nextStep = result.position + (dir * stepSize);

                if (NavMesh.SamplePosition(nextStep, out hit, nvMeshDist, NavMesh.AllAreas)){
                    float drift = Vector2.Distance(
                        new Vector2(nextStep.x,     nextStep.z),
                        new Vector2(hit.position.x, hit.position.z)
                    );

                    if (drift > maxHorizontalDrift){
                        result.neighbor = null;
                        return result;
                    }

                    Area foundNeighbor = FlattenedVoronoiCheck(hit.position, area);

                    if (foundNeighbor != null){ 
                        result.position = hit.position;
                        result.neighbor = foundNeighbor;
                        return result;
                    }

                    result.position =  hit.position;
                    distTraveled    += stepSize;
                    result.neighbor =  area;
                }
                else{
                    result.neighbor = null;
                    return result;
                }
            }

            return result;
        }

        public static Vector3 Trace(Vector3 origin, Vector3 direction, float maxDistance, float stepSize, float nvMeshDist, Func<Vector3, bool> stopPredicate){
            if (!NavMesh.SamplePosition(origin, out NavMeshHit hit, nvMeshDist, NavMesh.AllAreas))
                return origin;

            Vector3 currentPos = hit.position;

            // Define how much horizontal drift we allow (e.g., 10cm)
            // This replaces your "0.0005" setting with a manual logic check.
            const float maxHorizontalDrift = 0.0005f;

            float distTraveled = 0f;

            while (distTraveled < maxDistance){
                Vector3 nextStep = currentPos + (direction * stepSize);

                // 1. Ask NavMesh for ANY point within range (High tolerance for Y)
                if (NavMesh.SamplePosition(nextStep, out hit, nvMeshDist, NavMesh.AllAreas)){

                    // 2. THE MANUAL CYLINDER CHECK
                    // We strip the Y coordinate to check purely horizontal distance.
                    float drift = Vector2.Distance(
                        new Vector2(nextStep.x,     nextStep.z),
                        new Vector2(hit.position.x, hit.position.z)
                    );

                    // If the NavMesh pulled us sideways to find a point, REJECT IT.
                    // This detects if we fell off an edge and got snapped back to the wall.
                    if (drift > maxHorizontalDrift){
                        return currentPos; // We hit the void.
                    }

                    // 3. Valid Step
                    currentPos   =  hit.position;
                    distTraveled += stepSize;

                    if (stopPredicate != null && stopPredicate(currentPos)){
                        return currentPos;
                    }
                }
                else{
                    return currentPos; // Hit the void (nothing found even with high snap)
                }
            }
            return currentPos;
        }

        /// <summary>
        /// Converts a ring of vertices into logical Edges based on curvature.
        /// <param name="vertices">list of vertexes composing a geometric shape</param>
        /// <param name="segmentSize">how big each segment in each edge should be</param>
        /// <param name="tolerance">0.999f → extremely strict; 0.98f → good for meshes; 0.95f → organic geometry</param>
        /// </summary>
        public static List<Edge> ArbitraryPolygonToEdges(Vertex[] vertices, float segmentSize, float tolerance = 0.95f){
            List<Edge> edges = new();
            int        last  = -1;
            int        first = -1;
            int        count = vertices.Length;

            // Safety for empty rays
            if (count < 2) return edges;

            for (int i = 0; i < count; i++){
                Vector3 curr = vertices[i].position;
                Vector3 prev = vertices[(i - 1 + count) % count].position;
                Vector3 next = vertices[(i + 1) % count].position;

                Vector3 v1 = prev - curr;
                Vector3 v2 = next - curr;

                if (v1.sqrMagnitude < 1e-8f || v2.sqrMagnitude < 1e-8f){
                    Debug.LogWarning("Vectors returned a 0 magnitude");
                    continue;
                }

                // Dot Product: -1 is straight, 1 is sharp turn back
                float dot = Vector3.Dot(v1.normalized, v2.normalized);

                // If it's effectively a straight line, skip
                if (dot <= -tolerance) continue;

                if (last != -1)
                    edges.Add(new Edge(segmentSize,
                                       new ArraySegment<Vertex>(vertices, offset: last, count: (i - last) + 1)));
                last = i;
                if (first == -1) first = i;
            }

            edges.Add(new Edge(segmentSize,
                               new ArraySegment<Vertex>(vertices, offset: last, count: count - last),
                               new ArraySegment<Vertex>(vertices, offset: 0,
                                                        count: first + 1)));
            return edges;
        }
    }

    [Serializable]
    public class Vertex{
        public Vector3 position;
        public Area    neighbor;

        public Vertex(Vector3 position, Area neighbor){
            this.position = position;
            this.neighbor = neighbor;
        }
    }

    [Serializable]
    public class Edge{
        public Vector3       a, b;
        public Area          neighbor;
        public List<Vector3> segments;

        public static Edge Coalesce(List<Edge> edges) {
            if (edges == null || edges.Count == 0) {
                Debug.LogError("Cannot coalesce an empty list of edges.");
                return null;
            }
            
            Edge bigEdge = new(){
                a        = edges[0].a,
                b        = edges[^1].b,
                neighbor = edges[0].neighbor,
                segments = new List<Vector3>()
            };

            for (int i = 0; i < edges.Count; i++) {
                List<Vector3> currentPoints = edges[i].segments;

                if (currentPoints == null || currentPoints.Count == 0) continue;

                for (int j = i == 0 ? 0 : 1; j < currentPoints.Count; j++)
                    bigEdge.segments.Add(currentPoints[j]);
            }
            return bigEdge;
        }
        
        private Edge() { }
        
        public Edge(float size, params ArraySegment<Vertex>[] vertices){
            if (vertices == null || vertices.Length == 0 || vertices[0].Count == 0){
                Debug.LogError("Edge created with empty vertices!");
                segments = new List<Vector3>();
                return;
            }

            a = vertices[0][0].position;
            b = vertices[^1][^1].position;

            Dictionary<Area, int> neighborCounts = new();

            int nullNeighborCount = 0;

            foreach (ArraySegment<Vertex> arraySegment in vertices){
                foreach (Vertex vertex in arraySegment){
                    if (vertex.neighbor == null)
                        nullNeighborCount++;
                    else{
                        neighborCounts.TryAdd(vertex.neighbor, 0);
                        neighborCounts[vertex.neighbor]++;
                    }
                }
            }

            KeyValuePair<Area, int> dominantArea = neighborCounts
                                                  .OrderByDescending(kvp => kvp.Value)
                                                  .FirstOrDefault();

            neighbor = nullNeighborCount > dominantArea.Value ? null : dominantArea.Key;


            segments = new List<Vector3>();
            Vector3 delta  = b - a;
            float   length = delta.magnitude;
            
            if (length < size) {
                segments.Add(a);
                segments.Add(b);
                return;
            }

            int segmentCount = Mathf.FloorToInt(length / size);
            
            float remainder = length - segmentCount * size;
            float step      = size + remainder / segmentCount;

            Vector3 dir = delta / length;
            
            segments.Add(a);
            
            for (int s = 1; s < segmentCount; s++){
                Vector3 p = a + dir * (step * s);
                segments.Add(p);
            }
            
            segments.Add(b);
        }
    }
}