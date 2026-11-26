using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Pausa do jogo (ESC ou botão de pausa). Funciona em qualquer nível:
// só o Pnl Pause é obrigatório; os outros painéis são opcionais.
public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pnlPause;

    [Header("Outros painéis que impedem pausar (opcional)")]
    [SerializeField] private GameObject pnlTutorialInicio;
    [SerializeField] private GameObject pnlGameOver;
    [SerializeField] private GameObject pnlNivelConcluido;

    [Header("Animação e teclado (opcional)")]
    [SerializeField] private CanvasGroup grupoPause;
    [SerializeField] private RectTransform janelaPause;
    [SerializeField] private Selectable primeiroBotao;     // fica selecionado ao pausar (Enter / setas)
    [SerializeField] private float duracaoEntrada = 0.2f;

    private bool pausado;
    private TelaNivelConcluido telaConclusao;
    private TutorialNivel tutorial;

    public bool EstaPausado => pausado;

    private void Start()
    {
        if (pnlPause == null)
        {
            Debug.LogError("Preencha o campo Pnl Pause do PauseController.", this);
            enabled = false;
            return;
        }

        pnlPause.SetActive(false);

        // Telas novas são encontradas sozinhas
        telaConclusao = FindFirstObjectByType<TelaNivelConcluido>(FindObjectsInactive.Include);
        tutorial = FindFirstObjectByType<TutorialNivel>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        bool apertouEsc = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            apertouEsc = Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        apertouEsc = Input.GetKeyDown(KeyCode.Escape);
#endif

        if (!apertouEsc)
            return;

        if (pausado)
            Continuar();
        else
            Pausar();
    }

    private static bool Aberto(GameObject painel) => painel != null && painel.activeInHierarchy;

    private bool OutroPainelAberto()
    {
        return Aberto(pnlTutorialInicio) ||
               Aberto(pnlGameOver) ||
               Aberto(pnlNivelConcluido) ||
               (telaConclusao != null && telaConclusao.Aberta) ||
               (tutorial != null && tutorial.DicaAberta) ||
               CaixaDialogo.AlgumaAberta ||      // conversa da história
               TelaCarteira.Aberta ||
               TelaGaragem.Aberta;
    }

    public void Pausar()
    {
        if (!enabled || pausado || OutroPainelAberto())
            return;

        if (Time.timeScale == 0f)
            return;

        pausado = true;
        Sons.Tocar("Pausa");
        Sons.AbafarMusica(true);
        pnlPause.SetActive(true);
        pnlPause.transform.SetAsLastSibling();
        Time.timeScale = 0f;

        if (primeiroBotao != null) primeiroBotao.Select();
        StartCoroutine(Entrar());
    }

    public void Continuar()
    {
        if (!enabled || !pausado)
            return;

        pausado = false;
        Sons.Tocar("Despausa");
        Sons.AbafarMusica(false);
        pnlPause.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void VoltarMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    // Aparece com fade e um leve zoom (tempo real: o jogo está parado)
    private IEnumerator Entrar()
    {
        if (grupoPause == null && janelaPause == null) yield break;

        float t = 0f;
        while (t < duracaoEntrada && pausado)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracaoEntrada);
            float suave = 1f - (1f - k) * (1f - k);
            if (grupoPause != null) grupoPause.alpha = suave;
            if (janelaPause != null) janelaPause.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, suave);
            yield return null;
        }
        if (grupoPause != null) grupoPause.alpha = 1f;
        if (janelaPause != null) janelaPause.localScale = Vector3.one;
    }
}
