import sys, wave
import numpy as np

SR = 44100
OUT = sys.argv[1]
rng = np.random.default_rng(7)

def save(name, x):
    x = np.asarray(x, dtype=np.float64)
    peak = np.max(np.abs(x)) or 1.0
    x = x / peak * 0.85
    data = (x * 32767).astype('<i2')
    with wave.open(f"{OUT}/{name}.wav", 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())

def t(sec): return np.arange(int(SR * sec)) / SR

def env(n, a, r):
    e = np.ones(n)
    ai = int(SR * a); ri = int(SR * r)
    if ai: e[:ai] = np.linspace(0, 1, ai)
    if ri: e[-ri:] *= np.linspace(1, 0, ri)
    return e

def bell(freq, dur, decay=3.0):
    tt = t(dur)
    s = np.zeros_like(tt)
    for mult, amp in [(1, 1), (2.01, 0.45), (3.0, 0.25), (4.2, 0.12)]:
        s += amp * np.sin(2 * np.pi * freq * mult * tt)
    return s * np.exp(-decay * tt) * env(len(tt), 0.005, 0.05)

def lowpass(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.zeros_like(x); prev = 0.0
    for i, v in enumerate(x):
        prev = (1 - a) * v + a * prev; y[i] = prev
    return y

def pad(freqs, dur, lfo=0.125):
    tt = t(dur)
    s = np.zeros_like(tt)
    for i, f in enumerate(freqs):
        f = round(f * dur) / dur
        s += np.sin(2 * np.pi * f * tt) * (0.6 + 0.4 * np.sin(2 * np.pi * lfo * tt + i))
        s += 0.3 * np.sin(2 * np.pi * f * 2 * tt)
    return s

def note(n): return 440.0 * 2 ** ((n - 69) / 12)

save("ambient_day", pad([note(57), note(61), note(64), note(69)], 16.0) + 0.04 * lowpass(rng.standard_normal(int(SR*16)), 900))
save("ambient_night", pad([note(50), note(53), note(57), note(62)], 16.0, 0.0625))

n = int(SR * 8)
noise = lowpass(rng.standard_normal(n), 500)
fade = int(SR * 1.0)
loop = noise[:n - fade].copy()
loop[:fade] = loop[:fade] * np.linspace(0, 1, fade) + noise[n - fade:] * np.linspace(1, 0, fade)
gust = 0.7 + 0.3 * np.sin(2 * np.pi * (1 / 7) * t(len(loop) / SR))
save("wind_loop", loop * gust)

tt = t(0.07)
click = lowpass(rng.standard_normal(len(tt)), 3000) * np.exp(-60 * tt) + 0.6 * np.sin(2 * np.pi * 1400 * tt) * np.exp(-50 * tt)
save("build_click", click)

tt = t(0.05)
save("select_tick", np.sin(2 * np.pi * 2200 * tt) * np.exp(-90 * tt))

tt = t(0.25)
save("error_buzz", np.sign(np.sin(2 * np.pi * 140 * tt)) * 0.4 * np.exp(-8 * tt) * env(len(tt), 0.005, 0.03))

chime = np.concatenate([bell(note(76), 0.6, 4), bell(note(83), 1.6, 2.5)])
tt = t(len(chime) / SR)
birds = np.zeros_like(tt)
for start in [0.35, 0.55, 1.05, 1.2]:
    i0 = int(start * SR); m = int(0.09 * SR)
    tc = np.arange(m) / SR
    sweep = np.sin(2 * np.pi * (3200 + 1800 * np.sin(2 * np.pi * 22 * tc)) * tc) * np.hanning(m)
    birds[i0:i0 + m] += 0.25 * sweep
save("dawn_chime", chime + birds)

tt = t(3.0)
save("dusk_pad", pad([note(45), note(52), note(57)], 3.0, 0.3) * env(len(tt), 1.2, 1.2))

tt = t(1.2)
freq = np.linspace(420, 110, len(tt))
phase = 2 * np.pi * np.cumsum(freq) / SR
hum = np.sin(2 * np.pi * 50 * tt) + 0.5 * np.sin(2 * np.pi * 100 * tt)
hum[int(0.25 * SR):] *= np.exp(-30 * (tt[int(0.25 * SR):] - 0.25))
save("blackout", 0.8 * np.sin(phase) * np.exp(-1.6 * tt) + 0.35 * hum)

restored = np.concatenate([bell(note(n_), 0.18 if i < 2 else 1.2, 5 if i < 2 else 2.2) for i, n_ in enumerate([72, 76, 79])])
save("restored", restored)

tt = t(3.2)
chord = sum(bell(note(n_), 3.2, 1.0) for n_ in [60, 64, 67, 72]) + 0.5 * pad([note(48)], 3.2) * np.exp(-0.8 * tt)
save("results_chord", chord)
print("ok")
