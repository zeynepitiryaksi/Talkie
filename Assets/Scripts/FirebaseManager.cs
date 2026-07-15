using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using TMPro;

[Serializable]
public class OgrenciData
{
    public string ogrenciAdi;
    public string uretilenKod;
    public List<string> kelimeler;

    public OgrenciData(string ad, string kod)
    {
        this.ogrenciAdi = ad;
        this.uretilenKod = kod;
        this.kelimeler = new List<string>();
    }
}

public class FirebaseManager : MonoBehaviour
{
    private DatabaseReference dbReference;

    [Header("Terapist Panel UI Elemanlarý")]
    [SerializeField] private TMP_InputField ogrenciAdiInput; // Ýsim girdiðin kutu
    [SerializeField] private TMP_Text bilgiMesajiText; // Ortadaki bulutta kodu göreceðin büyük yazý

    private void Awake()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase Baðlantýsý Baþarýlý.");
            }
            else
            {
                Debug.LogError("Firebase hatasý: " + dependencyStatus);
            }
        });
    }

    // --- "ÖÐRENCÝ EKLE" VEYA "ÖDEV GÖNDER" BUTONUNA BAÐLANACAK FONKSÝYON ---
    public void OgrenciEkleVeKodUretButonu()
    {
        string inputName = ogrenciAdiInput.text.Trim();

        if (string.IsNullOrEmpty(inputName))
        {
            bilgiMesajiText.text = "Lütfen önce bir öðrenci ismi girin!";
            bilgiMesajiText.color = Color.red;
            return;
        }

        // 6 haneli kodu üretiyoruz
        string rastgeleKod = UnityEngine.Random.Range(100000, 999999).ToString();

        // Veri modelini hazýrlýyoruz
        OgrenciData yeniOgrenci = new OgrenciData(inputName, rastgeleKod);
        string json = JsonUtility.ToJson(yeniOgrenci);

        // Hem kod sorgulamasý için "codes/kod" altýna yazýyoruz
        dbReference.Child("codes").Child(rastgeleKod).SetRawJsonValueAsync(json);

        // Hem de terapistin ileride listeyi görebilmesi için "students/ogrenciAdi" altýna yedekliyoruz
        dbReference.Child("students").Child(inputName).SetRawJsonValueAsync(json).ContinueWith(task => {
            if (task.IsCompleted)
            {
                Debug.Log($"Firebase'e Kaydedildi. Kod: {rastgeleKod}");
            }
        });

        // Terapistin kodu anýnda görebilmesi için ekrandaki metni güncelliyoruz
        bilgiMesajiText.text = $"ÖÐRENCÝ EKLENDÝ!\n\nÖðrenci: {inputName}\nGiriþ Kodu: {rastgeleKod}";
        bilgiMesajiText.color = Color.green;

        // Ýsim girme kutusunu temizle ki yeni öðrenciye hazýr olsun
        ogrenciAdiInput.text = "";
    }

    // --- YENÝ EKLENEN SÝLME FONKSÝYONU ---
    // Çöp kutusuna basýldýðýnda TerapistHesapYonetimi bu fonksiyonu çaðýracak
    public void OgrenciSil(string ogrenciKodu, string ogrenciAdi, Action<bool> basariDurumu)
    {
        if (dbReference == null) dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        // 1. "codes/6_haneli_kod" altýndaki veriyi siliyoruz
        dbReference.Child("codes").Child(ogrenciKodu).RemoveValueAsync().ContinueWith(task => {

            // 2. Eðer "students/ogrenciAdi" altýnda da yedek tutuyorsan orayý da temizleyelim
            if (!string.IsNullOrEmpty(ogrenciAdi))
            {
                dbReference.Child("students").Child(ogrenciAdi).RemoveValueAsync();
            }

            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log($"[Firebase] {ogrenciKodu} kodlu öðrenci baþarýyla silindi.");
                basariDurumu?.Invoke(true); // TerapistHesapYonetimi'ne "silme baþarýlý" haberi uçurur
            }
            else
            {
                Debug.LogError($"[Firebase HATA] Öðrenci silinemedi.");
                basariDurumu?.Invoke(false); // Baþarýsýz bildirimi
            }
        });
    }
    // --- YENÝ EKLENEN ÖÐRENCÝ GÝRÝÞ SORGULAMA FONKSÝYONU ---
    // Öðrenci 6 haneli kodu girdiðinde bu fonksiyon ile Firebase'den sorgulama yapýyoruz.
    public void OgrenciGirisYap(string girilenKod, Action<bool, string> girisSonucu)
    {
        if (dbReference == null) dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        if (string.IsNullOrEmpty(girilenKod))
        {
            girisSonucu?.Invoke(false, "Lütfen kod alanýný boþ býrakmayýn!");
            return;
        }

        // Firebase'de "codes/girilenKod" yoluna gidip veriyi bir kere okuyoruz
        dbReference.Child("codes").Child(girilenKod).GetValueAsync().ContinueWith(task => {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[Firebase] Kod sorgulama hatasý.");
                girisSonucu?.Invoke(false, "Baðlantý hatasý oluþtu!");
                return;
            }

            DataSnapshot snapshot = task.Result;

            if (snapshot.Exists)
            {
                // Kod veritabanýnda bulundu!
                string ogrenciAdi = snapshot.Child("ogrenciAdi").Value.ToString();

                // Öðrencinin kodunu ve adýný yerel hafýzaya kaydediyoruz (ileride ödevleri çekmek vb. için gerekir)
                PlayerPrefs.SetString("GirisYapanOgrenciKodu", girilenKod);
                PlayerPrefs.SetString("GirisYapanOgrenciAdi", ogrenciAdi);
                PlayerPrefs.Save();

                Debug.Log($"[Firebase] Giriþ Baþarýlý! Hoþ geldin {ogrenciAdi}");

                // Giriþ baþarýlý (true) ve öðrenci adý bilgisini döndür
                girisSonucu?.Invoke(true, ogrenciAdi);
            }
            else
            {
                // Kod veritabanýnda yoksa
                Debug.LogWarning("[Firebase] Geçersiz giriþ kodu!");
                girisSonucu?.Invoke(false, "Geçersiz veya hatalý kod girdiniz!");
            }
        });
    }




}