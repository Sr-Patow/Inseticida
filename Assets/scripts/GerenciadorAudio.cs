using UnityEngine;
using System.Collections;

public class GerenciadorAudio : MonoBehaviour
{
    public static GerenciadorAudio Instance { get; private set; }

    [Header("Canais de Áudio (Alto-falantes)")]
    [SerializeField] private AudioSource canalMusica;
    [SerializeField] private AudioSource canalEfeitos;

    [Header("Músicas de Fundo (BGM)")]
    [SerializeField] private AudioClip musicaJogoNormal;
    [SerializeField] private AudioClip musicaMenuPause;
    [SerializeField] private AudioClip musicaGameOver;

    [Header("Efeitos Sonoros (SFX)")]
    [SerializeField] private AudioClip somMerdaNaTela;
    [SerializeField] private AudioClip somTiro;
    [SerializeField] private AudioClip somRecargaJogo;
    [SerializeField] private AudioClip somMorteInimigo;

    void Awake()
    {
        // Garante que só exista um Gerenciador de Áudio no jogo
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Opcional: mantém o som tocando entre trocas de cena
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Começa tocando a música do jogo normal
        TocarMusica(musicaJogoNormal);
    }

    // ==== CONTROLE DE MÚSICAS ====
    private void TocarMusica(AudioClip novaMusica)
    {
        if (canalMusica == null || novaMusica == null) return;

        // Se a música já estiver tocando, não reinicia do zero
        if (canalMusica.clip == novaMusica && canalMusica.isPlaying) return;

        canalMusica.clip = novaMusica;
        canalMusica.loop = true;
        canalMusica.Play();
    }

    public void MudarParaMusicaJogo() => TocarMusica(musicaJogoNormal);
    public void MudarParaMusicaPause() => TocarMusica(musicaMenuPause);
    public void MudarParaMusicaGameOver() => TocarMusica(musicaGameOver);

    public void PararMusica()
    {
        if (canalMusica != null) canalMusica.Stop();
    }

    // ==== CONTROLE DE EFEITOS SONOROS ====
    public void TocarSomTiro() => TocarEfeito(somTiro);
    public void TocarSomMerda() => TocarEfeito(somMerdaNaTela);
    public void TocarSomRecarga() => TocarEfeito(somRecargaJogo);
    public void TocarSomMorteInimigo() => TocarEfeito(somMorteInimigo);

    private void TocarEfeito(AudioClip som)
    {
        if (canalEfeitos != null && som != null)
        {
            canalEfeitos.PlayOneShot(som);
        }
    }
}