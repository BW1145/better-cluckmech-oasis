using System;
using CluckmechCoop;

static class Program
{
    static int checks;
    static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
    static void Input()
    {
        var input=new OtherworldActorInputState();
        Check(!input.Start(0,false,true)&&!input.Armed,"UI-blocked starts do not arm input");
        Check(!input.Start(0,true,false)&&!input.Armed,"paused or unfocused starts do not arm input");
        Check(input.Start(10,true,true),"allowed input starts a hold");
        Check(!input.Start(11,true,true),"duplicate started callback cannot restart or spend again");
        Check(!input.Qualify(10.249,true),"short press does not qualify");
        Check(!input.Qualify(10.25,false),"lost focus or paused input does not qualify");
        Check(input.Qualify(10.25,true),"input-clock threshold qualifies once");
        Check(!input.Qualify(10.5,true),"update and canceled callbacks cannot qualify twice");
        Check(input.Reset()&&!input.Armed&&!input.Qualified,"cancel clears all input state");
        Check(!input.Reset()&&!input.Qualify(100,true),"repeated cancel cannot revive a hold");
        foreach(string reason in new[]{"focus loss","pause","disable","death"})
        {
            Check(input.Start(20,true,true),reason+" fixture starts");
            Check(input.Reset()&&!input.Armed&&!input.Qualified,reason+" resets qualification and armed state");
            Check(!input.Reset(),reason+" repeat is harmless");
        }
        Check(input.Start(30,true,true)&&input.Reset(),"quick cancel retains a single armed release");
        Check(!input.Start(double.NaN,true,true)&&!input.Start(double.PositiveInfinity,true,true),"invalid input clock rejected");
    }
    static void Skills()
    {
        var skill=new OtherworldActorSkillState();skill.Initialize(3);
        Check(skill.Charges==3&&skill.RechargeRemaining(0)==0,"three-charge protocol starts full");
        Check(skill.Spend(10,8,true)&&skill.Charges==2&&skill.Held,"phase spends one charge and owns its hold");
        Check(skill.Remaining(10)==0&&skill.RechargeRemaining(10)==8,"usable charges coexist with recharge display");
        Check(skill.Spend(11,8,false)&&skill.RechargeRemaining(11)==7,"additional spend preserves first recharge deadline");
        Check(skill.Dash()&&skill.Held&&!skill.Dash(),"hold starts one chain and retains held input");
        Check(skill.Release()&&!skill.Release(),"repeat release is harmless");
        Check(skill.Spend(12,8,false)&&skill.Charges==0&&!skill.Spend(12,8,true),"empty charges cannot spend or rearm hold");
        Check(skill.Remaining(12)==6&&!skill.Recharge(17.9f,8,3),"empty cooldown remains until deadline");
        Check(skill.Reduce(13,99)&&skill.RechargeRemaining(13)==0,"cooldown reduction caps at current time");
        Check(skill.Recharge(13,8,3)&&skill.Charges==1&&skill.RechargeRemaining(13)==8,"one recharge starts next charge timer");
        Check(!skill.Recharge(13,8,3),"same-frame tick cannot refill another charge");
        Check(skill.Recharge(21,8,3)&&skill.Recharge(29,8,3)&&skill.Charges==3,"remaining charges refill on separate deadlines");
        Check(!skill.Recharge(100,8,3)&&skill.RechargeRemaining(100)==0,"full charge timer stays inactive");
        Check(!skill.Reduce(100,1),"reduction of inactive cooldown does nothing");
        skill.Initialize(1);Check(skill.Spend(1,10,true)&&skill.Release()&&!skill.Held,"focus loss or disable cancels authoritative hold");
        Check(!skill.Dash()&&skill.Charges==0,"cancel prevents dash without refunding spent charge");
        Check(skill.Receive(1,3,3,50)&&skill.Charges==1&&skill.RechargeRemaining(50)==3,"external state replaces charges and remaining deadline");
        Check(skill.Receive(0,2,1,60)&&skill.Remaining(60)==2,"external empty state drives cooldown presentation");
        foreach(var invalid in new[]{(-1,1f),(2,1f),(0,-1f),(0,float.NaN),(0,float.PositiveInfinity)})
            Check(!skill.Receive(invalid.Item1,invalid.Item2,1,60)&&skill.Charges==0&&skill.Remaining(60)==2,"invalid external state cannot corrupt current cooldown");
        Check(!skill.Reduce(60,float.NaN)&&!skill.Reduce(60,-1),"invalid reduction cannot increase or poison cooldown");
        skill.Initialize(3);Check(skill.Charges==3&&!skill.Held&&skill.RechargeRemaining(0)==0,"new lifetime clears all prior skill state");
    }
    static void DashChains()
    {
        for(int segments=1;segments<=3;segments++)
        {
            var skill=new OtherworldActorSkillState();skill.Initialize(1);
            Check(skill.Spend(100,5,true)&&skill.Cooldown==5&&skill.Remaining(100)==5,"short press starts five-second cooldown");
            Check(skill.Dash()&&!skill.Dash(),"one hold starts one authoritative chain");
            for(int step=1;step<=segments;step++)
                Check(skill.StartDashSegment()&&skill.DashSegments==step&&skill.Cooldown==8+step*2&&skill.Remaining(101)==7+step*2,"segment extends original deadline: "+step);
            if(segments<3)Check(skill.Release()&&!skill.StartDashSegment(),"release stops next segment without changing completed count");
            else Check(!skill.CanContinueDash&&!skill.StartDashSegment(),"third segment exhausts chain while held");
            float ready=100+8+segments*2;
            Check(!skill.Recharge(ready-.01f,skill.Cooldown,1)&&skill.Recharge(ready,skill.Cooldown,1),"only performed segments determine recharge deadline");
            skill.FinishDash();Check(!skill.Held&&!skill.CanContinueDash&&!skill.Dash(),"finished chain cannot restart from direction messages");
            Check(skill.Spend(ready,5,true)&&skill.Cooldown==5&&skill.DashSegments==0,"next short press resets long cooldown to five seconds");
        }
        var reduced=new OtherworldActorSkillState();reduced.Initialize(1);reduced.Spend(0,5,true);reduced.Dash();
        Check(reduced.StartDashSegment()&&reduced.Reduce(1,2)&&reduced.StartDashSegment()&&reduced.Remaining(1)==9,"later segment preserves earned cooldown reduction");
        Check(reduced.Release()&&!reduced.StartDashSegment()&&reduced.DashSegments==2,"cancel retains completed segment count");
        Check(reduced.Reduce(2,4)&&reduced.Remaining(2)==4,"return talent subtracts four seconds from remaining deadline");
        var remote=new OtherworldActorSkillState();remote.Initialize(1);
        Check(remote.Receive(0,11,1,20,14)&&remote.Cooldown==14&&remote.Remaining(20)==11,"remote state retains fourteen-second display total and remaining time");
        Check(!remote.Receive(0,2,1,20,float.NaN)&&remote.Cooldown==14&&remote.Remaining(20)==11,"invalid remote total cannot corrupt state");
        reduced.Initialize(1);reduced.Spend(0,5,true);reduced.Release();
        Check(!reduced.Dash()&&!reduced.StartDashSegment()&&reduced.Cooldown==5,"quick release remains short press");
    }
    static void Walls()
    {
        Check(KamuiDashPath.ReachableFraction(_=>true)==1,"open path reaches full segment");
        Check(KamuiDashPath.ReachableFraction(_=>false)==0,"wall contact keeps current position");
        float clipped=KamuiDashPath.ReachableFraction(t=>t<=.4f);
        Check(clipped<=.4f&&clipped>.398f,"partial segment stops before wall");
        var skill=new OtherworldActorSkillState();skill.Initialize(1);skill.Spend(0,5,true);skill.Dash();
        Check(skill.StartDashSegment()&&KamuiDashPath.ReachableFraction(_=>false)==0&&skill.CanContinueDash,"blocked segment retains held chain");
        Check(skill.StartDashSegment()&&KamuiDashPath.ReachableFraction(_=>true)==1&&skill.CanContinueDash,"turning away starts second segment");
        Check(skill.StartDashSegment()&&!skill.CanContinueDash&&skill.Cooldown==14,"third segment still finishes normally after wall contact");
        skill.Initialize(1);skill.Spend(0,5,true);skill.Dash();skill.StartDashSegment();
        Check(skill.Release()&&!skill.CanContinueDash&&skill.Cooldown==10,"release during interval finishes with one-segment cooldown");
    }
    static void Main(){Input();Skills();DashChains();Walls();Console.WriteLine($"PASS {checks} actor input and skill state checks");}
}
