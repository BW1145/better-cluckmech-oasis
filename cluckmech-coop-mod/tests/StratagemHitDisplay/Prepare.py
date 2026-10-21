"""Compile complete production methods without redistributing native game assemblies."""
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

world = (root/'src/StratagemWorld.cs').read_text(encoding='utf-8-sig')
network = (root/'src/OtherworldNetwork.cs').read_text(encoding='utf-8-sig')
protocol = (root/'src/OtherworldProtocol.cs').read_text(encoding='utf-8-sig')
start = world.index('internal sealed class StratagemDamage')
head = world[start:world.index('internal float Attack', start)]
hit = method(world, 'internal void Hit(')
state = method(world, 'internal 模块_状态属性.状态类 State(')
send = method(network, 'internal static void SendDisplay(')
receive = method(network, 'private static void Receive(OtherworldMessage message)')
route = receive[:receive.index('if (message.Kind == OtherworldMessageKind.DamageStatistics)')]
route = route.replace('private static void Receive(', 'internal static void Route(') + 'CharacterRoutes++;\n}'
(out/'Production.cs').write_text('''using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Mirror;
namespace CluckmechCoop {
''' + head + hit + '\n' + state + '\n}\n' + method(protocol, 'internal struct OtherworldMessage') +
 '\ninternal static partial class OtherworldNetwork { internal static int CharacterRoutes;\n' + send + '\n' + route + '\n}\n}\n', encoding='utf-8')

# Wiring checks cover every currently implemented source, including mineral hits.
unit = (root/'src/StratagemUnit.cs').read_text(encoding='utf-8-sig')
rover = (root/'src/StratagemRover.cs').read_text(encoding='utf-8-sig')
assert 'using(StratagemHitDisplay.Capture())' in hit
assert 'StratagemHitDisplay.TrackSource(source)' in head
assert unit.count('Damage.Hit(') >= 2 and 'Damage.Area(' in unit
assert 'Damage.EagleMinerals(' in unit and 'Hit(entry.Key,' in world
assert 'drone.Damage.Hit(' in rover
assert 'StratagemHitDisplay.Receive(message)' in route
assert route.index('StratagemHitDisplay.Receive(message)') < route.index('!OtherworldRegistry.Enabled')
assert not re.search(r'FindObjects|FindObjectOfType', (root/'src/StratagemHitDisplay.cs').read_text())
print('PASS: production Hit/constructor/network extraction and stratagem source wiring')
