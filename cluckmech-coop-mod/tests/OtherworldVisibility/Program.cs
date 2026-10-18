using System;
using System.Reflection;
using CluckmechCoop;
using UnityEngine;
using Mirror;
static class Program
{
    static int checks;static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
    static void Main()
    {
        foreach(bool original in new[]{false,true})
        {
            var renderer=new UnityEngine.Renderer{forceRenderingOff=original};
            OtherworldRenderLease.Hide(renderer);OtherworldRenderLease.Hide(renderer);
            Check(renderer.forceRenderingOff,"phase plus spatial hiding");
            OtherworldRenderLease.Reveal(renderer);Check(renderer.forceRenderingOff,"ending phase cannot expose other-space actor");
            OtherworldRenderLease.Reveal(renderer);Check(renderer.forceRenderingOff==original,"return restores pre-phase visibility");
            OtherworldRenderLease.Hide(renderer);OtherworldRenderLease.Hide(renderer);OtherworldRenderLease.Hide(renderer);
            OtherworldRenderLease.Reveal(renderer);OtherworldRenderLease.Reveal(renderer);Check(renderer.forceRenderingOff,"overlapping warp still owns hide");
            OtherworldRenderLease.Reveal(renderer);Check(renderer.forceRenderingOff==original,"last owner restores original visibility");
            OtherworldRenderLease.Reveal(renderer);Check(renderer.forceRenderingOff==original,"unmatched cleanup harmless");
        }
        OtherworldRenderLease.Hide(null);OtherworldRenderLease.Reveal(null);
        Visibility();Hidden();
        Console.WriteLine($"PASS {checks} visibility ownership, hierarchy, refresh and hidden-destroy checks");
    }
    static OtherworldUnit Unit(uint id,int space)
    {
        var obj=new GameObject();obj.AddComponent<NetworkIdentity>().netId=id;
        var unit=obj.AddComponent<OtherworldUnit>();unit.Space=space;unit.Health=obj.AddComponent<\u6a21\u5757_\u57fa\u672c\u5c5e\u6027>();
        obj.AddComponent<Renderer>();OtherworldWorld.Units[obj]=unit;return unit;
    }
    static void Visibility()
    {
        var first=Unit(1,2);var second=Unit(2,0);var interest=new KamuiInterest();
        var canvas=first.gameObject.AddComponent<Canvas>();canvas.enabled=false;
        interest.SetHostVisibility(first.GetComponent<NetworkIdentity>(),true);
        Check(first.GetComponent<Renderer>().forceRenderingOff,"host callback hides only addressed unit by space");
        Check(second.gameObject.Scans==0,"host spawn callback does not scan other units");
        int scans=first.gameObject.Scans;interest.SetHostVisibility(first.GetComponent<NetworkIdentity>(),true);
        Check(first.gameObject.Scans==scans,"stable unit reuses cached renderer and canvas lists");
        var child=new GameObject();first.transform.Add(child);var childRenderer=child.AddComponent<Renderer>();
        var childCanvas=child.AddComponent<Canvas>();first.gameObject.Message("LateUpdate");
        Check(childRenderer.forceRenderingOff&&!childCanvas.enabled,"new children inherit hidden state");
        Check(second.gameObject.Scans==0,"hierarchy changes only rescan their own unit");
        var grandchild=new GameObject();child.transform.Add(grandchild);var grandRenderer=grandchild.AddComponent<Renderer>();first.gameObject.Message("LateUpdate");
        Check(grandRenderer.forceRenderingOff,"nested hierarchy changes invalidate unit cache");
        first.Space=0;KamuiVisibility.Refresh();KamuiVisibility.Refresh();KamuiVisibility.Flush();
        Check(!first.GetComponent<Renderer>().forceRenderingOff&&!grandRenderer.forceRenderingOff,"spatial refresh reveals entire unit");
        Check(!canvas.enabled&&childCanvas.enabled,"canvases restore each original enabled value");
        scans=first.gameObject.Scans;KamuiVisibility.Flush();Check(first.gameObject.Scans==scans,"full refresh requests coalesce and do not repeat");
        first.Space=2;KamuiVisibility.Update(first.gameObject);OtherworldRenderLease.Hide(childRenderer);KamuiVisibility.Removed(first);
        Check(!first.GetComponent<Renderer>().forceRenderingOff&&childRenderer.forceRenderingOff,"removal releases only spatial hide ownership");
        OtherworldRenderLease.Reveal(childRenderer);Check(!childRenderer.forceRenderingOff,"remaining phase lease ends cleanly");
        var ordinary=new GameObject();ordinary.AddComponent<Renderer>();interest.SetHostVisibility(ordinary.AddComponent<NetworkIdentity>(),false);
        Check(!ordinary.GetComponent<Renderer>().enabled,"non-unit objects retain native host visibility");
        KamuiVisibility.Clear();OtherworldWorld.Units.Clear();
    }
    static object Invoke(string method,params object[] args)=>typeof(KamuiVisibility).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
    static void Hidden()
    {
        KamuiVisibility.Install();Check(NetworkClient.Handlers.ContainsKey(typeof(KamuiDestroyedMessage)),"destroy handler registered with prefab handlers");
        var unit=Unit(10,2);Invoke("Unspawn",unit.gameObject);KamuiVisibility.Removed(unit);
        OtherworldWorld.Units.Remove(unit.gameObject);
        Check(!unit.gameObject.activeSelf&&!unit.gameObject.Destroyed&&KamuiVisibility.Find(10)==unit.gameObject,"observer hide retains live object through lifecycle removal");
        var restored=(GameObject)Invoke("Spawn",new GameObject(),new SpawnMessage{netId=10});
        Check(restored==unit.gameObject&&restored.activeSelf&&KamuiVisibility.Restored(restored),"observer reappearance reuses instance and marks native start suppression");
        Check(OtherworldWorld.Unit(restored)==unit,"observer reappearance registers unit without waiting for another transfer message");
        Check(!KamuiVisibility.Restored(restored),"restore marker is consumed once");
        Invoke("Unspawn",unit.gameObject);Invoke("Destroyed",new KamuiDestroyedMessage{NetId=10,Token="wrong-run"});
        Check(KamuiVisibility.Find(10)==unit.gameObject,"stale-run destroy ignored");
        Invoke("Destroyed",new KamuiDestroyedMessage{NetId=10,Token=Plugin.Combat.Token});
        Check(unit.gameObject.Destroyed&&KamuiVisibility.Find(10)==null,"authoritative destroy reclaims hidden non-observer instance");
        var visible=Unit(11,2);NetworkClient.spawned[11]=visible.GetComponent<NetworkIdentity>();
        Invoke("Destroyed",new KamuiDestroyedMessage{NetId=11,Token=Plugin.Combat.Token});
        Check(!visible.gameObject.Destroyed,"visible native destroy retains its normal ordering");
        NetworkClient.spawned.Remove(11);Invoke("Unspawn",visible.gameObject);
        Check(visible.gameObject.Destroyed&&KamuiVisibility.Find(11)==null,"authoritative death cannot be recached when client health is stale");
        var pending=Unit(12,2);Invoke("Unspawn",pending.gameObject);KamuiVisibility.Forget(pending.gameObject);
        Check(KamuiVisibility.Find(12)==null,"external object destruction clears hidden reference");
        var disconnect=Unit(13,2);Invoke("Unspawn",disconnect.gameObject);KamuiVisibility.Clear();
        Check(disconnect.gameObject.Destroyed&&KamuiVisibility.Find(13)==null,"disconnect destroys cached hidden instances");
        Check(!NetworkClient.Handlers.ContainsKey(typeof(KamuiDestroyedMessage)),"disconnect unregisters destroy handler");
        Plugin.Role=1;NetworkServer.active=true;var host=Unit(14,2);KamuiVisibility.Install();KamuiVisibility.ServerDestroyed(host.gameObject);
        Check(NetworkServer.Sent.Count==1&&((KamuiDestroyedMessage)NetworkServer.Sent[0]).NetId==14,"server broadcasts original netId independently of observers");
        Check(Writer<KamuiDestroyedMessage>.write!=null&&Reader<KamuiDestroyedMessage>.read!=null,"custom message serialization bound");
        Plugin.Role=2;KamuiVisibility.ServerDestroyed(host.gameObject);Check(NetworkServer.Sent.Count==1,"clients cannot broadcast authoritative destruction");
        KamuiVisibility.Clear();OtherworldWorld.Units.Clear();NetworkClient.spawned.Clear();
    }
}
