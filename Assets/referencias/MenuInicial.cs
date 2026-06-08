using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuInicial : MonoBehaviour
{
    public void IniciarJogo()
    {
        SceneManager.LoadScene("fase 1");
    }

    public void FecharJogo()
    {
        Application.Quit();
    }
}
