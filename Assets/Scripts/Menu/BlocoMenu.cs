using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Um bloco (quadrado) do menu principal. O bloco é o próprio botão.
// Com o mouse em cima ou selecionado pelo teclado ele cresce, ganha borda e o ícone balança.
[RequireComponent(typeof(Button))]
public class BlocoMenu : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
{
    [Header("Partes (montadas pela ferramenta)")]
    [SerializeField] private RectTransform conteudo;   // o que cresce
    [SerializeField] private Image fundo;
    [SerializeField] private Image destaque;            // borda clara quando selecionado
    [SerializeField] private RectTransform icone;
    [SerializeField] private TMP_Text titulo;
    [SerializeField] private TMP_Text subtitulo;

    [Header("Texto da barra de baixo")]
    [TextArea(1, 3)] [SerializeField] private string descricao;

    [SerializeField] private float escalaSelecionado = 1.05f;

    private bool selecionado;
    private Color corFundo;
    private Vector2 posicaoIcone;
    private MenuBlocos menu;
    private TelaMenu tela;   // quando o bloco está numa tela do menu (Opções, Sobre...)

    public string Descricao => descricao;
    public bool Selecionado => selecionado;

    private void Awake()
    {
        if (fundo != null) corFundo = fundo.color;
        if (icone != null) posicaoIcone = icone.anchoredPosition;
        menu = GetComponentInParent<MenuBlocos>();
        tela = GetComponentInParent<TelaMenu>(true);
    }

    private void OnDisable()
    {
        selecionado = false;
        Aplicar(1f, 0f);
    }

    public void DefinirSubtitulo(string texto)
    {
        if (subtitulo != null) subtitulo.text = texto;
    }

    // Usado por quem cria blocos por código (ex.: botões de teste da loja)
    public void DefinirTextos(string novoTitulo, string novoSubtitulo, string novaDescricao)
    {
        if (titulo != null) titulo.text = novoTitulo;
        if (subtitulo != null) subtitulo.text = novoSubtitulo;
        descricao = novaDescricao;
    }

    public void DefinirCor(Color cor)
    {
        corFundo = cor;
        if (fundo != null) fundo.color = cor;
    }

    public void DefinirIcone(Sprite sprite)
    {
        Image img = icone != null ? icone.GetComponent<Image>() : null;
        if (img != null && sprite != null) img.sprite = sprite;
    }

    // O mouse também seleciona: assim mouse e teclado nunca destacam dois blocos ao mesmo tempo
    public void OnPointerEnter(PointerEventData dados)
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSelect(BaseEventData dados)
    {
        selecionado = true;
        if (menu != null) menu.Focou(this);
        else if (tela != null) tela.Focou(gameObject, descricao, null);
    }

    public void OnDeselect(BaseEventData dados) => selecionado = false;

    private float atual;   // 0 = normal, 1 = selecionado (suave)

    private void Update()
    {
        atual = Mathf.MoveTowards(atual, selecionado ? 1f : 0f, Time.unscaledDeltaTime * 7f);
        Aplicar(Mathf.SmoothStep(0f, 1f, atual), Time.unscaledTime);
    }

    private void Aplicar(float k, float tempo)
    {
        if (conteudo != null) conteudo.localScale = Vector3.one * Mathf.Lerp(1f, escalaSelecionado, k);

        if (destaque != null)
        {
            Color c = destaque.color;
            c.a = k * (0.75f + 0.25f * Mathf.Sin(tempo * 6f));
            destaque.color = c;
        }

        if (fundo != null) fundo.color = Color.Lerp(corFundo * new Color(0.8f, 0.8f, 0.8f, 1f), corFundo, 0.35f + 0.65f * k);

        if (icone != null)
            icone.anchoredPosition = posicaoIcone + new Vector2(0f, k * 6f * Mathf.Sin(tempo * 4f));
    }
}
