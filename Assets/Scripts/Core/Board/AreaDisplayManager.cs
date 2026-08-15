using System.Collections.Generic;
using UnityEngine;
using Utils; // Assuming your Area and Edge classes are here

namespace Core.Board {
    public class AreaDisplayManager : MonoBehaviour {

        /// <summary>
        /// Collects only the edges that form the outer boundary of the selected cluster of areas.
        /// </summary>
        // Change the type to List of Lists
        public List<List<Edge>> displayEdges = new();

        public void CollectEdges(List<Area> areas){
            displayEdges.Clear();
            if (areas == null || areas.Count == 0) return;
            HashSet<Area> selectedSet = new(areas);
            foreach (Area area in areas){
                List<Edge> currentChain = null;
                foreach (Edge edge in area.shape.borderSegments){
                    if (edge.neighbor == null || !selectedSet.Contains(edge.neighbor)){
                        currentChain ??= new List<Edge>();
                        currentChain.Add(edge);
                    }
                    else{
                        if (currentChain == null) continue;
                        displayEdges.Add(currentChain);
                        currentChain = null;
                    }
                }
                if (currentChain != null)
                    displayEdges.Add(currentChain);
            }
        }

        public List<List<Edge>> SortChainsIntoShapes(List<List<Edge>> chains){
            if (chains == null || chains.Count == 0) return new List<List<Edge>>();

            int    count = chains.Count;
            bool[] used  = new bool[count];

            int chainsProcessed = 0;
            int cursor          = 0;

            List<List<Edge>> allShapes = new();

            // --- MAIN LOOP ---
            while (chainsProcessed < count){
                // 1. Ensure cursor is valid (skip used chains)
                if (used[cursor]){
                    MoveCursorToNext();
                    // Safety check: if we looped all the way around and everything is used, we are done.
                    if (used[cursor]) break;
                }

                // 2. Start a new Shape with the chain at the cursor
                StartNewShape(cursor);

                // 3. Keep extending this shape until it closes
                while (true){
                    int nextChainIndex = FindNextConnection();

                    if (nextChainIndex != -1){
                        // We found a chain that continues the path -> Add it.
                        AddChainToShape(nextChainIndex);
                    }
                    else{
                        break;
                    }
                }
            }

            return allShapes;

            // --- HELPER FUNCTIONS ---

            void StartNewShape(int index){
                // Create a new Shape list and add the edges from the first chain
                List<Edge> newShape = new ();
                newShape.AddRange(chains[index]);
                allShapes.Add(newShape);
                TagProcessed(index);
            }

            void AddChainToShape(int index){
                // Flatten the chain into the current shape
                allShapes[^1].AddRange(chains[index]);
                TagProcessed(index);
            }

            void TagProcessed(int index){
                used[index]     =  true;
                chainsProcessed += 1;
            }

            void MoveCursorToNext(){
                int start = cursor;
                do{
                    cursor = (cursor + 1) % count;
                } while (used[cursor] && cursor != start);
            }
            int FindNextConnection(){
                List<Edge> currentShape = allShapes[^1];

                // The point we are trying to connect FROM
                Vector3 tailPos = currentShape[^1].b;

                // The point we eventually want to reach (to close the loop)
                Vector3 shapeStartPos = currentShape[0].a;

                // 1. Default to "Closing the Loop"
                // We calculate the distance to our own start. 
                // We will only accept a new chain if it is STRICTLY closer than this distance.
                float closestDistance = (tailPos - shapeStartPos).sqrMagnitude;
                int   selectedChain   = -1;

                // 2. Check all unused chains
                for (int i = 0; i < count; i++){
                    if (used[i]) continue;

                    // Check distance from Tail to the Start of chain[i]
                    // Note: chains[i][0] is the first edge of that chain
                    float newDistance = (tailPos - chains[i][0].a).sqrMagnitude;

                    // If this chain is further away (or equal) to our current best option, skip it.
                    // (If newDistance > closestDistance, it means "Closing the loop" is still the better option)
                    if (newDistance >= closestDistance) continue;

                    selectedChain   = i;
                    closestDistance = newDistance;
                }

                return selectedChain;
            }
        }

        public void OnDrawGizmos() {
            /*
            if (displayEdges == null || displayEdges.Count == 0) return;

            Gizmos.color = Color.yellow; // Choose your highlight color

            foreach (Edge edge in displayEdges) {
                // Draw the main line A -> B
                Gizmos.DrawLine(edge.a, edge.b);

                // Draw internal segments (the curved detail)
                if (edge.segments != null) {
                    foreach (Vector3 point in edge.segments) {
                        Gizmos.DrawSphere(point, 0.1f); // Adjust size as needed
                    }
                }
                
                // Optional: distinct visual for the "Joints" (Start/End) to see sorting
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(edge.a, 0.15f);
                Gizmos.color = Color.yellow;
            }
                */
        }
    }
}