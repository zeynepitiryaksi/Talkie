using UnityEngine;
using TMPro;
using System.Collections.Generic; // Listeleri kullanabilmek için ekledik

public class TerapistHesapYonetimi : MonoBehaviour
{
    [Header("Kayıt Alanları")]
    public TMP_InputField kayitKullaniciAdiInput;
    public TMP_InputField kayitEpostaInput;
    public TMP_InputField kayitSifreInput;
    public TMP_InputField kayitSifreTekrarInput;

    [Header("Giriş Alanları")]
    public TMP_InputField girisEpostaInput;
    public TMP_InputField girisSifreInput;

    [Header("Paneller")]
    public GameObject terapistGirisKayitPaneli;
    public GameObject terapistYonetimPaneli;
    public GameObject kayitOlAltPaneli;   // Kayıt başarılı olunca kapatmak için ekledik
    public GameObject girisYapAltPaneli;  // Kayıt başarılı olunca açmak için ekledik
    public GameObject anaButonlarGrubu;
    [Header("Kelime Listesi Ayarları")]
    public GameObject kelimePrefab;   // Proje panelindeki şablonumuz (Prefab)
    public Transform kelimeContainer; // Hiyerarşideki "Content" nesnemiz

    // Uygulama açıldığında listelenecek örnek kelimelerimiz
    private List<string> kelimeListesi = new List<string>()
    {
        "Elma", "Araba", "Balık", "Güneş", "Kitap", "Kalem", "Kedi", "Köpek"
    };

    public void TerapistKayitOl()
    {
        string kAdi = kayitKullaniciAdiInput.text.Trim();
        string eposta = kayitEpostaInput.text.Trim().ToLower();
        string sifre = kayitSifreInput.text;
        string sifreTekrar = kayitSifreTekrarInput.text;

        // Eksik alan kontrolü
        if (string.IsNullOrEmpty(kAdi) || string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre) || string.IsNullOrEmpty(sifreTekrar))
        {
            Debug.LogError("Lütfen tüm alanları doldurun!");
            return;
        }

        // Şifre uyuşmazlık kontrolü
        if (sifre != sifreTekrar)
        {
            Debug.LogError("Girdiğiniz şifreler birbiriyle uyuşmuyor!");
            return;
        }

        // E-posta format kontrolü
        if (!eposta.Contains("@"))
        {
            Debug.LogError("Lütfen geçerli bir e-posta adresi girin!");
            return;
        }

        // Bilgileri cihaz hafızasına kaydediyoruz
        PlayerPrefs.SetString("TerapistSifre_" + eposta, sifre);
        PlayerPrefs.SetString("TerapistKullanici_" + eposta, kAdi);
        PlayerPrefs.Save();

        Debug.Log("Kayıt Başarılı! Artık Giriş Yap ekranından e-postanızla giriş yapabilirsiniz.");

        // Kutuların içini temizliyoruz
        kayitKullaniciAdiInput.text = "";
        kayitEpostaInput.text = "";
        kayitSifreInput.text = "";
        kayitSifreTekrarInput.text = "";

        // --- OTOMATİK PANEL GEÇİŞİ ---
        // Kayıt bitince kayıt panelini kapatıp, giriş panelini otomatik açıyoruz
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

        // Kayıtlı e-posta kontrolü
        if (PlayerPrefs.HasKey("TerapistSifre_" + eposta))
        {
            string kayitliSifre = PlayerPrefs.GetString("TerapistSifre_" + eposta);

            // Şifre kontrolü
            if (sifre == kayitliSifre)
            {
                string kullaniciAdi = PlayerPrefs.GetString("TerapistKullanici_" + eposta);
                Debug.Log("Giriş Başarılı! Hoş geldiniz Terapist: " + kullaniciAdi);

                // Giriş panellerini komple kapatıp, Kelime Yönetim Panelini açıyoruz
                if (terapistGirisKayitPaneli != null) terapistGirisKayitPaneli.SetActive(false);

                if (terapistYonetimPaneli != null)
                {
                    terapistYonetimPaneli.SetActive(true);
                    KelimeListesiniDoldur(); // Kelimeleri ekrana basan fonksiyonu çağırıyoruz
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

    // Listeyi otomatik dolduran sihirbaz fonksiyonumuz
    void KelimeListesiniDoldur()
    {
        // Container içinde önceden kalan kalıntılar varsa temizle (Çakışma olmasın)
        foreach (Transform child in kelimeContainer)
        {
            Destroy(child.gameObject);
        }

        // Listemizdeki her bir kelime için ekranda otomatik yeni bir yazı objesi oluşturur
        foreach (string kelime in kelimeListesi)
        {
            GameObject yeniKelime = Instantiate(kelimePrefab, kelimeContainer);

            // 🚨 BU SATIRI "GetComponentInChildren" OLARAK GÜNCELLEDİK:
            TextMeshProUGUI txt = yeniKelime.GetComponentInChildren<TextMeshProUGUI>();

            if (txt != null)
            {
                txt.text = kelime;
            }
        } // <-- Eğer kodunun sonunda eksikse bu süslü parantezi de kapatmayı unutma!
    }
}