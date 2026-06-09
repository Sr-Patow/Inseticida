using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SpawnerAlvos : MonoBehaviour
{
    [Header("Configurações do Prefab")]
    [SerializeField] private GameObject prefabAlvo; 

    [Header("Arquivo de Dados (.CSV)")]
    [SerializeField] private TextAsset arquivoCsv; 

    [Header("Artes Estáticas (Sprites)")]
    [SerializeField] private Sprite spriteColmeia;
    [SerializeField] private Sprite spriteCoracao; 

    [Header("Controladores de Animação (Animators)")]
    [SerializeField] private RuntimeAnimatorController animAbelha;
    [SerializeField] private RuntimeAnimatorController animLesma;
    [SerializeField] private RuntimeAnimatorController animMosca;
    [SerializeField] private RuntimeAnimatorController animMacaco;

    [Header("Tempo de Spawn")]
    [SerializeField] private float tempoInicialEspera = 2f; 
    [SerializeField] private float intervaloSpawn = 1.5f;   
    [SerializeField] private float recuoBordaSuperior = 1f; 

    // ==== NOVOS CAMPOS: INTERFACE DE TEXTO PARA OS ROUNDS ====
    [Header("UI do Sistema de Rounds")]
    [SerializeField] private TextMeshProUGUI textoRoundHUD; // Mostra o round atual
    [SerializeField] private TextMeshProUGUI textoAvisoVidaInimigos; // Alerta de modificador de vida

    private struct DadosInimigo
    {
        public string nome;
        public int vida;
        public int moedas;
        public int pontos;
    }

    private List<DadosInimigo> listaInimigos = new List<DadosInimigo>();
    private Camera cameraPrincipal;

    // Variaveis de Controle de Ondas/Rounds
    private int roundAtual = 1;
    private int inimigosVivosNaTela = 0;
    private int vidaExtraInimigosGlobal = 0;

    // Listas internas de controle de quantidade para o round ativo
    private int qtdAbelhas, qtdColmeias, qtdLesmas, qtdMoscas, qtdMacacos, qtdCoracoes;

    void Start()
    {
        cameraPrincipal = Camera.main;
        
        if (textoAvisoVidaInimigos != null) 
            textoAvisoVidaInimigos.gameObject.SetActive(false);

        LerArquivoCSV(); 
        AtualizarTextoRound();
        
        // Iniciamos a nova rotina baseada em ondas sequenciais
        StartCoroutine(RotinaFluxoDeRounds());
    }

    private void LerArquivoCSV()
    {
        if (arquivoCsv == null) return;

        string[] linhas = arquivoCsv.text.Split('\n');

        for (int i = 1; i < linhas.Length; i++)
        {
            string cleanLine = linhas[i].Trim();
            if (string.IsNullOrEmpty(cleanLine)) continue;

            string[] colunas = cleanLine.Split(',');

            if (colunas.Length < 4) continue;

            DadosInimigo novoInimigo = new DadosInimigo();
            novoInimigo.nome = colunas[0].ToLower().Trim();   
            novoInimigo.vida = int.Parse(colunas[1]);   
            novoInimigo.pontos = int.Parse(colunas[2]); 
            novoInimigo.moedas = int.Parse(colunas[3]); 

            listaInimigos.Add(novoInimigo);
        }
    }

    // ==== REESTRUTURAÇÃO DA ROTINA PARA COMPORTAMENTO DE ROUNDS ====
    private IEnumerator RotinaFluxoDeRounds()
    {
        yield return new WaitForSeconds(tempoInicialEspera);

        while (true)
        {
            AtualizarTextoRound();

            // 1. Calcula as quantidades exatas baseadas nas suas regras de progressão
            CalcularMonstrosDoRound();

            // 2. Chance de 1 em 10 (10%) de aumentar a vida de todos os inimigos neste round
            int sorteio = Random.Range(1, 11);
            if (sorteio == 1)
            {
                vidaExtraInimigosGlobal += 1;
                StartCoroutine(MostrarAvisoVidaExtra());
            }

            // 3. Executa a criação sequencial dos elementos calculados
            yield return StartCoroutine(SpawnarFilaDeInimigos());

            // 4. Aguarda até o jogador limpar todos os alvos válidos da tela
            while (inimigosVivosNaTela > 0)
            {
                yield return null; 
            }

            // Passa para o próximo round
            roundAtual++;
            yield return new WaitForSeconds(2f); // Pequena folga de respiro entre um round e outro
        }
    }

    private void CalcularMonstrosDoRound()
    {
        // Round 1 Base: 0 abelhas, 1 colmeia, 0 lesma, 2 moscas, 1 macaco, 1 coracao
        qtdAbelhas = 0;
        qtdColmeias = 1;
        qtdLesmas = 0;
        qtdMoscas = 2;
        qtdMacacos = 1;
        qtdCoracoes = 1;

        if (roundAtual > 1)
        {
            int roundsPassados = roundAtual - 1;

            // A cada round: aumenta uma mosca
            qtdMoscas += roundsPassados;

            // A cada dois rounds: aumenta 1 coração, 1 colmeia e 1 abelha
            int bonusDeDois = roundsPassados / 2;
            qtdCoracoes += bonusDeDois;
            qtdColmeias += bonusDeDois;
            qtdAbelhas += bonusDeDois;

            // A cada três rounds: aumenta 1 lesma
            int bonusDeTres = roundsPassados / 3;
            qtdLesmas += bonusDeTres;

            // A cada cinco rounds: aumenta 1 macaco
            int bonusDeCinco = roundsPassados / 5;
            qtdMacacos += bonusDeCinco;
        }
    }

    private IEnumerator SpawnarFilaDeInimigos()
    {
        // Continua rodando enquanto houver qualquer criatura ou item na lista de pendências
        while (qtdAbelhas > 0 || qtdColmeias > 0 || qtdLesmas > 0 || qtdMoscas > 0 || qtdMacacos > 0 || qtdCoracoes > 0)
        {
            if (GameManager.Instance != null && !GameManager.Instance.EstaJogando)
            {
                yield return null;
                continue;
            }

            // Sorteia uma categoria disponível para instanciar
            int escolha = Random.Range(0, 6);

            if (escolha == 0 && qtdAbelhas > 0) { SpawnarPorNome("abelha"); qtdAbelhas--; }
            else if (escolha == 1 && qtdColmeias > 0) { SpawnarPorNome("colmeia"); qtdColmeias--; }
            else if (escolha == 2 && qtdLesmas > 0) { SpawnarPorNome("lesma"); qtdLesmas--; }
            else if (escolha == 3 && qtdMoscas > 0) { SpawnarPorNome("mosca"); qtdMoscas--; }
            else if (escolha == 4 && qtdMacacos > 0) { SpawnarPorNome("macaco"); qtdMacacos--; }
            else if (escolha == 5 && qtdCoracoes > 0) { SpawnarPorNome("coracao"); qtdCoracoes--; }

            yield return new WaitForSeconds(intervaloSpawn);
        }
    }

    private void SpawnarPorNome(string nomeInimigo)
    {
        if (cameraPrincipal == null || prefabAlvo == null || listaInimigos.Count == 0) return;

        // Se for macaco, usamos a lógica de posicionamento central fixado que você já possuía
        Vector3 posicaoSpawn;
        if (nomeInimigo == "macaco")
        {
            posicaoSpawn = cameraPrincipal.ViewportToWorldPoint(new Vector3(0.5f, 0.8f, cameraPrincipal.nearClipPlane));
            posicaoSpawn.z = 0f;
        }
        else
        {
            Vector3 limiteEsquerdo = cameraPrincipal.ViewportToWorldPoint(new Vector3(0.1f, 1f, cameraPrincipal.nearClipPlane));
            Vector3 limiteDireito = cameraPrincipal.ViewportToWorldPoint(new Vector3(0.9f, 1f, cameraPrincipal.nearClipPlane));
            Vector3 limiteSuperior = cameraPrincipal.ViewportToWorldPoint(new Vector3(0.5f, 1f, cameraPrincipal.nearClipPlane));
            float posicaoXAleatoria = Random.Range(limiteEsquerdo.x, limiteDireito.x);
            float posicaoY = limiteSuperior.y - recuoBordaSuperior;
            posicaoSpawn = new Vector3(posicaoXAleatoria, posicaoY, 0f);
        }

        DadosInimigo dados = listaInimigos.Find(x => x.nome == nomeInimigo);
        
        // Se não achar o nome no CSV por erro de digitação, define valores de contingência
        int vidaFinal = string.IsNullOrEmpty(dados.nome) ? 3 : dados.vida;
        int moedasFinal = string.IsNullOrEmpty(dados.nome) ? 1 : dados.moedas;
        int pontosFinal = string.IsNullOrEmpty(dados.nome) ? 10 : dados.pontos;

        // SE NÃO FOR CORAÇÃO, APLICA O MODIFICADOR DE VIDA EXTRA ADQUIRIDO NOS SORTEIOS
        if (nomeInimigo != "coracao")
        {
            vidaFinal += vidaExtraInimigosGlobal;
            inimigosVivosNaTela++; // Incrementa contador de alvos ativos na cena
        }

        GameObject novoAlvoObjeto = Instantiate(prefabAlvo, posicaoSpawn, Quaternion.identity);
        alvo scriptAlvo = novoAlvoObjeto.GetComponent<alvo>();

        if (scriptAlvo != null)
        {
            if (nomeInimigo == "colmeia")
            {
                scriptAlvo.InicializarAlvo(nomeInimigo, vidaFinal, moedasFinal, pontosFinal, spriteColmeia);
            }
            else if (nomeInimigo == "coracao")
            {
                scriptAlvo.InicializarAlvo(nomeInimigo, vidaFinal, moedasFinal, pontosFinal, spriteCoracao);
            }
            else
            {
                RuntimeAnimatorController animCorrespondente = RetornarAnimatorPorNome(nomeInimigo);
                scriptAlvo.InicializarAlvo(nomeInimigo, vidaFinal, moedasFinal, pontosFinal, animCorrespondente);
            }
        }
    }

    // MÉTODOS DE SUPORTE EXTERNOS
    public void InimigoDerrotado()
    {
        inimigosVivosNaTela = Mathf.Max(0, inimigosVivosNaTela - 1);
    }

    private IEnumerator MostrarAvisoVidaExtra()
    {
        if (textoAvisoVidaInimigos != null)
        {
            textoAvisoVidaInimigos.text = "OS INIMIGOS GANHARAM +1 DE VIDA NESTE ROUND!";
            textoAvisoVidaInimigos.gameObject.SetActive(true);
            yield return new WaitForSeconds(3f);
            textoAvisoVidaInimigos.gameObject.SetActive(false);
        }
    }

    private void AtualizarTextoRound()
    {
        if (textoRoundHUD != null)
        {
            textoRoundHUD.text = "ROUND: " + roundAtual;
        }
    }

    private RuntimeAnimatorController RetornarAnimatorPorNome(string nome)
    {
        switch (nome)
        {
            case "abelha": return animAbelha;
            case "lesma": return animLesma;
            case "mosca": return animMosca;
            case "macaco": return animMacaco;
            default: return null;
        }
    }

    // Mantido para compatibilidade se algum outro script chamava externamente
    public void SpawnarInimigoEspecifico(string nomeDoInimigoNoCSV, Vector3 posicao)
    {
        SpawnarPorNome(nomeDoInimigoNoCSV);
    }
}