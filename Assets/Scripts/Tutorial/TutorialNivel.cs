using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Mostra as dicas do tutorial, uma de cada vez, com entrada e saída suaves.
// As dicas "EmSequencia" aparecem na ordem da lista; as "AoPassarNoGatilho" aparecem
// quando o carro passa por um GatilhoDica (e entram na fila se outra dica estiver aberta).
public class TutorialNivel : MonoBehaviour
{
    [SerializeField] private List<PassoTutorial> passos = new List<PassoTutorial>();

    [Header("Interface (montada pela ferramenta)")]
    [SerializeField] private GameObject painel;
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private TMP_Text textoProgresso;
    [SerializeField] private TMP_Text textoTitulo;
    [SerializeField] private TMP_Text textoMensagem;
    [SerializeField] private TMP_Text textoAviso;
    [SerializeField] private TMP_Text textoRodape;
    [SerializeField] private Transform linhaTeclas;
    [SerializeField] private Image modeloTecla;
    [SerializeField] private Sprite[] spritesTeclas;     // na mesma ordem do enum TeclaTutorial
    [SerializeField] private float escalaTecla = 4f;     // tamanho dos pixels das teclas

    [Header("Suavidade")]
    [SerializeField] private float duracaoEntrada = 0.25f;
    [SerializeField] private float duracaoSaida = 0.2f;
    [SerializeField] private float duracaoCameraLenta = 0.35f;   // tempo para o jogo parar / voltar

    [Header("Ligações")]
    [SerializeField] private PauseController pause;

    private readonly Queue<PassoTutorial> fila = new Queue<PassoTutorial>();
    private readonly HashSet<PassoTutorial> concluidos = new HashSet<PassoTutorial>();
    private readonly HashSet<string> gatilhosUsados = new HashSet<string>();
    private readonly List<KeyValuePair<TeclaTutorial, RectTransform>> teclasNaTela =
        new List<KeyValuePair<TeclaTutorial, RectTransform>>();

    private bool aberta;
    private int mostradas;
    private float tempoEsperado = 1f;   // o Time.timeScale que o tutorial colocou por último
    private Coroutine rampa;
    private RectTransform painelRect;

    public bool DicaAberta => aberta;
    public List<PassoTutorial> Passos => passos;   // usado pelas ferramentas do Editor

    // Usado pela ferramenta do Editor para preencher as dicas padrão
    public void DefinirPassos(List<PassoTutorial> novosPassos)
    {
        passos = novosPassos;
    }

    private void Start()
    {
        if (painel == null || grupo == null || textoTitulo == null || textoMensagem == null)
        {
            Debug.LogError("TutorialNivel: monte a interface com Mr Driver > Tutorial > Montar tutorial novo.", this);
            enabled = false;
            return;
        }

        painelRect = painel.GetComponent<RectTransform>();
        painel.SetActive(false);
        if (modeloTecla != null) modeloTecla.gameObject.SetActive(false);
        if (pause == null) pause = FindFirstObjectByType<PauseController>();

        tempoEsperado = Time.timeScale;

        StartCoroutine(ProcessarFila());
        StartCoroutine(Sequencia());
    }

    // Chamado pelos GatilhoDica da pista
    public void Gatilho(string id)
    {
        if (string.IsNullOrEmpty(id) || !gatilhosUsados.Add(id)) return;

        foreach (PassoTutorial passo in passos)
        {
            if (passo.quando == PassoTutorial.Quando.AoPassarNoGatilho && passo.idGatilho == id &&
                !concluidos.Contains(passo) && !fila.Contains(passo))
            {
                fila.Enqueue(passo);
            }
        }
    }

    // ---------- Ordem das dicas ----------

    private IEnumerator Sequencia()
    {
        foreach (PassoTutorial passo in passos)
        {
            if (passo.quando != PassoTutorial.Quando.EmSequencia) continue;

            // Conta o tempo de jogo (para quando o jogo está pausado)
            if (passo.espera > 0f) yield return new WaitForSeconds(passo.espera);

            while (aberta || fila.Count > 0) yield return null;

            fila.Enqueue(passo);
            while (!concluidos.Contains(passo)) yield return null;
        }
    }

    private IEnumerator ProcessarFila()
    {
        while (true)
        {
            if (fila.Count > 0 && !JogoInterrompido())
                yield return Exibir(fila.Dequeue());
            else
                yield return null;
        }
    }

    // Pausa do jogador, game over ou nível concluído: não mostra dicas
    private bool JogoInterrompido()
    {
        if (pause != null && pause.EstaPausado) return true;
        return rampa == null && !Mathf.Approximately(Time.timeScale, tempoEsperado);
    }

    // ---------- Mostrar uma dica ----------

    private IEnumerator Exibir(PassoTutorial passo)
    {
        aberta = true;
        mostradas++;

        bool semPoder = passo.poder != null && !passo.poder.Comprado;
        bool temTeclas = passo.teclas != null && passo.teclas.Length > 0;
        bool fechaComTeclas = passo.fecharComAsTeclas && temTeclas && !semPoder;

        // Textos
        if (textoProgresso != null) textoProgresso.text = "DICA " + mostradas;
        textoTitulo.text = passo.titulo;
        textoMensagem.text = passo.mensagem;

        if (textoAviso != null)
        {
            textoAviso.gameObject.SetActive(semPoder);
            if (semPoder)
                textoAviso.text = "Você ainda não tem o poder " + passo.poder.Nome +
                                  ". Compre na LOJA do menu principal por " + passo.poder.Preco +
                                  " moedas (você tem " + Loja.Moedas + ").";
        }

        if (textoRodape != null)
        {
            if (fechaComTeclas)
                textoRodape.text = passo.teclas.Length == 1
                    ? "Aperte a tecla acima para continuar"
                    : "Aperte uma das teclas acima para continuar";
            else
                textoRodape.text = "Aperte ENTER para continuar";
        }

        MontarTeclas(passo.teclas);

        // Entrada suave
        if (passo.pausarJogo) MudarTempo(0f);
        painel.SetActive(true);
        yield return Animar(0f, 1f, duracaoEntrada);

        // Espera o jogador
        yield return null;   // ignora a tecla apertada no mesmo frame em que a dica abriu
        float tempo = 0f;
        RectTransform teclaApertada = null;

        while (true)
        {
            if (pause != null && pause.EstaPausado) { yield return null; continue; }
            if (JogoInterrompido()) break;

            tempo += Time.unscaledDeltaTime;

            if (Apertou(TeclaTutorial.Enter)) break;

            if (fechaComTeclas && (teclaApertada = TeclaApertada()) != null) break;

            if (passo.fecharSozinhoApos > 0f && tempo >= passo.fecharSozinhoApos) break;

            if (textoRodape != null)
                textoRodape.alpha = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);

            yield return null;
        }

        // Saída suave
        if (teclaApertada != null) StartCoroutine(Pulo(teclaApertada));
        yield return Animar(1f, 0f, duracaoSaida);
        painel.SetActive(false);

        if (passo.pausarJogo && !JogoInterrompido()) MudarTempo(1f);

        concluidos.Add(passo);
        aberta = false;
    }

    private void MontarTeclas(TeclaTutorial[] teclas)
    {
        foreach (KeyValuePair<TeclaTutorial, RectTransform> antiga in teclasNaTela)
            if (antiga.Value != null) Destroy(antiga.Value.gameObject);
        teclasNaTela.Clear();

        if (linhaTeclas == null || modeloTecla == null) return;

        bool tem = teclas != null && teclas.Length > 0;
        linhaTeclas.gameObject.SetActive(tem);
        if (!tem) return;

        foreach (TeclaTutorial tecla in teclas)
        {
            Image imagem = Instantiate(modeloTecla, linhaTeclas);
            imagem.gameObject.SetActive(true);

            int indice = (int)tecla;
            Sprite sprite = spritesTeclas != null && indice < spritesTeclas.Length ? spritesTeclas[indice] : null;
            imagem.sprite = sprite;

            LayoutElement tamanho = imagem.GetComponent<LayoutElement>();
            if (tamanho != null && sprite != null)
            {
                tamanho.preferredWidth = sprite.rect.width * escalaTecla;
                tamanho.preferredHeight = sprite.rect.height * escalaTecla;
            }

            teclasNaTela.Add(new KeyValuePair<TeclaTutorial, RectTransform>(tecla, imagem.rectTransform));
        }
    }

    private RectTransform TeclaApertada()
    {
        foreach (KeyValuePair<TeclaTutorial, RectTransform> par in teclasNaTela)
            if (Apertou(par.Key)) return par.Value;
        return null;
    }

    // ---------- Animações (usam tempo real, funcionam com o jogo pausado) ----------

    private IEnumerator Animar(float de, float para, float duracao)
    {
        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracao);
            float suave = 1f - (1f - k) * (1f - k);   // começa rápido e termina devagar
            AplicarAnimacao(Mathf.Lerp(de, para, suave));
            yield return null;
        }
        AplicarAnimacao(para);
    }

    private void AplicarAnimacao(float valor)
    {
        grupo.alpha = valor;
        if (painelRect != null) painelRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, valor);
    }

    // A tecla apertada dá um "pulo" antes da dica sumir
    private IEnumerator Pulo(RectTransform tecla)
    {
        float t = 0f;
        while (t < 0.2f && tecla != null)
        {
            t += Time.unscaledDeltaTime;
            tecla.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(Mathf.Clamp01(t / 0.2f) * Mathf.PI));
            yield return null;
        }
    }

    // Faz o jogo parar (ou voltar) aos poucos, como câmera lenta
    private void MudarTempo(float alvo)
    {
        if (rampa != null) StopCoroutine(rampa);
        rampa = StartCoroutine(Rampa(alvo));
    }

    private IEnumerator Rampa(float alvo)
    {
        float inicio = Time.timeScale;
        tempoEsperado = inicio;
        float t = 0f;

        while (t < duracaoCameraLenta)
        {
            // Outro script mudou o tempo (pausa, game over...): não briga com ele
            if (!Mathf.Approximately(Time.timeScale, tempoEsperado)) { rampa = null; yield break; }

            t += Time.unscaledDeltaTime;
            tempoEsperado = Mathf.Lerp(inicio, alvo, Mathf.SmoothStep(0f, 1f, t / duracaoCameraLenta));
            Time.timeScale = tempoEsperado;
            yield return null;
        }

        tempoEsperado = alvo;
        Time.timeScale = alvo;
        rampa = null;
    }

    // ---------- Teclado ----------

    private static bool Apertou(TeclaTutorial tecla)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        if (t == null) return false;
        switch (tecla)
        {
            case TeclaTutorial.Enter: return t.enterKey.wasPressedThisFrame || t.numpadEnterKey.wasPressedThisFrame;
            case TeclaTutorial.Espaco: return t.spaceKey.wasPressedThisFrame;
            case TeclaTutorial.Esc: return t.escapeKey.wasPressedThisFrame;
            case TeclaTutorial.Cima: return t.upArrowKey.wasPressedThisFrame;
            case TeclaTutorial.Baixo: return t.downArrowKey.wasPressedThisFrame;
            case TeclaTutorial.Esquerda: return t.leftArrowKey.wasPressedThisFrame;
            case TeclaTutorial.Direita: return t.rightArrowKey.wasPressedThisFrame;
            case TeclaTutorial.W: return t.wKey.wasPressedThisFrame;
            case TeclaTutorial.A: return t.aKey.wasPressedThisFrame;
            case TeclaTutorial.S: return t.sKey.wasPressedThisFrame;
            case TeclaTutorial.D: return t.dKey.wasPressedThisFrame;
            case TeclaTutorial.L: return t.lKey.wasPressedThisFrame;
            case TeclaTutorial.H: return t.hKey.wasPressedThisFrame;
            case TeclaTutorial.M: return t.mKey.wasPressedThisFrame;
            case TeclaTutorial.Ctrl: return t.leftCtrlKey.wasPressedThisFrame || t.rightCtrlKey.wasPressedThisFrame;
            case TeclaTutorial.B: return t.bKey.wasPressedThisFrame;
            case TeclaTutorial.J: return t.jKey.wasPressedThisFrame;
            case TeclaTutorial.K: return t.kKey.wasPressedThisFrame;
            case TeclaTutorial.G: return t.gKey.wasPressedThisFrame;
        }
        return false;
#else
        switch (tecla)
        {
            case TeclaTutorial.Enter: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            case TeclaTutorial.Espaco: return Input.GetKeyDown(KeyCode.Space);
            case TeclaTutorial.Esc: return Input.GetKeyDown(KeyCode.Escape);
            case TeclaTutorial.Cima: return Input.GetKeyDown(KeyCode.UpArrow);
            case TeclaTutorial.Baixo: return Input.GetKeyDown(KeyCode.DownArrow);
            case TeclaTutorial.Esquerda: return Input.GetKeyDown(KeyCode.LeftArrow);
            case TeclaTutorial.Direita: return Input.GetKeyDown(KeyCode.RightArrow);
            case TeclaTutorial.W: return Input.GetKeyDown(KeyCode.W);
            case TeclaTutorial.A: return Input.GetKeyDown(KeyCode.A);
            case TeclaTutorial.S: return Input.GetKeyDown(KeyCode.S);
            case TeclaTutorial.D: return Input.GetKeyDown(KeyCode.D);
            case TeclaTutorial.L: return Input.GetKeyDown(KeyCode.L);
            case TeclaTutorial.H: return Input.GetKeyDown(KeyCode.H);
            case TeclaTutorial.M: return Input.GetKeyDown(KeyCode.M);
            case TeclaTutorial.Ctrl: return Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
            case TeclaTutorial.B: return Input.GetKeyDown(KeyCode.B);
            case TeclaTutorial.J: return Input.GetKeyDown(KeyCode.J);
            case TeclaTutorial.K: return Input.GetKeyDown(KeyCode.K);
            case TeclaTutorial.G: return Input.GetKeyDown(KeyCode.G);
        }
        return false;
#endif
    }
}
