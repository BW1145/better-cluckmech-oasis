using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CluckmechCoop
{
    public sealed class ShopRewards:MonoBehaviour
    {
        private static ShopRewards current;
        internal static ShopRewards Current=>current;
        internal 商店脚本 Shop;
        private Button launcher;
        private Text progress;
        internal static void Attach(商店脚本 shop)
        {
            if(!ModuleSettings.Extra.Shop)return;
            current=shop.GetComponent<ShopRewards>()??shop.gameObject.AddComponent<ShopRewards>();current.Shop=shop;
            NativeUi.Capture(shop.transform);
            current.launcher=NativeUi.TransferAction(shop.transform,"商店回馈",0,0,320,66,current.Show);
            var rect=current.launcher.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-32,252);
            current.progress=NativeUi.Label(shop.transform,"",0,0,340,54,21);
            rect=current.progress.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-32,326);
            Refresh();
            ShopRewardNetwork.ApplyPending();
        }
        internal static void Spent(int amount,bool refresh=false)
        {
            if(!ModuleSettings.Extra.Shop || amount<=0)return;
            ModuleSettings.LocalProgress.ShopSpent(amount,refresh);Refresh();
        }
        internal static void Save()
        {
            if(Plugin.Role!=0)Plugin.LocalPlayer?.上传玩家总结数据();
            ModuleSettings.SaveProgress();if(Plugin.Role==2)Plugin.Summary.保存单局存档();Refresh();MapBoonsUi.RefreshCurrent();
        }
        internal static void Refresh()
        {
            if(current==null || current.progress==null)return;var p=ModuleSettings.LocalProgress;
            current.progress.text="轮盘进度 "+p.ShopRefreshCount%ShopRewardRules.SpinRefreshes+"／5　自选 "+(p.ShopNormalPickEarned?"已获得":Mathf.Min(p.ShopRefreshCount,15)+"／15");
            current.launcher.GetComponentInChildren<Text>().text="商店回馈　"+(p.ShopPicks+p.ShopSpins)+"次待领取";
        }
        internal List<int> Pool(bool abc=false)
        {
            int maximum=Shop.商品单位等级+Mathf.Max(0,Shop.第三栏超模额外等级);
            if(abc)maximum=Mathf.Min(3,maximum);
            return Plugin.Units.单位列表.Where(x=>x.单位等级>=1 && x.单位等级<=maximum && x.是否商店内出现 && x.是否游戏内出现 && Plugin.Units.是否抽取(x.id)).Select(x=>x.id).Distinct().ToList();
        }
        private void Show()
        {
            var modal=FeatureModal.Open("商店回馈",1040,820);if(modal==null)return;
            modal.gameObject.AddComponent<ShopRewardsView>().Build(this,modal);
        }
        private void OnDestroy(){if(current==this)current=null;}
    }
    public sealed class ShopRewardsView:MonoBehaviour
    {
        private ShopRewards source;
        private FeatureModal modal;
        private RectTransform body;
        private bool spinning;
        private int selected;
        private Image marker;
        internal void Build(ShopRewards shop,FeatureModal dialog){source=shop;modal=dialog;Home();}
        private void Clear()
        {
            if(body!=null){body.gameObject.SetActive(false);Destroy(body.gameObject);}
            body=NativeUi.Rect(modal.Panel,"内容",24,86,992,704);selected=0;marker=null;
        }
        private void Home()
        {
            Clear();var p=ModuleSettings.LocalProgress;
            NativeUi.Label(body,"本局付费刷新 "+p.ShopRefreshCount+"次　轮盘进度 "+p.ShopRefreshCount%5+"／5",10,0,972,48,25);
            NativeUi.Label(body,"每5次付费刷新获得轮盘；累计15次获得自选鸡械，常规自选整局一次。",10,54,972,42,23);
            var pick=NativeUi.TransferAction(body,"自选鸡械　剩余"+p.ShopPicks+"次",10,118,460,68,Pick);pick.interactable=p.ShopPicks>0;
            var spin=NativeUi.TransferAction(body,"奖励轮盘　剩余"+p.ShopSpins+"次",500,118,460,68,Wheel);spin.interactable=p.ShopSpins>0;
            var capacity=NativeUi.TransferAction(body,"选择强化上限＋1　剩余"+p.MachineUpgradeCredits+"次",10,204,950,56,OpenCapacity);
            capacity.interactable=p.MachineUpgradeCredits>0;
            NativeUi.Label(body,"随机奖励为本店允许且已解锁的ABC鸡械；自选使用本店合法范围。\n刷新进度与待领取机会本局跨店保留，轮盘抽中的自选独立累计。",10,280,972,80,23);
            NativeUi.Label(body,string.Join("\n",new[]{"金币：80／200／500","随机鸡械：1／2／4台","工匠刷新：1／3／10次","后备隐藏能源：增加1／2次","稀有奖励：自选强化上限＋1、自选鸡械","打开轮盘查看各档实际概率。"}),10,370,972,276,21);
            NativeUi.Label(body,p.ShopReceipt??"打开轮盘查看奖池与中奖结果。",10,630,972,58,22);
        }
        private void OpenCapacity()
        {modal.Close();Plugin.Instance.StartCoroutine(OpenCapacityNextFrame());}
        private static IEnumerator OpenCapacityNextFrame()
        {yield return null;GrowthPanel.ShowPending();}
        private static string PrizeTable()
        {
            bool growth=ModuleSettings.Extra.Growth,energy=ModuleSettings.Extra.Energy;
            float total=ShopRewardRules.Total(growth,energy);
            return string.Join("\n",ShopRewardRules.Prizes.Where(p=>ShopRewardRules.Available(p,growth,energy)).Select(p=>p.Name+"　"+(p.Weight*100/total).ToString("0.#")+"%"));
        }
        private void Pick()
        {
            Clear();var pool=source.Pool();
            NativeUi.TransferAction(body,"返回",10,0,140,48,Home);
            var description=NativeUi.Label(body,"选择一台鸡械，确认后直接加入背包。",170,0,800,66,23);
            var scroll=NativeUi.ScrollArea(body,"商品",10,84,948,492,out var content);
            content.sizeDelta=new Vector2(948,Mathf.Max(492,((pool.Count+3)/4)*154));
            var confirm=NativeUi.TransferAction(body,"确认领取",736,620,224,60,()=>Claim(description));confirm.interactable=false;
            int index=0;
            foreach(int id in pool)
            {
                int unitId=id;
                var stats=Plugin.Units.获取单位(id).GetComponent<模块_基本属性>();
                var rect=NativeUi.Rect(content,"鸡械",index%4*237,index/4*154,225,144);index++;
                var background=NativeUi.Image(rect,NativeUi.TransferCard,Color.white);
                var icon=NativeUi.Image(NativeUi.Rect(rect,"图标",72,8,80,82),stats.图标,Color.white);icon.preserveAspect=true;icon.raycastTarget=false;
                var name=NativeUi.Label(rect,stats.名称(),6,95,213,44,22);name.alignment=TextAnchor.MiddleCenter;
                var frame=NativeUi.Image(NativeUi.Rect(rect,"选中",0,0,225,144),NativeUi.Selection,Color.white);frame.raycastTarget=false;frame.enabled=false;
                var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=background;
                button.onClick.AddListener(()=>{if(marker!=null)marker.enabled=false;marker=frame;frame.enabled=true;selected=unitId;description.text="已选："+stats.名称();confirm.interactable=true;});
            }
            if(pool.Count==0)description.text="本店暂无可领取的鸡械，机会本局保留。";
        }
        private void Claim(Text description)
        {
            var p=ModuleSettings.LocalProgress;
            if(p.ShopPicks<=0 || !source.Pool().Contains(selected))return;
            int id=selected;p.ShopPicks--;Plugin.Summary.获得炮台(id);
            p.ShopReceipt="已领取："+Plugin.Units.获取单位(id).GetComponent<模块_基本属性>().名称();
            ShopRewards.Save();Home();
        }
        private void Wheel()
        {
            Clear();NativeUi.TransferAction(body,"返回",10,0,140,48,Home);
            NativeUi.Label(body,"商店回馈　每5次付费刷新获得1次抽取",178,0,790,54,26).color=new Color(1,.82f,.35f);
            var wheelRect=NativeUi.Rect(body,"轮盘",16,78,520,520);
            wheelRect.pivot=new Vector2(.5f,.5f);wheelRect.anchoredPosition=new Vector2(276,-338);
            var graphic=wheelRect.gameObject.AddComponent<ShopWheelGraphic>();graphic.raycastTarget=false;
            bool growth=ModuleSettings.Extra.Growth,energy=ModuleSettings.Extra.Energy;
            for(int i=0;i<ShopRewardRules.Groups.Length;i++)
            {
                float start=ShopRewardRules.SectorStart(i,growth,energy),end=ShopRewardRules.SectorEnd(i,growth,energy);
                if(end<=start)continue;
                float a=(start+end)*.5f*Mathf.Deg2Rad;
                if(i==4)
                {
                    var star=NativeUi.Label(wheelRect,"★",260+Mathf.Sin(a)*174-11,260-Mathf.Cos(a)*174-15,22,30,20);
                    star.alignment=TextAnchor.MiddleCenter;star.color=new Color(1,.89f,.44f);star.raycastTarget=false;star.gameObject.AddComponent<ShopWheelUpright>();
                    continue;
                }
                float percent=(end-start)/3.6f;
                var label=NativeUi.Label(wheelRect,ShopRewardRules.Groups[i]+"\n"+percent.ToString("0.#")+"%",
                    260+Mathf.Sin(a)*182-65,260-Mathf.Cos(a)*182-34,130,68,22);
                label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
                label.color=i>=4?new Color(1,.86f,.42f):new Color(.96f,.95f,.87f);
                label.gameObject.AddComponent<ShopWheelUpright>();
                Sprite icon=null;
                if(i==0)icon=NativeUi.GoldIcon;
                else if(i==1){var pool=source.Pool(true);if(pool.Count>0)icon=Plugin.Units.获取单位(pool[0]).GetComponent<模块_基本属性>().图标;}
                if(icon!=null)
                {
                    var image=NativeUi.Image(NativeUi.Rect(wheelRect,"奖励图标",260+Mathf.Sin(a)*182-19,260-Mathf.Cos(a)*182-72,38,38),icon,Color.white);
                    image.preserveAspect=true;image.raycastTarget=false;image.gameObject.AddComponent<ShopWheelUpright>();
                }
                else if(i==3)
                {
                    var symbol=NativeUi.Label(wheelRect,"F",260+Mathf.Sin(a)*182-24,260-Mathf.Cos(a)*182-74,48,44,30);
                    symbol.alignment=TextAnchor.MiddleCenter;symbol.color=new Color(1,.82f,.35f);symbol.raycastTarget=false;symbol.gameObject.AddComponent<ShopWheelUpright>();
                }
            }
            NativeUi.Image(NativeUi.Rect(body,"奖池底板",560,76,418,526),NativeUi.TransferCard,Color.white).raycastTarget=false;
            NativeUi.Label(body,"奖池与实际概率",578,88,382,40,25).color=new Color(1,.82f,.35f);
            NativeUi.Label(body,PrizeTable(),578,140,382,448,21).lineSpacing=1.16f;
            var pointer=NativeUi.Label(body,"▼",243,62,66,48,42);pointer.alignment=TextAnchor.MiddleCenter;pointer.color=new Color(1,.84f,.25f);pointer.raycastTarget=false;
            var rare=NativeUi.Label(body,"★ 稀有大奖　"+((ShopRewardRules.SectorEnd(4,growth,energy)-ShopRewardRules.SectorStart(4,growth,energy))/3.6f).ToString("0.#")+"%",16,604,520,36,22);
            rare.alignment=TextAnchor.MiddleCenter;rare.color=new Color(1,.84f,.35f);
            var result=NativeUi.Label(body,"扇区面积对应中奖概率",16,648,960,54,22);result.alignment=TextAnchor.MiddleCenter;
            var draw=NativeUi.TransferAction(body,"开始抽取\n剩余 "+ModuleSettings.LocalProgress.ShopSpins+"次",174,290,204,96,()=>{});
            draw.onClick.AddListener(()=>{if(!spinning)StartCoroutine(Spin(wheelRect,draw,result));});
            draw.interactable=ModuleSettings.LocalProgress.ShopSpins>0;
        }
        private IEnumerator Spin(RectTransform wheel,Button button,Text result)
        {
            if(!ShopRewardNetwork.Request())yield break;
            spinning=true;button.interactable=false;
            button.GetComponentInChildren<Text>().text="抽取中";
            float waiting=Time.realtimeSinceStartup;
            while(ShopRewardNetwork.Completed==null&&Time.realtimeSinceStartup-waiting<5)yield return null;
            if(ShopRewardNetwork.Completed==null)
            {
                spinning=false;result.text="等待主机结算，点击重试继续接收本次结果。";
                button.GetComponentInChildren<Text>().text="继续接收";button.interactable=true;yield break;
            }
            var completed=ShopRewardNetwork.Completed;var prize=ShopRewardRules.Prizes[completed.Prize];
            int sector=ShopRewardRules.Sector(prize.Kind);
            bool growth=ModuleSettings.Extra.Growth,energy=ModuleSettings.Extra.Energy;
            float start=ShopRewardRules.SectorStart(sector,growth,energy),end=ShopRewardRules.SectorEnd(sector,growth,energy);
            float stop=Mathf.Lerp(start,end,Random.Range(.2f,.8f)),angle=1440+stop,started=Time.unscaledTime;
            while(wheel!=null && Time.unscaledTime-started<3)
            {float t=(Time.unscaledTime-started)/3;wheel.localEulerAngles=new Vector3(0,0,angle*(1-Mathf.Pow(1-t,3)));yield return null;}
            if(wheel==null){spinning=false;yield break;}
            wheel.localEulerAngles=new Vector3(0,0,angle);
            var graphic=wheel.GetComponent<ShopWheelGraphic>();graphic.Highlight=sector;graphic.SetVerticesDirty();
            result.text="已到账："+prize.Name+(completed.Machines.Length>0?"\n"+string.Join("、",completed.Machines.Select(id=>Plugin.Units.获取单位(id).GetComponent<模块_基本属性>().名称())):"");
            spinning=false;button.GetComponentInChildren<Text>().text=prize.Name+"\n点击返回";
            button.onClick.RemoveAllListeners();button.onClick.AddListener(Home);button.interactable=true;
        }
    }
    public sealed class ShopWheelGraphic:MaskableGraphic
    {
        internal int Highlight=-1;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Color[] colors={new Color32(106,78,36,255),new Color32(53,82,77,255),new Color32(67,76,91,255),new Color32(55,75,110,255),new Color32(174,120,44,255)};
            var rect=rectTransform.rect;Vector2 center=rect.center;float radius=Mathf.Min(rect.width,rect.height)*.49f,inner=radius*.43f;
            for(int sector=0;sector<ShopRewardRules.Groups.Length;sector++)
            {
                float start=ShopRewardRules.SectorStart(sector,ModuleSettings.Extra.Growth,ModuleSettings.Extra.Energy),end=ShopRewardRules.SectorEnd(sector,ModuleSettings.Extra.Growth,ModuleSettings.Extra.Energy);
                Color tint=sector==Highlight?Color.Lerp(colors[sector],new Color(1,.84f,.35f),.45f):colors[sector];
                for(float a=start;a<end;a+=2)
                {
                    float b=Mathf.Min(a+2,end),ra=a*Mathf.Deg2Rad,rb=b*Mathf.Deg2Rad;int i=mesh.currentVertCount;
                    var da=new Vector2(Mathf.Sin(ra),Mathf.Cos(ra));var db=new Vector2(Mathf.Sin(rb),Mathf.Cos(rb));
                    mesh.AddVert(center+da*inner,tint,Vector2.zero);mesh.AddVert(center+da*radius,tint,Vector2.zero);
                    mesh.AddVert(center+db*radius,tint,Vector2.zero);mesh.AddVert(center+db*inner,tint,Vector2.zero);
                    mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
                    Rim(mesh,center,da,db,radius-3,radius,new Color(1,.77f,.28f));
                    Rim(mesh,center,da,db,inner,inner+2,new Color(.75f,.62f,.34f));
                }
            }
        }
        private static void Rim(VertexHelper mesh,Vector2 center,Vector2 a,Vector2 b,float inner,float outer,Color tint)
        {
            int i=mesh.currentVertCount;
            mesh.AddVert(center+a*inner,tint,Vector2.zero);mesh.AddVert(center+a*outer,tint,Vector2.zero);
            mesh.AddVert(center+b*outer,tint,Vector2.zero);mesh.AddVert(center+b*inner,tint,Vector2.zero);
            mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
        }
    }
    public sealed class ShopWheelUpright:MonoBehaviour
    {
        private void Awake()
        {
            var rect=(RectTransform)transform;
            rect.anchoredPosition+=new Vector2((.5f-rect.pivot.x)*rect.rect.width,(.5f-rect.pivot.y)*rect.rect.height);
            rect.pivot=new Vector2(.5f,.5f);
        }
        private void LateUpdate()=>transform.rotation=Quaternion.identity;
    }
}
