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

        // Ýlk baþta tüm panelleri kapatýp sadece baþlangýç panelini açalým
        TumPanelleriKapat();

        if (oyunBaslangicPaneli != null)
        {
            aktifPanel = oyunBaslangicPaneli;
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

        // Ekrandaki eski panelleri temizle
        TumPanelleriKapat();

        // Yenisini aktif yap ve hafýzaya al
        aktifPanel = acilanPanel;
        aktifPanel.SetActive(true);

        GeriButonunuGuncelle();
    }

    // Geri butonuna basýldýðýnda çalýþacak fonksiyon
    public void GeriDon()
    {
        if (panelGecmisi.Count > 0)
        {
            TumPanelleriKapat();

            // Hafýzadan bir önceki paneli çek ve aç
            aktifPanel = panelGecmisi.Pop();
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
            // Küçücük bir önlem: globalGeriButonu kazara listenin içindeyse onu kapatmasýn
            if (panel != null && panel != globalGeriButonu)
            {
                panel.SetActive(false);
            }
        }
    }

    private void GeriButonunuGuncelle()
    {
        if (globalGeriButonu != null)
        {
            // Geri butonu geçmiþte panel varsa aktif olsun
            globalGeriButonu.SetActive(panelGecmisi.Count > 0);
        }
    }
}