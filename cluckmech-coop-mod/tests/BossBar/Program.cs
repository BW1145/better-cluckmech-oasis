using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine
{
    public class Object { public static void Destroy(object value) { } }
    public class MonoBehaviour : Object { public void Invoke(string name, float time) { } }
    public class Transform { public int GetSiblingIndex() => 0; public void SetSiblingIndex(int value) { } }
    public class GameObject
    {
        public object Component;
        public GameObject gameObject => this;
        public T GetComponent<T>() => (T)Component;
        public void SetActive(bool value) { }
    }
    public struct Color { public Color(float r, float g, float b, float a) { } }
    public struct Color32
    {
        public Color32(byte r, byte g, byte b, byte a) { }
        public static implicit operator Color(Color32 value) => new Color();
    }
    public static class Mathf
    {
        public static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0, 1);
        public static float Abs(float value) => Math.Abs(value);
    }
}
namespace UnityEngine.UI
{
    public class Image
    {
        private float fill;
        public float fillAmount { get => fill; set => fill = Math.Clamp(value, 0, 1); }
        public Color color;
        public GameObject gameObject = new GameObject();
        public Transform transform = new Transform();
    }
    public class Text { public string text; }
}
public class 模块_基本属性
{
    public float 当前生命值, 当前生命值上限;
    public float 获取生命值最大值() => 当前生命值上限;
}
public class 刷怪器 { public bool 是否开启挑战 = true; }
namespace CluckmechCoop { internal static class Plugin { internal static 刷怪器 Spawner = new 刷怪器(); } }

static class Program
{
    static BossBloodstrip Make(模块_基本属性 hp)
    {
        var bar = new BossBloodstrip { 本体 = new GameObject { Component = hp }, currentBar = new Image(), middleBar = new Image(), nextBar = new Image(), countText = new Text() };
        bar.初始化();
        CluckmechCoop.Difficulty.RememberBar(bar, hp);
        return bar;
    }
    static void Check(bool result, string name) { if (!result) throw new Exception(name); }
    static void Tick(BossBloodstrip bar)
    {
        bar.血量变化();
        typeof(BossBloodstrip).GetMethod("血条流动", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bar, null);
    }
    static void Main()
    {
        var fresh = new BossBloodstrip { 本体 = new GameObject() };
        bool failed = false;
        try { fresh.血量变化(); } catch (NullReferenceException) { failed = true; }
        Check(failed, "reproduce native update before delayed initialization");
        var ready = typeof(CluckmechCoop.BossBarReadyGate).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
        Check(!(bool)ready.Invoke(null, new object[]{fresh,null}), "production gate skips uninitialized bar");
        var hp = new 模块_基本属性 { 当前生命值 = 5000, 当前生命值上限 = 5000 };
        var original = Make(hp);
        Check((bool)ready.Invoke(null, new object[]{original,hp}), "initialized bar retains native update");
        original.本体 = null;
        Check(!(bool)ready.Invoke(null, new object[]{original,hp}), "removed boss does not update stale UI");
        original.本体 = new GameObject { Component = hp };
        hp.当前生命值上限 = 7500; hp.当前生命值 = 7500;
        Tick(original);
        hp.当前生命值 -= 100;
        Tick(original);
        Check(original.currentBar.fillAmount == 1 && original.count == 1, "reproduce native bar staying full after post-initialization strengthening and damage");

        var cache = typeof(BossBloodstrip).GetField("生命值缓存", BindingFlags.Instance | BindingFlags.NonPublic);
        float cachedHealth = (float)cache.GetValue(original);
        CluckmechCoop.Difficulty.RefreshBar(original, hp, ref cachedHealth);
        cache.SetValue(original, cachedHealth);
        Tick(original);
        Check(original.count == 2 && original.countText.text == "X3", "production refresh includes strengthened health segments");
        Check(Math.Abs(original.currentBar.fillAmount - 0.96f) < 0.0001f, "100 damage is immediately visible after refresh");
        hp.当前生命值 -= 100;
        Tick(original);
        Check(Math.Abs(original.currentBar.fillAmount - 0.92f) < 0.0001f, "subsequent damage remains visible without resetting bar");
        cachedHealth = (float)cache.GetValue(original);
        CluckmechCoop.Difficulty.RefreshBar(original, hp, ref cachedHealth);
        Check(cachedHealth == hp.当前生命值, "unchanged maximum preserves damage cache");
        Console.WriteLine("PASS: 9 native boss-bar regression checks using production gate and refresh methods; Unity UI represented by test doubles");
    }
}

namespace CluckmechCoop { internal static class ModuleSettings { internal static bool Active = true; } }
