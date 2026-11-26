using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Caixa de diálogo estilo Pokémon antigo: o texto aparece letra por letra com um bipe de "voz",
// o retrato e o nome de quem fala ficam do lado, e uma setinha pisca quando dá para continuar.
// ENTER / ESPAÇO / E / Z (ou clique) completam o texto ou passam para a próxima fala.
// Segurar ESC pula o resto da conversa (só os textos: o roteiro da cena continua igual).
// A interface é montada por Mr Driver > História (fica no CanvasHistoria).
public class CaixaDialogo : MonoBehaviour
{
    [Header("Interface (montada pela ferramenta)")]
    [SerializeField] private GameObject painel;
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private RectTransform janela;
    [SerializeField] private GameObject caixaRetrato;
    [SerializeField] private Image retrato;
    [SerializeField] private GameObject placaNome;
    [SerializeField] private Image fundoNome;
    [SerializeField] private TMP_Text textoNome;
    [SerializeField] private TMP_Text texto;
    [SerializeField] private RectTransform seta;
    [SerializeField] private float margemComRetrato = 300f;   // onde o texto começa quando tem retrato
    [SerializeField] private float margemSemRetrato = 60f;

    [Header("Ritmo")]
    [SerializeField] private float letrasPorSegundo = 45f;
    [SerializeField] private float pausaPonto = 0.18f;        // depois de . ! ?
    [SerializeField] private float pausaVirgula = 0.07f;

    [Header("Pular conversa (segurar ESC)")]
    [SerializeField] private float segundosParaPular = 0.5f;

    [Header("Som (opcional)")]
    [SerializeField] private AudioSource voz;
    [SerializeField, Range(0f, 1f)] private float volumeVoz = 0.35f;

    private static CaixaDialogo instancia;
    private static readonly HashSet<CaixaDialogo> abertas = new HashSet<CaixaDialogo>();

    private bool aberta;          // conversa que espera o jogador
    private Coroutine comentario; // fala rápida que some sozinha
    private Vector2 posicaoJanela;
    private AudioClip bipe;
    private float alturaSeta;

    private bool pulando;          // o jogador segurou ESC: o resto das falas é ignorado
    private float tempoEsc;
    private GameObject dicaPular;  // "Segure ESC para pular" + barrinha (criados pelo código)
    private RectTransform barraPular;

    public static CaixaDialogo Instancia
    {
        get
        {
            if (instancia == null) instancia = FindFirstObjectByType<CaixaDialogo>(FindObjectsInactive.Include);
            return instancia;
        }
    }

    // Tem conversa aberta em alguma caixa? (o PauseController e o jogador andando olham isso)
    public static bool AlgumaAberta => abertas.Count > 0;

    public bool Aberta => aberta;

    private void Awake()
    {
        instancia = this;
        if (janela != null) posicaoJanela = janela.anchoredPosition;
        if (seta != null) alturaSeta = seta.anchoredPosition.y;
        if (painel != null) painel.SetActive(false);
        bipe = CriarBipe();
        if (voz != null) voz.playOnAwake = false;
        CriarDicaPular();
    }

    // Conta quanto tempo o ESC está segurado durante uma conversa
    private void Update()
    {
        if (!aberta || pulando)
        {
            tempoEsc = 0f;
            return;
        }

        tempoEsc = SegurandoEsc() ? tempoEsc + Time.unscaledDeltaTime : 0f;
        float progresso = segundosParaPular > 0f ? Mathf.Clamp01(tempoEsc / segundosParaPular) : 1f;
        if (barraPular != null) barraPular.localScale = new Vector3(progresso, 1f, 1f);

        if (progresso >= 1f)
        {
            pulando = true;
            Sons.Tocar("DialogoAvancar");
        }
    }

    private void CriarDicaPular()
    {
        if (janela == null || texto == null) return;

        dicaPular = new GameObject("DicaPular", typeof(RectTransform));
        RectTransform rt = dicaPular.GetComponent<RectTransform>();
        rt.SetParent(janela, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-28f, -14f);
        rt.sizeDelta = new Vector2(320f, 34f);

        TextMeshProUGUI dica = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        dica.rectTransform.SetParent(rt, false);
        dica.rectTransform.anchorMin = Vector2.zero;
        dica.rectTransform.anchorMax = Vector2.one;
        dica.rectTransform.offsetMin = dica.rectTransform.offsetMax = Vector2.zero;
        dica.font = texto.font;
        dica.fontSize = Mathf.Max(14f, texto.fontSize * 0.45f);
        dica.alignment = TextAlignmentOptions.TopRight;
        dica.color = new Color(1f, 1f, 1f, 0.55f);
        dica.text = "Segure ESC para pular";
        dica.raycastTarget = false;

        Image fundo = new GameObject("Barra", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        fundo.rectTransform.SetParent(rt, false);
        fundo.rectTransform.anchorMin = fundo.rectTransform.anchorMax = fundo.rectTransform.pivot = new Vector2(1f, 0f);
        fundo.rectTransform.anchoredPosition = Vector2.zero;
        fundo.rectTransform.sizeDelta = new Vector2(150f, 5f);
        fundo.color = new Color(1f, 1f, 1f, 0.18f);
        fundo.raycastTarget = false;

        Image cheia = new GameObject("Progresso", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        barraPular = cheia.rectTransform;
        barraPular.SetParent(fundo.rectTransform, false);
        barraPular.anchorMin = Vector2.zero;
        barraPular.anchorMax = new Vector2(0f, 1f);
        barraPular.pivot = new Vector2(0f, 0.5f);
        barraPular.sizeDelta = new Vector2(150f, 0f);
        barraPular.anchoredPosition = Vector2.zero;
        barraPular.localScale = new Vector3(0f, 1f, 1f);
        cheia.color = new Color(1f, 0.85f, 0.25f, 0.9f);
        cheia.raycastTarget = false;

        dicaPular.SetActive(false);
    }

    private void MostrarDicaPular(bool mostrar)
    {
        if (barraPular != null) barraPular.localScale = new Vector3(0f, 1f, 1f);
        if (dicaPular != null) dicaPular.SetActive(mostrar);
    }

    private void OnDisable()
    {
        abertas.Remove(this);
    }

    private void OnDestroy()
    {
        abertas.Remove(this);
        if (instancia == this) instancia = null;
    }

    // ---------- Conversa (espera o jogador apertar uma tecla em cada fala) ----------

    public IEnumerator Conversar(IList<Fala> falas, bool pausarJogo = false)
    {
        if (falas == null || falas.Count == 0) yield break;
        if (painel == null || texto == null)
        {
            Debug.LogError("CaixaDialogo: monte a interface com Mr Driver > História.", this);
            yield break;
        }

        while (aberta) yield return null;   // outra conversa ainda não terminou

        bool estavaComentando = comentario != null;
        if (estavaComentando) { StopCoroutine(comentario); comentario = null; }

        aberta = true;
        abertas.Add(this);
        pulando = false;
        tempoEsc = 0f;
        MostrarDicaPular(true);

        float tempoAntes = Time.timeScale;
        if (pausarJogo) Time.timeScale = 0f;

        painel.SetActive(true);
        if (!estavaComentando) Sons.Tocar("DialogoAbrir");
        if (!estavaComentando) yield return Animar(0f, 1f, 0.18f);
        else AplicarAnimacao(1f);

        foreach (Fala fala in falas)
        {
            if (pulando) break;
            if (fala == null || string.IsNullOrEmpty(fala.texto)) continue;
            yield return Escrever(fala, true);
        }

        MostrarDicaPular(false);
        if (pulando && seta != null) seta.gameObject.SetActive(false);
        yield return Animar(1f, 0f, 0.15f);
        painel.SetActive(false);

        if (pausarJogo && Mathf.Approximately(Time.timeScale, 0f))
            Time.timeScale = tempoAntes > 0f ? tempoAntes : 1f;

        abertas.Remove(this);
        aberta = false;
        pulando = false;
        yield return null;   // a tecla que fechou a conversa não vale para o jogo no mesmo frame
    }

    public IEnumerator Conversar(Fala fala, bool pausarJogo = false)
    {
        return Conversar(new List<Fala> { fala }, pausarJogo);
    }

    // ---------- Comentário rápido (não pausa e some sozinho; usado pelo instrutor no exame) ----------

    public void Comentar(Fala fala, float segundos = 2.5f)
    {
        if (aberta || fala == null || painel == null || !isActiveAndEnabled) return;
        if (comentario != null) StopCoroutine(comentario);
        comentario = StartCoroutine(RotinaComentario(fala, segundos));
    }

    private IEnumerator RotinaComentario(Fala fala, float segundos)
    {
        painel.SetActive(true);
        yield return Animar(grupo != null ? grupo.alpha : 0f, 1f, 0.12f);
        yield return Escrever(fala, false);

        float t = 0f;
        while (t < segundos) { t += Time.unscaledDeltaTime; yield return null; }

        yield return Animar(1f, 0f, 0.2f);
        painel.SetActive(false);
        comentario = null;
    }

    // ---------- Uma fala ----------

    private IEnumerator Escrever(Fala fala, bool esperarTecla)
    {
        bool narrador = fala.quem == null;

        if (caixaRetrato != null) caixaRetrato.SetActive(!narrador && fala.quem.retrato != null);
        if (!narrador && retrato != null) retrato.sprite = fala.quem.retrato;

        if (placaNome != null) placaNome.SetActive(!narrador);
        if (!narrador)
        {
            if (textoNome != null) textoNome.text = fala.quem.nome.ToUpper();
            if (fundoNome != null) fundoNome.color = fala.quem.corNome;
        }

        bool comRetrato = caixaRetrato != null && caixaRetrato.activeSelf;
        RectTransform rt = texto.rectTransform;
        rt.offsetMin = new Vector2(comRetrato ? margemComRetrato : margemSemRetrato, rt.offsetMin.y);
        texto.fontStyle = narrador ? FontStyles.Italic : FontStyles.Normal;
        texto.alignment = narrador ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft;

        texto.text = fala.texto;
        texto.maxVisibleCharacters = 0;
        texto.ForceMeshUpdate();
        int total = texto.textInfo.characterCount;

        if (seta != null) seta.gameObject.SetActive(false);

        if (fala.emote != Emote.Nenhum && fala.quem != null)
        {
            AnimadorPersonagem ator = AnimadorPersonagem.De(fala.quem);
            if (ator != null) ator.MostrarEmote(fala.emote);
        }

        if (fala.tremer) StartCoroutine(Tremer(0.35f));
        if (comRetrato) StartCoroutine(PuloRetrato());

        // Letra por letra
        yield return null;   // ignora a tecla que abriu a fala
        float tom = narrador ? 0.8f : fala.quem.tomVoz;
        narradorAtual = narrador;
        float acumulado = 0f;
        int visiveis = 0;
        while (visiveis < total)
        {
            if (esperarTecla && pulando) yield break;
            if (esperarTecla && Avancou()) { visiveis = total; break; }

            acumulado += Time.unscaledDeltaTime * letrasPorSegundo * (Segurando() ? 2.5f : 1f);
            while (acumulado >= 1f && visiveis < total)
            {
                acumulado -= 1f;
                char c = texto.textInfo.characterInfo[visiveis].character;
                visiveis++;

                if (!char.IsWhiteSpace(c) && visiveis % 2 == 1) TocarBipe(tom);

                if (c == '.' || c == '!' || c == '?') acumulado -= pausaPonto * letrasPorSegundo;
                else if (c == ',') acumulado -= pausaVirgula * letrasPorSegundo;
            }
            texto.maxVisibleCharacters = visiveis;
            yield return null;
        }
        texto.maxVisibleCharacters = total;

        if (!esperarTecla) yield break;

        // Espera o jogador
        if (seta != null) seta.gameObject.SetActive(true);
        yield return null;
        while (!Avancou())
        {
            if (pulando) yield break;
            if (seta != null)
                seta.anchoredPosition = new Vector2(seta.anchoredPosition.x,
                    alturaSeta + (Mathf.Repeat(Time.unscaledTime * 2f, 1f) < 0.5f ? 0f : -6f));
            yield return null;
        }
        Sons.Tocar("DialogoAvancar");
    }

    // ---------- Animações (tempo real: o jogo pode estar pausado) ----------

    private IEnumerator Animar(float de, float para, float duracao)
    {
        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracao);
            AplicarAnimacao(Mathf.Lerp(de, para, 1f - (1f - k) * (1f - k)));
            yield return null;
        }
        AplicarAnimacao(para);
    }

    private void AplicarAnimacao(float valor)
    {
        if (grupo != null) grupo.alpha = valor;
        if (janela != null) janela.anchoredPosition = posicaoJanela + new Vector2(0f, (1f - valor) * -40f);
    }

    private IEnumerator Tremer(float duracao)
    {
        float t = 0f;
        while (t < duracao && janela != null)
        {
            t += Time.unscaledDeltaTime;
            float forca = 10f * (1f - t / duracao);
            janela.anchoredPosition = posicaoJanela + new Vector2(Random.Range(-forca, forca), Random.Range(-forca, forca));
            yield return null;
        }
        if (janela != null) janela.anchoredPosition = posicaoJanela;
    }

    private IEnumerator PuloRetrato()
    {
        if (retrato == null) yield break;
        RectTransform rt = retrato.rectTransform;
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.unscaledDeltaTime;
            rt.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Mathf.Clamp01(t / 0.18f) * Mathf.PI));
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    // ---------- Som ----------

    private void TocarBipe(float tom)
    {
        if (voz == null) return;
        // Cada faixa de tom de voz ganha um timbre diferente (vozes graves mais "quadradas", agudas mais suaves)
        AudioClip clip = ClipDaVoz(tom);
        if (clip == null) return;
        voz.pitch = tom * Random.Range(0.95f, 1.05f);
        voz.PlayOneShot(clip, volumeVoz);
    }

    private AudioClip ClipDaVoz(float tom)
    {
        BancoSons banco = Sons.Banco;
        if (banco == null) return bipe;
        string id;
        if (Mathf.Approximately(tom, 0.8f) && narradorAtual) id = "VozNarrador";
        else if (tom < 0.85f) id = "VozGrave";
        else if (tom < 1.05f) id = "VozMedia";
        else if (tom < 1.25f) id = "VozFina";
        else id = "VozSuave";
        BancoSons.Efeito e = banco.Achar(id);
        return e != null && e.clip != null ? e.clip : bipe;
    }

    private bool narradorAtual;

    // Bipe curtinho de onda quadrada (som de videogame antigo), criado pelo código
    private static AudioClip CriarBipe()
    {
        const int taxa = 22050;
        int amostras = Mathf.RoundToInt(taxa * 0.045f);
        float[] dados = new float[amostras];
        for (int i = 0; i < amostras; i++)
        {
            float t = (float)i / taxa;
            float onda = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 520f * t));
            float envelope = 1f - (float)i / amostras;
            dados[i] = onda * envelope * 0.5f;
        }
        AudioClip clip = AudioClip.Create("BipeDialogo", amostras, 1, taxa, false);
        clip.SetData(dados, 0);
        return clip;
    }

    // ---------- Teclado ----------

    public static bool Avancou()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        bool tecla = t != null && (t.enterKey.wasPressedThisFrame || t.numpadEnterKey.wasPressedThisFrame ||
                                   t.spaceKey.wasPressedThisFrame || t.eKey.wasPressedThisFrame ||
                                   t.zKey.wasPressedThisFrame);
        bool clique = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return tecla || clique;
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
               Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) ||
               Input.GetKeyDown(KeyCode.Z) || Input.GetMouseButtonDown(0);
#endif
    }

    private static bool SegurandoEsc()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        return t != null && t.escapeKey.isPressed;
#else
        return Input.GetKey(KeyCode.Escape);
#endif
    }

    private static bool Segurando()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        return t != null && (t.enterKey.isPressed || t.spaceKey.isPressed || t.zKey.isPressed);
#else
        return Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Z);
#endif
    }
}
