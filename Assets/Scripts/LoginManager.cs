using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{

    public void OpenTeacherPanel()
    {
        SceneManager.LoadScene("Scene_Teacher");
    }

  
    public void OpenStudentPanel()
    {
        SceneManager.LoadScene("Scene_Student");
    }
}
