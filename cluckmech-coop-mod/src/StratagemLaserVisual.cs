using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class StratagemLaserVisual
    {
        private readonly SpriteRenderer[] parts=new SpriteRenderer[2];
        private readonly float radius;
        private Vector2 lastPoint;
        private readonly Sprite[][] beams=new Sprite[3][],grounds=new Sprite[3][];
        private int previous=-1;
        internal StratagemLaserVisual(float radius,SpriteRenderer source)
        {
            this.radius=radius;
            string[] phases={"start","loop","end"};
            for(int p=0;p<3;p++){beams[p]=StratagemAssets.Sequence("fx.orbital."+phases[p]+".beam.",8);grounds[p]=StratagemAssets.Sequence("fx.orbital."+phases[p]+".ground.",8);}
            for(int i=0;i<parts.Length;i++)
            {
                string name="fx.orbital.start."+(i==0?"beam.0":"ground.0");
                var image=StratagemAssets.Renderer(null,StratagemAssets.Named(name),1);
                image.sortingLayerID=source.sortingLayerID;image.sortingOrder=source.sortingOrder;
                parts[i]=image;
            }
        }
        internal void Tick(Vector2 point,float time,float remaining)
        {
            lastPoint=point;
            int phase=time<.32f?0:remaining<.32f?2:1;
            int frame=phase==0?Mathf.Min(7,Mathf.FloorToInt(time/.04f)):
                phase==2?Mathf.Min(7,Mathf.FloorToInt((.32f-remaining)/.04f)):Mathf.FloorToInt((time-.32f)*16)%8;
            var column=parts[0];
            if(previous!=phase*8+frame){previous=phase*8+frame;column.sprite=beams[phase][frame];parts[1].sprite=grounds[phase][frame];}
            column.transform.position=new Vector3(point.x,point.y+radius*.6f,StratagemAssets.Z(point.y)-.04f);
            column.transform.localScale=new Vector3(radius*2.4f/column.sprite.bounds.size.x,8.2f/column.sprite.bounds.size.y,1);
            column.color=Color.white;
            var contact=parts[1];
            contact.transform.position=new Vector3(point.x,point.y,StratagemAssets.Z(point.y)+.4f);
            contact.transform.localScale=new Vector3(radius*2.4f/contact.sprite.bounds.size.x,radius*1.8f/contact.sprite.bounds.size.y,1);
            contact.color=new Color(1,1,1,.95f);
        }
        internal void Dispose(bool finish=false)
        {
            if(finish)
            {
                var column=StratagemAssets.Animated("fx.orbital.end.beam.",8,lastPoint+Vector2.up*(radius*.6f),radius*2.4f,.32f,hold:true);
                column.transform.localScale=new Vector3(radius*2.4f/column.sprite.bounds.size.x,8.2f/column.sprite.bounds.size.y,1);
                var contact=StratagemAssets.Animated("fx.orbital.end.ground.",8,lastPoint,radius*2.4f,.32f,ground:true,hold:true);
                contact.transform.localScale=new Vector3(radius*2.4f/contact.sprite.bounds.size.x,radius*1.8f/contact.sprite.bounds.size.y,1);
            }
            foreach(var image in parts)if(image!=null)Object.Destroy(image.gameObject);
        }
    }

}
