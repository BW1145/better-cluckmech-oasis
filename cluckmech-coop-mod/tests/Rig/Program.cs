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
        public GameObject(){transform=new Transform(this);}
        public GameObject(string name):this(){}
        public T AddComponent<T>() where T:new(){var value=new T();if(value is LineRenderer line){line.gameObject=this;LineRenderer.All.Add(line);}return value;}
    }
    public sealed class Transform
    {
        public GameObject owner;public Transform parent;public Vector3 position,localPosition,localScale=Vector3.one;public Quaternion localRotation;
        public Transform(GameObject owner){this.owner=owner;}
        public Vector3 TransformPoint(Vector3 p)=>localPosition+new Vector3(p.x*localScale.x,p.y*localScale.y,0);
    }
    public sealed class SpriteRenderer
    {
        public GameObject gameObject=>transform.owner;
        public Sprite sprite;public bool enabled=true;public int sortingLayerID,sortingOrder;public Color color=Color.white;public Transform transform;
    }
    public sealed class Sprite {public string Name;public Bounds bounds;}
    public struct Bounds {public Vector3 size;}
    public struct Quaternion {public float z;public static Quaternion Euler(float x,float y,float z)=>new(){z=z};}
    public struct Color
    {
        public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new(1,1,1);
    }
    public struct Vector2
    {
        public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float sqrMagnitude=>x*x+y*y;
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
        public static int FloorToInt(float x)=>(int)Math.Floor(x);public static int Min(int a,int b)=>Math.Min(a,b);
        public static float Cos(float x)=>(float)Math.Cos(x);public static float Repeat(float x,float length)=>x-(float)Math.Floor(x/length)*length;
        public static float Lerp(float a,float b,float t)=>a+(b-a)*t;public static float SmoothStep(float a,float b,float t)=>Lerp(a,b,t*t*(3-2*t));
        public static float LerpAngle(float a,float b,float t){float d=Repeat(b-a,360);if(d>180)d-=360;return a+d*Clamp01(t);}
    }
}
public sealed class 模块_基本属性 {public int ID;}
namespace CluckmechCoop
{
    internal enum StratagemKind {Mines,Barrage,Laser,Eagle,Mortar,Shield,Rover}
    public sealed class StratagemUnit:MonoBehaviour
    {
        internal 模块_基本属性 Stats=new();internal StratagemKind Kind;
        public T[] GetComponentsInChildren<T>(bool inactive)=>gameObject.Renderers.OfType<T>().ToArray();
    }
    internal static class StratagemAssets
    {
        internal static readonly List<SpriteRenderer> LaserParts=new();
        internal static float Z(float y)=>(y+10000)*.01f;
        internal static SpriteRenderer Renderer(Transform parent,Sprite sprite,float width=2)
        {var obj=new GameObject();var image=new SpriteRenderer{sprite=sprite,transform=obj.transform};obj.Renderers.Add(image);LaserParts.Add(image);return image;}
        internal static Dictionary<string,Dictionary<string,float>> Boxes;
        internal static Sprite Named(string name){var b=Boxes[name];return new Sprite{Name=name,bounds=new Bounds{size=new Vector3(b["W"]/32,b["H"]/32,0)}};}
        internal static Vector3 Muzzle(string name){var b=Boxes[name];return new Vector3(b["MX"],b["MY"],0);}
        internal static SpriteRenderer Part(Transform parent,string name,float width,Vector3 position)
        {
            var obj=new GameObject();obj.transform.parent=parent;obj.transform.localPosition=position;
            var sprite=Named(name);obj.transform.localScale=Vector3.one*(width/sprite.bounds.size.x);
            var renderer=new SpriteRenderer{sprite=sprite,transform=obj.transform};parent.owner.Renderers.Add(renderer);return renderer;
        }
        internal static void Effect(string name,Vector2 point,float width,float duration,bool ground=false,float rise=0,float grow=0){Named(name);}
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
