"""Compose the mine combat cue and render a loudness-matched OGG preview."""
import json
import subprocess
import sys
import tempfile
import wave
from pathlib import Path

import numpy as np

from compose_previews import FFMPEG, OUT, ROOT, SF, SYNTH, Score
from normalize_collection import measure


SECTIONS = [(0, 4, "Ignition"), (4, 16, "Ore line"),
            (16, 24, "Crosscut"), (24, 32, "Deep vein"),
            (32, 40, "Shaft machinery"), (40, 48, "Ore line return"),
            (48, 56, "Deep vein return"), (56, 64, "Turnaround")]

THEME = [
    [(0, 74, .42), (.75, 77, .22), (1.25, 81, .63), (2.5, 79, .24), (3.25, 77, .48)],
    [(.25, 76, .32), (1, 74, .7), (2.25, 72, .22), (2.75, 73, .22), (3.25, 74, .5)],
    [(0, 77, .72), (1.25, 81, .28), (1.75, 84, .42), (2.75, 81, .28), (3.5, 79, .32)],
    [(.25, 77, .4), (1, 74, .88), (2.5, 72, .24), (3, 69, .68)],
    [(0, 79, .38), (.75, 82, .22), (1.25, 81, .5), (2.25, 77, .3), (3, 74, .64)],
    [(.5, 77, .3), (1.25, 79, .48), (2.25, 81, .28), (3, 77, .65)],
    [(0, 76, .5), (1, 73, .35), (1.75, 76, .3), (2.5, 79, .65)],
    [(.25, 77, .3), (1, 76, .32), (1.75, 73, .32), (2.5, 74, .92)],
]

COUNTERTHEME = [
    [(.25, 81, .68), (1.5, 79, .3), (2.25, 76, .62), (3.25, 74, .4)],
    [(0, 72, .88), (1.5, 76, .32), (2.25, 79, .3), (3, 81, .65)],
    [(.5, 82, .62), (1.5, 81, .25), (2.25, 77, .48), (3.25, 74, .36)],
    [(0, 76, .55), (1, 79, .48), (2, 77, .42), (3, 76, .72)],
    [(0, 74, .45), (.75, 79, .45), (1.75, 82, .75), (3, 81, .5)],
    [(.25, 79, .58), (1.5, 77, .4), (2.5, 74, .8)],
    [(0, 73, .52), (1, 76, .42), (2, 81, .65), (3.25, 79, .35)],
    [(.25, 76, .3), (1, 73, .45), (2, 74, 1.55)],
]


def phrase(score, channel, start, notes, velocity, transpose=0):
    for offset, pitch, length in notes:
        score.note(channel, start + offset, pitch + transpose, length, velocity)


def compose():
    s = Score("矿脉引擎", 128, [29, 34, 15, 81, 89, 11],
              [74, 91, 79, 57, 37, 52], [38, 64, 72, 88, 64, 28], 51428)
    s.sections = SECTIONS
    for start, _, name in SECTIONS:
        s.marker(start * 4, name)
    # Short, dry rhythm parts; space belongs to the lead and its delayed replies.
    for channel in [0, 1, 3, 9]:
        s.event(channel, 0, bytes([0xB0 | channel, 91, 10]), -1)
        s.event(channel, 0, bytes([0xB0 | channel, 93, 0]), -1)
    s.event(2, 0, bytes([0xB2, 91, 42]), -1)
    s.event(5, 0, bytes([0xB5, 91, 54]), -1)
    roots_a = [38, 38, 34, 34, 43, 43, 45, 45]
    roots_b = [41, 41, 34, 34, 43, 43, 45, 45]
    bass_patterns = [
        [(0, 0, .5), (.75, 0, .22), (1.5, 12, .26), (2, 0, .55), (3.25, 7, .3)],
        [(0, 0, .65), (1, 7, .27), (1.75, 12, .25), (2.5, 0, .4), (3.5, 0, .2)],
        [(0, 0, .5), (1.25, 0, .22), (2, 12, .3), (2.75, 7, .25), (3.5, 0, .2)],
        [(0, 0, .6), (1.5, 7, .25), (2.25, 12, .28), (3, 0, .6)],
    ]
    for bar in range(64):
        name = next(n for begin, end, n in SECTIONS if begin <= bar < end)
        t = bar * 4
        bridge = name.startswith("Deep")
        chamber = name == "Shaft machinery"
        intro = name == "Ignition"
        returning = "return" in name or name == "Turnaround"
        root = (roots_b if bridge else roots_a)[bar % 8]
        energy = .76 if intro else .86 if chamber else 1

        for off, degree, length in bass_patterns[bar % 4]:
            s.note(1, t + off, root + degree, length, 80 if intro else 94)

        # Palm-muted fifths alternate with open notes and rests rather than an ostinato chord on every offbeat.
        guitar = [0, .75, 2.5] if bar % 2 == 0 else [0, 1.5, 3]
        if chamber:
            guitar = [0] if bar % 2 == 0 else [2.5]
        for i, off in enumerate(guitar):
            s.chord(0, t + off, [root + 12, root + 19], .23 if i else .48,
                    75 if intro else 87 if returning else 80, .008)
        if returning and bar % 4 == 2:
            s.note(0, t + 3.25, root + 24, .42, 68)

        kicks = ([0, 1.5, 2.75] if bar % 2 == 0 else [0, .75, 2.5, 3.5])
        if chamber:
            kicks = [0, 2.5]
        for off in kicks:
            s.note(9, t + off, 36, .1, round(92 * energy))
        for off in [1, 3]:
            s.note(9, t + off, 38 if not chamber else 37, .09, round(86 * energy))
        if not intro and bar % 2:
            s.note(9, t + 2.75, 38, .055, 32)
        for i in range(8):
            s.note(9, t + i * .5, 42, .055, round(([48, 33, 39, 31][i % 4]) * energy))
        if returning and bar % 2 == 0:
            for off in [1.75, 3.75]:
                s.note(9, t + off, 42, .045, 27)
        # Metallic impacts answer the main drum rather than doubling it throughout.
        for off in ([.5, 2.25] if bar % 2 == 0 else [1.75, 3.5]):
            s.note(9, t + off, 56, .08, 39 if chamber else 30)
        if bar in [3, 15, 23, 31, 39, 47, 55, 63]:
            for i, pitch in enumerate([48, 45, 43, 41]):
                s.note(9, t + 3 + .25 * i, pitch, .09, 53 + i * 5)
        if bar in [4, 24, 40, 48, 56]:
            s.note(9, t, 49, .5, 54)

        if bar % 4 == 0:
            third = 4 if root in [34, 41, 45] else 3
            s.chord(4, t, [root + 12, root + 12 + third, root + 19, root + 24],
                    7.7, 31 if chamber else 38)

        if intro:
            if bar >= 2:
                phrase(s, 2, t, [(0, 74, .4), (1.25, 81, .5), (3, 77, .4)], 69)
        elif chamber:
            if bar % 2 == 0:
                phrase(s, 2, t, [(0, 62, .55), (1.5, 69, .45), (2.75, 65, .7)], 70)
                phrase(s, 5, t, [(.5, 74, .4), (2, 81, .4), (3.25, 77, .6)], 42)
            elif bar >= 36:
                phrase(s, 3, t, [(1.25, 57, .3), (2.5, 60, .3), (3.25, 61, .3)], 54)
        else:
            index = (bar - (24 if bridge else 4 if bar < 16 else 40 if name == "Ore line return" else 56)) % 8
            notes = COUNTERTHEME[index] if bridge else THEME[index]
            if name == "Crosscut":
                notes = [(o, p - 12, d * .8) for o, p, d in THEME[(bar - 16) % 8]]
            phrase(s, 2, t, notes, 91 if returning else 86)
            if returning:
                # Selective doubling makes the return stronger without burying the hammer attack.
                phrase(s, 3, t, [(o, p, d * .78) for o, p, d in notes if d >= .5], 57, -12)
            if bar % 2 == 1:
                answers = [(2.75, root + 31, .3), (3.5, root + 36, .35)]
                phrase(s, 5, t, answers, 43)
            if bar % 8 == 6:
                phrase(s, 3, t, [(3, 69, .22), (3.5, 73, .22)], 52)
    return s


def render_preview(score):
    midi = OUT / (score.name + ".mid")
    ogg = OUT / (score.name + ".ogg")
    score.write(midi)
    with tempfile.TemporaryDirectory(prefix="cluckmech-mine-", dir=ROOT) as directory:
        directory = Path(directory).resolve()
        assert directory.parent == ROOT.resolve()
        # Render three identical cycles and take the middle one, including its natural incoming release tails.
        for events in score.tracks:
            original = list(events)
            for cycle in [1, 2]:
                events.extend((tick + cycle * 256 * 480, priority, data) for tick, priority, data in original)
        cycle_midi, raw, mixed = (directory / n for n in ["cycles.mid", "raw.wav", "mix.wav"])
        score.write(cycle_midi, bars=192)
        subprocess.run([str(SYNTH), "-ni", "-q", "-r", "44100", "-g", "0.48",
                        "-o", "synth.reverb.room-size=0.38", "-o", "synth.reverb.level=0.17",
                        "-F", str(raw), "-T", "wav", str(SF), str(cycle_midi)],
                       check=True, stdout=subprocess.DEVNULL)
        with wave.open(str(raw), "rb") as source:
            rate, channels = source.getframerate(), source.getnchannels()
            tempo = round(60_000_000 / score.bpm)
            length = round(256 * tempo / 1_000_000 * rate)
            source.setpos(length)
            audio = np.frombuffer(source.readframes(length), np.int16).reshape(-1, channels).astype(np.float64) / 32768
        # A sub-millisecond splice keeps the waveform continuous without an audible fade between cycles.
        splice = 32
        junction = (audio[0] + audio[-1]) * .5
        audio[:splice] += (junction - audio[0])[None, :] * np.linspace(1, 0, splice)[:, None]
        audio[-splice:] += (junction - audio[-1])[None, :] * np.linspace(0, 1, splice)[:, None]
        audio *= .85 / max(float(np.abs(audio).max()), 1e-8)
        pcm = np.round(audio * 32767).astype(np.int16)
        with wave.open(str(mixed), "wb") as target:
            target.setnchannels(channels)
            target.setsampwidth(2)
            target.setframerate(rate)
            target.writeframes(pcm.tobytes())
        gain = -22 - measure(mixed)["lufs"]
        for _ in range(3):
            subprocess.run([str(FFMPEG), "-hide_banner", "-loglevel", "error", "-y",
                            "-i", str(mixed), "-af", f"volume={gain}dB",
                            "-c:a", "libvorbis", "-q:a", "5", str(ogg)], check=True)
            stats = measure(ogg)
            if abs(stats["lufs"] + 22) <= .1:
                break
            gain += -22 - stats["lufs"]
        assert abs(stats["lufs"] + 22) < .15 and stats["true_peak_dbfs"] < -1, stats
        result = {"title": score.name, "style": "矿洞 · 金属敲击与工业摇滚",
                  "description": "扬琴主题、短促吉他与低音推进，金属敲击呼应；机械间奏后回归完整主题。",
                  "bpm": score.bpm, "seconds": round(length / rate, 3), "notes": score.notes,
                  "sections": SECTIONS, **stats, "loop_step": float(np.abs(pcm[0].astype(int) - pcm[-1].astype(int)).max() / 32768)}
        (ROOT / "矿洞音乐检查.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(json.dumps(result, ensure_ascii=False), flush=True)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    render_preview(compose())
