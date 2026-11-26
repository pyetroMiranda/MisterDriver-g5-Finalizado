using TMPro;
using UnityEngine;

// Velocímetro do HUD: gira o ponteiro e mostra a velocidade em km/h.
// Funciona em qualquer nível: se o carro não for ligado no Inspector, procura sozinho.
public class Velocimetro : MonoBehaviour
{
    [Header("Ligações")]
    [SerializeField] private CarroController carro;        // opcional: procura sozinho
    [SerializeField] private RectTransform ponteiro;
    [SerializeField] private TMP_Text textoVelocidade;
    [SerializeField] private TMP_Text textoUnidade;

    [Header("Escala")]
    [Tooltip("Quantos km/h o mostrador marca quando o carro está na velocidade máxima normal (sem nitro).")]
    [SerializeField] private float kmhNaVelocidadeMaxima = 80f;
    [Tooltip("Velocidade no fim do mostrador. O nitro passa da velocidade máxima normal.")]
    [SerializeField] private float kmhFimDoMostrador = 160f;
    [SerializeField] private float anguloZero = 135f;       // ponteiro no começo do arco (embaixo à esquerda)
    [SerializeField] private float anguloFim = -135f;       // ponteiro no fim do arco (embaixo à direita)

    [Header("Suavidade")]
    [SerializeField] private float suavizacao = 0.12f;      // menor = ponteiro mais rápido
    [SerializeField] private float tremorNoLimite = 1.5f;   // graus de tremor perto do fim do mostrador

    [Header("Cores")]
    [SerializeField] private Color corNormal = Color.white;
    [SerializeField] private Color corNitro = new Color(0.18f, 0.89f, 0.9f);
    [SerializeField] private Color corUnidade = new Color(0.86f, 0.83f, 0.93f);
    [SerializeField] private Color corRe = new Color(1f, 0.85f, 0.2f);

    private Nitro nitro;
    private float kmhMostrado;
    private float velocidadePonteiro;
    private int ultimoNumero = -1;

    private void Start()
    {
        ProcurarCarro();
        AplicarAngulo(0f);
    }

    private void ProcurarCarro()
    {
        if (carro == null) carro = FindFirstObjectByType<CarroController>();
        if (carro != null) nitro = carro.GetComponent<Nitro>();
    }

    private void Update()
    {
        if (carro == null)
        {
            ProcurarCarro();
            if (carro == null) return;
        }

        float maxima = Mathf.Max(carro.VelocidadeMaximaBase, 0.01f);
        float kmh = carro.VelocidadeAtual / maxima * kmhNaVelocidadeMaxima;

        // Ponteiro com "peso": sobe e desce suave
        kmhMostrado = Mathf.SmoothDamp(kmhMostrado, kmh, ref velocidadePonteiro, suavizacao);
        float fracao = Mathf.Clamp01(kmhMostrado / kmhFimDoMostrador);
        AplicarAngulo(fracao);

        // Número (só reescreve quando muda)
        int numero = Mathf.RoundToInt(kmhMostrado);
        if (textoVelocidade != null)
        {
            if (numero != ultimoNumero)
            {
                textoVelocidade.text = numero.ToString();
                ultimoNumero = numero;
            }
            bool comNitro = nitro != null && nitro.Ativo;
            textoVelocidade.color = Color.Lerp(textoVelocidade.color, comNitro ? corNitro : corNormal, 10f * Time.unscaledDeltaTime);
        }

        // Unidade: mostra "RÉ" quando o carro anda para trás
        if (textoUnidade != null)
        {
            bool emRe = carro.VelocidadeComSinal < -0.05f;
            textoUnidade.text = emRe ? "RÉ" : "km/h";
            textoUnidade.color = emRe ? corRe : corUnidade;
        }
    }

    private void AplicarAngulo(float fracao)
    {
        if (ponteiro == null) return;

        float angulo = Mathf.Lerp(anguloZero, anguloFim, fracao);

        // Perto do fim do mostrador o ponteiro treme, como num carro de verdade
        if (fracao > 0.85f && Time.timeScale > 0f)
            angulo += Random.Range(-tremorNoLimite, tremorNoLimite) * (fracao - 0.85f) / 0.15f;

        ponteiro.localRotation = Quaternion.Euler(0f, 0f, angulo);
    }
}
