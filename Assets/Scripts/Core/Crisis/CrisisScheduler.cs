using System;
using System.Collections;
using System.Collections.Generic;
using Core.State;
using Data.Effects;
using UnityEngine;
using World.Actors;

namespace Core.Crisis{
    [Serializable]
    public class CrisisScheduler : ITracked{
        [SerializeField] private string key = "crisis_scheduler";
        [SerializeField] private List<ScheduledEntry> heap = new();

        public string Key => key;
        public int Count => heap.Count;

        public CrisisScheduler(string key = "crisis_scheduler") => this.key = key;

        /// Inserts a new scheduled effect entry into the priority queue.
        public void Schedule(float executionTime, EffectNode effect, EffectContext context){
            ScheduledEntry entry = new(executionTime, effect.Id, context.targetId, context);
            Push(entry);
        }

        /// Resolves all scheduled entries whose execution time has arrived.
        public void AdvanceClock(float currentTime, EffectDatabase database){
            while (heap.Count > 0 && heap[0].executionTime <= currentTime){
                ScheduledEntry entry = Pop();
                ResolveTargetReference(entry);
                EffectNode effect = database.Get(entry.effectId);
                effect.Run(entry.context);
            }
        }

        /// Cancels all scheduled entries associated with the specified target entity.
        public void CancelForTarget(string targetId){
            if (string.IsNullOrEmpty(targetId)) return;
            heap.RemoveAll(entry => entry.targetId == targetId);
            BuildHeap();
        }

        /// Serializes all active scheduled entries into the Blackboard partition.
        public void Save(Dictionary<string, object> state){
            List<object> serialized = new();
            foreach (ScheduledEntry entry in heap)
                serialized.Add(entry.ToDictionary());
            state[key] = serialized;
        }

        /// Restores scheduled entries from the Blackboard partition and rebuilds the heap.
        public void Load(Dictionary<string, object> state){
            if (!state.TryGetValue(key, out object rawList) || rawList is not IEnumerable list) return;
            heap.Clear();
            foreach (object item in list)
                if (item is Dictionary<string, object> dict)
                    heap.Add(ScheduledEntry.FromDictionary(dict));
            BuildHeap();
        }

        /// Pushes a new entry into the heap and sifts it up to maintain ordering.
        private void Push(ScheduledEntry entry){
            heap.Add(entry);
            SiftUp(heap.Count - 1);
        }

        /// Removes and returns the earliest entry from the heap.
        private ScheduledEntry Pop(){
            ScheduledEntry root = heap[0];
            int lastIndex = heap.Count - 1;
            heap[0] = heap[lastIndex];
            heap.RemoveAt(lastIndex);
            if (heap.Count > 0)
                SiftDown(0);
            return root;
        }

        /// Restores heap order by sifting down from parent nodes.
        private void BuildHeap(){
            for (int i = (heap.Count / 2) - 1; i >= 0; i--)
                SiftDown(i);
        }

        /// Sifts an entry up towards the root to restore heap invariant.
        private void SiftUp(int index){
            while (index > 0){
                int parent = (index - 1) / 2;
                if (heap[index].CompareTo(heap[parent]) >= 0) break;
                Swap(index, parent);
                index = parent;
            }
        }

        /// Sifts an entry down towards the leaves to restore heap invariant.
        private void SiftDown(int index){
            int count = heap.Count;
            while (true){
                int smallest = index;
                int left = 2 * index + 1;
                int right = 2 * index + 2;

                if (left < count && heap[left].CompareTo(heap[smallest]) < 0)
                    smallest = left;
                if (right < count && heap[right].CompareTo(heap[smallest]) < 0)
                    smallest = right;

                if (smallest == index) break;
                Swap(index, smallest);
                index = smallest;
            }
        }

        /// Swaps two entries at given indices.
        private void Swap(int a, int b) => (heap[a], heap[b]) = (heap[b], heap[a]);

        /// Injects the target component reference into the context if missing after deserialization.
        private void ResolveTargetReference(ScheduledEntry entry){
            if (entry.context.target != null || string.IsNullOrEmpty(entry.targetId)) return;
            foreach (BlackboardClient client in BlackboardClient.ActiveClients)
                if (client.EntityId == entry.targetId)
                    entry.context.target = client.GetComponent<Sheet>();
        }
    }
}
