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

    [Header("Sistema de Munição")]
    [SerializeField] private int maxMunicao = 15;
    [SerializeField] private float tempoRecarga = 1.5f;
    [SerializeField] private TextMeshProUGUI textoMunicao;
    private int municaoAtual;
    private bool estaRecarregando = false;

    [Header("Cadência de Tiro")]
    [SerializeField] private float tempoEntreTiros = 0.1f;
    private float proximoTempoTiro = 0f;

    private Camera cam;
    private ObjectPool<CodigoBala> poolBalas;
    private Rect limitesDasBalas;
    private WaitForSeconds esperaRecarga;

    void Start()
    {
        cam = Camera.main;
        esperaRecarga = new WaitForSeconds(tempoRecarga);

        // Mantém a munição ao trocar de cena (dia <-> noite)
        municaoAtual = DadosDaPartida.Municao >= 0
            ? Mathf.Min(DadosDaPartida.Municao, maxMunicao)
            : maxMunicao;

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
            defaultCapacity: 20,
            maxSize: 50);

        AtualizarTextoMunicao();
    }

    void Update()
    {
        // Ignora tudo se o jogo não está rodando ou está pausado/na loja
        if (!GameManager.Rodando || GameManager.Instance == null) return;
        if (Time.timeScale == 0f) return;

        // 1. MIRA (também funciona durante a recarga)
        Vector3 mundo = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direcao = mundo - transform.position;
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo - 90f);

        if (estaRecarregando) return;

        // 2. RECARGA MANUAL
        if (Input.GetKeyDown(KeyCode.R) && municaoAtual < maxMunicao)
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
        proximoTempoTiro = Time.time + tempoEntreTiros;
        municaoAtual--;
        AtualizarTextoMunicao();

        if (GerenciadorAudio.Instance != null) GerenciadorAudio.Instance.TocarSomTiro();

        CodigoBala b = poolBalas.Get();
        b.transform.SetPositionAndRotation(PontaArma.position, PontaArma.rotation);
        b.Disparar(poolBalas, PontaArma.up * velocidadBala, limitesDasBalas);
    }

    private IEnumerator Recarregar()
    {
        estaRecarregando = true;
        AtualizarTextoMunicao();

        yield return esperaRecarga;

        municaoAtual = maxMunicao;
        estaRecarregando = false;
        AtualizarTextoMunicao();
    }

    private void AtualizarTextoMunicao()
    {
        // Se trocar de cena no meio da recarga, ela é considerada concluída
        DadosDaPartida.Municao = estaRecarregando ? maxMunicao : municaoAtual;

        if (textoMunicao == null) return;

        if (estaRecarregando) textoMunicao.SetText("Recarregando...");
        else textoMunicao.SetText("Munição: {0} / {1}", municaoAtual, maxMunicao);
    }
}
