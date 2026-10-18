"""Execute actual asset selector against embedded production atlases, no game/graphics."""
import argparse,json,subprocess
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--dotnet-root',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
root=Path(__file__).resolve().parents[2];a.output.mkdir(parents=True,exist_ok=True)
refs=sorted((a.dotnet_root/'packs/Microsoft.NETCore.App.Ref').glob('*/ref/net8.0'))[-1]
csc=sorted((a.dotnet_root/'sdk').glob('*/Roslyn/bincore/csc.dll'))[-1]
out=a.output/'AssetSelection.dll';args=[str(a.dotnet_root/'dotnet'),str(csc),'-nologo','-target:exe',f'-out:{out}']
args += [f'-r:{f}' for f in refs.glob('*.dll')]
args += [str(root/'src/OtherworldAssets.cs'),str(Path(__file__).with_name('Program.cs'))]
for h in (0,1):
 for ext in ('png','json'):
  f=root/f'src/OtherworldAssets/faithful-{h}.{ext}'
  assert f.is_file(),f'Missing completed atlas {f.name}'
  args.append(f'-resource:{f},CluckmechCoop.OtherworldAssets.{f.name}')
subprocess.run(args,check=True)
out.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
subprocess.run([str(a.dotnet_root/'dotnet'),str(out)],check=True)
