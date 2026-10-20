using System.Collections.Generic;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class BronzeDragonPalette
    {
        private static readonly Dictionary<Sprite,Sprite> cache=new Dictionary<Sprite,Sprite>();
        internal static Sprite Sprite(Sprite source)
        {
            if(cache.TryGetValue(source,out var result))return result;
            // Native dragon sprites use unpacked textures; retain the complete sprite rectangle.
            var rect=source.rect;int width=(int)rect.width,height=(int)rect.height;
            var target=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            target.filterMode=FilterMode.Point;var active=RenderTexture.active;
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            try
            {
                Graphics.Blit(source.texture,target,new Vector2(rect.width/source.texture.width,rect.height/source.texture.height),new Vector2(rect.x/source.texture.width,rect.y/source.texture.height));
                RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);
            }
            finally {RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
            var pixels=texture.GetPixels32();
            for(int i=0;i<pixels.Length;i++)
            {
                Color original=pixels[i];Color.RGBToHSV(original,out float h,out float s,out float v);
                if(original.a==0||s<.15f||h<.07f||h>.22f)continue;
                Color bronze=Color.HSVToRGB(.075f,s*.7f,v*.86f);bronze.a=original.a;pixels[i]=bronze;
            }
            texture.SetPixels32(pixels);texture.Apply(false,true);
            result=UnityEngine.Sprite.Create(texture,new Rect(0,0,width,height),source.pivot/source.rect.size,source.pixelsPerUnit,0,SpriteMeshType.FullRect,source.border);
            result.name="Bronze_"+source.name;cache.Add(source,result);return result;
        }
    }
}
