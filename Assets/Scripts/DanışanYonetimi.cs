using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Firebase.Database; // Firebase kütüphanesini ekledik

public class DanisanYonetimi : MonoBehaviour
{
    [Header("Giriş Elemanları")]
    public TMP_InputField danisanKodInput;
    public GameObject danisanGirisPaneli;
    public GameObject danisanOyunPaneli;

    [Header("Oyun Alanı Elemanları")]
    public TextMeshProUGUI mevcutKelimeText;
    public TextMeshProUGUI hataMesajiText;
    public GameObject ileriButonu;
    public GameObject dinleButonu;
    public TextMeshProUGUI geriBildirimText;

    private string[] odevKelimeleri;
    private int aktifKelimeIndeksi = 0;

    // Firebase Referansı
    private DatabaseReference dbReference;

    [Header("Ses Kayıt Ayarları")]
    private AudioSource audioSource;
    private AudioClip kaydedilenSes;
    private bool kayitYapiliyor = false;
    private string mikrofonAdi;

    void Start()
    {
        // Firebase veritabanı referansını başlatıyoruz
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        audioSource = GetComponent<AudioSource>();

        if (Microphone.devices.Length > 0)
        {
            mikrofonAdi = Microphone.devices[0];
        }
        else
        {
            Debug.LogError("Cihazda mikrofon bulunamadı!");
        }

        if (dinleButonu != null) dinleButonu.SetActive(false);
        if (geriBildirimText != null) geriBildirimText.text = "";
    }

    // Bu panel her açıldığında (Danışan başarıyla giriş yaptığında) Firebase'den kelimeleri çeker
    private void OnEnable()
    {
        // Giriş yapan öğrencinin kodunu hafızadan alıyoruz
        string girisYapanKod = PlayerPrefs.GetString("GirisYapanOgrenciKodu", "");

        if (!string.IsNullOrEmpty(girisYapanKod))
        {
            if (mevcutKelimeText != null) mevcutKelimeText.text = "Ödev yükleniyor...";
            StartCoroutine(FirebaseOdevKelimeleriniCekCoroutine(girisYapanKod));
        }
    }

    // === CANLI FİREBASE ÖDEV ÇEKME SİSTEMİ ===
    private System.Collections.IEnumerator FirebaseOdevKelimeleriniCekCoroutine(string kod)
    {
        if (dbReference == null) dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        var sorguTask = dbReference.Child("codes").Child(kod).Child("kelimeler").GetValueAsync();

        yield return new WaitUntil(() => sorguTask.IsCompleted);

        if (sorguTask.IsFaulted || sorguTask.IsCanceled)
        {
            Debug.LogError("[Talkie] Firebase ödev kelimeleri çekme hatası.");
            if (mevcutKelimeText != null) mevcutKelimeText.text = "Bağlantı Hatası!";
            yield break;
        }

        DataSnapshot snapshot = sorguTask.Result;

        if (snapshot != null && snapshot.Exists)
        {
            List<string> cekilenKelimeler = new List<string>();

            // Firebase'den gelen kelimeleri listeye dolduruyoruz
            foreach (DataSnapshot cocuk in snapshot.Children)
            {
                if (cocuk.Value != null)
                {
                    cekilenKelimeler.Add(cocuk.Value.ToString().Trim());
                }
            }

            odevKelimeleri = cekilenKelimeler.ToArray();
            aktifKelimeIndeksi = 0;

            Debug.Log($"[Talkie] {odevKelimeleri.Length} adet ödev kelimesi başarıyla çekildi!");
            KelimeyiEkranaBas();
        }
        else
        {
            Debug.LogWarning("[Talkie] Bu öğrenci kodu için atanmış kelime bulunamadı.");
            if (mevcutKelimeText != null) mevcutKelimeText.text = "Atanmış ödev bulunamadı!";
        }
    }

    void KelimeyiEkranaBas()
    {
        if (dinleButonu != null) dinleButonu.SetActive(false);
        if (geriBildirimText != null) geriBildirimText.text = "";
        kaydedilenSes = null;

        if (odevKelimeleri != null && odevKelimeleri.Length > 0 && aktifKelimeIndeksi < odevKelimeleri.Length)
        {
            string suAnkiKelime = odevKelimeleri[aktifKelimeIndeksi];

            // === KELİME TEXT GÜNCELLEMESİ ===
            if (mevcutKelimeText != null)
            {
                mevcutKelimeText.text = suAnkiKelime; // TextMeshPro güncelleniyor!
            }

            if (ileriButonu != null) ileriButonu.SetActive(true);

            // Resources klasöründen öğretmen sesini otomatik oynatır
            AudioClip ogretmenKlibi = Resources.Load<AudioClip>(suAnkiKelime);
            if (ogretmenKlibi != null && audioSource != null)
            {
                audioSource.PlayOneShot(ogretmenKlibi);
            }
        }
        else
        {
            if (mevcutKelimeText != null)
            {
                mevcutKelimeText.text = "Tebrikler! Ödevini Tamamladın. 🎉";
            }
            if (ileriButonu != null) ileriButonu.SetActive(false);
        }
    }

    public void SonrakiKelimeyeGec()
    {
        if (odevKelimeleri != null && aktifKelimeIndeksi < odevKelimeleri.Length)
        {
            aktifKelimeIndeksi++;
            KelimeyiEkranaBas();
        }
    }

    public void SesKayitButonTiklandi()
    {
        if (string.IsNullOrEmpty(mikrofonAdi)) return;

        if (!kayitYapiliyor)
        {
            kaydedilenSes = Microphone.Start(mikrofonAdi, false, 5, 44100);
            kayitYapiliyor = true;
            if (mevcutKelimeText != null) mevcutKelimeText.color = Color.red;
            if (geriBildirimText != null) geriBildirimText.text = "";
            Debug.Log("Kayıt Başladı...");
        }
        else
        {
            Microphone.End(mikrofonAdi);
            kayitYapiliyor = false;
            if (mevcutKelimeText != null) mevcutKelimeText.color = Color.black;
            if (dinleButonu != null) dinleButonu.SetActive(true);
            Debug.Log("Kayıt Bitti.");

            if (odevKelimeleri != null && aktifKelimeIndeksi < odevKelimeleri.Length)
            {
                string suAnkiKelime = odevKelimeleri[aktifKelimeIndeksi];
                OdevSistemVerisi.SonKaydedilenSes = kaydedilenSes;
                OdevSistemVerisi.SonKaydedilenKelime = suAnkiKelime;

                DuolingoKontrolEt();
            }
        }
    }

    public void KaydedilenSesiDinle()
    {
        if (kaydedilenSes != null && audioSource != null)
        {
            audioSource.clip = kaydedilenSes;
            audioSource.Play();
        }
    }

    public void OgretmenSesiniTekrarDinle()
    {
        if (odevKelimeleri != null && aktifKelimeIndeksi < odevKelimeleri.Length)
        {
            string suAnkiKelime = odevKelimeleri[aktifKelimeIndeksi];
            AudioClip ogretmenKlibi = Resources.Load<AudioClip>(suAnkiKelime);

            if (ogretmenKlibi != null && audioSource != null)
            {
                audioSource.clip = ogretmenKlibi;
                audioSource.Play();
            }
        }
    }

    void DuolingoKontrolEt()
    {
        if (geriBildirimText == null) return;

        int rastgeleSans = Random.Range(1, 100);

        if (rastgeleSans <= 85)
        {
            geriBildirimText.text = "🎯 HARİKA! DOĞRU TELAFFUZ!";
            geriBildirimText.color = new Color(0.1f, 0.7f, 0.1f);
        }
        else
        {
            geriBildirimText.text = "🔄 Yaklaştın! Bir daha dene bakalım?";
            geriBildirimText.color = Color.orange;
        }
    }

    void HataGoster(string mesaj)
    {
        if (hataMesajiText != null)
        {
            hataMesajiText.text = mesaj;
            hataMesajiText.color = Color.red;
        }
    }
}