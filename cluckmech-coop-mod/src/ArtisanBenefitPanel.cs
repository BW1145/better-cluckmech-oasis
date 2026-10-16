using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CluckmechCoop
{
    public sealed class ArtisanBenefitPanel : MonoBehaviour
    {
        private static ArtisanBenefitPanel active;
        private RectTransform panel,content;
        private ScrollRect scroll;
        private Text title,chosen;
        private Button confirm,back;
        private 总结脚本.背包单元 machine;
        private int affix;
        private Image marker;
        private bool previousUi;
        private GameObject previousSelection;
        internal static void Show(ArtisanEconomyUi source)
        {
            if(active!=null || !ArtisanEconomy.Enabled || ModuleSettings.LocalProgress.ArtisanCredits<=0)return;
            var canvas=source.GetComponentInParent<Canvas>().rootCanvas;
            NativeUi.Capture(canvas.transform);
            var root=NativeUi.Rect(canvas.transform,"强化回馈选择",0,0,0,0);
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            active=root.gameObject.AddComponent<ArtisanBenefitPanel>();active.Build();
            var cursor=canvas.GetComponentInChildren<光标管理器>(true);
            if(cursor!=null)
            {
                var layer=cursor.transform;while(layer.parent!=canvas.transform && layer.parent!=null)layer=layer.parent;
                layer.SetAsLastSibling();
            }
        }
        private void Build()
        {
            previousUi=Plugin.Summary.是否光标位于UI界面;Plugin.Summary.是否光标位于UI界面=true;
            previousSelection=EventSystem.current==null?null:EventSystem.current.currentSelectedGameObject;
            NativeUi.Image((RectTransform)transform,null,new Color(0,0,0,.72f));
            panel=NativeUi.Rect(transform,"Panel",0,0,1000,760);
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=Vector2.zero;
            NativeUi.Image(panel,NativeUi.TransferPanel,Color.white);
            title=NativeUi.Label(panel,"",28,20,800,56,30);
            NativeUi.TransferAction(panel,"关闭",848,24,120,48,Close);
            scroll=NativeUi.ScrollArea(panel,"Choices",28,100,912,530,out content);
            chosen=NativeUi.Label(panel,"",28,646,540,70,23);
            back=NativeUi.TransferAction(panel,"重选鸡械",588,658,170,54,Machines);
            confirm=NativeUi.TransferAction(panel,"确认强化",778,658,190,54,Confirm);
            Machines();Resize();
        }
        private void Clear()
        {
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            marker=null;affix=0;chosen.text="";confirm.interactable=false;scroll.verticalNormalizedPosition=1;
        }
        private Image Mark(RectTransform rect)
        {var image=NativeUi.Image(NativeUi.Rect(rect,"选择",0,0,rect.rect.width,rect.rect.height),NativeUi.Selection,Color.white);image.raycastTarget=false;image.enabled=false;return image;}
        private void Machines()
        {
            Clear();machine=null;back.interactable=false;
            title.text="强化回馈：选择已有鸡械　剩余"+ModuleSettings.LocalProgress.ArtisanCredits+"次";
            var inventory=Plugin.Summary.当前拥有炮台;
            content.sizeDelta=new Vector2(912,Mathf.Max(530,((inventory.Count+3)/4)*170));
            bool available=false;
            for(int i=0;i<inventory.Count;i++)
            {
                var item=inventory[i];var stats=Plugin.Units.获取单位(item.炮台id).GetComponent<模块_基本属性>();
                var rect=NativeUi.Rect(content,"鸡械",i%4*228,i/4*170,218,160);
                var background=NativeUi.Image(rect,NativeUi.TransferCard,Color.white);
                var icon=NativeUi.Image(NativeUi.Rect(rect,"Icon",70,10,78,74),stats.图标,Color.white);icon.preserveAspect=true;icon.raycastTarget=false;
                var name=NativeUi.Label(rect,stats.名称(),8,86,202,35,22);name.alignment=TextAnchor.MiddleCenter;
                bool legal=ArtisanEconomy.Choices(item).Count>0;available|=legal;
                var count=NativeUi.Label(rect,item.强化词条.Count+"／"+item.强化上限+(legal?"":item.强化词条.Count>=item.强化上限?"　强化已满":"　暂无可选"),8,124,202,30,19);count.alignment=TextAnchor.MiddleCenter;
                var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=background;button.interactable=legal;
                button.onClick.AddListener(()=>Affixes(item));
            }
            if(!available)chosen.text="当前没有可强化的鸡械，机会本局保留。";
        }
        private static string Text(string value)=>Regex.Replace(value.Replace("\\n","\n").Replace("\\r",""),@"</?size(?:=[^>]+)?>","");
        private void Affixes(总结脚本.背包单元 item)
        {
            if(!Plugin.Summary.当前拥有炮台.Contains(item)){Machines();return;}
            Clear();machine=item;back.interactable=true;
            var stats=Plugin.Units.获取单位(item.炮台id).GetComponent<模块_基本属性>();
            title.text=stats.名称()+"　选择强化　"+item.强化词条.Count+"／"+item.强化上限;
            var library=Object.FindObjectOfType<cz词缀库>();
            float top=0;
            foreach(int id in ArtisanEconomy.Choices(item))
            {
                var data=library.获取词缀数据(id);
                var rect=NativeUi.Rect(content,"强化",0,top,912,140);
                var background=NativeUi.Image(rect,NativeUi.TransferCard,Color.white);
                var icon=NativeUi.Image(NativeUi.Rect(rect,"Icon",14,20,82,82),data.图标,Color.white);icon.preserveAspect=true;icon.raycastTarget=false;
                string name=Text(data.获取词缀名称());
                var heading=NativeUi.Label(rect,name,112,12,780,34,25);
                float nameHeight=Mathf.Max(34,heading.preferredHeight);
                heading.rectTransform.sizeDelta=new Vector2(780,nameHeight);
                string text=Text(data.获取词缀描述());
                if(data.是否唯一 && !text.Contains("唯一强化"))text="<color=orange>唯一强化</color>\n"+text;
                float descriptionTop=18+nameHeight;
                var description=NativeUi.Label(rect,text,112,descriptionTop,780,80,22);
                float height=Mathf.Max(136,descriptionTop+description.preferredHeight+14);rect.sizeDelta=new Vector2(912,height);
                description.rectTransform.sizeDelta=new Vector2(780,height-descriptionTop-10);
                var selected=Mark(rect);
                var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=background;
                int choice=id;
                button.onClick.AddListener(()=>
                {
                    if(marker!=null)marker.enabled=false;
                    marker=selected;marker.enabled=true;affix=choice;chosen.text=name;confirm.interactable=true;
                });
                top+=height+10;
            }
            content.sizeDelta=new Vector2(912,Mathf.Max(530,top));
        }
        private void Confirm()
        {
            if(ModuleSettings.LocalProgress.ArtisanCredits<=0){Close();return;}
            if(machine==null || !Plugin.Summary.当前拥有炮台.Contains(machine)){Machines();return;}
            if(!ArtisanEconomy.Choices(machine).Contains(affix)){Affixes(machine);return;}
            int slot=Plugin.Summary.当前拥有炮台.IndexOf(machine);
            Plugin.Summary.获得强化词条(slot,affix);ModuleSettings.LocalProgress.ArtisanCredits--;
            ArtisanEconomyUi.LastReceipt="已获得自选强化";ArtisanEconomy.Save();
            foreach(var bag in Object.FindObjectsOfType<玩家背包脚本>())bag.更新背包();
            if(ModuleSettings.LocalProgress.ArtisanCredits>0)Machines();else Close();
        }
        private void Update(){if(Input.GetKeyDown(KeyCode.Escape))Close();}
        private void Resize()
        {
            if(panel==null)return;
            var rect=((RectTransform)transform).rect;
            panel.localScale=Vector3.one*Mathf.Min(rect.height/1080,(rect.width-40)/1000,(rect.height-40)/760);
        }
        private void OnRectTransformDimensionsChange()=>Resize();
        private void Close(){Destroy(gameObject);}
        private void OnDestroy()
        {
            if(active!=this)return;active=null;
            if(Plugin.Summary!=null)Plugin.Summary.是否光标位于UI界面=previousUi;
            if(EventSystem.current!=null && previousSelection!=null)EventSystem.current.SetSelectedGameObject(previousSelection);
        }
    }
}
