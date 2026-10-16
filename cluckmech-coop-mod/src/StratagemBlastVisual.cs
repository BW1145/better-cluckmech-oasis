using UnityEngine;

namespace CluckmechCoop
{
    internal static class StratagemBlastVisual
    {
        internal static void Play(Vector2 point,float radius,bool heavy)
        {
            float width=radius*2;
            StratagemAssets.Flipbook(heavy?"fx.blast-heavy.":"fx.blast-small.",heavy?16:8,point,Mathf.Min(7,width*.95f),heavy?1.1f:.65f,hold:true);
            if(heavy)StratagemAssets.Flipbook("fx.eagle-shock.",8,point,width,.45f,ground:true,hold:true);
            StratagemNativeParticles.Play(StratagemParticleKind.Sparks,point,heavy?.8f:.4f,.25f);
            StratagemAssets.Effect(heavy?"fx.crater.1":"fx.crater.0",point,width*.55f,heavy?12:8,delay:heavy?.65f:.3f,ground:true,opacity:.7f);
        }
    }
}
