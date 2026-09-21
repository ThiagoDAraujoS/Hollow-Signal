using UnityEngine;

namespace Data.Effects{
    public abstract class EffectNode : ScriptableObject{
        [SerializeField] private string id;

        public string Id => id;

        /// Executes this effect logic using the provided effect context.
        public abstract void Run(EffectContext context);
    }
}
