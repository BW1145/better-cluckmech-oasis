using System;

namespace CluckmechCoop
{
    // Rendering policy only: no physics, input side effects or per-shot pose resets.
    internal sealed class OtherworldMotion
    {
        internal const int DashProtocol=1;
        internal static bool TryDashCommand(int protocol,float x,float y,out float dx,out float dy)
        {
            dx=dy=0;
            if(protocol!=DashProtocol||float.IsNaN(x)||float.IsNaN(y)||float.IsInfinity(x)||float.IsInfinity(y))return false;
            double squared=(double)x*x+(double)y*y;
            // Direction payloads may be unit or raw diagonal input, never world targets.
            if(squared<.0001||squared>2.001)return false;
            double length=Math.Sqrt(squared);dx=(float)(x/length);dy=(float)(y/length);return true;
        }
        internal int Facing { get; private set; }
        internal float LastX { get; private set; }
        internal float LastY { get; private set; } = -1;
        internal const float LegacyStride=4f/2.2f;
        internal float GaitCycles { get; private set; }
        internal int MovementFacing { get; private set; }
        internal int MovementDirection(float x,float y)=>StableDirection(MovementFacing,x,y,false);
        internal int RelativeMovement(int aimFacing)=>(MovementFacing-aimFacing+8)%8;
        internal int GaitFrame(int count)
        {
            if(count<=0)throw new ArgumentOutOfRangeException(nameof(count));
            // One stride still spans the same world distance when more in-betweens are authored.
            return (int)(GaitCycles*count)%count;
        }
        private float movingUntil;
        private float lastTravelAt;
        internal const float MaximumRunCyclesPerSecond=3;
        internal float AttackStarted { get; private set; }
        internal float AttackDuration { get; private set; } = .4f;
        private float attackUntil;
        internal bool Attacking(float now)=>now<attackUntil;
        internal void Fired(float now,float interval)
        {
            if(!Attacking(now))
            {
                AttackStarted=now;AttackDuration=Math.Max(.16f,Math.Min(.4f,interval));
                attackUntil=now+AttackDuration;
            }
            else
            {
                // Fast shots extend complete cosmetic cycles rather than pinning the
                // hand on frame zero. Slow shots finish and return to the idle pose.
                float cycles=(float)Math.Ceiling((now-AttackStarted)/AttackDuration+1);
                attackUntil=AttackStarted+cycles*AttackDuration;
            }
        }
        internal int AttackFrame(float now,int count)
        {
            if(count<=0)throw new ArgumentOutOfRangeException(nameof(count));
            return Attacking(now)?(int)((now-AttackStarted)/AttackDuration*count)%count:0;
        }
        internal bool Sample(float x,float y,float distance,float now,bool localIntent,float strideLength=LegacyStride,float maxCyclesPerSecond=float.PositiveInfinity)
        {
            if(!localIntent&&distance>=2){movingUntil=now;return false;}
            bool moving=x*x+y*y>(localIntent?.0001f:.000001f);
            if(moving)
            {
                float length=(float)Math.Sqrt(x*x+y*y);LastX=x/length;LastY=y/length;
                MovementFacing=MovementDirection(x,y);
                movingUntil=now+.08f;
            }
            // Local smoothed locomotion persists across render frames; remote snapshots need a short hold.
            if(distance>0&&distance<2&&strideLength>.001f)
            {
                // Preserve physics-step spacing at high render rates. Stylized run cadence
                // is bounded deliberately: exact contact at game speed would require
                // dozens of cycles per second for these short authored legs.
                float elapsed=Math.Max(0,Math.Min(.08f,now-lastTravelAt));
                GaitCycles+=Math.Min(distance/strideLength,float.IsPositiveInfinity(maxCyclesPerSecond)?float.PositiveInfinity:elapsed*maxCyclesPerSecond);
                lastTravelAt=now;
            }
            else if(!moving&&(localIntent||now>=movingUntil))lastTravelAt=now;
            return moving||!localIntent&&now<movingUntil;
        }
        internal int Face(float x,float y,bool locked=false)
        {Facing=StableDirection(Facing,x,y,locked);return Facing;}
        private static int StableDirection(int previous,float x,float y,bool locked)
        {
            if(x*x+y*y<.000001f)return previous;
            double angle=Math.Atan2(-x,-y)*4/Math.PI;
            double delta=angle-previous;while(delta>4)delta-=8;while(delta< -4)delta+=8;
            // Keep a sector until it is crossed by another five degrees.
            return locked||Math.Abs(delta)>.61?((int)Math.Round(angle)+8)%8:previous;
        }
        internal void Dash(float x,float y,out float dx,out float dy)
        {
            float length=(float)Math.Sqrt(x*x+y*y);
            dx=length>.01f?x/length:LastX;dy=length>.01f?y/length:LastY;
        }
    }
}
