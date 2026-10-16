using UnityEngine;

namespace CluckmechCoop
{
    public sealed class StratagemFlipbook : MonoBehaviour
    {
        private SpriteRenderer image;
        private Sprite[] frames;
        private float started,duration;
        internal void Set(SpriteRenderer renderer,string prefix,int count,float seconds,float delay)
        {
            image=renderer;frames=new Sprite[count];
            for(int i=0;i<count;i++)frames[i]=StratagemAssets.Named(prefix+i);
            started=Time.time+delay;duration=seconds;
        }
        private void Update()
        {
            float elapsed=Time.time-started;
            if(elapsed>=0)image.sprite=frames[Mathf.Min(frames.Length-1,Mathf.FloorToInt(elapsed/duration*frames.Length))];
        }
    }
    public sealed class StratagemAnimation : MonoBehaviour
    {
        private SpriteRenderer image;
        private Vector3 scale,origin;
        private Color color;
        private float started,duration,delay,grow,rise;
        private bool ground,hold;
        internal bool Steady;
        private string token;
        internal void Set(SpriteRenderer renderer,float seconds,float wait,float growth,float lift,bool floor,bool peak)
        {
            image=renderer; scale=transform.localScale; origin=transform.position; color=image.color;
            started=Time.time; duration=seconds; delay=wait; grow=growth; rise=lift; ground=floor;hold=peak;
            token=Plugin.Combat?.Token;
            image.enabled=wait==0;
        }
        private void Update()
        {
            float elapsed=Time.time-started-delay;
            if (!Plugin.InCombat || token!=Plugin.Combat?.Token || elapsed >= duration) { Destroy(gameObject); return; }
            if (elapsed < 0) { image.enabled=false; return; }
            image.enabled=true;
            float t=Mathf.Clamp01(elapsed/duration);
            float alpha=Steady?1:hold ? Mathf.Clamp01((1-t)/.4f) : ground ? Mathf.Clamp01((1-t)*4) : Mathf.Clamp01(t*12)*Mathf.Pow(1-t,.7f);
            image.color=new Color(color.r,color.g,color.b,color.a*alpha);
            if(grow!=0)transform.localScale=scale*(1+grow*t);
            if(rise!=0)transform.position=origin+new Vector3(0,rise*t,rise*t*.01f);
        }
    }
}
