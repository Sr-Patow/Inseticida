using UnityEngine;
using UnityEngine.Pool;

public class CodigoBala : MonoBehaviour
{
    private Rigidbody2D rb;
    private ObjectPool<CodigoBala> pool;
    private Rect limites;
    private bool ativa;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>Chamado pela arma toda vez que a bala sai do pool.</summary>
    public void Disparar(ObjectPool<CodigoBala> poolOrigem, Vector2 velocidade, Rect limitesDaTela)
    {
        pool = poolOrigem;
        limites = limitesDaTela;
        ativa = true;
        rb.linearVelocity = velocidade;
    }

    void Update()
    {
        if (!ativa) return;

        // Saiu da área da tela: volta para o pool (substitui o Destroy/OnBecameInvisible)
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

        if (obj.TryGetComponent(out alvo scriptDoAlvo)) scriptDoAlvo.TomarDano(1);

        Liberar();
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
