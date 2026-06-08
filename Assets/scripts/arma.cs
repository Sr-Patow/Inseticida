using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro; // Garante que o TextMeshPro continue funcionando

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
    [SerializeField] private float tempoEntreTiros = 0.1f; // O tempo de espera (0.5 segundos)
    private float proximoTempoTiro = 0f; // Guarda quando o jogador poderá atirar novamente

    private Vector2 direcao;
    private float angulo;

    void Start()
    {
        municaoAtual = maxMunicao;
        AtualizarTextoMunicao();
    }

    void Update()
    {

        // SE NÃO ESTIVER JOGANDO (Menu, Tutorial, Pause ou Game Over), NÃO FAZ NADA E RETORNA
        if (GameManager.Instance == null || !GameManager.Instance.EstaJogando) 
        {
            return; 
        }

        // Daqui para baixo fica o seu código normal de clique
        if (Input.GetMouseButtonDown(0))
        {
            if (GerenciadorAudio.Instance != null)
            {
                GerenciadorAudio.Instance.TocarSomTiro();
            }
            
        if (GameManager.Instance != null && !GameManager.Instance.EstaJogando) return;
        if (estaRecarregando) return; 

        // Controle da mira
        direcao = UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo - 90f);

        // Recarga manual com R
        if (Input.GetKeyDown(KeyCode.R) && municaoAtual < maxMunicao)
        {
            StartCoroutine(Recarregar());
            return;
        }

        // Controle do tiro
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            // NOVA CHECAGEM: Só atira se o tempo atual do jogo for maior que o 'proximoTempoTiro'
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
            if (GameManager.Instance != null && GameManager.Instance.EstaJogando)
            {
                // AUDIO CENTRALIZADO: Toca o som do disparo
                if (GerenciadorAudio.Instance != null)
                {
                    GerenciadorAudio.Instance.TocarSomTiro();
                }
            }
        }
    }
    }
    private void FireBullet()
    {
        // Define quando será o próximo tiro permitido (Tempo Atual + 0.5 segundos)
        proximoTempoTiro = Time.time + tempoEntreTiros;

        municaoAtual--; 
        AtualizarTextoMunicao();

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