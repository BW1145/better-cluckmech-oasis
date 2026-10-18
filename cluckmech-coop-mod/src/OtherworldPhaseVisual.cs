using UnityEngine;

namespace CluckmechCoop
{
    [DefaultExecutionOrder(200)]
    internal sealed class OtherworldPhaseVisual:MonoBehaviour
    {
        private const int Bands=8;
        private OtherworldActor actor;
        private Mesh mesh;
        private MeshRenderer drawing;
        private SpriteRenderer opening;
        private Vector3[] rest,vertices;
        private Vector2[] uv;
        private Color[] colors;
        private readonly Sprite[] frames=new Sprite[16];
        private bool playing,wasPhased,leased;
        private float started,finish;
        private int previous=-1;
        internal void Initialize(OtherworldActor owner)
        {
            actor=owner;
            var root=new GameObject("KamuiPhase");root.transform.SetParent(transform,false);
            root.transform.localPosition=actor.VisualOrigin;root.transform.localScale=Vector3.one*actor.VisualScale;
            drawing=root.AddComponent<MeshRenderer>();drawing.sharedMaterial=actor.Body.sharedMaterial;
            drawing.sortingLayerID=actor.Body.sortingLayerID;drawing.sortingOrder=actor.Body.sortingOrder;drawing.enabled=false;
            var block=new MaterialPropertyBlock();block.SetTexture("_MainTex",actor.CurrentFrame.texture);drawing.SetPropertyBlock(block);
            mesh=new Mesh{name="KamuiPhaseBody"};mesh.MarkDynamic();root.AddComponent<MeshFilter>().sharedMesh=mesh;
            rest=new Vector3[Bands*4];vertices=new Vector3[rest.Length];uv=new Vector2[rest.Length];colors=new Color[rest.Length];var triangles=new int[Bands*6];
            var bounds=actor.CurrentFrame.bounds;
            for(int band=0;band<Bands;band++)
            {
                int offset=band*4;float low=Mathf.Lerp(bounds.min.y,bounds.max.y,band/(float)Bands),high=Mathf.Lerp(bounds.min.y,bounds.max.y,(band+1)/(float)Bands);
                rest[offset]=new Vector3(bounds.min.x,low);rest[offset+1]=new Vector3(bounds.max.x,low);rest[offset+2]=new Vector3(bounds.min.x,high);rest[offset+3]=new Vector3(bounds.max.x,high);
                int index=band*6;triangles[index]=offset;triangles[index+1]=offset+2;triangles[index+2]=offset+1;triangles[index+3]=offset+1;triangles[index+4]=offset+2;triangles[index+5]=offset+3;
            }
            mesh.vertices=rest;mesh.triangles=triangles;
            var rear=new GameObject("KamuiPhaseOpening");rear.transform.SetParent(root.transform,false);
            var visible=OtherworldAssets.VisibleBounds(actor.CurrentFrame);rear.transform.localPosition=visible.center;rear.transform.localScale=Vector3.one*(visible.size.y*.75f);
            opening=rear.AddComponent<SpriteRenderer>();opening.sortingLayerID=actor.Body.sortingLayerID;opening.sortingOrder=actor.Body.sortingOrder-1;opening.enabled=false;
            for(int i=0;i<frames.Length;i++)frames[i]=OtherworldAssets.Named("kamui."+(i/8)+"."+(i%8));
        }
        private void LateUpdate()
        {
            if(actor==null||drawing==null)return;
            bool phased=actor.Phased&&!actor.Dashing&&!actor.Death.是否玩家死亡;
            if(actor.Dashing){Stop();wasPhased=false;return;}
            if(phased&&!wasPhased)
            {
                started=Time.time;playing=true;finish=0;previous=-1;
                if(!leased){OtherworldWarpVisual.Hide(actor.Body);leased=true;}
            }
            if(!phased&&wasPhased)finish=Time.time+.18f;
            wasPhased=phased;if(!playing)return;
            if(!phased&&Time.time>=finish){Stop();return;}
            drawing.enabled=true;drawing.sortingOrder=actor.Body.sortingOrder;
            float age=Time.time-started,weight=phased?Mathf.Clamp01(age/.12f):Mathf.Clamp01((finish-Time.time)/.18f);
            var visible=OtherworldAssets.VisibleBounds(OtherworldAssets.Hero(actor.Hero,0,actor.FacingDirection));
            float width=visible.size.x;
            for(int band=0;band<Bands;band++)
            {
                var sprite=actor.CurrentFrame;var rect=sprite.textureRect;var texture=sprite.texture;
                float height=Mathf.InverseLerp(visible.min.y,visible.max.y,(rest[band*4].y+rest[band*4+2].y)*.5f);
                float bodyWeight=weight*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.05f,.25f,height))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,.95f,height)));
                float phase=age*10-band*1.25f,shift=Mathf.Sin(phase)*width*.25f*bodyWeight,pinch=Mathf.Lerp(1,.78f+.22f*Mathf.Cos(phase),bodyWeight);
                var tint=actor.Body.color;tint.a*=Mathf.Lerp(1,.7f,bodyWeight);
                for(int corner=0;corner<4;corner++)
                {
                    int index=band*4+corner;var point=rest[index];point.x=visible.center.x+(point.x-visible.center.x)*pinch+shift;
                    point.y+=Mathf.Cos(phase)*width*.035f*bodyWeight;vertices[index]=point;colors[index]=tint;
                    uv[index]=new Vector2((rect.x+(corner%2)*rect.width)/texture.width,(rect.y+(band+(corner<2?0:1))*rect.height/Bands)/texture.height);
                }
            }
            mesh.vertices=vertices;mesh.colors=colors;mesh.uv=uv;mesh.RecalculateBounds();
            float portalTime=phased?age/.4f:1-(finish-Time.time)/.18f;
            opening.enabled=!phased||age<.4f;
            int frame=Mathf.Clamp((int)(portalTime*16),0,15);if(!phased)frame=15-frame;
            if(frame!=previous){opening.sprite=frames[frame];previous=frame;}
            opening.color=new Color(1,1,1,.8f*weight);
        }
        private void Stop()
        {
            if(drawing!=null)drawing.enabled=false;if(opening!=null)opening.enabled=false;
            if(leased){OtherworldWarpVisual.Reveal(actor.Body);leased=false;}
            playing=false;
        }
        private void OnDisable(){Stop();wasPhased=false;}
        private void OnDestroy(){Stop();if(mesh!=null)Destroy(mesh);}
    }
}
