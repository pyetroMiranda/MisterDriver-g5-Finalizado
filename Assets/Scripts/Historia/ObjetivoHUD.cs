using System.Collections;
using TMPro;
using UnityEngine;

// Plaquinha "OBJETIVO" no canto da tela. Desliza para dentro quando o objetivo muda.
public class ObjetivoHUD : MonoBehaviour
{
    [Header("Interface (montada pela ferramenta)")]
    [SerializeField] private RectTransform caixa;
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private TMP_Text texto;

    [SerializeField] private float duracao = 0.35f;

    private Vector2 posicaoFinal;
    private Coroutine animacao;

    public string Atual { get; private set; }

    private void Awake()
    {
        if (caixa != null) posicaoFinal = caixa.anchoredPosition;
        if (grupo != null) grupo.alpha = 0f;
    }

    public void Mostrar(string objetivo)
    {
        if (texto == null || caixa == null) return;
        if (objetivo != Atual && !string.IsNullOrEmpty(objetivo)) Sons.Tocar("Objetivo");
        Atual = objetivo;
        if (animacao != null) StopCoroutine(animacao);
        animacao = StartCoroutine(Trocar(objetivo));
    }

    public void Esconder()
    {
        Atual = null;
        if (animacao != null) StopCoroutine(animacao);
        if (isActiveAndEnabled) animacao = StartCoroutine(Mover(grupo != null ? grupo.alpha : 1f, 0f));
    }

    private IEnumerator Trocar(string objetivo)
    {
        if (grupo != null && grupo.alpha > 0.01f) yield return Mover(grupo.alpha, 0f);
        texto.text = objetivo;
        yield return Mover(0f, 1f);

        // Pulinho para chamar a atenção
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            caixa.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Mathf.Clamp01(t / 0.25f) * Mathf.PI));
            yield return null;
        }
        caixa.localScale = Vector3.one;
        animacao = null;
    }

    private IEnumerator Mover(float de, float para)
    {
        float t = 0f;
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float v = Mathf.Lerp(de, para, Mathf.SmoothStep(0f, 1f, t / duracao));
            Aplicar(v);
            yield return null;
        }
        Aplicar(para);
    }

    private void Aplicar(float v)
    {
        if (grupo != null) grupo.alpha = v;
        caixa.anchoredPosition = posicaoFinal + new Vector2((1f - v) * -60f, 0f);
    }
}
