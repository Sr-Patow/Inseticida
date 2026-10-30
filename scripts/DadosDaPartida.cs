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
    }

    // Garante estado limpo ao apertar Play (útil se o "Domain Reload" estiver desativado)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void AoIniciar()
    {
        Resetar(3);
    }
}
