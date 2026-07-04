using TMPro; 
using UnityEngine;

public class TerapistGirisKontrol : MonoBehaviour
{
    [Header("Giriþ Paneli Elemanlarý")]
    public TMP_InputField girisSifreKutusu;
    public GameObject girisGozAcikButonu;
    public GameObject girisGozKapaliButonu;

    [Header("Kayýt Paneli - Þifre")]
    public TMP_InputField kayitSifreKutusu;
    public GameObject kayitGozAcikButonu;
    public GameObject kayitGozKapaliButonu;

    [Header("Kayýt Paneli - Þifre Tekrar")]
    public TMP_InputField kayitTekrarSifreKutusu;
    public GameObject kayitTekrarGozAcikButonu;
    public GameObject kayitTekrarGozKapaliButonu;

   
    public void GirisSifreGosterKapat()
    {
        ToggleSifre(girisSifreKutusu, girisGozAcikButonu, girisGozKapaliButonu);
    }


    public void KayitSifreGosterKapat()
    {
        ToggleSifre(kayitSifreKutusu, kayitGozAcikButonu, kayitGozKapaliButonu);
    }

 
    public void KayitTekrarSifreGosterKapat()
    {
        ToggleSifre(kayitTekrarSifreKutusu, kayitTekrarGozAcikButonu, kayitTekrarGozKapaliButonu);
    }

   
    private void ToggleSifre(TMP_InputField inputField, GameObject gozAcik, GameObject gozKapali)
    {
        if (inputField.contentType == TMP_InputField.ContentType.Password)
        {
            inputField.contentType = TMP_InputField.ContentType.Standard;
            gozAcik.SetActive(true);
            gozKapali.SetActive(false);
        }
        else
        {
            inputField.contentType = TMP_InputField.ContentType.Password;
            gozAcik.SetActive(false);
            gozKapali.SetActive(true);
        }
        inputField.ForceLabelUpdate();
    }
}