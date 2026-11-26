using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Som de qualquer botão do jogo (o Sons coloca este componente sozinho em todos os botões):
// clique ao apertar, "tic" ao passar de um botão para outro pelo teclado/mouse
// e um "não pode" ao clicar num botão bloqueado (nível com cadeado, item sem moedas).
// Botões que já tocam um som próprio (o clique antigo do menu) e os blocos do menu não tocam de novo.
[RequireComponent(typeof(Button))]
public class SomBotao : MonoBehaviour, IPointerClickHandler, ISubmitHandler, ISelectHandler
{
    private Button botao;
    private bool temSomProprio;
    private bool menuJaFazTique;
    private static float ultimoTique;

    private void Awake()
    {
        botao = GetComponent<Button>();
        menuJaFazTique = GetComponentInParent<MenuBlocos>(true) != null || GetComponentInParent<TelaMenu>(true) != null;
        for (int i = 0; i < botao.onClick.GetPersistentEventCount(); i++)
        {
            Object alvo = botao.onClick.GetPersistentTarget(i);
            if (alvo is AudioSource || alvo is SomBotoes) temSomProprio = true;
        }
    }

    public void OnPointerClick(PointerEventData dados)
    {
        if (dados.button == PointerEventData.InputButton.Left) Apertou();
    }

    public void OnSubmit(BaseEventData dados) => Apertou();

    private void Apertou()
    {
        if (botao == null) return;
        if (!botao.interactable) { Sons.Tocar("SemMoedas", 0.6f); return; }
        if (!temSomProprio) Sons.Tocar("Clique");
    }

    public void OnSelect(BaseEventData dados)
    {
        if (menuJaFazTique || Time.unscaledTime - ultimoTique < 0.05f) return;
        ultimoTique = Time.unscaledTime;
        Sons.Tocar("Navegar");
    }
}
