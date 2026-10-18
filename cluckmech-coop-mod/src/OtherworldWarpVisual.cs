using UnityEngine;
using System.Collections.Generic;

namespace CluckmechCoop
{
    // A single mesh samples the live character sprite while its horizontal bands coil inward.
    internal sealed class OtherworldWarpVisual : MonoBehaviour
    {
        private const int Bands=16;
        private sealed class HideLease {internal int Count;internal bool Original;}
        private static readonly Dictionary<SpriteRenderer,HideLease> hidden=new Dictionary<SpriteRenderer,HideLease>();
        private SpriteRenderer source,portal;
        private Mesh mesh;
        private Vector3[] rest,vertices;
        private Color[] colors;
        private Vector3 focus;
        private float elapsed,duration,width;
        private readonly Sprite[] frames=new Sprite[16];
        private int lastFrame=-1;
        private bool returning,hideSource;
        internal static void Hide(SpriteRenderer source)
        {
            if(!hidden.TryGetValue(source,out var lease))hidden[source]=lease=new HideLease{Original=source.forceRenderingOff};
            lease.Count++;source.forceRenderingOff=true;
        }
        internal static void Reveal(SpriteRenderer source)
        {
            if(ReferenceEquals(source,null)||!hidden.TryGetValue(source,out var lease))return;
            if(--lease.Count==0){hidden.Remove(source);if(source!=null)source.forceRenderingOff=lease.Original;}
        }

        internal static void Play(SpriteRenderer source,bool returning,float duration=.55f,bool hideSource=true)
        {
            if(source==null || source.sprite==null)return;
            var root=new GameObject("KamuiTransition");
            root.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);root.transform.localScale=source.transform.lossyScale;
            root.AddComponent<OtherworldWarpVisual>().Initialize(source,returning,duration,hideSource);
        }

        private void Initialize(SpriteRenderer original,bool reverse,float seconds,bool hide)
        {
            source=original;returning=reverse;duration=seconds;hideSource=hide;
            if(hideSource)
            {
                Hide(source);
            }
            mesh=new Mesh{name="KamuiBands"};mesh.MarkDynamic();
            var renderer=gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=source.sharedMaterial;
            renderer.sortingLayerID=source.sortingLayerID;renderer.sortingOrder=source.sortingOrder;
            var properties=new MaterialPropertyBlock();source.GetPropertyBlock(properties);
            properties.SetTexture("_MainTex",source.sprite.texture);renderer.SetPropertyBlock(properties);
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var bounds=OtherworldAssets.VisibleBounds(source.sprite);
            width=bounds.size.x;
            var rect=source.sprite.textureRect;
            rect.x+=(bounds.min.x-source.sprite.bounds.min.x)*source.sprite.pixelsPerUnit;
            rect.y+=(bounds.min.y-source.sprite.bounds.min.y)*source.sprite.pixelsPerUnit;
            rect.width=bounds.size.x*source.sprite.pixelsPerUnit;rect.height=bounds.size.y*source.sprite.pixelsPerUnit;
            var texture=source.sprite.texture;
            focus=new Vector3(bounds.center.x,bounds.min.y+bounds.size.y*.8f,0);
            rest=new Vector3[Bands*4];vertices=new Vector3[rest.Length];colors=new Color[rest.Length];
            var uv=new Vector2[rest.Length];var triangles=new int[Bands*6];
            for(var i=0;i<Bands;i++)
            {
                float bottom=i/(float)Bands,top=(i+1)/(float)Bands;
                var offset=i*4;
                rest[offset]=new Vector3(bounds.min.x,Mathf.Lerp(bounds.min.y,bounds.max.y,bottom));
                rest[offset+1]=new Vector3(bounds.max.x,rest[offset].y);
                rest[offset+2]=new Vector3(bounds.min.x,Mathf.Lerp(bounds.min.y,bounds.max.y,top));
                rest[offset+3]=new Vector3(bounds.max.x,rest[offset+2].y);
                for(var corner=0;corner<4;corner++)
                {
                    float x=(corner%2==0)?0:1,y=corner<2?bottom:top;
                    if(source.flipX)x=1-x;if(source.flipY)y=1-y;
                    uv[offset+corner]=new Vector2((rect.x+rect.width*x)/texture.width,(rect.y+rect.height*y)/texture.height);
                }
                int index=i*6;
                triangles[index]=offset;triangles[index+1]=offset+2;triangles[index+2]=offset+1;
                triangles[index+3]=offset+1;triangles[index+4]=offset+2;triangles[index+5]=offset+3;
            }
            mesh.vertices=rest;mesh.uv=uv;mesh.triangles=triangles;
            var rear=new GameObject("KamuiOpening");rear.transform.SetParent(transform,false);
            rear.transform.localPosition=bounds.center;
            portal=rear.AddComponent<SpriteRenderer>();
            portal.sortingLayerID=source.sortingLayerID;portal.sortingOrder=source.sortingOrder-1;
            rear.transform.localScale=Vector3.one*(bounds.size.y*1.3f/2);
            for(var i=0;i<frames.Length;i++)frames[i]=OtherworldAssets.Named("kamui."+(i/8)+"."+(i%8));
            Render(0);
        }

        private void Update()
        {
            elapsed+=Time.deltaTime;
            if(source==null || elapsed>=duration){Destroy(gameObject);return;}
            Render(elapsed/duration);
        }

        private void Render(float time)
        {
            float t=returning?1-time:time;
            float contraction=t*t*(3-2*t),wave=Mathf.Sin(t*Mathf.PI);
            var tint=source.color;tint.a=1-Mathf.InverseLerp(.8f,1,t);
            for(var band=0;band<Bands;band++)
            {
                float angle=t*Mathf.PI*4+band*.48f;
                float shift=Mathf.Sin(angle)*width*.45f*wave;
                float pinch=Mathf.Lerp(1,Mathf.Cos(angle),wave*.75f);
                for(var corner=0;corner<4;corner++)
                {
                    int index=band*4+corner;
                    var position=rest[index];
                    position.x=focus.x+(position.x-focus.x)*pinch+shift;
                    position.y+=Mathf.Cos(angle)*width*.08f*wave;
                    vertices[index]=Vector3.LerpUnclamped(position,focus,contraction);
                    colors[index]=tint;
                }
            }
            mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();
            int frame=Mathf.Min(15,(int)(time*16));
            if(returning)frame=15-frame;
            if(frame!=lastFrame){portal.sprite=frames[frame];lastFrame=frame;}
        }

        private void OnDestroy()
        {
            if(hideSource)Reveal(source);
            if(mesh!=null)Destroy(mesh);
        }
    }
}
