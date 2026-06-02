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
        track_m.extend(generate_wave(notes_freq[n], d * beat_dur, 0.25, 'square12', 'staccato'))
        
    track_b = []
    for n, d in bass:
        track_b.extend(generate_wave(notes_freq[n], d * beat_dur, 0.25, 'square50', 'staccato'))
        
    track_sb = [] 
    for n, d in sub_bass:
        track_sb.extend(generate_wave(notes_freq[n], d * beat_dur, 0.40, 'sine', 'legato'))

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
# PERFECT SYNC BGM (192 BPM)
# 1 Cycle = 8 beats = 2500ms
# Accent placements: Beat 4.5, Beat 5.0, Beat 5.5
# ==============================================================

def sync_bass(note):
    # This creates a bass rhythm that perfectly highlights the slot timings.
    # Beats 1-4: Driving 16ths
    b1 = [(note, s)] * 14 + [('R', e)]
    # Beat 4.5 to 8: The accents
    b2 = [(note, e), (note, e), (note, e), ('R', e), (note, s)] * 1 # wait... 
    # Let's count properly:
    # Measure 1 (beats 1-4): 4 beats. 
    # Measure 2 (beats 5-8): Beat 4.5 is technically measure 2, beat 1.5 in standard counting? No, beats 1, 2, 3, 4, 5, 6, 7, 8.
    # Beat 1.0: downbeat.
    # Beat 4.5: e
    # Beat 5.0: e
    # Beat 5.5: e
    # Let's build exactly 8 beats per cycle.
    # Beats 1.0 to 4.5: (3.5 beats = 14 sixteenths)
    part1 = [(note, s)] * 14
    # Beat 4.5 to 6.0: (1.5 beats)
    # The accents are at 4.5, 5.0, 5.5. Each can be an eighth note (0.5 beats)
    part2 = [(note, e), (note, e), (note, e)]
    # Beat 6.0 to 8.0: (2.0 beats = 8 sixteenths)
    part3 = [(note, s)] * 8
    
    return part1 + part2 + part3

def sync_sub(note):
    # Heavy sustained sub matching the accents
    part1 = [(note, 3.5)]
    part2 = [(note, 0.5), (note, 0.5), (note, 0.5)]
    part3 = [(note, 3.0)] # 8 beats total
    return part1 + part2 + part3

# Let's build a 8-cycle song (64 beats)
# 4 cycles of Dm, 1 cycle of Bb, 1 cycle of C, 2 cycles of Dm
# Cycle 1-4: Intro -> A-part
melody_cycle_1 = [('D6', s), ('A5', s), ('F5', s), ('D5', s)] * 7 + [('E6', e), ('F6', e), ('G6', e)] + [('A6', e), ('G6', e), ('F6', e), ('E6', e), ('D6', q)]
# Wait, 7 * 4 = 28 sixteenths = 7 beats. 1 beat left.
# Let's match the 4.5, 5.0, 5.5 accents in the melody too!
def sync_melody(base_note, accent_notes, tail_notes):
    # base_note 14 sixteenths
    m1 = [(base_note, s)] * 14
    # accents (3 eighths)
    m2 = [(accent_notes[0], e), (accent_notes[1], e), (accent_notes[2], e)]
    # tail (8 sixteenths or custom)
    return m1 + m2 + tail_notes

m_c1 = sync_melody('D5', ['A5', 'F5', 'D5'], [('E5', e), ('F5', e), ('G5', e), ('A5', e)])
m_c2 = sync_melody('F5', ['C6', 'A5', 'F5'], [('G5', e), ('A5', e), ('Bb5', e), ('C6', e)])
m_c3 = sync_melody('A5', ['E6', 'C6', 'A5'], [('Bb5', e), ('C6', e), ('D6', e), ('E6', e)])
m_c4 = sync_melody('D6', ['A6', 'F6', 'D6'], [('E6', e), ('F6', e), ('G6', e), ('A6', e)])

# Cycle 5 (Bb)
m_c5 = sync_melody('Bb5', ['F6', 'D6', 'Bb5'], [('C6', e), ('D6', e), ('Eb6', e), ('F6', e)])
# Cycle 6 (C)
m_c6 = sync_melody('C6', ['G6', 'E6', 'C6'], [('D6', e), ('E6', e), ('F6', e), ('G6', e)])
# Cycle 7 (Dm)
m_c7 = sync_melody('D6', ['A6', 'F6', 'D6'], [('E6', e), ('F6', e), ('G6', e), ('A6', e)])
# Cycle 8 (Dm climax)
m_c8 = sync_melody('D6', ['D6', 'D6', 'D6'], [('A6', q), ('D6', q)])

melody_total = m_c1 + m_c2 + m_c3 + m_c4 + m_c5 + m_c6 + m_c7 + m_c8

bass_total = sync_bass('D2') * 4 + sync_bass('Bb1') + sync_bass('C2') + sync_bass('D2') * 2
sub_total = sync_sub('D1') * 4 + sync_sub('Bb0') + sync_sub('C1') + sync_sub('D1') * 2

compile_track('bgm_poke_sync_192.wav', 192, melody_total, bass_total, sub_total)
print("Sync BGM generated successfully")
