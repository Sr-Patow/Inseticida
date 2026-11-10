using UnityEngine;
using TMPro;

public class LojaUpgrades : MonoBehaviour
{
    [Header("Exclusivos: 3 slots fixos (ordem da tela)")]
    [SerializeField] private SlotExclusivo[] slotsExclusivos;
    [Tooltip("Opcional: texto onde aparece a descrição do exclusivo sob o mouse.")]
    [SerializeField] private TextMeshProUGUI textoDescricaoExclusivo;

    [Header("Cumulativos: sorteados a cada abertura")]
    [Tooltip("Arraste os 5 assets cumulativos.")]
    [SerializeField] private UpgradeDados[] cumulativos;
    [Tooltip("Os 2 cartões (caixa de texto + botão) da área da direita.")]
    [SerializeField] private CartaoUpgrade[] cartoesCumulativos;

    private UpgradeDados[] ofertas;      // o que cada cartão está oferecendo
    private bool[] compradoNoSlot;       // cartão já comprado nesta visita
    private UpgradeDados[] candidatos;   // buffer reutilizado, sem alocar a cada sorteio
    private bool iniciada;

    private void Inicializar()
    {
        if (iniciada) return;
        iniciada = true;

        ofertas = new UpgradeDados[cartoesCumulativos.Length];
        compradoNoSlot = new bool[cartoesCumulativos.Length];
        candidatos = new UpgradeDados[cumulativos.Length];

        for (int i = 0; i < slotsExclusivos.Length; i++) slotsExclusivos[i].Configurar(this, i);
        for (int i = 0; i < cartoesCumulativos.Length; i++) cartoesCumulativos[i].Configurar(this, i);
    }

    /// <summary>Chamado pelo Spawner toda vez que a loja abre.</summary>
    public void Abrir()
    {
        Inicializar();
        LimparDescricao();

        // Candidatos: cumulativos que ainda não chegaram no nível máximo
        int total = 0;
        float somaPesos = 0f;
        for (int i = 0; i < cumulativos.Length; i++)
        {
            UpgradeDados u = cumulativos[i];
            if (u == null || u.peso <= 0f || !CumulativoDisponivel(u)) continue;
            candidatos[total++] = u;
            somaPesos += u.peso;
        }

        // Sorteio ponderado sem repetir
        for (int slot = 0; slot < cartoesCumulativos.Length; slot++)
        {
            compradoNoSlot[slot] = false;
            ofertas[slot] = null;
            if (total == 0) continue;

            float r = Random.value * somaPesos;
            int escolhido = total - 1;
            for (int i = 0; i < total; i++)
            {
                r -= candidatos[i].peso;
                if (r <= 0f) { escolhido = i; break; }
            }

            ofertas[slot] = candidatos[escolhido];
            somaPesos -= candidatos[escolhido].peso;
            candidatos[escolhido] = candidatos[--total];
        }

        AtualizarTudo();
    }

    // ===================== COMPRAS =====================

    public void ComprarExclusivo(int indice)
    {
        UpgradeDados u = slotsExclusivos[indice].Dados;
        if (u == null || EstadoExclusivo(u) != EstadoCartao.Disponivel) return;
        if (GameManager.Instance == null) return;

        if (!GameManager.Instance.GastarMoedas(PrecoAtual(u))) return;

        DadosDaPartida.NiveisUpgrade[(int)u.efeito]++;
        AtualizarTudo(); // os outros dois exclusivos ganham o "X"
    }

    public void ComprarCumulativo(int slot)
    {
        UpgradeDados u = ofertas[slot];
        if (u == null || compradoNoSlot[slot] || !CumulativoDisponivel(u)) return;
        if (GameManager.Instance == null) return;

        if (!GameManager.Instance.GastarMoedas(PrecoAtual(u))) return;

        DadosDaPartida.NiveisUpgrade[(int)u.efeito]++;
        compradoNoSlot[slot] = true;
        AtualizarTudo();
    }

    // ===================== ATUALIZAÇÃO DA TELA =====================

    private void AtualizarTudo()
    {
        for (int i = 0; i < slotsExclusivos.Length; i++)
        {
            UpgradeDados u = slotsExclusivos[i].Dados;
            if (u == null) continue;
            slotsExclusivos[i].Mostrar(EstadoExclusivo(u), PrecoAtual(u));
        }

        int moedas = DadosDaPartida.Moedas;
        for (int i = 0; i < cartoesCumulativos.Length; i++)
        {
            UpgradeDados u = ofertas[i];
            if (u == null)
            {
                cartoesCumulativos[i].Esconder();
                continue;
            }

            int preco = PrecoAtual(u);
            EstadoCartao estado;
            if (compradoNoSlot[i]) estado = EstadoCartao.Comprado;
            else if (!CumulativoDisponivel(u)) estado = EstadoCartao.Bloqueado;
            else if (moedas < preco) estado = EstadoCartao.SemMoedas;
            else estado = EstadoCartao.Disponivel;

            cartoesCumulativos[i].Mostrar(u, estado, preco, DadosDaPartida.NivelUpgrade(u.efeito));
        }
    }

    public void MostrarDescricao(string texto)
    {
        if (textoDescricaoExclusivo != null) textoDescricaoExclusivo.text = texto;
    }

    public void LimparDescricao()
    {
        if (textoDescricaoExclusivo != null) textoDescricaoExclusivo.text = string.Empty;
    }

    // ===================== REGRAS =====================

    private EstadoCartao EstadoExclusivo(UpgradeDados u)
    {
        if (DadosDaPartida.NivelUpgrade(u.efeito) > 0) return EstadoCartao.Comprado;     // este foi o escolhido
        if (DadosDaPartida.TemExclusivo) return EstadoCartao.Bloqueado;                  // outro foi escolhido: "X"
        if (DadosDaPartida.Moedas < PrecoAtual(u)) return EstadoCartao.SemMoedas;
        return EstadoCartao.Disponivel;
    }

    private static bool CumulativoDisponivel(UpgradeDados u)
    {
        int max = u.NivelMaximo;
        return max <= 0 || DadosDaPartida.NivelUpgrade(u.efeito) < max;
    }

    private static int PrecoAtual(UpgradeDados u)
    {
        return u.precoBase + DadosDaPartida.NivelUpgrade(u.efeito) * u.incrementoPreco;
    }
}
