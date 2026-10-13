using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CluckmechSkipSplash
{
    public static class Patcher
    {
        public static IEnumerable<string> TargetDLLs { get { yield return "Unity.2D.Animation.Runtime.dll"; } }
        public static void Patch(AssemblyDefinition assembly)
        {
            string config = Path.Combine(Path.GetDirectoryName(typeof(Patcher).Assembly.Location), "..", "config", "billy.cluckmech.coop.cfg");
            if (!ReadEnabled(config)) return;
            var entry = assembly.MainModule.Types.Single(t => t.Name == "$BurstDirectCallInitializer").Methods.Single(m => m.Name == "Initialize");
            string corePath = Path.Combine(Path.GetDirectoryName(assembly.MainModule.FileName), "UnityEngine.CoreModule.dll");
            using (var core = AssemblyDefinition.ReadAssembly(corePath))
            {
                var splash = core.MainModule.Types.Single(t => t.FullName == "UnityEngine.Rendering.SplashScreen");
                var stop = assembly.MainModule.ImportReference(splash.Methods.Single(m => m.Name == "Stop"));
                if (entry.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.FullName == stop.FullName)) return;
                var il = entry.Body.GetILProcessor();
                var first = entry.Body.Instructions[0];
                il.InsertBefore(first, il.Create(OpCodes.Ldc_I4_0));
                il.InsertBefore(first, il.Create(OpCodes.Call, stop));
            }
        }
        internal static bool ReadEnabled(string file)
        {
            if (!File.Exists(file)) return true;
            bool modules = false;
            foreach (string line in File.ReadLines(file))
            {
                string value = line.Trim();
                if (value.StartsWith("[")) { modules = value == "[Modules]"; continue; }
                int equals = value.IndexOf('=');
                if (!modules || equals < 0 || value.Substring(0, equals).Trim() != "SkipSplash") continue;
                return !bool.TryParse(value.Substring(equals + 1).Trim(), out bool enabled) || enabled;
            }
            return true;
        }
    }
}
