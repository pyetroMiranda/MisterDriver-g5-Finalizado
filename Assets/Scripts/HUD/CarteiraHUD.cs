using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Carteira de motorista no HUD: mostra os pontos como uma barra de "vida" em blocos.
// Ao perder pontos, a carteira treme e os blocos perdidos "estouram".
// Procura sozinha a CarteiraMotorista do carro.
public class CarteiraHUD : MonoBehaviour
{
    [Header("Ligações")]
    [SerializeField] private CarteiraMotorista carteira;   // opcional: procura sozinho
    [SerializeField] private RectTransform cartao;         // o que treme
    [SerializeField] private TMP_Text textoPontos;
    [SerializeField] private Transform barra;
    [SerializeField] private Image modeloBloco;

    [Header("Cores")]
    [SerializeField] private Color corCheio = new Color(0.18f, 0.89f, 0.9f);    // ciano
    [SerializeField] private Color corMetade = new Color(1f, 0.85f, 0.2f);      // amarelo
    [SerializeField] private Color corPerigo = new Color(1f, 0.25f, 0.35f);     // vermelho
    [SerializeField] private Color corVazio = new Color(0.16f, 0.08f, 0.22f);
    [SerializeField] private int pontosEmPerigo = 5;

    [Header("Tremor")]
    [SerializeField] private float duracaoTremor = 0.35f;
    [SerializeField] private float forcaTremor = 10f;

    private readonly List<Image> blocos = new List<Image>();
    private Vector2 posicaoOriginal;
    private Coroutine tremendo;
    private int pontosAtuais;

    private void Start()
    {
        if (carteira == null) carteira = FindFirstObjectByType<CarteiraMotorista>();
        if (carteira == null)
        {
            gameObject.SetActive(false);   // nível sem carteira: esconde
            return;
        }

        if (cartao != null) posicaoOriginal = cartao.anchoredPosition;
        CriarBlocos(carteira.PontosIniciais);

        pontosAtuais = carteira.Pontos;
        Atualizar(pontosAtuais, false);
        carteira.aoPerderPontos.AddListener(AoPerderPontos);
    }

    private void OnDestroy()
    {
        if (carteira != null) carteira.aoPerderPontos.RemoveListener(AoPerderPontos);
    }

    private void Update()
    {
        // Pisca o texto quando está em perigo
        if (textoPontos != null && carteira != null && pontosAtuais <= pontosEmPerigo)
            textoPontos.alpha = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 8f);
    }

    private void CriarBlocos(int quantidade)
    {
        if (barra == null || modeloBloco == null) return;
        modeloBloco.gameObject.SetActive(false);

        for (int i = 0; i < quantidade; i++)
        {
            Image bloco = Instantiate(modeloBloco, barra);
            bloco.gameObject.SetActive(true);
            bloco.name = "Bloco " + (i + 1);
            blocos.Add(bloco);
        }
    }

    private void AoPerderPontos(int restantes)
    {
        int antes = pontosAtuais;
        pontosAtuais = restantes;
        Atualizar(restantes, true);

        // Blocos perdidos "estouram"
        for (int i = restantes; i < antes && i < blocos.Count; i++)
            StartCoroutine(Estourar(blocos[i]));

        if (cartao != null)
        {
            if (tremendo != null) StopCoroutine(tremendo);
            tremendo = StartCoroutine(Tremer());
        }
    }

    private void Atualizar(int pontos, bool animado)
    {
        int total = Mathf.Max(1, carteira.PontosIniciais);
        Color cor = pontos <= pontosEmPerigo ? corPerigo : (pontos <= total / 2 ? corMetade : corCheio);

        for (int i = 0; i < blocos.Count; i++)
            blocos[i].color = i < pontos ? cor : corVazio;

        if (textoPontos != null)
        {
            textoPontos.text = pontos <= 0 ? "SUSPENSA!" : pontos + "/" + total + " PONTOS";
            textoPontos.color = pontos <= pontosEmPerigo ? corPerigo : Color.white;
            if (pontos > pontosEmPerigo) textoPontos.alpha = 1f;
        }
    }

    private IEnumerator Estourar(Image bloco)
    {
        RectTransform rt = bloco.rectTransform;
        Color final = bloco.color;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.3f);
            rt.localScale = Vector3.one * (1f + 0.6f * Mathf.Sin(k * Mathf.PI));
            bloco.color = Color.Lerp(Color.white, final, k);
            yield return null;
        }
        rt.localScale = Vector3.one;
        bloco.color = final;
    }

    private IEnumerator Tremer()
    {
        float t = 0f;
        while (t < duracaoTremor)
        {
            t += Time.unscaledDeltaTime;
            float forca = forcaTremor * (1f - t / duracaoTremor);
            cartao.anchoredPosition = posicaoOriginal + Random.insideUnitCircle * forca;
            yield return null;
        }
        cartao.anchoredPosition = posicaoOriginal;
        tremendo = null;
    }
}
