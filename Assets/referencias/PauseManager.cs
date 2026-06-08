using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    // Singleton pra facilitar o acesso dos botões e do script do player
    public static PauseManager Instance { get; private set; }
    
    [Header("Referências")]
    [SerializeField] private GameObject menuPauseUI;
    
    [Header("Configurações")]
    [SerializeField] private string cenaMenuPrincipal = "MenuPrincipal";
    
    // Property pública pra outros scripts saberem se o jogo tá pausado
    public bool EstaPausado { get; private set; }
    
    void Awake()
    {
        // Setup do Singleton (mesmo padrão do GameManager)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        // Garante que o menu começa escondido
        if (menuPauseUI != null)
            menuPauseUI.SetActive(false);
        
        EstaPausado = false;
        Time.timeScale = 1f;
    }
    
    // Método principal: alterna entre pausado e não pausado
    public void AlternarPause()
    {
        if (EstaPausado)
            Continuar();
        else
            Pausar();
    }
    
    public void Pausar()
    {
        EstaPausado = true;
        Time.timeScale = 0f;
        
        if (menuPauseUI != null)
            menuPauseUI.SetActive(true);
    }
    
    // Método chamado pelo botão "VOLTAR"
    public void Continuar()
    {
        EstaPausado = false;
        Time.timeScale = 1f;
        
        if (menuPauseUI != null)
            menuPauseUI.SetActive(false);
    }
    
    // Método chamado pelo botão "MENU"
    public void IrParaMenuPrincipal()
    {
        // IMPORTANTE: restaura o timeScale antes de trocar de cena
        Time.timeScale = 1f;
        SceneManager.LoadScene(cenaMenuPrincipal);
    }
}