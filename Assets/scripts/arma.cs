using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using System.Collections;
using TMPro;

public class arma : MonoBehaviour
{
    [Header("Configurações da Bala")]
    [SerializeField] private Transform PontaArma;
    [SerializeField] private GameObject bullet; // prefab com o script CodigoBala
    [SerializeField] private float velocidadBala = 30f;

    [Header("Tiro Múltiplo (upgrades)")]
    [Tooltip("Tiro Duplo: distância entre a bala da frente e a de trás (uma atrás da outra).")]
    [SerializeField] private float distanciaTiroDuplo = 0.3f;
    [Tooltip("Ângulo (graus) entre as balas laterais e a central no Tiro Triplo.")]
    [SerializeField] private float anguloCone = 15f;

    [Header("Sistema de Munição (valores base)")]
    [SerializeField] private int maxMunicao = 15;
    [SerializeField] private float tempoRecarga = 1.5f;
    [SerializeField] private TextMeshProUGUI textoMunicao;
    private int municaoAtual;
    private bool estaRecarregando = false;

    [Header("Cadência de Tiro (valor base)")]
    [SerializeField] private float tempoEntreTiros = 0.1f;
    private float proximoTempoTiro = 0f;

    private Camera cam;
    private ObjectPool<CodigoBala> poolBalas;
    private Rect limitesDasBalas;

    // Valores finais já com os upgrades da loja
    private int MaxMunicaoAtual => maxMunicao + DadosDaPartida.BonusMunicao;
    private int ultimoMaxMunicao;
    private WaitForSeconds esperaRecarga;
    private float ultimoTempoRecarga = -1f;

    void Start()
    {
        cam = Camera.main;

        // Mantém a munição ao trocar de cena (dia <-> noite)
        ultimoMaxMunicao = MaxMunicaoAtual;
        municaoAtual = DadosDaPartida.Municao >= 0
            ? Mathf.Min(DadosDaPartida.Municao, ultimoMaxMunicao)
            : ultimoMaxMunicao;

        // Área em que a bala existe (um pouco maior que a tela). Fora dela, volta pro pool.
        Vector2 min = cam.ViewportToWorldPoint(new Vector3(-0.1f, -0.1f, 0f));
        Vector2 max = cam.ViewportToWorldPoint(new Vector3(1.1f, 1.1f, 0f));
        limitesDasBalas = new Rect(min, max - min);

        poolBalas = new ObjectPool<CodigoBala>(
            createFunc: () => Instantiate(bullet).GetComponent<CodigoBala>(),
            actionOnGet: b => b.gameObject.SetActive(true),
            actionOnRelease: b => b.gameObject.SetActive(false),
            actionOnDestroy: b => { if (b != null) Destroy(b.gameObject); },
            collectionCheck: false,
            defaultCapacity: 30,
            maxSize: 100);

        AtualizarTextoMunicao();
    }

    void Update()
    {
        // Compra de "Munição Máxima" na loja (loja pausa o jogo, então vem antes dos retornos)
        SincronizarMunicaoMaxima();

        if (!GameManager.Rodando || GameManager.Instance == null) return;
        if (Time.timeScale == 0f) return;

        // 1. MIRA (também funciona durante a recarga)
        Vector3 mundo = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direcao = mundo - transform.position;
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo - 90f);

        if (estaRecarregando) return;

        // 2. RECARGA MANUAL
        if (Input.GetKeyDown(KeyCode.R) && municaoAtual < MaxMunicaoAtual)
        {
            StartCoroutine(Recarregar());
            return;
        }

        // 3. TIRO
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (Time.time >= proximoTempoTiro)
            {
                if (municaoAtual > 0) FireBullet();
                else StartCoroutine(Recarregar());
            }
        }
    }

    private void FireBullet()
    {
        proximoTempoTiro = Time.time + tempoEntreTiros / DadosDaPartida.MultiplicadorCadencia;
        municaoAtual--; // um tiro gasta 1 de munição, mesmo com várias balas
        AtualizarTextoMunicao();

        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.TocarSomTiro();

        int dano = 1 + DadosDaPartida.BonusDano;
        bool perfura = DadosDaPartida.TiroPerfurante;

        if (DadosDaPartida.TiroTriplo)
        {
            DispararUma(0f, -anguloCone, dano, false);
            DispararUma(0f, 0f, dano, false);
            DispararUma(0f, anguloCone, dano, false);
        }
        else if (DadosDaPartida.TiroDuplo)
        {
            // Uma bala atrás da outra, na mesma direção e velocidade: não colidem entre si.
            // A munição já foi descontada uma única vez lá em cima.
            DispararUma(0f, 0f, dano, false);
            DispararUma(distanciaTiroDuplo, 0f, dano, false);
        }
        else
        {
            DispararUma(0f, 0f, dano, perfura);
        }
    }

    private void DispararUma(float deslocamentoParaTras, float anguloExtra, int dano, bool perfura)
    {
        Quaternion rotacao = PontaArma.rotation * Quaternion.Euler(0f, 0f, anguloExtra);
        Vector3 posicao = PontaArma.position - PontaArma.up * deslocamentoParaTras;

        CodigoBala b = poolBalas.Get();
        b.transform.SetPositionAndRotation(posicao, rotacao);
        b.Disparar(poolBalas, (Vector2)(rotacao * Vector3.up) * velocidadBala, limitesDasBalas, dano, perfura);
    }

    private IEnumerator Recarregar()
    {
        estaRecarregando = true;
        AtualizarTextoMunicao();

        yield return ObterEsperaRecarga();

        municaoAtual = MaxMunicaoAtual;
        estaRecarregando = false;
        AtualizarTextoMunicao();
    }

    // Só recria o WaitForSeconds quando o upgrade de recarga muda
    private WaitForSeconds ObterEsperaRecarga()
    {
        float tempo = tempoRecarga / DadosDaPartida.MultiplicadorRecarga;
        if (esperaRecarga == null || !Mathf.Approximately(tempo, ultimoTempoRecarga))
        {
            esperaRecarga = new WaitForSeconds(tempo);
            ultimoTempoRecarga = tempo;
        }
        return esperaRecarga;
    }

    private void SincronizarMunicaoMaxima()
    {
        int max = MaxMunicaoAtual;
        if (max == ultimoMaxMunicao) return;

        // Ao comprar, as balas novas já vêm cheias
        if (max > ultimoMaxMunicao) municaoAtual += max - ultimoMaxMunicao;
        municaoAtual = Mathf.Min(municaoAtual, max);
        ultimoMaxMunicao = max;
        AtualizarTextoMunicao();
    }

    private void AtualizarTextoMunicao()
    {
        // Se trocar de cena no meio da recarga, ela é considerada concluída
        DadosDaPartida.Municao = estaRecarregando ? MaxMunicaoAtual : municaoAtual;

        if (textoMunicao == null) return;

        if (estaRecarregando) textoMunicao.SetText("Recarregando...");
        else textoMunicao.SetText("Munição: {0} / {1}", municaoAtual, MaxMunicaoAtual);
    }
}
