using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI; // Buton renkleri için ekledik

public class TerapistHesapYonetimi : MonoBehaviour
{
    [Header("Kayıt Alanları")]
    public TMP_InputField kayitKullaniciAdiInput;
    public TMP_InputField kayitEpostaInput;
    public TMP_InputField kayitSifreInput;
    public TMP_InputField kayitSifreTekrarInput;
    private List<string> secilenKelimeler = new List<string>();
    [Header("Giriş Alanları")]
    public TMP_InputField girisEpostaInput;
    public TMP_InputField girisSifreInput;

    [Header("Paneller")]
    public GameObject terapistGirisKayitPaneli;
    public GameObject terapistYonetimPaneli;
    public GameObject kayitOlAltPaneli;
    public GameObject girisYapAltPaneli;
    public GameObject anaButonlarGrubu;

    [Header("Kelime Listesi Ayarları")]
    public GameObject kelimePrefab;
    public Transform kelimeContainer;


    [Header("Arama ve Bilgi Alanları")]
    public TMP_InputField aramaInput;
    public TextMeshProUGUI durumMesajiText;

    [Header("Seçim Renkleri")]
    public Color seciliRenk = Color.green;
    public Color normalRenk = Color.white;

    private List<string> kelimeListesi = new List<string>();
    private GameObject sonSecilenButon = null;

    public void TerapistKayitOl()
    {
        string kAdi = kayitKullaniciAdiInput.text.Trim();
        string eposta = kayitEpostaInput.text.Trim().ToLower();
        string sifre = kayitSifreInput.text;
        string sifreTekrar = kayitSifreTekrarInput.text;

        if (string.IsNullOrEmpty(kAdi) || string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre) || string.IsNullOrEmpty(sifreTekrar))
        {
            Debug.LogError("Lütfen tüm alanları doldurun!");
            return;
        }

        if (sifre != sifreTekrar)
        {
            Debug.LogError("Girdiğiniz şifreler birbiriyle uyuşmuyor!");
            return;
        }

        if (!eposta.Contains("@"))
        {
            Debug.LogError("Lütfen geçerli bir e-posta adresi girin!");
            return;
        }

        PlayerPrefs.SetString("TerapistSifre_" + eposta, sifre);
        PlayerPrefs.SetString("TerapistKullanici_" + eposta, kAdi);
        PlayerPrefs.Save();

        Debug.Log("Kayıt Başarılı! Artık Giriş Yap ekranından e-postanızla giriş yapabilirsiniz.");

        kayitKullaniciAdiInput.text = "";
        kayitEpostaInput.text = "";
        kayitSifreInput.text = "";
        kayitSifreTekrarInput.text = "";

        if (kayitOlAltPaneli != null) kayitOlAltPaneli.SetActive(false);
        if (girisYapAltPaneli != null) girisYapAltPaneli.SetActive(true);
    }

    public void TerapistGirisYap()
    {
        string eposta = girisEpostaInput.text.Trim().ToLower();
        string sifre = girisSifreInput.text;

        if (string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre))
        {
            Debug.LogError("Lütfen e-posta ve şifre alanlarını boş bırakmayın!");
            return;
        }

        if (PlayerPrefs.HasKey("TerapistSifre_" + eposta))
        {
            string kayitliSifre = PlayerPrefs.GetString("TerapistSifre_" + eposta);

            if (sifre == kayitliSifre)
            {
                string kullaniciAdi = PlayerPrefs.GetString("TerapistKullanici_" + eposta);
                Debug.Log("Giriş Başarılı! Hoş geldiniz Terapist: " + kullaniciAdi);

                if (terapistGirisKayitPaneli != null) terapistGirisKayitPaneli.SetActive(false);

                if (terapistYonetimPaneli != null)
                {
                    terapistYonetimPaneli.SetActive(true);


                    DosyadanKelimeleriYukle();
                    KelimeListesiniFiltreleVeDoldur("");
                }
            }
            else
            {
                Debug.LogError("Hatalı şifre girdiniz!");
            }
        }
        else
        {
            Debug.LogError("Bu e-posta adresine ait bir kayıt bulunamadı! Lütfen önce kayıt olun.");
        }
    }

    void DosyadanKelimeleriYukle()
    {
        kelimeListesi = new List<string>();
        TextAsset dosya = Resources.Load<TextAsset>("KelimeDosyalari/kelimeler");

        if (dosya != null)
        {
            string[] satirlar = dosya.text.Split(new string[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string satir in satirlar)
            {
                if (!string.IsNullOrEmpty(satir.Trim()))
                {
                    kelimeListesi.Add(satir.Trim());
                }
            }
        }
    }


    public void OnAramaDegisti()
    {
        if (aramaInput != null)
        {
            KelimeListesiniFiltreleVeDoldur(aramaInput.text.Trim());
        }
    }

    void KelimeListesiniFiltreleVeDoldur(string arananKelime)
    {

        foreach (Transform child in kelimeContainer)
        {
            Destroy(child.gameObject);
        }

        int eşleşenKelimeSayısı = 0;
        arananKelime = arananKelime.ToLower();

        foreach (string kelime in kelimeListesi)
        {

            if (string.IsNullOrEmpty(arananKelime) || kelime.ToLower().Contains(arananKelime))
            {
                eşleşenKelimeSayısı++;
                GameObject yeniButon = Instantiate(kelimePrefab, kelimeContainer);


                TextMeshProUGUI txt = yeniButon.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = kelime;


                Button btn = yeniButon.GetComponent<Button>();
                if (btn != null)
                {

                    btn.onClick.AddListener(() => ButonSecildi(yeniButon));
                }
            }
        }


        if (durumMesajiText != null)
        {
            if (eşleşenKelimeSayısı == 0)
            {
                durumMesajiText.text = "Kelime bulunamadı!";
                durumMesajiText.color = Color.red;
            }
            else if (string.IsNullOrEmpty(arananKelime))
            {
                durumMesajiText.text = "Seçmek istediğiniz kelimenin üstüne tıklayın";
                durumMesajiText.color = Color.gray;
            }
            else
            {
                durumMesajiText.text = $"Bulunan Kelime: {eşleşenKelimeSayısı}";
                durumMesajiText.color = Color.blue;
            }
        }
    }

    void ButonSecildi(GameObject basilanButon)
    {
        Image butonGorseli = basilanButon.GetComponent<Image>();
        TextMeshProUGUI butonYazisi = basilanButon.GetComponentInChildren<TextMeshProUGUI>();

        if (butonGorseli != null && butonYazisi != null)
        {
            string kelime = butonYazisi.text;

            if (butonGorseli.color == seciliRenk)
            {
                butonGorseli.color = normalRenk;
                if (secilenKelimeler.Contains(kelime)) secilenKelimeler.Remove(kelime); 
            }
            else
            {
                butonGorseli.color = seciliRenk;
                if (!secilenKelimeler.Contains(kelime)) secilenKelimeler.Add(kelime); 
            }
        }
    }

    public void OdevGonder()
    {
        if (secilenKelimeler.Count == 0)
        {
            if (durumMesajiText != null) durumMesajiText.text = "Lütfen önce kelime seçin!";
            return;
        }

       
        List<string> temizSecilenler = new List<string>();
        foreach (string k in secilenKelimeler)
        {
            if (!string.IsNullOrEmpty(k.Trim()))
            {
                temizSecilenler.Add(k.Trim());
            }
        }

        string odevPaketi = string.Join(",", temizSecilenler);

        PlayerPrefs.SetString("AktifOdevKelimeleri", odevPaketi);
        PlayerPrefs.Save();

        if (durumMesajiText != null)
        {
            durumMesajiText.text = $"Ödev başarıyla gönderildi! ({temizSecilenler.Count} Kelime)";
            durumMesajiText.color = Color.green;
        }

        Debug.Log("Terapistin Gönderdiği Ödev Paketi: " + odevPaketi);
    }

 
    [Header("Danışan Ses Takip Ayarı")]
    public AudioSource terapistAudioSource; 

    
    public void DanisaninSesiniDinle()
    {
        if (OdevSistemVerisi.SonKaydedilenSes != null)
        {
            AudioSource audio = GetComponent<AudioSource>();
            if (audio != null)
            {
                audio.clip = OdevSistemVerisi.SonKaydedilenSes;
                audio.Play();
            }
        }
    }
} 

