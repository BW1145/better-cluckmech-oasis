using UnityEngine;

namespace CluckmechCoop
{
    public sealed class StratagemDroneBeam : MonoBehaviour
    {
        private StratagemUnit source;
        private 模块_基本属性 target;
        private SpriteRenderer ray,contact;
        private Vector2 destination;
        private float until,length;
        internal void Set(StratagemUnit unit,Vector2 point,float seconds,uint id,float fixedLength)
        {
            source=unit;destination=point;until=Time.time+seconds;length=fixedLength;
            target=id==0?StratagemWorld.Target(point,.5f,false):StratagemWorld.Enemy(id);
            if(ray==null)
            {
                ray=StratagemAssets.Renderer(transform,StratagemAssets.Named("fx.laser-beam.0"),1);
                contact=StratagemAssets.Renderer(transform,StratagemAssets.Named("fx.laser-sparks.0"),1);
                var model=unit.GetComponent<StratagemRig>().Body;
                ray.sortingLayerID=contact.sortingLayerID=model.sortingLayerID;
                ray.sortingOrder=contact.sortingOrder=model.sortingOrder;
            }
            ray.enabled=contact.enabled=true;
        }
        internal void Stop()
        {until=0;if(ray!=null){ray.enabled=false;contact.enabled=false;}}
        private void LateUpdate()
        {
            if(source==null||!source.Alive||Time.time>=until){Stop();return;}
            if(target!=null&&target.当前生命值>0)destination=target.transform.position;
            Vector2 start=source.Muzzle,delta=destination-start;
            Vector2 end=length>0?start+delta.normalized*length:destination;
            int frame=Mathf.FloorToInt(Time.time*16)%8;
            ray.sprite=StratagemAssets.Named("fx.laser-beam."+frame);
            ray.transform.position=new Vector3((start.x+end.x)*.5f,(start.y+end.y)*.5f,StratagemAssets.Z(start.y)-.03f);
            ray.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-90);
            ray.transform.localScale=new Vector3(StratagemBeamGeometry.Width/ray.sprite.bounds.size.x,(end-start).magnitude/ray.sprite.bounds.size.y,1);
            ray.color=Color.white;
            contact.sprite=StratagemAssets.Named("fx.laser-sparks."+frame);
            contact.transform.position=new Vector3(destination.x,destination.y,StratagemAssets.Z(destination.y)-.03f);
            contact.transform.localScale=Vector3.one*(.3f/contact.sprite.bounds.size.x);
            contact.color=new Color(1,1,1,.9f);
        }
    }
}
