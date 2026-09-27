using System;
using System.Collections.Generic;
using World.Tactical;

namespace World.Threats{
    [Serializable]
    public struct AreaImpact{
        public TacticalZone zone;
        public int amount;
        public List<string> impactTypes;

        public AreaImpact(TacticalZone zone, int amount, List<string> impactTypes){
            this.zone = zone;
            this.amount = amount;
            this.impactTypes = impactTypes ?? new();
        }
    }

    [Serializable]
    public class ImpactPayload{
        public int globalDamage;
        public List<string> globalDamageTypes = new();
        public List<string> globalTags = new();
        public List<AreaImpact> areaImpacts = new();

        public ImpactPayload(){ }

        public ImpactPayload(int globalDamage, List<string> globalDamageTypes, List<string> globalTags, List<AreaImpact> areaImpacts){
            this.globalDamage = globalDamage;
            this.globalDamageTypes = globalDamageTypes ?? new();
            this.globalTags = globalTags ?? new();
            this.areaImpacts = areaImpacts ?? new();
        }
    }
}
