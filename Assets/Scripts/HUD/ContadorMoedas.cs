using System.Collections;
using TMPro;
using UnityEngine;

// Contador de moedas do HUD. Quando o jogador ganha moedas, o número sobe contando,
// a caixa dá um "pulo" e aparece um "+50" que desce e some.
public class ContadorMoedas : MonoBehaviour
{
    [SerializeField] private TMP_Text texto;
    [SerializeField] private RectTransform caixa;        // o que dá o "pulo"
    [SerializeField] private TMP_Text textoGanho;        // o "+50" (modelo, fica escondido)

    [Header("Animação")]
    [SerializeField] private float duracaoContagem = 0.8f;
    [SerializeField] private float duracaoGanho = 1.2f;

    private int mostrado;
    private Coroutine contando;

    private void OnEnable()
    {
        mostrado = Loja.Moedas;
        Escrever(mostrado);
        if (textoGanho != null) textoGanho.gameObject.SetActive(false);
        Loja.AoMudarMoedas += AoMudar;
    }

    private void OnDisable()
    {
        Loja.AoMudarMoedas -= AoMudar;
    }

    private void AoMudar(int saldo)
    {
        int diferenca = saldo - mostrado;
        if (diferenca > 0 && textoGanho != null) StartCoroutine(MostrarGanho(diferenca));

        if (contando != null) StopCoroutine(contando);
        contando = StartCoroutine(Contar(mostrado, saldo));
    }

    // Tudo em tempo real: funciona mesmo com o jogo parado (tela de conclusão)
    private IEnumerator Contar(int de, int para)
    {
        float t = 0f;
        while (t < duracaoContagem)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracaoContagem);
            mostrado = Mathf.RoundToInt(Mathf.Lerp(de, para, 1f - (1f - k) * (1f - k)));
            Escrever(mostrado);

            if (caixa != null) caixa.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }

        mostrado = para;
        Escrever(mostrado);
        if (caixa != null) caixa.localScale = Vector3.one;
        contando = null;
    }

    private IEnumerator MostrarGanho(int quantidade)
    {
        TMP_Text ganho = Instantiate(textoGanho, textoGanho.transform.parent);
        ganho.gameObject.SetActive(true);
        ganho.text = "+" + quantidade;

        RectTransform rt = ganho.rectTransform;
        Vector2 inicio = textoGanho.rectTransform.anchoredPosition;

        float t = 0f;
        while (t < duracaoGanho)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracaoGanho);
            rt.anchoredPosition = inicio + new Vector2(0f, -40f * k);
            ganho.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            yield return null;
        }

        Destroy(ganho.gameObject);
    }

    private void Escrever(int valor)
    {
        if (texto != null) texto.text = valor.ToString();
    }
}
