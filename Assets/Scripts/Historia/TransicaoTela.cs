using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Escurece e clareia a tela entre as cenas da história. Cria sozinho o que precisa (não vai em cena nenhuma).
// Uso: TransicaoTela.Carregar("Nivel1"); ou yield return TransicaoTela.Escurecer();
public class TransicaoTela : MonoBehaviour
{
    private const float Duracao = 0.6f;

    private static TransicaoTela instancia;
    private Image cortina;
    private bool carregando;
    private bool manterEscuro;

    private static TransicaoTela Instancia
    {
        get
        {
            if (instancia == null)
            {
                GameObject obj = new GameObject("TransicaoTela", typeof(Canvas), typeof(CanvasGroup));
                DontDestroyOnLoad(obj);
                Canvas canvas = obj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;   // acima de tudo
                obj.GetComponent<CanvasGroup>().blocksRaycasts = false;

                GameObject img = new GameObject("Cortina", typeof(RectTransform), typeof(Image));
                img.transform.SetParent(obj.transform, false);
                RectTransform rt = img.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;

                instancia = obj.AddComponent<TransicaoTela>();
                instancia.cortina = img.GetComponent<Image>();
                instancia.cortina.color = new Color(0f, 0f, 0f, 0f);
                instancia.cortina.raycastTarget = false;
            }
            return instancia;
        }
    }

    public static bool Carregando => instancia != null && instancia.carregando;

    // Tela preta na hora. A cena fica no escuro até ela mesma chamar Clarear()
    // (serve para começar a cena com uma narração ou um "despertar").
    public static void ComecarEscuro()
    {
        Instancia.manterEscuro = true;
        Instancia.cortina.color = Color.black;
    }

    public static IEnumerator Escurecer(float duracao = Duracao) => Instancia.Animar(1f, duracao);

    public static IEnumerator Clarear(float duracao = Duracao)
    {
        Instancia.manterEscuro = false;
        return Instancia.Animar(0f, duracao);
    }

    public static void Carregar(string cena)
    {
        if (Instancia.carregando) return;
        Sons.Tocar("Transicao");
        Instancia.StartCoroutine(Instancia.CarregarCena(cena));
    }

    private IEnumerator CarregarCena(string cena)
    {
        carregando = true;
        manterEscuro = false;
        yield return Animar(1f, Duracao);

        Time.timeScale = 1f;
        AsyncOperation op = SceneManager.LoadSceneAsync(cena);
        while (op != null && !op.isDone) yield return null;

        // Dá tempo do Start() da cena nova rodar: se ela chamou ComecarEscuro(), ela mesma clareia
        yield return null;
        yield return null;
        carregando = false;
        if (!manterEscuro) yield return Animar(0f, Duracao);
    }

    private IEnumerator Animar(float alvo, float duracao)
    {
        float inicio = cortina.color.a;
        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            cortina.color = new Color(0f, 0f, 0f, Mathf.Lerp(inicio, alvo, Mathf.SmoothStep(0f, 1f, t / duracao)));
            yield return null;
        }
        cortina.color = new Color(0f, 0f, 0f, alvo);
    }
}
