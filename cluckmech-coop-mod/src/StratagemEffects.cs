using System;
using System.Collections;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class StratagemEffects
    {
        internal static void Emit(StratagemUnit root, int action, Vector2 point, float radius = 1, float duration = 1, uint target = 0, Vector2 end = default)
        {
            var message = new StratagemEffectMessage { Kind = (int)root.Kind, Action = action, Root = StratagemWorld.Id(root.gameObject), Target = target,
                X = point.x, Y = point.y, EndX = end.x, EndY = end.y, Radius = radius, Duration = duration };
            PlayReady(message,root);
            if (Plugin.Role == 1) Plugin.Network.Broadcast(new SessionMessage { Type = "stratagem-effect", Token = Plugin.Combat.Token, Effect = message });
        }
        internal static void Play(StratagemEffectMessage message)
        {
            if (!Plugin.InCombat) return;
            if ((message.Action == 3 || message.Action == 4 || message.Action == 5 || message.Action == 15 || message.Action == 16) &&
                message.Root != 0 && !StratagemUnit.TryFind(message.Root,out _))
            { Plugin.Instance.StartCoroutine(WaitForUnit(message)); return; }
            PlayReady(message);
        }
        private static IEnumerator WaitForUnit(StratagemEffectMessage message)
        {
            float until = Time.time+2;
            while (Plugin.InCombat && Time.time < until && !StratagemUnit.TryFind(message.Root,out _)) yield return null;
            if (Plugin.InCombat && StratagemUnit.TryFind(message.Root,out _)) PlayReady(message);
        }
        private static void PlayReady(StratagemEffectMessage message,StratagemUnit localRoot = null)
        {
            var kind = (StratagemKind)message.Kind;
            Vector2 point = new Vector2(message.X,message.Y);
            var root = localRoot;
            if (root == null) StratagemUnit.TryFind(message.Root,out root);
            string sound = message.Action == 0 ? "mine" : message.Action == 2 ? "barrage" : message.Action == 3 ? "laser-start" :
                message.Action == 8 ? "eagle" : message.Action == 9 ? "ems" : message.Action == 11 ? "shield-hit" :
                message.Action == 12 ? "shield-break" : message.Action == 13 ? "rover" : null;
            if (sound != null) StratagemAudio.Play(sound,point);
            switch (message.Action)
            {
                case 0: StratagemAssets.Effect(StratagemKind.Mines,4,0,8,point,message.Radius*2,.6f); break;
                case 1: StratagemAssets.Effect(StratagemKind.Barrage,1,0,8,point,message.Radius*2,.45f); break;
                case 2:
                    var streak = StratagemAssets.Effect(StratagemKind.Barrage,5,0,4,point+Vector2.up*3,.18f,.09f);
                    streak.transform.localScale = new Vector3(.09f,3,1);
                    StratagemAssets.Effect(StratagemKind.Barrage,2,0,8,point,message.Radius*2,.75f);
                    StratagemAssets.Effect(StratagemKind.Barrage,3,0,8,point,message.Radius*2,.9f);
                    StratagemAssets.Effect(StratagemKind.Barrage,4,UnityEngine.Random.Range(0,3),1,point,message.Radius*1.5f,10);
                    break;
                case 3: root?.StartBeam(point,message.Duration,message.Radius); break;
                case 4: root?.BeamTarget(message.Target,point); break;
                case 5: root?.EndBeam(); break;
                case 6:
                    var plane = StratagemAssets.Effect(StratagemKind.Eagle,0,0,8,point+Vector2.up*4,2.5f,.7f);
                    plane.gameObject.AddComponent<StratagemTransit>().Set(point+new Vector2(-8,4),point+new Vector2(8,4),.7f);
                    break;
                case 7:
                    var bomb = StratagemAssets.Effect(StratagemKind.Eagle,1,0,1,point+Vector2.up*5,1.2f,.6f);
                    bomb.gameObject.AddComponent<StratagemTransit>().Set(point+Vector2.up*5,point,.6f);
                    Plugin.Instance.StartCoroutine(BuriedBomb(point,message.Duration));
                    break;
                case 8:
                    StratagemAssets.Effect(StratagemKind.Eagle,2,0,8,point,message.Radius*2,.8f);
                    StratagemAssets.Effect(StratagemKind.Eagle,3,0,8,point,message.Radius*2,1.1f);
                    StratagemAssets.Effect(StratagemKind.Eagle,4,0,8,point,message.Radius*2, .5f);
                    StratagemAssets.Effect(StratagemKind.Eagle,5,6,2,point,message.Radius*1.6f,12);
                    break;
                case 9:
                    root?.AttackAnimation();
                    var shell = StratagemAssets.Effect(StratagemKind.Mortar,2,4,4,point,.5f,.8f);
                    shell.gameObject.AddComponent<StratagemTransit>().Set(point,new Vector2(message.EndX,message.EndY),.8f,2);
                    break;
                case 10:
                    StratagemAssets.Effect(StratagemKind.Mortar,message.Target == 1 ? 5 : 3,0,8,point,message.Radius*2,message.Duration,true);
                    break;
                case 11: StratagemAssets.Effect(StratagemKind.Shield,3,0,6,point,1.5f,.4f); break;
                case 12:
                    StratagemAssets.Effect(StratagemKind.Shield,4,0,8,point,message.Radius*2,.6f);
                    break;
                case 13:
                    root?.AttackAnimation();
                    Vector2 destination = new Vector2(message.EndX,message.EndY);
                    var ray = StratagemAssets.Effect(StratagemKind.Laser,1,0,1,(point+destination)*.5f,.05f,.09f);
                    Vector2 difference = destination-point;
                    ray.transform.rotation = Quaternion.Euler(0,0,Mathf.Atan2(difference.y,difference.x)*Mathf.Rad2Deg-90);
                    ray.transform.localScale = new Vector3(.05f,difference.magnitude/ray.sprite.bounds.size.y,1);
                    break;
                case 14:
                    int row = kind == StratagemKind.Mines ? 3 : 5;
                    StratagemAssets.Effect(kind,row,0,kind == StratagemKind.Mines ? 6 : 8,point,2,.6f);
                    break;
                case 15: root?.MineSetup(message.Target,message.EndX,message.Duration); break;
                case 16: root?.DronePhase((int)message.Duration,message.Target); break;
            }
        }
        private static IEnumerator BuriedBomb(Vector2 point,float duration)
        {
            yield return new WaitForSeconds(.6f);
            if (Plugin.InCombat) StratagemAssets.Effect(StratagemKind.Eagle,1,4,4,point,1.2f,duration-.6f);
        }
    }
    public sealed class StratagemTransit : MonoBehaviour
    {
        private Vector2 start,end;
        private float duration,started,height;
        internal void Set(Vector2 from,Vector2 to,float seconds,float arc = 0)
        { start = from; end = to; duration = seconds; height = arc; started = Time.time; }
        private void Update()
        {
            float t = Mathf.Clamp01((Time.time-started)/duration);
            Vector2 position = Vector2.Lerp(start,end,t)+Vector2.up*(4*t*(1-t)*height);
            transform.position = new Vector3(position.x,position.y,(position.y+10000)*.01f-.03f);
        }
    }
}
