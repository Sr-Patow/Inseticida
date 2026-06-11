using UnityEngine;
using System.Collections;

public class alvo : MonoBehaviour
{
    [Header("Atributos do Alvo (Preenchidos pelo CSV)")]
    public string nomeInimigo;
    private int vida; 
    private int pontosQueVale;
    private int moedasQueVale;

    [Header("Tempo de Auto-Destruição Padrão")]
    [SerializeField] private float tempoDeVidaMaximo = 4f; 

    private SpriteRenderer spriteRenderer;
    private Animator animator; 
    private bool foiAtingido = false; 
    private bool estaMorrendo = false; // Garante que a rotina de morte rode apenas uma vez

    private float velocidadeEspecial = 0f;
    private Vector3 direcaoMovimento;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        ConfigurarComportamentosEspeciais();
    }

    private void ConfigurarComportamentosEspeciais()
    {
        if (nomeInimigo == "lesma")
        {
            tempoDeVidaMaximo = 2f; 
            velocidadeEspecial = 12f; 
            
            direcaoMovimento = Vector3.right; 
            if (transform.position.x > 0)
            {
                direcaoMovimento = Vector3.left;
            }
            Destroy(gameObject, tempoDeVidaMaximo);
        }
        else if (nomeInimigo == "mosca")
        {
            StartCoroutine(RotinaInteligenciaMosca());
        }
        else if (nomeInimigo == "macaco")
        {
            tempoDeVidaMaximo = 8f; 
            StartCoroutine(RotinaAtaqueMacaco());
            Destroy(gameObject, tempoDeVidaMaximo);
        }
        else
        {
            Destroy(gameObject, tempoDeVidaMaximo);
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.EstaJogando) return;
        if (estaMorrendo) return; // Trava o movimento se estiver rodando a animação de morte

        if (nomeInimigo == "lesma")
        {
            transform.Translate(direcaoMovimento * velocidadeEspecial * Time.deltaTime);
        }
    }

    private IEnumerator RotinaInteligenciaMosca()
    {
        Camera cam = Camera.main;
        int ciclosRestantes = 5;

        Vector3 posEsquerda = cam.ViewportToWorldPoint(new Vector3(0.2f, 0.6f, cam.nearClipPlane));
        Vector3 posCentro = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, cam.nearClipPlane));
        Vector3 posDireita = cam.ViewportToWorldPoint(new Vector3(0.8f, 0.7f, cam.nearClipPlane));
        Vector3[] pontosPossiveis = { posEsquerda, posCentro, posDireita };

        while (ciclosRestantes > 0 && !estaMorrendo)
        {
            if (GameManager.Instance != null && !GameManager.Instance.EstaJogando)
            {
                yield return null;
                continue;
            }

            Vector3 destinoSorteado = pontosPossiveis[Random.Range(0, pontosPossiveis.Length)];
            destinoSorteado.z = 0f;

            float tempoVoo = 0f;
            Vector3 posicaoInicial = transform.position;
            while (tempoVoo < 1f && !estaMorrendo)
            {
                if (GameManager.Instance != null && GameManager.Instance.EstaJogando)
                {
                    tempoVoo += Time.deltaTime * 3f; 
                    transform.position = Vector3.Lerp(posicaoInicial, destinoSorteado, tempoVoo);
                }
                yield return null;
            }

            yield return new WaitForSeconds(1f);
            ciclosRestantes--;
        }

        if (!estaMorrendo)
        {
            foiAtingido = true; 
            Destroy(gameObject);
        }
    }

    private IEnumerator RotinaAtaqueMacaco()
    {
        yield return new WaitForSeconds(1.5f);

        while (!estaMorrendo)
        {
            if (GameManager.Instance != null && GameManager.Instance.EstaJogando)
            {
                if (animator != null)
                {
                    animator.SetTrigger("atacar");
                }

                GameManager.Instance.SujarTelaComCoco();
            }
            yield return new WaitForSeconds(2.5f); 
        }
    }

    public void InicializarAlvo(string nome, int vidaInicial, int moedas, int pontos, Sprite spriteInimigo)
    {
        nomeInimigo = nome;
        vida = vidaInicial;
        moedasQueVale = moedas;
        pontosQueVale = pontos;

        if (spriteRenderer != null && spriteInimigo != null)
        {
            spriteRenderer.sprite = spriteInimigo;
        }

        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    public void InicializarAlvo(string nome, int vidaInicial, int moedas, int pontos, RuntimeAnimatorController controladorAnimacao)
    {
        transform.name = nome;
        nomeInimigo = nome;
        vida = vidaInicial;
        moedasQueVale = moedas;
        pontosQueVale = pontos;

        if (animator != null && controladorAnimacao != null)
        {
            animator.enabled = true;
            animator.runtimeAnimatorController = controladorAnimacao;
        }
    }

    public void TomarDano(int quantidadeDeDano)
    {
        if (estaMorrendo) return; // Evita tomar dano extra se já estiver morrendo

        vida -= quantidadeDeDano;
        if (vida <= 0)
        {
            foiAtingido = true; 
            StartCoroutine(RotinaMorte());
        }
    }

    // Transformado em Coroutine para suportar o tempo da animação
    private IEnumerator RotinaMorte()
    {
        estaMorrendo = true;

        // 1. Desativa o colisor para o jogador não clicar no corpo do inimigo morto
        Collider2D colisor = GetComponent<Collider2D>();
        if (colisor != null) colisor.enabled = false;

        // 2. Dá baixa no gerenciador de rounds imediatamente
        if (nomeInimigo != "coracao")
        {
            SpawnerAlvos spawner = Object.FindFirstObjectByType<SpawnerAlvos>();
            if (spawner != null)
            {
                spawner.InimigoDerrotado();
            }
        }

        // 3. Distribui as recompensas ao jogador
        if (GameManager.Instance != null)
        {
            if (nomeInimigo == "coracao")
            {
                GameManager.Instance.GanharVida();
            }
            else
            {
                GameManager.Instance.SomarPontos(pontosQueVale);
                GameManager.Instance.SomarMoedas(moedasQueVale);
            }
        }

        if (nomeInimigo == "colmeia") SpawnarAbelhas();

        if (GerenciadorAudio.Instance != null)
        {
            GerenciadorAudio.Instance.TocarSomMorteInimigo();
        }

        // 4. Dispara o gatilho de animação de morte na Unity
        if (animator != null && animator.enabled)
        {
            animator.SetTrigger("morrer");
        }

        // 5. Espera o tempo da animação rodar (0.5 segundos - ajuste se o seu sprite demorar mais)
        yield return new WaitForSeconds(0.5f);

        Destroy(gameObject);
    }

    private void SpawnarAbelhas()
    {
        SpawnerAlvos spawner = Object.FindFirstObjectByType<SpawnerAlvos>();
        if (spawner != null)
        {
            float larguraBase = 0.8f;  
            float alturaTriangulo = 0.7f; 

            Vector3 abelhaTopo = transform.position + new Vector3(0, alturaTriangulo / 2f, 0);
            Vector3 abelhaEsquerda = transform.position + new Vector3(-larguraBase / 2f, -alturaTriangulo / 2f, 0);
            Vector3 abelhaDireita = transform.position + new Vector3(larguraBase / 2f, -alturaTriangulo / 2f, 0);

            spawner.SpawnarInimigoEspecifico("abelha", abelhaTopo);
            spawner.SpawnarInimigoEspecifico("abelha", abelhaEsquerda);
            spawner.SpawnarInimigoEspecifico("abelha", abelhaDireita);
        }
    }

    private void OnDestroy()
    {
        // Se sumiu por tempo (fuga) e NÃO foi atingido nem está morrendo por dano
        if (!foiAtingido && !estaMorrendo && nomeInimigo != "coracao")
        {
            SpawnerAlvos spawner = Object.FindFirstObjectByType<SpawnerAlvos>();
            if (spawner != null)
            {
                spawner.InimigoDerrotado();
            }
        }

        if (GameManager.Instance != null && GameManager.Instance.EstaJogando && !foiAtingido && !estaMorrendo)
        {
            if (nomeInimigo == "coracao") return;
            GameManager.Instance.PerderVida();
        }
    }
}