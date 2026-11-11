using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum EstadoCartao { Disponivel, SemMoedas, Comprado, Bloqueado }

/// <summary>
/// Um dos 2 cartões de upgrade cumulativo (caixa de texto + botão de comprar).
/// Fica dentro da área grande à direita da loja. Só exibe dados; a lógica é do LojaUpgrades.
/// </summary>
public class CartaoUpgrade : MonoBehaviour
{
    [Header("Caixa de texto")]
    [SerializeField] private TextMeshProUGUI textoNome;
    [SerializeField] private TextMeshProUGUI textoDescricao;
    [SerializeField] private TextMeshProUGUI textoNivel;
    [SerializeField] private Image imagemIcone; // opcional

    [Header("Área de preço")]
    [Tooltip("Texto onde aparece o preço (ex.: '20 moedas'). Opcional: se vazio, o preço vai dentro do botão.")]
    [SerializeField] private TextMeshProUGUI textoPreco;
    [Tooltip("Cor do preço quando o jogador não tem moedas suficientes.")]
    [SerializeField] private Color corSemMoedas = Color.red;
    private Color corPrecoNormal = Color.white;

    [Header("Botão de comprar")]
    [SerializeField] private Button botao;
    [SerializeField] private TextMeshProUGUI textoBotao; // mostra o preço / "Comprado"

    private LojaUpgrades loja;
    private int slot;

    public void Configurar(LojaUpgrades lojaDona, int indiceDoSlot)
    {
        loja = lojaDona;
        slot = indiceDoSlot;

        // Se o campo Botao ficou vazio, tenta achar um Button dentro do cartão
        if (botao == null) botao = GetComponentInChildren<Button>(true);
        if (botao == null)
        {
            Debug.LogError("CartaoUpgrade '" + name + "' não tem nenhum componente Button. " +
                           "Adicione um Button ao botão de comprar e arraste no campo Botao.", this);
            return;
        }

        if (textoBotao == null) textoBotao = botao.GetComponentInChildren<TextMeshProUGUI>(true);
        if (textoPreco != null) corPrecoNormal = textoPreco.color;
        botao.onClick.AddListener(AoClicar);
    }

    private void AoClicar()
    {
        loja.ComprarCumulativo(slot);
    }

    public void Esconder()
    {
        gameObject.SetActive(false);
    }

    public void Mostrar(UpgradeDados dados, EstadoCartao estado, int preco, int nivelAtual)
    {
        gameObject.SetActive(true);

        if (textoNome != null) textoNome.text = dados.nome;
        if (textoDescricao != null) textoDescricao.text = dados.descricao;
        if (textoNivel != null) textoNivel.SetText("Nível {0}", nivelAtual);

        if (imagemIcone != null)
        {
            imagemIcone.sprite = dados.icone;
            imagemIcone.enabled = dados.icone != null;
        }

        bool temAreaPreco = textoPreco != null;

        if (temAreaPreco)
        {
            if (estado == EstadoCartao.Comprado) textoPreco.SetText("Comprado");
            else textoPreco.SetText("{0} moedas", preco);

            textoPreco.color = estado == EstadoCartao.SemMoedas ? corSemMoedas : corPrecoNormal;
        }

        if (textoBotao != null)
        {
            if (estado == EstadoCartao.Comprado) textoBotao.SetText("Comprado");
            else if (temAreaPreco) textoBotao.SetText("Comprar");
            else textoBotao.SetText("Comprar ({0})", preco);
        }

        if (botao != null) botao.interactable = estado == EstadoCartao.Disponivel;
    }
}
