import Vokaturi
import wave
import numpy as np
import sys
import os

# Charger la DLL
dll_path = os.path.join(os.path.dirname(__file__), "OpenVokaturi-4-0-win64.dll")
Vokaturi.load(dll_path)

def analyze(file_path):
    with wave.open(file_path, "rb") as f:
        num_frames = f.getnframes()
        audio = f.readframes(num_frames)
        sample_rate = f.getframerate()
        samples = np.frombuffer(audio, dtype=np.int16)
        samples = samples.astype(np.float32) / 32768.0

        voice = Vokaturi.Voice(sample_rate, len(samples))
        voice.fill(samples)

        quality = Vokaturi.Quality()
        emotion_probabilities = Vokaturi.EmotionProbabilities()
        voice.extract(quality, emotion_probabilities)

        if quality.valid:
            print(f"NEUTRAL={emotion_probabilities.neutrality:.3f}")
            print(f"HAPPY={emotion_probabilities.happiness:.3f}")
            print(f"SAD={emotion_probabilities.sadness:.3f}")
            print(f"ANGRY={emotion_probabilities.anger:.3f}")
            print(f"FEAR={emotion_probabilities.fear:.3f}")
        else:
            print("INVALID")

        voice.destroy()

# Appel principal
if __name__ == "__main__":
    wav_path = sys.argv[1]
    analyze(wav_path)
