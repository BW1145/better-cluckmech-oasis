using System.Linq;

namespace CluckmechCoop
{
    // Data invariants shared by disk snapshots and network payloads. Authority/token
    // checks belong to the network handler; optional legacy fields belong to storage.
    internal static class OtherworldDataValidation
    {
        internal static bool Finite(float value) => NetworkValues.Finite(value);

        internal static bool Run(OtherworldRun run)
        {
            if (run == null || OtherworldCatalog.Hero(run.Hero) == null || run.Talents == null ||
                run.Upgrades < 0 || run.ShockKills < 0 || run.SpaceKills < 0 || !Finite(run.BaseInfluence)) return false;
            if(run.NativeTalents!=null&&(run.NativeTalents.Any(id=>!OtherworldTalentState.Earned(id,run.Hero))||run.NativeTalents.Distinct().Count()!=run.NativeTalents.Count))return false;

            if (run.Initial != 0)
            {
                var initial = OtherworldCatalog.Talent(run.Initial);
                if (initial == null || !initial.Initial || initial.Hero != run.Hero) return false;
            }
            foreach (int id in run.Talents)
            {
                var talent = OtherworldCatalog.Talent(id);
                if (talent == null || talent.Initial || talent.Hero != run.Hero) return false;
            }
            if (run.LastDeployment != null && (!Rules.ValidMachine(run.LastDeployment) || run.LastDeployment.Upgrades.Any(id => id <= 0))) return false;
            return run.CardIds == null || (!run.CardIds.Any(string.IsNullOrWhiteSpace) && run.CardIds.Distinct().Count() == run.CardIds.Count);
        }

        internal static bool NetworkRun(OtherworldRun run)
        {
            if (!Run(run) || run.Initial == 0 || run.Talents.Count > MaximumNetworkTalents) return false;
            int branch = OtherworldCatalog.Branch(run.Initial);
            return run.Talents.All(id => OtherworldCatalog.Talent(id).Branch < 0 || OtherworldCatalog.Talent(id).Branch == branch);
        }

        private const int MaximumNetworkTalents = 6;
    }
}
