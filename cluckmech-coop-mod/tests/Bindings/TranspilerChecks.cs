using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

internal static class TranspilerChecks
{
    internal static void BronzeMinerals(Assembly original,Assembly mod,Action<List<CodeInstruction>> validateStack)
    {
        List<CodeInstruction> Body(Type type,string name)=>PatchProcessor.GetOriginalInstructions(AccessTools.DeclaredMethod(type,name));
        foreach(var type in mod.GetTypes().Where(t=>t.Name.StartsWith("Bronze")||t.Name.StartsWith("RegisterBronze")))
        foreach(HarmonyPatch attribute in type.GetCustomAttributes(typeof(HarmonyPatch),false))
            Require(AccessTools.Method(attribute.info.declaringType,attribute.info.methodName)!=null,"native bronze binding: "+type.Name);
        var registry=mod.GetType("CluckmechCoop.BronzeDragonRegistry",true);
        foreach(var overload in registry.GetMethods(BindingFlags.Static|BindingFlags.NonPublic).Where(m=>m.Name=="Install"))
            validateStack(PatchProcessor.GetOriginalInstructions(overload));
        var producer=mod.GetType("CluckmechCoop.BronzeDragon",true);
        var start=producer.GetNestedTypes(BindingFlags.NonPublic).Single(t=>t.Name.StartsWith("<Start>"));
        var begin=Body(start,"MoveNext");
        Require(begin.Any(c=>c.operand is MethodInfo m&&m.Name=="get_Role")&&begin.Any(c=>c.operand is FieldInfo f&&f.Name=="Resume")&&begin.Any(c=>c.operand is FieldInfo f&&f.Name=="Started"),"producer gates authority and resumes opening-reward state");
        var produce=Body(producer,"Produce");
        Require(produce.Count(c=>c.operand is MethodInfo m&&m.Name=="Broadcast")==1&&produce.Any(c=>c.operand is MethodInfo m&&m.Name=="Space"),"main-field coin batch uses one broadcast while private space retains its drop channel");
        var damage=mod.GetType("CluckmechCoop.StratagemDamage",true);
        var minerals=Body(damage,"EagleMinerals");
        Require(minerals.Count(c=>c.operand is MethodInfo m&&m.Name=="OverlapCircleAll")==1&&minerals.Any(c=>c.operand is MethodInfo m&&m.Name=="TryGetValue"),"mineral blast uses one overlap and deduplicates native roots");
        Require(minerals.Any(c=>c.operand is MethodInfo m&&m.Name=="EagleDamage")&&minerals.Count(c=>c.operand is MethodInfo m&&m.Name=="Space")==2,"mineral damage retains Eagle falloff and space separation");
        var unit=mod.GetType("CluckmechCoop.StratagemUnit",true);
        foreach(var iterator in unit.GetNestedTypes(BindingFlags.NonPublic).Where(t=>t.Name.StartsWith("<")&&AccessTools.DeclaredMethod(t,"MoveNext")!=null))
        {
            var body=Body(iterator,"MoveNext");
            int calls=body.Count(c=>c.operand is MethodInfo m&&m.Name=="EagleMinerals");
            Require(calls==(iterator.Name.StartsWith("<Eagle>")?1:0),"only Eagle adds mineral blast targets: "+iterator.Name);
        }
        var hit=AccessTools.DeclaredMethod(damage,"Hit");
        Require(hit.GetParameters().Last().Name=="mineral"&&Equals(hit.GetParameters().Last().DefaultValue,false),"shared hit retains enemy-only behavior by default");
        Require(Body(damage,"Hit").Any(c=>c.operand is MethodInfo m&&m.Name=="受到伤害"),"mineral damage uses the native hurt method");
        foreach(var body in new[]{produce,minerals,Body(damage,"Hit")})validateStack(body);
        Console.WriteLine("PASS: bronze native registry bindings, authority/opening state and coin routing; isolated Eagle mineral targeting, root deduplication, native damage and falloff");
    }
    internal static void FlameTrigger(Assembly mod,Action<List<CodeInstruction>> validateStack)
    {
        var actor=mod.GetType("CluckmechCoop.OtherworldActor",true);
        List<CodeInstruction> Body(Type type,string name)=>PatchProcessor.GetOriginalInstructions(AccessTools.DeclaredMethod(type,name));
        bool Call(CodeInstruction c,string name)=>c.operand is MethodInfo m&&m.DeclaringType==actor&&m.Name==name;
        var phase=AccessTools.Field(actor,"phaseFromReturn");
        var begin=Body(actor,"BeginPhase");
        int mark=begin.FindIndex(c=>c.opcode==OpCodes.Stfld&&Equals(c.operand,phase));
        Require(mark>0&&begin[mark-1].opcode==OpCodes.Ldarg_1,"each phase replaces the return marker; normal short presses reset it");
        var end=Body(actor,"EndPhase");
        Require(end.Any(c=>c.opcode==OpCodes.Ldarg_2)&&end.Any(c=>c.opcode==OpCodes.Ldfld&&Equals(c.operand,phase))&&end.Count(c=>Call(c,"EndAction"))==1,"phase completion forwards the flame permission and return marker to one action completion");
        var finish=Body(actor,"FinishDash");
        int space=finish.FindIndex(c=>Call(c,"get_Space"));
        Require(space>=0&&finish.Skip(space).Any(c=>Call(c,"EndPhase")),"dash completion uses successful space entry to permit the flame");
        var back=Body(mod.GetType("CluckmechCoop.KamuiWorld",true),"Return");
        int action=back.FindIndex(c=>Call(c,"EndAction"));
        Require(action>0&&back[action-1].opcode==OpCodes.Ldc_I4_0,"immediate return preserves action completion without emitting a flame");
        int continued=back.FindIndex(c=>Call(c,"BeginPhase"));
        Require(continued>0&&back[continued-1].opcode==OpCodes.Ldc_I4_1,"return protection is marked for its delayed completion");
        var complete=Body(actor,"EndAction");
        int permission=complete.FindIndex(c=>c.opcode==OpCodes.Ldarg_1);
        Require(permission>complete.FindIndex(c=>c.operand is MethodInfo m&&m.Name=="Counter")&&complete.Skip(permission).Any(c=>c.operand is MethodInfo m&&m.Name=="Flame"),"the flame gate preserves counter and rally processing");
        foreach(var code in new[]{begin,end,finish,back,complete})validateStack(code);
        Console.WriteLine("PASS: compiled short-press reset, successful-entry flame, immediate/delayed return suppression and retained counter action paths");
    }
    internal static void SettlementRepairs(Assembly original,Assembly mod,Action<List<CodeInstruction>> validateStack)
    {
        List<CodeInstruction> Body(Type type,string name)=>PatchProcessor.GetOriginalInstructions(AccessTools.DeclaredMethod(type,name));
        bool Call(CodeInstruction c,string type,string name)=>c.operand is MethodInfo m&&m.DeclaringType.Name==type&&m.Name==name;
        var target=AccessTools.DeclaredMethod(original.GetType("模块_玩家_死亡",true),"玩家死亡");
        var repair=mod.GetType("CluckmechCoop.RepairPlayerKillNotification",true);
        var patch=AccessTools.DeclaredMethod(repair,"Transpiler");
        var before=PatchProcessor.GetOriginalInstructions(target);var after=Apply(patch,before,target);validateStack(after);
        var send=AccessTools.DeclaredMethod(repair,"Send");
        Require(after.Count(c=>c.Calls(send))==1&&after.Count==before.Count,"player death replaces exactly one notification, retaining the full native body");
        Require(before.Any(c=>c.opcode==OpCodes.Ldstr&&Equals(c.operand,"击杀判断")),"native notification message is present");
        var nativeSend=AccessTools.Method(AccessTools.TypeByName("UnityEngine.GameObject"),"SendMessage",new[]{typeof(string),typeof(object)});
        ExpectInvalid(()=>Apply(patch,before.Where(c=>!c.Calls(nativeSend)),target),"Player kill notification");
        var duplicate=before.Select(c=>new CodeInstruction(c)).ToList();duplicate.Add(new CodeInstruction(OpCodes.Callvirt,nativeSend));
        ExpectInvalid(()=>Apply(patch,duplicate,target),"Player kill notification");
        Require(Body(repair,"Send").Any(c=>Call(c,"Transform","get_position")),"kill notification passes the victim position expected by native receivers");
        var fox=mod.GetType("CluckmechCoop.RepairFoxFriendsInitialization",true);
        foreach(var binding in new[]{("___攻击属性","模块_攻击属性"),("___光环","模块_光环")})
        {
            var native=AccessTools.Field(original.GetType("词条_342_狐朋狗友",true),binding.Item1.Substring(3));
            var parameter=AccessTools.DeclaredMethod(fox,"Prefix").GetParameters().Single(p=>p.Name==binding.Item1);
            Require(native.FieldType.MakeByRefType()==parameter.ParameterType,"fox initialization field binding: "+binding.Item1);
            Require(Body(fox,"Prefix").Any(c=>c.operand is MethodInfo m&&m.Name=="GetComponent"&&m.IsGenericMethod&&m.GetGenericArguments()[0].Name==binding.Item2),"fox retrieves existing "+binding.Item2);
        }
        var foxBody=Body(fox,"Prefix");
        int initialize=foxBody.FindIndex(c=>Call(c,"模块_光环","初始化"));
        int range=foxBody.FindIndex(c=>Call(c,"模块_光环","光环优化"));
        int refresh=foxBody.FindIndex(c=>Call(c,"模块_光环","检索光环内友方"));
        Require(initialize>=0&&range>initialize&&refresh>range,"fox initializes and refreshes current allies before its one-time native bonus calculation");
        var screen=original.GetType("UI_结算场景信息显示",true);
        Require(AccessTools.DeclaredMethod(screen,"更新角色")!=null&&AccessTools.Field(screen,"总结")!=null&&AccessTools.Field(screen,"角色名字UI")!=null,"settlement patch binds actual native refresh and name fields");
        var portraits=mod.GetType("CluckmechCoop.OtherworldScenePortraits",true);
        Require(Body(portraits,"Prefix").Any(c=>c.opcode==OpCodes.Isinst&&Equals(c.operand,screen)),"settlement selects the same layered portrait path as pause");
        var portrait=Body(portraits,"Portrait");
        foreach(string child in new[]{"角色","手"})Require(portrait.Any(c=>c.opcode==OpCodes.Ldstr&&Equals(c.operand,child)),"layered portrait addresses native child "+child);
        Require(portrait.Any(c=>Call(c,"Behaviour","set_enabled"))&&portrait.Any(c=>Call(c,"Image","set_overrideSprite")),"portrait disables animation writers and replaces overridden image");
        var actions=mod.GetType("CluckmechCoop.OtherworldActions",true);
        var flame=actions.GetNestedTypes(BindingFlags.NonPublic).Single(t=>t.Name.StartsWith("<Flame>"));
        var flameBody=Body(flame,"MoveNext");
        int consume=flameBody.FindIndex(c=>Call(c,"OtherworldBurnSettlement","Consume"));
        var statusDamage=original.GetType("模块_状态持续效果+状态伤害类",true);
        Require(consume>=0&&flameBody.Skip(consume).Any(c=>c.operand is MethodInfo m&&m.Name=="受到伤害"&&m.GetParameters().SingleOrDefault()?.ParameterType==statusDamage),"consumed burn uses native noncritical status damage overload");
        foreach(string field in new[]{"最近攻击者","最近受到伤害"})
            Require(flameBody.Skip(consume).Any(c=>c.opcode==OpCodes.Stfld&&c.operand is FieldInfo f&&f.Name==field),"burn settlement retains native kill attribution: "+field);
        Require(flameBody.Any(c=>Call(c,"HashSet`1","Add")),"settlement remains once per enemy per flame activation");
        Require(Body(mod.GetType("CluckmechCoop.OtherworldStatusDamage",true),"Prefix").All(c=>c.operand is not FieldInfo f||!f.Name.Contains("暴击")),"status damage modifier adds no critical multiplier");
        Console.WriteLine("PASS: native death IL/stack and contract, fox field bindings, settlement layered portrait/name and single noncritical burn-settlement wiring");
    }
    internal static void Shop(Assembly original,Assembly mod,Action<List<CodeInstruction>> validateStack)
    {
        var method=AccessTools.DeclaredMethod(original.GetType("商店脚本",true),"商店商品运算");
        var owner=mod.GetType("CluckmechCoop.NewShopStock",true);
        var patch=AccessTools.DeclaredMethod(owner,"Transpiler");
        var before=PatchProcessor.GetOriginalInstructions(method);var after=Apply(patch,before,method);
        validateStack(after);
        foreach(string helper in new[]{"Preferences","PreferredOffer","SpecialOfferCount"})
            Require(after.Count(c=>c.Calls(AccessTools.DeclaredMethod(owner,helper)))==1,"Shop preserved helper: "+helper);
        var weights=AccessTools.Field(original.GetType("角色天赋数据模板",true),"商店权重加成列表");
        Require(before.Count(c=>c.opcode==OpCodes.Ldfld&&Equals(c.operand,weights))==1,"original preference seam unchanged");
        Require(!after.Any(c=>c.opcode==OpCodes.Ldfld&&Equals(c.operand,weights)),"null talent field dereference replaced");
        var empty=AccessTools.DeclaredMethod(owner,"Preferences").Invoke(null,new object[]{null}) as System.Collections.IList;
        Require(empty!=null&&empty.Count==0,"null initial talent produces empty preference without dereference");
        ExpectInvalid(()=>Apply(patch,before.Where(c=>!Equals(c.operand,weights)),method),"Shop talent preferences");
        var extra=before.Select(c=>new CodeInstruction(c)).ToList();extra.Add(new CodeInstruction(OpCodes.Ldfld,weights));
        ExpectInvalid(()=>Apply(patch,extra,method),"Shop talent preferences");
        var progress=mod.GetType("CluckmechCoop.OtherworldProgress",true);
        var restore=AccessTools.Method(progress,"RestoreNative",Type.EmptyTypes);
        Require(PatchProcessor.GetOriginalInstructions(AccessTools.Method(mod.GetType("CluckmechCoop.KamuiWorld",true),"Load")).Any(c=>c.Calls(restore)),"load rebinds native talent references before later UI reads");
        Console.WriteLine("PASS: shop null-talent fallback, retained draw helpers, IL stack, missing/duplicate seams and load rebind wiring");
    }
    internal static void DashChain(Assembly mod)
    {
        var actor=mod.GetType("CluckmechCoop.OtherworldActor",true);
        var world=mod.GetType("CluckmechCoop.KamuiWorld",true);
        List<CodeInstruction> Body(Type type,string name)=>PatchProcessor.GetOriginalInstructions(AccessTools.DeclaredMethod(type,name));
        bool IsCall(CodeInstruction instruction,string type,string name)=>instruction.operand is MethodInfo method&&method.DeclaringType.Name==type&&method.Name==name;
        var iterator=world.GetNestedTypes(BindingFlags.NonPublic).Single(t=>t.Name.StartsWith("<DashRoutine>"));
        var move=Body(iterator,"MoveNext");
        int capture=move.FindIndex(c=>IsCall(c,"KamuiWorld","CaptureDashSegment"));
        int transfer=move.FindIndex(c=>IsCall(c,"KamuiWorld","Transfer"));
        Require(capture>=0&&transfer>capture,"segment enemies are captured before final player transfer");
        Require(move.Any(c=>IsCall(c,"OtherworldActor","get_CanContinueDash"))&&move.Any(c=>IsCall(c,"OtherworldActor","StartDashSegment")),"authoritative chain gates every segment");
        Require(Body(actor,"get_MovementIntent").Any(c=>IsCall(c,"InputAction","ReadValue")),"local direction samples live action while native translation is suspended");
        foreach(string method in new[]{"FinishDash","CancelClientDash"})
            Require(Body(actor,method).Any(c=>IsCall(c,"OtherworldDashNetwork","ClearHistory")),"dash exit clears interpolation history: "+method);
        var localDash=actor.GetNestedTypes(BindingFlags.NonPublic).Single(t=>t.Name.StartsWith("<DashLocally>"));
        Require(Body(localDash,"MoveNext").Any(c=>IsCall(c,"OtherworldDashNetwork","ClearHistory")),"client segment end clears interpolation history");
        Require(move.Any(c=>c.opcode==OpCodes.Ldc_R4&&Equals(c.operand,.2f)),"server interval contains the requested 0.2 game seconds");
        Require(move.Any(c=>IsCall(c,"OtherworldMotion","DashTravel"))&&Body(localDash,"MoveNext").Any(c=>IsCall(c,"OtherworldMotion","DashTravel")),"both peers use the same acceleration curve");
        Require(!Body(localDash,"MoveNext").Any(c=>c.opcode==OpCodes.Stfld&&c.operand is FieldInfo f&&f.Name=="Dashing"),"client keeps transform ownership through the segment interval");
        var receive=Body(mod.GetType("CluckmechCoop.OtherworldNetwork",true),"Receive");
        Require(receive.Any(c=>IsCall(c,"OtherworldActor","CancelClientDash")),"authoritative end message releases client interval ownership");
        var path=Body(mod.GetType("CluckmechCoop.KamuiNavigation",true),"TryDash");
        Require(path.Any(c=>IsCall(c,"KamuiDashPath","ReachableFraction")),"navigation uses wall clipping rather than movement-distance cancellation");
        var captureBody=Body(world,"CaptureDashSegment");
        foreach(string field in new[]{"Origin","Until"})
            Require(captureBody.Any(c=>c.opcode==OpCodes.Stfld&&c.operand is FieldInfo f&&f.DeclaringType.Name=="KamuiCapture"&&f.Name==field),"each enemy retains own "+field);
        var begin=Body(actor,"BeginPhase");
        Require(begin.Count(c=>IsCall(c,"OtherworldAudio","Play"))==1,"successful activation has one voice entry");
        foreach(var body in new[]{move,captureBody,Body(actor,"StartDashSegment"),Body(world,"Return")})
            Require(!body.Any(c=>IsCall(c,"OtherworldAudio","Play")),"segments, capture and return do not replay voice");
        var counter=Body(mod.GetType("CluckmechCoop.OtherworldActions",true),"Counter");
        Require(counter.Count(c=>IsCall(c,"OtherworldActor","ReduceCooldown"))==1&&counter.FindIndex(c=>IsCall(c,"OtherworldActor","ReduceCooldown"))>counter.FindIndex(c=>IsCall(c,"OtherworldActions","Hit")),"counter cooldown reward has one post-hit entry");
        var state=Body(mod.GetType("CluckmechCoop.OtherworldNetwork",true),"State");
        Require(state.Any(c=>c.opcode==OpCodes.Stfld&&c.operand is FieldInfo f&&f.Name=="Amount")&&state.Any(c=>IsCall(c,"OtherworldActor","get_Cooldown")),"state transmits actual cooldown total");
        Console.WriteLine("PASS: compiled segmented capture/player order, live input, per-enemy timers, single voice entry, counter reward and network cooldown wiring");
    }
    internal static void EnergyAudio(Assembly original,Assembly mod)
    {
        List<CodeInstruction> Body(Type type,string name)=>PatchProcessor.GetOriginalInstructions(AccessTools.DeclaredMethod(type,name));
        bool Call(CodeInstruction c,string type,string name)=>c.operand is MethodInfo m&&m.DeclaringType.Name==type&&m.Name==name;
        var combat=mod.GetType("CluckmechCoop.CombatFeatures",true);
        var start=Body(combat,"StartEnergy");
        int paid=start.FindIndex(c=>Call(c,"RunProgress","UseEnergy"));
        int healed=start.FindIndex(c=>c.opcode==OpCodes.Stfld&&c.operand is FieldInfo f&&f.Name=="当前生命值");
        Require(paid>=0&&healed>paid&&start.Take(healed).Any(c=>c.opcode==OpCodes.Ldfld&&c.operand is FieldInfo f&&f.Name=="当前生命值上限"),"activation consumes one use before restoring current maximum health");
        Require(!Body(combat,"ApplyEnergy").Any(c=>c.opcode==OpCodes.Stfld&&c.operand is FieldInfo f&&f.Name=="当前生命值"),"re-sync and expiration do not heal again");
        var attack=Body(mod.GetType("CluckmechCoop.ExtraAttack",true),"Postfix");
        Require(attack.Any(c=>c.opcode==OpCodes.Ldc_R4&&Equals(c.operand,1000f)),"active base attack bonus is 1000");
        var nativeSync=Body(original.GetType("联机_单位属性同步",true),"周期同步");
        Require(nativeSync.Any(c=>Call(c,"联机_单位属性同步","set_Network当前生命值")),"native host health replication retains base restoration");
        var world=mod.GetType("CluckmechCoop.KamuiWorld",true);
        foreach(string name in new[]{"CaptureDashSegment","Return","Tick"})
            Require(Body(world,name).Count(c=>Call(c,"OtherworldAudio","Transfer"))==1,"one sound event per transfer batch: "+name);
        Require(!Body(world,"Transfer").Any(c=>Call(c,"OtherworldAudio","Transfer")),"individual unit and replicated transfers do not duplicate sounds");
        var receive=Body(mod.GetType("CluckmechCoop.OtherworldNetwork",true),"Receive");
        int sound=receive.FindIndex(c=>Call(c,"OtherworldAudio","ReceiveTransfer"));
        int identity=receive.FindIndex(c=>Call(c,"NetworkClient","get_spawned"));
        if(identity<0)identity=receive.FindIndex(c=>c.opcode==OpCodes.Ldsfld&&c.operand is FieldInfo f&&f.DeclaringType.Name=="NetworkClient"&&f.Name=="spawned");
        Require(sound>=0&&identity>sound,"batch sound reception is independent of hidden unit identities");
        Console.WriteLine("PASS: compiled one-time energy restoration, +1000 attack, native health replication and batched transfer sound reception");
    }
    private sealed class Binding
    {
        internal string Type, Target, Patch, SourceType, Source, Helper;
        internal int Count;
        internal bool Field;
        internal Binding(string type, string target, string patch, string sourceType, string source, string helper, int count, bool field = false)
        { Type = type; Target = target; Patch = patch; SourceType = sourceType; Source = source; Helper = helper; Count = count; Field = field; }
    }

    internal static void Run(Assembly original, Assembly mod, Action<List<CodeInstruction>> validateStack)
    {
        var bindings = new[]
        {
            new Binding("模块_攻击", "检索目标", "KamuiTargetCandidates", "UnityEngine.GameObject", "FindGameObjectsWithTag", "Tagged", 1),
            new Binding("模块_玩家_攻击", "检索目标", "KamuiTargetCandidates", "UnityEngine.GameObject", "FindGameObjectsWithTag", "Tagged", 1),
            new Binding("模块_光环", "检索光环内敌方", "KamuiAuraCandidates", "世界事件系统", "场上所有单位", "LocalUnits", 1, true),
            new Binding("模块_光环", "检索光环内友方", "KamuiAuraCandidates", "世界事件系统", "场上所有单位", "LocalUnits", 1, true),
            new Binding("模块_基本属性", "运算生命值上限", "OtherworldHealth", "模块_基本属性", "生命值倍率", "Health", 1, true),
            new Binding("模块_攻击属性", "运算攻击力", "OtherworldAttackStats", "模块_攻击属性", "攻击力", "BaseAttack", 2, true),
            new Binding("模块_攻击属性", "运算攻击速度", "OtherworldAttackRate", "模块_攻击属性", "变化百分比攻击速度", "Rate", 3, true),
            new Binding("建造系统", "开始拖动", "OtherworldWorkshopPower", "模块_基本属性", "花费", "Power", 1, true),
            new Binding("建造系统", "结束拖动延迟", "OtherworldWorkshopPower", "模块_基本属性", "花费", "Power", 2, true),
            new Binding("建造系统", "开始拖动", "StartStratagemDrag", "dw单位库", "获取单位", "SelectedPrefab", 1)
        };
        int cases = 0;
        foreach (var binding in bindings)
        {
            var method = AccessTools.DeclaredMethod(original.GetType(binding.Type, true), binding.Target, Type.EmptyTypes);
            Require(method != null && !method.IsStatic && method.ReturnType == typeof(void), "Target signature: " + binding.Patch);
            var patchType = mod.GetType("CluckmechCoop." + binding.Patch, true);
            var targetList = AccessTools.DeclaredMethod(patchType, "TargetMethods");
            var declaredTargets = targetList == null
                ? new[] { (MethodBase)AccessTools.DeclaredMethod(patchType, "TargetMethod").Invoke(null, null) }
                : ((IEnumerable<MethodBase>)targetList.Invoke(null, null)).ToArray();
            Require(declaredTargets.Contains(method), "Patch bound to wrong target: " + binding.Patch);
            Require(declaredTargets.All(target => !target.IsStatic && ((MethodInfo)target).ReturnType == typeof(void) && target.GetParameters().Length == 0), "Declared target signature");
            var patch = AccessTools.DeclaredMethod(patchType, "Transpiler");
            var before = PatchProcessor.GetOriginalInstructions(method);
            Func<CodeInstruction, bool> seam = instruction => instruction.operand is MemberInfo member
                && member.DeclaringType?.FullName == binding.SourceType && member.Name == binding.Source
                && (binding.Field ? instruction.opcode == OpCodes.Ldfld || instruction.opcode == OpCodes.Ldsfld : instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt);
            Require(before.Count(seam) == binding.Count, "Native seam count: " + binding.Patch);
            string[] unchanged = Fingerprint(before);
            var after = Apply(patch, before, method);
            Require(unchanged.SequenceEqual(Fingerprint(before)), "Transpiler modified caller body: " + binding.Patch);
            var helpers = after.Where(instruction => instruction.operand is MethodInfo call && call.DeclaringType.Assembly == mod && call.Name == binding.Helper).ToList();
            Require(helpers.Count == binding.Count, "Replacement count: " + binding.Patch);
            ValidateHelper(binding, before.First(seam), (MethodInfo)helpers[0].operand);
            validateStack(after);
            Require(Fingerprint(Expected(before, seam, (MethodInfo)helpers[0].operand, binding)).SequenceEqual(Fingerprint(after)), "Native instruction, branch or exception boundary changed: " + binding.Patch);
            cases++;

            // Reject before mutation, including when other valid matches remain.
            var missing = before.Where((instruction, index) => index != before.FindIndex(item => seam(item))).ToList();
            ExpectMismatch(patch, missing, method, binding.Patch, binding.Count - 1);
            cases++;
            var duplicate = before.Concat(new[] { new CodeInstruction(before.First(seam)) }).ToList();
            ExpectMismatch(patch, duplicate, method, binding.Patch, binding.Count + 1);
            cases++;
        }

        var drag = AccessTools.DeclaredMethod(original.GetType("建造系统", true), "开始拖动", Type.EmptyTypes);
        var power = AccessTools.DeclaredMethod(mod.GetType("CluckmechCoop.OtherworldWorkshopPower", true), "Transpiler");
        var selected = AccessTools.DeclaredMethod(mod.GetType("CluckmechCoop.StartStratagemDrag", true), "Transpiler");
        var native = PatchProcessor.GetOriginalInstructions(drag);
        var powerFirst = Apply(selected, Apply(power, native, drag), drag);
        var selectedFirst = Apply(power, Apply(selected, native, drag), drag);
        Require(Fingerprint(powerFirst).SequenceEqual(Fingerprint(selectedFirst)), "Drag patches depend on unspecified order");
        validateStack(powerFirst);
        Require(powerFirst.Count(instruction => instruction.operand is MethodInfo call && call.Name == "Power") == 1
            && powerFirst.Count(instruction => instruction.operand is MethodInfo call && call.Name == "SelectedPrefab") == 1, "Combined drag replacement count");
        cases++;

        // Card derivation follows native and Stratagem cooldown setup. The drag
        // transpilers are independent and need no artificial priority constraint.
        var cards = AccessTools.DeclaredMethod(mod.GetType("CluckmechCoop.OtherworldWorkshopCards", true), "Postfix");
        var priority = (HarmonyPriority)cards.GetCustomAttributes(typeof(HarmonyPriority), false).Single();
        Require(priority.info.priority == Priority.Last, "Workshop cards must retain last postfix priority");
        cases++;
        var targetContract = AccessTools.DeclaredMethod(mod.GetType("CluckmechCoop.TranspilerContract", true), "InstanceVoid");
        foreach (string name in new[] { nameof(SignatureFixture.Static), nameof(SignatureFixture.Value), nameof(SignatureFixture.Argument) })
        {
            ExpectInvalid(() => targetContract.Invoke(null, new object[] { typeof(SignatureFixture), name }), "Incompatible game method");
            cases++;
        }
        Console.WriteLine($"PASS: {cases} transpiler cases: 10 native bodies/signatures/stacks and exact retained IL, 20 missing/duplicate seams, both drag orders, card priority and 3 rejected signatures");
    }

    private static List<CodeInstruction> Apply(MethodInfo patch, IEnumerable<CodeInstruction> code, MethodBase method)
    {
        object[] args = patch.GetParameters().Length == 2 ? new object[] { code, method } : new object[] { code };
        return ((IEnumerable<CodeInstruction>)patch.Invoke(null, args)).ToList();
    }

    private static List<CodeInstruction> Expected(List<CodeInstruction> before, Func<CodeInstruction, bool> seam, MethodInfo helper, Binding binding)
    {
        var expected = new List<CodeInstruction>();
        foreach (var instruction in before)
        {
            if (!seam(instruction)) { expected.Add(instruction); continue; }
            if (binding.Helper == "Tagged" || binding.Helper == "LocalUnits")
            {
                var owner = new CodeInstruction(OpCodes.Ldarg_0);
                owner.labels.AddRange(instruction.labels);
                owner.blocks.AddRange(instruction.blocks);
                expected.Add(owner);
            }
            else if (binding.Helper == "SelectedPrefab")
            {
                expected.Add(new CodeInstruction(instruction) { opcode = OpCodes.Call, operand = helper });
                continue;
            }
            else
            {
                expected.Add(instruction);
                if (binding.Helper != "Power") expected.Add(new CodeInstruction(OpCodes.Ldarg_0));
            }
            expected.Add(new CodeInstruction(OpCodes.Call, helper));
        }
        return expected;
    }

    private static void ValidateHelper(Binding binding, CodeInstruction source, MethodInfo helper)
    {
        Type result;
        Type[] args;
        if (source.operand is FieldInfo field)
        {
            result = field.FieldType;
            args = binding.Helper == "LocalUnits" ? new[] { AccessTools.TypeByName("UnityEngine.Component") }
                : binding.Helper == "Power" ? new[] { result } : new[] { result, field.DeclaringType };
        }
        else
        {
            var call = (MethodInfo)source.operand;
            result = call.ReturnType;
            args = call.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
            args = call.IsStatic ? args.Concat(new[] { AccessTools.TypeByName("UnityEngine.Component") }).ToArray()
                : new[] { call.DeclaringType }.Concat(args).ToArray();
        }
        Require(helper.IsStatic && helper.ReturnType == result && helper.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(args), "Helper signature: " + binding.Helper);
    }

    private static void ExpectMismatch(MethodInfo patch, List<CodeInstruction> code, MethodBase target, string patchName, int actual)
    {
        var before = Fingerprint(code);
        ExpectInvalid(() => Apply(patch, code, target), "found " + actual + "; patch refused");
        Require(before.SequenceEqual(Fingerprint(code)), "Rejected patch mutated body: " + patchName);
    }

    private static void ExpectInvalid(Action action, string message)
    {
        try { action(); }
        catch (Exception error)
        {
            while (error is TargetInvocationException && error.InnerException != null) error = error.InnerException;
            if (error is InvalidOperationException && error.Message.Contains(message)) return;
            throw new Exception("Wrong contract failure; expected " + message, error);
        }
        throw new Exception("Expected contract failure: " + message);
    }

    private static string[] Fingerprint(IEnumerable<CodeInstruction> code) => code.Select(instruction =>
        instruction.opcode + "|" + Operand(instruction.operand) + "|" + string.Join(",", instruction.labels.Select(label => label.GetHashCode()))
        + "|" + string.Join(",", instruction.blocks.Select(block => block.blockType + ":" + block.catchType))).ToArray();

    private static string Operand(object operand) => operand is Type type ? type.FullName
        : operand is MemberInfo member ? member.DeclaringType?.FullName + ":" + member
        : operand is Label[] labels ? string.Join(",", labels.Select(label => label.GetHashCode())) : operand?.ToString();

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class SignatureFixture
    {
        public static void Static() { }
        public int Value() => 0;
        public void Argument(int value) { }
    }
}
