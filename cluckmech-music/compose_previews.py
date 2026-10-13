from pathlib import Path
import json
import os
import shutil
import math
import random
import struct
import subprocess
import sys
import wave

import numpy as np

ROOT = Path(__file__).resolve().parent
OUT = ROOT / "试听"
OUT.mkdir(exist_ok=True)
TPQ = 480
SYNTH = os.environ.get("FLUIDSYNTH") or shutil.which("fluidsynth") or next((ROOT / "tools" / "fluidsynth").rglob("fluidsynth.exe"), "fluidsynth")
SF = Path(os.environ.get("SOUNDFONT", str(ROOT / "tools" / "GeneralUser-GS.sf2")))
FFMPEG = os.environ.get("FFMPEG") or shutil.which("ffmpeg") or "ffmpeg"


def vlq(n):
    parts = [n & 127]
    while n := n >> 7:
        parts.insert(0, (n & 127) | 128)
    return bytes(parts)


class Score:
    def __init__(self, name, bpm, programs, volumes, pans, seed):
        self.name, self.bpm = name, bpm
        self.rng = random.Random(seed)
        self.tracks = [[] for _ in range(10)]
        for ch, prog in enumerate(programs):
            self.event(ch, 0, bytes([0xC0 | ch, prog]), -3)
            for cc, val in [(7, volumes[ch]), (10, pans[ch]), (91, 29), (93, 12)]:
                self.event(ch, 0, bytes([0xB0 | ch, cc, val]), -2)
        for cc, val in [(7, 92), (10, 64), (91, 8)]:
            self.event(9, 0, bytes([0xB9, cc, val]), -2)
        self.notes = 0

    def event(self, ch, beat, payload, priority=0):
        self.tracks[ch].append((max(0, round(beat * TPQ)), priority, payload))

    def note(self, ch, beat, pitch, duration, velocity=80, human=True):
        shift = self.rng.uniform(-0.010, 0.010) if human else 0
        vel = max(1, min(127, velocity + self.rng.randint(-4, 4)))
        start = max(0, beat + shift)
        self.event(ch, start, bytes([0x90 | ch, pitch, vel]), 1)
        self.event(ch, start + duration, bytes([0x80 | ch, pitch, 0]), 0)
        self.notes += 1

    def chord(self, ch, beat, notes, duration, velocity=65, roll=0):
        for i, p in enumerate(notes):
            self.note(ch, beat + i * roll, p, duration, velocity)

    def bend(self, ch, beat, semitones):
        value = max(0, min(16383, round(8192 + semitones * 4096)))
        self.event(ch, beat, bytes([0xE0 | ch, value & 127, value >> 7]), -1)

    def marker(self, beat, name):
        text = name.encode("utf-8")
        self.event(0, beat, b"\xff\x06" + vlq(len(text)) + text, -1)

    def write(self, path, bars=64):
        title = self.name.encode("utf-8")
        tempo = round(60_000_000 / self.bpm)
        meta = b"\x00\xff\x03" + vlq(len(title)) + title
        meta += b"\x00\xff\x51\x03" + tempo.to_bytes(3, "big")
        meta += b"\x00\xff\x58\x04\x04\x02\x18\x08\x00\xff\x2f\x00"
        chunks = [b"MTrk" + struct.pack(">I", len(meta)) + meta]
        for events in self.tracks:
            if not events:
                continue
            data, last = bytearray(), 0
            for tick, _, payload in sorted(events, key=lambda e: (e[0], e[1])):
                data += vlq(tick - last) + payload
                last = tick
            end = max(last, bars * 4 * TPQ)
            data += vlq(end - last) + b"\xff\x2f\x00"
            chunks.append(b"MTrk" + struct.pack(">I", len(data)) + data)
        path.write_bytes(b"MThd" + struct.pack(">IHHH", 6, 1, len(chunks), TPQ) + b"".join(chunks))


SECTIONS = [(0, 4, "Intro"), (4, 16, "A"), (16, 24, "A variation"),
            (24, 32, "B"), (32, 36, "Break"), (36, 48, "A return"),
            (48, 56, "B variation"), (56, 64, "Return")]


def section(bar):
    return next(name for begin, end, name in SECTIONS if begin <= bar < end)


def drums(s, bar, style, energy):
    t = bar * 4
    if style == "desert":
        for beat, note, velocity in [(0, 64, 91), (.5, 62, 68), (1.5, 64, 78),
                                     (2, 63, 78), (3, 62, 80), (3.5, 60, 60)]:
            s.note(9, t + beat, note, .12, round(velocity * energy))
        for i in range(8):
            s.note(9, t + i * .5, 70, .08, round((45 if i % 2 else 32) * energy))
        if energy > .7:
            for b in [0, 2.5]:
                s.note(9, t + b, 36, .12, 63)
            s.note(9, t + 3, 37, .1, 52)
    elif style == "island":
        for b in [0, 1.75, 2.5]:
            s.note(9, t + b, 36, .12, round(78 * energy))
        for b in [1, 3]:
            s.note(9, t + b, 37, .1, round(81 * energy))
        for i in range(8):
            s.note(9, t + i * .5, 42, .08, round((51 if i % 2 else 35) * energy))
        for b, n in [(.75, 62), (1.5, 63), (2.75, 64), (3.5, 63)]:
            s.note(9, t + b, n, .12, round(62 * energy))
        if bar % 4 == 2:
            s.note(9, t + 3.5, 54, .15, 52)
    else:
        for b in [0, 2, 2.75]:
            s.note(9, t + b, 36, .14, round(79 * energy))
        for b in [1, 3]:
            s.note(9, t + b, 38, .1, round(57 * energy))
            s.note(9, t + b, 39, .12, round(30 * energy))
        for i in range(8):
            s.note(9, t + i * .5, 42, .07, round((49 if i % 2 else 27) * energy))
    if bar in [15, 23, 31, 47, 55, 63]:
        for i, n in enumerate([50, 48, 47, 45]):
            s.note(9, t + 3 + i * .25, n, .12, 52 + i * 4)
    if bar in [4, 24, 36, 48]:
        s.note(9, t, 49, .7, 49)


def line(s, ch, beat, phrase, root, scale, octave=0, velocity=78):
    for offset, degree, duration in phrase:
        if degree is None:
            continue
        octaves, index = divmod(degree, len(scale))
        s.note(ch, beat + offset, root + scale[index] + 12 * (octaves + octave),
               duration, velocity)


def egypt():
    s = Score("沙海齿轮", 118, [24, 68, 75, 33, 89, 15],
              [81, 78, 66, 93, 46, 56], [44, 68, 82, 64, 50, 90], 4101)
    scale = [0, 1, 4, 5, 7, 8, 10]
    a = [(0, 4, .45), (.75, 5, .2), (1, 4, .45), (1.75, 3, .2),
         (2, 2, .7), (3, 1, .2), (3.25, 0, .6),
         (4, 0, .45), (4.75, 1, .2), (5, 2, .45), (5.75, 3, .2),
         (6, 4, .65), (7, 2, .7)]
    av = [(0, 7, .7), (1, 6, .2), (1.25, 5, .45), (2, 4, .75),
          (3, 3, .4), (3.5, 2, .4), (4, 3, .45), (4.75, 4, .45),
          (5.5, 2, .7), (6.5, 1, .3), (7, 0, .7)]
    b = [(0, 3, .7), (1, 4, .45), (1.75, 5, .2), (2, 6, .75),
         (3, 5, .45), (3.5, 4, .3), (4, 7, .65), (5, 6, .45),
         (5.75, 5, .2), (6, 4, .5), (6.75, 2, .25), (7.25, 0, .5)]
    chords_a = [(38, [62, 66, 69]), (39, [63, 67, 70]),
                (43, [62, 67, 70]), (38, [62, 66, 69])]
    chords_b = [(43, [62, 67, 70]), (36, [60, 63, 67]),
                (39, [63, 67, 70]), (38, [62, 66, 69])]
    for begin, _, name in SECTIONS:
        s.marker(begin * 4, name)
    for bar in range(64):
        name, t = section(bar), bar * 4
        root, chord = (chords_b if name.startswith("B") else chords_a)[bar % 4]
        energy = .45 if name in ["Intro", "Break"] else .9
        drums(s, bar, "desert", energy)
        s.chord(4, t, [p - 12 for p in chord] + [chord[-1]], 3.85, 47)
        arp = [0, 2, 1, 2, 0, 1, 2, 1]
        for i, degree in enumerate(arp):
            s.note(0, t + i * .5, chord[degree], .32, 59 if i % 2 else 67)
        for off, pitch, length in [(0, root, .75), (1.5, root + 12, .35),
                                    (2, root, .65), (3.5, root + 7, .35)]:
            s.note(3, t + off, pitch, length, 84 if off == 0 else 72)
        if bar % 2 == 0 and name not in ["Intro", "Break"]:
            phrase = b if name.startswith("B") else (av if bar % 8 >= 4 else a)
            line(s, 1 if not name.startswith("B") else 2, t, phrase, 62, scale,
                 velocity=77 if name != "Return" else 70)
            if name in ["A return", "B variation"]:
                line(s, 5, t, [(o, d, min(l, .27)) for o, d, l in phrase if o >= 4],
                     62, scale, octave=1, velocity=42)
        elif name == "Break" and bar % 2 == 0:
            line(s, 2, t, [(0, 0, 1.5), (2, 1, .3), (3, 2, .6), (5, 4, 1.3),
                          (7, 2, .7)], 62, scale, velocity=62)
        if name == "Intro" and bar >= 2:
            line(s, 5, t, [(0, 0, .4), (1.5, 2, .4), (3, 4, .5)], 62, scale, velocity=52)
    return s, "荒漠 · 埃及风格", "拨弦、双簧管与手鼓，带机械低音节奏。"


def hawaii():
    s = Score("海风装配线", 126, [24, 27, 12, 33, 16, 80],
              [90, 77, 65, 95, 55, 48], [34, 80, 58, 64, 74, 50], 4102)
    scale = [0, 2, 4, 5, 7, 9, 11]
    chords_a = [(45, [61, 64, 68, 71]), (42, [61, 64, 66, 69]),
                (38, [62, 66, 69, 73]), (40, [62, 64, 68, 71])]
    chords_b = [(47, [62, 66, 69, 73]), (40, [62, 64, 68, 71]),
                (49, [61, 64, 68, 71]), (42, [61, 64, 66, 69])]
    a = [(0, 2, .35), (.5, 4, .35), (1.25, 5, .6), (2, 4, .35),
         (2.75, 2, .45), (3.5, 1, .35), (4.25, 0, .45), (5, 2, .35),
         (5.5, 4, .45), (6.25, 2, .35), (7, 1, .3), (7.5, 0, .4)]
    av = [(0, 7, .65), (1, 5, .4), (1.75, 4, .2), (2.5, 2, .45),
          (3.25, 4, .5), (4.25, 5, .45), (5, 4, .4), (5.75, 2, .5),
          (6.5, 1, .35), (7.25, 0, .55)]
    b = [(0, 1, .45), (.75, 3, .45), (1.5, 5, .7), (2.5, 4, .45),
         (3.25, 3, .55), (4, 6, .75), (5, 4, .45), (5.75, 2, .45),
         (6.5, 1, .35), (7.25, 0, .6)]
    for begin, _, name in SECTIONS:
        s.marker(begin * 4, name)
    for bar in range(64):
        name, t = section(bar), bar * 4
        root, chord = (chords_b if name.startswith("B") else chords_a)[bar % 4]
        energy = .55 if name in ["Intro", "Break"] else .95
        drums(s, bar, "island", energy)
        for off in [.5, 1.5, 2.5, 3.5]:
            s.chord(0, t + off, chord, .19, 61 if off in [.5, 2.5] else 70, .013)
        s.chord(4, t + .05, [p - 12 for p in chord], 3.65, 44)
        for off, pitch, length in [(0, root, .5), (.75, root + 12, .25),
                                    (1.5, root + 7, .3), (2.5, root, .45),
                                    (3.25, root + 7, .25), (3.75, root + 12, .18)]:
            s.note(3, t + off, pitch, length, 83 if off == 0 else 72)
        if bar % 2 == 0 and name not in ["Intro", "Break"]:
            phrase = b if name.startswith("B") else (av if bar % 8 >= 4 else a)
            line(s, 1, t, phrase, 69, scale, velocity=77)
            for off, degree, duration in phrase:
                if duration >= .55:
                    s.bend(1, t + off, -.8)
                    s.bend(1, t + off + .09, -.3)
                    s.bend(1, t + off + .17, 0)
            if name in ["A variation", "A return", "B variation"]:
                line(s, 2, t, [(o, d - 7, min(l, .3)) for o, d, l in phrase if o >= 4],
                     69, scale, velocity=49)
        if name in ["Intro", "Break"]:
            for i in range(4):
                s.note(2, t + i + .25, chord[i] + 12, .45, 57)
        if name == "B variation":
            for off in [.75, 2.75]:
                s.note(5, t + off, chord[2] + 12, .22, 48)
    return s, "海滩 · 夏威夷风格", "轻快拨弦、滑音吉他、康加鼓与放克贝斯。"


def nordic():
    s = Score("极光回路", 112, [8, 73, 48, 38, 89, 80],
              [76, 77, 54, 88, 53, 43], [40, 74, 84, 64, 47, 65], 4103)
    scale = [0, 2, 3, 5, 7, 8, 10]
    chords_a = [(38, [62, 65, 69, 76]), (34, [62, 65, 69, 70]),
                (41, [60, 65, 69, 72]), (36, [60, 64, 67, 74])]
    chords_b = [(43, [62, 67, 70, 74]), (34, [62, 65, 69, 70]),
                (41, [60, 65, 69, 72]), (36, [60, 64, 67, 74])]
    a = [(0, 0, .9), (1.5, 2, .4), (2.25, 4, .7), (3.25, 3, .55),
         (4, 2, .9), (5.5, 1, .45), (6.25, 0, 1.3)]
    av = [(0, 7, .9), (1.25, 6, .4), (2, 4, .7), (3, 5, .65),
          (4, 4, .75), (5, 3, .5), (5.75, 2, .5), (6.75, 0, 1)]
    b = [(0, 3, 1), (1.5, 4, .45), (2.25, 5, .7), (3.25, 4, .55),
         (4, 7, 1), (5.5, 6, .5), (6.25, 4, .5), (7, 3, .7)]
    for begin, _, name in SECTIONS:
        s.marker(begin * 4, name)
    for bar in range(64):
        name, t = section(bar), bar * 4
        root, chord = (chords_b if name.startswith("B") else chords_a)[bar % 4]
        energy = .35 if name in ["Intro", "Break"] else .85
        drums(s, bar, "nordic", energy)
        s.chord(4, t, [p - 12 for p in chord], 3.9, 45)
        if name not in ["Intro", "Break"]:
            s.chord(2, t + .03, chord[:3], 3.8, 43 if not name.startswith("B") else 51)
        for i in range(8):
            p = chord[[0, 2, 1, 3, 2, 1, 3, 2][i]] + 12
            s.note(0, t + i * .5, p, .37, 48 if i % 2 else 56)
        for off in [0, .75, 1.5, 2, 2.75, 3.5]:
            s.note(3, t + off, root if off in [0, 2] else root + 12, .32,
                   70 if off in [0, 2] else 57)
        if bar % 2 == 0 and name not in ["Intro", "Break"]:
            phrase = b if name.startswith("B") else (av if bar % 8 >= 4 else a)
            line(s, 1, t, phrase, 74, scale, velocity=75)
            if name in ["A return", "B variation"]:
                line(s, 5, t, [(o, d - 7, l * .65) for o, d, l in phrase if o >= 4],
                     74, scale, velocity=45)
        if name == "Break" and bar % 2 == 0:
            line(s, 1, t, [(0, 2, 1.5), (2, 1, 1.5), (4, 0, 2.8)],
                 74, scale, velocity=64)
    return s, "冰原 · 北欧氛围", "钟琴、长笛和弦乐主题，结合电子低音与机械节拍。"


def render(score, style, description):
    midi = OUT / (score.name + ".mid")
    raw = OUT / (score.name + "-render.wav")
    wav = OUT / (score.name + ".wav")
    score.write(midi)
    command = [str(SYNTH), "-ni", "-q", "-r", "44100", "-g", "0.65",
               "-o", "synth.reverb.room-size=0.35", "-o", "synth.reverb.level=0.18",
               "-F", str(raw), "-T", "wav", str(SF), str(midi)]
    subprocess.run(command, check=True, stdout=subprocess.DEVNULL)
    with wave.open(str(raw), "rb") as f:
        rate, channels = f.getframerate(), f.getnchannels()
        audio = np.frombuffer(f.readframes(f.getnframes()), np.int16).reshape(-1, channels).astype(np.float64) / 32768
    length = round(64 * 4 * 60 / score.bpm * rate)
    # Carry the renderer's release tail across the musical loop boundary.
    tail = audio[length:]
    audio = audio[:length].copy()
    if len(tail):
        n = min(len(tail), rate * 4, len(audio))
        audio[:n] += tail[:n] * np.linspace(1, 0, n)[:, None]
    peak = float(np.max(np.abs(audio)))
    audio *= .88 / max(peak, 1e-6)
    pcm = np.round(np.clip(audio, -1, 1) * 32767).astype(np.int16)
    with wave.open(str(wav), "wb") as f:
        f.setnchannels(channels); f.setsampwidth(2); f.setframerate(rate)
        f.writeframes(pcm.tobytes())
    for ext, codec in [("mp3", ["-c:a", "libmp3lame", "-b:a", "192k"]),
                       ("ogg", ["-c:a", "libvorbis", "-q:a", "5"])]:
        subprocess.run([str(FFMPEG), "-hide_banner", "-loglevel", "error", "-y",
                        "-i", str(wav), *codec, str(OUT / (score.name + "." + ext))], check=True)
    raw.unlink()
    seconds = length / rate
    windows = [audio[i:i + rate * 8] for i in range(0, len(audio), rate * 8)]
    rms = [round(float(np.sqrt(np.mean(x*x))), 4) for x in windows if len(x)]
    result = {"title": score.name, "style": style, "description": description,
              "bpm": score.bpm, "seconds": round(seconds, 2), "notes": score.notes,
              "peak_dbfs": round(20 * math.log10(float(np.max(np.abs(audio)))), 2),
              "rms_8s": rms, "sections": getattr(score, "sections", SECTIONS)}
    print(json.dumps(result, ensure_ascii=False), flush=True)
    return result


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    results = []
    for composer in [egypt, hawaii, nordic]:
        results.append(render(*composer()))
    (ROOT / "音乐清单.json").write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
