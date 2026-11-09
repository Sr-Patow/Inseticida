using UnityEngine;
using UnityEngine.Pool;

public class CodigoBala : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D colisor;
    private bool triggerOriginal;

    private ObjectPool<CodigoBala> pool;
    private Rect limites;
    private bool ativa;
    private int dano;
    private bool perfura;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        colisor = GetComponent<Collider2D>();
        if (colisor != null) triggerOriginal = colisor.isTrigger;
    }

    /// <summary>Chamado pela arma toda vez que a bala sai do pool.</summary>
    public void Disparar(ObjectPool<CodigoBala> poolOrigem, Vector2 velocidade, Rect limitesDaTela, int danoDaBala, bool tiroPerfurante)
    {
        pool = poolOrigem;
        limites = limitesDaTela;
        dano = danoDaBala;
        perfura = tiroPerfurante;
        ativa = true;

        // Bala perfurante precisa atravessar o inimigo, então vira trigger (sem colisão física)
        if (colisor != null) colisor.isTrigger = perfura || triggerOriginal;

        rb.linearVelocity = velocidade;
    }

    void Update()
    {
        if (!ativa) return;

        // Saiu da área da tela: volta para o pool
        if (!limites.Contains(transform.position)) Liberar();
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        Atingiu(outro.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        Atingiu(colisao.gameObject);
    }

    private void Atingiu(GameObject obj)
    {
        if (!ativa || !obj.CompareTag("alvo")) return;

        if (obj.TryGetComponent(out alvo scriptDoAlvo)) scriptDoAlvo.TomarDano(dano);

        // Perfurante continua voando; as demais somem no primeiro acerto
        if (!perfura) Liberar();
    }

    private void Liberar()
    {
        if (!ativa) return; // evita devolver duas vezes ao pool
        ativa = false;
        rb.linearVelocity = Vector2.zero;

        if (pool != null) pool.Release(this);
        else gameObject.SetActive(false);
    }
}
