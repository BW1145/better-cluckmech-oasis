from pathlib import Path
import json
import struct
import subprocess
import sys
import wave
import zipfile

import numpy as np

from compose_previews import ROOT, OUT, FFMPEG


def check_midi(path):
    data = path.read_bytes()
    assert data[:4] == b"MThd"
    size, fmt, count, tpq = struct.unpack(">IHHH", data[4:14])
    assert (size, fmt, tpq) == (6, 1, 480)
    pos = 14
    for _ in range(count):
        assert data[pos:pos + 4] == b"MTrk"
        length = struct.unpack(">I", data[pos + 4:pos + 8])[0]
        pos += 8 + length
        assert data[pos - 3:pos] == b"\xff\x2f\x00"
    assert pos == len(data)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    manifest_path = ROOT / "音乐清单.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    assert len(manifest) == 7
    report = []
    for row in manifest:
        row["style"] = row["style"].replace("冰原", "冰川")
        title = row["title"]
        check_midi(OUT / (title + ".mid"))
        with wave.open(str(OUT / (title + ".wav"))) as f:
            rate = f.getframerate()
            pcm = np.frombuffer(f.readframes(f.getnframes()), np.int16).reshape(-1, f.getnchannels())
        absolute = np.abs(pcm.astype(np.int32))
        peak = int(absolute.max())
        boundary = float(np.abs(pcm[0].astype(np.int32) - pcm[-1].astype(np.int32)).max() / 32768)
        assert peak < 32767, (title, "clipping")
        assert min(row["rms_8s"]) > .01, (title, "silent section")
        assert boundary < .03, (title, "loop boundary", boundary)
        durations = {}
        for ext in ["mp3", "ogg"]:
            result = subprocess.check_output([str(FFMPEG.with_name("ffprobe.exe")),
                "-v", "error", "-show_entries", "format=duration", "-of", "json",
                str(OUT / (title + "." + ext))])
            duration = float(json.loads(result)["format"]["duration"])
            assert abs(duration - row["seconds"]) < .15
            durations[ext] = round(duration, 3)
        report.append({"title": title, "seconds": round(len(pcm) / rate, 3),
                       "peak_pcm": peak, "loop_step": round(boundary, 6),
                       "minimum_rms_8s": min(row["rms_8s"]), "durations": durations})
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    (ROOT / "音频检查.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    archive = ROOT / "鸡械绿洲音乐试听合集.zip"
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as z:
        for row in manifest:
            for ext in ["mid", "mp3", "ogg"]:
                path = OUT / (row["title"] + "." + ext)
                z.write(path, "试听/" + path.name)
        z.write(OUT / "试听说明.md", "试听/试听说明.md")
    with zipfile.ZipFile(archive) as z:
        assert len(z.namelist()) == 22
        assert z.testzip() is None
    print(json.dumps({"tracks": report, "archive": str(archive),
                      "archive_mb": round(archive.stat().st_size / 1024**2, 2)}, ensure_ascii=False, indent=2))
