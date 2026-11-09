using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum EstadoCartao { Disponivel, SemMoedas, Comprado, Bloqueado }

/// <summary>Script do prefab "CartaoUpgrade". Só exibe dados; a lógica fica no LojaUpgrades.</summary>
public class CartaoUpgrade : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textoNome;
    [SerializeField] private TextMeshProUGUI textoDescricao;
    [SerializeField] private TextMeshProUGUI textoPreco;
    [SerializeField] private TextMeshProUGUI textoNivel;
    [SerializeField] private Image imagemIcone;
    [SerializeField] private Button botao;
    [Tooltip("Opcional: objeto (ex.: borda dourada) ligado só nos upgrades exclusivos/raros.")]
    [SerializeField] private GameObject marcadorRaro;

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
        loja.Comprar(slot);
    }

    public void Esconder()
    {
        gameObject.SetActive(false);
    }

    public void Mostrar(UpgradeDados dados, EstadoCartao estado, int preco, int nivelAtual)
    {
        gameObject.SetActive(true);

        textoNome.text = dados.nome;
        textoDescricao.text = dados.descricao;

        if (imagemIcone != null)
        {
            imagemIcone.sprite = dados.icone;
            imagemIcone.enabled = dados.icone != null;
        }

        if (marcadorRaro != null) marcadorRaro.SetActive(dados.tipo == TipoUpgrade.Exclusivo);

        if (textoNivel != null)
        {
            if (dados.tipo == TipoUpgrade.Exclusivo) textoNivel.SetText("Único");
            else textoNivel.SetText("Nível {0}", nivelAtual);
        }

        switch (estado)
        {
            case EstadoCartao.Comprado: textoPreco.SetText("Comprado"); break;
            case EstadoCartao.Bloqueado: textoPreco.SetText("Bloqueado"); break;
            default: textoPreco.SetText("{0} moedas", preco); break;
        }

        botao.interactable = estado == EstadoCartao.Disponivel;
    }
}
