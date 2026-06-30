using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
public class TeacherManager : MonoBehaviour
{
    [Header("UI Elementleri")]
    public Transform contentPanel;      // ScrollView içindeki Content nesnesi
    public GameObject wordPrefab;       // Az önce oluþturduðumuz WordItem_Prefab

    // Sistemdeki örnek kelime havuzu
    private List<string> wordPool = new List<string> { "Araba", "Tren", "Kitap", "Elma", "Güneþ", "Televizyon", "Kaþýk", "Ördek", "Balýk", "Uçak" };

    // Seçilen kelimelerin listesi
    private List<string> selectedWords = new List<string>();

    void Start()
    {
        GenerateWordList();
    }

    // Kelimeleri dinamik olarak ekrana basan fonksiyon
    // Kelimeleri dinamik olarak ekrana basan fonksiyon
    void GenerateWordList()
    {
        foreach (string word in wordPool)
        {
            // Prefab'ý Content panelinin altýnda oluþturuyoruz
            GameObject newWordItem = Instantiate(wordPrefab, contentPanel);
            newWordItem.transform.localScale = Vector3.one;

            // Prefab üzerindeki script'e ulaþýyoruz
            WordItemScript itemScript = newWordItem.GetComponent<WordItemScript>();

            if (itemScript != null && itemScript.wordTextObject != null)
            {
                // TextMeshPro bileþenlerini kontrol edip kelimeyi yazdýrýyoruz
                TextMeshProUGUI tmpText = itemScript.wordTextObject.GetComponent<TextMeshProUGUI>();
                if (tmpText == null) tmpText = itemScript.wordTextObject.GetComponentInChildren<TextMeshProUGUI>();

                if (tmpText != null)
                {
                    tmpText.text = word;
                }
                else
                {
                    Text normalText = itemScript.wordTextObject.GetComponent<Text>();
                    if (normalText != null) normalText.text = word;
                }
            }

         
            Toggle toggle = newWordItem.GetComponent<Toggle>();
            if (toggle != null)
            {
                // ÖNCE durumu kapatýyoruz (böylece fonksiyon kendi kendine tetiklenmiyor)
                toggle.isOn = false;

                // SONRA hocanýn týklamalarýný dinlemeye baþlýyoruz
                toggle.onValueChanged.AddListener((bool isChecked) => {
                    ToggleWordSelection(word, isChecked);
                });
            }
            
        }
    }
    public void ToggleWordSelection(string word, bool isSelected)
    {
        if (isSelected)
        {
            if (!selectedWords.Contains(word)) selectedWords.Add(word);
            Debug.Log($"Seçildi: {word}");
        }
        else
        {
            if (selectedWords.Contains(word)) selectedWords.Remove(word);
            Debug.Log($"Seçim Kaldýrýldý: {word}");
        }
    }

    public void SendAssignment()
    {
        if (selectedWords.Count == 0)
        {
            Debug.LogWarning("Lütfen önce en az bir kelime seçin!");
            return;
        }

        // Seçilenleri aralarýna virgül koyarak yerel hafýzaya kaydet
        string combinedWords = string.Join(",", selectedWords);
        PlayerPrefs.SetString("CurrentAssignment", combinedWords);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Scene_Student"); 
        Debug.Log("Ödev baþarýyla gönderildi: " + combinedWords);
    }
}