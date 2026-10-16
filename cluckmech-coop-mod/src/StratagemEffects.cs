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
        private static void PlayReady(StratagemEffectMessage message,StratagemUnit localRoot=null)
        {
            Vector2 point=new Vector2(message.X,message.Y),end=new Vector2(message.EndX,message.EndY);
            var root=localRoot;
            if(root==null)StratagemUnit.TryFind(message.Root,out root);
            string sound=message.Action==2?"barrage":message.Action==3?"laser-start":message.Action==8?"eagle":null;
            if(sound!=null)StratagemAudio.Play(sound,point);
            switch(message.Action)
            {
                case 0:StratagemBlastVisual.Play(point,message.Radius,false);break;
                case 1:root?.FadeBeacon();StratagemSurface.Create(StratagemSurfaceKind.Warning,point,message.Radius,.45f);break;
                case 2:
                    StratagemBlastVisual.Play(point,message.Radius,false);
                    if(root!=null&&root.Parameters.Scorch)StratagemSurface.Create(StratagemSurfaceKind.Fire,point,1.2f,message.Duration);
                    break;
                case 3:root?.StartBeam(point,message.Duration,message.Radius);break;
                case 4:root?.BeamTarget(message.Target,point);break;
                case 5:root?.EndBeam();break;
                case 6:
                    root?.Visible(false);
                    var plane=StratagemAssets.Animated("fx.eagle-flight.",8,point+Vector2.up*4,2.5f,.7f,hold:true);
                    plane.transform.rotation=Quaternion.Euler(0,0,-90);
                    plane.gameObject.AddComponent<StratagemTransit>().Set(point+new Vector2(-8,4),point+new Vector2(8,4),.7f);
                    break;
                case 7:
                    var bomb=StratagemAssets.Effect("old.3.1.0",point+Vector2.up*5,.65f,message.Duration,hold:true);
                    bomb.GetComponent<StratagemAnimation>().Steady=true;
                    bomb.gameObject.AddComponent<StratagemBombDrop>().Set(point,message.Duration);
                    break;
                case 8:StratagemBlastVisual.Play(point,message.Radius,true);break;
                case 9:
                    root?.Aim(end-(Vector2)root.transform.position);root?.AttackAnimation();
                    Vector2 muzzle=root!=null?root.Muzzle:point;
                    StratagemAssets.Effect("mortar-spark",muzzle,.22f,.1f);
                    var shell=StratagemAssets.Effect("mortar-shell",muzzle,.35f,message.Duration);
                    shell.gameObject.AddComponent<StratagemTransit>().Set(muzzle,end,message.Duration,2,true);
                    break;
                case 10:
                    StratagemSurface.Create(message.Target==1?StratagemSurfaceKind.Gas:StratagemSurfaceKind.StaticField,point,message.Radius,message.Duration);
                    break;
                case 11:root?.GetComponent<StratagemShield>()?.HitVisual(point);break;
                case 12:root?.GetComponent<StratagemShield>()?.BreakVisual();break;
                case 13:
                    root?.Aim(end-point);root?.AttackAnimation();
                    root?.DroneBeam(end,message.Duration,message.Target,message.Radius);
                    break;
                case 14:root?.RigDeath();break;
                case 15:root?.MineSetup(message.Target,message.EndX,message.Duration);break;
                case 16:root?.DronePhase((int)message.Duration,message.Target);break;
                case 17:
                    var streak=StratagemAssets.Effect("fx.streak",point+Vector2.up*3,.14f,message.Duration,hold:true);
                    streak.transform.localScale=new Vector3(.14f/streak.sprite.bounds.size.x,3/streak.sprite.bounds.size.y,1);
                    streak.gameObject.AddComponent<StratagemTransit>().Set(point+Vector2.up*3,point,message.Duration);
                    break;
            }
        }
    }
}
