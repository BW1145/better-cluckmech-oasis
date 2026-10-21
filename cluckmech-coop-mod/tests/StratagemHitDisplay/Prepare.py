"""Compile exact production methods; keep native game files out of the repository."""
from pathlib import Path
import re
root = Path(__file__).resolve().parents[2]
out = Path(__file__).resolve().parent / 'obj'
out.mkdir(exist_ok=True)

def method(text, declaration):
    start = text.index(declaration)
    brace = text.index('{', start)
    depth = 1
    end = brace + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[start:end]

def read(path):
    return (root/path).read_text(encoding='utf-8-sig')

def replace_once(text, old, new):
    assert text.count(old) == 1, old
    return text.replace(old, new, 1)

world = read('src/StratagemWorld.cs')
network = read('src/OtherworldNetwork.cs')
protocol = read('src/OtherworldProtocol.cs')
states = read('src/OtherworldStates.cs')
save = read('src/KamuiSave.cs')
bindings = read('tests/Bindings/CoopRepairChecks.cs')
start = world.index('internal sealed class StratagemDamage')
head = world[start:world.index('internal float Attack', start)]
hit = method(world, 'internal void Hit(')
state = method(world, 'internal 模块_状态属性.状态类 State(')
send = method(network, 'internal static void SendDisplay(')
receive = method(network, 'private static void Receive(OtherworldMessage message)')
route = receive[:receive.index('if (message.Kind == OtherworldMessageKind.DamageStatistics)')]
route = route.replace('private static void Receive(', 'internal static void Route(') + 'CharacterRoutes++;\n}'
production = '''using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using Mirror;
namespace CluckmechCoop {
''' + head + hit + '\n' + state + '\n}\n' + method(protocol, 'internal struct OtherworldMessage') + \
 '\ninternal static partial class OtherworldNetwork { internal static int CharacterRoutes;\n' + send + '\n' + route + '\n}\n'
production += method(states, 'internal sealed class OtherworldState:')
production += '\ninternal static class OtherworldStates {\n' + method(states, 'internal static OtherworldState Copy(') + '\nprivate static void Spread(){}\n}\n'
production += '\ninternal static class OtherworldStatusApplication {\n' + method(states, 'private static bool Prefix(模块_状态属性') + '\n}\n'
# Compile the actual save initializer too: older saves have no new feedback field.
restored = method(save, 'new OtherworldState{')
production += '''internal sealed class RestoredStateSample {
 public int Type;public bool BurnSpread;public float Remaining,EffectResistance,TimeResistance;public float[] Values=new float[20];
}
internal static class RestoreFeedbackProbe {
'''
unpack_start = save.index('private static 模块_攻击详细属性.状态属性单元 Unpack(')
unpack_end = save.index(';', unpack_start) + 1
production += save[unpack_start:unpack_end]
production += '\ninternal static OtherworldState Restore(RestoredStateSample state,OtherworldUnit unit,GameObject source,int sourceOwner)=>' + restored + ';\n}\n}\n'
production += '\ninternal static class CoopRepairChecks {\n' + method(bindings, 'internal static void CheckDisplayShow(') + '\n}\n'
(out/'Production.cs').write_text(production, encoding='utf-8')

# Expand only the surrounding test host. Production visibility and provenance
# implementations above remain unchanged, and are exercised with real MethodBody IL.
stubs = read('tests/CoopRepairs/Stubs.cs')
stubs = replace_once(stubs, 'public class 状态类{public GameObject 攻击者;', 'public class 状态类{public float 剩余时间;public 模块_抗性属性.状态抗性单元 自身抗性属性=new();public object Clone()=>MemberwiseClone();public GameObject 攻击者;')
stubs = replace_once(stubs, 'public int Count; public void 增加状态', 'public List<状态类> 当前状态列表=new();public int Count; public void 增加状态')
stubs = replace_once(stubs, 'public class OtherworldRun {public bool Pressure;', 'public class OtherworldRun {public int Initial;public bool Pressure;')
stubs = replace_once(stubs, 'public static 联机状态 当前联机状态;', 'public static 联机状态 当前联机状态;public static 联机玩家控件 玩家_客户端=new(){connectionToClient=new Mirror.NetworkConnectionToClient{connectionId=2}};')
stubs = replace_once(stubs, 'public class NetworkIdentity:Component {public uint assetId;}', 'public class NetworkIdentity:Component {public uint assetId,netId;public readonly Dictionary<int,NetworkConnectionToClient> observers=new();}')
stubs = replace_once(stubs, 'public static Dictionary<uint,NetworkIdentity> spawned=new();', 'public static IdentityTable spawned=new();')
stubs = replace_once(stubs, 'public static class NetworkServer {public static int Calls;', 'public static class NetworkServer {public static Dictionary<uint,NetworkIdentity> spawned=new();public static int Calls;')
stubs = replace_once(stubs, 'internal static class OtherworldActions {internal static OtherworldActor Viewer;', 'internal static class OtherworldActions {internal static void Status(){}internal static OtherworldActor Viewer;')
stubs = replace_once(stubs, 'public float 默认持续时间,当前持续时间;public UnityEngine.Vector4 默认属性效果,当前属性效果;', 'public float 默认持续时间,当前持续时间,变化持续时间,变化百分比持续时间;public UnityEngine.Vector4 默认属性效果,当前属性效果,变化属性效果,变化百分比属性效果;')
stubs += '''
public partial class 模块_基本属性 {public Steam联机管理器.联机状态 所属玩家=Steam联机管理器.联机状态.主机端;}
public class 模块_抗性属性 {public class 状态抗性单元 {public float 当前效果抗性,当前时间抗性;}}
public class 联机玩家控件 {public Mirror.NetworkConnectionToClient connectionToClient;}
namespace Mirror {
 public class NetworkConnectionToClient {public int connectionId;}
 public class IdentityTable:Dictionary<uint,NetworkIdentity> {
  public new NetworkIdentity this[uint id] {
   get=>base[id];set {base[id]=value;value.netId=id;NetworkServer.spawned[id]=value;
    var peer=Steam联机管理器.玩家_客户端?.connectionToClient;if(peer!=null)value.observers[peer.connectionId]=peer;}
  }
  public new void Clear(){base.Clear();NetworkServer.spawned.Clear();}
 }
}
'''
(out/'Stubs.cs').write_text(stubs, encoding='utf-8')

# Source wiring: test-only method substitutes are never evidence of game wiring.
unit = read('src/StratagemUnit.cs')
rover = read('src/StratagemRover.cs')
assert 'using(StratagemHitDisplay.Capture())' in hit
assert 'StratagemHitDisplay.TrackSource(source)' in head
assert unit.count('Damage.Hit(') >= 2 and 'Damage.Area(' in unit
assert 'Damage.EagleMinerals(' in unit and 'Hit(entry.Key,' in world
assert 'drone.Damage.Hit(' in rover
assert 'StratagemHitDisplay.Receive(message)' in route
assert route.index('StratagemHitDisplay.Receive(message)') < route.index('!OtherworldRegistry.Enabled')
assert 'HostDisplay=Plugin.Role==1' in restored
assert 'HostStatusFeedback.RequiresDisplay(__instance,状态)' in production
assert 'HostDisplay=state is OtherworldState feedback&&feedback.HostDisplay' in production
assert 'internal static void Status(' in read('src/OtherworldActions.cs')
assert 'private static void Spread(' in states
assert 'CheckDisplayShow(Body(Mod("OtherworldHitDisplay"),"Show"),Check)' in bindings
assert '==3,"cached normal/critical/color delegates"' not in bindings
for path in ['src/StratagemHitDisplay.cs','src/GuestHitVisibility.cs','src/HostStatusFeedback.cs']:
    assert not re.search(r'FindObjects|FindObjectOfType', read(path))
print('PASS: exact production damage, state application/copy/restore, visibility and display-binding preparation')
