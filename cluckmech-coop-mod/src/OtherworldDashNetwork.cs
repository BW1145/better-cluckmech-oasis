using HarmonyLib;
using Mirror;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class OtherworldDashNetwork
    {
        internal static void Transferred(OtherworldActor actor,int from,int to)
        {
            if(actor==null||actor.Hero!=13||from==to)return;
            actor.FilterKamuiSnapshots=true;
            ClearHistory(actor);
        }
        internal static void ClearHistory(OtherworldActor actor)
        {
            if(actor==null||actor.Hero!=13)return;
            var sync=actor.GetComponent<NetworkTransformBase>();
            if(sync==null)return;
            // Discard interpolation history on BOTH peers, never the delta codec
            // baselines. In-flight snapshots must still decode against their sender.
            sync.serverSnapshots.Clear();sync.clientSnapshots.Clear();
        }
        internal static bool Accept(NetworkTransformBase sync,Vector3? position)
        {
            var actor=sync.GetComponent<OtherworldActor>();
            if(actor==null||actor.Hero!=13||!actor.FilterKamuiSnapshots||!position.HasValue)return true;
            var point=position.Value;
            if(sync.coordinateSpace==CoordinateSpace.Local&&sync.target!=null&&sync.target.parent!=null)
                point=sync.target.parent.TransformPoint(point);
            return KamuiWorld.At(point)==actor.Space;
        }
    }

    [HarmonyPatch(typeof(NetworkTransformBase),"Apply")]
    internal static class OtherworldDashInterpolation
    {
        private static bool Prefix(NetworkTransformBase __instance)
        {var actor=__instance.GetComponent<OtherworldActor>();return actor==null||!actor.Dashing;}
    }
    // These callbacks run AFTER delta decoding. Returning false skips only snapshot
    // insertion: OnDeserialize still advances lastDeserializedPosition unconditionally.
    [HarmonyPatch(typeof(NetworkTransformReliable),"OnClientToServerSync")]
    internal static class OtherworldDashServerSnapshot
    {
        private static bool Prefix(NetworkTransformReliable __instance,Vector3? position)
            =>OtherworldDashNetwork.Accept(__instance,position);
    }
    [HarmonyPatch(typeof(NetworkTransformReliable),"OnServerToClientSync")]
    internal static class OtherworldDashClientSnapshot
    {
        private static bool Prefix(NetworkTransformReliable __instance,Vector3? position)
            =>OtherworldDashNetwork.Accept(__instance,position);
    }
}
