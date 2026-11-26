using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]


public class CarroController : MonoBehaviour
{
    [Header("Motor")]
    [SerializeField] private float velocidadeMaxima = 2f;
    [SerializeField] private float aceleracao = 1.5f;
    [SerializeField] private float frenagem = 4f;
    [SerializeField] private float desaceleracao = 0.7f;
    [SerializeField, Range(0f, 1f)] private float velocidadeMaximaRe = 0.4f;

    [Header("Freio de mão")]
    [SerializeField] private float freioDeMao = 6f;

    [Header("Direção")]
    [SerializeField] private float velocidadeCurva = 100f;
    [SerializeField] private float suavizacaoVolante = 5f;
    [SerializeField] private float retornoVolante = 8f;
    [SerializeField, Range(0.2f, 1f)] private float curvaEmAltaVelocidade = 0.6f;

    [Header("Drift")]
    [SerializeField] private float gripNormal = 20f;            // alto = carro "colado" no chão
    [SerializeField] private float gripDrift = 1.5f;            // baixo = desliza muito
    [SerializeField] private float suavizacaoGrip = 6f;         // rapidez da transição entre os dois
    [SerializeField, Range(0.1f, 1f)] private float velocidadeMinimaDrift = 0.5f; // proporção da velocidade máxima
    [SerializeField] private float multiplicadorCurvaDrift = 1.5f; // vira mais durante o drift
    [SerializeField] private float perdaVelocidadeDrift = 0.35f;   // quanto o drift "come" de velocidade
    [SerializeField] private float anguloMinimoMarcas = 12f;       // graus de derrapagem para marcar o chão

    [Header("Marcas de pneu (opcional)")]
    [SerializeField] private TrailRenderer[] marcasPneu;

    private Rigidbody2D corpo;
    private float velocidade;          // velocidade "do motor" na direção do capô
    private Vector2 velocidadeReal;    // para onde o carro realmente se move
    private float volante;
    private float gripAtual;
    private bool acelerar;
    private bool frear;
    private bool freioMao;
    private float direcao;



    public float VelocidadeAtual => velocidadeReal.magnitude;
    public float VelocidadeMaximaBase => velocidadeMaxima;   // sem nitro (usado pelo velocímetro)
    public bool EstaDerrapando { get; private set; }

    public bool Acelerando => acelerar;

    public bool Freando => frear;
    public float VelocidadeComSinal => velocidade;   // negativa = ré

    private void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
        gripAtual = gripNormal;
    }

    private void Update()
    {
        acelerar = false;
        frear = false;
        freioMao = false;
        direcao = 0f;

#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        acelerar = teclado.upArrowKey.isPressed || teclado.wKey.isPressed;
        frear = teclado.downArrowKey.isPressed || teclado.sKey.isPressed;
        freioMao = teclado.spaceKey.isPressed;

        if (teclado.leftArrowKey.isPressed || teclado.aKey.isPressed) direcao += 1f;
        if (teclado.rightArrowKey.isPressed || teclado.dKey.isPressed) direcao -= 1f;
#else
        acelerar = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        frear = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
        freioMao = Input.GetKey(KeyCode.Space);

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) direcao += 1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) direcao -= 1f;
#endif
    }
    public void ReduzirVelocidade(float fator)
    {
        velocidade *= fator;
        velocidadeReal *= fator;
    }
    public bool FreioDeMaoAtivo => freioMao;
    public float MultiplicadorVelocidade { get; set; } = 1f;
    public float MultiplicadorAceleracao { get; set; } = 1f;

    // Melhorias permanentes da loja (aplicadas pelo MelhoriasCarro)
    public float MelhoriaVelocidade { get; set; } = 1f;
    public float MelhoriaAceleracao { get; set; } = 1f;
    public float MelhoriaFreio { get; set; } = 1f;
    public float MelhoriaCurva { get; set; } = 1f;

    // Freios ABS: para o carro na hora
    public void PararImediatamente()
    {
        velocidade = 0f;
        velocidadeReal = Vector2.zero;
        if (corpo != null)
        {
            corpo.linearVelocity = Vector2.zero;
            corpo.angularVelocity = 0f;
        }
    }
    public bool ForcarAcelerar { get; set; }   // o nitro empurra o carro mesmo sem apertar W
    public float Volante => volante;   // -1 (direita) a 1 (esquerda)
    private void FixedUpdate()
    {
        float passo = Time.fixedDeltaTime;
        float limite = Mathf.Max(velocidadeMaxima * MultiplicadorVelocidade * MelhoriaVelocidade, 0.01f);
        float limiteRe = limite * velocidadeMaximaRe;

        // ---------- Drift: freio de mão + velocidade suficiente ----------
        bool driftAtivo = freioMao && velocidade > limite * velocidadeMinimaDrift;

        float gripAlvo = driftAtivo ? gripDrift : gripNormal;
        gripAtual = Mathf.Lerp(gripAtual, gripAlvo, suavizacaoGrip * passo);
        float fatorDrift = Mathf.InverseLerp(gripNormal, gripDrift, gripAtual); // 0 = normal, 1 = drift total

        // ---------- Volante suave ----------
        float taxaVolante = Mathf.Approximately(direcao, 0f) ? retornoVolante : suavizacaoVolante;
        volante = Mathf.MoveTowards(volante, direcao, taxaVolante * passo);

        // ---------- Velocidade do motor ----------
        if (frear)
        {
            if (velocidade > 0.01f)
            {
                velocidade = Mathf.MoveTowards(velocidade, 0f, frenagem * MelhoriaFreio * passo);
            }
            else
            {
                float perc = Mathf.Clamp01(-velocidade / limiteRe);
                float taxaRe = aceleracao * Mathf.Lerp(1f, 0.3f, perc);
                velocidade = Mathf.MoveTowards(velocidade, -limiteRe, taxaRe * passo);
            }
        }
        else if (acelerar || ForcarAcelerar)
        {
            if (velocidade < 0f)
            {
                velocidade = Mathf.MoveTowards(velocidade, 0f, frenagem * passo);
            }
            else
            {
                float perc = Mathf.Clamp01(velocidade / limite);
                float taxa = aceleracao * MultiplicadorAceleracao * MelhoriaAceleracao * Mathf.Lerp(1f, 0.25f, perc);
                velocidade = Mathf.MoveTowards(velocidade, limite, taxa * passo);
            }
        }
        else
        {
            float perc = Mathf.Clamp01(Mathf.Abs(velocidade) / limite);
            float taxa = desaceleracao * Mathf.Lerp(0.6f, 1.4f, perc);
            velocidade = Mathf.MoveTowards(velocidade, 0f, taxa * passo);
        }

        // ---------- Freio de mão: em drift só "come" um pouco de velocidade ----------
        if (freioMao)
        {
            float taxaFreio = driftAtivo ? perdaVelocidadeDrift : freioDeMao;
            velocidade = Mathf.MoveTowards(velocidade, 0f, taxaFreio * passo);
        }

        // ---------- Parado de verdade (motor e deslize) ----------
        if (Mathf.Abs(velocidade) < 0.001f && velocidadeReal.sqrMagnitude < 0.0001f)
        {
            velocidade = 0f;
            velocidadeReal = Vector2.zero;
            corpo.linearVelocity = Vector2.zero;
            corpo.angularVelocity = 0f;
            AtualizarMarcas(false);
            EstaDerrapando = false;
            return;
        }

        // ---------- Curva ----------
        // Usa a velocidade real, assim o carro ainda vira enquanto desliza
        float velCurva = Mathf.Max(Mathf.Abs(velocidade), velocidadeReal.magnitude);
        float fatorBaixa = Mathf.Clamp01(velCurva / (limite * 0.4f));
        float fatorAlta = Mathf.Lerp(1f, curvaEmAltaVelocidade, velCurva / limite);
        float fatorDriftCurva = Mathf.Lerp(1f, multiplicadorCurvaDrift, fatorDrift);
        float sentido = velocidade < -0.001f ? -1f : 1f;

        float angulo = corpo.rotation
            + volante * velocidadeCurva * MelhoriaCurva * fatorBaixa * fatorAlta * fatorDriftCurva
            * sentido * passo;

        // Mesma definição de "frente" do código antigo (transform.up)
        Vector2 frente = (Vector2)(Quaternion.Euler(0f, 0f, angulo) * Vector3.up);

        // ---------- Aderência: o movimento real acompanha o capô aos poucos ----------
        Vector2 velocidadeDesejada = frente * velocidade;
        float seguir = 1f - Mathf.Exp(-gripAtual * passo);
        velocidadeReal = Vector2.Lerp(velocidadeReal, velocidadeDesejada, seguir);

        // Durante o drift, o deslize também perde um pouco de velocidade
        if (fatorDrift > 0.01f)
        {
            velocidadeReal = Vector2.MoveTowards(velocidadeReal, Vector2.zero,
                perdaVelocidadeDrift * fatorDrift * passo);
        }

        corpo.MoveRotation(angulo);
        corpo.MovePosition(corpo.position + velocidadeReal * passo);

        // ---------- Estado de derrapagem (para marcas, som, fumaça) ----------
        float anguloDerrapagem = velocidadeReal.magnitude > 0.05f
            ? Vector2.Angle(frente, velocidadeReal) : 0f;
        // Em ré o ângulo é ~180°, então só conta o deslize lateral
        if (anguloDerrapagem > 90f) anguloDerrapagem = 180f - anguloDerrapagem;

        EstaDerrapando = anguloDerrapagem > anguloMinimoMarcas;
        AtualizarMarcas(EstaDerrapando);
    }

    private void AtualizarMarcas(bool ligado)
    {
        if (marcasPneu == null) return;
        foreach (TrailRenderer marca in marcasPneu)
        {
            if (marca != null) marca.emitting = ligado;
        }
    }

    public void AbsorverBatida(float fator)
    {
        velocidade *= fator;
        velocidadeReal *= fator;
    }

}