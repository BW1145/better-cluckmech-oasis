using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class StratagemLaserVisual
    {
        private readonly SpriteRenderer[] parts=new SpriteRenderer[2];
        private readonly float radius;
        private Vector2 lastPoint;
        internal StratagemLaserVisual(float radius,SpriteRenderer source)
        {
            this.radius=radius;
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
            string phase=time<.32f?"start":remaining<.32f?"end":"loop";
            int frame=phase=="start"?Mathf.Min(7,Mathf.FloorToInt(time/.04f)):
                phase=="end"?Mathf.Min(7,Mathf.FloorToInt((.32f-remaining)/.04f)):Mathf.FloorToInt((time-.32f)*16)%8;
            var column=parts[0];column.sprite=StratagemAssets.Named("fx.orbital."+phase+".beam."+frame);
            column.transform.position=new Vector3(point.x,point.y+radius*.6f,StratagemAssets.Z(point.y)-.04f);
            column.transform.localScale=new Vector3(radius*2.4f/column.sprite.bounds.size.x,8.2f/column.sprite.bounds.size.y,1);
            column.color=Color.white;
            var contact=parts[1];contact.sprite=StratagemAssets.Named("fx.orbital."+phase+".ground."+frame);
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
