using System.Collections.Generic;
using UnityEngine;

namespace Data.Effects{
    [CreateAssetMenu(fileName = "CompositeEffect", menuName = "CRPG/Effects/Composite Effect")]
    public class CompositeEffect : EffectNode{
        [SerializeField] private List<EffectNode> children = new();

        /// Runs each child effect in sequence passing along the shared context.
        public override void Run(EffectContext context){
            foreach (EffectNode child in children)
                child.Run(context);
        }
    }
}
