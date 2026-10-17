using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CluckmechCoop
{
    public sealed class FeatureModal:MonoBehaviour
    {
        private static FeatureModal active;
        internal RectTransform Panel;
        private float width,height;
        private bool previousUi;
        private GameObject previousSelection;
        internal static void Clear(){if(active!=null)active.Close();}
        internal static FeatureModal Open(string title,float width,float height)
        {
            if(active!=null)return null;
            var cursor=Object.FindObjectOfType<光标管理器>();
            var canvas=cursor!=null?cursor.GetComponentInParent<Canvas>().rootCanvas:Object.FindObjectOfType<Canvas>()?.rootCanvas;
            if(canvas==null)return null;NativeUi.Capture(canvas.transform);
            var root=NativeUi.Rect(canvas.transform,title,0,0,0,0);
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            var modal=root.gameObject.AddComponent<FeatureModal>();active=modal;
            modal.width=width;modal.height=height;
            modal.previousUi=Plugin.Summary.是否光标位于UI界面;Plugin.Summary.是否光标位于UI界面=true;
            modal.previousSelection=EventSystem.current?.currentSelectedGameObject;
            NativeUi.Image(root,null,new Color(0,0,0,.72f));
            modal.Panel=NativeUi.Rect(root,"Panel",0,0,width,height);
            modal.Panel.anchorMin=modal.Panel.anchorMax=modal.Panel.pivot=new Vector2(.5f,.5f);modal.Panel.anchoredPosition=Vector2.zero;
            NativeUi.Image(modal.Panel,NativeUi.TransferPanel,Color.white);
            NativeUi.Label(modal.Panel,title,28,18,width-200,54,30);
            NativeUi.TransferAction(modal.Panel,"关闭",width-152,22,120,48,modal.Close);
            modal.Resize();CursorLast(canvas);return modal;
        }
        internal static void CursorLast(Canvas canvas)
        {
            var cursor=canvas.GetComponentInChildren<光标管理器>(true);if(cursor==null)return;
            var layer=cursor.transform;while(layer.parent!=canvas.transform && layer.parent!=null)layer=layer.parent;
            layer.SetAsLastSibling();
        }
        private void Resize()
        {
            if(Panel==null)return;var rect=((RectTransform)transform).rect;
            Panel.localScale=Vector3.one*Mathf.Min(rect.height/1080,(rect.width-32)/width,(rect.height-32)/height);
        }
        private void OnRectTransformDimensionsChange()=>Resize();
        private void Update(){if(Input.GetKeyDown(KeyCode.Escape))Close();}
        internal void Close()=>Destroy(gameObject);
        private void OnDestroy()
        {
            if(active!=this)return;active=null;
            if(Plugin.Summary!=null)Plugin.Summary.是否光标位于UI界面=previousUi;
            if(EventSystem.current!=null && previousSelection!=null)EventSystem.current.SetSelectedGameObject(previousSelection);
        }
    }
}
