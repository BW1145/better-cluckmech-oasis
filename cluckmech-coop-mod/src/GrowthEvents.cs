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
        internal string Name, Story, Choice, Result, Leave, Declined;
    }

    internal static class GrowthEvents
    {
        internal static readonly GrowthEvent[] All = {
            new GrowthEvent { Id=88, Layout=50, Name="退休工程狮",
                Story="路边的工程狮正在收拾工坊。他把一叠调试券塞给你：\n“准备退休啦，这些也用不上了。拿去，让那些工匠多给你换几套方案，别看见第一个强化就急着点头！”",
                Choice="收下调试券", Leave="道谢离开",
                Result="工程狮拍了拍空下来的工具箱，满意地锁上工坊。\n你将调试券收好，准备在下一家工匠那里好好试试。\n\n<color=#00FF02>获得20次额外工匠强化刷新，本局保留。</color>",
                Declined="你谢过工程狮的好意，继续赶路。他挥了挥手，又开始整理剩下的工具。" },
            new GrowthEvent { Id=89, Layout=10, Name="多带一件",
                Story="一只工程狮围着你的基地车转了两圈，敲了敲后面的空支架：\n“这么大个地方，空着多可惜！”\n没等你回答，他已经从工具箱里拖出一套折叠挂架。",
                Choice="安装挂架", Leave="保持原样",
                Result="几声敲打过后，折叠挂架牢牢固定在车尾。工程狮拽了两下，确认它足以装下一台鸡械。\n\n<color=#00FF02>本局鸡械携带上限+1，5格→6格。</color>",
                Declined="你看了看熟悉的支架，决定保持原样。工程狮收起工具，祝你一路顺风。" },
            new GrowthEvent { Id=90, Layout=70, Name="电容还没坏",
                Story="你在废料堆里发现一组完整的储能电容。随车检测器亮起绿灯，随后弹出提示：\n“状态良好。建议安装。请勿再次当作饮料冷藏箱使用。”",
                Choice="接入储能系统", Leave="放回废料堆",
                Result="清走电容里最后一个空饮料罐后，你把线路接上了基地车。仪表上的储能刻度向右延伸了一截。\n\n<color=#00FF02>本局战斗动力上限+5，20→25。</color>",
                Declined="你将电容放回原处，检测器熄灭了绿灯。也许下一位路过的人更需要它。" }
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
            if(controller.全事件.Length>=91)return;
            var originals=controller.全事件;
            Array.Resize(ref controller.全事件,91);Array.Resize(ref controller.全事件布局,91);
            foreach(var item in All)
            {
                var root=UnityEngine.Object.Instantiate(originals[0],originals[0].transform.parent);
                root.SetActive(false);root.name=item.Name;
                foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if(script.GetType().Namespace?.StartsWith("UnityEngine.Localization",StringComparison.Ordinal)==true)
                        UnityEngine.Object.DestroyImmediate(script);
                var panel=root.AddComponent<GrowthEventPanel>();panel.Build(item,controller);
                controller.全事件[item.Id]=root;
                var background=UnityEngine.Object.Instantiate(controller.全事件布局[item.Layout],controller.全事件布局[item.Layout].transform.parent);
                background.SetActive(false);background.name=item.Name;
                controller.全事件布局[item.Id]=background;
            }
            if(!ModuleSettings.Extra.Growth || ModuleSettings.Progress.GrowthPoolsAdded)return;
            var events=summary.GetComponent<模块_事件总结>();
            events.沼泽随机=Add(events.沼泽随机,88);events.冰川随机=Add(events.冰川随机,88);
            events.火山随机=Add(events.火山随机,89,90);events.研究所随机=Add(events.研究所随机,89,90);
            ModuleSettings.Progress.GrowthPoolsAdded=true;
        }
        internal static string Reward(int id) => id==88 ? "获得20次额外工匠强化刷新，本局保留" :
            id==89 ? "本局鸡械携带上限+1（5→6）" : "本局战斗动力上限+5（20→25）";
    }

    public sealed class GrowthEventPanel : MonoBehaviour
    {
        private GrowthEvent entry;
        private 模块_事件场景控制 controller;
        private GameObject upper,lower,result;
        private Text resultText;
        internal void Build(GrowthEvent value,模块_事件场景控制 owner)
        {
            entry=value;controller=owner;
            upper=transform.Find("上框").gameObject;lower=transform.Find("下框").gameObject;
            transform.Find("上框/标题").GetComponent<Text>().text=entry.Name;
            transform.Find("上框/内容").GetComponent<Text>().text=entry.Story;
            result=transform.Find("选择反馈").gameObject;result.SetActive(false);
            resultText=result.transform.Find("上框 /内容").GetComponent<Text>();
            foreach(string name in new[]{"选择反馈 (1)","选择反馈 (2)"})
                UnityEngine.Object.DestroyImmediate(transform.Find(name).gameObject);
            var buttons=lower.transform.Find("按钮区域");
            UnityEngine.Object.DestroyImmediate(buttons.Find("按钮选项 (2)").gameObject);
            Bind(buttons.Find("按钮选项").GetComponent<Button>(),0,
                "<b>"+entry.Choice+"</b>\n<size=23><color=#00FF02>"+GrowthEvents.Reward(entry.Id)+"</color></size>",()=>Resolve(true));
            Bind(buttons.Find("按钮选项 (1)").GetComponent<Button>(),1,"<b>"+entry.Leave+"</b>",()=>Resolve(false));
            Bind(result.transform.Find("下框/按钮区域/按钮选项").GetComponent<Button>(),2,"离开",controller.退出事件场景);
            upper.SetActive(true);lower.SetActive(true);
        }
        private void Bind(Button button,int step,string label,UnityAction action)
        {
            var decision=button.GetComponent<联机_决策选择>();
            decision.提交决策值_int=8800+(entry.Id-88)*10+step;
            decision.事件_决策选择时=new UnityEvent();
            decision.事件_决策实现=new UnityEvent();decision.事件_决策实现.AddListener(action);
            decision.事件_决策实现_仅主机端=new UnityEvent();
            button.onClick=new Button.ButtonClickedEvent();button.onClick.AddListener(decision.决策选择);
            button.GetComponentInChildren<Text>(true).text=label;
        }
        private void Resolve(bool accepted)
        {
            if(accepted && ModuleSettings.LocalProgress.ClaimGrowthEvent(entry.Id))
            {
                CarryCapacity.Apply();ModuleSettings.SaveProgress();
                Plugin.Log("[Growth] Event claimed: "+entry.Id);
            }
            resultText.text=accepted?entry.Result:entry.Declined;
            upper.SetActive(false);lower.SetActive(false);result.SetActive(true);
        }
    }
    [HarmonyPatch(typeof(模块_事件场景控制),"Start")]
    internal static class InstallGrowthEvents
    { private static void Prefix(模块_事件场景控制 __instance)=>GrowthEvents.Install(__instance); }
}
