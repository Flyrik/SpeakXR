using UnityEngine;
using UnityEngine.UI;
using Microsoft.CognitiveServices.Speech;
using TMPro;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class SpeechManager : MonoBehaviour
{
    public Button startButton;
    public Button stopButton;
    public TextMeshProUGUI displayText;

    private string azureApiKey = "335bOIFCOH9AyB5FLMRpcvUWiRTkZ6tZNapRey57CjyygqjOJZsjJQQJ99BEAC5T7U2XJ3w3AAAYACOGCt1I";
    private string azureRegion = "francecentral";
    private SpeechRecognizer recognizer;
    private string language = "fr-FR";

    private string livePreviewText = "";
    private string finalTranscript = "";
    private bool isRecognizing = false;

    private System.DateTime lastRecognizedTime;
    private int pauseCount = 0;
    private float pauseThresholdSeconds = 2f;
    private string finalTranscriptWithPauses = "";
    private string finalTranscriptWithPausesRichText = "";

    private int fillerCount = 0;
    private float speechStartTime;

    private Queue<string> lastWords = new Queue<string>();
    private int repetitionCount = 0;
    private int repetitionWindowSize = 20;

    private string lastRecognizedText = "";
    private bool fillerDetectedInPartial = false;

    public GameObject speechCanvas;

    public GameObject resultsCanvas;
    public TextMeshProUGUI totalTimeText;
    public TextMeshProUGUI pauseCountText;
    public TextMeshProUGUI repetitionCountText;
    public TextMeshProUGUI hesitationCountText;
    public TextMeshProUGUI stressPercentText;

    [Header("Clipboard Prefab")]
    public GameObject clipboardPrefab;   // glisser votre prefab ici

    private GameObject clipboardInstance;
    private TextMeshProUGUI clipboardText;
    private ScrollRect clipboardScrollRect;
    public float scrollSpeed = 1f;


    public Transform leftHandTransform;   // Référence au Transform de ta main gauche






    void Start()
    {
        startButton.onClick.AddListener(StartRecognition);
        stopButton.onClick.AddListener(StopRecognition);
        displayText.text = "Appuie sur Démarrer pour parler.";
    }

    void Update()
    {
        if (isRecognizing)
            displayText.text = livePreviewText;

        // 2) Toggle du clipboard à l’appui de Y
        if (Input.GetKeyDown(KeyCode.Y) || Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            Debug.Log("[Update] Y press détecté");
            if (clipboardInstance == null)
            {
                Debug.LogWarning("[Update] clipboardInstance est null – as-tu déjà stoppé la reco ?");
            }
            else
            {
                bool now = !clipboardInstance.activeSelf;
                clipboardInstance.SetActive(now);
                Debug.Log($"[Update] clipboardInstance.SetActive({now})");
                if (now && clipboardScrollRect != null)
                {
                    clipboardScrollRect.verticalNormalizedPosition = 1f;
                    Debug.Log("[Update] Scroll reset on open");
                }
            }
        }

        // 3) Scrolling via joystick si affiché
        if (clipboardInstance != null && clipboardInstance.activeSelf)
        {
            if (clipboardScrollRect == null)
            {
                Debug.LogError("[Update] clipboardScrollRect est null !");
                return;
            }
            float v = Input.GetAxis("Vertical");
            if (Mathf.Abs(v) > 0.1f)
            {
                float before = clipboardScrollRect.verticalNormalizedPosition;
                float after = Mathf.Clamp01(before + v * scrollSpeed * Time.deltaTime);
                clipboardScrollRect.verticalNormalizedPosition = after;
                Debug.Log($"[Update] scroll v={v:F2}, pos {before:F2}→{after:F2}");
            }
        }
    }

    async void StartRecognition()
    {
        speechCanvas.SetActive(true);

        var config = SpeechConfig.FromSubscription(azureApiKey, azureRegion);
        config.SetServiceProperty("SpeechServiceResponse_PostProcessingOption", "None", ServicePropertyChannel.UriQueryParameter);
        config.OutputFormat = OutputFormat.Detailed;

        recognizer = new SpeechRecognizer(config, language);

        // Phrase list pour booster la détection des fillers
        var hints = PhraseListGrammar.FromRecognizer(recognizer);
        hints.AddPhrase("euh");
        hints.AddPhrase("euuu");
        hints.AddPhrase("euhhh");
        hints.AddPhrase("hum");
        hints.AddPhrase("heu");
        hints.AddPhrase("mh");
        hints.AddPhrase("mmh");

        livePreviewText = "Parle maintenant...";
        finalTranscript = "";
        finalTranscriptWithPauses = "";
        finalTranscriptWithPausesRichText = "";
        pauseCount = fillerCount = repetitionCount = 0;
        lastWords.Clear();
        lastRecognizedTime = default;
        lastRecognizedText = "";
        fillerDetectedInPartial = false;

        isRecognizing = true;

        recognizer.Recognizing += (s, e) =>
        {
            string partial = e.Result.Text;
            if (!string.IsNullOrWhiteSpace(partial))
            {
                // Détection partielle de fillers
                if (Regex.IsMatch(partial.ToLower(), @"\b(eu+h+|euh+|heu+|hum+|mmh+|mh+)\b"))
                    fillerDetectedInPartial = true;

                livePreviewText = finalTranscriptWithPauses + partial;
            }
        };

        recognizer.Recognized += (s, e) =>
        {
            // Cas où Azure n'a rien reconnu → peut être un filler isolé
            if (e.Result.Reason == ResultReason.NoMatch)
            {
                fillerCount++;
                Debug.Log($"NoMatch detected – possible hesitation. Total fillers = {fillerCount}");
                finalTranscriptWithPauses += " (euh) ";
                finalTranscriptWithPausesRichText += " <span style='color:orange'>(euh)</span> ";
                return;
            }

            if (e.Result.Reason == ResultReason.RecognizedSpeech &&
                !string.IsNullOrWhiteSpace(e.Result.Text) &&
                e.Result.Text != lastRecognizedText)
            {
                lastRecognizedText = e.Result.Text;
                string raw = e.Result.Text;

                // 1) Normalisation des formes prolongées
                string normalized = Regex.Replace(raw, @"\b(eu{2,}h+|euh{2,})\b", "euh", RegexOptions.IgnoreCase);
                normalized = Regex.Replace(normalized, @"\b(hum{2,})\b", "hum", RegexOptions.IgnoreCase);
                normalized = Regex.Replace(normalized, @"\b(m{2,}h{1,}|mh{2,})\b", "mmh", RegexOptions.IgnoreCase);

                // 2) Détection des fillers via regex étendue
                var fillerMatches = Regex.Matches(
                    normalized,
                    @"\b(eu+h+|euh+|heu+|hum+|mmh+|mh+)\b",
                    RegexOptions.IgnoreCase
                );
                if (fillerMatches.Count > 0)
                {
                    fillerCount += fillerMatches.Count;
                    foreach (Match m in fillerMatches)
                        Debug.Log($"Filler détecté : '{m.Value}' → total fillers = {fillerCount}");
                }
                else if (fillerDetectedInPartial)
                {
                    // Cas où le filler était en partiel mais a disparu en final
                    fillerCount++;
                    Debug.Log($"Filler en partiel omis en final → total fillers = {fillerCount}");
                    finalTranscriptWithPauses += " (euh) ";
                    finalTranscriptWithPausesRichText += " <span style='color:orange'>(euh)</span> ";
                }

                // ——— Répétitions ———
                var words = e.Result.Text
                    .Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => w.ToLower().Trim(new char[] { '.', ',', ';', '!', '?', '"', '\'' }))
                    .ToList();
                int localRepetitionCount = 0;
                int maxN = 4;
                // répétitions locales
                for (int i = 0; i < words.Count; i++)
                {
                    for (int n = 1; n <= maxN; n++)
                    {
                        if (i + 2 * n <= words.Count)
                        {
                            var firstSeq = words.GetRange(i, n);
                            var secondSeq = words.GetRange(i + n, n);
                            if (Enumerable.SequenceEqual(firstSeq, secondSeq))
                            {
                                localRepetitionCount++;
                                Debug.Log($"Répetition locale (n={n}) : {string.Join(" ", firstSeq)}");
                                i += n - 1;
                                break;
                            }
                        }
                    }
                }
                // répétitions entre segments
                for (int n = 1; n <= Mathf.Min(maxN, words.Count); n++)
                {
                    if (lastWords.Count >= n)
                    {
                        var prevN = lastWords.Skip(lastWords.Count - n).Take(n).ToList();
                        var currN = words.Take(n).ToList();
                        if (Enumerable.SequenceEqual(prevN, currN))
                        {
                            localRepetitionCount++;
                            Debug.Log($"Répetition entre segments (n={n}) : {string.Join(" ", currN)}");
                            break;
                        }
                    }
                }
                repetitionCount += localRepetitionCount;
                foreach (var w in words)
                {
                    lastWords.Enqueue(w);
                    if (lastWords.Count > repetitionWindowSize)
                        lastWords.Dequeue();
                }

                // ——— Pauses ———
                var currentTime = System.DateTime.Now;
                if (lastRecognizedTime != default)
                {
                    var delta = (currentTime - lastRecognizedTime).TotalSeconds;
                    if (delta > pauseThresholdSeconds)
                    {
                        pauseCount++;
                        finalTranscript += " (...) ";
                        finalTranscriptWithPauses += " (...) ";
                        finalTranscriptWithPausesRichText += " <span style='color:red'>(...)</span> ";
                        Debug.Log($"Pause détectée ({delta:F1}s) – total pauses = {pauseCount}");
                    }
                }
                lastRecognizedTime = currentTime;

                // ——— Ajout du texte final ———
                finalTranscript += raw + " ";
                finalTranscriptWithPauses += raw + " ";
                finalTranscriptWithPausesRichText += raw + " ";

                livePreviewText = raw;
                // Reset du flag pour la prochaine phrase
                fillerDetectedInPartial = false;
            }
        };

        recognizer.Canceled += (s, e) =>
        {
            Debug.LogError("Erreur Azure Speech : " + e.ErrorDetails);
            livePreviewText = "Erreur : " + e.ErrorDetails;
        };

        recognizer.SessionStopped += (s, e) =>
        {
            isRecognizing = false;
            Debug.Log("Session terminée.");
        };

        speechStartTime = Time.time;
        await recognizer.StartContinuousRecognitionAsync();
        Debug.Log("Reconnaissance démarrée.");
    }

    async void StopRecognition()
    {
        if (recognizer != null)
        {
            await recognizer.StopContinuousRecognitionAsync();
            recognizer.Dispose();
            recognizer = null;

            Debug.Log("Reconnaissance arrêtée.");
            isRecognizing = false;
            livePreviewText = "";

            float totalSpeechSeconds = Time.time - speechStartTime;

            float fillersPerMinute = fillerCount / totalSpeechSeconds * 60f;
            float pausesPerMinute = pauseCount / totalSpeechSeconds * 60f;
            float repetitionsPerMinute = repetitionCount / totalSpeechSeconds * 60f;

            float normFillers = Mathf.Clamp01(fillersPerMinute / 20f);
            float normPauses = Mathf.Clamp01(pausesPerMinute / 5f);

            float weightFillers = 0.6f;
            float weightPauses = 0.4f;

            float stressPercent = (normFillers * weightFillers + normPauses * weightPauses) * 100f;

            string formattedTime = System.TimeSpan.FromSeconds(totalSpeechSeconds).ToString(@"hh\:mm\:ss");



            /*displayText.text =
            $"<b>Texte final :</b>\n{finalTranscriptWithPauses}\n\n" +
            $"<b>Durée totale :</b> {formattedTime}\n\n" +
            $"<b>Nombre de pauses :</b> {pauseCount}  |  <b>Pauses/min :</b> {pausesPerMinute:F1}\n" +
            $"<b>Nombre d’hésitations :</b> {fillerCount}  |  <b>Hésitations/min :</b> {fillersPerMinute:F1}\n" +
            $"<b>Répétitions :</b> {repetitionCount}  |  <b>Répétitions/min :</b> {repetitionsPerMinute:F1}\n\n" +
            $"<color=yellow><b>Indice de stress :</b> {stressPercent:F1}%</color>";*/


            totalTimeText.text = $"Temps total : {formattedTime}";
            pauseCountText.text = $"Nombre de pauses : {pauseCount}";
            repetitionCountText.text = $"Nombre de répétitions : {repetitionCount}";
            hesitationCountText.text = $"Nombre d'hésitations : {fillerCount}";
            stressPercentText.text = $"Indice de stress : {stressPercent:F1}%";



            if (clipboardInstance == null)
            {
                if (clipboardPrefab == null || leftHandTransform == null)
                {
                    Debug.LogError("[StopRecognition] prefab ou leftHandTransform non assigné !");
                }
                else
                {
                    clipboardInstance = Instantiate(clipboardPrefab, leftHandTransform, false);

                    // Position & scale à ajuster si nécessaire
                    clipboardInstance.transform.localPosition = new Vector3(0.1f, -0.05f, 0.2f);
                    clipboardInstance.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    clipboardInstance.transform.localScale = Vector3.one * 0.5f;

                    clipboardText = clipboardInstance.GetComponentInChildren<TextMeshProUGUI>(true);
                    clipboardScrollRect = clipboardInstance.GetComponentInChildren<ScrollRect>(true);

                    if (clipboardText == null)
                        Debug.LogError("[StopRecognition] TextMeshProUGUI introuvable (incluant inactifs) !");
                    if (clipboardScrollRect == null)
                        Debug.LogError("[StopRecognition] ScrollRect introuvable (incluant inactifs) !");
                }
            }

            // Remplit et cache
            Debug.Log($"[StopRecognition] Contenu finalTranscriptWithPauses (len={finalTranscriptWithPauses.Length}):\n{finalTranscriptWithPauses}");

            // 2) Choix entre texte brut ou RichText
            string clipboardContent = string.IsNullOrEmpty(finalTranscriptWithPauses)
                ? finalTranscriptWithPausesRichText
                : finalTranscriptWithPauses;

            // 3) Vérifie que clipboardText cible bien ton TMP
            if (clipboardText == null)
            {
                Debug.LogError("[StopRecognition] clipboardText est null !");
            }
            else
            {
                // Log ce qu'on va mettre
                Debug.Log($"[StopRecognition] Mise à jour du clipboardText.text (len={clipboardContent.Length})");
                clipboardText.text = clipboardContent;
            }

            // 4) Cache le panneau jusqu'au toggle
            if (clipboardInstance != null)
                clipboardInstance.SetActive(false);


            SaveTranscriptToTextFile(totalSpeechSeconds);
            SaveTranscriptToHtmlFile(totalSpeechSeconds);

            speechCanvas.SetActive(false);



            pauseCount = 0;
            fillerCount = 0;
            repetitionCount = 0;



        }
    }

    void SaveTranscriptToTextFile(float totalSpeechSeconds)
    {
        string fileName = "Transcription_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        string formattedTime = System.TimeSpan.FromSeconds(totalSpeechSeconds).ToString(@"hh\:mm\:ss");

        string fullText = finalTranscriptWithPauses +
                          $"\n\nDurée totale : {formattedTime}" +
                          $"\nNombre de pauses détectées : {pauseCount}" +
                          $"\nNombre de fillers : {fillerCount}" +
                          $"\nNombre de répétitions : {repetitionCount}";
        File.WriteAllText(path, fullText);
        Debug.Log("Transcription TXT sauvegardée dans : " + path);
    }


    void SaveTranscriptToHtmlFile(float totalSpeechSeconds)
    {
        string fileName = "Transcription_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".html";
        string path = Path.Combine(Application.persistentDataPath, fileName);
        string formattedTime = System.TimeSpan.FromSeconds(totalSpeechSeconds).ToString(@"hh\:mm\:ss");

        // Prépare la transcription avec style enrichi
        string styledTranscript = finalTranscriptWithPausesRichText;

        // Ajout de style rouge pour les hésitations (fillers)
        styledTranscript = Regex.Replace(styledTranscript, @"\b(euh+|heu+|hum+|mmh+)\b", "<span class='filler'>$1</span>", RegexOptions.IgnoreCase);

        // Ajout de style rouge pour les répétitions simples (si visibles dans le texte final)
        var words = finalTranscriptWithPauses.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length - 1; i++)
        {
            if (words[i].ToLower() == words[i + 1].ToLower())
            {
                styledTranscript = Regex.Replace(
                    styledTranscript,
                    $@"\b({Regex.Escape(words[i])}\s+{Regex.Escape(words[i + 1])})\b",
                    "<span class='repetition'>$1</span>",
                    RegexOptions.IgnoreCase
                );
            }
        }

        string html = $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <title>Transcription du pitch</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; line-height: 1.6; }}
        .pause {{ color: red; font-weight: bold; }}
        .filler {{ color: red; font-style: italic; }}
        .repetition {{ color: red; text-decoration: underline; }}
        .stats {{ background-color: #f3f3f3; padding: 10px; border-left: 4px solid #ccc; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <div class='stats'>
        <strong>Durée totale :</strong> {formattedTime}<br/>
        <strong>Nombre de pauses :</strong> {pauseCount}<br/>
        <strong>Nombre d'hésitations :</strong> {fillerCount}<br/>
        <strong>Nombre de répétitions :</strong> {repetitionCount}
    </div>

    <h2>Transcription annotée</h2>
    <p>{styledTranscript}</p>
</body>
</html>";

        File.WriteAllText(path, html);
        Debug.Log("Transcription HTML enrichie sauvegardée dans : " + path);
    }
}
