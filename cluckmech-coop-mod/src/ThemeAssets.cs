using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace CluckmechCoop
{
    // Theme atlases share the production sprite-rectangle format with the stratagem loader.
    internal static class ThemeAssets
    {
        private sealed class Atlas
        {
            internal Texture2D Texture;
            internal Dictionary<string, StratagemSpriteRect> Rectangles;
            internal readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        }
        private static readonly Dictionary<string, Atlas> atlases = new Dictionary<string, Atlas>();
        private static int sortingLayer, sortingOrder;

        internal static void CaptureSorting(SpriteRenderer sample)
        {
            if (sample == null) return;
            sortingLayer = sample.sortingLayerID; sortingOrder = sample.sortingOrder;
        }

        private static string Resolve(string key)
        {
            if (key.StartsWith("castle.knight-leader.", StringComparison.Ordinal))
                return key.Replace("castle.knight-leader.", "castle.knight.");
            if (key.StartsWith("castle.boss-wine-bat.", StringComparison.Ordinal))
                return key.Replace("castle.boss-wine-bat.", "castle.wine-bat.");
            if (key.StartsWith("castle.shadow-knight.", StringComparison.Ordinal))
                return key.Replace("castle.shadow-knight.", "castle.knight.");
            if (key.StartsWith("castle.shadow-crossbow.", StringComparison.Ordinal))
                return key.Replace("castle.shadow-crossbow.", "castle.crossbow.");
            if (key.StartsWith("castle.shadow-leader.", StringComparison.Ordinal))
                return key.Replace("castle.shadow-leader.", "castle.knight.");
            return key;
        }

        internal static Sprite Named(string key)
        {
            key = Resolve(key);
            string theme = key.Substring(0, key.IndexOf('.'));
            if (!atlases.TryGetValue(theme, out var atlas))
            {
                var assembly = typeof(Plugin).Assembly;
                string prefix = "CluckmechCoop.ThemeAssets." + theme;
                atlas = new Atlas();
                using (var source = assembly.GetManifestResourceStream(prefix + ".png"))
                using (var memory = new MemoryStream())
                {
                    if (source == null) throw new InvalidDataException("Missing theme atlas: " + theme);
                    source.CopyTo(memory);
                    atlas.Texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) {
                        filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Theme." + theme
                    };
                    if (!atlas.Texture.LoadImage(memory.ToArray(), true))
                        throw new InvalidDataException("Invalid theme atlas: " + theme);
                }
                using (var source = assembly.GetManifestResourceStream(prefix + ".json"))
                using (var reader = new StreamReader(source, System.Text.Encoding.UTF8))
                    atlas.Rectangles = JsonConvert.DeserializeObject<Dictionary<string, StratagemSpriteRect>>(reader.ReadToEnd());
                atlases.Add(theme, atlas);
            }
            if (atlas.Sprites.TryGetValue(key, out var cached)) return cached;
            var box = atlas.Rectangles[key];
            var sprite = Sprite.Create(atlas.Texture,
                new Rect(box.X, atlas.Texture.height - box.Y - box.H, box.W, box.H),
                new Vector2(box.PX, box.PY), box.PPU, 0, SpriteMeshType.FullRect);
            sprite.name = key; atlas.Sprites.Add(key, sprite);
            return sprite;
        }

        internal static SpriteRenderer Renderer(Transform parent, string key, float width,
            Vector2 localPosition, bool ground = false)
        {
            var obj = new GameObject("Theme." + key);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(localPosition.x, localPosition.y,
                StratagemAssets.Z(localPosition.y) + (ground ? .6f : 0));
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = Named(key);
            renderer.sortingLayerID = sortingLayer; renderer.sortingOrder = sortingOrder;
            obj.transform.localScale = Vector3.one * (width / renderer.sprite.bounds.size.x);
            var world = obj.transform.position;
            world.z = StratagemAssets.Z(world.y) + (ground ? .6f : 0);
            obj.transform.position = world;
            return renderer;
        }

        internal static SpriteRenderer Part(Transform parent, string key, float width, Vector2 offset)
        {
            var image = Renderer(parent, key, width, offset);
            SetPart(image, key, width, offset);
            return image;
        }

        internal static void SetPart(SpriteRenderer image, string key, float width, Vector2 offset)
        {
            key = Resolve(key);
            image.sprite = Named(key);
            var box = atlases[key.Substring(0, key.IndexOf('.'))].Rectangles[key];
            float scale = width * box.PPU / box.W;
            image.transform.localScale = Vector3.one * scale;
            image.transform.localPosition = new Vector3(offset.x + box.MX * scale, offset.y + box.MY * scale, 0);
        }

        internal static int FrameCount(string prefix)
        {
            Named(prefix + "0");
            string resolved = Resolve(prefix);
            var entries = atlases[resolved.Substring(0, resolved.IndexOf('.'))].Rectangles;
            int count = 1;
            while (entries.ContainsKey(resolved + count)) count++;
            return count;
        }

        internal static void Release()
        {
            foreach (var atlas in atlases.Values)
            {
                foreach (var sprite in atlas.Sprites.Values) UnityEngine.Object.Destroy(sprite);
                UnityEngine.Object.Destroy(atlas.Texture);
            }
            atlases.Clear();
        }
    }
}
