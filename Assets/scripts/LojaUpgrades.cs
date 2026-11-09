using UnityEngine;

public class LojaUpgrades : MonoBehaviour
{
    [Header("Upgrades existentes (arraste os 8 assets)")]
    [SerializeField] private UpgradeDados[] todosUpgrades;

    [Header("UI")]
    [SerializeField] private CartaoUpgrade prefabCartao;
    [Tooltip("Objeto com Horizontal Layout Group dentro do painel da loja.")]
    [SerializeField] private Transform containerCartoes;

    [Header("Configuração")]
    [SerializeField] private int quantidadeNaLoja = 3;

    private CartaoUpgrade[] cartoes;
    private UpgradeDados[] ofertas;
    private bool[] compradoNoSlot;
    private UpgradeDados[] candidatos; // buffer reutilizado, evita alocar a cada sorteio
    private bool iniciada;

    private void Inicializar()
    {
        if (iniciada) return;
        iniciada = true;

        cartoes = new CartaoUpgrade[quantidadeNaLoja];
        ofertas = new UpgradeDados[quantidadeNaLoja];
        compradoNoSlot = new bool[quantidadeNaLoja];
        candidatos = new UpgradeDados[todosUpgrades.Length];

        for (int i = 0; i < quantidadeNaLoja; i++)
        {
            cartoes[i] = Instantiate(prefabCartao, containerCartoes);
            cartoes[i].Configurar(this, i);
        }
    }

    /// <summary>Chamado pelo Spawner toda vez que a loja abre: sorteia novas ofertas.</summary>
    public void Abrir()
    {
        Inicializar();

        // Monta a lista de candidatos que ainda podem aparecer
        int total = 0;
        float somaPesos = 0f;
        for (int i = 0; i < todosUpgrades.Length; i++)
        {
            UpgradeDados u = todosUpgrades[i];
            if (u == null || u.peso <= 0f || !Disponivel(u)) continue;
            candidatos[total++] = u;
            somaPesos += u.peso;
        }

        // Sorteio ponderado sem repetir: exclusivos têm peso menor, logo são mais raros
        for (int slot = 0; slot < quantidadeNaLoja; slot++)
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
            candidatos[escolhido] = candidatos[--total]; // remove do sorteio
        }

        AtualizarCartoes();
    }

    public void Comprar(int slot)
    {
        UpgradeDados u = ofertas[slot];
        if (u == null || compradoNoSlot[slot] || !Disponivel(u)) return;
        if (GameManager.Instance == null) return;

        if (!GameManager.Instance.GastarMoedas(PrecoAtual(u))) return;

        DadosDaPartida.NiveisUpgrade[(int)u.efeito]++;
        compradoNoSlot[slot] = true;

        // Atualiza tudo: bloqueia os outros exclusivos e recalcula quem ainda dá pra pagar
        AtualizarCartoes();
    }

    private void AtualizarCartoes()
    {
        int moedas = DadosDaPartida.Moedas;

        for (int i = 0; i < quantidadeNaLoja; i++)
        {
            UpgradeDados u = ofertas[i];
            if (u == null)
            {
                cartoes[i].Esconder();
                continue;
            }

            int preco = PrecoAtual(u);
            EstadoCartao estado;
            if (compradoNoSlot[i]) estado = EstadoCartao.Comprado;
            else if (!Disponivel(u)) estado = EstadoCartao.Bloqueado;
            else if (moedas < preco) estado = EstadoCartao.SemMoedas;
            else estado = EstadoCartao.Disponivel;

            cartoes[i].Mostrar(u, estado, preco, DadosDaPartida.NivelUpgrade(u.efeito));
        }
    }

    private static bool Disponivel(UpgradeDados u)
    {
        // Os 3 exclusivos formam um grupo: comprou um, os outros ficam bloqueados
        if (u.tipo == TipoUpgrade.Exclusivo) return !DadosDaPartida.TemExclusivo;

        int max = u.NivelMaximo;
        return max <= 0 || DadosDaPartida.NivelUpgrade(u.efeito) < max;
    }

    private static int PrecoAtual(UpgradeDados u)
    {
        return u.precoBase + DadosDaPartida.NivelUpgrade(u.efeito) * u.incrementoPreco;
    }
}
