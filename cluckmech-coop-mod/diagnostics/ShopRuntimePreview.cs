using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CluckmechCoopDiagnostics
{
    // Explicit development invocation, using the production view and draw path.
    internal static class ShopRuntimePreview
    {
        internal static void TryStart(BaseUnityPlugin owner,Assembly main)
        {
            const string prefix="--bco-shop-preview=";
            var argument=Environment.GetCommandLineArgs().FirstOrDefault(x=>x.StartsWith(prefix,StringComparison.Ordinal));
            if(argument==null)return;
            var guard=new Harmony("billy.cluckmech.preview.saves");
            foreach(string method in new[]{"保存","读取","存档载入内存","删除存档","删除备用存档","删除未加密存档"})
                guard.Patch(AccessTools.Method(typeof(存档脚本),method),prefix:new HarmonyMethod(AccessTools.Method(typeof(ShopRuntimePreview),nameof(KeepIsolated))));
            AudioListener.pause=true;AudioListener.volume=0;Application.runInBackground=true;
            owner.StartCoroutine(Show(owner,main,Path.GetFullPath(argument.Substring(prefix.Length))));
        }
        private static bool KeepIsolated()=>false;
        private static IEnumerator Show(BaseUnityPlugin owner,Assembly main,string output)
        {
            float deadline=Time.realtimeSinceStartup+30;
            var settings=main.GetType("CluckmechCoop.ModuleSettings",true);
            var plugin=main.GetType("CluckmechCoop.Plugin",true);
            while(Time.realtimeSinceStartup<deadline&&(SceneManager.GetActiveScene().buildIndex!=函数库.常数类.场景id_开始菜单||
                AccessTools.Property(plugin,"Summary").GetValue(null)==null||UnityEngine.Object.FindObjectOfType<Canvas>()==null))yield return null;
            if(Time.realtimeSinceStartup>=deadline){Debug.LogError("[SHOP-PREVIEW] Menu readiness timed out");Application.Quit(1);yield break;}
            yield return new WaitForSecondsRealtime(2);
            AccessTools.Field(plugin,"InCombat").SetValue(null,false);
            AccessTools.Property(settings,"InRun").SetValue(null,true);
            var modules=Activator.CreateInstance(main.GetType("CluckmechCoop.GameplayModules",true),true);
            foreach(string field in new[]{"Shop","Growth","Energy"})AccessTools.Field(modules.GetType(),field).SetValue(modules,true);
            AccessTools.Property(settings,"Extra").SetValue(null,modules);
            AccessTools.Property(settings,"Progress").SetValue(null,Activator.CreateInstance(main.GetType("CluckmechCoop.RunProgress",true),true));
            var progress=AccessTools.Property(settings,"LocalProgress").GetValue(null);
            AccessTools.Field(progress.GetType(),"ShopSpins").SetValue(progress,1);
            var root=new GameObject("ShopPreviewSource");root.SetActive(false);
            var shop=root.AddComponent<商店脚本>();shop.商品单位等级=3;shop.第三栏超模额外等级=1;
            var rewards=main.GetType("CluckmechCoop.ShopRewards",true);
            AccessTools.Method(rewards,"Attach").Invoke(null,new object[]{shop});
            AccessTools.Method(rewards,"Show").Invoke(root.GetComponent(rewards),null);
            var viewType=main.GetType("CluckmechCoop.ShopRewardsView",true);
            var view=(MonoBehaviour)UnityEngine.Object.FindObjectOfType(viewType);
            AccessTools.Method(viewType,"Wheel").Invoke(view,null);
            Directory.CreateDirectory(output);yield return null;yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"wheel-ready.png"));
            yield return new WaitForSecondsRealtime(.4f);
            var button=view.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text.StartsWith("开始抽取")==true);
            button.onClick.Invoke();yield return new WaitForSecondsRealtime(1.1f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"wheel-spinning.png"));
            yield return new WaitForSecondsRealtime(2.4f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"wheel-result.png"));
            yield return new WaitForSecondsRealtime(.5f);
            int spins=(int)AccessTools.Field(progress.GetType(),"ShopSpins").GetValue(progress);
            int sequence=(int)AccessTools.Field(progress.GetType(),"ShopAppliedSequence").GetValue(progress);
            bool images=new[]{"wheel-ready.png","wheel-spinning.png","wheel-result.png"}.All(f=>File.Exists(Path.Combine(output,f)));
            Debug.Log($"[SHOP-PREVIEW] screenshots={images}; remaining={spins}; applied={sequence}; production view and grant complete");
            Application.Quit(images&&spins==0&&sequence==1?0:1);
        }
    }
}
