using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

internal static class TranspilerChecks
{
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
