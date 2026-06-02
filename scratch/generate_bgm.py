import wave
import struct
import math

SAMPLE_RATE = 44100
BPM = 120
BEAT_DUR = 60.0 / BPM
QUARTER = BEAT_DUR
EIGHTH = BEAT_DUR / 2
SIXTEENTH = BEAT_DUR / 4

notes = {
    'F2': 87.31, 'G2': 98.00, 'A2': 110.00, 'B2': 123.47,
    'C3': 130.81, 'D3': 146.83, 'E3': 164.81, 'F3': 174.61, 'G3': 196.00, 'A3': 220.00, 'B3': 246.94,
    'C4': 261.63, 'D4': 293.66, 'E4': 329.63, 'F4': 349.23, 'G4': 392.00, 'A4': 440.00, 'B4': 493.88,
    'C5': 523.25, 'D5': 587.33, 'E5': 659.25, 'F5': 698.46, 'G5': 783.99, 'A5': 880.00, 'B5': 987.77,
    'C6': 1046.50, 'R': 0.0
}

def generate_square_wave(frequency, duration, volume=0.3, duty_cycle=0.5):
    if frequency == 0:
        return [0] * int(SAMPLE_RATE * duration)
    
    num_samples = int(SAMPLE_RATE * duration)
    wave_data = []
    period = SAMPLE_RATE / frequency
    for i in range(num_samples):
        env = 1.0
        if i < 0.05 * SAMPLE_RATE:
            env = i / (0.05 * SAMPLE_RATE)
        elif i > num_samples - 0.05 * SAMPLE_RATE:
            env = (num_samples - i) / (0.05 * SAMPLE_RATE)
        
        val = 1.0 if (i % period) < (period * duty_cycle) else -1.0
        wave_data.append(val * volume * env)
    return wave_data

def generate_triangle_wave(frequency, duration, volume=0.4):
    if frequency == 0:
        return [0] * int(SAMPLE_RATE * duration)
    num_samples = int(SAMPLE_RATE * duration)
    wave_data = []
    period = SAMPLE_RATE / frequency
    for i in range(num_samples):
        env = 1.0
        if i < 0.05 * SAMPLE_RATE:
            env = i / (0.05 * SAMPLE_RATE)
        elif i > num_samples - 0.05 * SAMPLE_RATE:
            env = (num_samples - i) / (0.05 * SAMPLE_RATE)
        
        pos = (i % period) / period
        if pos < 0.25: val = pos * 4
        elif pos < 0.75: val = 1.0 - (pos - 0.25) * 4
        else: val = -1.0 + (pos - 0.75) * 4
        wave_data.append(val * volume * env)
    return wave_data

melody = [
    ('C5', EIGHTH), ('E5', EIGHTH), ('G5', QUARTER),
    ('F5', EIGHTH), ('E5', EIGHTH), ('D5', QUARTER),
    ('E5', EIGHTH), ('C5', EIGHTH), ('G4', QUARTER),
    ('A4', EIGHTH), ('B4', EIGHTH), ('C5', QUARTER),
    
    ('C5', EIGHTH), ('E5', EIGHTH), ('G5', QUARTER),
    ('A5', EIGHTH), ('G5', EIGHTH), ('F5', QUARTER),
    ('G5', EIGHTH), ('E5', EIGHTH), ('C5', QUARTER),
    ('D5', EIGHTH), ('E5', EIGHTH), ('C5', QUARTER),
]

bass = [
    ('C3', QUARTER), ('G3', QUARTER), ('F2', QUARTER), ('G2', QUARTER),
    ('C3', QUARTER), ('G3', QUARTER), ('F2', QUARTER), ('G2', QUARTER),
    ('A2', QUARTER), ('E3', QUARTER), ('F2', QUARTER), ('C3', QUARTER),
    ('G2', QUARTER), ('D3', QUARTER), ('C3', QUARTER), ('G2', QUARTER),
]

melody_track = []
for n, d in melody * 4: # Loop 4 times
    melody_track.extend(generate_square_wave(notes[n], d, volume=0.15, duty_cycle=0.25))

bass_track = []
for n, d in bass * 4:
    bass_track.extend(generate_triangle_wave(notes[n], d, volume=0.25))

mixed = []
max_len = max(len(melody_track), len(bass_track))
for i in range(max_len):
    m = melody_track[i] if i < len(melody_track) else 0.0
    b = bass_track[i] if i < len(bass_track) else 0.0
    val = m + b
    if val > 1.0: val = 1.0
    if val < -1.0: val = -1.0
    mixed.append(val)

with wave.open('assets/bgm_grassland.wav', 'w') as wf:
    wf.setnchannels(1)
    wf.setsampwidth(2)
    wf.setframerate(SAMPLE_RATE)
    for sample in mixed:
        packed = struct.pack('<h', int(sample * 32767.0))
        wf.writeframes(packed)
print("BGM generated successfully at assets/bgm_grassland.wav")
