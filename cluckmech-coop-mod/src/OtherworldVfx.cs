using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class OtherworldVfx:MonoBehaviour
    {
        private static Sprite[] strikeFrames,impactFrames,fireFrames;
        private static Material fireMaterial;
        private SpriteRenderer ground,body;
        private Mesh mesh;
        private Vector2[] uv;
        private Vector2[] normalizedUv;
        private float elapsed,radius;
        private bool orbital,fire,landed;
        private int frame=-1;
        private Vector3 bodyOrigin;
        private OtherworldActor actor;
        private static Sprite[] Sequence(string prefix,int count)
        {var frames=new Sprite[count];for(int i=0;i<count;i++)frames[i]=OtherworldAssets.Named(prefix+i);return frames;}
        internal static void Clear()
        {strikeFrames=impactFrames=fireFrames=null;if(fireMaterial!=null)Destroy(fireMaterial);fireMaterial=null;}
        private SpriteRenderer Layer(string name,int order)
        {
            var obj=new GameObject(name);obj.transform.SetParent(transform,false);var renderer=obj.AddComponent<SpriteRenderer>();
            renderer.sortingLayerID=actor.Body.sortingLayerID;renderer.sortingOrder=actor.Body.sortingOrder+order;return renderer;
        }
        internal static void Strike(OtherworldActor actor,Vector2 point,float radius,bool orbital)
        {
            if(strikeFrames==null)strikeFrames=Sequence("electric.strike.",8);
            if(impactFrames==null)impactFrames=Sequence("electric.impact.",8);
            var obj=new GameObject("OtherworldElectricalImpact");obj.transform.position=point;
            var effect=obj.AddComponent<OtherworldVfx>();effect.actor=actor;effect.radius=radius;effect.orbital=orbital;
            effect.ground=effect.Layer("GroundDischarge",-2);effect.ground.sprite=impactFrames[0];
            // Peak authored perimeter is scaled to the exact damage diameter, independently of bolt height.
            float diameter=OtherworldAssets.VisibleBounds(impactFrames[2]).size.x;
            effect.ground.transform.localScale=Vector3.one*(2*radius/diameter);
            effect.body=effect.Layer(orbital?"OrbitalBlock":"Lightning",2);
            effect.body.sprite=orbital?OtherworldAssets.Named("orbital.block"):strikeFrames[0];
            var bounds=OtherworldAssets.VisibleBounds(effect.body.sprite);
            float scale=orbital?radius*.85f/bounds.size.x:2.8f/OtherworldAssets.VisibleBounds(strikeFrames[3]).size.y;
            effect.body.transform.localScale=Vector3.one*scale;
            effect.bodyOrigin=Vector3.up*(-bounds.min.y*scale);
            effect.body.transform.localPosition=effect.bodyOrigin+(orbital?Vector3.up*6:Vector3.zero);
            effect.ground.enabled=!orbital;
        }
        internal static void Fire(OtherworldActor actor,Vector2 point,Vector2 direction,float radius)
        {
            if(fireFrames==null)fireFrames=Sequence("spiral.fire.",16);
            var obj=new GameObject("OtherworldSpiralFire");obj.transform.position=point;
            obj.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            var effect=obj.AddComponent<OtherworldVfx>();effect.actor=actor;effect.fire=true;effect.radius=radius;
            const int count=32;var vertices=new Vector3[count+2];var triangles=new int[count*3];var colors=new Color[count+2];
            effect.uv=new Vector2[vertices.Length];effect.normalizedUv=new Vector2[vertices.Length];
            vertices[0]=Vector3.up*.4f;effect.normalizedUv[0]=new Vector2(.05f,.5f);colors[0]=Color.white;
            float halfAngle=Mathf.Acos(.35f);
            for(int i=0;i<=count;i++)
            {
                float angle=Mathf.Lerp(-halfAngle,halfAngle,(float)i/count);float x=Mathf.Cos(angle),y=Mathf.Sin(angle);
                vertices[i+1]=new Vector3(x*radius,y*radius+.4f,0);
                effect.normalizedUv[i+1]=new Vector2(.05f+x*.9f,.5f+y*.45f);colors[i+1]=Color.white;
                if(i<count){triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=i+2;}
            }
            effect.mesh=new Mesh{name="SpiralFireSector"};effect.mesh.vertices=vertices;effect.mesh.triangles=triangles;effect.mesh.colors=colors;
            obj.AddComponent<MeshFilter>().sharedMesh=effect.mesh;var renderer=obj.AddComponent<MeshRenderer>();
            if(fireMaterial==null){fireMaterial=new Material(actor.Body.sharedMaterial);fireMaterial.mainTexture=fireFrames[0].texture;}
            renderer.sharedMaterial=fireMaterial;renderer.sortingLayerID=actor.Body.sortingLayerID;renderer.sortingOrder=actor.Body.sortingOrder+2;
            effect.FireFrame(0);
        }
        private void FireFrame(int current)
        {
            if(current==frame)return;frame=current;var sprite=fireFrames[current];var rect=sprite.textureRect;
            for(int i=0;i<uv.Length;i++)uv[i]=new Vector2((rect.x+normalizedUv[i].x*rect.width)/sprite.texture.width,(rect.y+normalizedUv[i].y*rect.height)/sprite.texture.height);
            mesh.uv=uv;
        }
        private void ImpactParticles()
        {
            var shake=Object.FindObjectOfType<摄像机_震动>();if(shake!=null)shake.相机震动(.12f,.18f);
            var world=GameObject.Find(函数库.常数类.命名_世界);if(world==null)return;
            var references=world.GetComponent<效果物体引用器>();if(references==null)return;
            foreach(var prefab in new[]{references.特效.烟雾效果,references.特效.普通攻击效果})
            {
                if(prefab==null)continue;var dust=Instantiate(prefab,transform.position,Quaternion.identity);
                dust.transform.localScale=Vector3.one*Mathf.Min(2,radius*.4f);
                foreach(var sprite in dust.GetComponentsInChildren<SpriteRenderer>(true))sprite.enabled=false;
                foreach(var particles in dust.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main=particles.main;main.loop=false;main.maxParticles=24;main.stopAction=ParticleSystemStopAction.None;main.useUnscaledTime=false;
                    var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sortingLayerID=actor.Body.sortingLayerID;renderer.sortingOrder=actor.Body.sortingOrder;
                    particles.Play(false);
                }
                Destroy(dust,2);
            }
        }
        private void Update()
        {
            elapsed+=Time.deltaTime;
            if(fire){if(elapsed>=.64f){Destroy(gameObject);return;}FireFrame(Mathf.Min(15,(int)(elapsed/.64f*16)));return;}
            float impact=orbital?.14f:0;
            if(orbital&&elapsed<impact)
                body.transform.localPosition=bodyOrigin+Vector3.up*(6*(1-(elapsed/impact)*(elapsed/impact)));
            else
            {
                if(!landed)
                {
                    landed=true;body.transform.localPosition=bodyOrigin;ground.enabled=true;
                    if(orbital)ImpactParticles();
                }
                float time=elapsed-impact;int current=Mathf.Min(7,(int)(time/.5f*8));
                ground.sprite=impactFrames[current];ground.color=new Color(1,1,1,Mathf.Clamp01((.65f-time)/.2f));
                if(!orbital)body.sprite=strikeFrames[current];
                if(orbital&&time>1)body.color=new Color(1,1,1,Mathf.Clamp01((1.4f-time)/.4f));
            }
            if(elapsed>impact+(orbital?1.4f:.65f))Destroy(gameObject);
        }
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
