using UnityEngine;
using TMPro;
using System.Collections.Generic;

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
    private string gecerliKod = "1234";

    [Header("Ses Kayıt Ayarları")]
    private AudioSource audioSource;
    private AudioClip kaydedilenSes;
    private bool kayitYapiliyor = false;
    private string mikrofonAdi;

    void Start()
    {
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

    public void DanisanGirisYap()
    {
        string girilenKod = danisanKodInput.text.Trim();

        if (girilenKod == gecerliKod)
        {
            if (PlayerPrefs.HasKey("AktifOdevKelimeleri"))
            {
                string odevPaketi = PlayerPrefs.GetString("AktifOdevKelimeleri");
                string[] geciciDizi = odevPaketi.Split(',');
                List<string> temizKelimeler = new List<string>();

                foreach (string k in geciciDizi)
                {
                    if (!string.IsNullOrEmpty(k.Trim()))
                    {
                        temizKelimeler.Add(k.Trim());
                    }
                }

                odevKelimeleri = temizKelimeler.ToArray();
                aktifKelimeIndeksi = 0;

                if (danisanGirisPaneli != null) danisanGirisPaneli.SetActive(false);
                if (danisanOyunPaneli != null) danisanOyunPaneli.SetActive(true);

                KelimeyiEkranaBas();
            }
            else
            {
                HataGoster("Şu an öğretmeninizden gelen bir ödev yok!");
            }
        }
        else
        {
            HataGoster("Hatalı kod girdiniz!");
        }
    }

    void KelimeyiEkranaBas()
    {
        if (dinleButonu != null) dinleButonu.SetActive(false);
        if (geriBildirimText != null) geriBildirimText.text = "";
        kaydedilenSes = null;

        if (odevKelimeleri != null && aktifKelimeIndeksi < odevKelimeleri.Length)
        {
            string suAnkiKelime = odevKelimeleri[aktifKelimeIndeksi];
            mevcutKelimeText.text = suAnkiKelime;
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
            mevcutKelimeText.text = "Tebrikler! Ödevini Tamamladın. 🎉";
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
            mevcutKelimeText.color = Color.red;
            //if (dinleButonu != null) dinleButonu.SetActive(false);
            if (geriBildirimText != null) geriBildirimText.text = "";
            Debug.Log("Kayıt Başladı...");
        }
        else
        {
            Microphone.End(mikrofonAdi);
            kayitYapiliyor = false;
            mevcutKelimeText.color = Color.black;
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