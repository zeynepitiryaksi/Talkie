using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    // Terapist butonuna basýlýnca Scene_Teacher sahnesini açacak
    public void OpenTeacherPanel()
    {
        SceneManager.LoadScene("Scene_Teacher");
    }

    // Danýþan butonuna basýlýnca Scene_Student sahnesini açacak
    public void OpenStudentPanel()
    {
        SceneManager.LoadScene("Scene_Student");
    }
}
