"""Generate compact original stratagem effects as Vorbis clips."""
import argparse
import json
from pathlib import Path
import numpy as np
import soundfile as sf

parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, default=Path(__file__).resolve().parent.parent / 'src' / 'StratagemSounds')
args = parser.parse_args()
args.output.mkdir(parents=True, exist_ok=True)
sample_rate = 22050
rng = np.random.default_rng(500)
manifest = []

def noise(t, decay):
    raw = rng.normal(0, 1, len(t))
    low = np.convolve(raw, np.ones(15) / 15, mode='same')
    return low * np.exp(-t / decay)

def tone(t, start, end, decay):
    phase = 2*np.pi*(start*t + .5*(end-start)/max(t[-1],1/sample_rate)*t*t)
    return np.sin(phase) * np.exp(-t/decay)

def save(name, seconds, recipe, looping=False):
    t = np.arange(int(seconds*sample_rate)) / sample_rate
    data = recipe(t)
    if looping:
        data = data * .24
    else:
        fade = np.minimum(1,np.minimum(t/.005,(seconds-t)/.03))
        data = data*fade
        peak = max(float(np.max(np.abs(data))),.001)
        data *= .7/peak
    file = args.output / (name+'.ogg')
    sf.write(file,data,sample_rate,format='OGG',subtype='VORBIS')
    manifest.append({'file':file.name,'duration':round(len(data)/sample_rate,4),'sampleRate':sample_rate,'loop':looping,'peakDb':round(20*np.log10(max(float(np.max(np.abs(data))),1e-8)),2)})

save('barrage',1.0,lambda t: noise(t,.28)*2.5+tone(t,90,22,.3)*.7)
save('eagle',1.7,lambda t: noise(t,.55)*3+tone(t,65,16,.55)*.8)
save('laser-start',.6,lambda t: tone(t,240,950,.45)*.5+noise(t,.25)*.4)
save('laser-loop',1,lambda t: np.sin(2*np.pi*110*t)*.35+np.sin(2*np.pi*440*t)*(.25+.08*np.sin(2*np.pi*9*t)),True)
args.output.joinpath('sounds.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Generated',len(manifest),'stratagem sound effects.')
