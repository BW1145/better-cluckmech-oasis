using System;
using CluckmechCoop;

static class Program
{
    static int checks;
    static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
    static void Main()
    {
        foreach(int fps in new[]{30,60,144,240})
        {
            var state=new OtherworldMotion();float lastPhysics=0;
            for(int frame=0;frame<fps*2;frame++)
            {
                float now=frame/(float)fps,physics=(float)Math.Floor(now/.02f)*.02f;
                float distance=physics-lastPhysics;lastPhysics=physics;
                Check(state.Sample(1,0,distance,now,true),"held movement stays moving between physics ticks");
                Check(state.Face(-1,0)==2,"opposite aim stays left across render/physics sampling");
            }
            Check(Math.Abs(state.GaitCycles-lastPhysics/OtherworldMotion.LegacyStride)<.001,"gait follows travelled distance independent of FPS");
            state.Dash(1,0,out float dx,out float dy);
            Check(dx==1&&dy==0,"dash takes movement, not opposing aim");
            state.Dash(0,0,out dx,out dy);
            Check(dx==1&&dy==0,"stationary fallback uses last movement, not aim");
            Check(!state.Sample(0,0,0,2.1f,true),"local release stops without sticky locomotion");
        }
        var remote=new OtherworldMotion();
        Check(remote.Sample(0,1,.1f,0,false),"remote snapshot moves");
        Check(remote.Sample(0,0,0,.04f,false),"remote snapshot gap preserves locomotion");
        Check(!remote.Sample(0,0,0,.1f,false),"remote stop expires");
        float gait=remote.GaitCycles;Check(!remote.Sample(100,0,100,1,false),"teleport is not locomotion");
        Check(remote.LastX==0&&remote.LastY==1,"teleport does not replace last movement direction");
        Check(remote.GaitCycles==gait,"teleport does not advance gait");
        var fresh=new OtherworldMotion();fresh.Dash(0,0,out float x,out float y);
        Check(x==0&&y==-1,"fresh stationary dash follows initial down facing");
        fresh.Face(1,0);int facing=fresh.Facing;
        Check(fresh.Face(0,0)==facing,"zero aim preserves facing");
        fresh.Face(0,-1,true);
        Check(fresh.Face(-.4f,-1)==0,"facing hysteresis holds near sector boundary");
        Check(fresh.Face(-.6f,-1)==1,"intentional boundary crossing updates facing");
        foreach(float interval in new[]{.05f,.1f,.5f,1f})
        {
            var shooting=new OtherworldMotion();float nextShot=0;var seen=new bool[4];
            for(int frame=0;frame<180;frame++)
            {
                float now=frame/60f;
                if(now+.0001f>=nextShot)
                {
                    bool active=shooting.Attacking(now);float start=shooting.AttackStarted;
                    shooting.Fired(now,interval);nextShot+=interval;
                    Check(!active||shooting.AttackStarted==start,"active recoil is never reset by rapid shots");
                }
                if(shooting.Attacking(now))seen[shooting.AttackFrame(now,4)]=true;
            }
            foreach(bool visible in seen)Check(visible,"all four recoil phases visible at 60FPS");
            Check(!shooting.Attacking(4)&&shooting.AttackFrame(4,4)==0,"recoil returns idle after firing stops");
            shooting.Fired(5,interval);Check(shooting.AttackStarted==5,"new burst starts its own complete cycle");
        }
        var slowShot=new OtherworldMotion();slowShot.Fired(1,1);
        Check(slowShot.Attacking(1.3f)&&!slowShot.Attacking(1.5f),"slow shot completes one cycle rather than recoiling during full cooldown");
        Check(!OtherworldMotion.TryDashCommand(0,1,0,out _,out _),"legacy packet rejected even near world origin");
        Check(!OtherworldMotion.TryDashCommand(1,100,250,out _,out _),"world-coordinate target rejected");
        Check(!OtherworldMotion.TryDashCommand(1,0,0,out _,out _),"zero wire vector rejected; fallback belongs to owner");
        Check(!OtherworldMotion.TryDashCommand(1,float.NaN,0,out _,out _),"NaN rejected");
        Check(!OtherworldMotion.TryDashCommand(1,float.PositiveInfinity,1,out _,out _),"infinity rejected");
        Check(!OtherworldMotion.TryDashCommand(2,1,0,out _,out _),"unknown future protocol rejected");
        Check(OtherworldMotion.TryDashCommand(1,1,-1,out float nx,out float ny),"raw diagonal accepted");
        Check(Math.Abs(nx*nx+ny*ny-1)<.00001&&nx>0&&ny<0,"diagonal normalized without changing quadrant");
        Check(OtherworldMotion.TryDashCommand(1,-1,0,out nx,out ny)&&nx==-1&&ny==0,"unit cardinal retained");
        for(int aim=0;aim<8;aim++)
        {
            for(int move=0;move<8;move++)
            {
                var gaitPolicy=new OtherworldMotion();double angle=move*Math.PI/4;
                gaitPolicy.Sample(-(float)Math.Sin(angle),-(float)Math.Cos(angle),.3f,0,true);
                Check(gaitPolicy.MovementFacing==move,"movement keeps its own world-facing sector");
                Check(gaitPolicy.RelativeMovement(aim)==(move-aim+8)%8,"relative gait selects forward/backpedal/strafe independently of aim");
                Check(gaitPolicy.GaitFrame(4)==0&&gaitPolicy.GaitFrame(8)==1&&gaitPolicy.GaitFrame(12)==1,"frame count changes sampling, not travelled distance");
            }
        }
        var zeroAim=new OtherworldMotion();zeroAim.Face(1,0);
        zeroAim.Sample(-1,0,.2f,0,true);
        Check(zeroAim.Face(0,0)==6,"zero aim keeps last upper-body facing during opposing locomotion");
        Check(zeroAim.MovementFacing==2,"zero aim does not overwrite actual gait direction");
        var distanceGait=new OtherworldMotion();
        distanceGait.Sample(1,0,.91f,0,true);
        Check(distanceGait.GaitFrame(4)==2&&distanceGait.GaitFrame(8)==4,"eight-frame gait shares four-frame contact phase");
        distanceGait.Sample(1,0,.91f,1,true);
        Check(distanceGait.GaitFrame(4)==0&&distanceGait.GaitFrame(8)==0,"all frame counts wrap at same stride distance");
        var calibrated=new OtherworldMotion();calibrated.Sample(1,0,.5f,0,true,1);
        Check(calibrated.GaitFrame(8)==4,"one-world-unit calibrated stride reaches opposite foot at half unit");
        calibrated.Sample(0,1,.25f,1,true,2);
        Check(calibrated.GaitFrame(8)==5,"changing stride calibration preserves phase instead of resetting contact");
        var scaled=new OtherworldMotion();scaled.Sample(1,0,.5f,0,true,2);
        Check(scaled.GaitFrame(8)==2,"doubling rendered size doubles world stride rather than foot cadence");
        foreach(int renderRate in new[]{30,60,120})
        {
            var run=new OtherworldMotion();float previousPhysics=0;
            for(int frame=1;frame<=renderRate*2;frame++)
            {
                float now=(float)frame/renderRate,physics=(float)Math.Floor(now*50)/50;
                float distance=(physics-previousPhysics)*4.5f;previousPhysics=physics;
                run.Sample(1,0,distance,now,true,.1f,OtherworldMotion.MaximumRunCyclesPerSecond);
            }
            Check(run.GaitCycles<=6.001f&&run.GaitCycles>5.7f,"fast run remains readable at 3Hz across physics/render rates");
            Check(run.GaitFrame(16)>=0&&run.GaitFrame(16)<16,"production sixteen-phase selector stays bounded");
        }
        var remoteRun=new OtherworldMotion();float remotePhysics=0;
        for(int frame=1;frame<=480;frame++)
        {
            float now=frame/240f,physics=(float)Math.Floor(now*50)/50;
            float distance=(physics-remotePhysics)*4.5f;remotePhysics=physics;
            remoteRun.Sample(distance,0,distance,now,false,.1f,3);
        }
        Check(remoteRun.GaitCycles>5.7f&&remoteRun.GaitCycles<=6.001f,"remote snapshot gaps preserve capped cadence at 240Hz rendering");
        var slowRun=new OtherworldMotion();slowRun.Sample(1,0,.01f,.02f,true,1,3);
        Check(Math.Abs(slowRun.GaitCycles-.01f)<.00001f,"slow locomotion keeps calibrated distance cadence below cap");
        // Queue timestamps remain authoritative when both events are delivered after a stall.
        Check(!OtherworldMotion.LongPress(10,10.1),"100ms release stays a tap after delayed event processing");
        Check(OtherworldMotion.LongPress(10,10.4),"400ms hold qualifies at its event timestamp");
        Check(!OtherworldMotion.LongPress(10,9.9),"older event cannot qualify a hold");
        Console.WriteLine($"PASS {checks} motion policy checks");
    }
}
