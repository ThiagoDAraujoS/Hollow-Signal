using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data.Effects{
    [CreateAssetMenu(fileName = "EffectDatabase", menuName = "CRPG/Effects/Effect Database")]
    public class EffectDatabase : ScriptableObject{
        [SerializeField] private List<EffectNode> effects = new();

        private readonly Dictionary<string, EffectNode> _registry = new(StringComparer.OrdinalIgnoreCase);

        private void OnEnable() => BuildDictionaryCache();

        /// Rebuilds the internal dictionary cache for O(1) effect lookup.
        public void BuildDictionaryCache(){
            _registry.Clear();
            foreach (EffectNode node in effects)
                _registry[node.Id] = node;
        }

        /// Retrieves an effect node asset by its identifier.
        public EffectNode Get(string id) => _registry[id];
    }
}
