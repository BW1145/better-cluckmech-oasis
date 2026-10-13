import json
import sys

from compose_previews import ROOT, SECTIONS, Score, line, render, section


def percussion(s, bar, style, energy):
    patterns = {
        "mine": [(0, 36, 84), (1.5, 36, 77), (3, 36, 85), (.5, 37, 61),
                 (1, 38, 65), (2, 37, 58), (2.5, 38, 71), (3.5, 38, 77)],
    }
    t = bar * 4
    for off, pitch, vel in patterns[style]:
        s.note(9, t + off, pitch, .1, round(vel * energy))
    for i in range(8):
        s.note(9, t + i * .5, 42,
               .065, round((40 if i % 2 else 27) * energy))
    if bar in [15, 23, 31, 47, 55, 63]:
        for i, pitch in enumerate([50, 48, 47, 45]):
            s.note(9, t + 3 + i * .25, pitch, .1, round((55 + i * 5) * energy))
    if bar in [4, 24, 36, 48]:
        s.note(9, t, 49, .6, 44)


def mark_sections(s):
    for begin, _, name in SECTIONS:
        s.marker(begin * 4, name)


def arrangement(s, sections):
    s.sections = sections
    for begin, _, name in sections:
        s.marker(begin * 4, name)
    return [next(name for begin, end, name in sections if begin <= bar < end)
            for bar in range(64)]


def phrase(s, ch, t, notes, velocity):
    for off, pitch, duration in notes:
        s.note(ch, t + off, pitch, duration, velocity)


def rainforest():
    s = Score("雨林脉冲", 106, [108, 12, 73, 32, 89, 115],
              [91, 69, 64, 80, 37, 48], [38, 85, 62, 64, 90, 25], 4301)
    names = arrangement(s, [(0, 8, "Canopy"), (8, 20, "Wood pulse"),
                           (20, 28, "Bird calls"), (28, 36, "Clearing"),
                           (36, 52, "Interlocking"), (52, 60, "Calls return"),
                           (60, 64, "Canopy return")])
    cells = [
        [(0, 52, .4), (.5, 59, .2), (1.75, 55, .3), (2.5, 62, .2), (3.25, 59, .4)],
        [(.25, 52, .3), (1, 64, .3), (2, 59, .35), (3.5, 55, .2)],
        [(0, 52, .35), (.75, 62, .25), (1.5, 59, .3), (2.75, 55, .3), (3.5, 52, .25)],
        [(.5, 59, .3), (1.25, 55, .2), (2.25, 62, .3), (3.25, 64, .4)],
    ]
    calls = [
        [(0, 83, .17), (.25, 76, .45), (1.75, 79, .2), (2.25, 71, .8)],
        [(.5, 86, .2), (.875, 83, .2), (1.25, 76, .65), (3, 74, .3)],
        [(0, 79, .6), (1.5, 71, .25), (2, 76, 1.1)],
        [(.75, 83, .25), (1.25, 74, .65), (2.75, 71, .8)],
    ]
    for bar in range(64):
        name, t = names[bar], bar * 4
        quiet = name in ["Canopy", "Clearing", "Canopy return"]
        deep = name == "Interlocking"
        root = 40 if (bar // 8) % 2 == 0 else 43
        if name == "Clearing":
            root = 45
        phrase(s, 0, t, cells[bar % 4], 61 if quiet else 79)
        for off, pitch in [(0, root), (2.75, root + 7)]:
            s.note(3, t + off, pitch, 1.3 if off == 0 else .6, 67 if quiet else 82)
        if bar % 2 == 0:
            s.chord(4, t, [root + 12, root + 19, root + 22], 7.6, 37)
        for i, off in enumerate([0, .75, 1.75, 2.5, 3.25]):
            s.note(9, t + off, [64, 62, 63, 64, 60][(i + bar % 3) % 5],
                   .11, (55 if quiet else 75) - i * 3)
        for i in range(8):
            s.note(9, t + i * .5 + (.10 if i % 2 else 0), 69, .06,
                   28 if quiet else 40)
        s.note(9, t + (1.5 if bar % 2 else 0), 36, .16, 50 if quiet else 72)
        if not quiet:
            phrase(s, 1, t, [(1.125, 67, .18), (2.125, 64, .2), (3.625, 71, .2)], 53)
            s.note(9, t + 2, 37, .12, 50)
        if deep:
            for i in range(6):
                s.note(5, t + i * (2 / 3) + .16, [52, 59, 55][(i + bar) % 3], .19, 49)
            for off in [.25, 1.25, 2.25, 3.5]:
                s.note(9, t + off, 61 if off < 2 else 63, .09, 47)
        if name in ["Bird calls", "Calls return"] and bar % 3 == 2:
            phrase(s, 2, t, calls[(bar // 3) % 4], 72)
        elif name == "Wood pulse" and bar % 4 == 3:
            phrase(s, 2, t, calls[bar // 4 % 4][:2], 57)
        if name == "Clearing" and bar % 2 == 1:
            phrase(s, 2, t, [(1.25, 76, 1.2), (3, 71, .7)], 51)
        if bar % 8 == 7:
            for i in range(5):
                s.note(9, t + 2.5 + i * .25, [62, 60, 63, 61, 64][i], .09, 48 + i * 5)
    return s, "沼泽 · 雨林打击乐", "低音卡林巴与木质打击乐交错，跳进短笛句如鸟鸣呼应，三连音织出雨林脉动。"


def volcanic_drums(s, bar, mode):
    t = bar * 4
    kicks = [[0, .75, 2, 2.75], [0, 1.25, 2.5, 3.5],
             [0, .5, 1.75, 2.25, 3.75], [0, 1.5, 2, 3.25]]
    level = .62 if mode == "Ashes" else .8 if mode == "Ritual" else 1
    if mode != "Drum chamber":
        for off in kicks[bar % 4]:
            s.note(9, t + off, 36, .12, round(100 * level))
        for off in [1, 3]:
            s.note(9, t + off, 38, .11, round(96 * level))
            s.note(9, t + off - .035, 40, .065, round(35 * level))
    for i in range(16):
        if mode == "Ashes" and i % 2:
            continue
        s.note(9, t + i * .25, 51 if mode in ["Eruption", "Lava return"] else 42,
               .065, round(([61, 25, 41, 29][i % 4]) * level))
    for off in [.625, 1.875, 2.625, 3.875]:
        if mode != "Ashes":
            s.note(9, t + off, 38, .06, 30 + (bar % 3) * 5)
    for i in range(6):
        off = i * (2 / 3)
        s.note(9, t + off + .10, [64, 62, 63][(i + bar) % 3], .09,
               round((72 if i % 3 == 0 else 43) * level))
    tom_offsets = [0, .75, 1.5, 2.5, 3.25] if bar % 2 == 0 else [.25, 1, 1.75, 2.75, 3.5]
    for i, off in enumerate(tom_offsets):
        s.note(9, t + off, [41, 45, 43, 48, 47][(i + bar % 4) % 5], .14,
               round((87 if mode in ["Drum chamber", "Ritual"] else 64) * level))
    if mode in ["Eruption", "Drum chamber"] or bar % 4 == 3:
        start = 2 if mode == "Drum chamber" else 3
        count = 12 if mode == "Drum chamber" else 6
        for i in range(count):
            s.note(9, t + start + i / 6, [50, 48, 47, 45, 43, 41][i % 6],
                   .08, 48 + (i % 6) * 7)
    if bar % 8 == 0:
        s.note(9, t, 52 if mode == "Eruption" else 49, .75, 62)
    elif bar % 4 == 2 and mode != "Ashes":
        s.note(9, t + 3.5, 46, .18, 55)


def andes():
    s = Score("熔岩祭舞", 148, [47, 116, 38, 60, 75, 81],
              [86, 77, 84, 37, 42, 34], [38, 84, 64, 45, 77, 58], 4302)
    s.event(9, 0, bytes([0xB9, 7, 114]), -1)
    names = arrangement(s, [(0, 8, "Ritual"), (8, 24, "Lava engine"),
                           (24, 32, "Eruption"), (32, 40, "Drum chamber"),
                           (40, 48, "Ashes"), (48, 60, "Lava return"),
                           (60, 64, "Ritual return")])
    for bar in range(64):
        name, t = names[bar], bar * 4
        volcanic_drums(s, bar, name)
        for i, off in enumerate([0, 1.5, 3]):
            s.note(0, t + off, [36, 43, 36][i], .43, 85 if i == 0 else 66)
        for i, off in enumerate([.5, 1.25, 2.25, 3.5]):
            s.note(1, t + off, [48, 55, 50, 48][(i + bar) % 4], .19, 65)
        if name != "Drum chamber":
            root = [36, 36, 34, 31][bar // 4 % 4]
            for off in ([0, 2] if name in ["Ritual", "Ashes", "Ritual return"]
                        else [0, .75, 1.5, 2.25, 3, 3.75]):
                s.note(2, t + off, root, .34, 69)
        if name in ["Lava engine", "Lava return"] and bar % 4 == 2:
            phrase(s, 4, t, [(.25, 79, .24), (.625, 72, .7), (2.5, 70, .23),
                            (3, 67, .7)], 62)
        if name == "Eruption" and bar % 2 == 0:
            s.chord(3, t, [48, 55, 60], .42, 66)
            s.chord(3, t + 2.5, [46, 53, 58], .36, 54)
        if name == "Ashes" and bar % 4 == 0:
            phrase(s, 4, t, [(.5, 67, 1.4), (2.75, 60, 1.1)], 52)
        if name == "Lava return" and bar % 4 == 0:
            phrase(s, 5, t, [(0, 60, .17), (1.5, 67, .17), (3, 58, .17)], 45)
    return s, "火山 · 复节奏战鼓", "大鼓、通鼓、手鼓与定音鼓主导，十六分切分、三连滚奏和鼓组独奏推动熔岩爆发，排箫作短句呼应。"


def laboratory():
    s = Score("霓虹实验室", 132, [80, 39, 98, 107, 92, 77],
              [75, 90, 50, 58, 43, 60], [55, 64, 88, 32, 76, 42], 4303)
    names = arrangement(s, [(0, 6, "Boot"), (6, 18, "Circuit"),
                           (18, 30, "Pulse field"), (30, 38, "Suspended"),
                           (38, 54, "Overclock"), (54, 62, "Circuit return"),
                           (62, 64, "Boot return")])
    motifs = [
        [(.25, 84, .16), (1.625, 72, .24), (2.375, 79, .18), (3.5, 73, .16)],
        [(0, 78, .24), (.875, 85, .15), (2.25, 72, .28), (3.125, 79, .17)],
        [(.5, 73, .18), (1.75, 84, .24), (2.5, 78, .15), (3.75, 72, .15)],
        [(.125, 79, .16), (1.25, 73, .18), (2.875, 85, .16)],
    ]
    bass_cells = [[(0, 36), (.875, 36), (1.75, 43), (2.5, 36), (3.25, 37)],
                  [(.25, 36), (1.5, 48), (2.25, 36), (3.625, 43)],
                  [(0, 34), (.75, 34), (2, 41), (2.75, 46), (3.5, 34)]]
    for bar in range(64):
        name, t = names[bar], bar * 4
        quiet = name in ["Boot", "Suspended", "Boot return"]
        intense = name == "Overclock"
        phrase(s, 0, t, motifs[(bar // 2) % 4], 48 if quiet else 77)
        if bar % 3 == 0:
            s.chord(4, t, [48, 55, 61, 66], 11.5, 36)
        for off, pitch in bass_cells[bar % 3]:
            s.note(1, t + off, pitch, .14 if intense else .23, 53 if quiet else 82)
        kicks = [0, 1.75, 2.5] if bar % 2 == 0 else [.25, 1.5, 2.25, 3.5]
        for off in kicks:
            s.note(9, t + off, 36, .1, 48 if quiet else 90)
        for off in [1, 3]:
            s.note(9, t + off, 37 if quiet else 40, .08, 36 if quiet else 74)
        for i in range(16 if intense else 8):
            off = i * (.25 if intense else .5)
            if (i + bar) % 5 != 0:
                s.note(9, t + off, 42 if i % 3 else 76, .05,
                       (24 if quiet else 35) + (12 if i % 4 == 0 else 0))
        for off in ([.625, 2.875] if bar % 2 else [1.125, 3.375]):
            s.note(9, t + off, 75, .08, 35 if quiet else 50)
        if name in ["Circuit", "Circuit return"] and bar % 4 == 1:
            phrase(s, 3, t, [(.5, 67, .18), (1.125, 66, .18), (2.75, 60, .32)], 58)
        if name in ["Pulse field", "Overclock"]:
            sequence = [72, 78, 73, 84, 79]
            for i in range(5):
                off = (.375 + i * .75 + (bar % 3) * .125) % 4
                s.note(2, t + off, sequence[(i + bar) % 5], .11, 51)
        if name == "Suspended" and bar % 4 == 0:
            phrase(s, 5, t, [(.75, 78, 2.4), (3.5, 73, 1.6)], 55)
        if intense and bar % 4 == 3:
            for i in range(6):
                s.note(0, t + 2.5 + i * .25, [84, 72, 85, 73, 78, 66][i], .08, 52)
        if bar in [6, 18, 38, 54]:
            s.note(9, t, 55, .25, 46)
    return s, "研究所 · 日式冷调电子", "八度跳进的短促合成器信号、错位低音和电子碎拍构成主线，筝与尺八在间奏中呼应。"


def balkan():
    s = Score("岩窟快车", 140, [21, 71, 40, 58, 24, 15],
              [66, 83, 66, 91, 58, 45], [38, 70, 87, 64, 52, 91], 4204)
    scale = [0, 2, 3, 6, 7, 8, 11]
    progression = [(45, [64, 69, 72]), (41, [65, 69, 72]),
                   (38, [62, 66, 69]), (40, [64, 68, 71])]
    bridge = [(38, [62, 66, 69]), (43, [62, 67, 71]),
              (48, [64, 67, 72]), (40, [64, 68, 71])]
    a = [(0, 0, .25), (.5, 2, .25), (1, 4, .3), (1.5, 3, .6),
         (2.25, 4, .2), (2.75, 5, .2), (3, 4, .7), (4, 2, .25),
         (4.5, 1, .25), (5, 0, .3), (5.5, 1, .6), (6.25, 2, .25), (7, 0, .7)]
    av = [(0, 7, .3), (.5, 6, .2), (1, 5, .3), (1.5, 4, .6),
          (2.25, 3, .2), (2.75, 4, .2), (3, 2, .7), (4, 3, .3),
          (4.5, 4, .3), (5, 5, .3), (5.5, 4, .6), (6.25, 2, .25), (7, 0, .7)]
    b = [(0, 3, .65), (1, 4, .25), (1.5, 5, .65), (2.5, 4, .25),
         (3, 7, .65), (4, 6, .3), (4.5, 5, .3), (5, 4, .3),
         (5.5, 3, .65), (6.5, 2, .3), (7, 4, .7)]
    mark_sections(s)
    for bar in range(64):
        name, t = section(bar), bar * 4
        quiet = name in ["Intro", "Break"]
        root, chord = (bridge if name.startswith("B") else progression)[bar % 4]
        percussion(s, bar, "mine", .5 if quiet else .95)
        for off in [0, 1.5, 3]:
            s.note(3, t + off, root, .35, 76)
        for off in [.5, 1, 2, 2.5, 3.5]:
            s.chord(0, t + off, chord, .21, 52)
            s.chord(4, t + off, chord, .15, 42, .01)
        if name in ["A return", "B variation"]:
            for i in range(8):
                s.note(5, t + i * .5, chord[i % 3] + 12, .15, 41)
        if bar % 2 == 0:
            phrase = [(0, 0, .7), (1.5, 2, .7), (3, 4, .7), (4, 3, .7),
                      (5.5, 2, .7), (7, 0, .7)] if quiet else (
                b if name.startswith("B") else av if bar % 8 >= 4 else a)
            line(s, 2 if name.startswith("B") else 1, t, phrase, 69, scale,
                 velocity=56 if quiet else 77)
            if name in ["B variation", "Return"]:
                line(s, 1 if name.startswith("B") else 2, t,
                     [(o, d - 2, length) for o, d, length in phrase if o >= 4],
                     69, scale, velocity=43)
    return s, "矿洞 · 巴尔干舞曲", "单簧管与手风琴领奏，以3+3+2重音和低音铜管表现矿车疾行的颠簸律动。"


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    path = ROOT / "音乐清单.json"
    results = json.loads(path.read_text(encoding="utf-8"))
    composers = {fn.__name__: fn for fn in [rainforest, andes, laboratory, balkan]}
    for name in (sys.argv[1:] or ["rainforest", "andes", "laboratory"]):
        composer = composers[name]
        result = render(*composer())
        results = [row for row in results if row["title"] != result["title"]] + [result]
        path.write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
