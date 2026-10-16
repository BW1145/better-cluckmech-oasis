using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using CluckmechCoop;

namespace UnityEngine
{
    public class MonoBehaviour
    {
        public GameObject gameObject;public Transform transform=>gameObject.transform;
        public T GetComponent<T>()=>gameObject.GetComponent<T>();
        public static void Destroy(object value)=>Object.Destroy(value);
    }
    public static class Object{public static void Destroy(object value){if(value is GameObject go)go.Renderers.Clear();}}
    public sealed class Shader{public static Shader Find(string name)=>new();}
    public sealed class Material{public Material(Shader shader){}}
    public struct Keyframe{public float time,value;public Keyframe(float time,float value){this.time=time;this.value=value;}}
    public sealed class AnimationCurve{public Keyframe[] keys;public AnimationCurve(params Keyframe[] keys){this.keys=keys;}}
    public sealed class LineRenderer
    {
        public static readonly List<LineRenderer> All=new();
        public GameObject gameObject;public Material sharedMaterial;public bool useWorldSpace;public int sortingLayerID,sortingOrder,positionCount;
        public AnimationCurve widthCurve;public float widthMultiplier;public Color startColor,endColor;public Vector3[] positions=new Vector3[12];
        public void SetPosition(int index,Vector3 point)=>positions[index]=point;
    }
    public sealed class GameObject
    {
        public Transform transform;public readonly List<SpriteRenderer> Renderers=new();
        public readonly Dictionary<Type,object> Components=new();
        public T GetComponent<T>()=>Components.TryGetValue(typeof(T),out var value)?(T)value:default;
        public GameObject(){transform=new Transform(this);}
        public GameObject(string name):this(){}
        public T AddComponent<T>() where T:new(){var value=new T();if(value is MonoBehaviour behaviour)behaviour.gameObject=this;if(value is LineRenderer line){line.gameObject=this;LineRenderer.All.Add(line);}Components[typeof(T)]=value;return value;}
    }
    public sealed class Transform
    {
        public GameObject owner;public Transform parent;public Vector3 position,localPosition,localScale=Vector3.one;public Quaternion localRotation,rotation;
        public Transform(GameObject owner){this.owner=owner;}
        public void SetParent(Transform value,bool world){parent=value;}
        public Vector3 TransformPoint(Vector3 p)=>localPosition+new Vector3(p.x*localScale.x,p.y*localScale.y,0);
    }
    public sealed class SpriteRenderer
    {
        public GameObject gameObject=>transform.owner;
        public T GetComponent<T>()=>gameObject.GetComponent<T>();
        public Sprite sprite;public bool enabled=true;public int sortingLayerID,sortingOrder;public Color color=Color.white;public Transform transform;
    }
    public sealed class Sprite {public string Name;public Bounds bounds;}
    public struct Bounds {public Vector3 size;}
    public struct Quaternion {public float z;public static Quaternion identity=>new();public static Quaternion Euler(float x,float y,float z)=>new(){z=z};}
    public struct Color
    {
        public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new(1,1,1);
    }
    public struct Vector2
    {
        public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float sqrMagnitude=>x*x+y*y;
        public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
        public Vector2 normalized=>sqrMagnitude>0?this/(float)Math.Sqrt(sqrMagnitude):zero;
        public static Vector2 zero=>new(0,0);public static Vector2 up=>new(0,1);public static Vector2 left=>new(-1,0);
        public static Vector2 operator +(Vector2 a,Vector2 b)=>new(a.x+b.x,a.y+b.y);
        public static Vector2 operator -(Vector2 a,Vector2 b)=>new(a.x-b.x,a.y-b.y);
        public static Vector2 operator *(Vector2 a,float b)=>new(a.x*b,a.y*b);public static Vector2 operator /(Vector2 a,float b)=>new(a.x/b,a.y/b);
        public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>a+(b-a)*t;
        public static implicit operator Vector3(Vector2 v)=>new(v.x,v.y,0);
    }
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 one=>new(1,1,1);
        public Vector3 normalized=>this*(1/(float)Math.Sqrt(x*x+y*y+z*z));
        public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static implicit operator Vector2(Vector3 v)=>new(v.x,v.y);
    }
    public static class Time {public static float time;}
    public static class Mathf
    {
        public const float PI=(float)Math.PI,Rad2Deg=180/PI;
        public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);public static int RoundToInt(float x)=>(int)Math.Round(x);
        public static float Clamp01(float x)=>Math.Clamp(x,0,1);public static float Sin(float x)=>(float)Math.Sin(x);
        public static int FloorToInt(float x)=>(int)Math.Floor(x);public static int Min(int a,int b)=>Math.Min(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);
        public static float Cos(float x)=>(float)Math.Cos(x);public static float Repeat(float x,float length)=>x-(float)Math.Floor(x/length)*length;
        public static float Lerp(float a,float b,float t)=>a+(b-a)*t;public static float SmoothStep(float a,float b,float t)=>Lerp(a,b,t*t*(3-2*t));
        public static float LerpAngle(float a,float b,float t){float d=Repeat(b-a,360);if(d>180)d-=360;return a+d*Clamp01(t);}
    }
}
public sealed class 模块_基本属性 {public int ID;public float 当前生命值=100;public GameObject gameObject=new();public Transform transform=>gameObject.transform;}
namespace CluckmechCoop
{
    internal static class Plugin {internal static bool InCombat=true;internal static CombatState Combat=new();}
    internal sealed class CombatState {internal string Token="visual";}
    internal enum StratagemParticleKind {Fire,Sparks,Electric}
    internal static class StratagemNativeParticles {internal static void Play(StratagemParticleKind k,Vector2 p,float scale,float seconds,bool loop=false,Transform parent=null){}}
    internal static class StratagemWorld
    {
        internal static 模块_基本属性 Target(Vector2 p,float r,bool boss)=>null;
        internal static 模块_基本属性 Enemy(uint id)=>null;
    }
    internal enum StratagemKind {Mines,Barrage,Laser,Eagle,Mortar,Shield,Rover}
    public sealed class StratagemUnit:MonoBehaviour
    {
        internal 模块_基本属性 Stats=new();internal StratagemKind Kind;
        internal bool Alive=>Stats.当前生命值>0;internal Vector2 Muzzle=>transform.position;
        public T[] GetComponentsInChildren<T>(bool inactive)=>gameObject.Renderers.OfType<T>().ToArray();
    }
    internal static class StratagemAssets
    {
        internal static readonly List<SpriteRenderer> LaserParts=new();
        internal static float Z(float y)=>(y+10000)*.01f;
        internal static SpriteRenderer Renderer(Transform parent,Sprite sprite,float width=2)
        {var obj=new GameObject();obj.transform.parent=parent;obj.transform.localScale=Vector3.one*(width/sprite.bounds.size.x);var image=new SpriteRenderer{sprite=sprite,transform=obj.transform};obj.Renderers.Add(image);obj.Components[typeof(SpriteRenderer)]=image;LaserParts.Add(image);return image;}
        internal static Dictionary<string,Dictionary<string,float>> Boxes;
        internal static Sprite Named(string name){var b=Boxes[name];return new Sprite{Name=name,bounds=new Bounds{size=new Vector3(b["W"]/32,b["H"]/32,0)}};}
        internal static Vector3 Muzzle(string name){var b=Boxes[name];return new Vector3(b["MX"],b["MY"],0);}
        internal static SpriteRenderer Part(Transform parent,string name,float width,Vector3 position)
        {
            var obj=new GameObject();obj.transform.parent=parent;obj.transform.localPosition=position;
            var sprite=Named(name);obj.transform.localScale=Vector3.one*(width/sprite.bounds.size.x);
            var renderer=new SpriteRenderer{sprite=sprite,transform=obj.transform};parent.owner.Renderers.Add(renderer);obj.Renderers.Add(renderer);return renderer;
        }
        internal static SpriteRenderer Effect(string name,Vector2 point,float width,float duration,float delay=0,bool ground=false,float rise=0,float grow=0,float opacity=1,bool hold=false)
        {var image=Renderer(null,Named(name),width);image.transform.position=new Vector3(point.x,point.y,Z(point.y)+(ground?.4f:-.025f));image.color=new Color(1,1,1,opacity);image.gameObject.AddComponent<StratagemAnimation>().Set(image,duration,delay,grow,rise,ground,hold);return image;}
        internal static SpriteRenderer Animated(string prefix,int count,Vector2 point,float width,float duration,float delay=0,bool ground=false,float opacity=1,bool hold=false)
        {var image=Effect(prefix+"0",point,width,duration,delay,ground,opacity:opacity,hold:hold);image.gameObject.AddComponent<StratagemFlipbook>().Set(image,prefix,count,duration,delay);return image;}
        internal static void Flipbook(string prefix,int count,Vector2 point,float width,float duration,float delay=0,bool ground=false,float opacity=1,bool hold=false)=>Animated(prefix,count,point,width,duration,delay,ground,opacity,hold);
    }
}
static class Program
{
    private static readonly MethodInfo update=typeof(StratagemRig).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance);
    private static int checks;
    private static void Check(bool yes,string message){if(!yes)throw new Exception(message);checks++;}
    static void Main(string[] args)
    {
        string root=Path.GetFullPath(args[0]);
        StratagemAssets.Boxes=JsonSerializer.Deserialize<Dictionary<string,Dictionary<string,float>>>(File.ReadAllText(Path.Combine(root,"src/StratagemAssets/stratagem-v2.json")));
        var frames=new List<object>();
        if(args.Length>1&&args[1]=="--vfx-revision")
        {
            bool checkTail=!args.Contains("--render-only"),checkOpening=!args.Contains("--tail")&&!args.Contains("--render-only");
            void Invoke(object value,string method="Update")=>value.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(value,null);
            void AdvanceSprites(){foreach(var image in StratagemAssets.LaserParts.ToArray())foreach(var part in image.gameObject.Components.Values.OfType<MonoBehaviour>().ToArray())if(part is StratagemAnimation||part is StratagemFlipbook||part is StratagemBombDrop||part is StratagemTransit)Invoke(part);}
            void Capture(string kind,float t,IEnumerable<SpriteRenderer> images)
            {
                frames.Add(new{kind,time=t,parts=images.Where(x=>x.enabled&&x.gameObject.Renderers.Contains(x)).Select(x=>new{name=x.sprite.Name,
                    x=x.transform.parent==null?x.transform.position.x:x.transform.parent.position.x+x.transform.localPosition.x,
                    y=x.transform.parent==null?x.transform.position.y:x.transform.parent.position.y+x.transform.localPosition.y,
                    z=x.transform.parent==null?x.transform.position.z:x.transform.parent.position.z+x.transform.localPosition.z,
                    sx=x.transform.localScale.x,sy=x.transform.localScale.y,angle=x.transform.rotation.z+x.transform.localRotation.z,alpha=x.color.a}).ToArray()});
            }
            foreach(int id in new[]{20001,20002,20003,20004,20005,20006,20007,20101,20102})
            {
                Time.time=0;var go=new GameObject();var sample=new SpriteRenderer{transform=go.transform,sortingLayerID=4,sortingOrder=12,enabled=false};go.Renderers.Add(sample);
                var unit=new StratagemUnit{gameObject=go,Stats=new(){ID=id},Kind=(StratagemKind)(id==20101?0:id==20102?6:id-20001)};
                var rig=new StratagemRig{gameObject=go};rig.Build(unit);
                if(checkOpening)Check(go.Renderers.All(x=>x.sortingLayerID==4&&x.sortingOrder==12),"Every rig and dependent inherits the native sorting layer");
                if(id==20101)rig.LaunchMine(new Vector2(-1,1),.3f);
                foreach(float t in new[]{0f,.15f,.3f,.8f}){Time.time=t;Invoke(rig);Capture("rig-"+id,t,go.Renderers);}
                if(id==20005)for(int face=0;face<8;face++)
                {
                    float angle=(180-face*45)*Mathf.PI/180;rig.Aim(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)));Time.time=1+face*.4f;rig.Shoot();
                    foreach(float step in new[]{0f,.06f,.3f}){Time.time=1+face*.4f+step;Invoke(rig);Capture("mortar-"+face,Time.time,go.Renderers);}
                    if(checkOpening)Check(rig.Body.transform.localPosition.x==0&&rig.Body.transform.localPosition.y==0,"Mortar feet remain fixed while firing");
                }
            }
            StratagemAssets.LaserParts.Clear();Time.time=0;
            var field=StratagemSurface.Create(StratagemSurfaceKind.StaticField,new Vector2(0,0),2,4);
            if(checkOpening)Check(StratagemAssets.LaserParts.Count==1,"Electric field has a single drawn body");
            foreach(float t in new[]{0f,.08f,.16f,.24f,.32f,.5f,.67f,.83f,1f,3.69f,3.77f,3.85f,3.93f}){Time.time=t;Invoke(field);Capture("ems",t,StratagemAssets.LaserParts);}
            Time.time=1;var overlap=StratagemSurface.Create(StratagemSurfaceKind.StaticField,new Vector2(1,0),2,4);Time.time=2;Invoke(field);Invoke(overlap);Capture("ems-overlap",2,StratagemAssets.LaserParts);
            foreach(var kind in new[]{StratagemSurfaceKind.Gas,StratagemSurfaceKind.Fire})
            {StratagemAssets.LaserParts.Clear();Time.time=0;var area=StratagemSurface.Create(kind,new Vector2(0,0),2,4);foreach(float t in new[]{.1f,.3f,.5f,1f,3.9f}){Time.time=t;Invoke(area);Capture(kind.ToString(),t,StratagemAssets.LaserParts);}}
            StratagemAssets.LaserParts.Clear();Time.time=0;
            var shieldOwner=new StratagemUnit{gameObject=new GameObject(),Stats=new(){ID=20006}};
            var shield=StratagemShieldVisual.Create(shieldOwner);shield.SetOpen(true,4.5f);
            foreach(float t in new[]{0f,.16f,.32f,.48f,.64f,.8f,1f,1.16f,1.32f}){Time.time=t;Invoke(shield);Capture("shield",t,StratagemAssets.LaserParts);}
            int hitIndex=0;
            foreach(var point in new[]{new Vector2(4.5f,0),new Vector2(0,4.5f),new Vector2(-4.5f,0)})
            {
                float hitTime=2+hitIndex++*.8f;Time.time=hitTime;shield.Hit(point);var patch=StratagemAssets.LaserParts.Last();
                if(checkOpening)Check((patch.transform.position.x-point.x)*(patch.transform.position.x-point.x)+(patch.transform.position.y-point.y)*(patch.transform.position.y-point.y)<.01f,"Shield hit stays at the actual crossing point");
                for(int n=0;n<8;n++){Time.time=hitTime+n*.06f;AdvanceSprites();Invoke(shield);Capture("shield-hit-"+point.x+"-"+point.y,Time.time,StratagemAssets.LaserParts);}
            }
            Time.time=5;shield.SetOpen(false,4.5f);
            for(int n=0;n<10;n++){Time.time=5+n*.08f;Invoke(shield);AdvanceSprites();Capture("shield-break",Time.time,StratagemAssets.LaserParts);}
            if(checkTail)Check(!StratagemAssets.LaserParts[0].enabled,"Broken shield completes its drawn collapse");
            Time.time=6;shield.SetOpen(true,5.5f);Invoke(shield);Capture("shield-reboot",6,StratagemAssets.LaserParts);
            foreach(bool heavy in new[]{false,true})
            {
                StratagemAssets.LaserParts.Clear();Time.time=0;StratagemBlastVisual.Play(new Vector2(0,0),heavy?4:1.5f,heavy);
                for(int n=0;n<12;n++){Time.time=n*.1f;AdvanceSprites();Capture(heavy?"eagle-blast":"barrage-blast",Time.time,StratagemAssets.LaserParts);}
            }
            StratagemAssets.LaserParts.Clear();Time.time=0;
            var bomb=StratagemAssets.Effect("old.3.1.0",new Vector2(0,5),.65f,1.6f,hold:true);bomb.GetComponent<StratagemAnimation>().Steady=true;var drop=bomb.gameObject.AddComponent<StratagemBombDrop>();drop.Set(new Vector2(0,0),1.6f);
            foreach(float t in new[]{0f,.3f,.59f,.6f,.65f,.7f,.75f,.85f,1.3f}){Time.time=t;AdvanceSprites();Capture("eagle-drop",t,new[]{bomb});}
            StratagemAssets.LaserParts.Clear();Time.time=0;
            var laser=new StratagemLaserVisual(.85f,new SpriteRenderer());
            foreach(float t in new[]{0f,.08f,.16f,.24f,.32f,1f,1.1f,1.2f,2.69f,2.77f,2.85f,2.93f}){laser.Tick(new Vector2(0,0),t,3-t);Capture("orbital",t,StratagemAssets.LaserParts);}
            var ground=StratagemAssets.LaserParts[1];var column=StratagemAssets.LaserParts[0];
            if(checkTail)Check(Math.Abs(column.transform.position.y-(ground.transform.position.y+ground.sprite.bounds.size.y*ground.transform.localScale.y/3))<.0001f,"Orbital column and contact texture meet");
            Time.time=5;laser.Dispose(true);Time.time=5.16f;AdvanceSprites();Capture("orbital-stop",5.16f,StratagemAssets.LaserParts);
            File.WriteAllText(args[2],JsonSerializer.Serialize(frames));Console.WriteLine($"PASS: {checks} focused visual checks; {frames.Count} production poses");return;
        }
        if(args.Length>1&&args[1]=="--rover-beam")
        {
            var go=new GameObject();var unit=new StratagemUnit{gameObject=go,Stats=new(){ID=20102},Kind=StratagemKind.Rover};
            var rig=new StratagemRig{gameObject=go};rig.Build(unit);go.Components[typeof(StratagemRig)]=rig;
            var beam=new StratagemDroneBeam{gameObject=go};
            var tick=typeof(StratagemDroneBeam).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance);
            foreach(float aim in new[]{1f,3f})
            {
                beam.Set(unit,new Vector2(aim,0),1,0,4.5f);tick.Invoke(beam,null);
                var ray=StratagemAssets.LaserParts[0];
                Check(Math.Abs(ray.sprite.bounds.size.y*ray.transform.localScale.y-4.5f)<.0001f,"Piercing laser has fixed range independent of target distance");
                Check(Math.Abs(ray.transform.position.x-2.25f)<.0001f,"Fixed beam center corresponds to its full endpoint");
                frames.Add(new{aim,parts=StratagemAssets.LaserParts.Select(x=>new{name=x.sprite.Name,x=x.transform.position.x,y=x.transform.position.y,z=x.transform.position.z,sx=x.transform.localScale.x,sy=x.transform.localScale.y,angle=x.transform.rotation.z,alpha=x.color.a}).ToArray()});
            }
            beam.Stop();Check(StratagemAssets.LaserParts.All(x=>!x.enabled),"Beam and contact stop together");
            File.WriteAllText(args[2],JsonSerializer.Serialize(frames));Console.WriteLine($"PASS: {checks} fixed beam visual checks");return;
        }
        if(args.Length>1&&args[1]=="--dock-alignment")
        {
            var go=new GameObject();var unit=new StratagemUnit{gameObject=go,Stats=new(){ID=20102},Kind=StratagemKind.Rover};
            var rig=new StratagemRig{gameObject=go};rig.Build(unit);
            for(int n=0;n<8;n++)
            {
                float angle=n*Mathf.PI/4;var flight=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                var aligned=StratagemRig.DockDirection(flight,0);
                Check(Math.Abs(aligned.x)<.0001f&&Math.Abs(aligned.y-1)<.0001f,"Every return approach ends aligned to the dock");
                rig.Aim(flight);rig.Dock(2,.5f);
                Check(rig.Body.sprite.Name=="rover-drone.2","Docked state uses the same direction for both peers");
                for(int step=0;step<=4;step++)
                {
                    rig.Aim(StratagemRig.DockDirection(flight,.7f*(1-step/4f)));
                    frames.Add(new{type="alignment",id=20102,phase=n,time=step*.1f,parts=go.Renderers.Select(x=>new{name=x.sprite.Name,x=x.transform.localPosition.x,y=x.transform.localPosition.y,z=x.transform.localPosition.z,sx=x.transform.localScale.x,sy=x.transform.localScale.y,alpha=x.color.a}).ToArray()});
                }
            }
            File.WriteAllText(args[2],JsonSerializer.Serialize(frames));Console.WriteLine($"PASS: {checks} dock alignment checks");return;
        }
        if(args.Length>1 && args[1]=="--rover-visual")
        {
            foreach(int id in new[]{20007,20102})
            {
                Time.time=0;var go=new GameObject();var unit=new StratagemUnit{gameObject=go,Stats=new(){ID=id},Kind=StratagemKind.Rover};
                var rig=new StratagemRig{gameObject=go};rig.Build(unit);
                foreach(int phase in new[]{3,0,1,2,3,0})
                {
                    rig.Dock(phase,.5f);
                    foreach(float step in new[]{0f,.25f,.5f})
                    {
                        Time.time+=step;update.Invoke(rig,null);
                        Check(go.Renderers.Count(x=>x.enabled)==1,"Each dock and drone has one body without muzzle flash");
                        Check(Math.Abs(rig.Body.sprite.bounds.size.x*rig.Body.transform.localScale.x-(id==20007?1.15f:.85f)*.75f)<.001f,"Docking preserves physical model scale");
                        frames.Add(new{type="rover",id,phase,time=Time.time,parts=go.Renderers.Where(x=>x.enabled).Select(x=>new{name=x.sprite.Name,x=x.transform.localPosition.x,y=x.transform.localPosition.y,z=x.transform.localPosition.z,sx=x.transform.localScale.x,sy=x.transform.localScale.y,alpha=x.color.a}).ToArray()});
                    }
                }
            }
            var laser=new StratagemLaserVisual(.85f,new SpriteRenderer());
            foreach(float t in new[]{.001f,.161f,1f,14.681f,14.961f})
            {
                laser.Tick(new Vector2(0,0),t,15-t);
                Check(StratagemAssets.LaserParts[0].color.a==1&&Math.Abs(StratagemAssets.LaserParts[1].color.a-.95f)<.001f,"Laser retains source brightness throughout all drawn phases");
                frames.Add(new{type="laser",id=20003,phase=0,time=t,parts=StratagemAssets.LaserParts.Select(x=>new{name=x.sprite.Name,x=x.transform.position.x,y=x.transform.position.y,z=x.transform.position.z,sx=x.transform.localScale.x,sy=x.transform.localScale.y,alpha=x.color.a}).ToArray()});
            }
            File.WriteAllText(args[2],JsonSerializer.Serialize(frames));
            Console.WriteLine($"PASS: {checks} focused dock and laser checks");return;
        }
        if(args.Length>1 && args[1]=="--laser-seam")
        {
            var laser=new StratagemLaserVisual(.85f,new SpriteRenderer());laser.Tick(new Vector2(0,0),1,14);
            var beam=StratagemAssets.LaserParts[0];var ground=StratagemAssets.LaserParts[1];
            float groundTop=ground.transform.position.y+(1-StratagemAssets.Boxes[ground.sprite.Name]["PY"])*ground.sprite.bounds.size.y*ground.transform.localScale.y;
            Check(Math.Abs(beam.transform.position.y-groundTop)<.00001f,"Beam and ground texture meet at the same pixel boundary");
            File.WriteAllText(args[2],JsonSerializer.Serialize(StratagemAssets.LaserParts.Select(x=>new{name=x.sprite.Name,x=x.transform.position.x,y=x.transform.position.y,z=x.transform.position.z,sx=x.transform.localScale.x,sy=x.transform.localScale.y,alpha=x.color.a}).ToArray()));
            laser.Dispose();Console.WriteLine("PASS: beam-ground texture join");return;
        }
        if(args.Length>1 && args[1]=="--revision")
        {
            foreach(int id in new[]{20001,20002,20003,20004,20005,20006,20007})
            {
                Time.time=0;var go=new GameObject();var unit=new StratagemUnit{gameObject=go,Stats=new(){ID=id},Kind=(StratagemKind)(id-20001)};
                var rig=new StratagemRig{gameObject=go};rig.Build(unit);Time.time=1;update.Invoke(rig,null);
                float expected=id==20001||id==20005||id==20006?1.8f:id==20007?.85f:1.2f;
                Check(Math.Abs(rig.Body.sprite.bounds.size.x*rig.Body.transform.localScale.x-expected*.75f)<.001f,"Deployed visual width follows 75 percent scale");
                frames.Add(new{type="rig",id,time=1f,parts=go.Renderers.Where(x=>x.enabled).Select(x=>new{name=x.sprite.Name,x=x.transform.localPosition.x,y=x.transform.localPosition.y,z=x.transform.localPosition.z,sx=x.transform.localScale.x,sy=x.transform.localScale.y,alpha=x.color.a}).ToArray()});
            }
            var laser=new StratagemLaserVisual(.85f,new SpriteRenderer());
            foreach(var phase in new[]{"start","loop","end"})for(int i=0;i<8;i++)
            {
                float t=phase=="start"?i*.04f+.001f:phase=="loop"?1+i*.0625f:14.681f+i*.04f;
                laser.Tick(new Vector2(t*.1f,0),t,15-t);
                var parts=StratagemAssets.LaserParts.ToArray();
                Check(parts.Length==2&&LineRenderer.All.Count==0,"Texture column and ground are reused without line geometry");
                Check(parts[0].sprite.Name.StartsWith("fx.orbital."+phase+".beam.")&&parts[1].sprite.Name.StartsWith("fx.orbital."+phase+".ground."),"Three phases select their own drawn frames");
                Check(parts[1].transform.position.z>parts[0].transform.position.z,"Ground contact sorts behind the column");
                frames.Add(new{type="laser",phase,time=t,parts=parts.Select(x=>new{name=x.sprite.Name,x=x.transform.position.x,y=x.transform.position.y,z=x.transform.position.z,sx=x.transform.localScale.x,sy=x.transform.localScale.y,alpha=x.color.a}).ToArray()});
            }
            File.WriteAllText(args[2],JsonSerializer.Serialize(frames));
            laser.Dispose();Console.WriteLine($"PASS: {checks} focused size, phase and layering checks");return;
        }
        foreach(int id in new[]{20001,20002,20003,20004,20005,20006,20007,20101,20102})
        {
            Time.time=0;var go=new GameObject();var unit=new StratagemUnit{gameObject=go,Stats=new(){ID=id},Kind=(StratagemKind)(id==20101?0:id==20102?6:id-20001)};
            var rig=new StratagemRig{gameObject=go};rig.Build(unit);
            var basePart=rig.Body;var anchor=basePart.transform.localPosition;var scale=basePart.transform.localScale;
            void Snapshot(string stage)
            {
                update.Invoke(rig,null);
                frames.Add(new{id,stage,time=Time.time,parts=go.Renderers.Where(x=>x.enabled).Select(x=>new{name=x.sprite.Name,x=x.transform.localPosition.x,y=x.transform.localPosition.y,z=x.transform.localPosition.z,sx=x.transform.localScale.x,sy=x.transform.localScale.y,angle=x.transform.localRotation.z,alpha=x.color.a}).ToArray()});
            }
            if(id==20101)rig.LaunchMine(new Vector2(1,.8f),.3f);
            foreach(float t in new[]{0f,.2f,.4f,.8f,1f}){Time.time=t;Snapshot("deploy");}
            for(int n=0;n<8;n++)
            {
                float a=(180-n*45)*Mathf.PI/180;rig.Aim(new Vector2((float)Math.Cos(a),(float)Math.Sin(a)));Time.time=2+n;
                Snapshot("aim-"+n);rig.Shoot();Time.time+=.06f;Snapshot("fire-"+n);Time.time+=.24f;Snapshot("rest-"+n);
                if(id<20100)Check(basePart.transform.localPosition.x==anchor.x&&basePart.transform.localPosition.y==anchor.y&&basePart.transform.localScale.x==scale.x,"Fixed base remains unchanged while aiming and shooting");
                if(id==20005)Check(float.IsFinite(rig.Muzzle.x)&&float.IsFinite(rig.Muzzle.y),"Every barrel has finite muzzle coordinates");
            }
            if(id==20102)
            {
                Time.time=12;rig.Dock(1,.5f);
                foreach(float t in new[]{12f,12.25f,12.5f}){Time.time=t;Snapshot("return");}
                rig.Dock(2,0);Snapshot("dock");Check(go.Renderers.Count(x=>x.enabled&&x.sprite.Name.StartsWith("rover-drone"))==1,"Docked state contains exactly one drone");
                rig.Dock(3,.5f);foreach(float t in new[]{12.5f,12.75f,13f}){Time.time=t;Snapshot("launch");}
            }
            Time.time=14;rig.Die();Snapshot("destroyed");Check(!go.Renderers.Any(x=>x.enabled),"Destroyed rig does not re-enable its body or flashes");
        }
        Directory.CreateDirectory(Path.Combine(root,"evidence/0.5.1"));
        var first=new StratagemRig{gameObject=new GameObject()};var second=new StratagemRig{gameObject=new GameObject()};
        first.Build(new StratagemUnit{gameObject=first.gameObject,Stats=new(){ID=20102},Kind=StratagemKind.Rover});
        second.Build(new StratagemUnit{gameObject=second.gameObject,Stats=new(){ID=20102},Kind=StratagemKind.Rover});
        second.Dock(0,0);
        first.Dock(2,0);update.Invoke(first,null);update.Invoke(second,null);
        Check(first.Body.transform.localScale.x==second.Body.transform.localScale.x,"Docking preserves both drones' physical size");
        Check(first.gameObject.Renderers.Count(x=>x.enabled&&x.sprite.Name.StartsWith("rover-drone"))==1&&second.gameObject.Renderers.Count(x=>x.enabled&&x.sprite.Name.StartsWith("rover-drone"))==1,"Two bases maintain two independent drone renderers");
        File.WriteAllText(Path.Combine(root,"evidence/0.5.1/rig-poses.json"),JsonSerializer.Serialize(frames));
        Console.WriteLine($"PASS: {checks} production rig checks; {frames.Count} animation poses exported");
    }
}
