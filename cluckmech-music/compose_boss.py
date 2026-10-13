import json
import sys

from compose_previews import ROOT, OUT, Score, render, egypt, hawaii, nordic
from compose_regions import arrangement, phrase, rainforest, andes, laboratory, balkan
from normalize_collection import normalize_track
from verify_collection import check_midi


def brass_chase():
    s = Score("铜管通缉令", 152, [56, 67, 0, 32, 45, 57],
              [82, 73, 77, 88, 42, 63], [43, 79, 55, 64, 90, 59], 5101)
    for ch, reverb in [(0, 22), (1, 24), (2, 30), (5, 25)]:
        s.event(ch, 0, bytes([0xB0 | ch, 91, reverb]), -1)
    names = arrangement(s, [(0, 4, "Standoff"), (4, 16, "Chase"),
                           (16, 24, "Piano pursuit"), (24, 32, "Brass crossfire"),
                           (32, 40, "Sneaking bass"), (40, 56, "Full pursuit"),
                           (56, 64, "Closing in")])
    themes = [
        [(0, 74, .22), (2 / 3, 77, .17), (1, 73, .23), (1 + 2 / 3, 74, .24),
         (2.5, 70, .22), (3, 67, .32)],
        [(.5, 69, .20), (1, 70, .25), (2, 76, .3), (2 + 2 / 3, 74, .18), (3, 73, .3)],
        [(0, 79, .3), (1, 72, .18), (1 + 2 / 3, 75, .2), (2, 71, .22),
         (3, 72, .3), (3 + 2 / 3, 67, .18)],
        [(0, 69, .22), (2 / 3, 75, .18), (1, 78, .22), (2, 74, .2),
         (2 + 2 / 3, 73, .18), (3, 66, .55)],
    ]
    walks = [[43, 46, 50, 54], [55, 53, 50, 47],
             [48, 51, 55, 49], [50, 54, 57, 42]]
    return_walks = [[43, 50, 46, 42], [43, 45, 46, 47],
                    [48, 55, 51, 49], [50, 48, 45, 42]]
    voicings = [[58, 62, 65, 69], [58, 62, 64, 69],
                [58, 62, 63, 67], [60, 63, 66, 69]]
    # Piano responses occupy the rests in each particular brass phrase.
    comp = {
        0: [(.75, .22, 42)], 1: [(3, .28, 45)], 3: [(1.75, .24, 43)],
        4: [(3.5, .20, 51)], 5: [(0, .28, 44)], 6: [(2.375, .22, 50)],
        7: [(1.375, .20, 45)], 9: [(3 + 2 / 3, .18, 48)],
        10: [(2.5, .20, 50)], 12: [(3.5, .22, 55)], 13: [(0, .20, 42)],
        14: [(2.5, .25, 47)], 15: [(1.375, .20, 45)],
        24: [(0, .40, 82)], 25: [(0, .25, 50)], 26: [(2.5, .20, 50)],
        27: [(1.375, .20, 45)], 28: [(3.5, .23, 54)], 29: [(0, .25, 47)],
        30: [(2.5, .24, 48)], 31: [(1.375, .20, 44)],
        33: [(3.5, .20, 34)], 34: [(2, .40, 38)], 37: [(1.75, .22, 36)],
        40: [(0, .32, 80)], 41: [(0, .22, 45)], 42: [(2.5, .20, 49)],
        43: [(1.375, .22, 52)], 44: [(3.5, .20, 51)],
        46: [(2.5, .23, 53)], 47: [(1.375, .21, 56)],
        48: [(0, .38, 94)], 49: [(0, .20, 50)], 50: [(2.5, .23, 57)],
        51: [(1 + 1 / 3, .20, 55)], 52: [(3.5, .22, 54)],
        54: [(2.5, .22, 56)], 55: [(1 + 1 / 3, .20, 52)],
        56: [(.75, .22, 43)], 57: [(3.5, .23, 45)], 59: [(2.5, .20, 42)],
        60: [(.75, .22, 40)], 61: [(3 + 1 / 3, .19, 44)], 63: [(1.75, .20, 44)],
    }
    solo = {
        16: [(0, 74, .27, 94), (2/3, 70, .16, 71), (1.5, 71, .17, 76),
             (2, 72, .24, 85), (2+2/3, 67, .18, 72), (3.5, 66, .22, 65)],
        17: [(0, 67, .20, 76), (2/3, 73, .17, 67), (1, 74, .28, 87),
             (2, 77, .35, 93), (2+2/3, 76, .17, 75), (3.5, 70, .23, 71)],
        18: [(1/3, 79, .36, 91), (1+2/3, 75, .17, 73),
             (2, 74, .22, 81), (2+2/3, 72, .18, 68)],
        19: [(0, 66, .24, 83), (2/3, 69, .16, 72), (1, 72, .22, 85),
             (1.5, 71, .16, 68), (2, 75, .20, 91), (2+2/3, 74, .25, 76)],
        20: [(0, 86, .34, 96), (1, 82, .22, 84), (1+2/3, 81, .16, 72),
             (2, 79, .28, 86), (3, 74, .32, 78)],
        21: [(.5, 77, .15, 79), (2/3, 76, .15, 68), (1, 74, .30, 85),
             (2, 73, .18, 76), (2+2/3, 74, .28, 86)],
        22: [(0, 79, .26, 88), (2/3, 78, .17, 75), (1.5, 74, .24, 84),
             (2+2/3, 70, .23, 78), (3, 67, .35, 82)],
        23: [(.5, 66, .20, 78), (1, 69, .24, 87), (2, 72, .32, 97)],
    }
    left_hand = {
        16: [(0, [43, 55], .48, 91)], 17: [(1.5, [58, 62], .30, 57)],
        18: [(0, [48, 55], .40, 67)], 19: [(0, [50, 62], .40, 82)],
        20: [(0, [43, 55], .45, 94)], 21: [(0, [58, 64], .30, 61)],
        22: [(2, [51, 58], .28, 65)], 23: [(2, [50, 60, 66], .33, 89)],
    }
    sax_answers = {
        7: [(3.6, 54, .18)], 11: [(3.6, 57, .18)],
        18: [(3.25, 55, .28)], 21: [(3.5, 58, .20)],
        25: [(2.75, 62, .22), (3+1/3, 58, .30)],
        27: [(2, 54, .28), (2+2/3, 57, .19), (3.25, 60, .35)],
        29: [(2.75, 61, .20), (3+1/3, 58, .30)],
        30: [(3, 55, .23), (3+2/3, 58, .17)],
        33: [(1.75, 62, .35)], 35: [(2/3, 54, .32), (1+2/3, 57, .36)],
        39: [(3, 62, .16), (3+1/3, 66, .16), (3+2/3, 69, .16)],
        43: [(3.6, 54, .17)], 45: [(3.5, 58, .20)],
        47: [(3.6, 60, .18)], 49: [(3.5, 61, .20)],
        51: [(2, 54, .24), (2+2/3, 57, .18), (3.25, 62, .30)],
        53: [(3.5, 58, .18)], 57: [(2.75, 55, .27)], 61: [(2.75, 54, .28)],
    }
    unison_bars = {4: 70, 24: 79, 40: 74, 48: 93, 52: 84}
    lead_cuts = {15: 5, 25: 3, 27: 3, 29: 3, 30: 4, 31: 3, 51: 3, 55: 3}
    for bar in range(64):
        name, t, cell = names[bar], bar * 4, bar % 4
        sneaking = name == "Sneaking bass"
        piano_solo = name == "Piano pursuit"
        intro = name in ["Standoff", "Closing in"]
        rest_from = 3 if bar in [15, 23, 31, 55] else 4
        bass = return_walks[cell] if 40 <= bar < 56 else walks[cell]
        for beat, pitch in enumerate(bass):
            if beat < rest_from:
                s.note(3, t + beat, pitch, .45 if sneaking else .65,
                       66 if sneaking else 77 if piano_solo else 82)
        ride_level = 38 if sneaking else 44 if piano_solo else 54
        for beat in range(4):
            if beat >= rest_from:
                continue
            s.note(9, t + beat, 51, .09, ride_level + (4 if beat in [1, 3] else -4))
            if beat + 2/3 < rest_from and (not sneaking or beat == 3):
                s.note(9, t + beat + 2/3, 51, .06, ride_level - 18)
        for off in [1, 3]:
            if off < rest_from:
                s.note(9, t + off, 44, .065, 37 if sneaking else 48)
                s.note(9, t + off, 37 if sneaking or piano_solo else 38, .08,
                       30 if sneaking else 42 if piano_solo else 56)
        for off in ([0] if sneaking else [0, 2]):
            s.note(9, t + off, 36, .1, 39 if sneaking else 53 if piano_solo else 62)
        for off, duration, velocity in comp.get(bar, []):
            tones = voicings[cell] if bar in [24, 40, 48] else voicings[cell][:3]
            for i, pitch in enumerate(tones):
                s.note(2, t + off + i*.006, pitch, duration, velocity - (8 if i == 0 else 0))
        if piano_solo:
            for off, pitch, duration, velocity in solo[bar]:
                s.note(2, t + off, pitch, duration, velocity)
                if (bar, off) in [(16, 0), (20, 0)]:
                    s.note(2, t + off + .005, pitch - 12, duration*.85, velocity - 18)
            for off, pitches, duration, velocity in left_hand[bar]:
                s.chord(2, t + off, pitches, duration, velocity, .006)
        elif sneaking:
            pizz = {32: [(0, 67, .18), (1.75, 74, .17)],
                    36: [(0, 74, .18), (2.25, 67, .19)],
                    38: [(.5, 63, .18), (2.75, 67, .19)]}
            phrase(s, 4, t, pizz.get(bar, []), 45)
        else:
            if intro:
                notes = ([(0, 74, .25), (1.5, 70, .20), (2.5, 69, .18), (3.5, 66, .20)]
                         if cell % 2 == 0 else themes[cell][:3])
                level = 69 if bar < 4 else 72 - (bar-56)//2
            else:
                notes = themes[cell][:lead_cuts.get(bar, len(themes[cell]))]
                level = 88 if 48 <= bar < 56 else 78 if 24 <= bar < 32 else 73
            for i, (off, pitch, duration) in enumerate(notes):
                if bar == 48 and i == 0:
                    pitch = 81
                s.note(0, t + off, pitch, duration, level + (4 if i == 0 else -4 if i % 2 else 0))
        for i, (off, pitch, duration) in enumerate(sax_answers.get(bar, [])):
            level = 48 if sneaking else 60 if bar < 40 else 66
            s.note(1, t + off + .025, pitch, duration, level - i*3)
        if bar in unison_bars:
            level = unison_bars[bar]
            root = [43, 43, 48, 50][cell]
            s.chord(5, t, [root+12, root+19, root+22], .28, level - 12)
            s.note(1, t + .012, root + 12, .24, level - 8)
            s.note(9, t, 49, .55, 43 if bar != 48 else 61)
            if bar == 48:
                s.chord(2, t, [43, 55], .30, 92)
        if bar in [15, 31, 55]:
            s.chord(5, t + 2.5, [54, 60, 63], .28, 64 if bar < 40 else 80)
            s.note(9, t + 2.5, 38, .08, 57 if bar < 40 else 70)
        fills = {
            3: [(3+1/3, 38, 36), (3+2/3, 38, 46)],
            19: [(3+1/3, 37, 32), (3+2/3, 37, 42)],
            39: [(3, 38, 38), (3+1/3, 48, 45), (3+2/3, 45, 54)],
            47: [(3+1/3, 38, 45), (3+2/3, 38, 55)],
            59: [(3+2/3, 37, 37)],
        }
        for off, pitch, velocity in fills.get(bar, []):
            s.note(9, t + off, pitch, .07, velocity)
    return s, "普通Boss · 摇摆铜管追逐", "铜管领奏、钢琴独奏与萨克斯回应交替展开，轻声潜行逐步转入齐奏高潮，以留白和重音塑造戏剧化追逐。"


def melody(score, channel):
    return [(tick, int(payload[1])) for tick, _, payload in sorted(score.tracks[channel])
            if payload[0] & 0xF0 == 0x90]


def signatures(notes):
    return {tuple((notes[j + 1][1] - notes[j][1],
                   round((notes[j + 1][0] - notes[j][0]) / 120))
                  for j in range(i, i + 4)) for i in range(len(notes) - 4)}


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    references = [(egypt, 1), (hawaii, 1), (nordic, 1), (rainforest, 2),
                  (andes, 4), (laboratory, 0), (balkan, 1)]
    existing = {fn.__name__: signatures(melody(fn()[0], ch)) for fn, ch in references}
    results = []
    for composer in [brass_chase]:
        score, style, description = composer()
        theme = signatures(melody(score, 0))
        assert all(not theme.intersection(reference) for reference in existing.values())
        existing[score.name] = theme
        row = render(score, style, description)
        row["use"] = "普通Boss战斗"
        row["audio_check"] = normalize_track(row)
        check_midi(OUT / (score.name + ".mid"))
        results.append(row)
        (ROOT / "Boss音乐清单.json").write_text(json.dumps(results, ensure_ascii=False, indent=2),
                                               encoding="utf-8")
        print(json.dumps({"title": score.name, "ready": True}, ensure_ascii=False), flush=True)
