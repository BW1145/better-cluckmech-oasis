using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace CluckmechCoop
{
    // Checked before rewriting: a changed game build must not receive a partial patch.
    internal static class TranspilerContract
    {
        internal static MethodInfo InstanceVoid(Type type, string name)
        {
            var method = AccessTools.DeclaredMethod(type, name, Type.EmptyTypes);
            if (method == null || method.IsStatic || method.ReturnType != typeof(void) || method.IsGenericMethod)
                throw new InvalidOperationException($"Incompatible game method: {type.FullName}.{name}; expected instance void ()");
            return method;
        }

        internal static List<CodeInstruction> Exact(IEnumerable<CodeInstruction> code, string patch, int expected, Func<CodeInstruction, bool> matches)
        {
            var instructions = code.ToList();
            int actual = instructions.Count(matches);
            if (actual != expected)
                throw new InvalidOperationException($"Incompatible game IL for {patch}: expected {expected} matches, found {actual}; patch refused");
            // A caller may pass this body to another transpiler; never modify its instructions.
            return instructions.Select(instruction => new CodeInstruction(instruction)).ToList();
        }
    }
}
