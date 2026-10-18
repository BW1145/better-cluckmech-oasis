using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    // Spatial visibility and transient phase/warp replacement must share ownership.
    // Preserve intentional native hiding; never capture another owner's temporary hide.
    internal static class OtherworldRenderLease
    {
        private sealed class Lease { internal int Count;internal bool Original; }
        private static readonly Dictionary<Renderer,Lease> hidden=new Dictionary<Renderer,Lease>();
        private static readonly Dictionary<Canvas,Lease> hiddenCanvases=new Dictionary<Canvas,Lease>();

        internal static void HideCanvas(Canvas canvas)
        {
            if (canvas==null) return;
            if (!hiddenCanvases.TryGetValue(canvas,out var lease))
            {
                lease=new Lease { Original=canvas.enabled };
                hiddenCanvases.Add(canvas,lease);
            }
            lease.Count++;
            canvas.enabled=false;
        }

        internal static void RevealCanvas(Canvas canvas)
        {
            if (ReferenceEquals(canvas,null)||!hiddenCanvases.TryGetValue(canvas,out var lease)) return;
            if (--lease.Count!=0) return;
            hiddenCanvases.Remove(canvas);
            if (canvas!=null) canvas.enabled=lease.Original;
        }

        internal static void Hide(Renderer renderer)
        {
            if(renderer==null)return;
            if(!hidden.TryGetValue(renderer,out var lease))hidden[renderer]=lease=new Lease{Original=renderer.forceRenderingOff};
            lease.Count++;renderer.forceRenderingOff=true;
        }
        internal static void Reveal(Renderer renderer)
        {
            if(ReferenceEquals(renderer,null)||!hidden.TryGetValue(renderer,out var lease))return;
            if(--lease.Count==0){hidden.Remove(renderer);if(renderer!=null)renderer.forceRenderingOff=lease.Original;}
        }
    }
}
