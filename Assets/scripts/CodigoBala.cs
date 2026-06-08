using UnityEngine;

public class CodigoBala : MonoBehaviour
{
    private void OnBecameInvisible()
    {
        Destroy(gameObject); 
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.CompareTag("alvo"))
        {
            alvo scriptDoAlvo = outro.GetComponent<alvo>();
            
            if (scriptDoAlvo != null)
            {
                scriptDoAlvo.TomarDano(1);
            }

            Destroy(gameObject); 
        }
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        if (colisao.gameObject.CompareTag("alvo"))
        {
            alvo scriptDoAlvo = colisao.gameObject.GetComponent<alvo>();
            
            if (scriptDoAlvo != null)
            {
                scriptDoAlvo.TomarDano(1);
            }

            Destroy(gameObject); 
        }
    }
}