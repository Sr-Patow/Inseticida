using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    private struct DadosInimigo
    {
        public string nome;
        public int vida;
        public int moedas;
        public int pontos;
    }

    private List<DadosInimigo> listaInimigos = new List<DadosInimigo>();
    private Camera cameraPrincipal;
    private int contadorInimigosSpawnados = 0;

    void Start()
    {
        cameraPrincipal = Camera.main;
        LerArquivoCSV(); 
        StartCoroutine(RotinaSpawn());
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

    private IEnumerator RotinaSpawn()
    {
        yield return new WaitForSeconds(tempoInicialEspera);
        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.EstaJogando && listaInimigos.Count > 0)
            {
                contadorInimigosSpawnados++;

                if (contadorInimigosSpawnados % 20 == 0)
                {
                    SpawnarMacacoFixo();
                }
                else
                {
                    SpawnarAlvoAleatorio();
                }
            }
            yield return new WaitForSeconds(intervaloSpawn);
        }
    }

    private void SpawnarAlvoAleatorio()
    {
        if (cameraPrincipal == null || prefabAlvo == null) return;

        Vector3 limiteEsquerdo = cameraPrincipal.ViewportToWorldPoint(new Vector3(0.1f, 1f, cameraPrincipal.nearClipPlane));
        Vector3 limiteDireito = cameraPrincipal.ViewportToWorldPoint(new Vector3(0.9f, 1f, cameraPrincipal.nearClipPlane));
        Vector3 limiteSuperior = cameraPrincipal.ViewportToWorldPoint(new Vector3(0.5f, 1f, cameraPrincipal.nearClipPlane));
        float posicaoXAleatoria = Random.Range(limiteEsquerdo.x, limiteDireito.x);
        float posicaoY = limiteSuperior.y - recuoBordaSuperior;
        Vector3 posicaoSpawn = new Vector3(posicaoXAleatoria, posicaoY, 0f);

        GameObject novoAlvoObjeto = Instantiate(prefabAlvo, posicaoSpawn, Quaternion.identity);
        alvo scriptAlvo = novoAlvoObjeto.GetComponent<alvo>();

        if (scriptAlvo != null)
        {
            int indiceSorteado = Random.Range(0, listaInimigos.Count);
            DadosInimigo dadosSorteados = listaInimigos[indiceSorteado];

            // Se for colmeia OU coracao, inicializa como Sprite fixo
            if (dadosSorteados.nome == "colmeia")
            {
                scriptAlvo.InicializarAlvo(dadosSorteados.nome, dadosSorteados.vida, dadosSorteados.moedas, dadosSorteados.pontos, spriteColmeia);
            }
            else if (dadosSorteados.nome == "coracao")
            {
                scriptAlvo.InicializarAlvo(dadosSorteados.nome, dadosSorteados.vida, dadosSorteados.moedas, dadosSorteados.pontos, spriteCoracao);
            }
            else
            {
                RuntimeAnimatorController animCorrespondente = RetornarAnimatorPorNome(dadosSorteados.nome);
                scriptAlvo.InicializarAlvo(dadosSorteados.nome, dadosSorteados.vida, dadosSorteados.moedas, dadosSorteados.pontos, animCorrespondente);
            }
        }
    }

    private void SpawnarMacacoFixo()
    {
        Vector3 pontoFixoMacaco = cameraPrincipal.ViewportToWorldPoint(new Vector3(0.5f, 0.8f, cameraPrincipal.nearClipPlane));
        pontoFixoMacaco.z = 0f;

        GameObject novoAlvoObjeto = Instantiate(prefabAlvo, pontoFixoMacaco, Quaternion.identity);
        alvo scriptAlvo = novoAlvoObjeto.GetComponent<alvo>();

        if (scriptAlvo != null)
        {
            DadosInimigo dadosMacaco = listaInimigos.Find(x => x.nome == "macaco");
            
            int vidaMacaco = string.IsNullOrEmpty(dadosMacaco.nome) ? 5 : dadosMacaco.vida;
            int moedasMacaco = string.IsNullOrEmpty(dadosMacaco.nome) ? 5 : dadosMacaco.moedas;
            int pontosMacaco = string.IsNullOrEmpty(dadosMacaco.nome) ? 50 : dadosMacaco.pontos;

            scriptAlvo.InicializarAlvo("macaco", vidaMacaco, moedasMacaco, pontosMacaco, animMacaco);
        }
    }

    public void SpawnarInimigoEspecifico(string nomeDoInimigoNoCSV, Vector3 posicao)
    {
        if (prefabAlvo == null || listaInimigos.Count == 0) return;

        DadosInimigo dadosInimigo = listaInimigos.Find(x => x.nome == nomeDoInimigoNoCSV);
        if (string.IsNullOrEmpty(dadosInimigo.nome)) return;

        GameObject novoAlvoObjeto = Instantiate(prefabAlvo, posicao, Quaternion.identity);
        alvo scriptAlvo = novoAlvoObjeto.GetComponent<alvo>();

        if (scriptAlvo != null)
        {
            if (nomeDoInimigoNoCSV == "colmeia")
            {
                scriptAlvo.InicializarAlvo(dadosInimigo.nome, dadosInimigo.vida, dadosInimigo.moedas, dadosInimigo.pontos, spriteColmeia);
            }
            else
            {
                RuntimeAnimatorController animEspecial = RetornarAnimatorPorNome(nomeDoInimigoNoCSV);
                scriptAlvo.InicializarAlvo(dadosInimigo.nome, dadosInimigo.vida, dadosInimigo.moedas, dadosInimigo.pontos, animEspecial);
            }
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
}