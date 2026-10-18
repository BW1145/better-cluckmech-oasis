using System;
using System.Reflection;
using CluckmechCoop;
using UnityEngine;
using Mirror;
static class Program
{
    static int checks;static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
    static void Main(string[] args)
    {
        if (Array.IndexOf(args,"--nested-hierarchy")>=0)
        {
            NestedHierarchy();
            Console.WriteLine($"PASS {checks} nested unit hierarchy listener ownership checks");
            return;
        }
        if (Array.IndexOf(args,"--canvas-ownership")>=0)
        {
            CanvasOwnership();
            Console.WriteLine($"PASS {checks} parent-child canvas ownership and restoration checks");
            return;
        }
        if (Array.IndexOf(args,"--session-notifications")>=0)
        {
            SessionNotifications();
            Console.WriteLine($"PASS {checks} session notification registration and scene-boundary checks");
            return;
        }
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
    static void CanvasOwnership()
    {
        foreach(bool original in new[]{false,true})
        foreach(bool parentHidesFirst in new[]{false,true})
        foreach(bool parentReleasesFirst in new[]{false,true})
        {
            var parent=Unit(100,2);
            var child=Unit(101,2);
            parent.transform.Add(child.gameObject);
            var canvas=child.gameObject.AddComponent<Canvas>();
            canvas.enabled=original;
            var parentView=parent.gameObject.AddComponent<KamuiVisibilityView>();
            var childView=child.gameObject.AddComponent<KamuiVisibilityView>();
            var firstHide=parentHidesFirst?parentView:childView;
            var secondHide=parentHidesFirst?childView:parentView;
            firstHide.SetVisible(false);
            secondHide.SetVisible(false);
            Check(!canvas.enabled,"both nested unit views hide the shared canvas");

            // A hierarchy cache rebuild must not recapture another owner's false value.
            parentView.Changed();
            parentView.SetVisible(false);
            Check(!canvas.enabled,"rebuilding one owner preserves the other owner's hide");
            var firstRelease=parentReleasesFirst?parentView:childView;
            var lastRelease=parentReleasesFirst?childView:parentView;
            firstRelease.SetVisible(true);
            Check(!canvas.enabled,"revealing one unit cannot expose a canvas still owned by the hidden unit");
            lastRelease.Release();
            Check(canvas.enabled==original,"last owner restores the actual original canvas state");
            firstRelease.Release();
            lastRelease.Release();
            Check(canvas.enabled==original,"repeated cleanup does not change restored canvas state");
            OtherworldWorld.Units.Clear();
        }
    }
    static void NestedHierarchy()
    {
        var parent=Unit(110,0);
        var child=Unit(111,2);
        parent.transform.Add(child.gameObject);
        var anchor=new GameObject();
        child.transform.Add(anchor);
        var original=anchor.AddComponent<Canvas>();
        var parentView=parent.gameObject.AddComponent<KamuiVisibilityView>();
        var childView=child.gameObject.AddComponent<KamuiVisibilityView>();
        parentView.SetVisible(true);
        childView.SetVisible(false);
        Check(!original.enabled,"nested child unit starts hidden under a visible parent");
        parentView.Changed();
        parentView.SetVisible(true);
        var grandchild=new GameObject();
        anchor.transform.Add(grandchild);
        var renderer=grandchild.AddComponent<Renderer>();
        var canvas=grandchild.AddComponent<Canvas>();
        parent.gameObject.Message("LateUpdate");
        child.gameObject.Message("LateUpdate");
        Check(renderer.forceRenderingOff&&!canvas.enabled,"parent cache rebuild cannot steal deep child hierarchy notifications");
        parentView.Release();
        Check(renderer.forceRenderingOff&&!canvas.enabled,"releasing parent listener preserves hidden child's leases");
        var later=new GameObject();
        anchor.transform.Add(later);
        var laterRenderer=later.AddComponent<Renderer>();
        child.gameObject.Message("LateUpdate");
        Check(laterRenderer.forceRenderingOff,"child listener still receives changes after parent releases");
        childView.Release();
        Check(original.enabled&&canvas.enabled&&!renderer.forceRenderingOff&&!laterRenderer.forceRenderingOff,"final child cleanup restores all dynamically discovered visuals");
        OtherworldWorld.Units.Clear();
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
        var disconnect=Unit(13,2);Invoke("Unspawn",disconnect.gameObject);KamuiVisibility.Disconnect();
        Check(disconnect.gameObject.Destroyed&&KamuiVisibility.Find(13)==null,"disconnect destroys cached hidden instances");
        Check(!NetworkClient.Handlers.ContainsKey(typeof(KamuiDestroyedMessage)),"disconnect unregisters destroy handler");
        Plugin.Role=1;NetworkServer.active=true;var host=Unit(14,2);KamuiVisibility.Install();KamuiVisibility.ServerDestroyed(host.gameObject);
        Check(NetworkServer.Sent.Count==1&&((KamuiDestroyedMessage)NetworkServer.Sent[0]).NetId==14,"server broadcasts original netId independently of observers");
        Check(Writer<KamuiDestroyedMessage>.write!=null&&Reader<KamuiDestroyedMessage>.read!=null,"custom message serialization bound");
        Plugin.Role=2;KamuiVisibility.ServerDestroyed(host.gameObject);Check(NetworkServer.Sent.Count==1,"clients cannot broadcast authoritative destruction");
        KamuiVisibility.Disconnect();OtherworldWorld.Units.Clear();NetworkClient.spawned.Clear();
    }
    static void SessionNotifications()
    {
        Plugin.Role=1;
        NetworkServer.active=true;
        Plugin.Network.PeerReady=false;
        var ordinary=Unit(40,0);
        KamuiVisibility.ServerDestroyed(ordinary.gameObject);
        Check(NetworkServer.Sent.Count==0,"no new message reaches a peer before the plugin handshake");

        // Both session handshakes register messages even if neither actor ever creates a room.
        Plugin.Role=2;
        KamuiVisibility.RegisterMessages();
        var clientHandler=(Action<KamuiDestroyedMessage>)NetworkClient.Handlers[typeof(KamuiDestroyedMessage)];
        Check(Writer<KamuiDestroyedMessage>.write!=null&&Reader<KamuiDestroyedMessage>.read!=null,"session registers serializer and reader without Ensure");
        Check(NetworkClient.PrefabRegistrations==0&&NetworkServer.aoi==null,"message registration does not install room AOI or prefab reuse");
        int registrations=NetworkClient.Registrations;
        KamuiVisibility.RegisterMessages();
        Check(NetworkClient.Registrations==registrations,"repeated session registration keeps one handler");

        Plugin.Role=1;
        Plugin.Network.PeerReady=true;
        KamuiVisibility.ServerDestroyed(ordinary.gameObject);
        Check(NetworkServer.Sent.Count==1,"ordinary enemy destruction is serializable before the first Ensure");
        Plugin.Role=2;
        clientHandler((KamuiDestroyedMessage)NetworkServer.Sent[0]);
        Check(NetworkClient.Handlers.ContainsKey(typeof(KamuiDestroyedMessage)),"hero12 client with no room accepts notification through its session handler");

        foreach(bool hostFirst in new[]{true,false})
        {
            Plugin.Role=hostFirst?1:2;
            KamuiVisibility.Install();
            KamuiVisibility.Clear();
            Check(NetworkClient.Handlers.ContainsKey(typeof(KamuiDestroyedMessage)),"clearing first peer room preserves session handler");
            Plugin.Role=hostFirst?2:1;
            KamuiVisibility.Install();
            Check(NetworkClient.Registrations==registrations,"either peer can Ensure first without replacing message registration");
            KamuiVisibility.Clear();
        }

        Plugin.Role=2;
        var hidden=Unit(41,2);
        Invoke("Unspawn",hidden.gameObject);
        KamuiVisibility.Clear();
        Check(hidden.gameObject.Destroyed&&KamuiVisibility.Find(41)==null,"scene clear reclaims hidden objects");
        Check(NetworkClient.Handlers.ContainsKey(typeof(KamuiDestroyedMessage)),"scene clear retains live session message dispatch");
        clientHandler(new KamuiDestroyedMessage{NetId=41,Token=Plugin.Combat.Token});
        Check(KamuiVisibility.Find(41)==null,"in-flight old scene notification is harmless with no cached target");

        int unregistrations=NetworkClient.Unregistrations;
        KamuiVisibility.Disconnect();
        KamuiVisibility.Disconnect();
        Check(!NetworkClient.Handlers.ContainsKey(typeof(KamuiDestroyedMessage))&&NetworkClient.Unregistrations==unregistrations+1,"disconnect unregisters exactly once");
        NetworkClient.active=false;
        KamuiVisibility.RegisterMessages();
        Check(!NetworkClient.Handlers.ContainsKey(typeof(KamuiDestroyedMessage)),"inactive client does not register a session handler");
        NetworkClient.active=true;
        KamuiVisibility.RegisterMessages();
        Check(NetworkClient.Handlers.ContainsKey(typeof(KamuiDestroyedMessage))&&NetworkClient.Registrations==registrations+1,"reconnect registers a fresh active session handler");
        KamuiVisibility.Disconnect();
        OtherworldWorld.Units.Clear();
    }
}
