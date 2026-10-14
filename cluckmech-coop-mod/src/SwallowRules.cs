using System;
using System.Collections.Generic;

namespace CluckmechCoop
{
    internal static class SwallowSelection
    {
        internal static void Choose<T>(IList<T> candidates,T pointed,int count,Predicate<T> valid,Func<T,float> distance,List<T> selected) where T:class
        {
            selected.Clear();
            if(pointed!=null)
            {
                if(!valid(pointed))return;
                selected.Add(pointed);
            }
            while(selected.Count<count)
            {
                T best=null;float nearest=float.PositiveInfinity;
                foreach(var candidate in candidates)
                {
                    if(!valid(candidate)||selected.Contains(candidate))continue;
                    float value=distance(candidate);
                    if(value<nearest){best=candidate;nearest=value;}
                }
                if(best==null)break;
                selected.Add(best);
            }
        }
    }
}
