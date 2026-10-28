using UnityEngine;

public class GerenciadorAudio : MonoBehaviour
{
    public static GerenciadorAudio Instance { get; private set; }

    [Header("Canais de Áudio (Alto-falantes)")]
    [SerializeField] private AudioSource canalMusica;
    [SerializeField] private AudioSource canalEfeitos;

    [Header("Músicas de Fundo (BGM)")]
    [SerializeField] private AudioClip musicaJogoNormal;
    [Tooltip("Opcional: se vazio, a música do dia toca também à noite.")]
    [SerializeField] private AudioClip musicaJogoNoite;
    [SerializeField] private AudioClip musicaMenuPause;
    [SerializeField] private AudioClip musicaGameOver;

    [Header("Efeitos Sonoros (SFX)")]
    [SerializeField] private AudioClip somMerdaNaTela;
    [SerializeField] private AudioClip somTiro;
    [SerializeField] private AudioClip somRecargaJogo;
    [SerializeField] private AudioClip somMorteInimigo;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // mantém o som entre as trocas de cena
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // A cópia duplicada (cena recarregada) não deve tocar nada
        if (Instance != this) return;
        TocarMusica(musicaJogoNormal);
    }

    // ==== MÚSICAS ====
    private void TocarMusica(AudioClip novaMusica)
    {
        if (canalMusica == null || novaMusica == null) return;
        if (canalMusica.clip == novaMusica && canalMusica.isPlaying) return;

        canalMusica.clip = novaMusica;
        canalMusica.loop = true;
        canalMusica.Play();
    }

    public void MudarParaMusicaJogo()
    {
        bool usaNoite = DadosDaPartida.EhNoite && musicaJogoNoite != null;
        TocarMusica(usaNoite ? musicaJogoNoite : musicaJogoNormal);
    }

    public void MudarParaMusicaPause() => TocarMusica(musicaMenuPause);
    public void MudarParaMusicaGameOver() => TocarMusica(musicaGameOver);

    public void PararMusica()
    {
        if (canalMusica != null) canalMusica.Stop();
    }

    // ==== EFEITOS ====
    public void TocarSomTiro() => TocarEfeito(somTiro);
    public void TocarSomMerda() => TocarEfeito(somMerdaNaTela);
    public void TocarSomRecarga() => TocarEfeito(somRecargaJogo);
    public void TocarSomMorteInimigo() => TocarEfeito(somMorteInimigo);

    private void TocarEfeito(AudioClip som)
    {
        if (canalEfeitos != null && som != null) canalEfeitos.PlayOneShot(som);
    }
}
