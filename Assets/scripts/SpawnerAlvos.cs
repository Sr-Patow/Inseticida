using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;

public class SpawnerAlvos : MonoBehaviour
{
    public static SpawnerAlvos Instance { get; private set; }

    [Header("Configurações do Prefab")]
    [SerializeField] private GameObject prefabAlvo;

    [Header("Tabelas de Inimigos (.CSV)")]
    [Tooltip("Usada na cena 'Jogo' (dia)")]
    [SerializeField] private TextAsset csvDia;
    [Tooltip("Usada na cena 'JogoNoite' (noite)")]
    [SerializeField] private TextAsset csvNoite;

    [Header("Catálogo Visual (todos os inimigos de dia e de noite)")]
    [SerializeField] private ConfigInimigo[] catalogo;

    [Header("Pontos de Spawn (objetos vazios espalhados na tela)")]
    [Tooltip("Usados por todo inimigo que não tem pontos próprios nem 'Spawn Aleatorio'.")]
    [SerializeField] private Transform[] pontosDeSpawnGlobais;

    [Header("Tempo de Spawn")]
    [SerializeField] private float tempoInicialEspera = 2f;
    [SerializeField] private float intervaloSpawn = 1.5f;
    [SerializeField] private float recuoBordaSuperior = 1f;

    [Header("UI - Sistema de Loja")]
    [SerializeField] private GameObject painelLoja;
    [SerializeField] private TextMeshProUGUI textoMoedasLoja;

    [Header("UI - Sistema de Rounds")]
    [SerializeField] private TextMeshProUGUI textoRoundHUD;
    [SerializeField] private TextMeshProUGUI textoAvisoVidaInimigos;

    // ---- Dados da tabela carregada (dia OU noite) ----
    private readonly List<DadosInimigo> tabela = new List<DadosInimigo>();
    private readonly Dictionary<string, DadosInimigo> dadosPorNome = new Dictionary<string, DadosInimigo>();
    private readonly Dictionary<string, ConfigInimigo> configPorNome = new Dictionary<string, ConfigInimigo>();
    private int[] restantes; // quantos faltam spawnar de cada linha da tabela, neste round

    // ---- Limites da tela calculados uma única vez ----
    private Camera cameraPrincipal;
    private float xMin, xMax, yTopo; // usados só no spawn aleatório

    // ---- Pool ----
    private ObjectPool<alvo> pool;

    // ---- Controle ----
    private int inimigosVivosNaTela = 0;
    private bool lojaAberta = false;
    private WaitForSeconds esperaSpawn;
    private static readonly WaitForSeconds Espera1_5s = new WaitForSeconds(1.5f);
    private static readonly WaitForSeconds Espera3s = new WaitForSeconds(3f);

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        cameraPrincipal = Camera.main;
        esperaSpawn = new WaitForSeconds(intervaloSpawn);

        CalcularLimitesDaTela();
        MontarCatalogo();

        TextAsset csv = DadosDaPartida.EhNoite ? csvNoite : csvDia;
        if (csv == null)
        {
            Debug.LogWarning("CSV da cena atual não definido; usando o CSV do dia.");
            csv = csvDia;
        }
        CarregarTabela(csv);

        pool = new ObjectPool<alvo>(
            createFunc: CriarAlvo,
            actionOnGet: a => a.gameObject.SetActive(true),
            actionOnRelease: a => a.gameObject.SetActive(false),
            actionOnDestroy: a => { if (a != null) Destroy(a.gameObject); },
            collectionCheck: false,
            defaultCapacity: 16,
            maxSize: 64);

        if (textoAvisoVidaInimigos != null) textoAvisoVidaInimigos.gameObject.SetActive(false);
        if (painelLoja != null) painelLoja.SetActive(false);
        if (textoMoedasLoja != null) textoMoedasLoja.gameObject.SetActive(false);

        AtualizarTextoRound();
        StartCoroutine(RotinaFluxoDeRounds());
    }

    private alvo CriarAlvo()
    {
        GameObject obj = Instantiate(prefabAlvo);
        return obj.GetComponent<alvo>();
    }

    public void Devolver(alvo a)
    {
        if (pool != null) pool.Release(a);
        else a.gameObject.SetActive(false);
    }

    // =============== PREPARAÇÃO ===============

    private void CalcularLimitesDaTela()
    {
        float z = cameraPrincipal.nearClipPlane;
        xMin = cameraPrincipal.ViewportToWorldPoint(new Vector3(0.1f, 1f, z)).x;
        xMax = cameraPrincipal.ViewportToWorldPoint(new Vector3(0.9f, 1f, z)).x;
        yTopo = cameraPrincipal.ViewportToWorldPoint(new Vector3(0.5f, 1f, z)).y - recuoBordaSuperior;
    }

    private void MontarCatalogo()
    {
        configPorNome.Clear();
        if (catalogo == null) return;
        for (int i = 0; i < catalogo.Length; i++)
        {
            if (catalogo[i] == null || string.IsNullOrEmpty(catalogo[i].nome)) continue;
            configPorNome[catalogo[i].nome.Trim().ToLowerInvariant()] = catalogo[i];
        }
    }

    private void CarregarTabela(TextAsset csv)
    {
        tabela.Clear();
        dadosPorNome.Clear();
        if (csv == null) return;

        string[] linhas = csv.text.Split('\n');

        for (int i = 1; i < linhas.Length; i++) // pula o cabeçalho
        {
            string linha = linhas[i].Trim();
            if (string.IsNullOrEmpty(linha)) continue;

            string[] c = linha.Split(',');
            if (c.Length < 4) continue;

            DadosInimigo d = new DadosInimigo();
            d.nome = c[0].Trim().ToLowerInvariant();
            d.vida = LerInt(c[1], 3);
            d.pontos = LerInt(c[2], 10);
            d.moedas = LerInt(c[3], 1);
            d.qtdInicial = c.Length > 4 ? LerInt(c[4], 1) : 1;
            d.crescACada = c.Length > 5 ? LerInt(c[5], 0) : 0;

            tabela.Add(d);
            dadosPorNome[d.nome] = d;
        }

        restantes = new int[tabela.Count];
    }

    private static int LerInt(string texto, int padrao)
    {
        return int.TryParse(texto.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int valor) ? valor : padrao;
    }

    // =============== FLUXO DE ROUNDS ===============

    private IEnumerator RotinaFluxoDeRounds()
    {
        if (tabela.Count == 0)
        {
            Debug.LogError("SpawnerAlvos: tabela de inimigos vazia. Verifique o CSV.");
            yield break;
        }

        yield return new WaitForSeconds(tempoInicialEspera);

        while (true)
        {
            AtualizarTextoRound();
            PrepararQuantidadesDoRound();

            // 10% de chance de todos os inimigos ganharem +1 de vida
            if (Random.Range(1, 11) == 1)
            {
                DadosDaPartida.VidaExtraInimigos++;
                StartCoroutine(MostrarAvisoVidaExtra());
            }

            yield return StartCoroutine(SpawnarFilaDeInimigos());

            while (inimigosVivosNaTela > 0) yield return null;

            int roundTerminado = DadosDaPartida.Round;

            // Loja a cada 2 rounds
            if (roundTerminado % 2 == 0)
            {
                AbrirLoja();
                while (lojaAberta) yield return null;
            }

            DadosDaPartida.Round++;
            AtualizarTextoRound();
            yield return Espera1_5s;

            // Ciclo dia/noite: a cada 5 rounds troca de cena
            if (roundTerminado % DadosDaPartida.RoundsPorCiclo == 0)
            {
                // Espera se estiver pausado; aborta se deu Game Over
                while (GameManager.Instance != null && !GameManager.Instance.EstaJogando)
                {
                    if (GameManager.Instance.EstadoAtual == GameManager.EstadoJogo.GameOver) yield break;
                    yield return null;
                }

                SceneManager.LoadScene(DadosDaPartida.ProximaCena);
                yield break;
            }
        }
    }

    private void PrepararQuantidadesDoRound()
    {
        int roundsPassados = DadosDaPartida.Round - 1;
        for (int i = 0; i < tabela.Count; i++)
        {
            DadosInimigo d = tabela[i];
            restantes[i] = d.qtdInicial + (d.crescACada > 0 ? roundsPassados / d.crescACada : 0);
        }
    }

    private IEnumerator SpawnarFilaDeInimigos()
    {
        int total = 0;
        for (int i = 0; i < restantes.Length; i++) total += restantes[i];

        while (total > 0)
        {
            if (!GameManager.Rodando)
            {
                yield return null;
                continue;
            }

            // Sorteia entre os que ainda faltam (sem desperdiçar o intervalo)
            int sorteio = Random.Range(0, total);
            for (int i = 0; i < restantes.Length; i++)
            {
                if (sorteio < restantes[i])
                {
                    restantes[i]--;
                    total--;
                    SpawnarDaTabela(tabela[i]);
                    break;
                }
                sorteio -= restantes[i];
            }

            yield return esperaSpawn;
        }
    }

    // =============== SPAWN ===============

    private void SpawnarDaTabela(DadosInimigo dados)
    {
        ConfigInimigo cfg = ObterConfig(dados.nome);
        if (cfg == null) return;

        CriarInimigo(dados, cfg, EscolherPosicaoSpawn(cfg));
    }

    private Vector3 EscolherPosicaoSpawn(ConfigInimigo cfg)
    {
        if (!cfg.spawnAleatorio)
        {
            // Pontos próprios do inimigo; se não houver, os globais
            Transform[] pontos = (cfg.pontosDeSpawn != null && cfg.pontosDeSpawn.Length > 0)
                ? cfg.pontosDeSpawn
                : pontosDeSpawnGlobais;

            if (pontos != null && pontos.Length > 0)
            {
                Transform ponto = pontos[Random.Range(0, pontos.Length)];
                if (ponto != null)
                {
                    Vector3 pos = ponto.position;
                    pos.z = 0f;
                    return pos;
                }
            }
        }

        // Spawn aleatório no topo da tela (modo escolhido ou sem pontos configurados)
        return new Vector3(Random.Range(xMin, xMax), yTopo, 0f);
    }

    /// <summary>Usado por inimigos que geram outros (colmeia -> abelhas). Agora respeita a posição.</summary>
    public void SpawnarInimigoEspecifico(string nome, Vector3 posicao)
    {
        string chave = nome.Trim().ToLowerInvariant();
        ConfigInimigo cfg = ObterConfig(chave);
        if (cfg == null) return;

        if (!dadosPorNome.TryGetValue(chave, out DadosInimigo dados))
        {
            dados = new DadosInimigo { nome = chave, vida = 3, pontos = 10, moedas = 1 };
        }

        posicao.z = 0f;
        CriarInimigo(dados, cfg, posicao);
    }

    private void CriarInimigo(DadosInimigo dados, ConfigInimigo cfg, Vector3 posicao)
    {
        if (prefabAlvo == null || pool == null) return;

        int vidaFinal = dados.vida;
        if (cfg.comportamento != Comportamento.Cura)
        {
            vidaFinal += DadosDaPartida.VidaExtraInimigos;
            inimigosVivosNaTela++;
        }

        alvo a = pool.Get();
        a.transform.SetPositionAndRotation(posicao, Quaternion.identity);
        a.Inicializar(cfg, dados, vidaFinal);
    }

    private ConfigInimigo ObterConfig(string nome)
    {
        if (configPorNome.TryGetValue(nome, out ConfigInimigo cfg)) return cfg;
        Debug.LogWarning("SpawnerAlvos: '" + nome + "' não está no Catálogo Visual.");
        return null;
    }

    public void InimigoDerrotado()
    {
        inimigosVivosNaTela = Mathf.Max(0, inimigosVivosNaTela - 1);
    }

    // =============== LOJA ===============

    private void AbrirLoja()
    {
        lojaAberta = true;
        Time.timeScale = 0f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LojaAberta = true;
            GameManager.Instance.RestaurarMousePadrao();
        }

        if (painelLoja != null) painelLoja.SetActive(true);
        if (textoMoedasLoja != null) textoMoedasLoja.gameObject.SetActive(true);

        AtualizarMoedasLoja();
    }

    // Ligue este método ao botão "Continuar" da loja
    public void FecharLoja()
    {
        if (painelLoja != null) painelLoja.SetActive(false);
        if (textoMoedasLoja != null) textoMoedasLoja.gameObject.SetActive(false);

        Time.timeScale = 1f;
        lojaAberta = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LojaAberta = false;
            GameManager.Instance.AtivarMiraCustomizada();
        }
    }

    public void AtualizarMoedasLoja()
    {
        if (textoMoedasLoja != null && GameManager.Instance != null)
        {
            textoMoedasLoja.SetText("Moedas: {0}", GameManager.Instance.ObterMoedasAtual());
        }
    }

    // =============== UI ===============

    private IEnumerator MostrarAvisoVidaExtra()
    {
        if (textoAvisoVidaInimigos == null) yield break;

        textoAvisoVidaInimigos.text = "Os inimigos parecem mais fortes...";
        textoAvisoVidaInimigos.gameObject.SetActive(true);
        yield return Espera3s;
        textoAvisoVidaInimigos.gameObject.SetActive(false);
    }

    private void AtualizarTextoRound()
    {
        if (textoRoundHUD != null) textoRoundHUD.SetText("Round: {0}", DadosDaPartida.Round);
    }
}
