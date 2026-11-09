using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Guarda tudo o que precisa sobreviver à troca de cena (Jogo <-> JogoNoite).
/// É uma classe estática pura: não cria GameObject, não usa DontDestroyOnLoad
/// e não consome memória extra além de alguns inteiros.
///
/// Se a loja criar upgrades (munição máxima, dano, etc.), guarde-os aqui também.
/// </summary>
public static class DadosDaPartida
{
    // ===== Nomes das cenas (precisam estar no Build Settings) =====
    public const string NomeCenaMenu = "MenuPrincipal";
    public const string NomeCenaDia = "Jogo";
    public const string NomeCenaNoite = "JogoNoite";

    /// <summary>A cada quantos rounds a cena troca (dia -> noite -> dia ...).</summary>
    public const int RoundsPorCiclo = 5;

    // ===== Estado da partida =====
    public static bool PartidaEmAndamento;
    public static int Vidas = 3;
    public static int Pontos;
    public static int Moedas;
    public static int Round = 1;
    public static int VidaExtraInimigos;
    public static int Municao = -1; // -1 = ainda não definida (usa munição cheia)

    // ===== Upgrades da loja =====
    // Valores por nível (ajuste aqui para balancear)
    public const float BonusCadenciaPorNivel = 0.25f;  // +25% de velocidade de tiro por nível
    public const float BonusRecargaPorNivel = 0.50f;   // +50% de velocidade de recarga por nível
    public const int BonusVidaPorNivel = 1;
    public const int BonusDanoPorNivel = 1;
    public const int BonusMunicaoPorNivel = 5;

    public static readonly int[] NiveisUpgrade = new int[System.Enum.GetValues(typeof(EfeitoUpgrade)).Length];

    public static int NivelUpgrade(EfeitoUpgrade e) => NiveisUpgrade[(int)e];

    public static bool TiroDuplo => NivelUpgrade(EfeitoUpgrade.TiroDuplo) > 0;
    public static bool TiroTriplo => NivelUpgrade(EfeitoUpgrade.TiroTriplo) > 0;
    public static bool TiroPerfurante => NivelUpgrade(EfeitoUpgrade.TiroPerfurante) > 0;
    public static bool TemExclusivo => TiroDuplo || TiroTriplo || TiroPerfurante;

    public static float MultiplicadorCadencia => 1f + BonusCadenciaPorNivel * NivelUpgrade(EfeitoUpgrade.VelocidadeTiro);
    public static float MultiplicadorRecarga => 1f + BonusRecargaPorNivel * NivelUpgrade(EfeitoUpgrade.VelocidadeRecarga);
    public static int BonusVidaMaxima => BonusVidaPorNivel * NivelUpgrade(EfeitoUpgrade.VidaMaxima);
    public static int BonusDano => BonusDanoPorNivel * NivelUpgrade(EfeitoUpgrade.DanoAumentado);
    public static int BonusMunicao => BonusMunicaoPorNivel * NivelUpgrade(EfeitoUpgrade.MunicaoMaxima);

    public static bool EhNoite => SceneManager.GetActiveScene().name == NomeCenaNoite;
    public static string ProximaCena => EhNoite ? NomeCenaDia : NomeCenaNoite;

    public static void Resetar(int vidasIniciais)
    {
        PartidaEmAndamento = false;
        Vidas = vidasIniciais;
        Pontos = 0;
        Moedas = 0;
        Round = 1;
        VidaExtraInimigos = 0;
        Municao = -1;
        System.Array.Clear(NiveisUpgrade, 0, NiveisUpgrade.Length);
    }

    // Garante estado limpo ao apertar Play (útil se o "Domain Reload" estiver desativado)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void AoIniciar()
    {
        Resetar(3);
    }
}
