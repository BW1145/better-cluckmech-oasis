using System.Collections;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class StratagemEffects
    {
        internal static void Emit(StratagemUnit root,int action,Vector2 point,float radius=1,float duration=1,uint target=0,Vector2 end=default)
        {
            var message=new StratagemEffectMessage{Kind=(int)root.Kind,Action=action,Root=StratagemWorld.Id(root.gameObject),Target=target,
                X=point.x,Y=point.y,EndX=end.x,EndY=end.y,Radius=radius,Duration=duration};
            PlayReady(message,root);
            if(Plugin.Role==1)Plugin.Network.Broadcast(new SessionMessage{Type="stratagem-effect",Token=Plugin.Combat.Token,Effect=message});
        }
        internal static void Play(StratagemEffectMessage message)
        {
            if(!Plugin.InCombat)return;
            if((message.Action==3||message.Action==4||message.Action==5||message.Action==15||message.Action==16)&&
                message.Root!=0&&!StratagemUnit.TryFind(message.Root,out _))
            {Plugin.Instance.StartCoroutine(WaitForUnit(message));return;}
            PlayReady(message);
        }
        private static IEnumerator WaitForUnit(StratagemEffectMessage message)
        {
            string token=Plugin.Combat.Token;
            float until=Time.time+2;
            while(Plugin.InCombat&&token==Plugin.Combat.Token&&Time.time<until&&!StratagemUnit.TryFind(message.Root,out _))yield return null;
            if(Plugin.InCombat&&token==Plugin.Combat.Token&&StratagemUnit.TryFind(message.Root,out _))PlayReady(message);
        }
        private static void Blast(Vector2 point,float radius,bool heavy)
        {
            float width=radius*2;
            StratagemAssets.Effect("fx.flash",point,width*.5f,.13f,grow:.4f,opacity:.8f);
            StratagemAssets.Effect(heavy?"fx.fire-big":"fx.fire-small",point,width*(heavy?.85f:.7f),heavy?.65f:.4f,delay:.03f,grow:.18f);
            var ring=StratagemSurface.Create(StratagemSurfaceKind.Ripple,point,radius,heavy?.45f:.25f);
            ring.gameObject.AddComponent<StratagemExpansion>().Set(.35f,1.15f,heavy?.45f:.25f);
            StratagemAssets.Effect("fx.fragments",point,width*.6f,.45f,grow:.5f,rise:.3f);
            StratagemAssets.Effect(heavy?"fx.smoke":"fx.smoke-thin",point+Vector2.up*.2f,width*.6f,heavy?1.3f:.8f,delay:.18f,grow:.5f,rise:heavy?.8f:.45f,opacity:.45f);
            StratagemAssets.Effect(heavy?"fx.crater.1":"fx.crater.0",point,width*.55f,heavy?12:8,delay:.12f,ground:true,opacity:.55f);
        }
        private static void PlayReady(StratagemEffectMessage message,StratagemUnit localRoot=null)
        {
            Vector2 point=new Vector2(message.X,message.Y),end=new Vector2(message.EndX,message.EndY);
            var root=localRoot;
            if(root==null)StratagemUnit.TryFind(message.Root,out root);
            string sound=message.Action==2?"barrage":message.Action==3?"laser-start":message.Action==8?"eagle":null;
            if(sound!=null)StratagemAudio.Play(sound,point);
            switch(message.Action)
            {
                case 0:Blast(point,message.Radius,false);break;
                case 1:root?.FadeBeacon();StratagemSurface.Create(StratagemSurfaceKind.Warning,point,message.Radius,.45f);break;
                case 2:
                    var streak=StratagemAssets.Effect("fx.streak",point+Vector2.up*3,.14f,.09f,opacity:.75f);
                    streak.transform.localScale=new Vector3(.14f/streak.sprite.bounds.size.x,3/streak.sprite.bounds.size.y,1);
                    streak.gameObject.AddComponent<StratagemTransit>().Set(point+Vector2.up*3,point,.09f);
                    Blast(point,message.Radius,false);
                    if(root!=null&&root.Parameters.Scorch)StratagemSurface.Create(StratagemSurfaceKind.Fire,point,1.2f,3);
                    break;
                case 3:root?.StartBeam(point,message.Duration,message.Radius);break;
                case 4:root?.BeamTarget(message.Target,point);break;
                case 5:root?.EndBeam();break;
                case 6:
                    root?.Visible(false);
                    var plane=StratagemAssets.Effect("old.3.0.0",point+Vector2.up*4,2.5f,.7f);
                    plane.transform.rotation=Quaternion.Euler(0,0,-90);
                    plane.gameObject.AddComponent<StratagemTransit>().Set(point+new Vector2(-8,4),point+new Vector2(8,4),.7f);
                    break;
                case 7:
                    var bomb=StratagemAssets.Effect("old.3.1.0",point+Vector2.up*5,.65f,.6f);
                    bomb.gameObject.AddComponent<StratagemTransit>().Set(point+Vector2.up*5,point,.6f);
                    StratagemAssets.Effect("old.3.1.4",point,.65f,Mathf.Max(.01f,message.Duration-.6f),delay:.6f,ground:true);
                    break;
                case 8:Blast(point,message.Radius,true);break;
                case 9:
                    root?.Aim(end-(Vector2)root.transform.position);root?.AttackAnimation();
                    Vector2 muzzle=root!=null?root.Muzzle:point;
                    StratagemAssets.Effect("mortar-spark",muzzle,.22f,.1f);
                    var shell=StratagemAssets.Effect("mortar-shell",muzzle,.35f,message.Duration);
                    shell.gameObject.AddComponent<StratagemTransit>().Set(muzzle,end,message.Duration,2);
                    break;
                case 10:
                    StratagemSurface.Create(message.Target==1?StratagemSurfaceKind.Gas:StratagemSurfaceKind.StaticField,point,message.Radius,message.Duration);
                    break;
                case 11:StratagemSurface.Create(StratagemSurfaceKind.Ripple,point,.5f,.22f);break;
                case 12:
                    var burst=StratagemSurface.Create(StratagemSurfaceKind.Ripple,point,message.Radius,.4f);
                    burst.gameObject.AddComponent<StratagemExpansion>().Set(.85f,1.1f,.4f);break;
                case 13:
                    root?.Aim(end-point);root?.AttackAnimation();
                    Vector2 source=root!=null?root.Muzzle:point;
                    var ray=StratagemAssets.Effect("old.2.1.0",(source+end)*.5f,.04f,.09f,opacity:.7f);
                    Vector2 difference=end-source;
                    ray.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(difference.y,difference.x)*Mathf.Rad2Deg-90);
                    ray.transform.localScale=new Vector3(.04f/ray.sprite.bounds.size.x,difference.magnitude/ray.sprite.bounds.size.y,1);
                    break;
                case 14:root?.RigDeath();break;
                case 15:root?.MineSetup(message.Target,message.EndX,message.Duration);break;
                case 16:root?.DronePhase((int)message.Duration,message.Target);break;
            }
        }
    }
    public sealed class StratagemTransit:MonoBehaviour
    {
        private Vector2 start,end;
        private float duration,started,height;
        internal void Set(Vector2 from,Vector2 to,float seconds,float arc=0)
        {start=from;end=to;duration=seconds;height=arc;started=Time.time;transform.position=new Vector3(from.x,from.y,StratagemAssets.Z(from.y)-.03f);}
        private void Update()
        {
            float t=Mathf.Clamp01((Time.time-started)/duration);
            Vector2 position=Vector2.Lerp(start,end,t)+Vector2.up*(4*t*(1-t)*height);
            transform.position=new Vector3(position.x,position.y,StratagemAssets.Z(position.y)-.03f);
        }
    }
    public sealed class StratagemExpansion:MonoBehaviour
    {
        private float from,to,started,duration;
        internal void Set(float start,float end,float seconds){from=start;to=end;duration=seconds;started=Time.time;transform.localScale=Vector3.one*from;}
        private void Update(){transform.localScale=Vector3.one*Mathf.Lerp(from,to,Mathf.Clamp01((Time.time-started)/duration));}
    }
}
