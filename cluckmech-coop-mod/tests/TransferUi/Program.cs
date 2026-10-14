using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text.Json;
using CluckmechCoop;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine
{
    public class Object
    {
        public static readonly Dictionary<Type, object> Scene = new Dictionary<Type, object>();
        public static T FindObjectOfType<T>() => Scene.TryGetValue(typeof(T), out var value) ? (T)value : default;
        public static GameObject Instantiate(GameObject original, Transform parent) { var go=new GameObject(original.name);go.transform.SetParent(parent,false);return go; }
        public static T[] FindObjectsOfType<T>() => Array.Empty<T>();
        public static void Destroy(Object value) { if (value is GameObject destroyed) destroyed.SetActive(false); if(value is GameObject go && go.GetComponent<GrowthPanel>() is GrowthPanel growth) typeof(GrowthPanel).GetMethod("OnDestroy",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(growth,null); }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public string name { get => gameObject.name; set => gameObject.name=value; }
        public T GetComponentInChildren<T>() { var own=GetComponent<T>(); if(own!=null)return own;foreach(var child in transform.Children){var found=child.GetComponentInChildren<T>();if(found!=null)return found;}return default; }
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() => gameObject.GetComponent<T>();
        public T GetComponentInParent<T>() { for (var p = transform; p != null; p = p.parent) { var c = p.gameObject.GetComponent<T>(); if (c != null) return c; } return default; }
    }
    public class MonoBehaviour : Component { }
    public class GameObject : Object
    {
        public string name;
        public bool Active = true;
        public RectTransform transform;
        private readonly Dictionary<Type, Component> components = new Dictionary<Type, Component>();
        public GameObject(string name) { this.name = name; transform = new RectTransform { gameObject = this }; components[typeof(RectTransform)] = transform; }
        public T AddComponent<T>() where T : Component, new() { var c = new T { gameObject = this }; components[typeof(T)] = c; return c; }
        public T GetComponent<T>() => components.TryGetValue(typeof(T), out var value) ? (T)(object)value : default;
        public T GetComponentInChildren<T>() => transform.GetComponentInChildren<T>();
        public void SetActive(bool value) => Active = value;
    }
    public class Transform : Component
    {
        public Transform parent;
        public Transform Find(string name) => Children.Find(c=>c.gameObject.name==name);
        public Vector3 TransformPoint(Vector2 value)=>new Vector3(value.x,value.y,0);
        public Vector3 InverseTransformPoint(Vector3 value)=>value;
        public readonly List<Transform> Children = new List<Transform>();
        public void SetParent(Transform target, bool unused) { parent?.Children.Remove(this); parent = target; target.Children.Add(this); }
        public void SetAsLastSibling() { if (parent != null) { parent.Children.Remove(this); parent.Children.Add(this); } }
    }
    public class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta, offsetMin, offsetMax;
        public Vector3 localScale;
        public Rect rect => new Rect(sizeDelta.x == 0 ? 1920 : sizeDelta.x, sizeDelta.y == 0 ? 1080 : sizeDelta.y);
    }
    public struct Rect { public float width, height; public Vector2 center=>new Vector2(width/2,height/2); public Rect(float w, float h) { width = w; height = h; } }
    public struct Vector2 { public float x,y; public Vector2(float x, float y) { this.x=x; this.y=y; } public static Vector2 zero => new Vector2(); public static Vector2 one => new Vector2(1,1); }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;} public Vector3(float v) { x=y=z=v; } public static Vector3 one => new Vector3(1); public static Vector3 operator *(Vector3 v, float s) => new Vector3(v.x*s); }
    public struct Vector2Int { public int x,y; public Vector2Int(int x,int y){this.x=x;this.y=y;} }
    public struct Vector3Int { public int x,y,z; public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;} }
    public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a=1) { this.r=r;this.g=g;this.b=b;this.a=a; } public static Color white => new Color(1,1,1); public static Color clear => new Color(0,0,0,0); }
    public enum TextAnchor { MiddleLeft, MiddleCenter, UpperLeft }
    public sealed class Sprite { public string Name; public Sprite(string name="") { Name=name; } }
    public sealed class Canvas : Component { public Canvas rootCanvas => this; }
    public static class Mathf { public static float Min(params float[] values) { var result = values[0]; foreach(var value in values) result=Math.Min(result,value); return result; } public static int Max(int a,int b)=>Math.Max(a,b); }
}
namespace UnityEngine.UI
{
    public class Graphic : Component { public bool raycastTarget, enabled=true; public Color color; }
    public sealed class Image : Graphic { public enum Type { Simple, Sliced, Tiled } public Sprite sprite; public Type type; public float pixelsPerUnitMultiplier; public bool preserveAspect; }
    public sealed class Text : Graphic { public RectTransform rectTransform => gameObject.transform; public string text; public TextAnchor alignment; public int FontSize; public float lineSpacing = 1; }
    public struct ColorBlock { public Color normalColor, highlightedColor, pressedColor; }
    public sealed class Click { private Action action; public void AddListener(Action value) => action += value; public void Invoke() => action?.Invoke(); }
    public sealed class ValueChanged { private Action<bool> action; public void AddListener(Action<bool> value) => action += value; public void Invoke(bool value) => action?.Invoke(value); }
    public sealed class Toggle : Component { public bool isOn; public ValueChanged onValueChanged = new ValueChanged(); public void SetIsOnWithoutNotify(bool value) { isOn = value; } }
    public sealed class Button : Component { public Image targetGraphic; public Image image => targetGraphic; public bool interactable; public ColorBlock colors; public Click onClick = new Click(); public string Title; }
    public sealed class InputField : Component { public enum ContentType { IntegerNumber } public Image targetGraphic; public Text textComponent; public ContentType contentType; public string text { get=>textComponent.text; set=>textComponent.text=value; } public bool interactable; }
    public sealed class RectMask2D : Component { }
    public sealed class Scrollbar : Component { public enum Direction { BottomToTop } public RectTransform handleRect; public Image targetGraphic; public Direction direction; }
    public sealed class Padding { public int left, right; }
    public sealed class GridLayoutGroup : Component { public enum Constraint { FixedColumnCount } public Constraint constraint; public int constraintCount; public Vector2 cellSize=new Vector2(250,200), spacing; public Padding padding=new Padding(); }
    public static class LayoutRebuilder { public static void ForceRebuildLayoutImmediate(RectTransform rect) { } }
    public sealed class ScrollRect : Component { public enum MovementType { Clamped } public enum ScrollbarVisibility { AutoHide } public bool horizontal; public MovementType movementType; public float scrollSensitivity; public RectTransform viewport,content; public Scrollbar verticalScrollbar; public ScrollbarVisibility verticalScrollbarVisibility; }
}
namespace HarmonyLib { public static class AccessTools { public static FieldInfo Field(Type type,string name)=>type.GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance); } public static class Priority { public const int Last=0; } [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyPriority : Attribute { public HarmonyPriority(int value) { } } [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch : Attribute { public HarmonyPatch(Type type,string name) { } public HarmonyPatch(Type type,string name,Type[] args) { } } }
public sealed class 全局加成 { public float 商店价格倍率加成=-.2f; }
public sealed class dw单位价格库 { public int[] 单位价格 = {10,20,60,80,100,120}; }
public sealed class 背包类预制体脚本 : Component {
 public List<int> 当前已选单元=new List<int>();public List<GameObject> 单元列表=new List<GameObject>();
 public void 更新背包(int count){单元列表.Clear();for(int i=0;i<count;i++){var go=new GameObject("Stock");go.transform.SetParent(transform,false);单元列表.Add(go);}}
}
public sealed class 商店脚本 : Component { private 总结脚本 总结 = Plugin.Summary; public GameObject 商品背包类; public Vector3Int[] 当前商品=new Vector3Int[3];public int 刷新费用=5, 商品单位等级=2;public Vector2Int 价格变化区间=new Vector2Int(0,1);public dw单位价格库 价格库=new dw单位价格库();public 全局加成 全局加成=new 全局加成(); }
public sealed class 选择词条脚本 { public int 词条剩余刷新次数=1; }
public sealed class 选择词条流程脚本 { public 选择词条脚本 选择词条=new 选择词条脚本(); public Button 刷新按钮=new Button();public Text 剩余刷新次数文本=new Text(); }
namespace CluckmechCoop { public sealed class RuleTooltip : MonoBehaviour { public string Text; } }
namespace UnityEngine { public static class Random { public static int Range(int min,int max)=>min; } }
namespace UnityEngine.SceneManagement { public static class SceneManager { public struct Scene { public int buildIndex; } public static Scene GetActiveScene() => new Scene { buildIndex=10 }; } }
namespace 函数库 { public static class 常数类 { public const int 场景id_异步加载场景=-1, 场景id_选择地图=8; } public static class 场景函数 { } }
public sealed class 联机玩家控件 { public void 上传玩家总结数据() { } }
public sealed class 玩家背包脚本 { public void 更新背包() { } }
public sealed class 刷怪器 { public sealed class Data { public bool 是否最终关卡; public List<Stage> 波次数据=new List<Stage>(); } public sealed class Stage { public List<Group> 组次数据 = new List<Group>(); } public sealed class Group { public List<GameObject> 单位=new List<GameObject>(); } public Data 数据=new Data(); }
namespace CluckmechCoop { internal class SessionMessage { public string Type, Token; } internal class GrowthNetwork { internal void Broadcast(SessionMessage value) { } } }
public sealed class 总结脚本 { public void 获得金钱(int value) { 当前金钱数+=value; } public class Map { public bool 是否位于工匠; public int 当前地图页位置=1; public string 当前地图页="area"; } public Map 地图存档=new Map(); public void 保存单局存档() { } public sealed class 背包单元 { public int 炮台id,强化上限; public List<int> 强化词条 = new List<int>(); } public List<背包单元> 当前拥有炮台 = new List<背包单元>(); public bool 是否光标位于UI界面; public int 当前金钱数; }
public sealed class 模块_基本属性 : Component { public string Name="同名鸡械"; public string 名称() => Name; public Sprite 图标 = new Sprite(); }
public sealed class cz词缀库 : Component { public int Calls; public string 获取词缀名称(int id) { Calls++; return "强化" + id; } }
public sealed class dw单位库 : Component { public sealed class 单位单元 { public int id, 单位等级=2; public bool 是否商店内出现=true, 是否游戏内出现=true; } public List<单位单元> 单位列表=new List<单位单元>(); public bool 是否抽取(int id)=>true; public GameObject Unit = new GameObject("Unit"); public Dictionary<int,GameObject> Examples=new Dictionary<int,GameObject>(); public GameObject 获取单位(int id) => Examples.TryGetValue(id,out var item)?item:Unit; }
public sealed class 光标管理器 : Component { }
public sealed class 设置管理器 : Component { public float UI界面大小=1; }
namespace CluckmechCoop
{
    internal static class ModuleSettings { internal static bool Transfer = true; internal static GameplayModules Extra = new GameplayModules { Growth = true }; internal static string NodeKey="shop-1"; internal static PlayerProgress LocalProgress = new PlayerProgress(); internal static void SaveProgress() { } private static readonly Dictionary<ModuleId, bool> values = new Dictionary<ModuleId, bool>(); internal static bool Preference(ModuleId id) => !values.TryGetValue(id, out bool value) || value; internal static void Preference(ModuleId id, bool value) => values[id] = value; }
    internal static class Plugin
    {
        internal static CoopPanel Panel;
        internal static int Role;
        internal static 联机玩家控件 LocalPlayer = new 联机玩家控件();
        internal static GrowthNetwork Network = new GrowthNetwork();
        internal static void Log(string value) { }
        internal static bool InCombat = false, CanTrade = true;
        internal static 总结脚本 Summary = new 总结脚本();
        internal static dw单位库 Units;
        internal static TransferService Transfers = new TransferService();
    }
    internal sealed class TransferService
    {
        internal bool Locked, PeerReady=true;
        internal string Status="";
        internal int Requests, Index;
        internal Machine Expected;
        internal void Request(string kind,int amount,int index,Machine expected=null) { Requests++; Index=index; Expected=expected; }
        internal static Machine Snapshot(总结脚本.背包单元 item) => new Machine { Id=item.炮台id, UpgradeLimit=item.强化上限, Upgrades=new List<int>(item.强化词条) };
    }
    internal static class NativeUi
    {
        internal static void Capture(Transform source) => Canvas=source.GetComponentInParent<Canvas>();
        internal static Canvas Canvas;
        internal static Sprite TransferTexture=new Sprite("UI_通用点阵_0"), TransferPanel=new Sprite("UI_备战界面_1"), GoldIcon=new Sprite("货币"), TransferCard=new Sprite("UI_黑色底框"), Selection=new Sprite("选择框");
        internal static readonly List<Button> Actions = new List<Button>();
        internal static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h) { var r=new GameObject(name).transform; r.SetParent(parent,false); r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r; }
        internal static Image Image(RectTransform rect,Sprite sprite,Color color) { var i=rect.gameObject.AddComponent<Image>();i.sprite=sprite;i.color=color;return i; }
        internal static Text Label(Transform parent,string text,float x,float y,float w,float h,int size) { var t=Rect(parent,"Text",x,y,w,h).gameObject.AddComponent<Text>();t.text=text;t.FontSize=size;t.color=Color.white;return t; }
        internal static Button TransferAction(Transform parent,string text,float x,float y,float w,float h,Action action) { var r=Rect(parent,text,x,y,w,h);var b=r.gameObject.AddComponent<Button>();b.Title=text;b.targetGraphic=Image(r,new Sprite("UI_天赋强化确认"),Color.white);b.targetGraphic.pixelsPerUnitMultiplier=8;var label=Label(r,text,8,0,w-16,h,24);label.color=new Color(.18f,.17f,.14f);b.onClick.AddListener(action);Actions.Add(b);return b; }
        internal static ScrollRect ScrollArea(Transform parent, string name, float x, float y, float w, float h, out RectTransform content)
        {
            var root = NativeUi.Rect(parent, name, x, y, w + 24, h);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            var viewport = NativeUi.Rect(root, "Viewport", 0, 0, w, h);
            NativeUi.Image(viewport, null, Color.clear);
            viewport.gameObject.AddComponent<RectMask2D>(); scroll.viewport = viewport;
            content = NativeUi.Rect(viewport, "Content", 0, 0, w, h); scroll.content = content;
            var track = NativeUi.Rect(root, "Scrollbar", w + 8, 0, 12, h);
            NativeUi.Image(track, NativeUi.TransferCard, Color.white);
            var handle = NativeUi.Rect(track, "Handle", 0, 0, 0, 0);
            handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one; handle.offsetMin = handle.offsetMax = Vector2.zero;
            var handleImage = NativeUi.Image(handle, NativeUi.TransferPanel, Color.white); handleImage.pixelsPerUnitMultiplier = 4;
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle; bar.targetGraphic = handleImage; bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }
        internal static Toggle Toggle(Transform parent,string title,float x,float y,float w,float h,bool value,Action<bool> changed) {
            var r=Rect(parent,title,x,y,w,h);var t=r.gameObject.AddComponent<Toggle>();
            var check=Image(Rect(r,"Check",0,0,40,40),new Sprite(value?"UI_暂停界面开关_1":"UI_暂停界面开关_0"),Color.white);
            t.SetIsOnWithoutNotify(value);t.onValueChanged.AddListener(v=>{check.sprite=new Sprite(v?"UI_暂停界面开关_1":"UI_暂停界面开关_0");changed(v);}); return t;
        }

    }
}
static class Program
{
    static int checks;
    static void Check(bool condition,string name) { if(!condition) throw new Exception(name);checks++; }
    static object Field(object item,string name) => item.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance).GetValue(item);
    static Button Card(IList cards,int i) => (Button)Field(cards[i],"Button");
    static void Layout(Transform node,float x,float y,List<object> items,float[] clip=null)
    {
        if(!node.gameObject.Active)return;
        if(node.gameObject.name=="Scrollbar" && node.parent.GetComponent<ScrollRect>() is ScrollRect owner && owner.content.sizeDelta.y<=owner.viewport.sizeDelta.y)return;
        var rect=(RectTransform)node;x+=rect.anchoredPosition.x;y-=rect.anchoredPosition.y;
        var scroll=node.GetComponent<ScrollRect>();
        if(scroll!=null)items.Add(new{kind="scroll",x=x+scroll.viewport.sizeDelta.x+8,y,h=scroll.viewport.sizeDelta.y,contentHeight=scroll.content.sizeDelta.y});
        if(node.GetComponent<RectMask2D>()!=null)clip=new[]{x,y,rect.sizeDelta.x,rect.sizeDelta.y};
        var image=node.GetComponent<Image>();var text=node.GetComponent<Text>();
        if(image!=null&&image.enabled)items.Add(new{kind="image",name=node.gameObject.name,x,y,w=rect.sizeDelta.x,h=rect.sizeDelta.y,sprite=image.sprite?.Name,tiled=image.type==Image.Type.Tiled,multiplier=image.pixelsPerUnitMultiplier,color=new[]{image.color.r,image.color.g,image.color.b,image.color.a},clip});
        if(text!=null&&text.enabled)items.Add(new{kind="text",x,y,w=rect.sizeDelta.x,h=rect.sizeDelta.y,text=text.text,size=text.FontSize,center=text.alignment==TextAnchor.MiddleCenter,top=text.alignment==TextAnchor.UpperLeft,spacing=text.lineSpacing,color=new[]{text.color.r,text.color.g,text.color.b,text.color.a},clip});
        foreach(var child in node.Children)Layout(child,x,y,items,clip);
    }
    static void Main()
    {
        var map=new GameObject("Map"); NativeUi.Canvas=map.AddComponent<Canvas>();
        var overlay=new GameObject("画布");overlay.AddComponent<Canvas>();
        var cursor=new GameObject("光标");cursor.transform.SetParent(overlay.transform,false);
        UnityEngine.Object.Scene[typeof(光标管理器)]=cursor.AddComponent<光标管理器>();
        var db=new GameObject("Database");Plugin.Units=db.AddComponent<dw单位库>();db.AddComponent<cz词缀库>();Plugin.Units.Unit.AddComponent<模块_基本属性>();
        Plugin.Panel=new GameObject("Mod").AddComponent<CoopPanel>();
        for(int i=0;i<40;i++) Plugin.Summary.当前拥有炮台.Add(new 总结脚本.背包单元 { 炮台id=12, 强化上限=10, 强化词条=new List<int>{i,3,3} });
        Plugin.Panel.Open(); var cards=(IList)Field(Plugin.Panel,"cards"); var confirm=NativeUi.Actions.Find(b=>b.Title=="交给队友");
        Check(cards.Count==40,"large inventory creates all cards");
        Check(!confirm.interactable,"confirm disabled until selection");
        Check(overlay.transform.Children[overlay.transform.Children.Count-1]==cursor.transform,"native cursor stays above dialog");
        Check(((GameObject)Field(Plugin.Panel,"canvasRoot")).transform.parent==overlay.transform,"dialog shares native cursor canvas");
        Card(cards,31).onClick.Invoke(); var chosen=Plugin.Summary.当前拥有炮台[31];
        Check(Plugin.Transfers.Requests==0,"card click selects without sending");
        Check(confirm.interactable,"selection enables confirmation");
        Check(((Image)Field(cards[31],"Selection")).enabled,"chosen card has selected corners");
        Check(((Text)Field(Plugin.Panel,"selectedName")).text=="同名鸡械","details show chosen name");
        var layout=new List<object>();Layout((RectTransform)Field(Plugin.Panel,"panelRect"),0,0,layout);
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"layout.json"),JsonSerializer.Serialize(layout));
        var inventory=(RectTransform)Field(Plugin.Panel,"inventory");inventory.anchoredPosition=new Vector2(0,700);
        Plugin.Summary.当前拥有炮台.Reverse(); confirm.onClick.Invoke();
        Check(Plugin.Transfers.Index==8 && Plugin.Transfers.Expected.Upgrades[0]==31,"confirmation follows concrete selected item after reordering");
        var firstButton=Card(cards,0); Plugin.Panel.RefreshInventory();
        Check(Field(Plugin.Panel,"selected")==null && !confirm.interactable,"inventory update clears selection");
        Check(Card(cards,0)==firstButton,"inventory update reuses existing button");
        Check(inventory.anchoredPosition.y==700,"inventory update preserves scroll position");
        Card(cards,2).onClick.Invoke();Card(cards,3).onClick.Invoke();
        Check(!((Image)Field(cards[2],"Selection")).enabled && ((Image)Field(cards[3],"Selection")).enabled,"selection switches only previous and next markers");
        Plugin.Transfers.Locked=true;Plugin.Panel.RefreshState();
        Check(!confirm.interactable && !Card(cards,3).interactable,"pending transfer locks selection and confirmation");
        Plugin.Transfers.Locked=false;Plugin.Panel.RefreshState();
        var old=Plugin.Summary.当前拥有炮台[39];Plugin.Summary.当前拥有炮台.RemoveAt(39);Plugin.Panel.RefreshInventory();
        int requests=Plugin.Transfers.Requests; Card(cards,39).onClick.Invoke();
        Check(Plugin.Transfers.Requests==requests && Field(Plugin.Panel,"selected")==null,"stale removed card cannot become selection");
        Check(!Card(cards,39).gameObject.Active,"removed card is hidden");
        Card(cards,10).onClick.Invoke();Plugin.Summary.当前拥有炮台[10].强化词条[0]=900;
        Card(cards,10).onClick.Invoke();Check(Field(Plugin.Panel,"selected")==null,"changed affix invalidates displayed selection");
        int nameCalls=db.GetComponent<cz词缀库>().Calls;Card(cards,10).onClick.Invoke();Card(cards,11).onClick.Invoke();Card(cards,10).onClick.Invoke();
        Check(db.GetComponent<cz词缀库>().Calls==nameCalls+2,"selected details cache repeated affix names");
        Plugin.CanTrade=false;Plugin.Panel.RefreshState();
        Check(!(bool)Field(Plugin.Panel,"open") && !Plugin.Summary.是否光标位于UI界面,"close restores native input focus");
        Plugin.Summary.当前拥有炮台.Clear();Plugin.Summary.当前金钱数=480;Plugin.Transfers.Status="";
        var names=new[]{"铁甲虫车","圣甲虫炮台","空投甲虫"};var sprites=new[]{"铁甲虫车-50×50_0","圣甲虫炮台_50×50_0","空投甲虫-25x25_0"};
        for(int i=0;i<3;i++) { var unit=new GameObject(names[i]);var attr=unit.AddComponent<模块_基本属性>();attr.Name=names[i];attr.图标=new Sprite(sprites[i]);Plugin.Units.Examples[50+i]=unit; }
        for(int i=0;i<24;i++)Plugin.Summary.当前拥有炮台.Add(new 总结脚本.背包单元{炮台id=50+i%3,强化上限=7,强化词条=new List<int>{2,3,i%5}});
        Plugin.CanTrade=true;Plugin.Panel.RefreshInventory();Plugin.Panel.Open();inventory.anchoredPosition=Vector2.zero;Card(cards,2).onClick.Invoke();
        layout.Clear();Layout((RectTransform)Field(Plugin.Panel,"panelRect"),0,0,layout);File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"layout.json"),JsonSerializer.Serialize(layout));
        var settings = new GameObject("Settings");settings.transform.sizeDelta = new Vector2(1484,924);
        var modules=settings.AddComponent<ModulePanel>(); typeof(ModulePanel).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(modules,null);
        var switches=(IList)Field(modules,"toggles"); var highlights=(IList)Field(modules,"selections");
        Check(switches.Count==10,"control panel builds ten separate module switches");
        ((Toggle)switches[8]).onValueChanged.Invoke(false);
        Check(!ModuleSettings.Preference(ModuleId.SkipSplash) && ModuleSettings.Preference(ModuleId.Speed) && ((Text)Field(modules,"details")).text.Contains("下次启动"),"startup module has its own preference and restart timing description");
        typeof(ModulePanel).GetMethod("Select",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(modules,new object[]{0});
        Check(((Text)Field(modules,"title")).text=="倍速" && ((Text)Field(modules,"details")).text.Contains("左Ctrl"),"initial module details include control key");
        ((Toggle)switches[2]).onValueChanged.Invoke(false);
        Check(!ModuleSettings.Preference(ModuleId.Energy) && ModuleSettings.Preference(ModuleId.Speed),"control panel toggles one independent module");
        Check(((Text)Field(modules,"details")).text.Contains("500") && ((Text)Field(modules,"title")).text=="基地车后备隐藏能源","selection shows concrete effect and amount");
        Check(((Image)highlights[2]).enabled && !((Image)highlights[0]).enabled,"selected module corners follow selection");
        layout.Clear();Layout((RectTransform)Field(modules,"board"),0,0,layout);File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"modules.json"),JsonSerializer.Serialize(layout));
        typeof(ModulePanel).GetMethod("Select",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(modules,new object[]{1});
        Check(((Text)Field(modules,"details")).text.Contains("冷却30秒")&&((Text)Field(modules,"details")).text.Contains("前5秒生命、格挡×2"),"character details show updated dog cooldown and support defense");
        layout.Clear();Layout((RectTransform)Field(modules,"board"),0,0,layout);File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"modules-characters.json"),JsonSerializer.Serialize(layout));
        typeof(ModulePanel).GetMethod("Select",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(modules,new object[]{6});
        Check(((Text)Field(modules,"details")).text.Contains("绿洲最终Boss只出现一次，生命×10、攻击×2"),"expedition details identify final oasis boss multiplier");
        layout.Clear();Layout((RectTransform)Field(modules,"board"),0,0,layout);File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"modules-expedition.json"),JsonSerializer.Serialize(layout));
        Plugin.CanTrade=false;Plugin.Panel.RefreshState();ModuleSettings.LocalProgress.PendingRegion="area-1";
        GrowthPanel.ShowPending();var growth=(GrowthPanel)typeof(GrowthPanel).GetField("active",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
        var growCards=(IList)Field(growth,"cards");var growConfirm=NativeUi.Actions.FindLast(b=>b.Title=="确认");
        Check(growCards.Count==24&&!growConfirm.interactable,"growth selection lists actual inventory and requires a selection");
        Check(overlay.transform.Children[overlay.transform.Children.Count-1]==cursor.transform,"growth window remains beneath native cursor");
        Card(growCards,5).onClick.Invoke();var upgraded=Plugin.Summary.当前拥有炮台[5];int limit=upgraded.强化上限;
        Plugin.Summary.当前拥有炮台.Reverse();growConfirm.onClick.Invoke();
        Check(upgraded.强化上限==limit+1&&ModuleSettings.LocalProgress.Regions.Contains("area-1"),"growth confirmation upgrades concrete selected machine after reorder");
        Check(ModuleSettings.LocalProgress.PendingRegion==null&&!Plugin.Summary.是否光标位于UI界面,"growth claim clears pending reward and restores native input");
        ModuleSettings.LocalProgress.PendingRegion="area-2";GrowthPanel.ShowPending();growth=(GrowthPanel)typeof(GrowthPanel).GetField("active",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);growCards=(IList)Field(growth,"cards");
        var reusable=Card(growCards,0);Card(growCards,0).onClick.Invoke();Plugin.Summary.当前拥有炮台[0].强化词条.Add(99);GrowthPanel.InventoryChanged();
        Check(Card(growCards,0)==reusable&&Field(growth,"selected")==null,"growth inventory update reuses cards and clears stale selection");
        var spawner=new 刷怪器();spawner.数据.波次数据.Add(new 刷怪器.Stage { 组次数据 = new List<刷怪器.Group>{new 刷怪器.Group{单位=new List<GameObject>{new GameObject("王_飘逸海草")}}}});GrowthFeatures.Inspect(spawner);
        Check(GrowthFeatures.Region==null,"ordinary boss does not award region upgrade");
        spawner.数据.波次数据[0].组次数据[0].单位[0]=new GameObject("王_水果海盗团");GrowthFeatures.Inspect(spawner);
        Check(GrowthFeatures.Region=="1:area","region boss encounter is identified from native boss prefab");
        spawner.数据.是否最终关卡=true;GrowthFeatures.Inspect(spawner);Check(GrowthFeatures.Region==null,"final oasis completion does not offer next-region upgrade");GrowthPanel.Close();
        var shopObject=new GameObject("Shop");shopObject.transform.SetParent(overlay.transform,false);var shop=shopObject.AddComponent<商店脚本>();shop.商品背包类=new GameObject("Stock bag");shop.商品背包类.transform.SetParent(shopObject.transform,false);shop.商品背包类.AddComponent<背包类预制体脚本>();
        var viewport=new GameObject("Viewport");viewport.transform.sizeDelta=new Vector2(1000,250);viewport.transform.SetParent(shop.商品背包类.transform,false);var gridObject=new GameObject("Grid");gridObject.transform.SetParent(viewport.transform,false);gridObject.AddComponent<GridLayoutGroup>();
        for(int i=1;i<=3;i++)new GameObject("商品"+i).transform.SetParent(shopObject.transform,false);
        for(int i=100;i<110;i++)Plugin.Units.单位列表.Add(new dw单位库.单位单元{id=i});
        ModuleSettings.Extra.Shop=true;Plugin.Summary.当前金钱数=500;var feature=ShopFeatures.For(shop);
        void Reroll(){typeof(PaidShopRefresh).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{shop});feature.PrepareStock();for(int i=0;i<3;i++)shop.当前商品[i]=new Vector3Int(100+i,60,0);shop.商品背包类.GetComponent<背包类预制体脚本>().更新背包(shop.当前商品.Length);feature.StockReady(false);}
        Reroll();Reroll();Check(shop.当前商品.Length==3,"two paid shop rerolls retain three slots");Reroll();Check(shop.当前商品.Length==4&&shop.当前商品[3].x==103&&shop.当前商品[3].y==48,"third reroll adds distinct fourth offer and native price modifier");
        Plugin.Summary.当前金钱数=0;int rerolls=feature.State.Refreshes;typeof(PaidShopRefresh).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{shop});Check(feature.State.Refreshes==rerolls,"failed paid refresh does not advance four-slot threshold");
        var stock=shop.商品背包类.GetComponent<背包类预制体脚本>();stock.当前已选单元.Add(3);Plugin.Summary.当前金钱数=100;object[] purchase={shop,null};typeof(ShopPurchase).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,purchase);typeof(ShopPurchase).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,purchase);
        Check(!feature.State.Sold[3],"skipped native purchase leaves stock available despite sufficient gold");
        Plugin.Summary.当前金钱数-=shop.当前商品[3].y;
        typeof(ShopPurchase).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,purchase);
        Check(!feature.State.Sold[3],"deduction without granting machine cannot sell out stock");
        Plugin.Summary.当前拥有炮台.Add(new 总结脚本.背包单元{炮台id=shop.当前商品[3].x});
        typeof(ShopPurchase).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,purchase);
        Check(feature.State.Sold[3],"fourth-slot purchase persists sold state");purchase=new object[]{shop,null};Check(!(bool)typeof(ShopPurchase).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,purchase),"sold fourth slot cannot be purchased twice");
        stock.当前已选单元[0]=99;purchase=new object[]{shop,null};
        Check(!(bool)typeof(ShopPurchase).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,purchase),"stale out-of-range shop selection is rejected safely");
        stock.当前已选单元[0]=-1;purchase=new object[]{shop,null};
        Check(!(bool)typeof(ShopPurchase).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,purchase),"negative shop selection is rejected safely");
        stock.当前已选单元[0]=0;Plugin.Summary.当前金钱数=0;purchase=new object[]{shop,null};
        typeof(ShopPurchase).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,purchase);
        typeof(ShopPurchase).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,purchase);
        Check(!feature.State.Sold[0],"insufficient gold cannot persist sold marker");
        shop.当前商品[0]=new Vector3Int(100,0,0);purchase=new object[]{shop,null};
        typeof(ShopPurchase).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,purchase);
        Plugin.Summary.当前拥有炮台.Add(new 总结脚本.背包单元{炮台id=100});
        typeof(ShopPurchase).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,purchase);
        Check(feature.State.Sold[0],"free native purchase persists sold marker after granting machine");
        stock.更新背包(4);feature.StockReady(true);Check(!stock.单元列表[3].Active&&shop.当前商品[3].x==103,"restored shop retains fourth offer and hides purchased card");
        Plugin.Summary.当前金钱数=100;feature.BuildCoupon();NativeUi.Actions.FindLast(b=>b.Title=="工匠刷新券  30金币").onClick.Invoke();Check(Plugin.Summary.当前金钱数==70&&ModuleSettings.LocalProgress.Coupons==3&&feature.State.TicketBought,"coupon purchase deducts thirty and credits three rerolls");
        NativeUi.Actions.FindLast(b=>b.Title=="工匠刷新券  30金币").onClick.Invoke();Check(Plugin.Summary.当前金钱数==70&&ModuleSettings.LocalProgress.Coupons==3,"same shop cannot sell coupon pack twice");
        Plugin.Summary.地图存档.是否位于工匠=true;var artisan=new 选择词条流程脚本();artisan.选择词条.词条剩余刷新次数=0;Check((bool)typeof(ArtisanCouponRefresh).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{artisan})&&ModuleSettings.LocalProgress.Coupons==2&&artisan.选择词条.词条剩余刷新次数==1,"artisan credits native refresh only after consuming one coupon");
        artisan.选择词条.词条剩余刷新次数=0;ArtisanCouponUi.Refresh(artisan);Check(artisan.刷新按钮.interactable&&artisan.剩余刷新次数文本.text=="2","artisan button displays available retained coupon balance");
        Console.WriteLine($"PASS: {checks} production transfer UI behavior checks with code-level Unity objects");
    }
}
