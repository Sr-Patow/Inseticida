using UnityEngine;

/// <summary>Comportamentos possíveis de um inimigo. Novos inimigos reaproveitam um destes.</summary>
public enum Comportamento
{
    Padrao,        // fica parado e some após "tempoDeVida" (ex.: abelha, colmeia)
    Corredor,      // cruza a tela na horizontal (ex.: lesma)
    Voador,        // pula entre pontos da tela (ex.: mosca)
    AtacaTela,     // ataca suja a tela periodicamente (ex.: macaco)
    SpawnaFilhos,  // ao morrer cria 3 inimigos "filho" (ex.: colmeia -> abelhas)
    Cura           // dá vida ao jogador, não conta no round (ex.: coração)
}

/// <summary>Parte visual/comportamental de cada inimigo (preenchida no Inspector do Spawner).</summary>
[System.Serializable]
public class ConfigInimigo
{
    [Tooltip("Mesmo nome usado na primeira coluna do CSV (minúsculo).")]
    public string nome;
    public Comportamento comportamento = Comportamento.Padrao;

    [Tooltip("Use Sprite OU Animator. Se tiver Animator, o sprite é ignorado.")]
    public Sprite sprite;
    public RuntimeAnimatorController animator;

    [Tooltip("Segundos até fugir (perde vida do jogador). Ignorado no Voador.")]
    public float tempoDeVida = 4f;
    [Tooltip("Só para Corredor.")]
    public float velocidade = 12f;
    [Tooltip("Só para Voador: quantos saltos antes de ir embora.")]
    public int ciclosDeVoo = 5;
    [Tooltip("Só para SpawnaFilhos: nome do inimigo criado ao morrer.")]
    public string filho;

    [Header("Spawn")]
    [Tooltip("Marcado: nasce em posição aleatória no topo da tela, ignorando os objetos vazios.")]
    public bool spawnAleatorio = false;
    [Tooltip("Pontos de spawn exclusivos deste inimigo. Vazio = usa os pontos globais do Spawner.")]
    public Transform[] pontosDeSpawn;

    [Header("Movimento (Voador)")]
    [Tooltip("Objetos vazios entre os quais o inimigo voa (ex.: os 3 pontos da mosca).")]
    public Transform[] pontosDeMovimento;
}

/// <summary>Linha do CSV: nome,vida,pontos,moedas[,qtdInicial,crescACadaNRounds]</summary>
public struct DadosInimigo
{
    public string nome;
    public int vida;
    public int pontos;
    public int moedas;
    public int qtdInicial;   // quantidade no round 1
    public int crescACada;   // +1 a cada N rounds (0 = nunca cresce)
}
