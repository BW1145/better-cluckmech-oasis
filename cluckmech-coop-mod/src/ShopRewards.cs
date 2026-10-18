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
            current.progress.text="付费刷新 "+p.ShopRefreshCount+"／30　消费 "+p.ShopSpend+"／800";
            current.launcher.GetComponentInChildren<Text>().text="商店回馈　"+(p.ShopPicks+p.ShopSpins)+"次待领取";
        }
        internal List<int> Pool()
        {
            int maximum=Shop.商品单位等级+Mathf.Max(0,Shop.第三栏超模额外等级);
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
            NativeUi.Label(body,"本局累计　付费刷新 "+p.ShopRefreshCount+"／30　商店消费 "+p.ShopSpend+"／800金币",10,0,972,48,25);
            NativeUi.Label(body,"每30次付费刷新获得一次自选鸡械；每消费800金币获得一次轮盘。",10,54,972,42,23);
            var pick=NativeUi.TransferAction(body,"自选鸡械　剩余"+p.ShopPicks+"次",10,118,460,68,Pick);pick.interactable=p.ShopPicks>0;
            var spin=NativeUi.TransferAction(body,"奖励轮盘　剩余"+p.ShopSpins+"次",500,118,460,68,Wheel);spin.interactable=p.ShopSpins>0;
            NativeUi.Label(body,"奖励范围：本店当前允许出现且已解锁的鸡械。\n消费与刷新进度、未领取机会本局跨店保留。",10,208,972,80,24);
            NativeUi.Label(body,"随机鸡械4台　40%\n工匠刷新10次　30%\n金币500　25%\n自选鸡械1次　5%",10,324,972,200,27);
            NativeUi.Label(body,p.ShopReceipt??"购买鸡械、购买工匠刷新次数和付费刷新均计入商店消费。",10,548,972,142,23);
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
            NativeUi.Label(body,"随机鸡械4台40%　工匠刷新10次30%　500金币25%　自选鸡械5%",164,0,806,54,23);
            var wheelRect=NativeUi.Rect(body,"轮盘",246,78,500,500);
            wheelRect.pivot=new Vector2(.5f,.5f);wheelRect.anchoredPosition=new Vector2(496,-328);
            var wheel=wheelRect.gameObject.AddComponent<ShopWheelGraphic>();wheel.raycastTarget=false;
            string[] labels={"鸡械×4\n40%","工匠刷新×10\n30%","500金币\n25%","自选鸡械\n5%"};
            float[] angles={72,198,297,351};
            for(int i=0;i<4;i++)
            {
                float a=angles[i]*Mathf.Deg2Rad;
                var label=NativeUi.Label(wheelRect,labels[i],250+Mathf.Sin(a)*155-78,250-Mathf.Cos(a)*155-46,156,92,25);label.alignment=TextAnchor.MiddleCenter;
            }
            var pointer=NativeUi.Label(body,"▼",461,62,70,50,40);pointer.alignment=TextAnchor.MiddleCenter;pointer.color=new Color(1,.84f,.25f);
            var result=NativeUi.Label(body,"",10,590,972,94,24);result.alignment=TextAnchor.MiddleCenter;
            var draw=NativeUi.TransferAction(body,"开始抽取",376,502,240,68,()=>{});
            draw.onClick.AddListener(()=>{if(!spinning)StartCoroutine(Spin(wheelRect,draw,result));});
            draw.interactable=ModuleSettings.LocalProgress.ShopSpins>0;
        }
        private IEnumerator Spin(RectTransform wheel,Button button,Text result)
        {
            var p=ModuleSettings.LocalProgress;var pool=source.Pool();if(p.ShopSpins<=0 || pool.Count==0)yield break;
            spinning=true;button.interactable=false;
            int roll=Random.Range(0,100),prize=roll<40?0:roll<70?1:roll<95?2:3;
            p.ShopSpins--;
            if(prize==0)
            {
                var names=new List<string>();
                for(int i=0;i<4;i++){int id=pool[Random.Range(0,pool.Count)];Plugin.Summary.获得炮台(id);names.Add(Plugin.Units.获取单位(id).GetComponent<模块_基本属性>().名称());}
                p.ShopReceipt="获得4台鸡械："+string.Join("、",names);
            }
            else if(prize==1){p.Coupons+=10;p.ShopReceipt="获得10次工匠刷新机会。";}
            else if(prize==2){Plugin.Summary.获得金钱(500);p.ShopReceipt="获得500金币。";}
            else {p.ShopPicks++;p.ShopReceipt="获得1次自选鸡械机会。";}
            ShopRewards.Save();
            float[] starts={0,144,252,342},spans={144,108,90,18};
            float stop=starts[prize]+Random.Range(.2f,.8f)*spans[prize],angle=1440+stop,started=Time.unscaledTime;
            while(wheel!=null && Time.unscaledTime-started<3)
            {float t=(Time.unscaledTime-started)/3;wheel.localEulerAngles=new Vector3(0,0,angle*(1-Mathf.Pow(1-t,3)));yield return null;}
            if(wheel==null){spinning=false;yield break;}
            wheel.localEulerAngles=new Vector3(0,0,angle);result.text=p.ShopReceipt;spinning=false;
            button.GetComponentInChildren<Text>().text="返回奖励面板";button.onClick.RemoveAllListeners();button.onClick.AddListener(Home);button.interactable=true;
        }
    }
    public sealed class ShopWheelGraphic:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();float[] ends={144,252,342,360};
            Color32[] colors={new Color32(88,73,43,255),new Color32(49,83,72,255),new Color32(115,80,26,255),new Color32(102,48,43,255)};
            float previous=0;var rect=rectTransform.rect;Vector2 center=rect.center;float radius=Mathf.Min(rect.width,rect.height)*.49f;
            for(int sector=0;sector<4;sector++)
            {
                float end=ends[sector];
                for(float a=previous;a<end;a+=2)
                {
                    float b=Mathf.Min(a+2,end),ra=a*Mathf.Deg2Rad,rb=b*Mathf.Deg2Rad;int i=mesh.currentVertCount;
                    mesh.AddVert(center,colors[sector],Vector2.zero);
                    mesh.AddVert(center+new Vector2(Mathf.Sin(ra),Mathf.Cos(ra))*radius,colors[sector],Vector2.zero);
                    mesh.AddVert(center+new Vector2(Mathf.Sin(rb),Mathf.Cos(rb))*radius,colors[sector],Vector2.zero);mesh.AddTriangle(i,i+1,i+2);
                }
                previous=end;
            }
        }
    }
}
