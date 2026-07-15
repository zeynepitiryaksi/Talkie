using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Firebase;
using Firebase.Database;

public class TerapistHesapYonetimi : MonoBehaviour
{
    private Coroutine aktifListelemeCoroutine = null;
    public PanelYoneticisi panelYoneticisi;

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
    public GameObject terapistYonetimPaneli; // Kelimelerin listelendiği sepetli ana panel
    public GameObject kayitOlAltPaneli;
    public GameObject girisYapAltPaneli;
    public GameObject anaButonlarGrubu;
    public GameObject terapistAnaPaneli; // Öğrenci listesinin olduğu panel

    [Header("Öğrenci (Danışan) Giriş Ayarları")]
    [SerializeField] private TMP_InputField danisanKodGirisInput; // Öğrencinin giriş kodunu yazacağı kutu
    [SerializeField] private TextMeshProUGUI danisanGirisDurumText; // "Hatalı Kod" vb. yazacak durum metni
    [SerializeField] private GameObject danisanGirisPaneli; // Danışan kod girme paneli
    [SerializeField] private GameObject danisanOyunPaneli; // Danışan giriş yapınca açılacak ana oyun paneli

    [Header("Kelime Listesi Ayarları")]
    public GameObject kelimePrefab;
    public Transform kelimeContainer;

    [Header("Öğrenci Listesi Ayarları")]
    public GameObject ogrenciButonPrefab; // Scroll View içine eklenecek buton şablonu
    public Transform ogrenciListeContainer; // Öğrenci Scroll View -> Content alanı

    [Header("Arama ve Bilgi Alanları")]
    public TMP_InputField aramaInput;
    public TMP_InputField secilenlerAramaInput;
    public TextMeshProUGUI durumMesajiText;
    public TextMeshProUGUI secilenlerDurumMesajiText;

    // NOT: Bu alan artık kelime listesi panelinde sadece seçilen aktif öğrencinin adını GÖSTERMEK için kullanılacak!
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
    public TextMeshProUGUI kelimePaneliDurumMesajiText;
    [SerializeField] private RectTransform sepetTransform;
    private bool danisanGirisBasariliMi = false;
    // Listeler
    private List<string> kelimeListesi = new List<string>();
    private List<string> secilenKelimeler = new List<string>();

    // Firebase Referansı
    private DatabaseReference dbReference;
    private bool firebaseHazirMi = false;

    // Thread-Safe Güvenli Geçiş Sinyalleri (Update döngüsü tarafından dinlenir)
    private bool anaThreaddeListele = false;
    private bool anaThreaddeGirisYapildi = false;
    private string girisDurumMesaji = "";
    [Header("Yeni Eklenen Arayüz Alanları")]
    public TMPro.TMP_InputField anaMenuOgrenciEkleInput; // 1. Paneldeki (Ana Menü) yeni öğrenci adı yazma kutusu
    void Start()
    {
        // Oyun başlar başlamaz container'ı aktif ediyoruz
        if (ogrenciListeContainer != null)
        {
            ogrenciListeContainer.gameObject.SetActive(true);
        }

        DosyadanKelimeleriYukle();
        FirebaseBaslat();
    }

    void Update()
    {
        // 1. Öğrenci Listesini Yenileme Sinyali
        if (anaThreaddeListele)
        {
            anaThreaddeListele = false;

            if (aktifListelemeCoroutine != null)
            {
                StopCoroutine(aktifListelemeCoroutine);
            }

            aktifListelemeCoroutine = StartCoroutine(FirebaseOgrencileriniListeleCoroutine());
        }

        // 2. Danışan Giriş Yapma Sinyali (YENİLENDİ)
        if (anaThreaddeGirisYapildi)
        {
            anaThreaddeGirisYapildi = false;

            if (danisanGirisDurumText != null)
            {
                danisanGirisDurumText.text = girisDurumMesaji;
            }

            // Artık yazı aramıyoruz, doğrudan boolean onayımıza bakıyoruz!
            if (danisanGirisBasariliMi)
            {
                danisanGirisBasariliMi = false; // Tekrar tetiklenmesin diye sıfırla
                DanisanGirisPanelleriniGecisYap();
            }
        }
    }

    void FirebaseBaslat()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                firebaseHazirMi = true;
                Debug.Log("[Talkie] Firebase Başarıyla Bağlandı.");

                // Update fonksiyonuna listeleme sinyali gönder
                anaThreaddeListele = true;
            }
            else
            {
                Debug.LogError("[Talkie HATA] Firebase başlatılamadı: " + dependencyStatus);
            }
        });
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
    }

    // --- ÖĞRENCİ (DANIŞAN) GİRİŞ YAPMA FONKSİYONU ---
    // 1. BUTONUN ÇALIŞTIRACAĞI METOT
    public void Buton_DanisanGirisYapBasildi()
    {
        if (dbReference == null) dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        string girilenKod = danisanKodGirisInput.text.Trim();

        if (string.IsNullOrEmpty(girilenKod))
        {
            if (danisanGirisDurumText != null)
            {
                danisanGirisDurumText.text = "Lütfen 6 haneli giriş kodunuzu yazın!";
                danisanGirisDurumText.color = Color.red;
            }
            return;
        }

        if (danisanGirisDurumText != null)
        {
            danisanGirisDurumText.text = "Kod doğrulanıyor...";
            danisanGirisDurumText.color = Color.black;
        }

        danisanGirisBasariliMi = false;

        // Arka planda donup kalmayı engellemek için Coroutine başlatıyoruz!
        StartCoroutine(DanisanGirisKontrolCoroutine(girilenKod));
    }

    // 2. ARKA PLANDA ÇALIŞACAK VE ASLA ASILI KALMAYACAK YENİ KONTROL METODU
    private System.Collections.IEnumerator DanisanGirisKontrolCoroutine(string kod)
    {
        var sorguTask = dbReference.Child("codes").Child(kod).GetValueAsync();

        // Sorgu bitene kadar Unity'yi dondurmadan bekliyoruz
        yield return new WaitUntil(() => sorguTask.IsCompleted);

        if (sorguTask.IsFaulted || sorguTask.IsCanceled)
        {
            Debug.LogError("[Talkie HATA] Firebase bağlantı hatası oluştu.");
            girisDurumMesaji = "Bağlantı hatası! İnterneti kontrol edin.";
            danisanGirisBasariliMi = false;
        }
        else
        {
            DataSnapshot snapshot = sorguTask.Result;

            if (snapshot != null && snapshot.Exists)
            {
                string ogrenciAdi = "Öğrenci";
                if (snapshot.Child("ogrenciAdi").Value != null)
                {
                    ogrenciAdi = snapshot.Child("ogrenciAdi").Value.ToString();
                }

                PlayerPrefs.SetString("GirisYapanOgrenciKodu", kod);
                PlayerPrefs.SetString("GirisYapanOgrenciAdi", ogrenciAdi);
                PlayerPrefs.Save();

                Debug.Log($"[Talkie] Giriş Başarılı! Hoş geldin {ogrenciAdi}");

                girisDurumMesaji = $"Giriş Başarılı! Hoş geldin {ogrenciAdi}";
                danisanGirisBasariliMi = true; // Onay verildi
            }
            else
            {
                Debug.LogWarning("[Talkie] Geçersiz kod girildi.");
                girisDurumMesaji = "Girdiğiniz kod hatalı veya geçersiz!";
                danisanGirisBasariliMi = false;
            }
        }

        // Update fonksiyonuna "işlem bitti, paneli değiştirebilirsin" sinyali gönderiyoruz
        anaThreaddeGirisYapildi = true;
    }
    private void DanisanGirisPanelleriniGecisYap()
    {
        Debug.Log("[Talkie HATA TAKİBİ] 1. DanisanGirisPanelleriniGecisYap tetiklendi!");

        if (danisanGirisPaneli == null)
        {
            Debug.LogError("[Talkie HATA TAKİBİ] 'danisanGirisPaneli' Inspector'da BOŞ (Null)! Lütfen sürükleyip bırakın.");
        }
        else
        {
            danisanGirisPaneli.SetActive(false); // Giriş ekranını kapat
            Debug.Log("[Talkie HATA TAKİBİ] 2. Giriş paneli kapatıldı.");
        }

        if (danisanOyunPaneli == null)
        {
            Debug.LogError("[Talkie HATA TAKİBİ] 'danisanOyunPaneli' Inspector'da BOŞ (Null)! Lütfen sürükleyip bırakın.");
        }
        else
        {
            danisanOyunPaneli.SetActive(true); // Oyun ekranını aç
            Debug.Log("[Talkie HATA TAKİBİ] 3. Danışan Oyun Paneli AKTİF EDİLDİ!");
        }

        if (panelYoneticisi == null)
        {
            Debug.LogWarning("[Talkie HATA TAKİBİ] 'panelYoneticisi' referansı boş! Panel yöneticisi üzerinden geçiş yapılamadı.");
        }
        else if (danisanOyunPaneli != null)
        {
            panelYoneticisi.BasariliGirisGecisiYap(danisanOyunPaneli);
            Debug.Log("[Talkie HATA TAKİBİ] 4. PanelYoneticisi sistemine başarılı giriş bildirildi.");
        }

        if (danisanKodGirisInput != null) danisanKodGirisInput.text = "";
    }
    public void Buton_OgrenciEkleBasildi()
    {
        // 1. Yeni öğrenci adını artık ana menüdeki özel kutudan (anaMenuOgrenciEkleInput) alıyoruz!
        string ogrenciAdi = "";
        if (anaMenuOgrenciEkleInput != null)
        {
            ogrenciAdi = anaMenuOgrenciEkleInput.text.Trim();
        }

        // Ad boşsa durum mesajına hata yazdırıyoruz
        if (string.IsNullOrEmpty(ogrenciAdi))
        {
            if (durumMesajiText != null)
            {
                durumMesajiText.text = "Lütfen önce bir öğrenci ismi girin!";
                durumMesajiText.color = Color.red;
            }
            return;
        }

        // 2. 6 haneli benzersiz kodu ve zamanı hemen üretiyoruz
        string uretilenKod = UnityEngine.Random.Range(100000, 999999).ToString();
        long suAnkiZaman = System.DateTime.Now.Ticks;

        Debug.Log($"[Talkie] Öğrenci ekleme başlatıldı: {ogrenciAdi} ({uretilenKod})");

        // === 1. ADIM: DURUM MESAJINI ANINDA GÜNCELLE ===
        if (durumMesajiText != null)
        {
            durumMesajiText.text = $"ÖĞRENCİ\nEKLENDİ!\nİsim: {ogrenciAdi}\nKod:\n{uretilenKod}";
            durumMesajiText.color = Color.black;
        }

        // === 2. ADIM: EKEREK ARAYÜZDE ANINDA GÖSTER (Sıfır Gecikme) ===
        if (ogrenciListeContainer != null && ogrenciButonPrefab != null)
        {
            ogrenciListeContainer.gameObject.SetActive(true);

            GameObject yeniOgrenciKutusu = Instantiate(ogrenciButonPrefab, ogrenciListeContainer, false);
            yeniOgrenciKutusu.transform.localScale = Vector3.one;

            // En yeni ekleneni her zaman listenin en tepesine fırlatıyoruz
            yeniOgrenciKutusu.transform.SetAsFirstSibling();

            // İsim ve kod TMP metnini yazıyoruz
            Transform ogrenciAdiTransform = yeniOgrenciKutusu.transform.Find("ogrenciadı");
            if (ogrenciAdiTransform != null)
            {
                TextMeshProUGUI txt = ogrenciAdiTransform.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = $"{ogrenciAdi} - ({uretilenKod})";
            }

            // Buton dinleyicilerini (onClick) anında bağlıyoruz
            Transform odevGonderTransform = yeniOgrenciKutusu.transform.Find("ödevigönder");
            if (odevGonderTransform != null)
            {
                Button odevBtn = odevGonderTransform.GetComponent<Button>();
                if (odevBtn != null)
                {
                    odevBtn.onClick.RemoveAllListeners();
                    odevBtn.onClick.AddListener(() => Buton_OgrenciSatirindakiOdevGonderBasildi(uretilenKod, ogrenciAdi));
                }
            }

            Transform durumTransform = yeniOgrenciKutusu.transform.Find("Durum");
            if (durumTransform != null)
            {
                Button durumBtn = durumTransform.GetComponent<Button>();
                if (durumBtn != null)
                {
                    durumBtn.onClick.RemoveAllListeners();
                    durumBtn.onClick.AddListener(() => Buton_OgrenciDurumuGorBasildi(uretilenKod, ogrenciAdi));
                }
            }

            Transform copKutusuTransform = yeniOgrenciKutusu.transform.Find("CopKutusu");
            if (copKutusuTransform != null)
            {
                Button copBtn = copKutusuTransform.GetComponent<Button>();
                if (copBtn != null)
                {
                    copBtn.onClick.RemoveAllListeners();
                    copBtn.onClick.AddListener(() => Buton_CopKutusunaBasildi(uretilenKod, ogrenciAdi));
                }
            }

            // Arayüzü milisaniyeler içinde tazeletiyoruz
            Canvas.ForceUpdateCanvases();
            RectTransform containerRect = ogrenciListeContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            }
        }

        // Terapist Ana Menü'deki beyaz input alanını temizliyoruz
        if (anaMenuOgrenciEkleInput != null)
        {
            anaMenuOgrenciEkleInput.text = "";
        }

        // === 3. ADIM: ARKA PLANDA FIREBASE'E KAYDET (Sessizce çalışır) ===
        if (dbReference == null)
        {
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        }

        // Yeni öğrenci objesini oluşturuyoruz (Zaman damgasıyla birlikte)
        OdevKutusu yeniOgrenci = new OdevKutusu(ogrenciAdi, new List<string>(), suAnkiZaman);
        string json = JsonUtility.ToJson(yeniOgrenci);

        dbReference.Child("codes").Child(uretilenKod).SetRawJsonValueAsync(json).ContinueWith(task => {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($"[Talkie HATA] Arka planda {ogrenciAdi} kaydedilirken hata oluştu.");
                return;
            }
            Debug.Log($"[Talkie] {ogrenciAdi} arka planda Firebase'e başarıyla kaydedildi.");
        });
    }
    public void Buton_OgrenciSatirindakiOdevGonderBasildi(string ogrenciKodu, string ogrenciAdi)
    {
        // 1. Ödevin hangi öğrenciye gideceğini hafızaya alıyoruz
        PlayerPrefs.SetString("SonUretilenOgrenciKodu", ogrenciKodu);
        PlayerPrefs.SetString("SonUretilenOgrenciAdi", ogrenciAdi);
        PlayerPrefs.Save();

        Debug.Log($"[Talkie] Satırdan öğrenci seçildi: {ogrenciAdi} ({ogrenciKodu}). Geçiş yapılıyor...");

        // === YENİ EKLEMELER: ÖĞRENCİ ADINI KELİME PANELİNDEKİ TEXT ALANINA YAZDIR ===
        // Kelime listesi açıldığında en üstteki "Öğrenci adı..." yazan salt gösterim alanına tıkladığımız öğrencinin adını yazdırıyoruz.
        if (kelimePaneliOgrenciAdiText != null)
        {
            kelimePaneliOgrenciAdiText.text = ogrenciAdi;
        }

        // 2. Panelleri anında kapatıp açıyoruz (Sıfır bekleme)
        if (terapistAnaPaneli != null)
        {
            terapistAnaPaneli.SetActive(false); // Öğrenci listesini kapat
        }
        if (terapistYonetimPaneli != null)
        {
            terapistYonetimPaneli.SetActive(true); // Kelime seçim panelini aç
        }

        // 3. Panel yöneticisine durumu bildiriyoruz
        if (panelYoneticisi != null && terapistYonetimPaneli != null)
        {
            panelYoneticisi.PanelGecisiniKodaBildir(terapistYonetimPaneli);
        }

        // === KELİMELERİN YÜKLENMESİ VE SIRALANMASI ===
        // 4. Eğer listede hiç kelime yoksa veya ilk kez açılıyorsa dosyadan kelimeleri oku
        if (kelimeListesi == null || kelimeListesi.Count == 0)
        {
            DosyadanKelimeleriYukle();
        }

        // 5. Kelimeleri arayüze sıfır gecikmeyle anında diz
        KelimeListesiniFiltreleVeDoldur("");
    }

    private IEnumerator FirebaseOgrencileriniListeleCoroutine()
    {
        if (dbReference == null) yield break;

        var task = dbReference.Child("codes").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError("[Talkie] Öğrenci listesi Firebase'den çekilemedi.");
            yield break;
        }

        DataSnapshot snapshot = task.Result;
        if (snapshot.Exists)
        {
            // Beklemesiz, anında çizen fonksiyonumuzu çağırıyoruz!
            AnindaOgrenciListesiniCiz(snapshot);
        }
    }

    private void AnindaOgrenciListesiniCiz(DataSnapshot snapshot)
    {
        if (ogrenciListeContainer == null || ogrenciButonPrefab == null) return;

        // 1. Container'ı anında aktif et
        ogrenciListeContainer.gameObject.SetActive(true);

        // 2. Eski listeyi sıfır saniyede yok et
        for (int i = ogrenciListeContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(ogrenciListeContainer.GetChild(i).gameObject); // Anında yok etmek için DestroyImmediate
        }

        // 3. Verileri listeye alıyoruz
        List<DataSnapshot> ogrenciListesi = new List<DataSnapshot>();
        foreach (DataSnapshot childSnapshot in snapshot.Children)
        {
            if (childSnapshot.Child("ogrenciAdi").Value != null)
            {
                ogrenciListesi.Add(childSnapshot);
            }
        }

        // === KESİN SIRALAMA ÇÖZÜMÜ (ZAMAN DAMGASI) ===
        // Öğrencileri eklenme zamanına (timestamp) göre büyükten küçüğe sıralıyoruz.
        // Eğer eski veritabanı kayıtlarında timestamp yoksa null hatası vermemesi için güvenli kontrol ekledik.
        ogrenciListesi.Sort((a, b) => {
            long zamanA = 0;
            long zamanB = 0;

            if (a.HasChild("timestamp") && a.Child("timestamp").Value != null)
                long.TryParse(a.Child("timestamp").Value.ToString(), out zamanA);

            if (b.HasChild("timestamp") && b.Child("timestamp").Value != null)
                long.TryParse(b.Child("timestamp").Value.ToString(), out zamanB);

            // Büyük zaman (en yeni) en başta olacak şekilde karşılaştırıyoruz
            return zamanB.CompareTo(zamanA);
        });

        // 4. Sıfır saniyede prefabları üret
        foreach (DataSnapshot childSnapshot in ogrenciListesi)
        {
            string kod = childSnapshot.Key;
            string ogrenciAdi = childSnapshot.Child("ogrenciAdi").Value.ToString();

            GameObject yeniOgrenciKutusu = Instantiate(ogrenciButonPrefab, ogrenciListeContainer, false);
            yeniOgrenciKutusu.transform.localScale = Vector3.one;

            // Öğrenci Adı
            Transform ogrenciAdiTransform = yeniOgrenciKutusu.transform.Find("ogrenciadı");
            if (ogrenciAdiTransform != null)
            {
                TextMeshProUGUI txt = ogrenciAdiTransform.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = $"{ogrenciAdi} - ({kod})";
            }

            // Ödev Gönder Butonu
            Transform odevGonderTransform = yeniOgrenciKutusu.transform.Find("ödevigönder");
            if (odevGonderTransform != null)
            {
                Button odevBtn = odevGonderTransform.GetComponent<Button>();
                if (odevBtn != null)
                {
                    odevBtn.onClick.RemoveAllListeners();
                    odevBtn.onClick.AddListener(() => Buton_OgrenciSatirindakiOdevGonderBasildi(kod, ogrenciAdi));
                }
            }

            // Durum Butonu
            Transform durumTransform = yeniOgrenciKutusu.transform.Find("Durum");
            if (durumTransform != null)
            {
                Button durumBtn = durumTransform.GetComponent<Button>();
                if (durumBtn != null)
                {
                    durumBtn.onClick.RemoveAllListeners();
                    durumBtn.onClick.AddListener(() => Buton_OgrenciDurumuGorBasildi(kod, ogrenciAdi));
                }
            }

            // Çöp Kutusu Butonu
            Transform copKutusuTransform = yeniOgrenciKutusu.transform.Find("CopKutusu");
            if (copKutusuTransform != null)
            {
                Button copBtn = copKutusuTransform.GetComponent<Button>();
                if (copBtn != null)
                {
                    copBtn.onClick.RemoveAllListeners();
                    copBtn.onClick.AddListener(() => Buton_CopKutusunaBasildi(kod, ogrenciAdi));
                }
            }
        }

        // 5. Arayüzün boyutlarını ve çizimini sıfır gecikmeyle zorla yenile
        Canvas.ForceUpdateCanvases();

        var layoutGroup = ogrenciListeContainer.GetComponent<UnityEngine.UI.LayoutGroup>();
        var sizeFitter = ogrenciListeContainer.GetComponent<UnityEngine.UI.ContentSizeFitter>();

        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
            layoutGroup.enabled = true; // Kapatıp açarak sıfır gecikmeyle yeniliyoruz
        }

        if (sizeFitter != null)
        {
            sizeFitter.enabled = false;
            sizeFitter.enabled = true;
        }

        RectTransform containerRect = ogrenciListeContainer.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }
    }

    private void Buton_OgrenciDurumuGorBasildi(string kod, string isim)
    {
        Debug.Log($"[Talkie] {isim} ({kod}) isimli öğrencinin durum kontrolü tetiklendi.");
    }

    public void KelimeListesiniFiltreleVeDoldur(string arananKelime)
    {
        if (kelimeContainer == null) return;

        // 1. Önce içerideki tüm eski kelime butonlarını temizliyoruz
        foreach (Transform child in kelimeContainer)
        {
            Destroy(child.gameObject);
        }

        arananKelime = arananKelime.ToLower();
        int bulunanKelimeSayisi = 0; // Bulunan kelimeleri saymak için bir sayaç tutuyoruz

        // 2. Kelimeleri filtreleyip ekrana çiziyoruz
        foreach (string kelime in kelimeListesi)
        {
            if (string.IsNullOrEmpty(arananKelime) || kelime.ToLower().Contains(arananKelime))
            {
                bulunanKelimeSayisi++;

                GameObject yeniButon = Instantiate(kelimePrefab, kelimeContainer);
                yeniButon.transform.localScale = Vector3.one;

                TextMeshProUGUI txt = yeniButon.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = kelime;

                Image ImageGorseli = yeniButon.GetComponent<Image>();
                if (ImageGorseli != null)
                {
                    if (secilenKelimeler.Contains(kelime)) ImageGorseli.color = seciliRenk;
                    else ImageGorseli.color = normalRenk;
                }

                Button btn = yeniButon.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ButonSecildi(yeniButon));
                }
            }
        }
        // === KELİME BULUNAMADI UYARISI ===
        if (bulunanKelimeSayisi == 0)
        {
            // 1. ADIM: Yazıyı dikeyde aşağı itecek boş bir boşluk (Spacer) nesnesi oluşturuyoruz
            GameObject boslukObjesi = new GameObject("UyariBosluk");
            boslukObjesi.transform.SetParent(kelimeContainer, false);

            var boslukLayout = boslukObjesi.AddComponent<LayoutElement>();
            // Bu değeri artırarak yazıyı daha da aşağıya itebilirsin! (Örn: 250 veya 300)
            boslukLayout.preferredHeight = 250;

            // 2. ADIM: Şimdi gerçek uyarı yazımızı oluşturuyoruz
            GameObject uyariObjesi = new GameObject("KelimeBulunamadiUyari");
            uyariObjesi.transform.SetParent(kelimeContainer, false);

            var layoutElement = uyariObjesi.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 80;
            layoutElement.preferredWidth = 500;

            TextMeshProUGUI uyariMetni = uyariObjesi.AddComponent<TextMeshProUGUI>();
            uyariMetni.text = "Aradığınız kelime bulunamadı!";
            uyariMetni.fontSize = 28;

            uyariMetni.enableWordWrapping = false;
            uyariMetni.alignment = TextAlignmentOptions.Center;
            uyariMetni.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        }
        SecilenlerListesiniGuncelle();
    }
    void ButonSecildi(GameObject basilanButon)
    {
        Image ImageGorseli = basilanButon.GetComponent<Image>();
        TextMeshProUGUI butonYazisi = basilanButon.GetComponentInChildren<TextMeshProUGUI>();

        if (ImageGorseli != null && butonYazisi != null)
        {
            string kelime = butonYazisi.text;

            if (secilenKelimeler.Contains(kelime))
            {
                secilenKelimeler.Remove(kelime);
                ImageGorseli.color = normalRenk;
            }
            else
            {
                secilenKelimeler.Add(kelime);
                ImageGorseli.color = seciliRenk;
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

        // === SEÇİLEN DURUM MESAJI GÜNCELLEMESİ (1) ===
        // Kelimeler daha hiyerarşide oluşturulmadan önce sepet durum mesajını güncelliyoruz
        if (secilenlerDurumMesajiText != null)
        {
            if (secilenKelimeler.Count == 0)
            {
                secilenlerDurumMesajiText.text = "Henüz kelime seçilmedi.";
                secilenlerDurumMesajiText.color = Color.gray;
            }
            else
            {
                secilenlerDurumMesajiText.text = $"{secilenKelimeler.Count} kelime seçildi.";
                secilenlerDurumMesajiText.color = Color.black; // Tasarımına göre değiştirebilirsin
            }
        }

        foreach (string kelime in secilenKelimeler)
        {
            if (!string.IsNullOrEmpty(filtre) && !kelime.ToLower().Contains(filtre)) continue;

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
        string aktifKod = PlayerPrefs.GetString("SonUretilenOgrenciKodu", "");

        Debug.Log($"[Talkie] Ödev gönderimi tetiklendi. Aktif Öğrenci Kodu: {aktifKod}");

        if (string.IsNullOrEmpty(aktifKod))
        {
            if (kelimePaneliDurumMesajiText != null)
            {
                kelimePaneliDurumMesajiText.text = "Hata: Öğrenci seçilmedi!";
                kelimePaneliDurumMesajiText.color = Color.red;
            }
            return;
        }

        if (secilenKelimeler.Count == 0)
        {
            if (kelimePaneliDurumMesajiText != null)
            {
                kelimePaneliDurumMesajiText.text = "Lütfen önce kelime seçin!";
                kelimePaneliDurumMesajiText.color = Color.red;
            }
            return;
        }

        List<string> temizSecilenler = new List<string>();
        foreach (string k in secilenKelimeler)
        {
            if (!string.IsNullOrEmpty(k.Trim())) temizSecilenler.Add(k.Trim());
        }

        dbReference.Child("codes").Child(aktifKod).Child("kelimeler").SetValueAsync(temizSecilenler);

        if (kelimePaneliDurumMesajiText != null)
        {
            kelimePaneliDurumMesajiText.text = "Ödev Gönderildi!";
            kelimePaneliDurumMesajiText.color = Color.blue;
        }

        // === SEÇİLEN DURUM MESAJI GÜNCELLEMESİ (2) ===
        // Ödev gönderildikten sonra sepet durum mesajını eski temiz haline getiriyoruz
        if (secilenlerDurumMesajiText != null)
        {
            secilenlerDurumMesajiText.text = "Ödev başarıyla gönderildi!";
            secilenlerDurumMesajiText.color = Color.blue;
        }

        // Kelime seçimlerini ve sepeti sıfırla
        secilenKelimeler.Clear();
        SecilenlerListesiniGuncelle();
        KelimeListesiniFiltreleVeDoldur("");
        SepetKapa();
    }
    public void OnAramaDegisti()
    {
        if (aramaInput != null) KelimeListesiniFiltreleVeDoldur(aramaInput.text.Trim());
    }

    public void OnSecilenlerAramaDegisti()
    {
        if (secilenlerAramaInput != null) SecilenlerListesiniGuncelle(secilenlerAramaInput.text.Trim());
    }

    public void SepetAc()
    {
        if (sepetTransform == null) return;
        if (!sepetTransform.gameObject.activeSelf) sepetTransform.gameObject.SetActive(true);

        Animator panelAnim = sepetTransform.GetComponent<Animator>();
        if (panelAnim != null)
        {
            panelAnim.SetBool("IsOpen", true);
        }
    }

    public void SepetKapa()
    {
        if (sepetTransform == null) return;

        Animator panelAnim = sepetTransform.GetComponent<Animator>();
        if (panelAnim != null)
        {
            panelAnim.SetBool("IsOpen", false);
        }
    }

    public void TerapistKayitOl()
    {
        string kAdi = kayitKullaniciAdiInput.text.Replace("\u200B", "").Trim();
        string eposta = kayitEpostaInput.text.Replace("\u200B", "").Trim().ToLower();
        string sifre = kayitSifreInput.text.Replace("\u200B", "");
        string sifreTekrar = kayitSifreTekrarInput.text.Replace("\u200B", "");

        if (kayıtdurumMesajiText != null) kayıtdurumMesajiText.text = "";

        if (string.IsNullOrWhiteSpace(kAdi) || string.IsNullOrWhiteSpace(eposta) || string.IsNullOrWhiteSpace(sifre) || string.IsNullOrWhiteSpace(sifreTekrar))
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
        string eposta = girisEpostaInput.text.Replace("\u200B", "").Trim().ToLower();
        string sifre = girisSifreInput.text.Replace("\u200B", "");

        if (girisDurumMesajiText != null) girisDurumMesajiText.text = "";

        if (string.IsNullOrWhiteSpace(eposta) || string.IsNullOrWhiteSpace(sifre))
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

            if (panelYoneticisi != null && terapistAnaPaneli != null)
            {
                panelYoneticisi.BasariliGirisGecisiYap(terapistAnaPaneli);
            }
            else
            {
                if (girisYapAltPaneli != null) girisYapAltPaneli.SetActive(false);
                if (terapistAnaPaneli != null) terapistAnaPaneli.SetActive(true);
            }
        }
        else
        {
            GirisHataMesajiGoster("Şifre hatalı!");
        }
    }

    public void Buton_CopKutusunaBasildi(string kod, string isim)
    {
        Debug.Log($"[Talkie] Öğrenci silme tetiklendi: {isim} ({kod})");

        // === 1. ADIM: ARAYÜZDE ANINDA YOK ET (Sıfır Gecikme) ===
        foreach (Transform child in ogrenciListeContainer)
        {
            TextMeshProUGUI txt = child.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null && txt.text.Contains($"({kod})"))
            {
                Destroy(child.gameObject);
                break;
            }
        }

        Canvas.ForceUpdateCanvases();
        RectTransform containerRect = ogrenciListeContainer.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }

        // === 2. ADIM: ARKA PLANDA FIREBASE'DEN SİL ===
        if (dbReference == null)
        {
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        }

        dbReference.Child("codes").Child(kod).RemoveValueAsync().ContinueWith(task => {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($"[Talkie HATA] Arka planda {isim} silinirken hata oluştu.");
                return;
            }
            Debug.Log($"[Talkie] {isim} arka planda veritabanından başarıyla temizlendi.");
        });
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

    public void HesabinYokMuButonu()
    {
        if (girisYapAltPaneli != null) girisYapAltPaneli.SetActive(false);
        if (kayitOlAltPaneli != null) kayitOlAltPaneli.SetActive(true);
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

    [System.Serializable]
    private class OdevKutusu
    {
        public string ogrenciAdi;
        public List<string> kelimeler;
        public long timestamp;

        public OdevKutusu(string ad, List<string> liste, long zaman)
        {
            ogrenciAdi = ad;
            kelimeler = liste;
            timestamp = zaman;
        }
    }
}