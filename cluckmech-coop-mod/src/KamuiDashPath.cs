using System;

namespace CluckmechCoop
{
    internal static class KamuiDashPath
    {
        internal static float ReachableFraction(Func<float,bool> reachable)
        {
            if(reachable(1))return 1;
            float low=0,high=1;
            for(int i=0;i<9;i++)
            {
                float middle=(low+high)*.5f;
                if(reachable(middle))low=middle;else high=middle;
            }
            return low;
        }
    }
}
