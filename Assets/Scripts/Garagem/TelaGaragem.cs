using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Tela da garagem do Betão: escolher o carro e a cor.
// SETAS esquerda/direita (ou A/D) trocam o carro, SETAS cima/baixo (ou W/S) trocam a cor,
// ENTER confirma e ESC volta sem mudar nada. Tudo também funciona com o mouse.
// Montada por Mr Driver > História (cena Historia_Garagem).
public class TelaGaragem : MonoBehaviour
{
    [SerializeField] private CatalogoCarros catalogo;

    [Header("Interface (montada pela ferramenta)")]
    [SerializeField] private GameObject painel;
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private RectTransform janela;
    [Tooltip("Gira devagar, como a plataforma de uma concessionária.")]
    [SerializeField] private RectTransform giro;
    [SerializeField] private Image imgLataria;
    [SerializeField] private Image imgDetalhes;
    [SerializeField] private TMP_Text txtContador;
    [SerializeField] private TMP_Text txtNome;
    [SerializeField] private TMP_Text txtDescricao;
    [SerializeField] private TMP_Text txtCor;
    [SerializeField] private RectTransform gradeCores;
    [Tooltip("Botão de cor desligado, copiado para cada cor do catálogo.")]
    [SerializeField] private Button modeloBotaoCor;
    [SerializeField] private RectTransform selecaoCor;
    [SerializeField] private AudioSource som;

    [Header("Ajustes")]
    [SerializeField] private float alturaCarro = 420f;
    [SerializeField] private float velocidadeGiro = 24f;

    public static bool Aberta { get; private set; }

    private readonly List<RectTransform> botoesCores = new List<RectTransform>();
    private int modelo, cor;
    private bool decidido, confirmou, aceitandoTeclas;
    private float pulo;
    private AudioClip tique, confirmar;

    private void Awake()
    {
        if (painel != null) painel.SetActive(false);
        if (modeloBotaoCor != null) modeloBotaoCor.gameObject.SetActive(false);
        tique = CriarSom("TiqueGaragem", 1400f, 0.05f);
        confirmar = CriarSom("ConfirmarGaragem", 880f, 0.18f);
    }

    private void OnDisable() => Aberta = false;

    // Abre a tela e espera o jogador decidir. aoFechar recebe true se ele confirmou.
    public IEnumerator Abrir(Action<bool> aoFechar)
    {
        if (catalogo == null) catalogo = AcharCatalogo();
        if (catalogo == null || catalogo.Quantidade == 0 || painel == null)
        {
            Debug.LogError("TelaGaragem: sem catálogo de carros. Rode Mr Driver > História > 6. Carros da garagem.", this);
            aoFechar?.Invoke(false);
            yield break;
        }

        CriarBotoesCores();
        modelo = catalogo.IndiceDe(GaragemJogador.ModeloId);
        cor = Mathf.Clamp(GaragemJogador.CorIndice, 0, Mathf.Max(0, catalogo.cores.Count - 1));
        decidido = false;
        confirmou = false;
        aceitandoTeclas = false;
        Aberta = true;

        painel.SetActive(true);
        Atualizar(false);
        if (giro != null) giro.localRotation = Quaternion.Euler(0f, 0f, -20f);
        yield return Animar(0f, 1f, 0.22f);
        yield return null;   // a tecla que abriu a tela não vale aqui dentro
        aceitandoTeclas = true;

        while (!decidido)
        {
            LerTeclado();
            yield return null;
        }

        aceitandoTeclas = false;
        if (confirmou) GaragemJogador.Escolher(catalogo.modelos[modelo].id, cor);
        Tocar(confirmou ? confirmar : tique, confirmou ? 1f : 0.7f);

        yield return Animar(1f, 0f, 0.16f);
        painel.SetActive(false);
        Aberta = false;
        yield return null;   // a tecla que fechou não vale para o jogo
        aoFechar?.Invoke(confirmou);
    }

    // Plano B: o mesmo catálogo usado pelo carro da garagem ou pelo carro do jogador
    private static CatalogoCarros AcharCatalogo()
    {
        foreach (VitrineCarro v in FindObjectsByType<VitrineCarro>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (v.Catalogo != null) return v.Catalogo;
        return null;
    }

    // ---------- Botões (também ligados pela ferramenta) ----------

    public void Anterior() => TrocarModelo(-1);
    public void Proximo() => TrocarModelo(1);
    public void CorAnterior() => EscolherCor(cor - 1);
    public void CorProxima() => EscolherCor(cor + 1);

    public void Confirmar()
    {
        if (!Aberta || decidido) return;
        confirmou = true;
        decidido = true;
    }

    public void Voltar()
    {
        if (!Aberta || decidido) return;
        confirmou = false;
        decidido = true;
    }

    public void EscolherCor(int indice)
    {
        if (!Aberta || decidido || catalogo.cores.Count == 0) return;
        int n = catalogo.cores.Count;
        cor = ((indice % n) + n) % n;
        Atualizar(true);
    }

    private void TrocarModelo(int passo)
    {
        if (!Aberta || decidido || catalogo.Quantidade == 0) return;
        int n = catalogo.Quantidade;
        modelo = ((modelo + passo) % n + n) % n;
        Atualizar(true);
    }

    // ---------- Desenho ----------

    private void Atualizar(bool comSom)
    {
        CatalogoCarros.Modelo m = catalogo.modelos[modelo];

        Mostrar(imgLataria, m.lataria, catalogo.CorOu(cor));
        Mostrar(imgDetalhes, m.detalhes, Color.white);

        if (txtContador != null) txtContador.text = "CARRO " + (modelo + 1) + " DE " + catalogo.Quantidade;
        if (txtNome != null) txtNome.text = m.nome.ToUpperInvariant();
        if (txtDescricao != null) txtDescricao.text = m.descricao;
        CatalogoCarros.Cor c = catalogo.CorDe(cor);
        if (txtCor != null) txtCor.text = "COR: <color=#FFE14D>" + (c != null ? c.nome.ToUpperInvariant() : "-") + "</color>";

        if (selecaoCor != null && cor < botoesCores.Count)
        {
            selecaoCor.gameObject.SetActive(true);
            selecaoCor.SetParent(botoesCores[cor], false);
            selecaoCor.anchorMin = Vector2.zero;
            selecaoCor.anchorMax = Vector2.one;
            selecaoCor.offsetMin = new Vector2(-10f, -10f);
            selecaoCor.offsetMax = new Vector2(10f, 10f);
            selecaoCor.SetAsLastSibling();
        }

        if (comSom)
        {
            pulo = 1f;
            Tocar(tique, 1f);
        }
    }

    private void Mostrar(Image img, Sprite sprite, Color c)
    {
        if (img == null) return;
        img.sprite = sprite;
        img.enabled = sprite != null;
        img.color = c;
        if (sprite == null) return;
        float aspecto = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        img.rectTransform.sizeDelta = new Vector2(alturaCarro * aspecto, alturaCarro);
    }

    private void Update()
    {
        if (!Aberta) return;

        if (giro != null)
        {
            giro.Rotate(0f, 0f, -velocidadeGiro * Time.unscaledDeltaTime);
            pulo = Mathf.MoveTowards(pulo, 0f, Time.unscaledDeltaTime * 5f);
            float s = 1f + 0.08f * Mathf.Sin(pulo * Mathf.PI);
            giro.localScale = new Vector3(s, s, 1f);
        }

        if (selecaoCor != null && selecaoCor.gameObject.activeSelf)
        {
            float s = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f);
            selecaoCor.localScale = new Vector3(s, s, 1f);
        }
    }

    private void CriarBotoesCores()
    {
        if (gradeCores == null || modeloBotaoCor == null || botoesCores.Count == catalogo.cores.Count) return;

        foreach (RectTransform b in botoesCores) if (b != null) Destroy(b.gameObject);
        botoesCores.Clear();

        for (int i = 0; i < catalogo.cores.Count; i++)
        {
            Button b = Instantiate(modeloBotaoCor, gradeCores);
            b.gameObject.SetActive(true);
            b.name = "Cor " + catalogo.cores[i].nome;
            Image img = b.GetComponent<Image>();
            if (img != null) img.color = catalogo.cores[i].cor;
            int indice = i;
            b.onClick.AddListener(() => EscolherCor(indice));
            botoesCores.Add((RectTransform)b.transform);
        }
    }

    private IEnumerator Animar(float de, float para, float duracao)
    {
        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duracao);
            float a = Mathf.Lerp(de, para, k);
            if (grupo != null) grupo.alpha = a;
            if (janela != null) janela.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, a);
            yield return null;
        }
        if (grupo != null)
        {
            grupo.alpha = para;
            grupo.interactable = para > 0.5f;
            grupo.blocksRaycasts = para > 0.5f;
        }
        if (janela != null) janela.localScale = Vector3.one;
    }

    // ---------- Teclado ----------

    private void LerTeclado()
    {
        if (!aceitandoTeclas || decidido) return;
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        if (t == null) return;
        if (t.leftArrowKey.wasPressedThisFrame || t.aKey.wasPressedThisFrame) Anterior();
        else if (t.rightArrowKey.wasPressedThisFrame || t.dKey.wasPressedThisFrame) Proximo();
        else if (t.upArrowKey.wasPressedThisFrame || t.wKey.wasPressedThisFrame) CorAnterior();
        else if (t.downArrowKey.wasPressedThisFrame || t.sKey.wasPressedThisFrame) CorProxima();
        else if (t.enterKey.wasPressedThisFrame || t.numpadEnterKey.wasPressedThisFrame || t.spaceKey.wasPressedThisFrame) Confirmar();
        else if (t.escapeKey.wasPressedThisFrame || t.backspaceKey.wasPressedThisFrame) Voltar();
#else
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Anterior();
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Proximo();
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) CorAnterior();
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) CorProxima();
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) Confirmar();
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)) Voltar();
#endif
    }

    // ---------- Som (feito pelo código, sem arquivo) ----------

    private void Tocar(AudioClip clip, float volume)
    {
        if (som != null && clip != null) som.PlayOneShot(clip, volume);
    }

    private static AudioClip CriarSom(string nome, float frequencia, float duracao)
    {
        const int taxa = 44100;
        int amostras = Mathf.CeilToInt(taxa * duracao);
        float[] dados = new float[amostras];
        for (int i = 0; i < amostras; i++)
        {
            float t = (float)i / taxa;
            float f = frequencia * (1f + 0.5f * t / duracao);   // sobe um pouquinho: som de "confirmado"
            float envelope = Mathf.Pow(1f - (float)i / amostras, 2f);
            dados[i] = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t)) * envelope * 0.25f;
        }
        AudioClip clip = AudioClip.Create(nome, amostras, 1, taxa, false);
        clip.SetData(dados, 0);
        return clip;
    }
}
