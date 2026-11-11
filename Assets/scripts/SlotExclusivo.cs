using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Um dos 3 slots fixos de upgrade exclusivo (tiro duplo, triplo, perfurante).
/// Fica sobre os quadrados escuros da arte da loja.
/// </summary>
public class SlotExclusivo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Upgrade deste slot")]
    [SerializeField] private UpgradeDados dados;

    [Header("UI")]
    [SerializeField] private Button botao;
    [Tooltip("Opcional: se o asset tiver ícone, ele é aplicado aqui automaticamente.")]
    [SerializeField] private Image imagemIcone;
    [Tooltip("O 'X' desenhado: aparece quando outro exclusivo foi comprado.")]
    [SerializeField] private GameObject marcadorX;
    [Tooltip("Opcional: destaque (moldura, check) quando ESTE upgrade foi comprado.")]
    [SerializeField] private GameObject marcadorComprado;
    [Tooltip("Opcional: texto pequeno com o preço.")]
    [SerializeField] private TextMeshProUGUI textoPreco;

    private LojaUpgrades loja;
    private int indice;

    public UpgradeDados Dados => dados;

    public void Configurar(LojaUpgrades lojaDona, int indiceDoSlot)
    {
        loja = lojaDona;
        indice = indiceDoSlot;

        if (botao == null) botao = GetComponent<Button>();
        if (botao == null)
        {
            Debug.LogError("SlotExclusivo '" + name + "' não tem componente Button. " +
                           "Adicione um Button neste objeto e arraste no campo Botao.", this);
            return;
        }

        botao.onClick.AddListener(AoClicar);

        if (imagemIcone != null && dados != null && dados.icone != null) imagemIcone.sprite = dados.icone;
    }

    private void AoClicar()
    {
        loja.ComprarExclusivo(indice);
    }

    public void Mostrar(EstadoCartao estado, int preco)
    {
        if (marcadorX != null) marcadorX.SetActive(estado == EstadoCartao.Bloqueado);
        if (marcadorComprado != null) marcadorComprado.SetActive(estado == EstadoCartao.Comprado);

        if (textoPreco != null)
        {
            bool mostraPreco = estado == EstadoCartao.Disponivel || estado == EstadoCartao.SemMoedas;
            textoPreco.gameObject.SetActive(mostraPreco);
            if (mostraPreco) textoPreco.SetText("{0}", preco);
        }

        if (botao != null) botao.interactable = estado == EstadoCartao.Disponivel;
    }

    // Passar o mouse mostra a descrição (campo opcional no LojaUpgrades)
    public void OnPointerEnter(PointerEventData e)
    {
        if (loja != null && dados != null) loja.MostrarDescricao(dados.descricao);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (loja != null) loja.LimparDescricao();
    }
}
