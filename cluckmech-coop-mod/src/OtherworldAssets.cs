using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class OtherworldAssets
    {
        private static Texture2D atlas,space,vfx;
        private static Sprite ground;
        private static readonly Texture2D[] faithful=new Texture2D[2];
        private static readonly Dictionary<string,int[]>[] faithfulRegions=new Dictionary<string,int[]>[2];
        private static Dictionary<string,int[]> regions;
        private static Dictionary<string,int[]> vfxRegions;
        private static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        private static readonly Dictionary<Sprite,Bounds> visible=new Dictionary<Sprite,Bounds>();
        private static Texture2D Load(string file)
        {
            using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("CluckmechCoop.OtherworldAssets."+file))
            using(var reader=new BinaryReader(stream))
            {
                var image=new Texture2D(2,2,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                if(!image.LoadImage(reader.ReadBytes((int)stream.Length),true))throw new InvalidDataException(file);
                return image;
            }
        }
        internal static Sprite Named(string key)
        {
            if(sprites.TryGetValue(key,out var sprite))return sprite;
            bool effect=key.StartsWith("electric.")||key.StartsWith("spiral.")||key=="orbital.block";
            if(effect&&vfx==null)
            {
                vfx=Load("vfx.png");
                using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("CluckmechCoop.OtherworldAssets.vfx.json"))
                using(var reader=new StreamReader(stream,System.Text.Encoding.UTF8))vfxRegions=JsonConvert.DeserializeObject<Dictionary<string,int[]>>(reader.ReadToEnd());
            }
            if(!effect&&atlas==null)
            {
                atlas=Load("characters.png");
                using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("CluckmechCoop.OtherworldAssets.characters.json"))
                using(var reader=new StreamReader(stream,System.Text.Encoding.UTF8))regions=JsonConvert.DeserializeObject<Dictionary<string,int[]>>(reader.ReadToEnd());
            }
            var texture=effect?vfx:atlas;var r=(effect?vfxRegions:regions)[key];
            var pivot=key.StartsWith("hero.")||key.StartsWith("anim.")?new Vector2(.5f,.0625f):new Vector2(.5f,.5f);
            if(key.StartsWith("effect.0.")||key.StartsWith("effect.1."))pivot=new Vector2(.5f,.1f);
            if(key.StartsWith("effect.5.")||key.StartsWith("effect.6."))pivot=new Vector2(.1f,.5f);
            if(key.StartsWith("electric.strike."))pivot=new Vector2(.5f,.09f);
            if(key=="orbital.block")pivot=new Vector2(.5f,.0625f);
            sprite=Sprite.Create(texture,new Rect(r[0],texture.height-r[1]-r[3],r[2],r[3]),pivot,32,0,SpriteMeshType.FullRect);
            visible[sprite]=new Bounds(new Vector3(((r[4]+r[6])*.5f-r[2]*pivot.x)/32, (r[3]-(r[5]+r[7])*.5f-r[3]*pivot.y)/32),new Vector3((r[6]-r[4])/32f,(r[7]-r[5])/32f));
            sprites.Add(key,sprite);return sprite;
        }
        private static Sprite Faithful(int hero,string key)
        {
            if(sprites.TryGetValue(key,out var sprite))return sprite;
            if(faithful[hero]==null)
            {
                faithful[hero]=Load("faithful-"+hero+".png");
                using(var stream=typeof(Plugin).Assembly.GetManifestResourceStream("CluckmechCoop.OtherworldAssets.faithful-"+hero+".json"))
                using(var reader=new StreamReader(stream,System.Text.Encoding.UTF8))faithfulRegions[hero]=JsonConvert.DeserializeObject<Dictionary<string,int[]>>(reader.ReadToEnd());
            }
            var texture=faithful[hero];var r=faithfulRegions[hero][key];
            sprite=Sprite.Create(texture,new Rect(r[0],texture.height-r[1]-r[3],r[2],r[3]),new Vector2(.5f,.0625f),32,0,SpriteMeshType.FullRect);
            visible[sprite]=new Bounds(new Vector3(((r[4]+r[6])*.5f-32)/32,(64-(r[5]+r[7])*.5f-4)/32),new Vector3((r[6]-r[4])/32f,(r[7]-r[5])/32f));
            sprites.Add(key,sprite);return sprite;
        }
        internal static int MovementFrameCount(int hero)=>16;
        internal static int AttackFrameCount(int hero)=>4;
        internal static float MovementStridePixels(int hero,int moveFacing,int aimFacing)
        {
            hero=FaithfulHero(hero);
            float angle=(moveFacing&7)*Mathf.PI/4;
            float x=Mathf.Sin(angle)*(hero==0?20f:16.8f),y=Mathf.Cos(angle)*(hero==0?13.2f:9.6f);
            return Mathf.Sqrt(x*x+y*y);
        }
        internal static Sprite Movement(int hero,int frame,int moveFacing,int aimFacing,bool attacking,int attackFrame=0)
        {
            hero=FaithfulHero(hero);
            int aim=aimFacing&7,relative=(moveFacing-aim+8)&7;
            string key="gait."+hero+"."+(attacking?"attack":"walk")+"."+aim+"."+relative+"."+((frame%16+16)%16);
            return Faithful(hero,key+(attacking?"."+((attackFrame%4+4)%4):""));
        }
        private static int FaithfulHero(int hero)
        {
            if(hero==12||hero==13)return hero-12;
            throw new System.ArgumentOutOfRangeException(nameof(hero),"Faithful characters require hero ID 12 or 13.");
        }
        internal static Sprite Pose(int hero,int aimFacing,bool attacking=false,int attackFrame=0)
        {
            hero=FaithfulHero(hero);
            return Faithful(hero,"pose."+hero+"."+(aimFacing&7)+"."+(attacking?"attack."+((attackFrame%4+4)%4):"idle"));
        }
        internal static Sprite Dash(int hero,int direction,int frame=0)
        {
            hero=FaithfulHero(hero);return Faithful(hero,"dash."+hero+"."+(direction&7)+(hero==1?"."+Mathf.Clamp(frame,0,OtherworldMotion.DashFrames-1):""));
        }
        internal static Sprite Hero(int hero,int row=0,int direction=0)=>Named("hero."+hero+"."+row+"."+direction);
        internal static Sprite Animation(int hero,string action,int frame,int direction)=>Named("anim."+hero+"."+action+"."+frame+"."+direction);
        internal static Bounds VisibleBounds(Sprite sprite)=>visible.TryGetValue(sprite,out var bounds)?bounds:sprite.bounds;
        internal static Sprite Portrait(int hero)
        {
            string key="portrait."+hero;if(sprites.TryGetValue(key,out var cached))return cached;
            var original=Hero(hero);var bounds=VisibleBounds(original);var rect=original.textureRect;
            rect.x+=(bounds.min.x-original.bounds.min.x)*32;rect.y+=(bounds.min.y-original.bounds.min.y)*32;
            rect.width=bounds.size.x*32;rect.height=bounds.size.y*32;
            var portrait=Sprite.Create(atlas,rect,new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect);sprites[key]=portrait;return portrait;
        }
        internal static Sprite Ground
        {
            get
            {
                if(ground!=null)return ground;
                space=Load("space.png");
                return ground=Sprite.Create(space,new Rect(0,0,space.width,space.height),new Vector2(.5f,.575f),space.width/72f,0,SpriteMeshType.FullRect);
            }
        }
        internal static void Release()
        {
            foreach(var sprite in sprites.Values)Object.Destroy(sprite);
            sprites.Clear();visible.Clear();if(ground!=null)Object.Destroy(ground);
            if(atlas!=null)Object.Destroy(atlas);if(space!=null)Object.Destroy(space);
            if(vfx!=null)Object.Destroy(vfx);vfx=null;vfxRegions=null;
            for(int i=0;i<faithful.Length;i++){if(faithful[i]!=null)Object.Destroy(faithful[i]);faithful[i]=null;faithfulRegions[i]=null;}
            atlas=space=null;ground=null;regions=null;
        }
    }
}
