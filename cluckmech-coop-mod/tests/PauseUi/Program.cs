using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CluckmechCoop;

internal static class Program
{
    private static int checks;
    private static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
    private static bool Refresh(UI_暂停菜单信息显示 ui,int? id)
        =>(bool)typeof(OtherworldPauseCharacter).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static)
            .Invoke(null,new object[]{ui,id.HasValue?new 总结脚本{单局统计数据=new Run{使用角色=id.Value}}:null});
    private static void Main()
    {
        var ui=new UI_暂停菜单信息显示{角色=Enumerable.Range(0,14).Select(_=>new Portrait{active=true}).ToArray(),角色名字UI=new Label()};
        Check(!Refresh(ui,12),"custom character skips native localization");
        Check(ui.角色名字UI.text=="佩丽卡","Perlica catalog name");
        Check(ui.角色.Where((p,i)=>p.active!=(i==12)).Count()==0,"only Perlica portrait active");
        Check(!Refresh(ui,13)&&ui.角色名字UI.text=="宇智波带土","Obito refresh replaces name");
        Check(ui.角色.Where((p,i)=>p.active!=(i==13)).Count()==0,"only Obito portrait active");
        Check(Refresh(ui,0),"vanilla localization still runs");
        Check(ui.角色[0].active&&!ui.角色[12].active&&!ui.角色[13].active,"returning to vanilla hides custom portraits");
        ui.角色[2]=null;Check(Refresh(ui,2),"missing native portrait is tolerated");
        ui.角色=null;ui.角色名字UI=null;Check(!Refresh(ui,12),"missing display references are tolerated");
        Check(Refresh(ui,null),"missing summary falls through to native handling");
        var settlement=new UI_结算场景信息显示{角色=Enumerable.Range(0,14).Select(_=>new Portrait{active=true}).ToArray(),角色名字UI=new Label()};
        var settlementRefresh=typeof(OtherworldSettlementCharacter).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static);
        bool Settlement(int id)=>(bool)settlementRefresh.Invoke(null,new object[]{settlement,new 总结脚本{单局统计数据=new Run{使用角色=id}}});
        Check(!Settlement(13)&&settlement.角色名字UI.text=="宇智波带土","settlement uses catalog name rather than missing localization key");
        Check(settlement.角色.Where((p,i)=>p.active!=(i==13)).Count()==0,"settlement activates only Obito");
        Check(!Settlement(12)&&settlement.角色名字UI.text=="佩丽卡","settlement refresh switches custom character");
        Check(Settlement(0)&&settlement.角色[0].active&&!settlement.角色[12].active&&!settlement.角色[13].active,"settlement preserves native character handling and clears reused portraits");
        var visibility=typeof(MapBoonsPauseVisibility).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static);
        visibility.Invoke(null,new object[]{new 暂停菜单{是否开启=true}});
        Check(MapBoonsUi.Paused,"open event hides map card, including co-op where time continues");
        visibility.Invoke(null,new object[]{new 暂停菜单{是否开启=false}});
        Check(!MapBoonsUi.Paused,"close event restores map card");
        var targets=(IEnumerable<MethodBase>)typeof(MapBoonsPauseVisibility).GetMethod("TargetMethods",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        Check(targets.Select(x=>x.Name).SequenceEqual(new[]{"开启菜单","关闭菜单"}),"both native menu transitions patched");
        Console.WriteLine($"PASS: {checks} pause UI behavior checks (stubbed Unity/native objects; no runtime UI claim)");
    }
}
public sealed class Portrait {public bool active;public void SetActive(bool value)=>active=value;}
public sealed class Label {public string text;}
public sealed class Run {public int 使用角色;}
public sealed class 总结脚本 {public Run 单局统计数据;}
public sealed class UI_暂停菜单信息显示 {public Portrait[] 角色;public Label 角色名字UI;}
public sealed class UI_结算场景信息显示 {public Portrait[] 角色;public Label 角色名字UI;}
public sealed class 暂停菜单 {public bool 是否开启;public void 开启菜单(){}public void 关闭菜单(){}}
namespace CluckmechCoop
{
    internal sealed class OtherworldHero {internal string Name;}
    internal static class OtherworldCatalog
    {internal static OtherworldHero Hero(int id)=>id==12?new OtherworldHero{Name="佩丽卡"}:id==13?new OtherworldHero{Name="宇智波带土"}:null;}
    internal static class MapBoonsUi {internal static bool Paused;internal static void SetPauseVisibility(bool value)=>Paused=value;}
}
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]public sealed class HarmonyPatch:Attribute {public HarmonyPatch(){}public HarmonyPatch(Type t,string n){}}
    public static class AccessTools {public static MethodInfo Method(Type type,string name)=>type.GetMethod(name);}
}
