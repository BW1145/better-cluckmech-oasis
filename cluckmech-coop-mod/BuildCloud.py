#!/usr/bin/env python3
"""Offline cloud compile/package using an installed official .NET SDK's Roslyn.

Never launches or installs the game. Game assemblies are read-only references,
never copied into the package. This is a Linux fallback for Package.ps1 when
MSBuild first-use IPC is unavailable. Normal Windows builds can keep using it.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parent
REPO = ROOT.parent


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet-root', type=Path, required=True)
    parser.add_argument('--game-dir', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--check', action='store_true', help='Validate inputs without building')
    parser.add_argument('--package', action='store_true', help='Also produce full installation ZIP; never install it')
    args = parser.parse_args()
    sdk = sorted((args.dotnet_root/'sdk').glob('*/Roslyn/bincore/csc.dll'))[-1]
    managed = args.game_dir/'Cluckmech Oasis_Data/Managed'
    loader = ROOT/'dependencies/BepInEx'
    projects = [ROOT/'src/CluckmechCoop.csproj', ROOT/'diagnostics/CluckmechCoopDiagnostics.csproj', REPO/'cluckmech-skip-splash/src/CluckmechSkipSplash.csproj']
    music = sorted((ROOT/'src/MusicAssets').glob('*.ogg'))
    sounds = [ROOT/'src/StratagemSounds'/name for name in ('barrage.ogg','eagle.ogg','laser-start.ogg','laser-loop.ogg','sounds.json')]
    assert len(music) == 10, 'Expected all ten OGG music tracks'
    required = music + sounds + [ROOT/'src/MusicAssets/mix.json', ROOT/'使用说明.md', ROOT/'design/异界角色设计.md', ROOT/'dependencies/BepInEx-LICENSE.txt', ROOT/'evidence/startup-destruction/BepInEx.cfg.updated']
    required += [loader/name for name in ('winhttp.dll','doorstop_config.ini','.doorstop_version','changelog.txt','BepInEx/core/BepInEx.dll','BepInEx/core/0Harmony.dll','BepInEx/core/BepInEx.Preloader.dll')]
    required += [ROOT/"src/OtherworldAssets"/f"faithful-{hero}.{ext}" for hero in (0,1) for ext in ("png","json")]
    plans=[]
    for project in projects:
        xml=ET.parse(project)
        name=xml.findtext('.//AssemblyName');version=xml.findtext('.//Version')
        refs=[managed/(name+'.dll') for name in ('mscorlib','System','System.Core','netstandard')]
        for ref in xml.findall('.//Reference'):
            path=ref.findtext('HintPath').replace('\\','/')
            path=path.replace('$(Managed)',str(managed)).replace('$(Loader)',str(loader/'BepInEx/core'))
            refs.append((project.parent/path).resolve() if not Path(path).is_absolute() else Path(path))
        resources=[]
        for entry in xml.findall('.//EmbeddedResource'):
            for pattern in entry.attrib['Include'].split(';'):
                for asset in sorted(project.parent.glob(pattern)):
                    logical=entry.findtext('LogicalName').replace('%(Filename)',asset.stem).replace('%(Extension)',asset.suffix)
                    resources.append((asset,logical))
        sources=sorted(project.parent.glob('*.cs'))
        assert sources, f'No source files for {name}'
        required += refs+[asset for asset,_ in resources]
        plans.append((name,version,refs,resources,sources))
    missing=[str(path) for path in required if not path.is_file()]
    assert not missing, 'Missing build inputs:\n'+'\n'.join(missing)
    print(f'Inputs OK: {len(plans)} plugins, {len(plans[0][3])} embedded resources, 10 music OGG, 4 effect OGG; game references stay private.')
    if args.check:return
    output=args.output.resolve();build=output/'build';build.mkdir(parents=True,exist_ok=True)
    binaries=[]
    for name,version,refs,resources,sources in plans:
        metadata=build/(name+'.AssemblyInfo.cs')
        metadata.write_text('using System.Reflection;\nusing System.Runtime.Versioning;\n'+f'[assembly: AssemblyVersion("{version}.0")]\n[assembly: AssemblyFileVersion("{version}.0")]\n[assembly: AssemblyInformationalVersion("{version}")]\n[assembly: TargetFramework(".NETFramework,Version=v4.7.2")]\n',encoding='utf-8')
        dll=build/(name+'.dll')
        command=[str(args.dotnet_root/'dotnet'),str(sdk),'-nologo','-target:library','-langversion:latest','-optimize+','-deterministic+',f'-pathmap:{REPO}=./,{build}=./build',f'-out:{dll}']
        command += [f'-r:{path}' for path in refs]
        command += [f'-resource:{path},{logical}' for path,logical in resources]
        command += [str(path) for path in sources]+[str(metadata)]
        subprocess.run(command,check=True)
        binaries.append(dll)
        print(f'Built {name} {version}: {len(resources)} embedded resources')
    if not args.package:return
    version=plans[0][1];package=output/f'Better-Cluckmech-Oasis-{version}-Full'
    # A fresh directory prevents leftovers from an earlier build leaking into a release.
    package.mkdir(exist_ok=False)
    shutil.copytree(loader/'BepInEx',package/'BepInEx',dirs_exist_ok=True)
    for name in ('winhttp.dll','doorstop_config.ini','.doorstop_version','changelog.txt'):shutil.copy2(loader/name,package/name)
    plugin=package/'BepInEx/plugins/CluckmechCoop';plugin.mkdir(parents=True,exist_ok=True)
    patchers=package/'BepInEx/patchers';patchers.mkdir(parents=True,exist_ok=True)
    for dll in binaries:shutil.copy2(dll,(patchers if dll.stem=='CluckmechSkipSplash' else plugin)/dll.name)
    shutil.copytree(ROOT/'src/MusicAssets',plugin/'Music')
    (plugin/'StratagemSounds').mkdir()
    for path in sounds:shutil.copy2(path,plugin/'StratagemSounds'/path.name)
    config=package/'BepInEx/config';config.mkdir(exist_ok=True)
    shutil.copy2(ROOT/'evidence/startup-destruction/BepInEx.cfg.updated',config/'BepInEx.cfg')
    (config/'billy.cluckmech.coop.diagnostics.cfg').write_bytes(b'[Diagnostics]\r\nDetailedScan = false\r\n')
    modules=('Speed','Characters','Energy','Music','Growth','Shop','Expedition','Transfer','SkipSplash','Stratagems','NativeRepairs','Boons','Wealth','Otherworld')
    (config/'billy.cluckmech.coop.cfg').write_bytes(('[Modules]\r\n'+''.join(f'{name} = true\r\n' for name in modules)).encode())
    shutil.copy2(ROOT/'使用说明.md',package/'使用说明.md');shutil.copy2(ROOT/'dependencies/BepInEx-LICENSE.txt',package/'BepInEx-LICENSE.txt')
    (package/'design').mkdir();shutil.copy2(ROOT/'design/异界角色设计.md',package/'design/异界角色设计.md')
    archive=output/(package.name+'.zip')
    with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
        for path in sorted(package.rglob('*')):
            if path.is_file():z.write(path,path.relative_to(package).as_posix())
    hashes={}
    with zipfile.ZipFile(archive) as z:
        assert not z.testzip(), 'ZIP integrity failure'
        names=z.namelist()
        for dll in binaries:
            entries=[name for name in names if name.endswith('/'+dll.name)]
            assert len(entries)==1 and z.read(entries[0])==dll.read_bytes(),f'Plugin hash mismatch: {dll.name}'
            hashes[dll.name]=hashlib.sha256(dll.read_bytes()).hexdigest()
        assert len([n for n in names if '/Music/' in n and n.endswith('.ogg')])==10
        assert len([n for n in names if '/StratagemSounds/' in n and n.endswith('.ogg')])==4
        assert not any(Path(n).name in ('Assembly-CSharp.dll','UnityEngine.dll','mscorlib.dll','netstandard.dll','Mirror.dll','Mirror.Components.dll') for n in names),'Private game assembly included'
        assert b'HideManagerGameObject = true' in z.read('BepInEx/config/BepInEx.cfg')
    hashes['archive']=hashlib.sha256(archive.read_bytes()).hexdigest()
    (output/'package-hashes.json').write_text(json.dumps(hashes,indent=2)+'\n',encoding='utf-8')
    print(f'Full package verified: {archive.name} ({archive.stat().st_size} bytes)\nSHA256 {hashes["archive"]}')

if __name__=='__main__':main()
