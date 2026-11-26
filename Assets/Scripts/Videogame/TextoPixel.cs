using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Texto feito com letras em pixel art (uma imagem por letra), para o visual de videogame antigo.
// As letras vêm da FontePixel (montada pela ferramenta a partir da fonte pixel do jogo).
[RequireComponent(typeof(RectTransform))]
public class TextoPixel : MonoBehaviour
{
    public enum Alinhamento { Esquerda, Centro, Direita }

    [SerializeField] private FontePixel fonte;
    [SerializeField] private string texto = "";
    [SerializeField] private Color cor = Color.white;
    [Tooltip("Tamanho de cada pixel da letra (1 = um pixel da tela do videogame).")]
    [SerializeField] private float escala = 1f;
    [SerializeField] private Alinhamento alinhamento = Alinhamento.Centro;

    private readonly List<Image> letras = new List<Image>();
    private bool montado;

    public string Texto
    {
        get => texto;
        set
        {
            string novo = (value ?? "").ToUpperInvariant();
            if (montado && novo == texto) return;
            texto = novo;
            Montar();
        }
    }

    public Color Cor
    {
        get => cor;
        set
        {
            cor = value;
            foreach (Image i in letras) if (i != null) i.color = cor;
        }
    }

    public void Definir(FontePixel novaFonte, string novoTexto, Color novaCor, float novaEscala, Alinhamento novoAlinhamento)
    {
        fonte = novaFonte;
        texto = (novoTexto ?? "").ToUpperInvariant();
        cor = novaCor;
        escala = novaEscala;
        alinhamento = novoAlinhamento;
        Montar();
    }

    private void Awake()
    {
        // Letras criadas pela ferramenta (no Editor) são reaproveitadas
        letras.Clear();
        foreach (Transform t in transform)
        {
            Image i = t.GetComponent<Image>();
            if (i != null && t.name.StartsWith("Letra")) letras.Add(i);
        }
        Montar();
    }

    private void Montar()
    {
        montado = true;
        if (fonte == null) return;
        string t = texto ?? "";

        // Largura total, para alinhar
        float total = 0f;
        for (int k = 0; k < t.Length; k++)
        {
            total += Largura(t[k]);
            if (k < t.Length - 1) total += fonte.espaco;
        }
        float x = alinhamento == Alinhamento.Esquerda ? 0f : alinhamento == Alinhamento.Centro ? -total / 2f : -total;

        int usadas = 0;
        for (int k = 0; k < t.Length; k++)
        {
            Sprite s = fonte.Glifo(t[k]);
            float w = Largura(t[k]);
            if (s != null)
            {
                Image img = Letra(usadas++);
                img.sprite = s;
                img.color = cor;
                RectTransform rt = img.rectTransform;
                rt.anchoredPosition = new Vector2(Mathf.Round(x) * escala, -fonte.altura / 2f * escala);
                rt.sizeDelta = new Vector2(w * escala, fonte.altura * escala);
                img.gameObject.SetActive(true);
            }
            x += w + fonte.espaco;
        }
        for (int k = usadas; k < letras.Count; k++) if (letras[k] != null) letras[k].gameObject.SetActive(false);
    }

    private float Largura(char c)
    {
        Sprite s = fonte.Glifo(c);
        return s != null ? s.rect.width : 3f;
    }

    private Image Letra(int indice)
    {
        while (letras.Count <= indice)
        {
            GameObject obj = new GameObject("Letra", typeof(RectTransform), typeof(Image));
            obj.layer = gameObject.layer;
            obj.transform.SetParent(transform, false);
            RectTransform rt = (RectTransform)obj.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = Vector2.zero;
            Image img = obj.GetComponent<Image>();
            img.raycastTarget = false;
            letras.Add(img);
        }
        return letras[indice];
    }
}
