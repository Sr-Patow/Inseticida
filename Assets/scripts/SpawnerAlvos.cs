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

    [Header("UI - Sistema de Loja")]
    [SerializeField] private GameObject painelLoja; 
    [SerializeField] private TextMeshProUGUI textoMoedasLoja; 

    [Header("UI - Sistema de Rounds")]
    [SerializeField] private TextMeshProUGUI textoRoundHUD; 
    [SerializeField] private TextMeshProUGUI textoAvisoVidaInimigos; 

    private struct DadosInimigo
    {
        public string nome;
        public int vida;
        public int moedas;
        public int pontos;
    }

    private List<DadosInimigo> listaInimigos = new List<DadosInimigo>();
    private Camera cameraPrincipal;

    // Variáveis de Controle de Ondas/Rounds e Loja
    private int roundAtual = 1;
    private int inimigosVivosNaTela = 0;
    private int vidaExtraInimigosGlobal = 0;
    private bool lojaAberta = false;

    // Listas internas de controle de quantidade para o round ativo
    private int qtdAbelhas, qtdColmeias, qtdLesmas, qtdMoscas, qtdMacacos, qtdCoracoes;

    void Start()
    {
        cameraPrincipal = Camera.main;
        
        // Garante o estado inicial correto das UIs
        if (textoAvisoVidaInimigos != null) textoAvisoVidaInimigos.gameObject.SetActive(false);
        if (painelLoja != null) painelLoja.SetActive(false);
        if (textoMoedasLoja != null) textoMoedasLoja.gameObject.SetActive(false);

        LerArquivoCSV(); 
        AtualizarTextoRound();
        
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

    private IEnumerator RotinaFluxoDeRounds()
    {
        yield return new WaitForSeconds(tempoInicialEspera);

        while (true)
        {
            AtualizarTextoRound();
            CalcularMonstrosDoRound();

            // Chance de 1 em 10 (10%) de aumentar a vida de todos os inimigos neste round
            int sorteio = Random.Range(1, 11);
            if (sorteio == 1)
            {
                vidaExtraInimigosGlobal += 1;
                StartCoroutine(MostrarAvisoVidaExtra());
            }

            // Spawna os inimigos e espera o jogador limpar a tela
            yield return StartCoroutine(SpawnarFilaDeInimigos());

            while (inimigosVivosNaTela > 0)
            {
                yield return null; 
            }

            // ABRIR LOJA A CADA 2 ROUNDS
            if (roundAtual % 2 == 0)
            {
                AbrirLoja();
                
                // Segura o código aqui enquanto a loja estiver aberta
                while (lojaAberta)
                {
                    yield return null; 
                }
            }

            // Avança para o próximo round
            roundAtual++;
            yield return new WaitForSeconds(1.5f); 
        }
    }

    private void CalcularMonstrosDoRound()
    {
        // Round 1 Base
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
        while (qtdAbelhas > 0 || qtdColmeias > 0 || qtdLesmas > 0 || qtdMoscas > 0 || qtdMacacos > 0 || qtdCoracoes > 0)
        {
            if (GameManager.Instance != null && !GameManager.Instance.EstaJogando)
            {
                yield return null;
                continue;
            }

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
        
        int vidaFinal = string.IsNullOrEmpty(dados.nome) ? 3 : dados.vida;
        int moedasFinal = string.IsNullOrEmpty(dados.nome) ? 1 : dados.moedas;
        int pontosFinal = string.IsNullOrEmpty(dados.nome) ? 10 : dados.pontos;

        if (nomeInimigo != "coracao")
        {
            vidaFinal += vidaExtraInimigosGlobal;
            inimigosVivosNaTela++; 
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

    // Gerenciamento da Loja
    private void AbrirLoja()
    {
        lojaAberta = true;
        Time.timeScale = 0f; // Pausa o jogo

        if (painelLoja != null) painelLoja.SetActive(true);
        if (textoMoedasLoja != null) textoMoedasLoja.gameObject.SetActive(true);

        // MUDANÇA AQUI: Avisa o GameManager para voltar o mouse ao padrão do Windows na Loja
        if (GameManager.Instance != null)
        {
            // Usamos o Invoke para chamar a função privada via reflexão de forma simples,
            // ou você pode mudar a função 'RestaurarMousePadrao' para public no GameManager.
            GameManager.Instance.Invoke("RestaurarMousePadrao", 0f);
        }

        AtualizarMoedasLoja();
    }

    public void FecharLoja()
    {
        if (painelLoja != null) painelLoja.SetActive(false);
        if (textoMoedasLoja != null) textoMoedasLoja.gameObject.SetActive(false);
        
        Time.timeScale = 1f; // Despausa o jogo
        lojaAberta = false; 

        // MUDANÇA AQUI: Reativa a mira customizada do jogo ao fechar a loja
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Invoke("AtivarMiraCustomizada", 0f);
        }
    }

    public void AtualizarMoedasLoja()
    {
        if (textoMoedasLoja != null && GameManager.Instance != null)
        {
            // Puxa o valor do seu GameManager (mude para sua variável se não for uma função)
            textoMoedasLoja.text = "Moedas: " + GameManager.Instance.ObterMoedasAtual(); 
        }
    }

    public void InimigoDerrotado()
    {
        inimigosVivosNaTela = Mathf.Max(0, inimigosVivosNaTela - 1);
    }

    private IEnumerator MostrarAvisoVidaExtra()
    {
        if (textoAvisoVidaInimigos != null)
        {
            textoAvisoVidaInimigos.text = "Os inimigos parecem mais fortes...";
            textoAvisoVidaInimigos.gameObject.SetActive(true);
            yield return new WaitForSeconds(3f);
            textoAvisoVidaInimigos.gameObject.SetActive(false);
        }
    }

    private void AtualizarTextoRound()
    {
        if (textoRoundHUD != null)
        {
            textoRoundHUD.text = "Round: " + roundAtual;
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

    // Mantido com o nome corrigido para o script do alvo encontrar
    public void SpawnarInimigoEspecifico(string nomeDoInimigoNoCSV, Vector3 posicao)
    {
        SpawnarPorNome(nomeDoInimigoNoCSV);
    }
}