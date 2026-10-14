using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    internal enum StratagemSurfaceKind { StaticField,Gas,Fire,Shield,Warning,Ripple }
    public sealed class StratagemSurface : MonoBehaviour
    {
        private static Material material;
        private static readonly List<StratagemSurface> fields=new List<StratagemSurface>();
        private Mesh mesh;
        private MeshRenderer renderer;
        private readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        private float started,duration,radius;
        private int overlaps;
        private string token;
        private bool field;
        internal static StratagemSurface Create(StratagemSurfaceKind kind,Vector2 point,float radius,float duration,Transform parent=null)
        {
            var obj=new GameObject("StratagemSurface");
            if(parent!=null) obj.transform.SetParent(parent,false);
            else obj.transform.position=new Vector3(point.x,point.y,StratagemAssets.Z(point.y+radius)+.08f);
            var surface=obj.AddComponent<StratagemSurface>();
            surface.radius=radius;surface.started=Time.time;surface.duration=duration;surface.token=Plugin.Combat?.Token;
            surface.field=kind==StratagemSurfaceKind.StaticField||kind==StratagemSurfaceKind.Gas;
            surface.renderer=obj.AddComponent<MeshRenderer>();
            if(material==null) material=new Material(Shader.Find("Sprites/Default"));
            surface.renderer.sharedMaterial=material;
            surface.mesh=Build(kind,radius);
            obj.AddComponent<MeshFilter>().sharedMesh=surface.mesh;
            if(surface.field)
            {
                foreach(var other in fields) if(surface.Overlaps(other)){surface.overlaps++;other.overlaps++;}
                fields.Add(surface);
            }
            return surface;
        }
        private bool Overlaps(StratagemSurface other) => ((Vector2)transform.position-(Vector2)other.transform.position).sqrMagnitude<(radius+other.radius)*(radius+other.radius);
        internal void Visible(bool visible){renderer.enabled=visible;}
        internal void Strength(float strength)
        {
            properties.SetColor("_Color",new Color(1,1,1,strength/(1+.5f*overlaps)));
            renderer.SetPropertyBlock(properties);
        }
        private void Update()
        {
            if(!Plugin.InCombat || token!=Plugin.Combat?.Token || duration>0 && Time.time-started>=duration){Destroy(gameObject);return;}
            if(duration<=0)return;
            float elapsed=Time.time-started;
            float fade=Mathf.Min(Mathf.Clamp01(elapsed/.12f),Mathf.Clamp01((duration-elapsed)/.25f));
            Strength(fade);
        }
        private void OnDestroy()
        {
            if(field && fields.Remove(this)) foreach(var other in fields) if(Overlaps(other))other.overlaps--;
            if(mesh!=null)Destroy(mesh);
        }
        internal static void Clear()
        { foreach(var field in fields.ToArray()) if(field!=null)Destroy(field.gameObject);fields.Clear(); }
        private static Mesh Build(StratagemSurfaceKind kind,float radius)
        {
            var v=new List<Vector3>();var c=new List<Color>();var triangles=new List<int>();
            Color color=kind==StratagemSurfaceKind.Gas?new Color(.4f,.7f,.22f):kind==StratagemSurfaceKind.Fire?new Color(1,.45f,.1f):
                kind==StratagemSurfaceKind.StaticField?new Color(.28f,.72f,.95f):new Color(1,.76f,.25f);
            if(kind==StratagemSurfaceKind.Warning)color=new Color(.95f,.2f,.14f);
            bool dome=kind==StratagemSurfaceKind.Shield;
            bool fill=kind==StratagemSurfaceKind.StaticField||kind==StratagemSurfaceKind.Gas||kind==StratagemSurfaceKind.Fire||dome;
            Vector2 Point(float angle,float r)
            {
                float x=Mathf.Cos(angle)*r,y=Mathf.Sin(angle)*r;
                if(dome)y*=y<0?.42f:1.05f;
                return new Vector2(Mathf.Round(x*32)/32,Mathf.Round(y*32)/32);
            }
            void Tri(Vector2 a,Vector2 b,Vector2 d,float alpha)
            {
                int start=v.Count;v.Add(a);v.Add(b);v.Add(d);
                color.a=alpha;c.Add(color);c.Add(color);c.Add(color);
                triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
            }
            void Line(Vector2 a,Vector2 b,float width,float alpha)
            {
                Vector2 delta=b-a;if(delta.sqrMagnitude<.00001f)return;
                Vector2 n=new Vector2(-delta.y,delta.x).normalized*width*.5f;
                Tri(a-n,a+n,b+n,alpha);Tri(a-n,b+n,b-n,alpha);
            }
            for(int n=0;n<64;n++)
            {
                float a=n*Mathf.PI*2/64,b=(n+1)*Mathf.PI*2/64;
                Vector2 pa=Point(a,radius),pb=Point(b,radius);
                if(fill)Tri(Vector2.zero,pa,pb,dome?.1f:.12f);
                Line(pa,pb,.035f,.4f);
            }
            if(dome)
            {
                float hex=.28f;
                for(int row=-8;row<=12;row++)for(int column=-12;column<=12;column++)
                {
                    Vector2 center=new Vector2((column+(row%2==0?0:.5f))*hex*1.73f,row*hex*1.5f);
                    float sy=center.y<0?center.y/.42f:center.y/1.05f;
                    if(center.x*center.x+sy*sy>(radius-hex)*(radius-hex))continue;
                    for(int edge=0;edge<3;edge++)
                    {
                        float a=(edge*60+30)*Mathf.Deg2Rad,b=((edge+1)*60+30)*Mathf.Deg2Rad;
                        Line(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*hex,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*hex,.018f,.18f);
                    }
                }
            }
            else if(fill)
            {
                for(int n=0;n<18;n++)
                {
                    float a=n*2.399963f,r=radius*Mathf.Sqrt((n+.5f)/18)*.86f;
                    Vector2 point=Point(a,r);
                    float length=kind==StratagemSurfaceKind.Gas?.055f:.09f;
                    Line(point,point+new Vector2(length,.03f),.025f,.09f);
                    if(kind==StratagemSurfaceKind.StaticField && n%3==0)Line(point+new Vector2(length,.03f),point+new Vector2(length*.75f,.09f),.022f,.2f);
                }
            }
            var mesh=new Mesh{vertices=v.ToArray(),colors=c.ToArray(),triangles=triangles.ToArray()};
            var uv=new Vector2[v.Count];for(int i=0;i<uv.Length;i++)uv[i]=Vector2.zero;mesh.uv=uv;
            mesh.RecalculateBounds();return mesh;
        }
    }
}
