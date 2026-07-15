using System.Collections.Generic;
using UnityEngine;

public class PanelYoneticisi : MonoBehaviour
{
    [Header("Geri Dönüþ Butonu")]
    public GameObject globalGeriButonu;

    [Header("Oyun Ýlk Açýldýðýnda Ekranda Duran Panel")]
    public GameObject oyunBaslangicPaneli;

    [Header("Sahnede Yönetilecek Tüm Alt Paneller")]
    [Tooltip("Üst üste binmeyi önlemek için tüm panelleri bu listeye sürükleyin!")]
    public List<GameObject> tumPaneller = new List<GameObject>();

    private Stack<GameObject> panelGecmisi = new Stack<GameObject>();
    private GameObject aktifPanel;

    private void Start()
    {
        panelGecmisi = new Stack<GameObject>();

        // === KRÝTÝK DÜZELTME 1 ===
        // Ýlk baþlangýç panelini aktifPanel olarak atayýp, 
        // ardýndan diðer tüm her þeyi kapatmaya zorluyoruz.
        if (oyunBaslangicPaneli != null)
        {
            aktifPanel = oyunBaslangicPaneli;
        }

        // Listeye eklediðin ne varsa (giriþ panelleri, ana butonlar vs.) hepsini kapatýr
        TumPanelleriKapat();

        // Sadece baþlangýç panelini temiz bir þekilde açar
        if (aktifPanel != null)
        {
            aktifPanel.SetActive(true);
        }

        GeriButonunuGuncelle();
    }

    // Yeni bir panele geçmek için butonlara ekleyeceðin fonksiyon
    public void PanelGecisiniKodaBildir(GameObject acilanPanel)
    {
        if (acilanPanel == null)
        {
            Debug.LogError("PanelYoneticisi: Açýlmaya çalýþýlan panel boþ (Null)!");
            return;
        }

        // Eðer þu an ekranda bir panel varsa ve yenisinden farklýysa eskisini geçmiþe at
        if (aktifPanel != null && aktifPanel != acilanPanel)
        {
            panelGecmisi.Push(aktifPanel);
        }

        // Yenisini aktif panel yapýp her þeyi kapatýyoruz
        aktifPanel = acilanPanel;
        TumPanelleriKapat();

        // Sadece gitmek istediðimiz paneli açýyoruz
        aktifPanel.SetActive(true);

        GeriButonunuGuncelle();
    }

    // --- BAÞARILI GÝRÝÞ YAPILDIÐINDA ÇALIÞACAK GÜVENLÝ FONKSÝYON ---
    public void BasariliGirisGecisiYap(GameObject anaPanel)
    {
        if (anaPanel == null)
        {
            Debug.LogError("PanelYoneticisi: BasariliGirisGecisiYap için gelen anaPanel Null!");
            return;
        }

        // Eðer bu yeni panel listemizde yoksa otomatik olarak listeye ekleyelim ki kazara kapanmasýn!
        if (!tumPaneller.Contains(anaPanel))
        {
            tumPaneller.Add(anaPanel);
        }

        // Eski aktif paneli (Giriþ Panelini) geçmiþe atýyoruz
        if (aktifPanel != null && aktifPanel != anaPanel)
        {
            panelGecmisi.Clear(); // Önceki sayfalarý temizle
            panelGecmisi.Push(aktifPanel); // Sadece GÝRÝÞ PANELÝNÝ geçmiþe ekle
        }

        // Yeni paneli aktif yapýp diðer her þeyi kapatýyoruz
        aktifPanel = anaPanel;
        TumPanelleriKapat();

        // Sadece yeni paneli aç
        aktifPanel.SetActive(true);

        GeriButonunuGuncelle();
    }

    // Geri butonuna basýldýðýnda çalýþacak fonksiyon
    public void GeriDon()
    {
        if (panelGecmisi.Count > 0)
        {
            // Hafýzadan bir önceki paneli çek
            aktifPanel = panelGecmisi.Pop();

            // Diðerlerini kapatýp sadece geçmiþten çekileni aç
            TumPanelleriKapat();
            if (aktifPanel != null)
            {
                aktifPanel.SetActive(true);
            }
        }

        GeriButonunuGuncelle();
    }

    private void TumPanelleriKapat()
    {
        foreach (GameObject panel in tumPaneller)
        {
            if (panel != null && panel != globalGeriButonu)
            {
                // EÐER OBJENÝN ADINDA "Scroll" VEYA "Content" VEYA "Viewport" GEÇÝYORSA KAZARA KAPATMA!
                if (panel.name.Contains("Scroll") || panel.name.Contains("Content") || panel.name.Contains("Viewport"))
                {
                    continue;
                }

                // Eðer kapatýlmaya çalýþýlan panel o an açmak istediðimiz aktif panel ise kapatmýyoruz!
                if (aktifPanel != null && panel == aktifPanel)
                {
                    continue;
                }

                panel.SetActive(false);
            }
        }
    }

    private void GeriButonunuGuncelle()
    {
        if (globalGeriButonu != null)
        {
            globalGeriButonu.SetActive(panelGecmisi.Count > 0);
        }
    }
}