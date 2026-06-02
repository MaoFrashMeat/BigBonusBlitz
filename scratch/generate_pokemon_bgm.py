import wave
import struct
import math

SAMPLE_RATE = 44100
BPM = 170
BEAT_DUR = 60.0 / BPM
QUARTER = BEAT_DUR
EIGHTH = BEAT_DUR / 2
SIXTEENTH = BEAT_DUR / 4

notes = {
    'R': 0.0,
    'G2': 98.00, 'A2': 110.00, 'Bb2': 116.54, 'C3': 130.81, 'D3': 146.83, 'Eb3': 155.56, 'F3': 174.61, 'Gb3': 185.00, 'G3': 196.00,
    'A3': 220.00, 'Bb3': 233.08, 'B3': 246.94, 'C4': 261.63, 'Db4': 277.18, 'D4': 293.66, 'Eb4': 311.13, 'E4': 329.63, 'F4': 349.23,
    'Gb4': 369.99, 'G4': 392.00, 'Ab4': 415.30, 'A4': 440.00, 'Bb4': 466.16, 'B4': 493.88,
    'C5': 523.25, 'Db5': 554.37, 'D5': 587.33, 'Eb5': 622.25, 'E5': 659.25, 'F5': 698.46, 'Gb5': 739.99, 'G5': 783.99, 
    'Ab5': 830.61, 'A5': 880.00, 'Bb5': 932.33, 'B5': 987.77,
    'C6': 1046.50, 'D6': 1174.66, 'Eb6': 1244.51
}

def generate_square_wave(frequency, duration, volume=0.3, duty_cycle=0.5):
    if frequency == 0:
        return [0] * int(SAMPLE_RATE * duration)
    
    num_samples = int(SAMPLE_RATE * duration)
    wave_data = []
    period = SAMPLE_RATE / frequency
    for i in range(num_samples):
        env = 1.0
        if i < 0.01 * SAMPLE_RATE:
            env = i / (0.01 * SAMPLE_RATE)
        elif i > num_samples - 0.01 * SAMPLE_RATE:
            env = (num_samples - i) / (0.01 * SAMPLE_RATE)
        
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
        if i < 0.01 * SAMPLE_RATE:
            env = i / (0.01 * SAMPLE_RATE)
        elif i > num_samples - 0.01 * SAMPLE_RATE:
            env = (num_samples - i) / (0.01 * SAMPLE_RATE)
        
        pos = (i % period) / period
        if pos < 0.25: val = pos * 4
        elif pos < 0.75: val = 1.0 - (pos - 0.25) * 4
        else: val = -1.0 + (pos - 0.75) * 4
        wave_data.append(val * volume * env)
    return wave_data

intro_melody = [
    ('G5', SIXTEENTH), ('F5', SIXTEENTH), ('Eb5', SIXTEENTH), ('D5', SIXTEENTH),
    ('C5', SIXTEENTH), ('Bb4', SIXTEENTH), ('A4', SIXTEENTH), ('G4', SIXTEENTH),
    ('F4', SIXTEENTH), ('Eb4', SIXTEENTH), ('D4', SIXTEENTH), ('C4', SIXTEENTH),
    ('Bb3', SIXTEENTH), ('A3', SIXTEENTH), ('G3', SIXTEENTH), ('F3', SIXTEENTH),
    
    ('G5', SIXTEENTH), ('D5', SIXTEENTH), ('Bb4', SIXTEENTH), ('G4', SIXTEENTH),
    ('D5', SIXTEENTH), ('Bb4', SIXTEENTH), ('G4', SIXTEENTH), ('D4', SIXTEENTH),
    ('Bb4', SIXTEENTH), ('G4', SIXTEENTH), ('D4', SIXTEENTH), ('Bb3', SIXTEENTH),
    ('G4', SIXTEENTH), ('D4', SIXTEENTH), ('Bb3', SIXTEENTH), ('G3', SIXTEENTH),
]
intro_bass = [('G2', EIGHTH)] * 16

melody_loop = [
    ('G4', EIGHTH), ('D5', EIGHTH), ('G5', EIGHTH), ('F5', EIGHTH),
    ('Eb5', EIGHTH), ('D5', EIGHTH), ('C5', EIGHTH), ('Bb4', EIGHTH),
    ('C5', EIGHTH), ('D5', EIGHTH), ('Eb5', EIGHTH), ('C5', EIGHTH),
    ('D5', QUARTER), ('R', QUARTER),
    ('G4', EIGHTH), ('D5', EIGHTH), ('G5', EIGHTH), ('F5', EIGHTH),
    ('Eb5', EIGHTH), ('D5', EIGHTH), ('C5', EIGHTH), ('Bb4', EIGHTH),
    ('C5', EIGHTH), ('Bb4', EIGHTH), ('A4', EIGHTH), ('G4', EIGHTH),
    ('G4', QUARTER), ('R', QUARTER),
]

bass_loop = [
    ('G2', SIXTEENTH), ('G2', SIXTEENTH), ('R', SIXTEENTH), ('G2', SIXTEENTH),
    ('G2', SIXTEENTH), ('G2', SIXTEENTH), ('R', SIXTEENTH), ('G2', SIXTEENTH),
    ('G2', SIXTEENTH), ('G2', SIXTEENTH), ('R', SIXTEENTH), ('G2', SIXTEENTH),
    ('G2', SIXTEENTH), ('G2', SIXTEENTH), ('R', SIXTEENTH), ('G2', SIXTEENTH),
    
    ('Eb3', SIXTEENTH), ('Eb3', SIXTEENTH), ('R', SIXTEENTH), ('Eb3', SIXTEENTH),
    ('Eb3', SIXTEENTH), ('Eb3', SIXTEENTH), ('R', SIXTEENTH), ('Eb3', SIXTEENTH),
    ('D3', SIXTEENTH), ('D3', SIXTEENTH), ('R', SIXTEENTH), ('D3', SIXTEENTH),
    ('D3', SIXTEENTH), ('D3', SIXTEENTH), ('R', SIXTEENTH), ('D3', SIXTEENTH),

    ('G2', SIXTEENTH), ('G2', SIXTEENTH), ('R', SIXTEENTH), ('G2', SIXTEENTH),
    ('G2', SIXTEENTH), ('G2', SIXTEENTH), ('R', SIXTEENTH), ('G2', SIXTEENTH),
    ('G2', SIXTEENTH), ('G2', SIXTEENTH), ('R', SIXTEENTH), ('G2', SIXTEENTH),
    ('G2', SIXTEENTH), ('G2', SIXTEENTH), ('R', SIXTEENTH), ('G2', SIXTEENTH),
    
    ('C3', SIXTEENTH), ('C3', SIXTEENTH), ('R', SIXTEENTH), ('C3', SIXTEENTH),
    ('D3', SIXTEENTH), ('D3', SIXTEENTH), ('R', SIXTEENTH), ('D3', SIXTEENTH),
    ('G2', SIXTEENTH), ('G2', SIXTEENTH), ('R', SIXTEENTH), ('G2', SIXTEENTH),
    ('G2', SIXTEENTH), ('G2', SIXTEENTH), ('R', SIXTEENTH), ('G2', SIXTEENTH),
]

melody = intro_melody + melody_loop * 4
bass = intro_bass + bass_loop * 4

melody_track = []
for n, d in melody:
    melody_track.extend(generate_square_wave(notes[n], d, volume=0.2, duty_cycle=0.25))

bass_track = []
for n, d in bass:
    bass_track.extend(generate_square_wave(notes[n], d, volume=0.25, duty_cycle=0.5))

mixed = []
max_len = max(len(melody_track), len(bass_track))
for i in range(max_len):
    m = melody_track[i] if i < len(melody_track) else 0.0
    b = bass_track[i] if i < len(bass_track) else 0.0
    val = m + b
    if val > 1.0: val = 1.0
    if val < -1.0: val = -1.0
    mixed.append(val)

with wave.open('assets/bgm/bgm_pokemon.wav', 'w') as wf:
    wf.setnchannels(1)
    wf.setsampwidth(2)
    wf.setframerate(SAMPLE_RATE)
    for sample in mixed:
        packed = struct.pack('<h', int(sample * 32767.0))
        wf.writeframes(packed)
print("Pokemon style BGM generated successfully")
