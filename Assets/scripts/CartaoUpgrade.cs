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

    [Header("Botão de comprar")]
    [SerializeField] private Button botao;
    [SerializeField] private TextMeshProUGUI textoBotao; // mostra o preço / "Comprado"

    private LojaUpgrades loja;
    private int slot;

    public void Configurar(LojaUpgrades lojaDona, int indiceDoSlot)
    {
        loja = lojaDona;
        slot = indiceDoSlot;
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

        if (textoBotao != null)
        {
            if (estado == EstadoCartao.Comprado) textoBotao.SetText("Comprado");
            else textoBotao.SetText("Comprar ({0})", preco);
        }

        botao.interactable = estado == EstadoCartao.Disponivel;
    }
}
