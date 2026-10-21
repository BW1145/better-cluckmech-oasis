using System.Collections.Generic;
using Mirror;

namespace CluckmechCoop
{
    // Read existing player/observer registries only. This filters transient display
    // results, never entity, health, death or persistent-state synchronization.
    internal static class GuestHitVisibility
    {
        private struct View
        {
            internal NetworkConnectionToClient Connection;
            internal int Space;
        }
        private static View Guest()
        {
            var peer=Steam联机管理器.玩家_客户端;
            var connection=peer==null?null:peer.connectionToClient;
            var viewer=OtherworldActions.Player(2)?.gameObject??StratagemWorld.Player(2)?.gameObject;
            return new View{Connection=connection,Space=OtherworldWorld.Space(viewer)};
        }
        private static bool Visible(View view,NetworkIdentity identity,int space)
        {
            return view.Connection!=null&&view.Space==space&&identity!=null&&identity.netId!=0&&identity.gameObject.activeInHierarchy&&
                identity.observers.TryGetValue(view.Connection.connectionId,out var observer)&&ReferenceEquals(observer,view.Connection);
        }
        internal static bool Visible(NetworkIdentity identity,int space)=>Visible(Guest(),identity,space);
        internal static void Filter(List<OtherworldDisplayHit> hits)
        {
            var view=Guest();int keep=0,count=hits.Count;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];
                if(!NetworkServer.spawned.TryGetValue(hit.Target,out var identity)||
                    !Visible(view,identity,hit.Space)||OtherworldWorld.Space(identity.gameObject)!=hit.Space)continue;
                hits[keep++]=hit;
            }
            if(keep<count)hits.RemoveRange(keep,count-keep);
        }
    }
}
