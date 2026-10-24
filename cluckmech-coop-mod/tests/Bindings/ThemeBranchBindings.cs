using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using HarmonyLib;

internal static class ThemeBranchBindings
{
    internal static void MessageSize(Assembly native,Assembly mod)
    {
        int checks=0;
        void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
        MapRoundTrip(native,mod,mod.GetType("CluckmechCoop.ThemeMapCodec",true),Check,true);
        Console.WriteLine($"PASS: {checks} Chinese native-map settings and actual Mirror string checks");
    }

    internal static void Run(Assembly native, Assembly mod)
    {
        int checks=0;
        void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
        Type Native(string name)=>native.GetType(name)??native.GetTypes().Single(t=>t.Name==name);
        Type Mod(string name)=>mod.GetType("CluckmechCoop."+name,true);
        List<CodeInstruction> Body(Type type,string name)=>PatchProcessor.GetOriginalInstructions(AccessTools.DeclaredMethod(type,name));
        bool Calls(CodeInstruction code,string type,string name)=>code.operand is MethodInfo method&&method.DeclaringType.Name==type&&method.Name==name;
        bool Field(CodeInstruction code,string type,string name)=>code.operand is FieldInfo field&&field.DeclaringType.Name==type&&field.Name==name;
        void Binding(string patchName,string nativeType,string nativeMethod,Type[] arguments=null)
        {
            var patch=Mod(patchName);var info=new HarmonyMethod();
            foreach(HarmonyPatch attribute in patch.GetCustomAttributes(typeof(HarmonyPatch),false))
            {
                if(attribute.info.declaringType!=null)info.declaringType=attribute.info.declaringType;
                if(attribute.info.methodName!=null)info.methodName=attribute.info.methodName;
                if(attribute.info.argumentTypes!=null)info.argumentTypes=attribute.info.argumentTypes;
            }
            var expected=AccessTools.Method(Native(nativeType),nativeMethod,arguments);
            Check(expected!=null&&expected.GetMethodBody()!=null,"native method body: "+nativeType+"."+nativeMethod);
            Check(info.declaringType==expected.DeclaringType&&info.methodName==expected.Name&&
                AccessTools.Method(info.declaringType,info.methodName,info.argumentTypes)==expected,"Harmony target: "+patchName);
        }

        Binding("ThemeBranchMapInstall","地图管理器","Start");
        Binding("ThemeBranchNodeData","地图页","运算地图页内节点的场景");
        Binding("ThemeBranchNodeAction","地图页","触发节点作用");
        Binding("ThemeBranchServiceExit","节点场景信息保存","退出节点场景");
        Binding("ThemeBranchEventExit","模块_事件场景控制","退出事件场景");
        Binding("ThemeBranchPreparation","战斗准备背包","最终Boss关卡检测");
        Binding("InstallThemeBranchEvents","模块_事件场景控制","Start");
        Binding("ThemeBranchFixedEvent","模块_事件场景控制","随机场景控制");
        Binding("ThemeBranchRewardExit","场景函数","切换场景",new[]{typeof(int)});
        Binding("ThemeBranchMapSync","联机玩家控件","地图同步给客户端",Type.EmptyTypes);
        var targets=((IEnumerable<MethodBase>)AccessTools.DeclaredMethod(Mod("ThemeBranchMapPrepare"),"TargetMethods").Invoke(null,null)).ToArray();
        Check(targets.Length==2&&targets.Contains(AccessTools.Method(Native("地图管理器"),"地图初始化"))&&
            targets.Contains(AccessTools.Method(Native("地图管理器"),"地图加载")),"map preparation binds native initialization and continuation");
        foreach(var pair in new[]{("地图页","管理器"),("地图页","总结"),("地图页","大地图场景库"),("地图页","关卡奖励库"),
            ("地图页","关卡挑战库"),("地图节点","地图页"),("地图节点","地图管理器"),("地图节点","总结")})
            Check(AccessTools.Field(Native(pair.Item1),pair.Item2)!=null,"native injection field: "+pair.Item1+"."+pair.Item2);
        foreach(var seam in new[]{("ThemeBranchMapInstall","Prefix","ThemeBranchMaps","Install"),
            ("ThemeBranchMapPrepare","Prefix","ThemeBranchRuntime","PrepareMap"),
            ("ThemeBranchNodeAction","Prefix","ThemeBranchRuntime","HandleNode"),
            ("ThemeBranchFixedEvent","Prefix","ThemeBranchEvents","SelectFixed"),
            ("ThemeBranchServiceExit","Postfix","ThemeBranchRuntime","ServiceExited"),
            ("ThemeBranchEventExit","Prefix","ThemeBranchRuntime","EventExited")})
            Check(Body(Mod(seam.Item1),seam.Item2).Count(c=>Calls(c,seam.Item3,seam.Item4))==1,"compiled hook: "+seam.Item1);

        var changed=Body(Mod("ThemeBranchRuntime"),"Changed");
        Check(changed.FindIndex(c=>Calls(c,"ThemeBranchRuntime","Capture"))>=0&&
            changed.FindIndex(c=>Calls(c,"ThemeBranchRuntime","Capture"))<changed.FindIndex(c=>Calls(c,"总结脚本","保存单局存档"))&&
            changed.FindIndex(c=>Calls(c,"总结脚本","保存单局存档"))<changed.FindIndex(c=>Calls(c,"NetworkService","PublishSettings")),
            "branch changes capture, save and publish in that order");
        var mapSync=Body(Mod("ThemeBranchMapSync"),"Prefix");
        Check(mapSync.FindIndex(c=>Calls(c,"ThemeBranchRuntime","Capture"))>=0&&
            mapSync.FindIndex(c=>Calls(c,"ThemeBranchRuntime","Capture"))<mapSync.FindIndex(c=>Calls(c,"NetworkService","PublishSettings")),
            "native map sync publishes the captured branch before the original RPC");
        Check(Body(Native("联机玩家控件"),"地图同步给客户端").Any(c=>Calls(c,"联机玩家控件","接收地图数据_Rpc")),"native map sender retains its map RPC");
        var receiver=Body(Native("联机玩家控件"),"UserCode_接收地图数据_Rpc__联机总结数据包");
        int applyMap=receiver.FindIndex(c=>c.opcode==OpCodes.Stfld&&Field(c,"总结脚本","地图存档"));
        int syncDone=receiver.FindIndex(c=>c.opcode==OpCodes.Stfld&&Field(c,"地图管理器","完成同步"));
        Check(applyMap>=0&&syncDone>applyMap,
            "native map receiver applies the map before completing synchronization");
        var sessionRpc=(MethodInfo)AccessTools.Field(Mod("NetworkService"),"rpc").GetValue(null);
        Check(sessionRpc==AccessTools.Method(Native("联机玩家控件"),"发送消息广播_Rpc",new[]{typeof(string),typeof(string)}),"settings use the existing native RPC");
        foreach(string method in new[]{"接收地图数据_Rpc","发送消息广播_Rpc"})
        {
            var target=method=="发送消息广播_Rpc" ? AccessTools.Method(Native("联机玩家控件"),method,new[]{typeof(string),typeof(string)}) :
                AccessTools.DeclaredMethod(Native("联机玩家控件"),method);
            var code=PatchProcessor.GetOriginalInstructions(target);
            int send=code.FindIndex(c=>c.operand is MethodInfo call&&call.Name=="SendRPCInternal");
            Check(send>=0,"native RPC wrapper: "+method);
            var parameters=((MethodInfo)code[send].operand).GetParameters();
            int channel=Array.FindIndex(parameters,p=>p.Name=="channelId"||p.Name=="channel");
            Check(channel>=0&&Constant(code[send-1-(parameters.Length-1-channel)])==0,"settings and map RPCs use reliable channel zero: "+method);
        }
        var load=Body(Mod("ModuleSettings"),"Load");
        Check(load.FindIndex(c=>Calls(c,"ThemeBranchRuntime","Restore"))>=0&&
            load.FindIndex(c=>Calls(c,"ThemeBranchRuntime","Restore"))<load.FindIndex(c=>Calls(c,"ModuleSettings","EnterRun")),"branch continuation restores before the first settings publication");
        Check(Body(Mod("ModuleSettings"),"Save").Any(c=>Calls(c,"ThemeBranchRuntime","Capture"))&&
            Body(Mod("ModuleSettings"),"ApplyHostSettings").Any(c=>Calls(c,"ThemeBranchRuntime","SettingsReceived")),"existing module facade owns branch save and settings receipt");
        var settlement=AccessTools.DeclaredMethod(Mod("ThemeBranchRewardExit"),"Prefix");
        Check(settlement.GetCustomAttributes(typeof(HarmonyPriority),false).Cast<HarmonyPriority>().Any(x=>x.info.priority==Priority.Last),"reward return observes earlier scene-transition decisions");
        var settlementCode=PatchProcessor.GetOriginalInstructions(settlement);
        Check(settlementCode.Any(c=>Calls(c,"ThemeBranchRuntime","RewardsExited"))&&
            settlementCode.Any(c=>Calls(c,"SceneManager","GetActiveScene"))&&
            settlementCode.Any(c=>Field(c,"常数类","场景id_选择奖励"))&&settlementCode.Any(c=>Field(c,"常数类","场景id_选择地图"))&&
            settlement.GetParameters().Any(p=>p.Name=="__runOriginal"&&p.ParameterType==typeof(bool)),
            "battle completion is limited to reward scene 3 returning to map 4");
        Check(AccessTools.Method(Native("刷怪器"),"结算界面切换").GetMethodBody()!=null&&
            Body(Mod("ThemeBranchEventPanel"),"Bind").Any(c=>Calls(c,"联机_决策选择","决策选择")),"native rewards and cooperative decisions retain their original entries");

        var codec=Mod("ThemeMapCodec");
        var assets=codec.GetNestedType("MapAssetIds",BindingFlags.NonPublic);
        var assetConverter=Activator.CreateInstance(assets,true);
        foreach(string name in new[]{"关卡挑战模版","关卡奖励模版"})
            Check((bool)AccessTools.Method(assets,"CanConvert").Invoke(assetConverter,new object[]{Native(name)}),"asset ID converter handles "+name);
        var writeAssets=Body(assets,"WriteJson");
        Check(writeAssets.Any(c=>Field(c,"关卡挑战模版","id"))&&writeAssets.Any(c=>Field(c,"关卡奖励模版","id")),"map assets serialize their native IDs");
        var readAssets=Body(assets,"ReadJson");
        Check(readAssets.Any(c=>Calls(c,"数据库_关卡挑战","查询关卡挑战"))&&readAssets.Any(c=>Calls(c,"数据库_关卡奖励","查询关卡奖励")),"asset IDs resolve through native databases");

        MapRoundTrip(native,mod,codec,Check);
        Console.WriteLine($"PASS: {checks} final theme branch bindings, native sync/reward seams, map JSON and settings size checks");
    }

    private static int? Constant(CodeInstruction code)
    {
        if(code.opcode==OpCodes.Ldc_I4_0)return 0;
        if(code.opcode==OpCodes.Ldc_I4_3)return 3;
        if(code.opcode==OpCodes.Ldc_I4_4)return 4;
        if(code.opcode==OpCodes.Ldc_I4||code.opcode==OpCodes.Ldc_I4_S)return Convert.ToInt32(code.operand);
        return null;
    }

    private static void MapRoundTrip(Assembly native,Assembly mod,Type codec,Action<bool,string> check,bool sizeOnly=false)
    {
        object Empty(Type type)=>FormatterServices.GetUninitializedObject(type);
        FieldInfo Field(object item,string name)=>AccessTools.Field(item.GetType(),name)??throw new Exception("Missing native map field: "+name);
        void Set(object item,string name,object value)=>Field(item,name).SetValue(item,value);
        IList List(object item,string name)
        {
            var field=Field(item,name);var list=(IList)Activator.CreateInstance(field.FieldType);
            field.SetValue(item,list);return list;
        }
        var mapType=native.GetType("地图管理器+地图存档类",true);
        object map=Empty(mapType);
        var pages=List(map,"地图页列表");var pageInfos=List(map,"地图页信息列表");
        var infoType=Field(map,"地图页信息列表").FieldType.GetGenericArguments()[0];
        for(int page=0;page<9;page++)
        {
            string name="地图页_第"+(page+1)+"层_研究所_A";pages.Add(name);
            var info=Empty(infoType);Set(info,"地图页名",name);var nodes=List(info,"节点列表");
            for(int node=0;node<20;node++)nodes.Add("第"+(page+1)+"层_研究所_A_"+node);
            pageInfos.Add(info);
        }
        var walked=List(map,"走过的节点");walked.Add("第7层_研究所_A_0");walked.Add("第7层_研究所_A_11");
        var operations=List(map,"节点作用列表");var operationType=Field(map,"节点作用列表").FieldType.GetGenericArguments()[0];
        for(int node=0;node<20;node++){var entry=Empty(operationType);Set(entry,"节点名","第7层_研究所_A_"+node);operations.Add(entry);}
        Set(map,"当前地图页位置",6);Set(map,"当前地图页","地图页_第7层_研究所_A");Set(map,"当前节点名","第7层_研究所_A_11");
        Set(map,"当前场景名","焦香火山");Set(map,"待跳转场景id",4);
        Set(map,"是否位于事件",true);Set(map,"事件id",93);
        var encode=AccessTools.Method(codec,"Encode");var clone=AccessTools.Method(codec,"Clone");
        string main=(string)encode.Invoke(null,new[]{map});
        if(!sizeOnly)
        {
            object restored=clone.Invoke(null,new[]{map});
            check((string)encode.Invoke(null,new[]{restored})==main,"native map clone retains all serialized fields, nine pages and twenty nodes per page");
            check(!ReferenceEquals(Field(map,"地图页列表").GetValue(map),Field(restored,"地图页列表").GetValue(restored)),"restored route owns its map lists");
            foreach(var field in mapType.GetFields(BindingFlags.Public|BindingFlags.Instance).Where(f=>f.FieldType.Name=="关卡挑战模版"||f.FieldType.Name=="关卡奖励模版"))
                check(field.GetValue(restored)==null,"null native database reference round trip: "+field.Name);
        }

        var json=Assembly.Load("Newtonsoft.Json");var settings=AccessTools.Method(codec,"Settings").Invoke(null,null);
        var convert=json.GetType("Newtonsoft.Json.JsonConvert",true);var settingsType=settings.GetType();
        var serialize=convert.GetMethod("SerializeObject",new[]{typeof(object),settingsType});
        var deserialize=convert.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type),settingsType});
        var unity=Assembly.Load("UnityEngine.CoreModule");
        if(!sizeOnly)foreach(var sample in new[]{("UnityEngine.Vector2Int",new[]{11,-7},"[11,-7]"),("UnityEngine.Vector3Int",new[]{3,4,-5},"[3,4,-5]")})
        {
            var type=unity.GetType(sample.Item1,true);var vector=Activator.CreateInstance(type,sample.Item2.Cast<object>().ToArray());
            string encoded=(string)serialize.Invoke(null,new[]{vector,settings});
            object decoded=deserialize.Invoke(null,new object[]{encoded,type,settings});
            check(encoded==sample.Item3&&(string)serialize.Invoke(null,new[]{decoded,settings})==encoded,"production integer-grid converter round trip: "+type.Name);
        }
        object branchMap=clone.Invoke(null,new[]{map});Set(branchMap,"当前地图页","Cluckmech.Theme.Castle");Set(branchMap,"当前节点名","Cluckmech.Theme.Castle.2");
        string active=(string)encode.Invoke(null,new[]{branchMap});
        var progress=Activator.CreateInstance(mod.GetType("CluckmechCoop.RunProgress",true),true);
        var branch=AccessTools.Field(progress.GetType(),"Branch").GetValue(progress);
        Set(branch,"Used",true);Set(branch,"Active",true);Set(branch,"Theme",1);Set(branch,"Completed",1);
        Set(branch,"Token",Guid.NewGuid().ToString("N"));Set(branch,"MainMap",main);Set(branch,"ActiveMap",active);
        var message=Activator.CreateInstance(mod.GetType("CluckmechCoop.SessionMessage",true),true);
        Set(message,"Type","settings");Set(message,"Progress",progress);
        string settingsJson=(string)convert.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{message});
        int max=(int)AccessTools.Field(mod.GetType("CluckmechCoop.NetworkJson",true),"MaxLength").GetRawConstantValue();
        check(settingsJson.Length<max,"nine-page native return and branch maps fit one settings message: "+settingsJson.Length+"/"+max);
        var mirror=Assembly.Load("Mirror");
        var writerType=mirror.GetType("Mirror.NetworkWriter",true);
        var extensionType=mirror.GetType("Mirror.NetworkWriterExtensions",true);
        var write=extensionType.GetMethod("WriteString",BindingFlags.Public|BindingFlags.Static,null,new[]{writerType,typeof(string)},null);
        var limits=writerType.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static)
            .Concat(extensionType.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static))
            .Where(f=>f.IsLiteral&&f.Name.IndexOf("String",StringComparison.OrdinalIgnoreCase)>=0)
            .Select(f=>f.DeclaringType.Name+"."+f.Name+"="+f.GetRawConstantValue());
        int bytes=Encoding.UTF8.GetByteCount(settingsJson);
        Console.WriteLine($"Theme settings: characters={settingsJson.Length}, UTF8 bytes={bytes}, Mirror limits=[{string.Join(", ",limits)}]");
        check(write!=null,"actual Mirror WriteString method is available");
        var writer=Activator.CreateInstance(writerType);
        try{write.Invoke(null,new[]{writer,settingsJson});}
        catch(TargetInvocationException error)
        {
            Console.WriteLine("Mirror WriteString rejected settings: "+error.InnerException?.Message);
            throw error.InnerException??error;
        }
        int writerLength=AccessTools.Property(writerType,"Position") is PropertyInfo position ? (int)position.GetValue(writer) :
            (int)AccessTools.Field(writerType,"Position").GetValue(writer);
        Console.WriteLine("Theme settings actual Mirror writer length="+writerLength);
        check(writerLength>=bytes,"actual Mirror writer contains the entire UTF8 settings message");
    }
}
