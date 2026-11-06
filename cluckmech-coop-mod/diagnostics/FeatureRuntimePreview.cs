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
using 函数库;

namespace CluckmechCoopDiagnostics
{
    internal static class FeatureRuntimePreview
    {
        internal static void TryStart(BaseUnityPlugin owner,Assembly main)
        {
            const string prefix="--bco-feature-preview=";var arg=Environment.GetCommandLineArgs().FirstOrDefault(x=>x.StartsWith(prefix,StringComparison.Ordinal));if(arg==null)return;
            var guard=new Harmony("billy.cluckmech.feature-preview.saves");
            foreach(string method in new[]{"保存","读取","存档载入内存","删除存档","删除备用存档","删除未加密存档"})
                guard.Patch(AccessTools.Method(typeof(存档脚本),method),prefix:new HarmonyMethod(AccessTools.Method(typeof(FeatureRuntimePreview),nameof(Isolated))));
            AudioListener.pause=true;AudioListener.volume=0;Application.runInBackground=true;
            owner.StartCoroutine(Show(main,Path.GetFullPath(arg.Substring(prefix.Length))));
        }
        private static bool Isolated()=>false;
        private static IEnumerator Show(Assembly main,string output)
        {
            int testCase=0;var caseArg=Environment.GetCommandLineArgs().FirstOrDefault(x=>x.StartsWith("--bco-feature-case="));if(caseArg!=null)testCase=int.Parse(caseArg.Substring("--bco-feature-case=".Length));
            Type T(string name)=>main.GetType("CluckmechCoop."+name,true);
            object Call(string type,string method,params object[] args)=>AccessTools.Method(T(type),method).Invoke(null,args);
            float limit=Time.realtimeSinceStartup+28;
            while(Time.realtimeSinceStartup<limit&&(SceneManager.GetActiveScene().buildIndex!=常数类.场景id_开始菜单||UnityEngine.Object.FindObjectOfType<Canvas>()==null))yield return null;
            yield return new WaitForSecondsRealtime(2);Directory.CreateDirectory(output);
            var effects=Resources.FindObjectsOfTypeAll<效果物体引用器>().First(e=>e.特效.普通攻击效果!=null);
            Call("VisualParticlePool","Register",effects);
            var first=(GameObject)Call("VisualParticlePool","Create",effects.特效.普通攻击效果,Vector3.zero,Quaternion.identity);
            Call("VisualParticlePool","Return",first);
            var second=(GameObject)Call("VisualParticlePool","Create",effects.特效.普通攻击效果,Vector3.one,Quaternion.identity);
            bool reused=first==second;Call("VisualParticlePool","Return",second);
            var flags=(bool[])AccessTools.Field(T("VisualPreferences"),"active").GetValue(null);for(int i=0;i<4;i++)flags[i]=true;
            var timer=System.Diagnostics.Stopwatch.StartNew();int reuseCount=0;
            for(int i=0;i<100;i++)
            {
                var item=(GameObject)Call("VisualParticlePool","Create",effects.特效.普通攻击效果,Vector3.zero,Quaternion.identity);
                if(item==first)reuseCount++;Call("VisualParticlePool","Return",item);
            }
            timer.Stop();
            var receiver=new GameObject("StatusPresentationProbe");
            var statusA=(GameObject)Call("VisualParticlePool","Status",effects.特效.灼烧效果,receiver.transform);
            var statusB=(GameObject)Call("VisualParticlePool","Status",effects.特效.灼烧效果,receiver.transform);
            bool statusSingle=statusA==statusB;
            var coinReceiver=new GameObject("CoinPresentationProbe").AddComponent<CircleCollider2D>();
            var coinA=(GameObject)Call("VisualSceneCache","Coin",effects.特效.吃金币效果,Vector3.zero,Quaternion.identity,coinReceiver);
            var coinB=(GameObject)Call("VisualSceneCache","Coin",effects.特效.吃金币效果,Vector3.zero,Quaternion.identity,coinReceiver);
            bool coinsSingle=coinA!=null&&coinB==null;
            var smoke=UnityEngine.Object.Instantiate(effects.特效.烟雾效果);var smokeSystem=smoke.GetComponentInChildren<ParticleSystem>();int originalMax=smokeSystem.main.maxParticles;
            Call("VisualPreferences","ThinSmoke",smoke);bool halfSmoke=smokeSystem.main.maxParticles==Mathf.Max(1,Mathf.CeilToInt(originalMax*.5f));UnityEngine.Object.Destroy(smoke);
            Debug.Log($"[FEATURE-PREVIEW] repeat-reused={reuseCount}/100; pool-ms={timer.Elapsed.TotalMilliseconds:F3}; status-single={statusSingle}; coins-single={coinsSingle}; smoke-half={halfSmoke}");
            var target=Resources.FindObjectsOfTypeAll<模块_受伤>().First();var prefab=effects.显示精灵.伤害显示精灵;
            var color=new Vector4(1,1,1,1);
            var number=(GameObject)Call("VisualDamageNumbers","Create",prefab,Vector3.zero,Quaternion.identity,target,color);number.GetComponent<显示精灵>().UI显示数字(12,color);
            var merged=(GameObject)Call("VisualDamageNumbers","Create",prefab,Vector3.zero,Quaternion.identity,target,color);merged.GetComponent<显示精灵>().UI显示数字(18,color);
            bool summed=number==merged&&number.GetComponent<显示精灵>().显示的数字.text=="30";
            var colored=(GameObject)Call("VisualDamageNumbers","Create",prefab,Vector3.zero,Quaternion.identity,target,new Vector4(1,.4f,.2f,1));
            bool separateColor=colored!=merged;
            if(testCase==2)
            {
                int combinations=0;
                for(int mask=0;mask<16;mask++)
                {
                    for(int i=0;i<4;i++)flags[i]=(mask&(1<<i))!=0;
                    Call("VisualDamageNumbers","Clear");Call("VisualSceneCache","Clear");
                    var a=(GameObject)Call("VisualDamageNumbers","Create",prefab,Vector3.zero,Quaternion.identity,target,color);a.GetComponent<显示精灵>().UI显示数字(7,color);
                    var b=(GameObject)Call("VisualDamageNumbers","Create",prefab,Vector3.zero,Quaternion.identity,target,color);b.GetComponent<显示精灵>().UI显示数字(9,color);
                    bool numberOk=(a==b)==flags[0]&&b.GetComponent<显示精灵>().显示的数字.text==(flags[0]?"16":"9");
                    var ca=(GameObject)Call("VisualSceneCache","Coin",effects.特效.吃金币效果,Vector3.zero,Quaternion.identity,coinReceiver);
                    var cb=(GameObject)Call("VisualSceneCache","Coin",effects.特效.吃金币效果,Vector3.zero,Quaternion.identity,coinReceiver);
                    bool coinOk=(cb==null)==flags[1];
                    var parent=new GameObject("StatusCombinationProbe");
                    var sa=(GameObject)Call("VisualParticlePool","Status",effects.特效.感电效果,parent.transform);
                    var sb=(GameObject)Call("VisualParticlePool","Status",effects.特效.感电效果,parent.transform);
                    bool statusOk=(sa==sb)==flags[3];
                    var sm=UnityEngine.Object.Instantiate(effects.特效.烟雾效果);var ps=sm.GetComponentInChildren<ParticleSystem>();int before=ps.main.maxParticles;
                    Call("VisualPreferences","ThinSmoke",sm);bool smokeOk=ps.main.maxParticles==(flags[2]?Mathf.Max(1,Mathf.CeilToInt(before*.5f)):before);
                    UnityEngine.Object.Destroy(sm);Call("VisualParticlePool","Return",sa);if(sb!=sa)Call("VisualParticlePool","Return",sb);UnityEngine.Object.Destroy(parent);
                    if(numberOk&&coinOk&&statusOk&&smokeOk)combinations++;
                }
                AccessTools.Property(T("ThemeTest"),"Active").SetValue(null,true);
                var saveRoot=new GameObject("TestIsolationProbe");saveRoot.SetActive(false);var save=saveRoot.AddComponent<存档脚本>();save.文件名="danju";
                bool isolated=!(bool)AccessTools.Method(T("ThemeTestSaveGuard"),"Prefix").Invoke(null,new object[]{save});
                bool statistics=!(bool)AccessTools.Method(T("ThemeTestStatisticsGuard"),"Prefix").Invoke(null,null);
                object[] sceneArgs={常数类.场景id_游戏失败};AccessTools.Method(T("ThemeTestFailureReturn"),"Prefix").Invoke(null,sceneArgs);
                bool failure=(int)sceneArgs[0]==常数类.场景id_开始菜单;
                AccessTools.Property(T("ThemeTest"),"Active").SetValue(null,false);UnityEngine.Object.Destroy(saveRoot);
                float frameStart=Time.realtimeSinceStartup;for(int i=0;i<10;i++)yield return null;
                Debug.Log($"[FEATURE-PREVIEW] combinations={combinations}/16; save-isolated={isolated}; statistics-isolated={statistics}; failure-menu={failure}; sample-frame-ms={(Time.realtimeSinceStartup-frameStart)*100:F2}");
                if(combinations!=16||!isolated||!statistics||!failure){Application.Quit(1);yield break;}
            }
            Debug.Log($"[FEATURE-PREVIEW] particle-reused={reused}; number-sum={summed}");
            for(int i=0;i<4;i++)flags[i]=false;
            Call("VisualPreferences","Show");yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"visual-settings.png"));yield return new WaitForEndOfFrame();yield return null;
            Call("FeatureModal","Clear");yield return null;
            Call("ThemeTest","Menu");yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"theme-menu.png"));
            yield return new WaitForSecondsRealtime(.3f);
            var button=UnityEngine.Object.FindObjectsOfType<Button>().First(b=>b.GetComponentInChildren<Text>()?.text==(testCase<2?"古堡":"深海")+" · "+(testCase%2==0?"常规":"异化"));button.onClick.Invoke();
            while(Time.realtimeSinceStartup<limit&&UnityEngine.Object.FindObjectOfType<选择英雄缓存器脚本>()==null)yield return null;
            yield return new WaitForSecondsRealtime(1);
            var hero=UnityEngine.Object.FindObjectOfType<选择英雄缓存器脚本>();if(hero==null){Debug.LogError("[FEATURE-PREVIEW] hero selection missing");Application.Quit(1);yield break;}
            hero.当前选择的英雄(0);
            var summary=UnityEngine.Object.FindObjectOfType<总结脚本>();summary.单局统计数据.当前难度=15;
            if(summary.单局统计数据.角色初始天赋==null)
                hero.选择天赋(Resources.FindObjectsOfTypeAll<角色天赋数据模板>().First(t=>t.是否指定角色&&t.指定角色id==0).id);
            hero.进入游戏实现();场景函数.切换选择地图();
            while(Time.realtimeSinceStartup<limit&&UnityEngine.Object.FindObjectOfType(T("ThemeTestLoadout"))==null)yield return null;
            var view=(MonoBehaviour)UnityEngine.Object.FindObjectOfType(T("ThemeTestLoadout"));
            if(view==null){Debug.LogError("[FEATURE-PREVIEW] loadout missing");Application.Quit(1);yield break;}
            yield return new WaitForSecondsRealtime(3);ScreenCapture.CaptureScreenshot(Path.Combine(output,"theme-loadout.png"));yield return new WaitForEndOfFrame();yield return null;
            AccessTools.Method(T("ThemeTestLoadout"),"Machines").Invoke(view,null);yield return null;
            var unitButton=view.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text!="返回配装"&&b.GetComponentInChildren<Text>()?.text!="关闭");unitButton.onClick.Invoke();yield return null;
            view.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text=="强化").onClick.Invoke();yield return null;
            view.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text=="获得强化").onClick.Invoke();yield return null;
            AccessTools.Method(T("ThemeTestLoadout"),"Inventory").Invoke(view,null);yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"theme-equipped.png"));yield return new WaitForEndOfFrame();yield return null;
            Call("ThemeTest","StartBattles");yield return new WaitForSecondsRealtime(3);
            var state=AccessTools.Property(T("ThemeBranchRuntime"),"State").GetValue(null);int theme=(int)AccessTools.Field(state.GetType(),"Theme").GetValue(state);
            bool active=(bool)AccessTools.Field(state.GetType(),"Active").GetValue(state);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"theme-route.png"));yield return new WaitForSecondsRealtime(.4f);
            Debug.Log($"[FEATURE-PREVIEW] theme={theme}; active={active}; machines={summary.当前拥有炮台.Count}; affixes={summary.当前拥有炮台[0].强化词条.Count}; difficulty={summary.单局统计数据.当前难度}; page={summary.地图存档.当前地图页}");
            bool[] variants=(bool[])AccessTools.Field(state.GetType(),"Eldritch").GetValue(state);
            bool correctVariants=variants.All(v=>v==(testCase%2!=0));
            Debug.Log($"[FEATURE-PREVIEW] variants={correctVariants}; colors-separate={separateColor}; pooled={reuseCount}");
            Application.Quit(reused&&reuseCount==100&&summed&&separateColor&&statusSingle&&coinsSingle&&halfSmoke&&correctVariants&&active&&theme==(testCase<2?1:2)&&summary.当前拥有炮台.Count==1&&summary.当前拥有炮台[0].强化词条.Count==1?0:1);
        }
    }
}
