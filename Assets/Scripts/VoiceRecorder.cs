using UnityEngine;
using System.IO;
using UnityEngine.UI;

public class VoiceRecorder : MonoBehaviour
{
    public Button playButton;
    public Button stopButton;

    private AudioClip recordedClip;
    private string microphoneDevice;
    private bool isRecording = false;
    private string filePath;

    void Start()
    {
        playButton.onClick.AddListener(StartRecording);
        stopButton.onClick.AddListener(StopRecording);

        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log(" Micro utilisé : " + microphoneDevice);
        }
        else
        {
            Debug.LogError(" Aucun micro détecté !");
        }

        // Crée le chemin complet du fichier
        filePath = Path.Combine(Application.dataPath, "EmotionAnalysis/test.wav");
    }

    void StartRecording()
    {
        if (microphoneDevice == null) return;

        Debug.Log("Enregistrement démarré...");
        recordedClip = Microphone.Start(microphoneDevice, false, 30, 44100); // max 30s
        isRecording = true;
    }

    void StopRecording()
    {
        if (!isRecording) return;

        int position = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);

        if (position > 0)
        {
            AudioClip trimmedClip = TrimSilence(recordedClip, position);
            WavUtility.SaveWav(filePath, trimmedClip);
            Debug.Log("Fichier .wav sauvegardé : " + filePath);
        }
        else
        {
            Debug.LogWarning(" Aucun son détecté !");
        }

        isRecording = false;
    }

    AudioClip TrimSilence(AudioClip clip, int position)
    {
        float[] samples = new float[position * clip.channels];
        clip.GetData(samples, 0);

        AudioClip newClip = AudioClip.Create(clip.name + "_trimmed", position, clip.channels, clip.frequency, false);
        newClip.SetData(samples, 0);
        return newClip;
    }
}
