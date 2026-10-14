using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CluckmechCoop;
namespace CluckmechCoop
{
    internal static class StratagemDeployment
    {
        internal static int Requests,Carried,AppliedCount;
        internal static void Begin() { }
        internal static void Capture() { }
        internal static void Carry(int role,Machine[] cards) { Carried++; }
        internal static void Place(int role,StratagemPlacement request) { Requests++; }
        internal static void Applied(int role,int sequence,int slot,float delay,float power,bool accepted) { AppliedCount++; }
    }
    internal static class StratagemEffects
    {
        internal static int Count;
        internal static void Play(StratagemEffectMessage message) { Count++; }
    }
}
static class StratagemHarness
{
    internal static void Run(Action<bool,string> check)
    {
        var oldHost = Harness.Host; var oldGuest = Harness.Guest;
        Harness.Host = new Context(1); Harness.Guest = new Context(2);
        try
        {
            using (Harness.Use(Harness.Host))
            {
                ModuleSettings.MainMenu(); ModuleSettings.EnterRun();
                Harness.Host.Save.后缀 = ".stratagem.cd";
                Plugin.Combat.Token = "stratagem-battle"; Plugin.Network.PublishSettings();
                var machine = new Machine { Id = 20004, UpgradeLimit = 7, Upgrades = new List<int> {25043,25044} };
                var request = new SessionMessage { Type = "stratagem-place", Token = Plugin.Combat.Token,
                    Placement = new StratagemPlacement { Sequence = 1, Slot = 3, Machine = machine, X = 5, Y = -2 } };
                int before = StratagemDeployment.Requests;
                Plugin.Network.ServerReceive(Steam联机管理器.玩家_客户端,JsonConvert.SerializeObject(request));
                check(StratagemDeployment.Requests == before+1,"valid guest deployment reaches authoritative placement");
                foreach (var change in new Action<SessionMessage>[] {
                    x => x.Token = "old-battle", x => x.Placement.Slot = -1, x => x.Placement.Slot = 5,
                    x => x.Placement.Sequence = 0, x => x.Placement.Machine.Id = 1, x => x.Placement.Machine.Upgrades = null,
                    x => x.Placement.X = float.NaN, x => x.Placement = null })
                {
                    var invalid = JsonConvert.DeserializeObject<SessionMessage>(JsonConvert.SerializeObject(request)); change(invalid);
                    before = StratagemDeployment.Requests;
                    Plugin.Network.ServerReceive(Steam联机管理器.玩家_客户端,JsonConvert.SerializeObject(invalid));
                    check(StratagemDeployment.Requests == before,"invalid deployment cannot reach placement");
                }
                ModuleSettings.Extra.Stratagems = false; before = StratagemDeployment.Requests;
                Plugin.Network.ServerReceive(Steam联机管理器.玩家_客户端,JsonConvert.SerializeObject(request));
                check(StratagemDeployment.Requests == before,"disabled module rejects new deployments");
                ModuleSettings.Extra.Stratagems = true;
                var cards = new SessionMessage { Type = "stratagem-carry", Token = Plugin.Combat.Token, Cards = new[] {machine,null,null,null,null} };
                before = StratagemDeployment.Carried; Plugin.Network.ServerReceive(Steam联机管理器.玩家_客户端,JsonConvert.SerializeObject(cards));
                check(StratagemDeployment.Carried == before+1,"five-slot carry snapshot reaches validated deck handler");
                cards.Cards = new[] {machine}; before = StratagemDeployment.Carried;
                Plugin.Network.ServerReceive(Steam联机管理器.玩家_客户端,JsonConvert.SerializeObject(cards));
                check(StratagemDeployment.Carried == before,"incorrect carry width rejected");
                Plugin.Network.PublishSettings();
                int applied = StratagemDeployment.AppliedCount, effects = StratagemEffects.Count;
                Plugin.Network.Broadcast(new SessionMessage { Type = "stratagem-placed", Token = Plugin.Combat.Token, Role = 2, Revision = 1, Stage = 3, Delay = 45, X = 8, Held = true });
                Plugin.Network.Broadcast(new SessionMessage { Type = "stratagem-effect", Token = Plugin.Combat.Token, Effect = new StratagemEffectMessage {Kind = 3,Action = 8,Radius = 4,Duration = 1} });
                check(StratagemDeployment.AppliedCount == applied+1 && StratagemEffects.Count == effects+1,"guest receives placement acknowledgement and effect separately");
                Plugin.Network.Broadcast(new SessionMessage { Type = "stratagem-effect", Token = "old-battle", Effect = new StratagemEffectMessage {Kind = 3,Action = 8,Radius = 4,Duration = 1} });
                check(StratagemEffects.Count == effects+1,"old battle effect ignored");
                var progress = ModuleSettings.Progress.Player(1);
                progress.StratagemCooldowns.Add(new StratagemCooldown {Node = "page:node",Slot = 3,Machine = machine,Remaining = 21.5f});
                ModuleSettings.Save(Harness.Host.Save); ModuleSettings.MainMenu(); ModuleSettings.Load(Harness.Host.Save);
                var restored = ModuleSettings.Progress.Player(1).StratagemCooldowns[0];
                check(restored.Slot == 3 && restored.Remaining == 21.5f && Rules.SameMachine(restored.Machine,machine),"saved cooldown retains exact upgraded card and slot");
                string key = "lianji_m.stratagem.cdCluckmechCoop.Progress.v4";
                var old = JObject.Parse((string)ES3.Data[key]);
                ((JObject)old["Players"]["1"]).Remove("StratagemCooldowns"); ES3.Data[key] = old.ToString(Formatting.None);
                ModuleSettings.MainMenu(); ModuleSettings.Load(Harness.Host.Save);
                check(ModuleSettings.Progress.Player(1).StratagemCooldowns.Count == 0,"previous version progress defaults to initial card cooldowns");
            }
        }
        finally { Harness.Host = oldHost; Harness.Guest = oldGuest; }
    }
}
