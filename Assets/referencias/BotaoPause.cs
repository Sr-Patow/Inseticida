using UnityEngine;
using UnityEngine.Events;

public class BotaoPause : MonoBehaviour
{
    // UnityEvent permite configurar a ação do botão pelo Inspector,
    // sem precisar de um script diferente pra cada botão.
    [SerializeField] private UnityEvent aoClicar;
    
    // Efeito visual opcional: escala o botão quando o mouse passa por cima
    [Header("Efeito Hover")]
    [SerializeField] private float escalaHover = 1.1f;
    
    private Vector3 escalaOriginal;
    
    void Start()
    {
        // Guarda a escala inicial pra restaurar depois do hover
        escalaOriginal = transform.localScale;
    }
    
    // Método mágico da Unity: chamado quando o mouse clica sobre o collider
    void OnMouseDown()
    {
        aoClicar?.Invoke();
    }
    
    // Método mágico: chamado quando o mouse ENTRA na área do collider
    void OnMouseEnter()
    {
        transform.localScale = escalaOriginal * escalaHover;
    }
    
    // Método mágico: chamado quando o mouse SAI da área do collider
    void OnMouseExit()
    {
        transform.localScale = escalaOriginal;
    }
}