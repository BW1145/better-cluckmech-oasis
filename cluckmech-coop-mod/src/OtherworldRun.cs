using System.Collections.Generic;

namespace CluckmechCoop
{
    internal sealed class OtherworldRun
    {
        public int Hero, Initial, Upgrades, ShockKills, SpaceKills;
        public HashSet<int> Talents=new HashSet<int>();
        [Newtonsoft.Json.JsonProperty(NullValueHandling=Newtonsoft.Json.NullValueHandling.Ignore)]
        public List<int> NativeTalents;
        public string LastMachine;
        public Machine LastDeployment;
        public List<string> CardIds=new List<string>();
        public float BaseInfluence;
        internal bool Has(int id) => Talents.Contains(id);
        internal float Influence => OtherworldCatalog.ResearchLevel(ShockKills)*(Has(OtherworldTalentIds.AdvancedAnalysis)?.15f:.1f);
        internal float Hunting => .5f+(SpaceKills/10)*(Has(OtherworldTalentIds.SoulMark)?.1f:.05f);
        internal float BurnBonus => .5f+Upgrades*(Has(OtherworldTalentIds.Embers)?.1f:.07f);
    }
}
