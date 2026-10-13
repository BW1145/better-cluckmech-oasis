import json
import math
import subprocess
import sys
import wave

import numpy as np

from compose_previews import ROOT, OUT, FFMPEG


TARGET_LUFS = -22.0


def measure(path):
    result = subprocess.run([str(FFMPEG), "-hide_banner", "-nostats", "-i", str(path),
        "-af", "loudnorm=I=-22:TP=-2:LRA=20:print_format=json", "-f", "null", "-"],
        capture_output=True, check=True)
    log = result.stderr.decode("utf-8", errors="replace")
    stats = json.JSONDecoder().raw_decode(log[log.rfind("{"):])[0]
    return {"lufs": float(stats["input_i"]), "true_peak_dbfs": float(stats["input_tp"]),
            "loudness_range": float(stats["input_lra"])}


def normalize_track(row):
    title = row["title"]
    wav = OUT / (title + ".wav")
    before = measure(wav)
    gain_db = TARGET_LUFS - before["lufs"]
    with wave.open(str(wav), "rb") as f:
        rate, channels = f.getframerate(), f.getnchannels()
        source_pcm = np.frombuffer(f.readframes(f.getnframes()), np.int16).reshape(-1, channels)
    for attempt in range(2):
        assert before["true_peak_dbfs"] + gain_db < -2.0
        pcm = np.round(source_pcm.astype(np.float64) * 10 ** (gain_db / 20)).astype(np.int16)
        with wave.open(str(wav), "wb") as f:
            f.setnchannels(channels)
            f.setsampwidth(2)
            f.setframerate(rate)
            f.writeframes(pcm.tobytes())
        measured = measure(wav)
        difference = TARGET_LUFS - measured["lufs"]
        if abs(difference) < .10:
            break
        gain_db += difference
    audio = pcm.astype(np.float64) / 32768
    row["peak_dbfs"] = round(20 * math.log10(float(np.abs(audio).max())), 2)
    row["rms_8s"] = [round(float(np.sqrt(np.mean(audio[i:i + rate * 8] ** 2))), 4)
                     for i in range(0, len(audio), rate * 8)]
    outputs = {"wav": measured}
    for ext, codec in [("mp3", ["-c:a", "libmp3lame", "-b:a", "192k"]),
                       ("ogg", ["-c:a", "libvorbis", "-q:a", "5"])]:
        destination = OUT / (title + "." + ext)
        codec_gain = 0
        for attempt in range(2):
            subprocess.run([str(FFMPEG), "-hide_banner", "-loglevel", "error", "-y",
                "-i", str(wav), "-af", f"volume={codec_gain}dB", *codec, str(destination)], check=True)
            outputs[ext] = measure(destination)
            difference = TARGET_LUFS - outputs[ext]["lufs"]
            if abs(difference) < .10:
                break
            codec_gain += difference
    for stats in outputs.values():
        assert abs(stats["lufs"] - TARGET_LUFS) < .15, (title, stats)
        assert stats["true_peak_dbfs"] < -1.0, (title, stats)
    row["integrated_lufs"] = outputs["wav"]["lufs"]
    row["true_peak_dbfs"] = outputs["wav"]["true_peak_dbfs"]
    return {"title": title, "gain_db": round(gain_db, 2), "formats": outputs}


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    manifest_path = ROOT / "音乐清单.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    report = []
    for row in manifest:
        report.append(normalize_track(row))
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
        (ROOT / "响度统一检查.json").write_text(json.dumps({"target_lufs": TARGET_LUFS,
            "tracks": report}, ensure_ascii=False, indent=2), encoding="utf-8")
        print(json.dumps(report[-1], ensure_ascii=False), flush=True)
