using System.Collections.Generic;
using System.Linq;

namespace CluckmechCoop
{
    internal static class OtherworldTalentState
    {
        internal static bool Initial(int id,int hero)
        {var talent=OtherworldCatalog.Talent(id);return talent!=null&&talent.Initial&&talent.Hero==hero;}
        internal static bool Earned(int id,int hero)
        {var talent=OtherworldCatalog.Talent(id);return id>0&&(talent==null||!talent.Initial&&talent.Hero==hero);}
        internal static void Capture(OtherworldRun run,int initial,IEnumerable<int> owned)
        {
            if(Initial(initial,run.Hero))run.Initial=initial;
            run.NativeTalents=(run.NativeTalents??new List<int>()).Concat(run.Talents).Concat(owned)
                .Where(id=>Earned(id,run.Hero)).Distinct().ToList();
            run.Talents=new HashSet<int>(run.NativeTalents.Where(id=>OtherworldCatalog.Talent(id)?.Initial==false));
        }
    }
}
