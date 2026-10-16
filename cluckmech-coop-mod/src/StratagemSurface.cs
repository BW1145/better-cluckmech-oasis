using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    internal enum StratagemSurfaceKind { StaticField,Gas,Fire,Warning }
    public sealed class StratagemSurface : MonoBehaviour
    {
        private static readonly List<StratagemSurface> fields=new List<StratagemSurface>();
        private SpriteRenderer image;
        private float started,duration,radius,opacity;
        private string token,prefix;
        private int overlaps,frames;
        private bool field,electric;
        internal static StratagemSurface Create(StratagemSurfaceKind kind,Vector2 point,float radius,float duration,Transform parent=null)
        {
            var obj=new GameObject("StratagemSurface");
            obj.transform.position=new Vector3(point.x,point.y,StratagemAssets.Z(point.y)+.4f);
            if(parent!=null)obj.transform.SetParent(parent,true);
            var surface=obj.AddComponent<StratagemSurface>();
            surface.radius=radius;surface.started=Time.time;surface.duration=duration;surface.token=Plugin.Combat?.Token;
            surface.electric=kind==StratagemSurfaceKind.StaticField;
            surface.field=surface.electric||kind==StratagemSurfaceKind.Gas;
            surface.prefix=surface.electric?"fx.ems.loop.":kind==StratagemSurfaceKind.Gas?"fx.gas-field.":
                kind==StratagemSurfaceKind.Fire?"fx.crater.0":"fx.warning.";
            surface.frames=kind==StratagemSurfaceKind.Fire?1:8;
            surface.opacity=surface.electric?1:kind==StratagemSurfaceKind.Gas?.5f:kind==StratagemSurfaceKind.Fire?.65f:.8f;
            // The painted electric perimeter spans 66% of the registered frame.
            float width=radius*2/(surface.electric?.66f:1);
            surface.image=StratagemAssets.Renderer(obj.transform,StratagemAssets.Named(surface.prefix+(surface.frames>1?"0":"")),width);
            if(kind==StratagemSurfaceKind.Gas)surface.image.transform.localScale=new Vector3(width/surface.image.sprite.bounds.size.x,width/surface.image.sprite.bounds.size.y,1);
            if(surface.field)
            {
                foreach(var other in fields)if(surface.Overlaps(other)){surface.overlaps++;other.overlaps++;}
                fields.Add(surface);
            }
            if(kind==StratagemSurfaceKind.Fire)StratagemNativeParticles.Play(StratagemParticleKind.Fire,point,radius*.6f,duration,true,obj.transform);
            if(surface.electric)StratagemNativeParticles.Play(StratagemParticleKind.Electric,point,radius*.4f,duration,true,obj.transform);
            surface.Tick(0);
            return surface;
        }
        private bool Overlaps(StratagemSurface other)=>((Vector2)transform.position-(Vector2)other.transform.position).sqrMagnitude<(radius+other.radius)*(radius+other.radius);
        private void Tick(float elapsed)
        {
            float remaining=duration-elapsed;
            if(electric)
            {
                string phase=elapsed<.32f?"start":remaining<.32f?"end":"loop";
                int frame=phase=="start"?Mathf.Min(7,Mathf.FloorToInt(elapsed/.04f)):
                    phase=="end"?Mathf.Min(7,Mathf.FloorToInt((.32f-remaining)/.04f)):Mathf.FloorToInt(elapsed*12)%8;
                image.sprite=StratagemAssets.Named("fx.ems."+phase+"."+frame);
            }
            else if(frames>1)image.sprite=StratagemAssets.Named(prefix+Mathf.FloorToInt(elapsed*12)%frames);
            float fade=electric?1:Mathf.Min(Mathf.Clamp01(elapsed/.08f),Mathf.Clamp01(remaining/.3f));
            image.color=new Color(1,1,1,opacity*fade/(1+.25f*overlaps));
        }
        private void Update()
        {
            if(!Plugin.InCombat||token!=Plugin.Combat?.Token||Time.time-started>=duration){Destroy(gameObject);return;}
            Tick(Time.time-started);
        }
        private void OnDestroy()
        {if(field&&fields.Remove(this))foreach(var other in fields)if(Overlaps(other))other.overlaps--;}
        internal static void Clear()
        {foreach(var field in fields.ToArray())if(field!=null)Destroy(field.gameObject);fields.Clear();}
    }
}
