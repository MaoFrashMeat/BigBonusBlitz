import wave
import struct
import math
import os

SAMPLE_RATE = 44100

notes_freq = {
    'R': 0.0,
    'G0': 24.50, 'Ab0': 25.96, 'A0': 27.50, 'Bb0': 29.14, 'B0': 30.87,
    'C1': 32.70, 'Db1': 34.65, 'D1': 36.71, 'Eb1': 38.89, 'E1': 41.20, 'F1': 43.65, 'Gb1': 46.25, 'G1': 49.00, 'Ab1': 51.91, 'A1': 55.00, 'Bb1': 58.27, 'B1': 61.74, 
    'C2': 65.41, 'Db2': 69.30, 'D2': 73.42, 'Eb2':77.78, 'E2': 82.41, 'F2': 87.31, 'Gb2': 92.50, 'G2': 98.00, 'Ab2': 103.83, 'A2': 110.00, 'Bb2': 116.54, 'B2': 123.47,
    'C3': 130.81, 'Db3': 138.59, 'D3': 146.83, 'Eb3': 155.56, 'E3': 164.81, 'F3': 174.61, 'Gb3': 185.00, 'G3': 196.00, 'Ab3': 207.65, 'A3': 220.00, 'Bb3': 233.08, 'B3': 246.94,
    'C4': 261.63, 'Db4': 277.18, 'D4': 293.66, 'Eb4': 311.13, 'E4': 329.63, 'F4': 349.23, 'Gb4': 369.99, 'G4': 392.00, 'Ab4': 415.30, 'A4': 440.00, 'Bb4': 466.16, 'B4': 493.88,
    'C5': 523.25, 'Db5': 554.37, 'D5': 587.33, 'Eb5': 622.25, 'E5': 659.25, 'F5': 698.46, 'Gb5': 739.99, 'G5': 783.99, 'Ab5': 830.61, 'A5': 880.00, 'Bb5': 932.33, 'B5': 987.77,
    'C6': 1046.50, 'Db6': 1108.73, 'D6': 1174.66, 'Eb6': 1244.51, 'E6': 1318.51, 'F6': 1396.91, 'Gb6': 1479.98, 'G6': 1567.98, 'Ab6': 1661.22, 'A6': 1760.00, 'Bb6': 1864.66, 'B6': 1975.53
}

def generate_wave(frequency, duration, vol, wave_type, envelope='staccato'):
    if frequency == 0:
        return [0] * int(SAMPLE_RATE * duration)
    
    num_samples = int(SAMPLE_RATE * duration)
    wave_data = []
    period = SAMPLE_RATE / frequency
    for i in range(num_samples):
        env = 1.0
        if envelope == 'staccato':
            if i < 0.01 * SAMPLE_RATE: env = i / (0.01 * SAMPLE_RATE)
            elif i > num_samples - 0.02 * SAMPLE_RATE: env = (num_samples - i) / (0.02 * SAMPLE_RATE)
        elif envelope == 'legato':
            if i < 0.05 * SAMPLE_RATE: env = i / (0.05 * SAMPLE_RATE)
            elif i > num_samples - 0.05 * SAMPLE_RATE: env = (num_samples - i) / (0.05 * SAMPLE_RATE)
            
        pos = (i % period) / period
        
        if wave_type == 'square25':
            val = 1.0 if pos < 0.25 else -1.0
        elif wave_type == 'square50':
            val = 1.0 if pos < 0.50 else -1.0
        elif wave_type == 'square12':
            val = 1.0 if pos < 0.125 else -1.0
        elif wave_type == 'triangle':
            if pos < 0.25: val = pos * 4
            elif pos < 0.75: val = 1.0 - (pos - 0.25) * 4
            else: val = -1.0 + (pos - 0.75) * 4
        elif wave_type == 'sine':
            val = math.sin(2 * math.pi * pos)
        
        wave_data.append(val * vol * env)
    return wave_data

def compile_track(filename, bpm, melody, bass, sub_bass):
    print(f"Generating {filename}...")
    beat_dur = 60.0 / bpm
    
    mixed = []
    
    track_m = []
    for n, d in melody:
        track_m.extend(generate_wave(notes_freq[n], d * beat_dur, 0.20, 'square12', 'staccato'))
        
    track_b = []
    for n, d in bass:
        track_b.extend(generate_wave(notes_freq[n], d * beat_dur, 0.20, 'square50', 'staccato'))
        
    track_sb = [] 
    for n, d in sub_bass:
        track_sb.extend(generate_wave(notes_freq[n], d * beat_dur, 0.35, 'sine', 'legato'))

    max_len = max(len(track_m), len(track_b), len(track_sb))
    for i in range(max_len):
        m = track_m[i] if i < len(track_m) else 0.0
        b = track_b[i] if i < len(track_b) else 0.0
        sb = track_sb[i] if i < len(track_sb) else 0.0
        val = m + b + sb
        if val > 1.0: val = 1.0
        if val < -1.0: val = -1.0
        mixed.append(val)

    os.makedirs('assets/bgm', exist_ok=True)
    with wave.open(f'assets/bgm/{filename}', 'w') as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(SAMPLE_RATE)
        for sample in mixed:
            packed = struct.pack('<h', int(sample * 32767.0))
            wf.writeframes(packed)

q = 1.0; e = 0.5; s = 0.25

# ==============================================================
# EPIC TRAINER BATTLE (160 BPM)
# ==============================================================
# A-part (32 beats)
melody_A_t = [('E5', e), ('B4', e), ('E5', e), ('Gb5', e), ('G5', q), ('Gb5', q), ('E5', e), ('D5', e), ('E5', e), ('Gb5', e), ('E5', q), ('R', q)] * 4
bass_A_t = ([('E2', s)] * 16 + [('D2', s)] * 8 + [('E2', s)] * 8) * 4
sub_A_t = ([('E1', q)] * 4 + [('D1', q)] * 2 + [('E1', q)] * 2) * 4

# B-part (16 beats)
melody_B_t = [('A4', q), ('C5', q), ('E5', q), ('C5', q), ('G4', q), ('B4', q), ('D5', q), ('B4', q), ('A4', q), ('C5', q), ('E5', q), ('A5', q), ('B5', q), ('R', q), ('R', e), ('B5', e), ('C6', e), ('D6', e)]
bass_B_t = ([('A2', e)] * 8 + [('G2', e)] * 8 + [('F2', e)] * 8 + [('B2', e)] * 8)
sub_B_t = [('A1', q)] * 4 + [('G1', q)] * 4 + [('F1', q)] * 4 + [('B1', q)] * 4

# C-part (32 beats)
melody_C_t = [
    ('C6', q+e), ('B5', e), ('A5', q), ('G5', q), ('A5', q+e), ('E5', e), ('E5', q), ('R', q),
    ('F5', q+e), ('E5', e), ('D5', q), ('C5', q), ('D5', q+e), ('E5', e), ('E5', q), ('R', q),
    ('C6', q+e), ('B5', e), ('A5', q), ('G5', q), ('A5', q+e), ('E5', e), ('E5', q), ('R', q),
    ('F5', e), ('E5', e), ('D5', e), ('C5', e), ('B4', e), ('C5', e), ('D5', e), ('E5', e), ('A5', q), ('R', q), ('R', e), ('B5', e), ('C6', e), ('D6', e)
]
bass_C_t = ([('A2', s), ('A2', s), ('E3', s), ('A2', s)] * 4 + [('G2', s), ('G2', s), ('D3', s), ('G2', s)] * 4 + [('F2', s), ('F2', s), ('C3', s), ('F2', s)] * 4 + [('E2', s), ('E2', s), ('B2', s), ('E2', s)] * 4) * 2
sub_C_t = ([('A1', q)] * 4 + [('G1', q)] * 4 + [('F1', q)] * 4 + [('E1', q)] * 4) * 2

compile_track('bgm_poke_trainer_epic.wav', 160, melody_A_t + melody_B_t + melody_C_t, bass_A_t + bass_B_t + bass_C_t, sub_A_t + sub_B_t + sub_C_t)

# ==============================================================
# EPIC CHAMPION BATTLE (190 BPM)
# ==============================================================
# A-part (32 beats) - Heroic Arpeggios and climbs instead of creepy descents
melody_A_c = (
    [('D5', s), ('F5', s), ('A5', s), ('D6', s)] * 4 + [('A5', e), ('G5', e), ('F5', e), ('E5', e), ('D5', q), ('R', q)] +
    [('Bb4', s), ('D5', s), ('F5', s), ('Bb5', s)] * 4 + [('F5', e), ('Eb5', e), ('D5', e), ('C5', e), ('Bb4', q), ('R', q)] +
    [('C5', s), ('E5', s), ('G5', s), ('C6', s)] * 4 + [('G5', e), ('F5', e), ('E5', e), ('D5', e), ('C5', q), ('R', q)] +
    [('D5', s), ('F5', s), ('A5', s), ('D6', s)] * 4 + [('A5', e), ('G5', e), ('F5', e), ('E5', e), ('D5', q), ('R', q)]
)
bass_A_c = (
    [('D2', s)] * 16 + [('A1', e), ('G1', e), ('F1', e), ('E1', e), ('D1', q), ('R', q)] +
    [('Bb1', s)] * 16 + [('F1', e), ('Eb1', e), ('D1', e), ('C1', e), ('Bb0', q), ('R', q)] +
    [('C2', s)] * 16 + [('G1', e), ('F1', e), ('E1', e), ('D1', e), ('C1', q), ('R', q)] +
    [('D2', s)] * 16 + [('A1', e), ('G1', e), ('F1', e), ('E1', e), ('D1', q), ('R', q)]
)
sub_A_c = (
    [('D1', q)] * 4 + [('A1', e), ('G1', e), ('F1', e), ('E1', e), ('D1', q), ('R', q)] +
    [('Bb1', q)] * 4 + [('F1', e), ('Eb1', e), ('D1', e), ('C1', e), ('Bb0', q), ('R', q)] +
    [('C1', q)] * 4 + [('G1', e), ('F1', e), ('E1', e), ('D1', e), ('C1', q), ('R', q)] +
    [('D1', q)] * 4 + [('A1', e), ('G1', e), ('F1', e), ('E1', e), ('D1', q), ('R', q)]
)

# B-part (16 beats)
def arpeggio(chord):
    return [(chord[0], s), (chord[1], s), (chord[2], s), (chord[3], s)] * 4
melody_B_c = arpeggio(['D5','F5','A5','D6']) + arpeggio(['Bb4','D5','F5','Bb5']) + arpeggio(['G4','Bb4','D5','G5']) + arpeggio(['A4','Db5','E5','A5'])
bass_B_c = [('D2', e)] * 8 + [('Bb1', e)] * 8 + [('G1', e)] * 8 + [('A1', e)] * 8
sub_B_c = [('D1', q)] * 4 + [('Bb1', q)] * 4 + [('G1', q)] * 4 + [('A1', q)] * 4

# C-part (32 beats) - Syncopated dynamic melody, galloping bass
melody_C_c = [
    ('D6', q+e), ('A5', e), ('F5', e), ('G5', e), ('A5', q),
    ('Bb5', q+e), ('A5', e), ('G5', e), ('F5', e), ('E5', q),
    ('F5', q+e), ('E5', e), ('D5', e), ('E5', e), ('F5', q),
    ('G5', e), ('F5', e), ('E5', e), ('D5', e), ('Db5', q), ('A5', q),
    
    ('D6', q+e), ('A5', e), ('F5', e), ('G5', e), ('A5', q),
    ('Bb5', q+e), ('A5', e), ('G5', e), ('F5', e), ('E5', q),
    ('F5', e), ('G5', e), ('A5', e), ('Bb5', e), ('C6', e), ('D6', e), ('E6', e), ('F6', e),
    ('D6', q), ('R', q), ('D6', q), ('R', q) 
]
def gallop(note):
    return [(note, s), (note, s), ('R', s), (note, s)] * 4
bass_C_c = (gallop('D2') + gallop('A1') + gallop('Bb1') + gallop('A1')) * 2
sub_C_c = ([('D1', q)] * 4 + [('A1', q)] * 4 + [('Bb1', q)] * 4 + [('A1', q)] * 4) * 2

compile_track('bgm_poke_champion_epic.wav', 190, melody_A_c + melody_B_c + melody_C_c, bass_A_c + bass_B_c + bass_C_c, sub_A_c + sub_B_c + sub_C_c)

print("Epic BGMs generated successfully")
