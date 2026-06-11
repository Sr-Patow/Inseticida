using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class arma : MonoBehaviour
{
    [Header("Configurações da Bala")]
    [SerializeField] private Transform PontaArma;
    [SerializeField] private GameObject bullet;
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

    private Vector2 direcao;
    private float angulo;

    void Start()
    {
        municaoAtual = maxMunicao;
        AtualizarTextoMunicao();
    }

    void Update()
    {
        // Se o jogo não estiver rolando, ignora tudo abaixo
        if (GameManager.Instance == null || !GameManager.Instance.EstaJogando) return;
        if (estaRecarregando) return; 

        // 1. CONTROLE DA MIRA (Roda livremente a cada frame no Update)
        direcao = UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo - 90f);

        // 2. RECARGA MANUAL COM "R" (Agora funciona livremente!)
        if (Input.GetKeyDown(KeyCode.R) && municaoAtual < maxMunicao)
        {
            StartCoroutine(Recarregar());
            return; // Sai do Update para não processar tiro nesse frame
        }

        // 3. CONTROLE DO TIRO
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            // Só atira se a cadência de tiro permitir
            if (Time.time >= proximoTempoTiro)
            {
                if (municaoAtual > 0)
                {
                    FireBullet();
                }
                else
                {
                    StartCoroutine(Recarregar());
                }
            }
        }
    }

    private void FireBullet()
    {
        proximoTempoTiro = Time.time + tempoEntreTiros;
        municaoAtual--; 
        AtualizarTextoMunicao();

        // AUDIO CENTRALIZADO: Toca o som do disparo
        if (GerenciadorAudio.Instance != null)
        {
            GerenciadorAudio.Instance.TocarSomTiro();
        }

        GameObject fireBullet = Instantiate(bullet, PontaArma.position, PontaArma.rotation);
        fireBullet.GetComponent<Rigidbody2D>().linearVelocity = PontaArma.up * velocidadBala;
    }

    private IEnumerator Recarregar()
    {
        estaRecarregando = true;
        AtualizarTextoMunicao();

        yield return new WaitForSeconds(tempoRecarga);

        municaoAtual = maxMunicao; 
        estaRecarregando = false;
        AtualizarTextoMunicao();
    }

    private void AtualizarTextoMunicao()
    {
        if (textoMunicao != null)
        {
            if (estaRecarregando)
            {
                textoMunicao.text = "Recarregando...";
            }
            else
            {
                textoMunicao.text = "Munição: " + municaoAtual + " / " + maxMunicao;
            }
        }
    }
}