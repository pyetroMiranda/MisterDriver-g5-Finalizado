using System.Collections;
using UnityEngine;
using TMPro;

public class CarteiraUI : MonoBehaviour
{
    [Header("Ligação")]
    [SerializeField] private CarteiraMotorista carteira;   // arraste o CarroNissan_0

    [Header("Interface")]
    [SerializeField] private RectTransform cartao;         // o objeto Carteira (imagem)
    [SerializeField] private TMP_Text textoPontos;         // o TextoPontos

    [Header("Aviso")]
    [SerializeField] private int pontosEmAviso = 5;        // abaixo disso o texto fica vermelho
    [SerializeField] private Color corNormal = Color.white;
    [SerializeField] private Color corAviso = new Color(0.9f, 0.2f, 0.2f);

    [Header("Tremor ao perder pontos")]
    [SerializeField] private float duracaoTremor = 0.35f;
    [SerializeField] private float forcaTremor = 12f;

    private Vector2 posicaoOriginal;
    private Coroutine tremendo;

    private void Start()
    {
        if (cartao != null) posicaoOriginal = cartao.anchoredPosition;
        Atualizar(carteira.Pontos);
    }

    private void OnEnable()
    {
        carteira.aoPerderPontos.AddListener(AoPerderPontos);
    }

    private void OnDisable()
    {
        if (carteira != null) carteira.aoPerderPontos.RemoveListener(AoPerderPontos);
    }

    private void AoPerderPontos(int restantes)
    {
        Atualizar(restantes);

        if (cartao != null)
        {
            if (tremendo != null) StopCoroutine(tremendo);
            tremendo = StartCoroutine(Tremer());
        }
    }

    private void Atualizar(int pontos)
    {
        if (textoPontos == null) return;
        textoPontos.text = pontos + " / " + carteira.PontosIniciais;
        textoPontos.color = pontos <= pontosEmAviso ? corAviso : corNormal;
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