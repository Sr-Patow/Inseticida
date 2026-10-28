using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>True se o jogo está rodando (ou se ainda não existe GameManager na cena).</summary>
    public static bool Rodando => Instance == null || Instance.EstaJogando;

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

    [Header("Animação do Game Over")]
    [SerializeField] private Animator animatorGameOver;

    [Header("Configuração da Mira (Mouse)")]
    [SerializeField] private Texture2D texturaMira;

    [Header("Configurações do Jogador")]
    [SerializeField] private int vidasIniciais = 3;
    [SerializeField] private int vidasMaximas = 5;

    public enum EstadoJogo { Menu, Tutorial, Jogando, Pausado, GameOver }
    public EstadoJogo EstadoAtual { get; private set; }
    public bool EstaJogando => EstadoAtual == EstadoJogo.Jogando;

    /// <summary>Marcado pelo Spawner enquanto a loja está aberta (impede pausar com ESC).</summary>
    public bool LojaAberta { get; set; }

    private static readonly WaitForSeconds Espera4s = new WaitForSeconds(4f);
    private Coroutine rotinaLimpezaCoco;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        string cenaAtual = SceneManager.GetActiveScene().name;

        if (cenaAtual == DadosDaPartida.NomeCenaMenu)
        {
            DadosDaPartida.Resetar(vidasIniciais);
            EstadoAtual = EstadoJogo.Menu;
            Time.timeScale = 1f;
            RestaurarMousePadrao();
            return;
        }

        if (painelGameOver != null) painelGameOver.SetActive(false);
        if (painelPause != null) painelPause.SetActive(false);
        if (overlayCoco != null) overlayCoco.SetActive(false);

        if (DadosDaPartida.PartidaEmAndamento)
        {
            // Veio de uma troca de cena (dia <-> noite): continua direto, sem tutorial
            EstadoAtual = EstadoJogo.Jogando;
            Time.timeScale = 1f;
            if (painelTutorial != null) painelTutorial.SetActive(false);
            AtivarMiraCustomizada();
        }
        else
        {
            // Partida nova
            DadosDaPartida.Resetar(vidasIniciais);
            EstadoAtual = EstadoJogo.Tutorial;
            Time.timeScale = 0f;
            if (painelTutorial != null) painelTutorial.SetActive(true);
            RestaurarMousePadrao();
        }

        AtualizarTextoMoedas();
        AtualizarTextoVidas();

        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.MudarParaMusicaJogo();
    }

    void Update()
    {
        if (EstadoAtual == EstadoJogo.Tutorial && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            IniciarJogoDeVerdade();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && !LojaAberta)
        {
            if (EstadoAtual == EstadoJogo.Jogando) PausarJogo();
            else if (EstadoAtual == EstadoJogo.Pausado) RetomarJogo();
        }
    }

    private void IniciarJogoDeVerdade()
    {
        EstadoAtual = EstadoJogo.Jogando;
        DadosDaPartida.PartidaEmAndamento = true;
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

    // ===================== VIDAS =====================

    public void PerderVida()
    {
        if (!EstaJogando) return;
        DadosDaPartida.Vidas--;
        AtualizarTextoVidas();
        if (DadosDaPartida.Vidas <= 0) EncerrarJogo();
    }

    public void GanharVida()
    {
        if (!EstaJogando) return;
        if (DadosDaPartida.Vidas < vidasMaximas)
        {
            DadosDaPartida.Vidas++;
            AtualizarTextoVidas();
        }
    }

    // ===================== COCO NA TELA =====================

    public void SujarTelaComCoco()
    {
        if (overlayCoco == null) return;

        overlayCoco.SetActive(true);
        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.TocarSomMerda();

        if (rotinaLimpezaCoco != null) StopCoroutine(rotinaLimpezaCoco);
        rotinaLimpezaCoco = StartCoroutine(ContagemLimparTela());
    }

    private IEnumerator ContagemLimparTela()
    {
        yield return Espera4s;
        if (overlayCoco != null) overlayCoco.SetActive(false);
        rotinaLimpezaCoco = null;
    }

    // ===================== GAME OVER =====================

    public void EncerrarJogo()
    {
        if (EstadoAtual == EstadoJogo.GameOver) return;
        EstadoAtual = EstadoJogo.GameOver;

        if (overlayCoco != null) overlayCoco.SetActive(false);
        if (rotinaLimpezaCoco != null) StopCoroutine(rotinaLimpezaCoco);

        if (painelHUD != null) painelHUD.SetActive(false);
        if (painelGameOver != null) painelGameOver.SetActive(true);

        StartCoroutine(RotinaMorteComAnimacao());

        if (textoPontuacaoFinal != null)
            textoPontuacaoFinal.SetText("Pontuação Final: \n{0}", DadosDaPartida.Pontos);
        RestaurarMousePadrao();

        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.MudarParaMusicaGameOver();
    }

    private IEnumerator RotinaMorteComAnimacao()
    {
        if (animatorGameOver != null)
        {
            animatorGameOver.gameObject.SetActive(true);
            animatorGameOver.enabled = true;
            animatorGameOver.Play(0, 0, 0f);
        }

        yield return null;
        Time.timeScale = 0f;
    }

    // ===================== BOTÕES =====================

    public void ReiniciarFaseBotao()
    {
        EstadoAtual = EstadoJogo.Menu;
        Time.timeScale = 1f;
        DadosDaPartida.Resetar(vidasIniciais); // partida nova: zera tudo
        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.TocarSomRecarga();
        SceneManager.LoadScene(DadosDaPartida.NomeCenaDia);
    }

    public void VoltarMenuBotao()
    {
        EstadoAtual = EstadoJogo.Menu;
        Time.timeScale = 1f;
        DadosDaPartida.Resetar(vidasIniciais);
        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.PararMusica();
        SceneManager.LoadScene(DadosDaPartida.NomeCenaMenu);
    }

    public void SairDoJogoBotao()
    {
        EstadoAtual = EstadoJogo.Menu;
        Time.timeScale = 0f;
        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.TocarSomRecarga();
        Application.Quit();
    }

    // ===================== MOUSE =====================

    public void AtivarMiraCustomizada()
    {
        if (texturaMira != null)
        {
            Vector2 centroDaMira = new Vector2(texturaMira.width / 2f, texturaMira.height / 2f);
            Cursor.SetCursor(texturaMira, centroDaMira, CursorMode.Auto);
        }
    }

    public void RestaurarMousePadrao()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // ===================== PONTOS / MOEDAS =====================

    public void SomarPontos(int pontosParaGanhar)
    {
        if (EstaJogando) DadosDaPartida.Pontos += pontosParaGanhar;
    }

    public void SomarMoedas(int moedasParaGanhar)
    {
        if (!EstaJogando) return;
        DadosDaPartida.Moedas += moedasParaGanhar;
        AtualizarTextoMoedas();
    }

    /// <summary>Use nos botões da loja. Retorna false se não houver moedas suficientes.</summary>
    public bool GastarMoedas(int valor)
    {
        if (valor < 0 || DadosDaPartida.Moedas < valor) return false;
        DadosDaPartida.Moedas -= valor;
        AtualizarTextoMoedas();
        if (SpawnerAlvos.Instance != null) SpawnerAlvos.Instance.AtualizarMoedasLoja();
        return true;
    }

    public int ObterMoedasAtual() => DadosDaPartida.Moedas;

    private void AtualizarTextoMoedas()
    {
        if (textoMoedas != null) textoMoedas.SetText("Moedas: {0}", DadosDaPartida.Moedas);
    }

    private void AtualizarTextoVidas()
    {
        if (textoVidas != null) textoVidas.SetText("Vidas: {0}", DadosDaPartida.Vidas);
    }
}
