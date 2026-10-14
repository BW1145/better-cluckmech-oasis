using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace CluckmechCoop
{
    internal sealed class StratagemSpriteRect
    {
        public int X{get;set;} public int Y{get;set;} public int W{get;set;} public int H{get;set;}
        public float PX{get;set;} public float PY{get;set;} public float MX{get;set;} public float MY{get;set;}
    }
    internal static class StratagemAssets
    {
        private static Texture2D texture;
        private static Dictionary<string,StratagemSpriteRect> rectangles;
        private static readonly Dictionary<string,Sprite> sprites = new Dictionary<string,Sprite>();
        internal static float Z(float y) => (y+10000)*.01f;
        internal static Sprite Named(string name)
        {
            if (sprites.TryGetValue(name,out var sprite)) return sprite;
            if (texture == null)
            {
                var assembly = typeof(Plugin).Assembly;
                using (var source = assembly.GetManifestResourceStream("CluckmechCoop.StratagemAssets.stratagem-v2.png"))
                using (var memory = new MemoryStream())
                {
                    source.CopyTo(memory);
                    texture = new Texture2D(2,2,TextureFormat.RGBA32,false) {filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                    if (!texture.LoadImage(memory.ToArray(),true)) throw new InvalidDataException("Invalid stratagem atlas");
                }
                using (var source = assembly.GetManifestResourceStream("CluckmechCoop.StratagemAssets.stratagem-v2.json"))
                using (var reader = new StreamReader(source,System.Text.Encoding.UTF8))
                    rectangles = JsonConvert.DeserializeObject<Dictionary<string,StratagemSpriteRect>>(reader.ReadToEnd());
            }
            var box = rectangles[name];
            sprite = Sprite.Create(texture,new Rect(box.X,texture.height-box.Y-box.H,box.W,box.H),new Vector2(box.PX,box.PY),32,0,SpriteMeshType.FullRect);
            sprites.Add(name,sprite); return sprite;
        }
        internal static Sprite Icon(StratagemKind kind,int id = 0) => Named("icon."+(id == 0 ? 20001+(int)kind : id));
        internal static Sprite Preview(int id) => Named("preview."+id);
        internal static Vector3 Muzzle(string name)
        { Named(name); var box=rectangles[name]; return new Vector3(box.MX,box.MY,0); }
        internal static Sprite AffixIcon(int id)
        {
            switch (id)
            {
                case 25012: id=25016; break; case 25013: id=25011; break; case 25025: id=25024; break;
                case 25034: id=25032; break; case 25042: id=25046; break; case 25054: id=25053; break;
                case 25056: id=25051; break; case 25066: id=25062; break; case 25073: id=25072; break;
            }
            return Named("affix."+id);
        }
        internal static Sprite Frame(StratagemKind kind,int row,int column) => Named("old."+(int)kind+"."+row+"."+column);
        internal static SpriteRenderer Renderer(Transform parent,Sprite sprite,float width = 2)
        {
            var obj = new GameObject("StratagemSprite");
            if (parent != null) obj.transform.SetParent(parent,false);
            var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite=sprite;
            obj.transform.localScale=Vector3.one*(width/sprite.bounds.size.x);
            return renderer;
        }
        internal static SpriteRenderer Part(Transform parent,string name,float width,Vector3 position)
        {
            var renderer=Renderer(parent,Named(name),width); renderer.transform.localPosition=position;
            return renderer;
        }
        internal static SpriteRenderer Effect(string name,Vector2 position,float width,float duration,float delay=0,bool ground=false,float grow=0,float rise=0,float opacity=1)
        {
            var renderer=Renderer(null,Named(name),width);
            renderer.transform.position=new Vector3(position.x,position.y,Z(position.y)+(ground?.4f:-.025f));
            renderer.color=new Color(1,1,1,opacity);
            renderer.gameObject.AddComponent<StratagemAnimation>().Set(renderer,duration,delay,grow,rise,ground);
            return renderer;
        }
    }
    public sealed class StratagemAnimation : MonoBehaviour
    {
        private SpriteRenderer image;
        private Vector3 scale,origin;
        private Color color;
        private float started,duration,delay,grow,rise;
        private bool ground;
        private string token;
        internal void Set(SpriteRenderer renderer,float seconds,float wait,float growth,float lift,bool floor)
        {
            image=renderer; scale=transform.localScale; origin=transform.position; color=image.color;
            started=Time.time; duration=seconds; delay=wait; grow=growth; rise=lift; ground=floor;
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
            float alpha=ground ? Mathf.Clamp01((1-t)*4) : Mathf.Clamp01(t*12)*Mathf.Pow(1-t,.7f);
            image.color=new Color(color.r,color.g,color.b,color.a*alpha);
            transform.localScale=scale*(1+grow*t);
            if(rise!=0)transform.position=origin+new Vector3(0,rise*t,rise*t*.01f);
        }
    }
}
