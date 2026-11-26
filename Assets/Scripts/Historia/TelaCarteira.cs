using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A carteira de motorista do Jason aparecendo depois do exame: o cartão entra girando,
// o carimbo "APROVADO" cai em cima e o jogador aperta ENTER para continuar.
public class TelaCarteira : MonoBehaviour
{
    [Header("Interface (montada pela ferramenta)")]
    [SerializeField] private GameObject painel;
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private RectTransform cartao;
    [SerializeField] private Image retrato;
    [SerializeField] private TMP_Text textoNome;
    [SerializeField] private TMP_Text textoDetalhes;
    [SerializeField] private RectTransform carimbo;
    [SerializeField] private TMP_Text textoContinuar;

    public static bool Aberta { get; private set; }

    private void Awake()
    {
        if (painel != null) painel.SetActive(false);
    }

    private void OnDestroy() => Aberta = false;

    public IEnumerator Mostrar(Sprite foto, string nome, int faltas, string instrutor)
    {
        if (painel == null) yield break;

        Aberta = true;
        Sons.TocarVinheta(Sons.Vinheta.Carteira);
        if (retrato != null && foto != null) retrato.sprite = foto;
        if (textoNome != null) textoNome.text = nome.ToUpper();
        if (textoDetalhes != null)
            textoDetalhes.text = "CATEGORIA: <b>B</b>\nPONTOS: <b>20</b>\nFALTAS NO EXAME: <b>" + faltas + "</b>\n" +
                                 "INSTRUTOR: <b>" + instrutor.ToUpper() + "</b>";
        if (carimbo != null) carimbo.gameObject.SetActive(false);
        if (textoContinuar != null) textoContinuar.gameObject.SetActive(false);

        painel.SetActive(true);

        // Cartão entra girando
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.6f);
            float pulo = 1f + 2.7f * Mathf.Pow(k - 1f, 3f) + 1.7f * Mathf.Pow(k - 1f, 2f);
            if (grupo != null) grupo.alpha = Mathf.Clamp01(k * 2f);
            if (cartao != null)
            {
                cartao.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, 1f, pulo);
                cartao.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-25f, -3f, k));
            }
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.35f);

        // Carimbo cai com força
        if (carimbo != null)
        {
            carimbo.gameObject.SetActive(true);
            t = 0f;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                carimbo.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, Mathf.Clamp01(t / 0.18f));
                yield return null;
            }
            carimbo.localScale = Vector3.one;

            Vector2 base_ = cartao.anchoredPosition;
            t = 0f;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                float forca = 14f * (1f - t / 0.25f);
                cartao.anchoredPosition = base_ + new Vector2(Random.Range(-forca, forca), Random.Range(-forca, forca));
                yield return null;
            }
            cartao.anchoredPosition = base_;
        }

        yield return new WaitForSecondsRealtime(0.3f);
        if (textoContinuar != null) textoContinuar.gameObject.SetActive(true);

        while (!CaixaDialogo.Avancou())
        {
            if (textoContinuar != null) textoContinuar.alpha = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
            yield return null;
        }

        t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            if (grupo != null) grupo.alpha = 1f - t / 0.3f;
            yield return null;
        }
        painel.SetActive(false);
        Aberta = false;
        yield return null;
    }
}
