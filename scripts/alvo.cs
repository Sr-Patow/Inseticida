using UnityEngine;
using System.Collections;

public class alvo : MonoBehaviour
{
    // Cacheados uma única vez: evitam alocação de memória a cada uso
    private static readonly int HashAtacar = Animator.StringToHash("atacar");
    private static readonly int HashMorrer = Animator.StringToHash("morrer");
    private static readonly WaitForSeconds Espera1s = new WaitForSeconds(1f);
    private static readonly WaitForSeconds Espera1_5s = new WaitForSeconds(1.5f);
    private static readonly WaitForSeconds Espera2_5s = new WaitForSeconds(2.5f);
    private static readonly WaitForSeconds EsperaMorte = new WaitForSeconds(0.5f);

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Collider2D colisor;

    private ConfigInimigo config;
    private int vida;
    private int pontosQueVale;
    private int moedasQueVale;

    private bool ativo;          // false = está no pool (ou já foi devolvido)
    private bool estaMorrendo;
    private bool usaTimer;
    private float tempoRestante;
    private Vector3 direcao;

    private bool EhCura => config.comportamento == Comportamento.Cura;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        colisor = GetComponent<Collider2D>();
    }

    /// <summary>Chamado pelo Spawner toda vez que o objeto sai do pool (a posição já foi definida).</summary>
    public void Inicializar(ConfigInimigo cfg, DadosInimigo dados, int vidaFinal)
    {
        config = cfg;
        vida = vidaFinal;
        pontosQueVale = dados.pontos;
        moedasQueVale = dados.moedas;

        ativo = true;
        estaMorrendo = false;
        if (colisor != null) colisor.enabled = true;

        // Visual: Animator OU sprite estático
        if (cfg.animator != null)
        {
            if (animator != null)
            {
                animator.enabled = true;
                animator.runtimeAnimatorController = cfg.animator;
                animator.Rebind(); // limpa triggers/estado da vida anterior do objeto
            }
        }
        else
        {
            if (animator != null) animator.enabled = false;
            if (spriteRenderer != null && cfg.sprite != null) spriteRenderer.sprite = cfg.sprite;
        }

        // Comportamento
        usaTimer = cfg.comportamento != Comportamento.Voador && cfg.tempoDeVida > 0f;
        tempoRestante = cfg.tempoDeVida;

        switch (cfg.comportamento)
        {
            case Comportamento.Corredor:
                direcao = transform.position.x > 0f ? Vector3.left : Vector3.right;
                break;
            case Comportamento.Voador:
                StartCoroutine(RotinaVoo());
                break;
            case Comportamento.AtacaTela:
                StartCoroutine(RotinaAtaque());
                break;
        }
    }

    void Update()
    {
        if (!ativo || estaMorrendo) return;
        if (!GameManager.Rodando) return;

        if (config.comportamento == Comportamento.Corredor)
        {
            transform.Translate(direcao * config.velocidade * Time.deltaTime);
        }

        if (usaTimer)
        {
            tempoRestante -= Time.deltaTime;
            if (tempoRestante <= 0f) Escapar(true);
        }
    }

    // ===================== COMPORTAMENTOS =====================

    private IEnumerator RotinaVoo()
    {
        Transform[] pontos = config.pontosDeMovimento;
        if (pontos == null || pontos.Length == 0)
        {
            Debug.LogWarning("Inimigo Voador '" + config.nome + "' sem Pontos De Movimento no Catálogo.");
            yield break;
        }

        int indiceAtual = -1;

        for (int ciclo = 0; ciclo < config.ciclosDeVoo && !estaMorrendo; ciclo++)
        {
            while (!GameManager.Rodando) yield return null;

            // Sorteia um ponto diferente do último (a mosca sempre se desloca)
            int indice = Random.Range(0, pontos.Length);
            if (pontos.Length > 1 && indice == indiceAtual) indice = (indice + 1) % pontos.Length;
            indiceAtual = indice;

            if (pontos[indice] == null) continue;

            Vector3 destino = pontos[indice].position;
            destino.z = 0f;
            Vector3 inicio = transform.position;
            float t = 0f;

            while (t < 1f && !estaMorrendo)
            {
                if (GameManager.Rodando)
                {
                    t += Time.deltaTime * 3f;
                    transform.position = Vector3.Lerp(inicio, destino, t);
                }
                yield return null;
            }

            yield return Espera1s;
        }

        // Terminou os saltos sem ser abatida: vai embora sem tirar vida do jogador
        if (!estaMorrendo) Escapar(false);
    }

    private IEnumerator RotinaAtaque()
    {
        yield return Espera1_5s;

        while (!estaMorrendo)
        {
            if (GameManager.Rodando && GameManager.Instance != null)
            {
                if (animator != null && animator.enabled) animator.SetTrigger(HashAtacar);
                GameManager.Instance.SujarTelaComCoco();
            }
            yield return Espera2_5s;
        }
    }

    // ===================== DANO / MORTE / FUGA =====================

    public void TomarDano(int quantidadeDeDano)
    {
        if (!ativo || estaMorrendo) return;

        vida -= quantidadeDeDano;
        if (vida <= 0) StartCoroutine(RotinaMorte());
    }

    private IEnumerator RotinaMorte()
    {
        estaMorrendo = true;

        // O jogador não pode mais clicar no corpo
        if (colisor != null) colisor.enabled = false;

        SpawnerAlvos spawner = SpawnerAlvos.Instance;
        GameManager gm = GameManager.Instance;

        // Baixa no gerenciador de rounds
        if (!EhCura && spawner != null) spawner.InimigoDerrotado();

        // Recompensas
        if (gm != null)
        {
            if (EhCura)
            {
                gm.GanharVida();
            }
            else
            {
                gm.SomarPontos(pontosQueVale);
                gm.SomarMoedas(moedasQueVale);
            }
        }

        if (config.comportamento == Comportamento.SpawnaFilhos) SpawnarFilhos(spawner);

        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.TocarSomMorteInimigo();

        bool temAnimacao = animator != null && animator.enabled;
        if (temAnimacao)
        {
            animator.SetTrigger(HashMorrer);
            yield return EsperaMorte; // tempo da animação de morte
        }

        Devolver();
    }

    private void SpawnarFilhos(SpawnerAlvos spawner)
    {
        if (spawner == null || string.IsNullOrEmpty(config.filho)) return;

        const float larguraBase = 0.8f;
        const float alturaTriangulo = 0.7f;
        Vector3 p = transform.position;

        spawner.SpawnarInimigoEspecifico(config.filho, p + new Vector3(0f, alturaTriangulo / 2f, 0f));
        spawner.SpawnarInimigoEspecifico(config.filho, p + new Vector3(-larguraBase / 2f, -alturaTriangulo / 2f, 0f));
        spawner.SpawnarInimigoEspecifico(config.filho, p + new Vector3(larguraBase / 2f, -alturaTriangulo / 2f, 0f));
    }

    /// <summary>Inimigo sumiu sem ser morto. Substitui a antiga lógica do OnDestroy.</summary>
    private void Escapar(bool tiraVidaDoJogador)
    {
        if (!ativo || estaMorrendo) return;
        ativo = false;

        if (!EhCura)
        {
            if (SpawnerAlvos.Instance != null) SpawnerAlvos.Instance.InimigoDerrotado();
            if (tiraVidaDoJogador && GameManager.Instance != null) GameManager.Instance.PerderVida();
        }

        Devolver();
    }

    private void Devolver()
    {
        ativo = false;
        StopAllCoroutines();

        if (SpawnerAlvos.Instance != null) SpawnerAlvos.Instance.Devolver(this);
        else gameObject.SetActive(false);
    }
}
