using UnityEngine;

public class ControleCamera : MonoBehaviour
{
    [SerializeField] private Transform target; // Arraste o Player aqui no Inspector
    [SerializeField] private Vector3 offset = new Vector3(0, 0, 0); // Distância da câmera
    [SerializeField] private float smoothTime = 0.25f; // Suavidade
    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target != null)
        {
            // Move a câmera suavemente para a posição do alvo + offset
            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}
