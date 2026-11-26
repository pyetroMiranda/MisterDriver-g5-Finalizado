using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Uma tela do menu (Sobre, Opções, Níveis, Loja) no estilo do menu em blocos:
// entra deslizando, mantém sempre algo selecionado (setas / WASD navegam, ENTER confirma),
// ESC ou BACKSPACE voltam para o menu e a barra de baixo descreve o que está selecionado.
// Montado por Mr Driver > Menu > Montar telas do menu.
public class TelaMenu : MonoBehaviour
{
    [Header("Partes (montadas pela ferramenta)")]
    [SerializeField] private Selectable primeiro;          // vazio = o primeiro botão que estiver disponível
    [SerializeField] private Button botaoVoltar;
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private RectTransform animado;        // o miolo da tela, que entra deslizando
    [SerializeField] private TMP_Text textoDescricao;      // barra de baixo
    [SerializeField] private TMP_Text textoDestaque;       // título grande do item selecionado (opcional)
    [TextArea(1, 3)] [SerializeField] private string descricaoInicial;

    [Header("Som ao trocar de item (opcional)")]
    [SerializeField] private AudioSource fonte;
    [SerializeField, Range(0f, 1f)] private float volume = 0.25f;

    private GameObject ultimo;
    private AudioClip tique;
    private bool tocarSom;
    private Vector2 posicaoBase;
    private bool temPosicao;

    private void Awake()
    {
        tique = CriarTique();
        if (animado != null)
        {
            posicaoBase = animado.anchoredPosition;
            temPosicao = true;
        }
    }

    private void OnEnable()
    {
        if (textoDescricao != null) textoDescricao.text = descricaoInicial;
        ultimo = null;
        StartCoroutine(Entrar());
    }

    private IEnumerator Entrar()
    {
        tocarSom = false;
        if (grupo != null) grupo.alpha = 0f;
        yield return null;   // espera os botões (e os cartões da loja) ficarem prontos
        Selecionar(PrimeiroDisponivel());
        tocarSom = true;

        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / 0.3f);
            if (grupo != null) grupo.alpha = k;
            if (temPosicao) animado.anchoredPosition = posicaoBase + new Vector2(0f, -40f * (1f - k));
            yield return null;
        }
        if (grupo != null) grupo.alpha = 1f;
        if (temPosicao) animado.anchoredPosition = posicaoBase;
    }

    private void Update()
    {
        if (ApertouVoltar() && botaoVoltar != null && botaoVoltar.interactable)
        {
            botaoVoltar.onClick.Invoke();
            return;
        }

        // Clique fora de um botão não pode deixar a tela sem nada selecionado
        EventSystem es = EventSystem.current;
        if (es == null || !tocarSom) return;
        GameObject atual = es.currentSelectedGameObject;
        if (atual == null || !atual.activeInHierarchy || !atual.transform.IsChildOf(transform))
            Selecionar(ultimo != null && ultimo.activeInHierarchy ? ultimo.GetComponent<Selectable>() : PrimeiroDisponivel());
    }

    // Chamado por SelecaoTela e BlocoMenu quando algo é selecionado
    public void Focou(GameObject quem, string descricao, string destaque)
    {
        if (textoDescricao != null && !string.IsNullOrEmpty(descricao)) textoDescricao.text = descricao;
        if (textoDestaque != null && !string.IsNullOrEmpty(destaque)) textoDestaque.text = destaque;
        if (tocarSom && quem != ultimo && fonte != null && tique != null) fonte.PlayOneShot(tique, volume);
        ultimo = quem;
    }

    private Selectable PrimeiroDisponivel()
    {
        if (primeiro != null && primeiro.isActiveAndEnabled && primeiro.interactable) return primeiro;
        foreach (Selectable s in GetComponentsInChildren<Selectable>())
        {
            if (!s.interactable || s is Scrollbar) continue;
            if (botaoVoltar != null && s.gameObject == botaoVoltar.gameObject) continue;
            return s;
        }
        return botaoVoltar;
    }

    private static void Selecionar(Selectable s)
    {
        if (s == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(s.gameObject);
    }

    private static bool ApertouVoltar()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        return k != null && (k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
#endif
    }

    // "Tic" curtinho criado pelo código (o mesmo do menu em blocos)
    private static AudioClip CriarTique()
    {
        const int taxa = 22050;
        int n = Mathf.RoundToInt(taxa * 0.03f);
        float[] dados = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / taxa;
            dados[i] = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 1250f * t)) * (1f - (float)i / n) * 0.4f;
        }
        AudioClip clip = AudioClip.Create("TiqueTela", n, 1, taxa, false);
        clip.SetData(dados, 0);
        return clip;
    }
}
