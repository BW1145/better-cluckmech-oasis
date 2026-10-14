using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class StratagemGauge
    {
        private static Sprite pixel;
        private readonly Transform fill;
        private float previous = -1;
        internal StratagemGauge(Transform parent,float height,Color color)
        {
            if (pixel == null)
            {
                var texture = new Texture2D(1,1,TextureFormat.RGBA32,false) {filterMode = FilterMode.Point};
                texture.SetPixel(0,0,Color.white); texture.Apply(false,true);
                pixel = Sprite.Create(texture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
            }
            var frame = Part(parent,"Gauge frame",new Color(.08f,.08f,.07f),Vector2.zero,new Vector2(1.48f,.16f));
            frame.localPosition = new Vector3(0,height,-.07f);
            Part(frame,"Steel rim",new Color(.6f,.6f,.54f),Vector2.zero,new Vector2(.97f,.78f));
            Part(frame,"Gauge track",new Color(.15f,.16f,.16f),Vector2.zero,new Vector2(.89f,.5f));
            fill = Part(frame,"Gauge fill",color,new Vector2(-.445f,0),new Vector2(0,.5f));
        }
        private static Transform Part(Transform parent,string name,Color color,Vector2 position,Vector2 size)
        {
            var image = StratagemAssets.Renderer(parent,pixel,1); image.name = name; image.color = color;
            image.transform.localPosition = new Vector3(position.x,position.y,-.001f);
            image.transform.localScale = new Vector3(size.x,size.y,1); return image.transform;
        }
        internal void Set(float value)
        {
            value = Mathf.Clamp01(value);
            if (fill == null || Mathf.Abs(value-previous) < .005f) return;
            previous = value; fill.localScale = new Vector3(.89f*value,.5f,1);
            fill.localPosition = new Vector3(-.445f+.445f*value,0,-.003f);
        }
    }
}
