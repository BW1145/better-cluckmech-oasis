import json
import math
import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT.parent / "cluckmech-research/tools/unitypy"))
sys.path.insert(0, str(ROOT.parent / "cluckmech-music"))
import UnityPy
from compose_previews import FFMPEG
from normalize_collection import measure

MUSIC = ROOT.parent / "cluckmech-music"
DEST = ROOT / "src/MusicAssets"
TRACKS = [
    ("战斗场景_荒漠", "沙海齿轮"), ("战斗场景_沼泽", "雨林脉冲"),
    ("战斗场景_冰川", "极光回路"), ("战斗场景_火山", "熔岩祭舞"),
    ("战斗场景_海滩", "海风装配线"), ("战斗场景_研究所", "霓虹实验室"),
    ("战斗场景_矿洞", "岩窟快车"), ("Boss出场_普通Boss", "铜管通缉令"),
    ("Boss出场_普通Boss2", "铜管通缉令")]


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    DEST.mkdir(exist_ok=True)
    game_dir = os.environ.get("CLUCKMECH_GAME_DIR")
    if not game_dir:
        raise SystemExit("Set CLUCKMECH_GAME_DIR to the game installation directory")
    game = Path(game_dir) / "Cluckmech Oasis_Data"
    env = UnityPy.load(str(game / "sharedassets0.assets"))
    needed = {"BGM_" + name for name, _ in TRACKS}
    clips = {}
    for obj in env.objects:
        if obj.type.name == "AudioClip":
            clip = obj.read()
            if clip.m_Name in needed:
                clips[clip.m_Name] = clip
    research = json.loads((ROOT.parent / "cluckmech-research/inspection/角色强化音乐调查.json").read_text(encoding="utf-8"))
    playlists = next(row["data"]["musicPlaylists"] for row in research["tables"] if row["class"] == "MasterAudio")
    volumes = {p["playlistName"]: p["MusicSettings"][0]["volume"] for p in playlists if len(p["MusicSettings"]) == 1}
    controllers = [row["data"] for row in research["tables"] if row["class"] == "PlaylistController"]
    assert all(row["_playlistVolume"] == 1 for row in controllers)
    existing = json.loads((DEST / "mix.json").read_text(encoding="utf-8")) if (DEST / "mix.json").exists() else []
    manifest = []
    for name, title in TRACKS:
        native_name = "BGM_" + name
        clip = clips[native_name]
        samples = clip.samples
        assert len(samples) == 1, native_name
        result = subprocess.run([str(FFMPEG), "-hide_banner", "-nostats", "-i", "pipe:0",
            "-af", "loudnorm=I=-22:TP=-1:LRA=20:print_format=json", "-f", "null", "-"],
            input=next(iter(samples.values())), capture_output=True, check=True)
        log = result.stderr.decode("utf-8", errors="replace")
        stats = json.JSONDecoder().raw_decode(log[log.rfind("{"):])[0]
        loudness = float(stats["input_i"])
        song_volume = volumes["背景音乐_" + name]
        effective_lufs = loudness + 20 * math.log10(song_volume)
        gain_db = -22 - effective_lufs
        assert float(stats["input_tp"]) + gain_db + 20 * math.log10(song_volume) < -1, native_name
        source = MUSIC / "试听" / (title + ".ogg")
        custom = measure(source)
        assert abs(custom["lufs"] + 22) < .5 and custom["true_peak_dbfs"] < -1
        shutil.copyfile(source, DEST / source.name)
        row = {"Playlist": "背景音乐_" + name, "Title": title, "File": source.name,
               "OriginalGains": {native_name: 10 ** (gain_db / 20)},
               "OriginalLufs": loudness, "OriginalSongVolume": song_volume, "OriginalEffectiveLufs": round(effective_lufs, 2), "OriginalGainDb": round(gain_db, 2), "CustomLufs": custom["lufs"]}
        if name == "战斗场景_矿洞":
            extra = MUSIC / "试听" / "矿脉引擎.ogg"
            extra_stats = measure(extra)
            assert abs(extra_stats["lufs"] + 22) < .5 and extra_stats["true_peak_dbfs"] < -1
            shutil.copyfile(extra, DEST / extra.name)
            row["AdditionalSongs"] = [{"Title": "矿脉引擎", "File": extra.name}]
        manifest.append(row)
        print(json.dumps(row, ensure_ascii=False), flush=True)
    boss_rows = [row for row in manifest if row["Title"] == "铜管通缉令"]
    boss_sources = [row["Playlist"] for row in boss_rows]
    boss_gains = {name: gain for row in boss_rows for name, gain in row["OriginalGains"].items()}
    for row in boss_rows:
        row["OriginalPlaylists"] = boss_sources
        row["OriginalGains"] = boss_gains
    manifest.extend(row for row in existing if row.get("Menu"))
    (DEST / "mix.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
