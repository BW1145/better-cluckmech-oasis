using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CluckmechCoop
{
    internal static class StratagemAssets
    {
        private static readonly string[] names = { "md6", "barrage380", "orbitalLaser", "eagle500", "emsMortar", "shieldRelay", "laserRover" };
        private static readonly Dictionary<StratagemKind, Sprite[]> atlases = new Dictionary<StratagemKind, Sprite[]>();
        internal static Sprite Frame(StratagemKind kind, int row, int column)
        {
            if (!atlases.TryGetValue(kind, out var frames))
            {
                using (var source = typeof(Plugin).Assembly.GetManifestResourceStream("CluckmechCoop.StratagemAssets." + names[(int)kind] + ".png"))
                {
                    if (source == null) throw new FileNotFoundException("Stratagem atlas missing: " + kind);
                    using (var memory = new MemoryStream())
                    {
                        source.CopyTo(memory);
                        var texture = new Texture2D(2,2,TextureFormat.RGBA32,false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                        if (!texture.LoadImage(memory.ToArray(), true)) throw new InvalidDataException("Invalid stratagem atlas: " + kind);
                        frames = new Sprite[48];
                        float w = texture.width / 8f;
                        var rows = StratagemCatalog.AtlasRows[(int)kind];
                        for (int y = 0; y < 6; y++) for (int x = 0; x < 8; x++)
                            frames[y * 8 + x] = Sprite.Create(texture, new Rect(x*w,texture.height-rows[y+1],w,rows[y+1]-rows[y]), new Vector2(.5f,.5f), w / 2f);
                        atlases.Add(kind, frames);
                    }
                }
            }
            return frames[row * 8 + column];
        }
        internal static Sprite Icon(StratagemKind kind, int id = 0)
        {
            if (id == 20101) return Frame(kind,2,4);
            if (id == 20102) return Frame(kind,1,0);
            switch (kind)
            {
                case StratagemKind.Mines: return Frame(kind,2,0);
                case StratagemKind.Laser: return Frame(kind,5,0);
                case StratagemKind.Eagle: return Frame(kind,1,0);
                case StratagemKind.Mortar: return Frame(kind,2,0);
                case StratagemKind.Shield: return Frame(kind,1,0);
                case StratagemKind.Rover: return Frame(kind,0,4);
                default: return Frame(kind,0,0);
            }
        }
        internal static SpriteRenderer Renderer(Transform parent, Sprite sprite, float width = 2)
        {
            var obj = new GameObject("StratagemSprite");
            if (parent != null) obj.transform.SetParent(parent, false);
            var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            obj.transform.localScale = Vector3.one * (width / sprite.bounds.size.x);
            return renderer;
        }
        internal static SpriteRenderer Effect(StratagemKind kind, int row, int start, int count, Vector2 position, float width, float duration, bool loop = false)
        {
            var renderer = Renderer(null, Frame(kind,row,start),width);
            renderer.transform.position = new Vector3(position.x,position.y,(position.y+10000)*.01f-.02f);
            var animation = renderer.gameObject.AddComponent<StratagemAnimation>();
            animation.Set(kind,row,start,count,duration,loop);
            return renderer;
        }
    }
    public sealed class StratagemAnimation : MonoBehaviour
    {
        private SpriteRenderer renderer;
        private StratagemKind kind;
        private int row, first, count;
        private float duration, started;
        private bool looping;
        internal void Set(StratagemKind type, int y, int x, int frames, float seconds, bool loop)
        { renderer = GetComponent<SpriteRenderer>(); kind = type; row = y; first = x; count = frames; duration = seconds; started = Time.time; looping = loop; }
        private void Update()
        {
            float elapsed = Time.time - started;
            if (elapsed >= duration) { Destroy(gameObject); return; }
            if (renderer == null || !Plugin.InCombat) { Destroy(gameObject); return; }
            float cycle = looping ? Mathf.Min(duration,.8f) : duration;
            int frame = Mathf.Min(count-1,(int)((looping ? elapsed % cycle : elapsed) / cycle * count));
            renderer.sprite = StratagemAssets.Frame(kind,row,first+frame);
        }
    }
}
