using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class StratagemLaserVisual
    {
        private readonly SpriteRenderer[] parts=new SpriteRenderer[2];
        private readonly float radius;
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
            string phase=time<.32f?"start":remaining<.32f?"end":"loop";
            int frame=phase=="start"?Mathf.Min(7,Mathf.FloorToInt(time/.04f)):
                phase=="end"?Mathf.Min(7,Mathf.FloorToInt((.32f-remaining)/.04f)):Mathf.FloorToInt((time-.32f)*16)%8;
            var column=parts[0];column.sprite=StratagemAssets.Named("fx.orbital."+phase+".beam."+frame);
            column.transform.position=new Vector3(point.x,point.y+.4f,StratagemAssets.Z(point.y)-.04f);
            column.transform.localScale=new Vector3(radius*3.2f/column.sprite.bounds.size.x,8.2f/column.sprite.bounds.size.y,1);
            column.color=Color.white;
            var contact=parts[1];contact.sprite=StratagemAssets.Named("fx.orbital."+phase+".ground."+frame);
            contact.transform.position=new Vector3(point.x,point.y,StratagemAssets.Z(point.y)+.4f);
            contact.transform.localScale=new Vector3(radius*3.2f/contact.sprite.bounds.size.x,1.2f/contact.sprite.bounds.size.y,1);
            contact.color=new Color(1,1,1,.95f);
        }
        internal void Dispose()
        {foreach(var image in parts)if(image!=null)Object.Destroy(image.gameObject);}
    }

}
