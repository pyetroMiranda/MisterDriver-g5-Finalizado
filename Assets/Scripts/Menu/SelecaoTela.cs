using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Qualquer coisa selecionável de uma tela do menu (controle de volume, botão de nível, botão de comprar):
// o mouse em cima também seleciona, a faixa de destaque pisca enquanto está selecionado
// e a TelaMenu mostra a descrição na barra de baixo.
public class SelecaoTela : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private string titulo;                 // aparece em destaque (opcional)
    [TextArea(1, 3)] [SerializeField] private string descricao;
    [SerializeField] private Graphic destaque;              // faixa/borda que aparece quando selecionado (opcional)
    [SerializeField] private RectTransform crescer;         // cresce um pouco quando selecionado (opcional)
    [SerializeField] private float escalaSelecionado = 1.04f;

    private bool selecionado;
    private float atual;
    private TelaMenu tela;
    private Color corDestaque = Color.white;

    public void Definir(string novoTitulo, string novaDescricao, Graphic novoDestaque, RectTransform novoCrescer)
    {
        titulo = novoTitulo;
        descricao = novaDescricao;
        destaque = novoDestaque;
        crescer = novoCrescer;
    }

    private void Awake()
    {
        tela = GetComponentInParent<TelaMenu>(true);
        if (destaque != null) corDestaque = destaque.color;
        Aplicar(0f, 0f);
    }

    private void OnDisable()
    {
        selecionado = false;
        atual = 0f;
        Aplicar(0f, 0f);
    }

    public void OnPointerEnter(PointerEventData dados)
    {
        Selectable s = GetComponent<Selectable>();
        if (s != null && !s.interactable) return;
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSelect(BaseEventData dados)
    {
        selecionado = true;
        if (tela == null) tela = GetComponentInParent<TelaMenu>(true);
        if (tela != null) tela.Focou(gameObject, descricao, titulo);
    }

    public void OnDeselect(BaseEventData dados) => selecionado = false;

    private void Update()
    {
        atual = Mathf.MoveTowards(atual, selecionado ? 1f : 0f, Time.unscaledDeltaTime * 7f);
        Aplicar(Mathf.SmoothStep(0f, 1f, atual), Time.unscaledTime);
    }

    private void Aplicar(float k, float tempo)
    {
        if (destaque != null)
        {
            Color c = corDestaque;
            c.a = corDestaque.a * k * (0.75f + 0.25f * Mathf.Sin(tempo * 6f));
            destaque.color = c;
        }
        if (crescer != null) crescer.localScale = Vector3.one * Mathf.Lerp(1f, escalaSelecionado, k);
    }
}
