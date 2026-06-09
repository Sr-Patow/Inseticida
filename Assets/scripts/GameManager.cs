using UnityEngine;           
using UnityEngine.UI;        
using TMPro;                 
using System.Collections;
using UnityEngine.SceneManagement;    

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("UI - Durante o Jogo")]
    [SerializeField] private TextMeshProUGUI textoMoedas; 
    [SerializeField] private TextMeshProUGUI textoVidas; 
    [SerializeField] private GameObject painelPause; 
    [SerializeField] private GameObject painelTutorial; 

    [Header("UI - Painel HUD")]
    [SerializeField] private GameObject painelHUD;

    [Header("UI - Mecânicas Novas")]
    [SerializeField] private GameObject overlayCoco;    

    [Header("UI - Game Over")]
    [SerializeField] private GameObject painelGameOver; 
    [SerializeField] private TextMeshProUGUI textoPontuacaoFinal; 

    // ==== IGUAL AO DOS INIMIGOS: REFERÊNCIA DO ANIMATOR ====
    [Header("Animação do Game Over")]
    [SerializeField] private Animator animatorGameOver; 

    [Header("Configuração da Mira (Mouse)")]
    [SerializeField] private Texture2D texturaMira; 

    public enum EstadoJogo { Menu, Tutorial, Jogando, Pausado, GameOver }
    public EstadoJogo EstadoAtual { get; private set; }
    
    private int pontuacaoAtual = 0; 
    private int moedasAtual = 0; 
    
    [Header("Configurações do Jogador")]
    [SerializeField] private int vidasMaximas = 3; 
    private int vidasAtuais;
    
    public bool EstaJogando => EstadoAtual == EstadoJogo.Jogando;
    private Coroutine rotinaLimpezaCoco;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    void Start()
    {
        string cenaAtual = SceneManager.GetActiveScene().name;

        if (cenaAtual == "MenuPrincipal")
        {
            EstadoAtual = EstadoJogo.Menu;
            Time.timeScale = 1f;
            RestaurarMousePadrao();
        }
        else
        {
            EstadoAtual = EstadoJogo.Tutorial; 
            Time.timeScale = 0f;
            vidasAtuais = vidasMaximas;
            
            if (painelGameOver != null) painelGameOver.SetActive(false);
            if (painelPause != null) painelPause.SetActive(false);
            if (painelTutorial != null) painelTutorial.SetActive(true);
            if (overlayCoco != null) overlayCoco.SetActive(false);     
            
            AtualizarTextoMoedas();
            AtualizarTextoVidas();
            RestaurarMousePadrao(); 

            if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.MudarParaMusicaJogo();
        }
    }
    
    void Update()
    {
        if (EstadoAtual == EstadoJogo.Tutorial && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            IniciarJogoDeVerdade();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (EstadoAtual == EstadoJogo.Jogando) PausarJogo();
            else if (EstadoAtual == EstadoJogo.Pausado) RetomarJogo();
        }
    }

    private void IniciarJogoDeVerdade()
    {
        EstadoAtual = EstadoJogo.Jogando;
        Time.timeScale = 1f;
        if (painelTutorial != null) painelTutorial.SetActive(false);
        AtivarMiraCustomizada();
    }
    
    public void PausarJogo()
    {
        if (EstadoAtual != EstadoJogo.Jogando) return;
        EstadoAtual = EstadoJogo.Pausado;
        Time.timeScale = 0f;
        if (painelPause != null) painelPause.SetActive(true);
        RestaurarMousePadrao();

        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.MudarParaMusicaPause();
    }

    public void RetomarJogo()
    {
        if (EstadoAtual != EstadoJogo.Pausado) return;
        EstadoAtual = EstadoJogo.Jogando;
        Time.timeScale = 1f;
        if (painelPause != null) painelPause.SetActive(false);
        AtivarMiraCustomizada();

        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.MudarParaMusicaJogo();
    }

    public void PerderVida()
    {
        if (!EstaJogando) return;
        vidasAtuais--;
        AtualizarTextoVidas();
        if (vidasAtuais <= 0) EncerrarJogo();
    }

    public void GanharVida()
    {
        if (!EstaJogando) return;
        if (vidasAtuais < vidasMaximas)
        {
            vidasAtuais++;
            AtualizarTextoVidas();
        }
    }

    public void SujarTelaComCoco()
    {
        if (overlayCoco != null)
        {
            overlayCoco.SetActive(true);

            if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.TocarSomMerda();

            if (rotinaLimpezaCoco != null) StopCoroutine(rotinaLimpezaCoco);
            rotinaLimpezaCoco = StartCoroutine(ContagemLimparTela());
        }
    }

    private IEnumerator ContagemLimparTela()
    {
        yield return new WaitForSeconds(4f);
        if (overlayCoco != null) overlayCoco.SetActive(false);
        rotinaLimpezaCoco = null;
    }

    private void AtualizarTextoVidas()
    {
        if (textoVidas != null) textoVidas.text = "Vidas: " + vidasAtuais.ToString();
    }

    // ==== FUNÇÃO ATUALIZADA COM O DISPARO DA ANIMAÇÃO IGUAl AOS INIMIGOS ====
    public void EncerrarJogo()
    {
        if (EstadoAtual == EstadoJogo.GameOver) return; 
        EstadoAtual = EstadoJogo.GameOver;

        // Paramos as rotinas normais do jogo
        if (overlayCoco != null) overlayCoco.SetActive(false);
        if (rotinaLimpezaCoco != null) StopCoroutine(rotinaLimpezaCoco);

        // Esconde o HUD e mostra os botões de Game Over
        if (painelHUD != null) painelHUD.SetActive(false);
        if (painelGameOver != null) painelGameOver.SetActive(true);

        // CHAMA A NOVA ROTINA: Ela vai ligar a animação ANTES de congelar o jogo
        StartCoroutine(RotinaMorteComAnimacao());

        if (textoPontuacaoFinal != null) textoPontuacaoFinal.text = "Pontuação Final: \n" + pontuacaoAtual.ToString();
        RestaurarMousePadrao();

        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.MudarParaMusicaGameOver();
    }

    // ==== COROUTINE NOVA QUE RESOLVE O CONGELAMENTO DO ANIMATOR ====
    private IEnumerator RotinaMorteComAnimacao()
    {
        if (animatorGameOver != null)
        {
            // 1. Ativa o objeto do fundo enquanto o tempo do jogo AINDA ESTÁ RODANDO
            animatorGameOver.gameObject.SetActive(true); 
            animatorGameOver.enabled = true;
            
            // 2. Manda a animação iniciar
            animatorGameOver.Play(0, 0, 0f); 
        }

        // 3. ESPERA UM FRAME: Dá tempo para a Unity carregar os sprites na memória
        yield return null; 

        // 4. AGORA SIM: Congela o tempo do jogo. 
        // Como o Animator já começou a rodar e está em Unscaled Time, ele vai continuar fluindo!
        Time.timeScale = 0f; 
    }

    public void ReiniciarFaseBotao()
    {
        EstadoAtual = EstadoJogo.Menu; 
        Time.timeScale = 1f; 
        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.TocarSomRecarga();
        SceneManager.LoadScene("Jogo");
    }

    public void VoltarMenuBotao()
    {
        EstadoAtual = EstadoJogo.Menu; 
        Time.timeScale = 1f; 
        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.PararMusica();
        SceneManager.LoadScene("MenuPrincipal");
    }

    public void SairDoJogoBotao()
    {
        EstadoAtual = EstadoJogo.Menu; 
        Time.timeScale = 0f; 
        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.TocarSomRecarga();
        Debug.Log("Saiu do Jogo!");
        Application.Quit();
    }

    private void AtivarMiraCustomizada()
    {
        if (texturaMira != null)
        {
            Vector2 centroDaMira = new Vector2(texturaMira.width / 2f, texturaMira.height / 2f);
            Cursor.SetCursor(texturaMira, centroDaMira, CursorMode.Auto);
        }
    }

    private void RestaurarMousePadrao()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void SomarPontos(int pontosParaGanhar) { if (EstaJogando) pontuacaoAtual += pontosParaGanhar; }
    public void SomarMoedas(int moedasParaGanhar) { if (EstaJogando) { moedasAtual += moedasParaGanhar; AtualizarTextoMoedas(); } }
    private void AtualizarTextoMoedas() { if (textoMoedas != null) textoMoedas.text = "Moedas: " + moedasAtual.ToString(); }
}