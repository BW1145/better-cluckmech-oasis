using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CluckmechCoop
{
    internal sealed class GrowthEvent
    {
        internal int Id, Layout;
        internal string Name, Story, Choice, Alternative, Reward, AlternativeReward, Result, AlternativeResult;
    }
    internal static class GrowthEvents
    {
        internal static readonly GrowthEvent[] All = {
            new GrowthEvent { Id=88, Layout=50, Name="退休工程狮",
                Story="退休的工程狮正把调试券分成两叠：\n“这叠十张，拿了就能用。那只封好的工具袋嘛，里面可能有二十张，也可能全是过期收据。你挑一样吧！”",
                Choice="收下十张调试券", Alternative="试试封好的工具袋", Reward="获得10次工匠刷新", AlternativeReward="50%获得20次刷新，50%获得0次",
                Result="你数好十张调试券，装进工具箱。", AlternativeResult="你打开工具袋，仔细翻找了一遍。" },
            new GrowthEvent { Id=89, Layout=10, Name="多带一件",
                Story="工程狮敲了敲基地车后面的空支架：\n“这么大个地方，空着多可惜！”\n他拿来一套折叠挂架，又指了指收购箱：“不装的话，这套零件也能卖一百五。”",
                Choice="安装挂架", Alternative="出售挂架", Reward="本局携带上限5→6", AlternativeReward="获得150金币",
                Result="折叠挂架牢牢固定在车尾，正好多装一台鸡械。", AlternativeResult="工程狮收回挂架，将一袋金币交给你。" },
            new GrowthEvent { Id=90, Layout=70, Name="电容还没坏",
                Story="废料堆里躺着一组完整的储能电容，检测器亮起绿灯。\n“状态良好，建议安装。”\n旁边的回收终端则给出了另一种建议：“完整储能电容，收购价150金币。”",
                Choice="接入储能系统", Alternative="交给回收终端", Reward="本局战斗动力上限20→25", AlternativeReward="获得150金币",
                Result="接上线缆后，仪表上的储能刻度向右延伸了一截。", AlternativeResult="终端收走电容，吐出一袋金币。" },
            new GrowthEvent { Id=91, Layout=50, Name="老零件的新位置",
                Story="工坊墙上贴着一张手写告示：“旧款升级，批量优惠！”\n工匠掏出一盒扩展接头：“C级的结构简单，我能把你包里每台都多接三个槽。要是只改一台，什么型号都行，多接一个。”",
                Choice="批量改装C级鸡械", Alternative="精加工一台鸡械", Reward="背包所有C级鸡械强化上限+3", AlternativeReward="选择任意一台鸡械，强化上限+1",
                Result="工匠逐台装好扩展接头，把余下的零件扫回盒子里。", AlternativeResult="工匠铺开软垫：“挑一台放上来吧。”" },
            new GrowthEvent { Id=92, Layout=70, Name="应急电池箱",
                Story="一只密封箱卡在路边，里面装着两块后备能源电池。\n检测器确认接口匹配，基地车的应急供能装置发出短促的提示音。\n箱盖上还贴着回收标签：“整箱收购，100金币。”",
                Choice="补充后备隐藏能源", Alternative="出售电池箱", Reward="整局共用的后备隐藏能源次数+2", AlternativeReward="获得100金币",
                Result="电池卡入备用插槽，基地车多了两次启动后备隐藏能源的机会。", AlternativeResult="你将密封箱交给回收商，收下100金币。" }
        };
        private static int[] Add(int[] pool,params int[] ids)
        {
            var result=new List<int>(pool);
            foreach(int id in ids)if(!result.Contains(id))result.Add(id);
            return result.ToArray();
        }
        internal static void Install(模块_事件场景控制 controller)
        {
            var summary=GameObject.Find("总结").GetComponent<总结脚本>();
            Plugin.BindSummary(summary);ModuleSettings.EnterRun();
            if(controller.全事件.Length>=93)return;
            var originals=controller.全事件;
            Array.Resize(ref controller.全事件,93);Array.Resize(ref controller.全事件布局,93);
            foreach(var item in All)
            {
                var root=UnityEngine.Object.Instantiate(originals[0],originals[0].transform.parent);
                root.SetActive(false);root.name=item.Name;
                foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if(script.GetType().Namespace?.StartsWith("UnityEngine.Localization",StringComparison.Ordinal)==true)
                        UnityEngine.Object.DestroyImmediate(script);
                root.AddComponent<GrowthEventPanel>().Build(item,controller);
                controller.全事件[item.Id]=root;
                var background=UnityEngine.Object.Instantiate(controller.全事件布局[item.Layout],controller.全事件布局[item.Layout].transform.parent);
                background.SetActive(false);background.name=item.Name;controller.全事件布局[item.Id]=background;
            }
            if(!ModuleSettings.Extra.Growth)return;
            var events=summary.GetComponent<模块_事件总结>();
            if(!ModuleSettings.Progress.GrowthPoolsAdded)
            {
                events.沼泽随机=Add(events.沼泽随机,88);events.冰川随机=Add(events.冰川随机,88);
                events.火山随机=Add(events.火山随机,89,90);events.研究所随机=Add(events.研究所随机,89,90);
                ModuleSettings.Progress.GrowthPoolsAdded=true;
            }
            if(ModuleSettings.Progress.GrowthEventVersion<2)
            {
                events.沼泽随机=Add(events.沼泽随机,91,92);events.冰川随机=Add(events.冰川随机,91,92);
                ModuleSettings.Progress.GrowthEventVersion=2;
            }
        }
    }
    public sealed class GrowthEventPanel : MonoBehaviour
    {
        private GrowthEvent entry;
        private 模块_事件场景控制 controller;
        private GameObject upper,lower,result;
        private Text resultText;
        private bool resolved;
        internal void Build(GrowthEvent value,模块_事件场景控制 owner)
        {
            entry=value;controller=owner;
            upper=transform.Find("上框").gameObject;lower=transform.Find("下框").gameObject;
            transform.Find("上框/标题").GetComponent<Text>().text=entry.Name;
            transform.Find("上框/内容").GetComponent<Text>().text=entry.Story;
            result=transform.Find("选择反馈").gameObject;result.SetActive(false);
            resultText=result.transform.Find("上框 /内容").GetComponent<Text>();
            foreach(string name in new[]{"选择反馈 (1)","选择反馈 (2)"})UnityEngine.Object.DestroyImmediate(transform.Find(name).gameObject);
            var buttons=lower.transform.Find("按钮区域");
            UnityEngine.Object.DestroyImmediate(buttons.Find("按钮选项 (2)").gameObject);
            Bind(buttons.Find("按钮选项").GetComponent<Button>(),0,Label(entry.Choice,entry.Reward),()=>Resolve(0));
            Bind(buttons.Find("按钮选项 (1)").GetComponent<Button>(),1,Label(entry.Alternative,entry.AlternativeReward),()=>Resolve(1));
            Bind(result.transform.Find("下框/按钮区域/按钮选项").GetComponent<Button>(),2,"继续",controller.退出事件场景,false);
            upper.SetActive(true);lower.SetActive(true);
        }
        private static string Label(string choice,string reward)=>"<b>"+choice+"</b>\n<size=23><color=#00FF02>"+reward+"</color></size>";
        private void Bind(Button button,int step,string label,UnityAction action,bool hostOnly=true)
        {
            var decision=button.GetComponent<联机_决策选择>();decision.提交决策值_int=8800+(entry.Id-88)*10+step;
            decision.事件_决策选择时=new UnityEvent();decision.事件_决策实现=new UnityEvent();decision.事件_决策实现_仅主机端=new UnityEvent();
            (hostOnly?decision.事件_决策实现_仅主机端:decision.事件_决策实现).AddListener(action);
            button.onClick=new Button.ButtonClickedEvent();button.onClick.AddListener(decision.决策选择);
            button.GetComponentInChildren<Text>(true).text=label;
        }
        private void Resolve(int choice)
        {
            if(resolved)return;
            int roll=entry.Id==88 && choice==1 && UnityEngine.Random.Range(0,2)==1?20:0;
            ApplyResult(choice,roll);
            Plugin.Network.Broadcast(new SessionMessage{Type="growth-event",Token=ModuleSettings.NodeKey,Stage=entry.Id,Revision=choice,Role=roll});
        }
        internal static void Receive(SessionMessage message)
        {
            var panel=UnityEngine.Object.FindObjectOfType<GrowthEventPanel>();
            if(panel!=null && panel.entry.Id==message.Stage && ModuleSettings.NodeKey==message.Token)panel.ApplyResult(message.Revision,message.Role);
        }
        private void ApplyResult(int choice,int roll)
        {
            if(resolved)return;resolved=true;
            if(ModuleSettings.LocalProgress.ClaimGrowthEvent(entry.Id,choice,roll))
            {
                int gold=choice==1 && (entry.Id==89 || entry.Id==90)?150:choice==1 && entry.Id==92?100:0;
                if(gold>0)Plugin.Summary.获得金钱(gold);
                if(entry.Id==91 && choice==0)
                    foreach(var machine in Plugin.Summary.当前拥有炮台)
                        if(Plugin.Units.获取单位单元(machine.炮台id).单位等级==1)machine.强化上限+=3;
                if(entry.Id==92 && choice==0)ModuleSettings.Progress.EnergyBonus=2;
                CarryCapacity.Apply();
                if(Plugin.Role!=0)Plugin.LocalPlayer?.上传玩家总结数据();
                ModuleSettings.SaveProgress();
                Plugin.Log("[Growth] Event claimed: "+entry.Id+"; choice "+choice+"; lottery "+roll);
            }
            string reward=choice==0?entry.Reward:entry.AlternativeReward;
            if(entry.Id==88 && choice==1)reward="获得"+roll+"次工匠刷新";
            resultText.text=(choice==0?entry.Result:entry.AlternativeResult)+"\n\n<color=#00FF02>"+reward+"。</color>";
            upper.SetActive(false);lower.SetActive(false);result.SetActive(true);
        }
    }
    [HarmonyPatch(typeof(模块_事件场景控制),"Start")]
    internal static class InstallGrowthEvents
    { private static void Prefix(模块_事件场景控制 __instance)=>GrowthEvents.Install(__instance); }
}