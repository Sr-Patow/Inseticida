using UnityEngine;

public class golpe : MonoBehaviour
{
    public GameObject golpeReal;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, 0.25f);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
