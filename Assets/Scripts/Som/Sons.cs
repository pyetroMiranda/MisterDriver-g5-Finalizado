using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sistema de som do jogo. Liga sozinho ao abrir qualquer cena (não precisa colocar nada na cena):
// - carrega o BancoSons (Resources) e aplica os volumes salvos no menu Opções;
// - toca a música de cada cena, trocando com suavidade (e a de perseguição na volta do mercado);
// - coloca o SomCarro no carro do jogador e o SomBotao em todos os botões;
// - toca efeitos pelo id: Sons.Tocar("Moeda") ou Sons.TocarEm("Batida", posicao) (mais baixo quando longe da câmera).
public static class Sons
{
    private const string CaminhoBanco = "BancoSons";
    public const string ChaveMusica = "VolumeMusica";   // mesmos nomes do VolumesJogo (PlayerPrefs e mixer)
    public const string ChaveEfeitos = "VolumeSFX";

    private static BancoSons banco;
    private static readonly Dictionary<string, BancoSons.Efeito> mapa = new Dictionary<string, BancoSons.Efeito>();
    private static SistemaSom sistema;
    private static bool tentouCarregar;

    public static BancoSons Banco => Garantir() ? banco : null;
    public static AudioMixerGroup GrupoEfeitos => Garantir() ? banco.grupoEfeitos : null;

    // ---------- Começo ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Iniciar()
    {
        tentouCarregar = false;
        if (!Garantir()) return;
        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;
        Loja.AoComprar -= AoComprar;
        Loja.AoComprar += AoComprar;
        AoCarregarCena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static bool Garantir()
    {
        if (sistema != null) return true;
        if (banco == null)
        {
            if (tentouCarregar) return false;
            tentouCarregar = true;
            banco = Resources.Load<BancoSons>(CaminhoBanco);
            if (banco == null)
            {
                Debug.Log("Sons: ainda não há sons. Rode Mr Driver > Sons > Gerar sons e músicas.");
                return false;
            }
        }
        mapa.Clear();
        foreach (BancoSons.Efeito e in banco.efeitos)
            if (e != null && !string.IsNullOrEmpty(e.id) && e.clip != null) mapa[e.id] = e;

        GameObject obj = new GameObject("SistemaSom");
        Object.DontDestroyOnLoad(obj);
        sistema = obj.AddComponent<SistemaSom>();
        sistema.Preparar(banco);
        return true;
    }

    private static void AoComprar(ItemLoja item) => Tocar("Comprar");

    private static void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        if (!Garantir()) return;

        // Tocadores de música antigos da cena ficam calados (a música agora é daqui);
        // fontes de som sem grupo passam a obedecer o volume de efeitos
        foreach (AudioSource fonte in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (fonte.gameObject.scene != cena) continue;
            if (banco.EhMusica(fonte.clip))
            {
                fonte.Stop();
                fonte.playOnAwake = false;
                fonte.enabled = false;
            }
            else if (fonte.outputAudioMixerGroup == null) fonte.outputAudioMixerGroup = banco.grupoEfeitos;
        }

        foreach (CarroController carro in Object.FindObjectsByType<CarroController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (carro.gameObject.scene == cena && carro.GetComponent<SomCarro>() == null) carro.gameObject.AddComponent<SomCarro>();

        foreach (Button botao in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (botao.gameObject.scene == cena && botao.GetComponent<SomBotao>() == null) botao.gameObject.AddComponent<SomBotao>();

        sistema.Abafar(false);
        MusicaDaCena();
    }

    // ---------- Efeitos ----------

    public static void Tocar(string id, float volume = 1f, float tom = 1f)
    {
        if (!Garantir() || !mapa.TryGetValue(id, out BancoSons.Efeito e)) return;
        float variacao = e.variacaoTom > 0f ? Random.Range(1f - e.variacaoTom, 1f + e.variacaoTom) : 1f;
        sistema.Efeito(e.clip, e.volume * volume, tom * variacao, 0f);
    }

    // Toca mais baixo quanto mais longe da câmera, e um pouco para o lado em que está
    public static void TocarEm(string id, Vector3 posicao, float volume = 1f, float tom = 1f, float alcance = 22f)
    {
        float fator = FatorDistancia(posicao, alcance, out float lado);
        if (fator <= 0.01f) return;
        if (!Garantir() || !mapa.TryGetValue(id, out BancoSons.Efeito e)) return;
        float variacao = e.variacaoTom > 0f ? Random.Range(1f - e.variacaoTom, 1f + e.variacaoTom) : 1f;
        sistema.Efeito(e.clip, e.volume * volume * fator, tom * variacao, lado);
    }

    public static float FatorDistancia(Vector3 posicao, float alcance, out float lado)
    {
        lado = 0f;
        Camera cam = Camera.main;
        if (cam == null) return 1f;
        Vector2 rel = posicao - cam.transform.position;
        float d = rel.magnitude;
        lado = Mathf.Clamp(Vector2.Dot(rel, cam.transform.right) / 12f, -0.6f, 0.6f);
        float k = 1f - Mathf.Clamp01((d - 4f) / Mathf.Max(1f, alcance - 4f));
        return k * k;
    }

    public static float VolumeDe(string id) => Garantir() && mapa.TryGetValue(id, out BancoSons.Efeito e) ? e.volume : 0f;

    // Fonte de som em loop presa a um objeto (motor, derrapagem...). Começa em silêncio.
    public static AudioSource CriarLoop(GameObject dono, string id)
    {
        if (!Garantir() || !mapa.TryGetValue(id, out BancoSons.Efeito e)) return null;
        AudioSource fonte = dono.AddComponent<AudioSource>();
        fonte.clip = e.clip;
        fonte.loop = true;
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;
        fonte.volume = 0f;
        fonte.outputAudioMixerGroup = banco.grupoEfeitos;
        fonte.time = Random.Range(0f, e.clip.length * 0.9f);
        fonte.Play();
        return fonte;
    }

    // ---------- Música ----------

    public static void MusicaDaCena()
    {
        if (!Garantir()) return;
        string cena = SceneManager.GetActiveScene().name;
        if (cena == ProgressoHistoria.PrimeiroNivel && MissaoMercado.VoltandoDoMercado)
        {
            MusicaPerseguicao();
            return;
        }
        BancoSons.MusicaCena m = banco.MusicaDe(cena);
        sistema.Musica(m != null ? m.musica : null, m != null ? m.volume : 0f);
    }

    public static void PararMusica()
    {
        if (Garantir()) sistema.Musica(null, 0f);
    }

    public static void MusicaPerseguicao()
    {
        if (Garantir()) sistema.Musica(banco.musicaPerseguicao, banco.volumePerseguicao);
    }

    public enum Vinheta { Vitoria, Carteira, Derrota }

    public static void TocarVinheta(Vinheta v)
    {
        if (!Garantir()) return;
        AudioClip clip = v == Vinheta.Vitoria ? banco.vinhetaVitoria : v == Vinheta.Carteira ? banco.vinhetaCarteira : banco.vinhetaDerrota;
        sistema.Vinheta(clip, banco.volumeVinhetas);
    }

    // Música mais baixa enquanto o jogo está pausado
    public static void AbafarMusica(bool abafar)
    {
        if (Garantir()) sistema.Abafar(abafar);
    }

    // ---------- Volumes (os mesmos do menu Opções) ----------

    public static void AplicarVolumesSalvos()
    {
        if (!Garantir() || banco.mixer == null) return;
        banco.mixer.SetFloat(ChaveMusica, Decibeis(PlayerPrefs.GetFloat(ChaveMusica, 0.8f)));
        banco.mixer.SetFloat(ChaveEfeitos, Decibeis(PlayerPrefs.GetFloat(ChaveEfeitos, 0.8f)));
    }

    private static float Decibeis(float v) => v <= 0.0001f ? -80f : Mathf.Log10(v) * 20f;
}

// O objeto que fica vivo entre as cenas e de fato toca os sons
public class SistemaSom : MonoBehaviour
{
    private const int TotalFontes = 24;

    private BancoSons banco;
    private readonly List<AudioSource> fontes = new List<AudioSource>();
    private int proxima;
    private AudioSource musicaA, musicaB, vinheta;
    private bool usandoA = true;
    private float volumeMusica, abafar = 1f, abafarAlvo = 1f;
    private Coroutine troca;

    public void Preparar(BancoSons novoBanco)
    {
        banco = novoBanco;
        for (int i = 0; i < TotalFontes; i++) fontes.Add(NovaFonte("Efeito " + i, banco.grupoEfeitos));
        musicaA = NovaFonte("Musica A", banco.grupoMusica);
        musicaB = NovaFonte("Musica B", banco.grupoMusica);
        musicaA.loop = musicaB.loop = true;
        vinheta = NovaFonte("Vinheta", banco.grupoMusica);
    }

    private AudioSource NovaFonte(string nome, AudioMixerGroup grupo)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(transform, false);
        AudioSource f = obj.AddComponent<AudioSource>();
        f.playOnAwake = false;
        f.spatialBlend = 0f;
        f.ignoreListenerPause = true;
        f.outputAudioMixerGroup = grupo;
        return f;
    }

    private void Start() => Sons.AplicarVolumesSalvos();

    public void Efeito(AudioClip clip, float volume, float tom, float lado)
    {
        if (clip == null) return;
        // Procura uma fonte livre; se todas estiverem tocando, usa a mais antiga
        AudioSource f = null;
        for (int i = 0; i < fontes.Count; i++)
        {
            AudioSource c = fontes[(proxima + i) % fontes.Count];
            if (!c.isPlaying) { f = c; proxima = (proxima + i + 1) % fontes.Count; break; }
        }
        if (f == null) { f = fontes[proxima]; proxima = (proxima + 1) % fontes.Count; }
        f.Stop();
        f.clip = clip;
        f.volume = Mathf.Clamp01(volume);
        f.pitch = tom;
        f.panStereo = lado;
        f.Play();
    }

    // ---------- Música ----------

    public void Musica(AudioClip clip, float volume)
    {
        AudioSource atual = usandoA ? musicaA : musicaB;
        if (clip != null && atual.clip == clip && atual.isPlaying)
        {
            volumeMusica = volume;   // a mesma música continua (ex.: da casa para a garagem)
            return;
        }
        if (troca != null) StopCoroutine(troca);
        troca = StartCoroutine(Trocar(clip, volume));
    }

    private IEnumerator Trocar(AudioClip clip, float volume)
    {
        AudioSource sai = usandoA ? musicaA : musicaB;
        AudioSource entra = usandoA ? musicaB : musicaA;
        usandoA = !usandoA;
        if (vinheta.isPlaying) vinheta.Stop();

        float volSai = sai.volume;
        volumeMusica = volume;
        if (clip != null)
        {
            entra.clip = clip;
            entra.volume = 0f;
            entra.Play();
        }
        else entra.Stop();

        const float duracao = 1.2f;
        for (float t = 0f; t < duracao; t += Time.unscaledDeltaTime)
        {
            float k = t / duracao;
            sai.volume = volSai * (1f - k);
            if (clip != null) entra.volume = volumeMusica * abafar * k;
            yield return null;
        }
        sai.Stop();
        sai.clip = null;
        if (clip != null) entra.volume = volumeMusica * abafar;
        troca = null;
    }

    public void Vinheta(AudioClip clip, float volume)
    {
        if (clip == null) return;
        if (troca != null) { StopCoroutine(troca); troca = null; }
        StartCoroutine(SumirMusica());
        vinheta.clip = clip;
        vinheta.volume = volume;
        vinheta.Play();
    }

    private IEnumerator SumirMusica()
    {
        AudioSource a = musicaA, b = musicaB;
        float va = a.volume, vb = b.volume;
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / 0.4f;
            a.volume = va * k;
            b.volume = vb * k;
            yield return null;
        }
        a.Stop(); b.Stop();
        a.clip = null; b.clip = null;
    }

    public void Abafar(bool sim) => abafarAlvo = sim ? 0.4f : 1f;

    private void Update()
    {
        if (Mathf.Approximately(abafar, abafarAlvo) || troca != null) return;
        abafar = Mathf.MoveTowards(abafar, abafarAlvo, Time.unscaledDeltaTime * 2f);
        AudioSource atual = usandoA ? musicaA : musicaB;
        if (atual.isPlaying) atual.volume = volumeMusica * abafar;
    }
}
