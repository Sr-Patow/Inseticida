using UnityEngine;

public enum TipoUpgrade { Exclusivo, Cumulativo }

/// <summary>Cada efeito também é o índice do upgrade em DadosDaPartida.NiveisUpgrade.</summary>
public enum EfeitoUpgrade
{
    TiroDuplo,
    TiroTriplo,
    TiroPerfurante,
    VelocidadeTiro,
    VelocidadeRecarga,
    VidaMaxima,
    DanoAumentado,
    MunicaoMaxima
}

/// <summary>
/// Um asset por upgrade: botão direito na pasta Project > Create > Jogo > Upgrade.
/// </summary>
[CreateAssetMenu(fileName = "NovoUpgrade", menuName = "Jogo/Upgrade")]
public class UpgradeDados : ScriptableObject
{
    [Header("Aparência")]
    public string nome;
    [TextArea] public string descricao;
    public Sprite icone;

    [Header("Efeito")]
    public TipoUpgrade tipo = TipoUpgrade.Cumulativo;
    public EfeitoUpgrade efeito;

    [Header("Preço")]
    public int precoBase = 10;
    [Tooltip("Quanto o preço sobe a cada nível já comprado (cumulativos).")]
    public int incrementoPreco = 5;

    [Header("Limites e Raridade")]
    [Tooltip("Só cumulativos. 0 = sem limite. Exclusivos sempre têm limite 1.")]
    public int nivelMaximo = 0;
    [Tooltip("Só cumulativos: chance relativa de ser sorteado na loja (use 10 em todos para chance igual). Exclusivos têm slot fixo e ignoram o peso.")]
    public float peso = 10f;

    public int NivelMaximo => tipo == TipoUpgrade.Exclusivo ? 1 : nivelMaximo;
}
