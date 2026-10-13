import argparse,os,shutil,sys,json,subprocess
from pathlib import Path
sys.stdout.reconfigure(encoding='utf-8')
root=Path(__file__).resolve().parent
parser=argparse.ArgumentParser(description="Prepare the menu music from a source audio file")
parser.add_argument("source", type=Path)
args=parser.parse_args()
ffmpeg=os.environ.get("FFMPEG") or shutil.which("ffmpeg") or "ffmpeg"
source=args.source
destination=root/'src/MusicAssets/haruhikage.ogg'
result=subprocess.run([str(ffmpeg),'-hide_banner','-nostats','-i',str(source),'-af','loudnorm=I=-22:TP=-1:LRA=20:print_format=json','-f','null','-'],capture_output=True,check=True)
stats=json.JSONDecoder().raw_decode(result.stderr[result.stderr.rfind(b'{'):].decode('ascii'))[0]
filter='loudnorm=I=-22:TP=-1:LRA=20:linear=true:'+':'.join(f'{a}={stats[b]}' for a,b in [('measured_I','input_i'),('measured_TP','input_tp'),('measured_LRA','input_lra'),('measured_thresh','input_thresh'),('offset','target_offset')])
subprocess.run([str(ffmpeg),'-hide_banner','-loglevel','error','-y','-i',str(source),'-af',filter,'-ar','48000','-c:a','libvorbis','-q:a','6',str(destination)],check=True)
manifest=root/'src/MusicAssets/mix.json';tracks=json.loads(manifest.read_text(encoding='utf-8'))
tracks=[t for t in tracks if t['Playlist']!='背景音乐_主界面']
tracks.append(dict(Playlist='背景音乐_主界面',Title='春日影 (MyGO!!!!! ver.)',File='haruhikage.ogg',OriginalGains={},Replace=True,Menu=True))
manifest.write_text(json.dumps(tracks,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Prepared menu OGG and replacement playlist; input loudness',stats['input_i'])
