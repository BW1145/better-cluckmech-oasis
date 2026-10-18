using System;
using System.Collections.Generic;
using System.Linq;
using CluckmechCoop;

static class Program
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static void Main()
    {
        Check(SceneLifecycle.Enter(1),"first scene creates its owner");
        var first=SceneLifecycle.Current;
        Check(SceneLifecycle.IsCurrent(first)&&Trace.Events.Count==0,"first scene does not clean unowned state");
        Check(first.BeginCombat()&&!first.BeginCombat(),"repeated combat Begin does not rebuild active combat");
        Check(!SceneLifecycle.Enter(1)&&ReferenceEquals(first,SceneLifecycle.Current),"duplicate scene notification keeps the same owner");
        SceneLifecycle.Enter(2);
        Check(!SceneLifecycle.IsCurrent(first),"delayed capture/restore from old scene is invalid before new scene starts");
        Check(Trace.Events.SequenceEqual(Trace.Order),"production scene coordinator follows the full cleanup order");
        Check(Trace.ClosedBeforeCleanup,"old callback scope closes before resource cleanup callbacks");
        var second=SceneLifecycle.Current;
        Check(second.BeginCombat(),"read-and-continue into another scene can start combat");
        Trace.Events.Clear();SceneLifecycle.Leave();SceneLifecycle.Leave();
        Check(Trace.Events.SequenceEqual(Trace.Order),"repeated Clear releases scene resources once");
        SceneLifecycle.Enter(2);
        Check(!SceneLifecycle.IsCurrent(second)&&SceneLifecycle.Current.BeginCombat(),"new run in same scene handle does not reuse a closed scope");

        var session=new NetworkSessionState();session.Open();
        ulong handshake=session.Capture();
        Check(session.Current(handshake)&&!session.Ready,"session opens before peer settings exist");
        session.Published();session.PeerSeen();
        Check(!session.Ready,"seeing peer alone does not acknowledge the new revision");
        session.Acknowledge();Check(session.Ready,"matching acknowledgement establishes readiness");
        session.CancelPending();
        Check(!session.Current(handshake)&&session.Ready,"scene change cancels old handshake while retaining session settings");
        ulong nextSceneHandshake=session.Capture();session.Close();session.Close();
        Check(!session.Current(nextSceneHandshake)&&!session.Ready&&session.Revision==0&&session.Acknowledged== -1,"disconnect twice clears session and outstanding callbacks");
        session.Open();
        Check(!session.Current(handshake)&&!session.Current(nextSceneHandshake),"reconnect never revives earlier session callbacks");
        session.AcceptSettings(3);
        Check(session.Ready&&session.Revision==3,"new session accepts host settings atomically");
        session.Mismatch();
        Check(!session.Ready&&!session.Peer&&session.Acknowledged== -1,"version mismatch revokes peer acknowledgement");
        Console.WriteLine($"PASS {checks} production scene coordinator/session lifecycle scenarios");
    }
}

namespace CluckmechCoop
{
    static class Trace
    {
        internal static readonly List<string> Events=new List<string>();
        internal static bool ClosedBeforeCleanup=true;
        internal static readonly string[] Order={"network","actions","combat","growth","boon-ui","modal","difficulty","module-scene","stratagem","kamui","units","deployment","swallow"};
        internal static void Record(string item){ClosedBeforeCleanup&= !SceneLifecycle.IsCurrent(SceneLifecycle.Current);Events.Add(item);}
    }
    static class Plugin {internal static bool InCombat;internal static readonly NetworkStub Network=new NetworkStub();internal static readonly CombatStub Combat=new CombatStub();}
    sealed class NetworkStub {internal void ClearScene()=>Trace.Record("network");}
    sealed class CombatStub {internal void ClearScene()=>Trace.Record("combat");}
    static class UnitLifecycle {internal static void ClearScene()=>Trace.Record("actions");}
    static class GrowthPanel {internal static void Close()=>Trace.Record("growth");}
    static class MapBoonsUi {internal static void Clear()=>Trace.Record("boon-ui");}
    static class FeatureModal {internal static void Clear()=>Trace.Record("modal");}
    static class Difficulty {internal static void Reset()=>Trace.Record("difficulty");}
    static class ModuleSettings {internal static void ClearScene()=>Trace.Record("module-scene");}
    static class CharacterBoons {internal static bool Started;}
    static class StratagemWorld {internal static void Clear()=>Trace.Record("stratagem");}
    static class KamuiWorld {internal static void Clear()=>Trace.Record("kamui");}
    static class OtherworldWorld {internal static void Clear()=>Trace.Record("units");}
    static class OtherworldDeployment {internal static void ClearScene()=>Trace.Record("deployment");}
    static class SwallowTargets {internal static void Clear()=>Trace.Record("swallow");}
}
