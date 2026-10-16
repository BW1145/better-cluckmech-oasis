using UnityEngine;

namespace CluckmechCoop
{
    public sealed class StratagemTransit:MonoBehaviour
    {
        private Vector2 start,end;
        private float duration,started,height;
        private bool rotate;
        internal void Set(Vector2 from,Vector2 to,float seconds,float arc=0,bool followTangent=false)
        {start=from;end=to;duration=seconds;height=arc;rotate=followTangent;started=Time.time;transform.position=new Vector3(from.x,from.y,StratagemAssets.Z(from.y)-.03f);}
        private void Update()
        {
            float t=Mathf.Clamp01((Time.time-started)/duration);
            Vector2 position=Vector2.Lerp(start,end,t)+Vector2.up*(4*t*(1-t)*height);
            transform.position=new Vector3(position.x,position.y,StratagemAssets.Z(position.y)-.03f);
            if(rotate)
            {Vector2 tangent=end-start+Vector2.up*(4*height*(1-2*t));transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg-90);}
        }
    }
    public sealed class StratagemBombDrop:MonoBehaviour
    {
        private Vector2 point;
        private float started;
        private SpriteRenderer image;
        internal void Set(Vector2 target,float seconds)
        {point=target;started=Time.time;image=GetComponent<SpriteRenderer>();}
        private void Update()
        {
            float elapsed=Time.time-started;
            if(elapsed<.6f)
            {
                float t=elapsed/.6f;
                Vector2 p=point+Vector2.up*(5*(1-t*t)+.3f);
                transform.position=new Vector3(p.x,p.y,StratagemAssets.Z(p.y)-.03f);
                transform.rotation=Quaternion.Euler(0,0,-12*(1-t));
            }
            else
            {
                float t=Mathf.Clamp01((elapsed-.6f)/.2f);
                image.sprite=StratagemAssets.Named("fx.eagle-embed."+Mathf.Min(3,Mathf.FloorToInt(t*4)));
                transform.rotation=Quaternion.identity;
                transform.localScale=Vector3.one*(.65f/image.sprite.bounds.size.x);
                transform.position=new Vector3(point.x,point.y,StratagemAssets.Z(point.y)+.4f);
            }
        }
    }
}
