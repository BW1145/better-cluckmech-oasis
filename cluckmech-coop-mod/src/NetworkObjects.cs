using Mirror;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class NetworkObjects
    {
        internal static uint Id(GameObject obj) => obj == null ? 0 : obj.GetComponent<NetworkIdentity>()?.netId ?? 0;
    }
}
