"""Render original Kamui transition sound previews without playing or installing audio."""
import argparse
import json
import wave
from pathlib import Path
import numpy as np

RATE = 48000


def filtered_noise(rng, length, low, high):
    noise = rng.normal(0, 1, length)
    frequency = np.fft.rfftfreq(length, 1/RATE)
    gain = 1 / np.sqrt(1 + (low / np.maximum(frequency, 1))**6)
    gain *= 1 / np.sqrt(1 + (frequency / high)**6)
    result = np.fft.irfft(np.fft.rfft(noise) * gain, n=length)
    return result / max(np.std(result), 1e-9)


def transition(inward):
    rng = np.random.default_rng(6901 if inward else 6902)
    seconds = .78 if inward else .72
    t = np.arange(round(seconds * RATE)) / RATE
    u = t / seconds
    # Moving narrow bands and uneven pulses make a hollow revolving pressure sound.
    low = filtered_noise(rng, len(t), 85, 750)
    air = filtered_noise(rng, len(t), 500, 4800)
    grain = filtered_noise(rng, len(t), 1300, 6200)
    sweep = (230 + 900 * u**1.5) if inward else (1180 * np.exp(-5 * u) + 95)
    phase = 2 * np.pi * np.cumsum(sweep) / RATE
    revolutions = 2 * np.pi * (3.1 * u + 3.8 * u**2)
    folds = (.42 + .58 * (.5 + .5 * np.sin(revolutions)))
    env = np.sin(np.pi * np.clip(u, 0, 1))**(.75 if inward else 1.25)
    env *= (.3 + .7 * u) if inward else np.exp(-2.2 * u)
    body = .48 * low + .28 * air * np.sin(phase)
    body += .12 * np.sin(phase + 2.5 * np.sin(phase * .37))
    body += .12 * grain * np.sin(phase * .5)
    mono = body * folds * env
    # Inward motion closes decisively; outward motion releases with a short low flutter.
    center = .64 if inward else .075
    pulse = np.exp(-((t - center) / (.023 if inward else .04))**2)
    mono += .28 * pulse * (low * .55 + np.sin(2 * np.pi * 105 * t) * .45)
    pan = .25 * np.sin(revolutions)
    stereo = np.stack([mono * np.sqrt((1 - pan) / 2), mono * np.sqrt((1 + pan) / 2)], axis=1)
    dry = stereo.copy()
    for delay, gain in ((.027, .23), (.061, .12), (.103, .06)):
        offset = round(delay * RATE)
        stereo[offset:] += dry[:-offset, ::-1] * gain
    fade = np.minimum(1, t / .008) * np.minimum(1, (seconds - t) / .045)
    stereo *= fade[:, None]
    stereo -= stereo.mean(axis=0)
    stereo *= 10**(-5 / 20) / np.max(np.abs(stereo))
    return stereo


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=Path(__file__).resolve().parent / "KamuiSounds")
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    def save(path, data):
        with wave.open(str(path), "wb") as output:
            output.setnchannels(2)
            output.setsampwidth(2)
            output.setframerate(RATE)
            output.writeframes(np.round(np.clip(data, -1, 1)*32767).astype("<i2").tobytes())
    manifest = []
    rendered = []
    for name, inward in (("01-神威吸入", True), ("02-神威放出", False)):
        data = transition(inward)
        save(args.output / (name + ".wav"), data)
        rendered.append(data)
        manifest.append({"name": name, "duration": len(data)/RATE, "sampleRate": RATE,
                         "peakDb": round(20*np.log10(np.max(np.abs(data))), 2),
                         "rmsDb": round(20*np.log10(np.sqrt(np.mean(data**2))), 2)})
    combined = np.concatenate([rendered[0], np.zeros((round(.65*RATE), 2)), rendered[1]])
    save(args.output / "03-吸入与放出.wav", combined)
    (args.output / "音效参数.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    print(json.dumps(manifest, ensure_ascii=False))


if __name__ == "__main__":
    main()
