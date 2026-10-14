using System;
using System.Collections.Generic;
using System.Reflection;
using CluckmechCoop;
using Newtonsoft.Json;

namespace UnityEngine
{
    public class MonoBehaviour : Object { }
    public static class Time { public static float time; public static float timeScale { get => Harness.Current.TimeScale; set => Harness.Current.TimeScale = value; } }
    public static class Application { public static bool isFocused = true; }
    public class WaitForSeconds { public WaitForSeconds(float value) { } }
    public struct Vector3 { public static Vector3 zero => new Vector3(); }
    public class Transform { public Vector3 localPosition; public Transform parent; }
    public enum ParticleSystemStopBehavior { StopEmittingAndClear }
    public enum ParticleSystemStopAction { None }
    public enum ParticleSystemSimulationSpace { Local }
    public class ParticleSystem
    {
        public class Main { public bool loop; public ParticleSystemStopAction stopAction; public ParticleSystemSimulationSpace simulationSpace; }
        public Main main=new Main(); public bool Playing;
        public void Stop(bool children,ParticleSystemStopBehavior action){Playing=false;}
        public void Play(){Playing=true;}
    }
}
public class 音效_播放音效 { public bool enabled=true; }
public class 效果物体引用器
{
    public class Effects { public UnityEngine.GameObject 通用提升效果_静音版=new UnityEngine.GameObject(); }
    public Effects 特效=new Effects();
}
namespace UnityEngine.EventSystems
{
    public class EventSystem { public static EventSystem current; public UnityEngine.GameObject currentSelectedGameObject; }
}
namespace UnityEngine.UI { public class InputField { } }
namespace UnityEngine.InputSystem
{
    public class TestKey { public bool isPressed, wasPressedThisFrame; }
    public class Keyboard { public static Keyboard current = new Keyboard(); public TestKey leftCtrlKey = new TestKey(), fKey = new TestKey(); }
}
public class 设置管理器 { }
public class yy引用实例数据库 { public UnityEngine.GameObject 基地车; }
public class 模块_基本属性
{
    public UnityEngine.Transform transform=new UnityEngine.Transform();
    public enum 单位定位枚举 { 基地 }
    public List<单位定位枚举> 单位定位列表 = new List<单位定位枚举>();
    public float 当前生命值 = 3000, 当前抗性, 抗性;
    public 模块_攻击属性 Attack = new 模块_攻击属性();
    public T GetComponent<T>() => (T)(object)Attack;
    public void 运算抗性() { 当前抗性 = 抗性; ExpansionHarness.Call(typeof(EnergyResistance), "Postfix", this); }
}
public class 模块_攻击属性
{
    public float 当前攻击力, 攻击力 = 10;
    public void 运算攻击力() { 当前攻击力 = 攻击力; ExpansionHarness.Call(typeof(ExtraAttack), "Postfix", this); }
}
public class 模块_移动属性 { public float 当前移动速度; }
namespace CluckmechCoop
{
    internal static class RunSaveRepair { internal static bool Blocked; internal static bool IsRunSlot(存档脚本 save) => save.文件名 == "danju" || save.文件名 == "lianji"; internal static bool CanSave(存档脚本 save) => true; }
    public static class UnitModifiers { internal static void Attack(模块_攻击属性 value) { } internal static void Move(模块_移动属性 value) { } }
    internal static class CharacterFeatures { internal static void ReceiveAim(int role, SessionMessage message) { } }
    internal static class GrowthFeatures { internal static void Offer(string region) { ModuleSettings.LocalProgress.OfferRegion(region); } }
}

static class ExpansionHarness
{
    internal static void Call(Type type, string method, params object[] args) => type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Invoke(null, args);
    private static void Update(CombatFeatures combat) => typeof(CombatFeatures).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(combat, null);
    internal static void Run(Action<bool, string> check)
    {
        using (Harness.Use(new Context(0)))
        {
            ModuleSettings.MainMenu(); ModuleSettings.EnterRun();
            check(ModuleCatalog.All.Length == 10 && ModuleSettings.Extra.Speed && ModuleSettings.Extra.Characters && ModuleSettings.Extra.Energy && ModuleSettings.Extra.Growth && ModuleSettings.Extra.Shop && ModuleSettings.Extra.Stratagems && ModuleSettings.Music && ModuleSettings.Preference(ModuleId.SkipSplash), "ten modules start enabled");
            ModuleSettings.Preference(ModuleId.Speed, false); ModuleSettings.EnterRun();
            check(ModuleSettings.Extra.Speed, "new module preferences do not mutate active run");
            foreach (var module in ModuleCatalog.All)
            {
                ModuleSettings.MainMenu();
                foreach (var choice in ModuleCatalog.All) ModuleSettings.Preference(choice.Id, true);
                ModuleSettings.Preference(module.Id, false); ModuleSettings.EnterRun();
                bool[] actual = { ModuleSettings.Extra.Speed, ModuleSettings.Extra.Characters, ModuleSettings.Extra.Energy, ModuleSettings.Music, ModuleSettings.Extra.Growth, ModuleSettings.Extra.Shop, ModuleSettings.Expedition, ModuleSettings.Transfer, ModuleSettings.Preference(ModuleId.SkipSplash), ModuleSettings.Extra.Stratagems };
                bool independent = true;
                for (int i = 0; i < actual.Length; i++) independent &= actual[i] == (i != (int)module.Id);
                check(independent, "each of ten module switches changes only its own state");
            }
            ModuleSettings.MainMenu(); ModuleSettings.Preference(ModuleId.Speed, false); ModuleSettings.EnterRun();
            var progress = ModuleSettings.LocalProgress;
            check(progress.OfferRegion("region-1") && !progress.OfferRegion("region-1"), "region reward offers once");
            check(progress.ClaimRegion() && !progress.ClaimRegion() && !progress.OfferRegion("region-1"), "region reward can be claimed once");
            check(!progress.BuyCoupons("shop-1", 29) && progress.Coupons == 0, "insufficient gold cannot buy coupons");
            check(progress.BuyCoupons("shop-1", 30) && progress.Coupons == 1 && !progress.BuyCoupons("shop-1", 100), "one coupon pack per shop");
            check(!progress.SpendCoupon(1) && progress.Coupons == 1, "free reroll consumed first");
            check(progress.SpendCoupon(0) && progress.Coupons == 0, "extra reroll consumes exactly one coupon");
            check(progress.ClaimRepair("page:repair-1") && !progress.ClaimRepair("page:repair-1") && progress.ClaimRepair("page:repair-2") && progress.Coupons == 2, "each repair node credits exactly one retained reroll");
            var shop = progress.ShopAt("shop-1"); shop.Refreshed(); shop.Refreshed();
            check(!shop.Expanded, "first two shop rerolls show three offers");
            shop.Refreshed(); shop.FourthId = 24; shop.FourthPrice = 80; shop.Sold[3] = true;
            check(shop.Expanded && !progress.ShopAt("shop-2").Expanded, "third reroll expands only current shop");
            var saved = JsonConvert.DeserializeObject<PlayerProgress>(JsonConvert.SerializeObject(progress));
            check(saved.Coupons == 2 && saved.Regions.Contains("region-1") && saved.ShopAt("shop-1").FourthId == 24 && saved.ShopAt("shop-1").FourthPrice == 80 && saved.ShopAt("shop-1").Sold[3], "growth and four-slot shop state survive serialization");
            shop.Refreshed(); check(shop.Expanded && !shop.Sold[3] && shop.FourthId == -1, "next reroll replenishes stock and sold markers");
            ModuleSettings.Progress.UseEnergy(); ModuleSettings.Save(Harness.Current.Save); ModuleSettings.MainMenu(); ModuleSettings.Load(Harness.Current.Save);
            check(ModuleSettings.Progress.EnergyUsed && ModuleSettings.LocalProgress.Coupons == 2, "continue preserves shared energy consumption and coupons");
            check(!ModuleSettings.Extra.Speed, "continue reads chosen main-menu module preferences");
        }
        ModuleSettings.Expedition = false; ModuleSettings.Extra = new GameplayModules { Speed = true, Energy = true, Growth = true };
        ModuleSettings.Progress = new RunProgress(); Plugin.Combat.ClearScene(); Plugin.Combat.Token = "battle-1";
        var hp = new 模块_基本属性(); Plugin.Combat.BindBase(hp);
        using (Harness.Use(Harness.Guest)) Plugin.Combat.BindBase(new 模块_基本属性());
        Plugin.Network.PublishSettings();
        Plugin.Network.Send(new SessionMessage { Type = "speed-request", Token = "battle-1", Held = true });
        check(UnityEngine.Time.timeScale == 2 && Harness.Guest.TimeScale == 2, "host hold accelerates both peers through production protocol");
        using (Harness.Use(Harness.Guest)) Plugin.Network.Send(new SessionMessage { Type = "speed-request", Token = "battle-1", Held = true });
        Plugin.Network.Send(new SessionMessage { Type = "speed-request", Token = "battle-1", Held = false });
        check(UnityEngine.Time.timeScale == 2 && Harness.Guest.TimeScale == 2, "one peer release retains speed while other holds");
        using (Harness.Use(Harness.Guest)) Plugin.Network.Send(new SessionMessage { Type = "speed-request", Token = "battle-1", Held = false });
        check(UnityEngine.Time.timeScale == 1 && Harness.Guest.TimeScale == 1, "both releases restore both peers");
        Plugin.Network.Send(new SessionMessage { Type = "speed-request", Token = "previous-battle", Held = true });
        check(UnityEngine.Time.timeScale == 1, "late hold from old battle ignored");
        UnityEngine.Time.timeScale = 0; Plugin.Combat.SetSpeed(true); Plugin.Combat.SetSpeed(false);
        check(UnityEngine.Time.timeScale == 0, "release never cancels native pause");
        UnityEngine.Time.timeScale = .2f; Plugin.Combat.SetSpeed(true); Plugin.Combat.SetSpeed(false);
        check(UnityEngine.Time.timeScale == .2f, "release preserves construction slow motion");
        UnityEngine.Time.timeScale = 1; UnityEngine.Time.time = 0;
        using (Harness.Use(Harness.Guest)) Plugin.Network.Send(new SessionMessage { Type = "energy-request", Token = "battle-1" });
        Plugin.Network.Send(new SessionMessage { Type = "energy-request", Token = "battle-1" });
        check(ModuleSettings.Progress.EnergyUsed && hp.当前抗性 == 1 && hp.Attack.当前攻击力 == 510 && Plugin.Combat.EnergyActive, "simultaneous energy requests apply one activation and native attribute hooks");
        var energyVisual=Plugin.Combat.EnergyEffect;
        check(energyVisual!=null&&energyVisual.transform.parent==hp.transform&&energyVisual.GetComponent<UnityEngine.ParticleSystem>().main.loop&&energyVisual.GetComponent<UnityEngine.ParticleSystem>().Playing&&!energyVisual.GetComponent<音效_播放音效>().enabled,"energy uses looping native particles attached to base");
        using(Harness.Use(Harness.Guest)) check(Plugin.Combat.EnergyEffect!=null,"energy broadcast creates native effect on guest too");
        Plugin.Combat.ApplyEnergy(10);check(ReferenceEquals(energyVisual,Plugin.Combat.EnergyEffect),"state synchronization reuses existing effect");
        hp.抗性 = .25f; hp.Attack.攻击力 += 25; hp.运算抗性(); hp.Attack.运算攻击力();
        check(hp.当前抗性 == 1 && hp.Attack.当前攻击力 == 535, "original stat recalculation retains energy and intervening upgrades");
        UnityEngine.Time.time = 9.99f; Update(Plugin.Combat); check(Plugin.Combat.EnergyActive, "energy lasts ten game seconds");
        UnityEngine.Time.time = 10; Update(Plugin.Combat);
        check(!Plugin.Combat.EnergyActive && hp.当前抗性 == .25f && hp.Attack.当前攻击力 == 35, "energy expiry removes own bonus and preserves other upgrades");
        check(energyVisual.Destroyed&&Plugin.Combat.EnergyEffect==null,"energy expiry destroys particles with effect");
        using(Harness.Use(Harness.Guest)) check(Plugin.Combat.EnergyEffect==null,"expiry synchronization removes guest effect");
        check(!Plugin.Combat.StartEnergy(), "expired energy cannot recharge within run");
        ModuleSettings.Save(Harness.Host.Save); ModuleSettings.MainMenu(); ModuleSettings.Load(Harness.Host.Save);
        check(ModuleSettings.Progress.EnergyUsed, "shared energy remains spent after save and continue");
        ModuleSettings.Extra.Speed = true; UnityEngine.Time.timeScale = 1;
        UnityEngine.InputSystem.Keyboard.current.leftCtrlKey.isPressed = true; Update(Plugin.Combat);
        check(UnityEngine.Time.timeScale == 2, "input press calls speed protocol");
        typeof(CombatFeatures).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Plugin.Combat, new object[] { false });
        check(UnityEngine.Time.timeScale == 1 && Harness.Guest.TimeScale == 1, "focus loss sends release and restores both peers");
        UnityEngine.InputSystem.Keyboard.current.leftCtrlKey.isPressed = false;
        using (Harness.Use(Harness.Guest))
        {
            ModuleSettings.LocalProgress.Coupons = 7; Plugin.Network.SendProgress(ModuleSettings.LocalProgress);
        }
        check(ModuleSettings.Progress.Player(2).Coupons == 7, "guest progression updates corresponding saved player only");
    }
}
