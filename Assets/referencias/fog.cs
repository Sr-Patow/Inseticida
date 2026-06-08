using UnityEngine;

public class fog : MonoBehaviour
{
    public bool tem_porta = true;
    public GameObject porta;
    void Update()
    {

        if(porta == null)
        {
            tem_porta = false;
        }

        if(tem_porta == false)
        {
            Destroy(gameObject);
        }
    }
}
