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

    private float velocidadeEspecial = 0f;
    private Vector3 direcaoMovimento;

    void Awake()
    {
        // Pega ambos os componentes. Se o inimigo não usar um deles, não tem problema.
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

        while (ciclosRestantes > 0)
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
            while (tempoVoo < 1f)
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

        foiAtingido = true; 
        Destroy(gameObject);
    }

    private IEnumerator RotinaAtaqueMacaco()
    {
        yield return new WaitForSeconds(1.5f);

        while (true)
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
        vida -= quantidadeDeDano;
        if (vida <= 0)
        {
            foiAtingido = true; 
            Morrer();
        }
    }

    private void Morrer()
    {
        // ==== NOVO: CORRIGE O FINDFIRSTOBJECTBYTYPE PARA NOTIFICAR O GERENCIADOR DE ROUNDS ====
        if (nomeInimigo != "coracao")
        {
            SpawnerAlvos spawner = Object.FindFirstObjectByType<SpawnerAlvos>();
            if (spawner != null)
            {
                spawner.InimigoDerrotado();
            }
        }

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
        // Se o inimigo sumir por tempo sem o jogador clicar, ainda sim precisamos dar baixa no Round
        if (!foiAtingido && nomeInimigo != "coracao")
        {
            SpawnerAlvos spawner = Object.FindFirstObjectByType<SpawnerAlvos>();
            if (spawner != null)
            {
                spawner.InimigoDerrotado();
            }
        }

        if (GameManager.Instance != null && GameManager.Instance.EstaJogando && !foiAtingido)
        {
            if (nomeInimigo == "coracao") return;
            GameManager.Instance.PerderVida();
        }
    }
}