using System;
using CluckmechCoop;
namespace UnityEngine { public class Renderer { public bool forceRenderingOff; } }
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
        Console.WriteLine($"PASS {checks} render visibility ownership checks");
    }
}
