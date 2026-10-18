using System;
using CluckmechCoop;

static class Program
{
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static void Main()
    {
        var target=new UnitActionLifetime();
        ulong pull=target.Capture(),orbital=target.Capture();
        Check(target.Current(pull)&&target.Current(orbital),"concurrent actions remain valid in their original space");
        target.Invalidate(); // 26113 capture while 26104 pull is between frames.
        Check(!target.Current(pull)&&!target.Current(orbital),"transfer cancels every old action on the captured target");
        ulong roomAction=target.Capture();
        Check(target.Current(roomAction),"new room actions can run immediately");
        target.Invalidate(); // return to main map
        Check(!target.Current(pull)&&!target.Current(roomAction),"returning to the original space cannot revive a stale action");
        ulong beforeDisable=target.Capture();target.Invalidate();
        Check(!target.Current(beforeDisable),"unit removal cancels outstanding work");
        ulong respawn=target.Capture();
        Check(target.Current(respawn)&&!target.Current(beforeDisable),"a re-enabled unit accepts only new actions");
        var other=new UnitActionLifetime();ulong otherAction=other.Capture();
        target.Invalidate();
        Check(other.Current(otherAction),"removing another unit does not cancel unrelated work");
        Console.WriteLine($"PASS {checks} production action lifetime checks");
    }
}
