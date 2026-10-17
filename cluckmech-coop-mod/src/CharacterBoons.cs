using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using 函数库;

namespace CluckmechCoop
{
    internal static class CharacterBoons
    {
        internal static bool Started;
        internal static int Rarities(IEnumerable<总结脚本.背包单元> bag)
            => bag==null?0:bag.Select(x=>Plugin.Units.获取单位单元(x.炮台id)?.单位等级??0).Where(x=>x>=1 && x<=6).Distinct().Count();
        internal static int Tier(int rarities,bool energy)
            => (energy?1:0)+(rarities>=2?1:0)+(rarities>=3?1:0)+(rarities>=4?1:0);
        internal static float Health(int tier)=>tier>=4?11:tier==3?6:tier==2?4:tier==1?2:1;
        internal static float Attack(int tier)=>tier>=4?2:tier>=3?1.1f:1;
        internal static float Speed(int tier)=>tier>=4?1.4f:tier>=2?1.05f:1;
        internal static int CurrentTier=>Tier(Rarities(Plugin.Summary?.当前拥有炮台),ModuleSettings.Progress.EnergyRemaining>0);
        internal static int Level(int role)=>ModuleSettings.Extra.Boons && ModuleSettings.Progress.BattleBoons.TryGetValue(role,out int tier)?tier:0;
        internal static void Begin()
        {
            if(Started)return;Started=true;
            if(Plugin.Role!=2)
            {
                ModuleSettings.Progress.BattleBoons.Clear();
                if(ModuleSettings.Extra.Boons)
                {
                    ModuleSettings.Progress.BattleBoons[Plugin.Role]=CurrentTier;
                    if(Plugin.Role==1)
                    {
                        var guest=Steam联机管理器.玩家_客户端?.Network总结数据_联机;
                        ModuleSettings.Progress.BattleBoons[2]=Tier(Rarities(guest?.当前拥有炮台),ModuleSettings.Progress.EnergyRemaining>0);
                    }
                }
                Plugin.Network.PublishSettings();
            }
            Synchronize();
        }
        internal static void Synchronize()
        {
            if(!Started || !ModuleSettings.Extra.Boons)return;
            foreach(var player in UnityEngine.Object.FindObjectsOfType<模块_玩家_死亡>())Attach(player.gameObject);
        }
        internal static void Attach(GameObject unit)
        {
            if(!Started || !ModuleSettings.Extra.Boons || Plugin.Spawner==null)return;
            var hp=unit.GetComponent<模块_基本属性>();
            if(hp==null || !hp.单位定位列表.Contains(模块_基本属性.单位定位枚举.玩家))return;
            var boon=unit.GetComponent<PlayerBoon>()??unit.AddComponent<PlayerBoon>();
            boon.Apply(hp,Level((int)hp.所属玩家));
        }
    }

    public sealed class PlayerBoon:MonoBehaviour
    {
        internal int Tier=-1;
        internal bool Saved;
        internal float ProtectedUntil;
        private 模块_基本属性 health;
        internal void Apply(模块_基本属性 hp,int tier)
        {
            if(Tier==tier)return;
            health=hp;Tier=tier;
            hp.运算生命值上限();
            var attack=GetComponent<模块_攻击属性>();attack?.运算攻击力();attack?.运算攻击速度();
        }
        internal bool Protecting=>ProtectedUntil>Time.time;
        internal bool SaveFromFatal()
        {
            if(Tier<3 || health==null || health.当前生命值>0)return false;
            if(!Protecting)
            {
                if(Saved)return false;
                Saved=true;ProtectedUntil=Time.time+2;
                var display=GetComponent<模块_玩家_受击反馈>();
                if(display!=null && display.玩家身体!=null)
                {
                    var sprite=display.玩家身体.GetComponent<SpriteRenderer>();
                    if(sprite!=null)StartCoroutine(Flash(sprite));
                }
                Plugin.Log("[Boons] Last stand activated for player "+(int)health.所属玩家);
            }
            health.当前生命值=1;return true;
        }
        private System.Collections.IEnumerator Flash(SpriteRenderer sprite)
        {
            // A brief native sprite tint communicates protection without an extra HUD.
            Color original=sprite.color;
            while(Protecting && sprite!=null)
            {sprite.color=Color.Lerp(original,new Color(1,.84f,.25f,original.a),.35f+.25f*Mathf.Sin(Time.time*12));yield return null;}
            if(sprite!=null)sprite.color=original;
        }
    }
    [HarmonyPatch(typeof(模块_玩家_死亡),"Start")]
    internal static class PlayerBoonReady
    {private static void Postfix(模块_玩家_死亡 __instance)=>CharacterBoons.Attach(__instance.gameObject);}
    [HarmonyPatch(typeof(模块_基本属性),"运算生命值上限")]
    internal static class PlayerBoonHealth
    {
        private static void Prefix(模块_基本属性 __instance,out float __state)
        {var boon=__instance.GetComponent<PlayerBoon>();__state=boon==null?1:CharacterBoons.Health(boon.Tier);__instance.生命值倍率*=__state;}
        private static void Postfix(模块_基本属性 __instance,float __state){__instance.生命值倍率/=__state;}
    }
    [HarmonyPatch(typeof(模块_攻击属性),"运算攻击力")]
    internal static class PlayerBoonAttack
    {private static void Postfix(模块_攻击属性 __instance){var boon=__instance.GetComponent<PlayerBoon>();if(boon!=null)__instance.当前攻击力*=CharacterBoons.Attack(boon.Tier);}}
    [HarmonyPatch(typeof(模块_攻击属性),"运算攻击速度")]
    internal static class PlayerBoonSpeed
    {private static void Postfix(模块_攻击属性 __instance){var boon=__instance.GetComponent<PlayerBoon>();if(boon!=null)__instance.当前攻击速度*=CharacterBoons.Speed(boon.Tier);}}
    [HarmonyPatch]
    internal static class PlayerBoonDamage
    {
        private static IEnumerable<MethodBase> TargetMethods()=>AccessTools.GetDeclaredMethods(typeof(模块_受伤)).Where(x=>x.Name=="受到伤害");
        private static bool Prefix(模块_受伤 __instance)=>__instance.GetComponent<PlayerBoon>()?.Protecting!=true;
        private static void Postfix(模块_受伤 __instance)=>__instance.GetComponent<PlayerBoon>()?.SaveFromFatal();
    }
    [HarmonyPatch(typeof(模块_玩家_死亡),"玩家死亡")]
    internal static class PlayerBoonDeath
    {private static bool Prefix(模块_玩家_死亡 __instance)=>__instance.GetComponent<PlayerBoon>()?.SaveFromFatal()!=true;}

    public sealed class MapBoonsUi:MonoBehaviour
    {
        private static MapBoonsUi current;
        private Text summary;
        private Image[] ticks;
        private int previous=-1;
        internal static void Clear(){if(current!=null)Destroy(current.gameObject);current=null;}
        internal static void Attach()
        {
            if(SceneManager.GetActiveScene().buildIndex!=常数类.场景id_选择地图 || !ModuleSettings.Extra.Boons || current!=null)return;
            var canvas=UnityEngine.Object.FindObjectOfType<光标管理器>()?.GetComponentInParent<Canvas>().rootCanvas??UnityEngine.Object.FindObjectOfType<Canvas>()?.rootCanvas;
            if(canvas==null)return;NativeUi.Capture(canvas.transform);
            var root=NativeUi.Rect(canvas.transform,"战备协同",0,0,460,104);
            root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,1);root.anchoredPosition=new Vector2(0,-14);
            current=root.gameObject.AddComponent<MapBoonsUi>();
            var image=NativeUi.Image(root,NativeUi.TransferCard,Color.white);
            var button=root.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(current.Details);
            current.summary=NativeUi.Label(root,"",18,8,424,44,24);
            current.ticks=new Image[4];
            for(int i=0;i<4;i++)current.ticks[i]=NativeUi.Image(NativeUi.Rect(root,"档位",18+i*108,62,100,18),null,Color.gray);
            current.Refresh();FeatureModal.CursorLast(canvas);
        }
        internal static void RefreshCurrent(){if(current!=null)current.Refresh();}
        private void Refresh()
        {
            int tier=CharacterBoons.CurrentTier;if(previous==tier)return;previous=tier;
            summary.text="战备协同　"+tier+"／4　查看增益";
            for(int i=0;i<4;i++)ticks[i].color=i<tier?new Color(1,.75f,.15f):new Color(.25f,.25f,.25f);
        }
        private void Details()
        {
            var dialog=FeatureModal.Open("战备协同",860,670);if(dialog==null)return;
            int rarity=CharacterBoons.Rarities(Plugin.Summary.当前拥有炮台),tier=CharacterBoons.CurrentTier;
            string[] conditions={"剩余后备隐藏能源至少1次","背包持有至少2种评级鸡械","背包持有至少3种评级鸡械","背包持有至少4种评级鸡械"};
            bool[] met={ModuleSettings.Progress.EnergyRemaining>0,rarity>=2,rarity>=3,rarity>=4};
            NativeUi.Label(dialog.Panel,"满足条件："+tier+"／4　　当前持有"+rarity+"种评级",30,86,800,40,25);
            for(int i=0;i<4;i++)
            {var line=NativeUi.Label(dialog.Panel,(met[i]?"● ":"○ ")+conditions[i],34,145+i*54,790,46,25);line.color=met[i]?new Color(1,.8f,.25f):new Color(.6f,.6f,.6f);}
            string bonus="当前角色增益\n生命＋"+(int)((CharacterBoons.Health(tier)-1)*100)+"%　攻速＋"+Mathf.RoundToInt((CharacterBoons.Speed(tier)-1)*100)+"%　攻击＋"+Mathf.RoundToInt((CharacterBoons.Attack(tier)-1)*100)+"%";
            if(tier>=3)bonus+="\n每场一次：致命伤害保留1生命，无敌2秒。";
            NativeUi.Label(dialog.Panel,bonus,34,385,790,140,25);
            NativeUi.Label(dialog.Panel,"四档生命：＋100%／300%／500%／1000%\n增益作用于角色自身，开战时锁定本场档位。",34,550,790,82,23);
        }
        private void OnDestroy(){if(current==this)current=null;}
    }
}
