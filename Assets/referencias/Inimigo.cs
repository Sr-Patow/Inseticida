using UnityEngine;

public class Inimigo : MonoBehaviour
{
    public GameObject Player;
    private bool deteccao;
    private float distanciaP = 10f;
    private Rigidbody2D rb;
    private bool colidiu = false;
    private int delay = 120;
    private int vida = 100;

    void Start()
    {
        rb = GetComponent <Rigidbody2D> ();
    }

    void Update()
    {
        // Calcula a distância entre os dois objetos
        float distancia = Vector3.Distance(Player.transform.position, transform.position);
        if(distancia < distanciaP)
        {
            if(colidiu == false)
            {
                if(transform.position.x > Player.transform.position.x)
             {
                rb.linearVelocity = new Vector2(-2.5f, rb.linearVelocity.y);
             }
                else
             {
                rb.linearVelocity = new Vector2(2.5f, rb.linearVelocity.y);
             }

            }
 

        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if(delay > 0 )
        {
            delay--;
        }

        if(delay == 0)
        {
            colidiu = false;
        }

        if(vida <= 0)
        {
            Destroy(gameObject);
        }
        //Debug.Log(transform.position.x + " " + Player.transform.position.x);

    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {

            colidiu = true;
            Vector2 v = rb.linearVelocity;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * -3, rb.linearVelocity.y);
            delay = 120;
        }

        if(collision.gameObject.CompareTag("golpe"))
        {
            vida -= 25;
            Destroy(collision.gameObject);
        }
    }

}
