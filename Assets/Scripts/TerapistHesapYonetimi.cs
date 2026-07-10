using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    public TextMeshProUGUI girisDurumMesajiText;

    [Header("Paneller")]
    public GameObject terapistGirisKayitPaneli;
    public GameObject terapistYonetimPaneli; // Kelime listesinin olduğu ana panel
    public GameObject kayitOlAltPaneli;
    public GameObject girisYapAltPaneli;
    public GameObject anaButonlarGrubu;
    public GameObject terapistAnaPaneli;

    [Header("Kelime Listesi Ayarları")]
    public GameObject kelimePrefab; // Klonlanacak kelime kutusu şablonu
    public Transform kelimeContainer; // Kelimelerin dizileceği Scroll View Content alanı

    [Header("Arama ve Bilgi Alanları")]
    public TMP_InputField aramaInput;
    public TMP_InputField secilenlerAramaInput;
    public TextMeshProUGUI durumMesajiText;
    public TextMeshProUGUI secilenlerDurumMesajiText;
    public TMPro.TMP_InputField kelimePaneliOgrenciAdiText;

    [Header("Yeni Seçilenler Paneli Ayarları")]
    public Transform secilenlerContainer;
    public GameObject secilenKelimePrefab;

    [Header("Seçim Renkleri")]
    public Color seciliRenk = Color.green;
    public Color normalRenk = Color.white;

    [Header("Durum Bildirim Alanı")]
    public TextMeshProUGUI kayıtdurumMesajiText;

    [Header("Danışan Ses Takip Ayarı")]
    public AudioSource terapistAudioSource;

    [SerializeField] private RectTransform sepetTransform;

    // Listeler
    private List<string> kelimeListesi = new List<string>(); // Dosyadan dolan tüm kelimeler
    private List<string> secilenKelimeler = new List<string>(); // Terapistin seçtiği kelimeler
    private bool sepetAcikMi = false;

    // 1. KONTROL: Oyun başlar başlamaz kelimeleri dosyadan hafızaya çekiyoruz
    void Start()
    {
        DosyadanKelimeleriYukle();
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
            Debug.Log($"[Talkie] Kelimeler dosyadan yüklendi. Toplam: {kelimeListesi.Count} kelime var.");
        }
        else
        {
            Debug.LogError("[Talkie HATA] Resources/KelimeDosyalari/kelimeler dosyası bulunamadı! Klasör adını ve dosya adını kontrol et.");
        }
    }

    // Terapist Ana Menüden "Öğrenci Ekle/Kelime Listesi" butonuna bastığında çalışacak
    public void Buton_OgrenciEkleBasildi()
    {
        if (terapistAnaPaneli != null) terapistAnaPaneli.SetActive(false);

        if (terapistYonetimPaneli != null)
        {
            terapistYonetimPaneli.SetActive(true);

            // 2. KONTROL: Panel açıldığı an kelimeleri ekrana tıkır tıkır diziyoruz
            KelimeListesiniFiltreleVeDoldur("");
        }
    }

    public void KelimeListesiniFiltreleVeDoldur(string arananKelime)
    {
        if (kelimeContainer == null) return;

        // Eski butonları temizle
        foreach (Transform child in kelimeContainer)
        {
            Destroy(child.gameObject);
        }

        int eslesenKelimeSayisi = 0;
        arananKelime = arananKelime.ToLower();

        foreach (string kelime in kelimeListesi)
        {
            if (string.IsNullOrEmpty(arananKelime) || kelime.ToLower().Contains(arananKelime))
            {
                eslesenKelimeSayisi++;
                GameObject yeniButon = Instantiate(kelimePrefab, kelimeContainer);
                yeniButon.transform.localScale = Vector3.one;

                TextMeshProUGUI txt = yeniButon.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = kelime;

                Image butonGorseli = yeniButon.GetComponent<Image>();
                if (butonGorseli != null)
                {
                    if (secilenKelimeler.Contains(kelime)) butonGorseli.color = seciliRenk;
                    else butonGorseli.color = normalRenk;
                }

                Button btn = yeniButon.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ButonSecildi(yeniButon));
                }
            }
        }

        SecilenlerListesiniGuncelle();

        if (durumMesajiText != null)
        {
            if (eslesenKelimeSayisi == 0)
            {
                durumMesajiText.text = "Kelime bulunamadı!";
                durumMesajiText.color = Color.red;
            }
            else if (string.IsNullOrEmpty(arananKelime))
            {
                durumMesajiText.text = "";
            }
            else
            {
                durumMesajiText.text = $"Bulunan Kelime: {eslesenKelimeSayisi}";
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

            if (secilenKelimeler.Contains(kelime))
            {
                secilenKelimeler.Remove(kelime);
                butonGorseli.color = normalRenk;
            }
            else
            {
                secilenKelimeler.Add(kelime);
                butonGorseli.color = seciliRenk;
            }

            SecilenlerListesiniGuncelle();
        }
    }

    private void SecilenlerListesiniGuncelle(string filtre = "")
    {
        if (secilenlerContainer == null || secilenKelimePrefab == null) return;

        foreach (Transform child in secilenlerContainer)
        {
            Destroy(child.gameObject);
        }

        filtre = filtre.ToLower();
        int eslesenKelimeSayisi = 0;

        foreach (string kelime in secilenKelimeler)
        {
            if (!string.IsNullOrEmpty(filtre) && !kelime.ToLower().Contains(filtre)) continue;

            eslesenKelimeSayisi++;

            GameObject yeniKutu = Instantiate(secilenKelimePrefab, secilenlerContainer);
            yeniKutu.transform.localScale = Vector3.one;

            TextMeshProUGUI txt = yeniKutu.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = kelime;

            Transform carpiTransform = yeniKutu.transform.Find("CarpiButonu");
            if (carpiTransform != null)
            {
                Button carpiBtn = carpiTransform.GetComponent<Button>();
                if (carpiBtn == null) carpiBtn = carpiTransform.gameObject.AddComponent<Button>();

                carpiBtn.onClick.RemoveAllListeners();
                carpiBtn.onClick.AddListener(() => YeniPaneldenSecimKaldir(kelime));
            }

            Button anaButon = yeniKutu.GetComponent<Button>();
            if (anaButon != null) anaButon.interactable = false;
        }

        if (secilenlerDurumMesajiText != null)
        {
            if (secilenKelimeler.Count == 0)
            {
                secilenlerDurumMesajiText.text = "Henüz kelime seçilmedi.";
                secilenlerDurumMesajiText.color = Color.gray;
            }
            else
            {
                secilenlerDurumMesajiText.text = $"Toplam Seçilen: {secilenKelimeler.Count}";
                secilenlerDurumMesajiText.color = Color.black;
            }
        }
    }

    void YeniPaneldenSecimKaldir(string kelime)
    {
        if (secilenKelimeler.Contains(kelime)) secilenKelimeler.Remove(kelime);

        string anlikFiltre = (secilenlerAramaInput != null) ? secilenlerAramaInput.text.Trim() : "";
        SecilenlerListesiniGuncelle(anlikFiltre);
        KelimeListesiniFiltreleVeDoldur((aramaInput != null) ? aramaInput.text.Trim() : "");
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
            if (!string.IsNullOrEmpty(k.Trim())) temizSecilenler.Add(k.Trim());
        }

        string odevPaketi = string.Join(",", temizSecilenler);

        // TeacherManager'ın yaptığı gibi hem ödevi kaydedip hem de öğrenci sahnesine geçiş yapıyoruz
        PlayerPrefs.SetString("AktifOdevKelimeleri", odevPaketi);
        PlayerPrefs.SetString("CurrentAssignment", odevPaketi); // İki ihtimale karşı ikisini de kaydedelim
        PlayerPrefs.Save();

        if (durumMesajiText != null)
        {
            durumMesajiText.text = "Ödev başarıyla gönderildi! Yönlendiriliyorsunuz...";
            durumMesajiText.color = Color.blue;
        }

        Debug.Log("Ödev Gönderildi: " + odevPaketi);

        // Öğrenci sahnesine geçiş
        SceneManager.LoadScene("Scene_Student");
    }

    public void OnAramaDegisti()
    {
        if (aramaInput != null) KelimeListesiniFiltreleVeDoldur(aramaInput.text.Trim());
    }

    public void OnSecilenlerAramaDegisti()
    {
        if (secilenlerAramaInput != null) SecilenlerListesiniGuncelle(secilenlerAramaInput.text.Trim());
    }

    // Sadece "Eklenen Kelimeler" butonu bunu tetikleyecek

    public void SepetAc()
    {
        if (sepetTransform == null) return;
        if (!sepetTransform.gameObject.activeSelf) sepetTransform.gameObject.SetActive(true);

        Animator panelAnim = sepetTransform.GetComponent<Animator>();
        if (panelAnim != null)
        {
            // Play yerine SetBool kullanıyoruz
            panelAnim.SetBool("IsOpen", true);
        }
    }

    // Çarpı (X) butonu buna bağlanacak
    public void SepetKapa()
    {
        if (sepetTransform == null) return;

        Animator panelAnim = sepetTransform.GetComponent<Animator>();
        if (panelAnim != null)
        {
            // Kapatırken bool değerini false yapıyoruz
            panelAnim.SetBool("IsOpen", false);
        }
    }
    // --- KAYIT VE GİRİŞ FONKSİYONLARI (Aynen Korundu) ---
    public void TerapistKayitOl()
    {
        string kAdi = kayitKullaniciAdiInput.text.Trim();
        string eposta = kayitEpostaInput.text.Trim().ToLower();
        string sifre = kayitSifreInput.text;
        string sifreTekrar = kayitSifreTekrarInput.text;

        if (kayıtdurumMesajiText != null) kayıtdurumMesajiText.text = "";

        if (string.IsNullOrEmpty(kAdi) || string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre) || string.IsNullOrEmpty(sifreTekrar))
        {
            HataMesajiGoster("Lütfen tüm alanları doldurun!");
            return;
        }

        if (!eposta.Contains("@") || !eposta.Contains("."))
        {
            HataMesajiGoster("Lütfen geçerli bir e-posta adresi girin!");
            return;
        }

        if (sifre != sifreTekrar)
        {
            HataMesajiGoster("Şifreler uyuşmuyor!");
            return;
        }

        if (PlayerPrefs.HasKey("TerapistKullanici_" + eposta))
        {
            HataMesajiGoster("Bu e-posta zaten kayıtlı!");
            return;
        }

        PlayerPrefs.SetString("TerapistSifre_" + eposta, sifre);
        PlayerPrefs.SetString("TerapistKullanici_" + eposta, kAdi);
        PlayerPrefs.Save();

        if (kayıtdurumMesajiText != null) kayıtdurumMesajiText.text = "Kayıt Başarılı!";

        if (kayitOlAltPaneli != null) kayitOlAltPaneli.SetActive(false);
        if (girisYapAltPaneli != null) girisYapAltPaneli.SetActive(true);
    }

    public void TerapistGirisYap()
    {
        string eposta = girisEpostaInput.text.Trim().ToLower();
        string sifre = girisSifreInput.text;

        if (girisDurumMesajiText != null) girisDurumMesajiText.text = "";

        if (string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre))
        {
            GirisHataMesajiGoster("Lütfen tüm alanları doldurun!");
            return;
        }

        if (!PlayerPrefs.HasKey("TerapistKullanici_" + eposta))
        {
            GirisHataMesajiGoster("Hesap bulunamadı!");
            return;
        }

        if (sifre == PlayerPrefs.GetString("TerapistSifre_" + eposta))
        {
            if (girisDurumMesajiText != null) girisDurumMesajiText.text = "Giriş Başarılı!";
            Invoke("TerapistAnaPanelineGec", 1.0f);
        }
        else
        {
            GirisHataMesajiGoster("Şifre hatalı!");
        }
    }

    private void TerapistAnaPanelineGec()
    {
        if (girisYapAltPaneli != null) girisYapAltPaneli.SetActive(false);
        if (terapistAnaPaneli != null) terapistAnaPaneli.SetActive(true);
    }

    private void HataMesajiGoster(string mesaj)
    {
        if (kayıtdurumMesajiText != null) kayıtdurumMesajiText.text = mesaj;
    }

    private void GirisHataMesajiGoster(string mesaj)
    {
        if (girisDurumMesajiText != null) girisDurumMesajiText.text = mesaj;
    }

    public void ZatenHesabimVarButonu()
    {
        if (kayitOlAltPaneli != null) kayitOlAltPaneli.SetActive(false);
        if (girisYapAltPaneli != null) girisYapAltPaneli.SetActive(true);
    }
    // --- BU FONKSİYONU KODUNA EKLE ---
    public void HesabinYokMuButonu()
    {
        if (girisYapAltPaneli != null) girisYapAltPaneli.SetActive(false); // Giriş panelini kapat
        if (kayitOlAltPaneli != null) kayitOlAltPaneli.SetActive(true);   // Kayıt panelini aç
    }
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