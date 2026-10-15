using System;

namespace CluckmechCoop
{
    internal static class StratagemBeamGeometry
    {
        internal const float Width=.16f;
        // Intersect the rendered beam segment with a unit's world-space hit bounds.
        internal static bool Intersects(float x,float y,float dx,float dy,float length,
            float cx,float cy,float ex,float ey,out float distance)
        {
            float from=0,to=length;
            bool hit=Axis(x,dx,cx-ex-Width*.5f,cx+ex+Width*.5f,ref from,ref to)&&
                Axis(y,dy,cy-ey-Width*.5f,cy+ey+Width*.5f,ref from,ref to);
            distance=from;return hit;
        }
        private static bool Axis(float origin,float direction,float min,float max,ref float from,ref float to)
        {
            if(Math.Abs(direction)<.00001f)return origin>=min&&origin<=max;
            float a=(min-origin)/direction,b=(max-origin)/direction;
            from=Math.Max(from,Math.Min(a,b));to=Math.Min(to,Math.Max(a,b));return from<=to;
        }
    }
}
