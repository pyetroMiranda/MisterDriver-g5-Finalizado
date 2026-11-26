using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Tela que aparece ao concluir um nível: sugere ir para o próximo nível ou voltar ao menu.
public class TelaNivelConcluido : MonoBehaviour
{
    [Header("Interface (montada pela ferramenta)")]
    [SerializeField] private GameObject painel;
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private RectTransform janela;
    [SerializeField] private TMP_Text textoTitulo;
    [SerializeField] private TMP_Text textoMensagem;
    [SerializeField] private TMP_Text textoMoedas;
    [SerializeField] private Button botaoProximo;
    [SerializeField] private Button botaoMenu;

    [Header("Cenas")]
    [Tooltip("Vazio = automático (Nivel1 → Nivel2, Nivel2 → Nivel3...).")]
    [SerializeField] private string proximaCena = "";
    [SerializeField] private string cenaMenu = "Menu";

    [Header("Textos")]
    [Tooltip("Vazio = automático (\"NÍVEL 1 CONCLUÍDO!\").")]
    [SerializeField] private string titulo = "";
    [SerializeField] private string mensagemComProximo = "Você estacionou certinho na vaga! O próximo nível foi liberado.";
    [SerializeField] private string mensagemSemProximo = "Você completou todos os níveis disponíveis. Novos níveis em breve!";

    [Header("Suavidade")]
    [SerializeField] private float duracaoEntrada = 0.35f;

    private bool aberta;
    private bool saindo;
    private bool gameOver;   // a mesma tela serve para "GAME OVER" (botão principal = tentar de novo)
    private string cenaSeguinte;

    public bool Aberta => aberta;

    private void Awake()
    {
        if (painel != null) painel.SetActive(false);
        if (botaoProximo != null) botaoProximo.onClick.AddListener(IrParaProximo);
        if (botaoMenu != null) botaoMenu.onClick.AddListener(IrParaMenu);
    }

    public void Mostrar(int moedasGanhas)
    {
        Sons.TocarVinheta(Sons.Vinheta.Vitoria);
        cenaSeguinte = DescobrirProxima();
        bool temProximo = !string.IsNullOrEmpty(cenaSeguinte) && Application.CanStreamedLevelBeLoaded(cenaSeguinte);

        if (textoTitulo != null) textoTitulo.text = string.IsNullOrEmpty(titulo) ? TituloAutomatico() : titulo;
        if (textoMensagem != null) textoMensagem.text = temProximo ? mensagemComProximo : mensagemSemProximo;
        if (textoMensagem != null && EhTutorial)
            textoMensagem.text = temProximo
                ? "Você já sabe dirigir! Agora comece a história do Jason e tire a sua carteira."
                : "Treino concluído! Volte ao menu e continue jogando.";
        if (textoMoedas != null)
        {
            textoMoedas.gameObject.SetActive(moedasGanhas > 0);
            textoMoedas.text = "+" + moedasGanhas + " moedas  (total: " + Loja.Moedas + ")";
        }
        if (botaoProximo != null) botaoProximo.gameObject.SetActive(temProximo);

        painel.SetActive(true);
        aberta = true;
        StartCoroutine(Entrar());

        // Deixa o botão principal selecionado (Enter / setas também funcionam)
        Button principal = temProximo ? botaoProximo : botaoMenu;
        if (principal != null) principal.Select();
    }

    // Game over (ex.: carteira suspensa): mesma janela, botão principal recomeça a fase
    public void MostrarGameOver(string tituloGameOver, string mensagem)
    {
        if (aberta || painel == null) return;
        gameOver = true;
        Time.timeScale = 0f;
        Sons.TocarVinheta(Sons.Vinheta.Derrota);

        if (textoTitulo != null) textoTitulo.text = tituloGameOver;
        if (textoMensagem != null) textoMensagem.text = mensagem;
        if (textoMoedas != null) textoMoedas.gameObject.SetActive(false);
        if (botaoProximo != null)
        {
            botaoProximo.gameObject.SetActive(true);
            botaoProximo.onClick.RemoveListener(IrParaProximo);
            botaoProximo.onClick.AddListener(JogarNovamente);
            TMP_Text rotulo = botaoProximo.GetComponentInChildren<TMP_Text>(true);
            if (rotulo != null) rotulo.text = "TENTAR DE NOVO";
        }

        painel.SetActive(true);
        aberta = true;
        StartCoroutine(Entrar());
        if (botaoProximo != null) botaoProximo.Select();
    }

    private void Update()
    {
        if (!aberta || saindo) return;

#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        bool enter = t != null && (t.enterKey.wasPressedThisFrame || t.numpadEnterKey.wasPressedThisFrame);
#else
        bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        if (!enter) return;

        if (gameOver) JogarNovamente();
        else if (botaoProximo != null && botaoProximo.gameObject.activeSelf) IrParaProximo();
        else IrParaMenu();
    }

    public void IrParaProximo() => Carregar(cenaSeguinte);
    public void IrParaMenu() => Carregar(cenaMenu);

    public void JogarNovamente() => Carregar(SceneManager.GetActiveScene().name);

    private void Carregar(string cena)
    {
        if (saindo || string.IsNullOrEmpty(cena)) return;
        saindo = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(cena);
    }

    // ---------- Automáticos ----------

    private string DescobrirProxima()
    {
        if (!string.IsNullOrEmpty(proximaCena)) return proximaCena;

        // O Tutorial: depois dele vem a história (ou o menu, se ela já foi jogada)
        if (EhTutorial) return ProgressoHistoria.HistoriaConcluida ? "" : ProgressoHistoria.ProximaCena();

        Match numero = Regex.Match(SceneManager.GetActiveScene().name, @"^(.*?)(\d+)$");
        if (!numero.Success) return "";
        return numero.Groups[1].Value + (int.Parse(numero.Groups[2].Value) + 1);
    }

    private static bool EhTutorial => SceneManager.GetActiveScene().name == ProgressoHistoria.CenaTutorial;

    private string TituloAutomatico()
    {
        if (EhTutorial) return "TUTORIAL CONCLUÍDO!";

        Match numero = Regex.Match(SceneManager.GetActiveScene().name, @"(\d+)$");
        return numero.Success ? "NÍVEL " + numero.Groups[1].Value + " CONCLUÍDO!" : "NÍVEL CONCLUÍDO!";
    }

    // ---------- Animação (tempo real: o jogo está parado) ----------

    private IEnumerator Entrar()
    {
        float t = 0f;
        while (t < duracaoEntrada)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracaoEntrada);
            if (grupo != null) grupo.alpha = k;
            if (janela != null) janela.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, SaidaComPulo(k));
            yield return null;
        }
        if (grupo != null) grupo.alpha = 1f;
        if (janela != null) janela.localScale = Vector3.one;
    }

    // Passa um pouquinho do tamanho final e volta (efeito "pulo")
    private static float SaidaComPulo(float k)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
    }
}
