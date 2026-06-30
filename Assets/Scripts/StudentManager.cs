using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class StudentManager : MonoBehaviour
{
    [Header("UI Elementleri")]
    public TextMeshProUGUI txtCurrentWord; 
    public Button btnRecord;              
    public Button btnNextWord;           
    public Button btnListen;              

    [Header("Ses Ayarları")]
    private AudioClip recordingClip;
    private bool isRecording = false;
    private int sampleRate = 44100;
    private AudioSource audioSource;      


    private List<string> assignedWords = new List<string>();
    private int currentWordIndex = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        LoadAssignment();
        UpdateUI();

        btnRecord.onClick.AddListener(OnRecordButtonClicked);
        btnNextWord.onClick.AddListener(NextWord);
        btnListen.onClick.AddListener(PlayCurrentWordSound);
    }

    void LoadAssignment()
    {
        string rawWords = PlayerPrefs.GetString("CurrentAssignment", "");

        if (!string.IsNullOrEmpty(rawWords))
        {
            assignedWords = new List<string>(rawWords.Split(','));
            currentWordIndex = 0;
            Debug.Log($"Toplam {assignedWords.Count} ödev kelimesi yüklendi.");
        }
        else
        {
            assignedWords = new List<string> { "Ödev Bulunamadı!" };
            btnRecord.interactable = false;
            btnNextWord.interactable = false;
            btnListen.interactable = false;
        }
    }

    void UpdateUI()
    {
        if (assignedWords.Count > 0 && currentWordIndex < assignedWords.Count)
        {
            txtCurrentWord.text = assignedWords[currentWordIndex].ToUpper();

            if (currentWordIndex == assignedWords.Count - 1)
            {
                btnNextWord.GetComponentInChildren<TextMeshProUGUI>().text = "Ödevi Bitir";
            }
        }
    }

   
    public void PlayCurrentWordSound()
    {
        if (assignedWords.Count == 0 || currentWordIndex >= assignedWords.Count) return;

      
        string currentWord = assignedWords[currentWordIndex].ToLower().Trim();

       
        AudioClip wordSound = Resources.Load<AudioClip>(currentWord);

        if (wordSound != null)
        {
            audioSource.clip = wordSound;
            audioSource.Play();
            Debug.Log($"{currentWord} sesi oynatılıyor...");
        }
        else
        {
            Debug.LogWarning($"Resources klasöründe '{currentWord}' isimli bir ses dosyası bulunamadı!");
        }
    }

    void OnRecordButtonClicked()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("Mikrofon bulunamadı!");
            return;
        }

        if (!isRecording)
        {
            isRecording = true;
            btnRecord.image.color = Color.red;
            btnRecord.GetComponentInChildren<TextMeshProUGUI>().text = "KAYDI DURDUR";

            recordingClip = Microphone.Start(null, false, 10, sampleRate);
        }
        else
        {
            isRecording = false;
            btnRecord.image.color = Color.white;
            btnRecord.GetComponentInChildren<TextMeshProUGUI>().text = "SESİ KAYDET";

            int microphonePosition = Microphone.GetPosition(null);
            Microphone.End(null);

            if (microphonePosition > 0)
            {
                SaveRecording(microphonePosition);
            }
        }
    }

    void SaveRecording(int lastPosition)
    {
        float[] samples = new float[lastPosition * recordingClip.channels];
        recordingClip.GetData(samples, 0);

        AudioClip trimmedClip = AudioClip.Create("Recording", lastPosition, recordingClip.channels, sampleRate, false);
        trimmedClip.SetData(samples, 0);

        string currentWord = assignedWords[currentWordIndex];
        string fileName = $"Kelime_{currentWord}.wav";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        byte[] wavData = ConvertToWav(trimmedClip);
        File.WriteAllBytes(filePath, wavData);

        Debug.Log($"Ses başarıyla kaydedildi! Yol: {filePath}");
    }

    void NextWord()
    {
        if (currentWordIndex < assignedWords.Count - 1)
        {
            currentWordIndex++;
            UpdateUI();
        }
        else
        {
            Debug.Log("Tüm ödev kelimeleri tamamlandı!");
            txtCurrentWord.text = "Tebrikler!\nÖdev Bitti.";
            btnRecord.interactable = false;
            btnNextWord.interactable = false;
            btnListen.interactable = false;
        }
    }

    private byte[] ConvertToWav(AudioClip clip)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];
        Byte[] bytesData = new Byte[samples.Length * 2];
        int rescaleFactor = 32767;

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (Int16)(samples[i] * rescaleFactor);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write((Int32)(36 + bytesData.Length));
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write((Int32)16);
                writer.Write((Int16)1);
                writer.Write((Int16)clip.channels);
                writer.Write((Int32)clip.frequency);
                writer.Write((Int32)(clip.frequency * clip.channels * 2));
                writer.Write((Int16)(clip.channels * 2));
                writer.Write((Int16)16);
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write((Int32)bytesData.Length);
                writer.Write(bytesData);
            }
            return stream.ToArray();
        }
    }
}