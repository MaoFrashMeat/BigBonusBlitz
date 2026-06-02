import wave
import struct
import math
import os

SAMPLE_RATE = 44100

notes_freq = {
    'R': 0.0,
    # Sub bass (Woofer)
    'C1': 32.70, 'Db1': 34.65, 'D1': 36.71, 'Eb1': 38.89, 'E1': 41.20, 'F1': 43.65, 'Gb1': 46.25, 'G1': 49.00, 'Ab1': 51.91, 'A1': 55.00, 'Bb1': 58.27, 'B1': 61.74, 
    # Mid bass
    'C2': 65.41, 'Db2': 69.30, 'D2': 73.42, 'Eb2':77.78, 'E2': 82.41, 'F2': 87.31, 'Gb2': 92.50, 'G2': 98.00, 'Ab2': 103.83, 'A2': 110.00, 'Bb2': 116.54, 'B2': 123.47,
    # Mid
    'C3': 130.81, 'Db3': 138.59, 'D3': 146.83, 'Eb3': 155.56, 'E3': 164.81, 'F3': 174.61, 'Gb3': 185.00, 'G3': 196.00, 'Ab3': 207.65, 'A3': 220.00, 'Bb3': 233.08, 'B3': 246.94,
    'C4': 261.63, 'Db4': 277.18, 'D4': 293.66, 'Eb4': 311.13, 'E4': 329.63, 'F4': 349.23, 'Gb4': 369.99, 'G4': 392.00, 'Ab4': 415.30, 'A4': 440.00, 'Bb4': 466.16, 'B4': 493.88,
    # High (Melody)
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

def generate_track(filename, bpm, melody, bass, sub_bass, loops=4):
    print(f"Generating {filename}...")
    beat_dur = 60.0 / bpm
    
    mixed = []
    
    track_m = []
    for n, d in melody * loops:
        track_m.extend(generate_wave(notes_freq[n], d * beat_dur, 0.20, 'square12', 'staccato'))
        
    track_b = []
    for n, d in bass * loops:
        track_b.extend(generate_wave(notes_freq[n], d * beat_dur, 0.20, 'square50', 'staccato'))
        
    track_sb = [] 
    for n, d in sub_bass * loops:
        # Woofer! Heavy sine wave sub bass
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

# Q = Quarter(1.0), E = Eighth(0.5), S = Sixteenth(0.25)
q = 1.0; e = 0.5; s = 0.25

# 1. Wild Battle (170 BPM)
melody1 = [('G5', s), ('F5', s), ('Eb5', s), ('D5', s)] * 4 + [('Bb4', e), ('C5', e), ('D5', q), ('Eb5', e), ('D5', e), ('C5', q)]
bass1 = [('G2', s), ('G2', s), ('R', s), ('G2', s)] * 4 + [('Eb2', e), ('F2', e), ('G2', q), ('F2', e), ('Eb2', e), ('D2', q)]
sub1 = [('G1', q)] * 4 + [('Eb1', e), ('F1', e), ('G1', q), ('F1', e), ('Eb1', e), ('D1', q)]
generate_track('bgm_poke_wild.wav', 170, melody1, bass1, sub1)

# 2. Trainer Battle (160 BPM)
melody2 = [('E5', e), ('B4', e), ('E5', e), ('Gb5', e), ('G5', q), ('Gb5', q), ('E5', e), ('D5', e), ('E5', e), ('Gb5', e), ('E5', q), ('R', q)]
bass2 = [('E2', s)] * 16 + [('D2', s)] * 8 + [('E2', s)] * 8
sub2 = [('E1', q), ('E1', q), ('E1', q), ('E1', q), ('D1', q), ('D1', q), ('E1', q), ('E1', q)]
generate_track('bgm_poke_trainer.wav', 160, melody2, bass2, sub2)

# 3. Gym Leader (180 BPM)
melody3 = [('A5', s), ('A5', s), ('E5', e), ('A5', s), ('A5', s), ('C6', e), ('B5', e), ('A5', e), ('G5', e), ('E5', e)] * 2
bass3 = [('A2', s), ('R', s), ('A2', s), ('R', s)] * 4 + [('F2', e), ('G2', e), ('A2', q)] * 2
sub3 = [('A1', q)] * 4 + [('F1', e), ('G1', e), ('A1', q)] * 2
generate_track('bgm_poke_gym.wav', 180, melody3, bass3, sub3)

# 4. Rival Battle (150 BPM)
melody4 = [('C5', e), ('G4', e), ('C5', e), ('D5', e), ('Eb5', q), ('D5', q), ('C5', e), ('Bb4', e), ('G4', q), ('C5', q)]
bass4 = [('C2', e), ('C2', e), ('G2', e), ('C2', e)] * 2 + [('Bb1', e), ('Bb1', e), ('F2', e), ('Bb1', e), ('C2', e), ('C2', e), ('G2', e), ('C2', e)]
sub4 = [('C1', q), ('C1', q)] * 2 + [('Bb0', q), ('Bb0', q), ('C1', q), ('C1', q)] # Sub bass extremely low
# Bb0 might be too low, let's use Bb1
sub4 = [('C1', q), ('C1', q)] * 2 + [('Bb1', q), ('Bb1', q), ('C1', q), ('C1', q)]
generate_track('bgm_poke_rival.wav', 150, melody4, bass4, sub4)

# 5. Champion (190 BPM) - Intense woofer
melody5 = [('D6', s), ('Db6', s), ('C6', s), ('B5', s)] * 4 + [('A5', e), ('G5', e), ('F5', e), ('E5', e), ('D5', q), ('R', q)]
bass5 = [('D2', s), ('D2', s), ('D2', s), ('D2', s)] * 4 + [('A1', e), ('G1', e), ('F1', e), ('E1', e), ('D1', q), ('R', q)]
sub5 = [('D1', q), ('D1', q), ('D1', q), ('D1', q)] + [('A1', e), ('G1', e), ('F1', e), ('E1', e), ('D1', q), ('R', q)]
generate_track('bgm_poke_champion.wav', 190, melody5, bass5, sub5)

# 6. Route (120 BPM)
melody6 = [('C5', q), ('E5', q), ('G5', q), ('E5', q), ('A5', e), ('G5', e), ('F5', e), ('E5', e), ('D5', q), ('R', q)]
bass6 = [('C3', e), ('G2', e)] * 4 + [('F2', e), ('C3', e)] * 2 + [('G2', e), ('D3', e)] * 2
sub6 = [('C2', q), ('C2', q), ('C2', q), ('C2', q), ('F1', q), ('F1', q), ('G1', q), ('G1', q)]
generate_track('bgm_poke_route.wav', 120, melody6, bass6, sub6)

# 7. Cave (90 BPM) - Echoey high notes, heavy low sub
melody7 = [('C6', s), ('R', q+e+s), ('Eb6', s), ('R', q+e+s)]
bass7 = [('C2', q), ('R', q), ('Ab1', q), ('R', q)]
sub7 = [('C1', q), ('R', q), ('Ab0', q), ('R', q)] # Extremely low rumble (approx 32Hz, Ab0 = 25Hz wait, we don't have Ab0, let's add it or use Ab1)
notes_freq['Ab0'] = 25.96
sub7 = [('C1', q), ('R', q), ('Ab0', q), ('R', q)]
generate_track('bgm_poke_cave.wav', 90, melody7, bass7, sub7)

# 8. Surf (130 BPM) - Waltz-like 3/4 feel, but we'll adapt to 4/4
melody8 = [('E5', q), ('C5', e), ('D5', e), ('E5', q), ('F5', q), ('G5', q), ('E5', q), ('C5', q), ('R', q)]
bass8 = [('C3', e), ('G3', e), ('E3', e), ('G3', e)] * 2 + [('F2', e), ('C3', e), ('A2', e), ('C3', e)] * 2
sub8 = [('C2', q), ('R', q), ('C2', q), ('R', q), ('F1', q), ('R', q), ('F1', q), ('R', q)]
generate_track('bgm_poke_surf.wav', 130, melody8, bass8, sub8)

# 9. Bicycle (170 BPM) - Fast and upbeat
melody9 = [('C5', e), ('E5', e), ('G5', e), ('C6', e), ('B5', e), ('G5', e), ('A5', e), ('F5', e), ('G5', q), ('E5', q), ('C5', q), ('R', q)]
bass9 = [('C3', e), ('R', e), ('E3', e), ('R', e), ('G3', e), ('R', e), ('F3', e), ('R', e), ('E3', e), ('R', e), ('C3', e), ('R', e), ('G2', e), ('R', e), ('C3', e), ('R', e)]
sub9 = [('C2', q), ('E2', q), ('G2', q), ('F2', q), ('E2', q), ('C2', q), ('G1', q), ('C2', q)]
generate_track('bgm_poke_bicycle.wav', 170, melody9, bass9, sub9)

# 10. Town (100 BPM) - Relaxing
melody10 = [('C5', q), ('D5', e), ('E5', e), ('F5', q), ('E5', q), ('D5', q), ('B4', q), ('C5', q), ('R', q)]
bass10 = [('C3', q), ('G2', q), ('F2', q), ('C3', q), ('G2', q), ('G2', q), ('C3', q), ('R', q)]
sub10 = [('C2', q), ('G1', q), ('F1', q), ('C2', q), ('G1', q), ('G1', q), ('C2', q), ('R', q)]
generate_track('bgm_poke_town.wav', 100, melody10, bass10, sub10)

print("All 10 Pokemon style BGMs generated successfully")
