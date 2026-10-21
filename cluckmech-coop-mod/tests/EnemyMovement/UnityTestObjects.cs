using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine
{
    public class SerializeField : Attribute { }
    public class HideInInspector : Attribute { }
    public class DisallowMultipleComponent : Attribute { }
    public class Header : Attribute { public Header(string title) { } }
    public class AddComponentMenu : Attribute { public AddComponentMenu(string title) { } }
    public enum FindObjectsSortMode { None }
    public enum Space { Self }
    public enum ForceMode2D { Force }
    public struct Quaternion { public Quaternion(float x, float y, float z, float w) { } public static Quaternion identity => new Quaternion(); }
    public struct Color { public Color(float r, float g, float b, float a = 1) { } public static Color blue => new Color(); }
    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2();
        public float magnitude => MathF.Sqrt(x * x + y * y);
        public Vector2 normalized => magnitude == 0 ? zero : this / magnitude;
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float n) => new Vector2(a.x * n, a.y * n);
        public static Vector2 operator *(float n, Vector2 a) => a * n;
        public static Vector2 operator /(Vector2 a, float n) => new Vector2(a.x / n, a.y / n);
        public static bool operator ==(Vector2 a, Vector2 b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public override bool Equals(object value) => value is Vector2 v && this == v;
        public override int GetHashCode() => HashCode.Combine(x, y);
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static Vector2 ClampMagnitude(Vector2 value, float max) => value.magnitude > max ? value.normalized * max : value;
        public static Vector2 MoveTowards(Vector2 a, Vector2 b, float max) => a + ClampMagnitude(b - a, max);
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z = 0) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3();
        public float magnitude => MathF.Sqrt(x * x + y * y + z * z);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => new Vector3(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t), Mathf.Lerp(a.z, b.z, t));
    }
    public static class Mathf
    {
        public const float Epsilon = float.Epsilon;
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Ceil(float a) => MathF.Ceiling(a);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0, 1);
        public static float Atan2(float y, float x) => MathF.Atan2(y, x);
        public static float MoveTowardsAngle(float a, float b, float max) => a + Math.Clamp(b - a, -max, max);
    }
    public static class Time { public static float deltaTime = 0.1f, time; }
    public static class Random { public static float Range(float a, float b) => a; }
    public static class Debug { public static void LogError(string message) => throw new Exception(message); public static void DrawLine(Vector2 a, Vector2 b, Color color) { } }
    public class Object
    {
        public static int RewardCount;
        public static int FeedbackCount;
        public static void Destroy(object value) { }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sort) => GameObject.Registry.SelectMany(g => g.Components).OfType<T>().ToArray();
        public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation) { if (original.name == "talent") RewardCount++; if (original.name == "通关反馈UI") FeedbackCount++; return original; }
    }
    public class Transform
    {
        public Vector3 position, localEulerAngles;
        public Transform transform => this;
        public void Translate(Vector2 distance, Space space) { position += (Vector3)distance; }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
        public bool TryGetComponent<T>(out T value) where T : class => gameObject.TryGetComponent(out value);
        public T AddComponent<T>() where T : Component, new() => gameObject.AddComponent<T>();
    }
    public class WaitForSeconds { public WaitForSeconds(float time) { } }
    public class MonoBehaviour : Component
    {
        public readonly List<IEnumerator> Coroutines = new List<IEnumerator>();
        public void Invoke(string name, float time) { }
        public void InvokeRepeating(string name, float time, float interval) { }
        public void StartCoroutine(IEnumerator task) { Coroutines.Add(task); }
        public static void print(string message) { }
    }
    public class GameObject : Object
    {
        public static readonly List<GameObject> Registry = new List<GameObject>();
        public readonly List<Component> Components = new List<Component>();
        public string name;
        public bool activeSelf = true;
        public Transform transform = new Transform();
        public GameObject(string name) { this.name = name; Registry.Add(this); }
        public static GameObject Find(string name) => Registry.FirstOrDefault(g => g.name == name);
        public T AddComponent<T>() where T : Component, new() { var c = new T { gameObject = this }; Components.Add(c); return c; }
        public T GetComponent<T>() where T : class => Components.OfType<T>().FirstOrDefault();
        public bool TryGetComponent<T>(out T value) where T : class { value = GetComponent<T>(); return value != null; }
        public void SetActive(bool value) { activeSelf = value; }
        public void SendMessage(string method) { }
    }
    public class Rigidbody2D : Component { public Vector2 velocity; public void AddForce(Vector2 value, ForceMode2D mode = ForceMode2D.Force) { } }
    public class Animator : Component { public string Trigger; public void SetTrigger(string name) { Trigger = name; } }
}
namespace UnityEngine.UI { public class Text : UnityEngine.Component { public string text; } }
namespace Unity.VisualScripting { }
namespace PolyNav
{
    // The test map is an unobstructed area; navigation decisions and movement are executed by the native agent source.
    public class PolyNavMap : UnityEngine.Component
    {
        public static PolyNavMap current;
        public bool PointIsValid(UnityEngine.Vector2 p) => true;
        public UnityEngine.Vector2 GetCloserEdgePoint(UnityEngine.Vector2 p) => p;
        public bool CheckLOS(UnityEngine.Vector2 a, UnityEngine.Vector2 b) => true;
        public void FindPath(UnityEngine.Vector2 from, UnityEngine.Vector2 to, Action<UnityEngine.Vector2[]> result) => result(new[] { to });
    }
}
public class 模块_基本属性 : UnityEngine.Component
{
    public enum 单位类型枚举 { 地面单位 = 11, 飞行单位 = 12, 地面友方单位 = 9, 玩家地面单位 = 18 }
    public 单位类型枚举 单位类型 = 单位类型枚举.地面单位;
    public bool 是否无敌, 当前是否无敌, 免疫击退;
    public float 生命值 = 100, 当前生命值 = 100;
}
public class 模块_状态属性 : UnityEngine.Component { }
public class 模块_词条_词缀管理器 : UnityEngine.Component { public void 收到词条(世界类.词条数据包 data) { } }
public class 世界类 { public class 词条数据包 { public int id; public bool 是否为默认词条, 是否显性; public string 词条名字; } public class 仇恨数据包 { public float 仇恨值; public UnityEngine.GameObject 仇恨目标; } }
public class 导航管理器 : UnityEngine.Component
{
    public PolyNav.PolyNavMap 地面导航地图, 空中导航地图;
    public static event Action 事件_重新生成地图;
}
public class 效果物体引用器 : UnityEngine.Component { public class Prefabs { public UnityEngine.GameObject Boss死亡反馈; } public Prefabs 预制体 = new Prefabs(); }
public class 刷怪器 : UnityEngine.Component
{
 public enum 关卡状态枚举 { 无, 等待, 正在出怪, 战斗, 结束, 放置等待 }
 public bool 是否开启挑战 = true, 是否计时;
 public float 倒计时;
 public int 当前波次, 波次 = 3;
 public 关卡状态枚举 关卡状态;
}
public static class 常数类 { public static string 命名_导航管理器 = "navigation"; }
namespace 函数库 { public static class 常用函数 { public static T 搜索物体并返回物体内组件<T>(string name) where T : class => UnityEngine.GameObject.Find(name).GetComponent<T>(); } }
namespace CluckmechCoop
{
    internal static class Plugin
    {
        internal static 刷怪器 Spawner = new 刷怪器();
        internal static int Role;
        internal static UnityEngine.MonoBehaviour Instance = new UnityEngine.MonoBehaviour();
        internal static void Log(string text) { }
    }
}

namespace CluckmechCoop { internal static class ModuleSettings { internal static bool Active = true; } }

namespace CluckmechCoop { internal static class WealthEncounter {internal static bool Pending=>false;internal static bool DeferBoss(object owner,Action continuation)=>false;} }
