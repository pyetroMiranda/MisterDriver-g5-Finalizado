using UnityEngine;

public class VeiculoJogador : MonoBehaviour
{
    [Header("Motor")]
    public float potenciaMotor = 25f;
    public float velocidadeMaxima = 12f;
    public float velocidadeMaximaRe = 5f;
    public float suavizacaoAcelerador = 3f;   // menor = acelera mais devagar

    [Header("Freios")]
    public float forcaFreio = 30f;
    public float forcaFreioDeMao = 18f;
    public float freioMotor = 1.2f;           // desaceleração ao soltar o acelerador
    public float resistenciaAr = 0.35f;

    [Header("Direção")]
    public float velocidadeVirada = 140f;     // graus por segundo
    public float suavizacaoVolante = 5f;      // quão rápido o volante vira
    public float retornoVolante = 7f;         // quão rápido o volante volta ao centro

    [Header("Aderência")]
    public float aderencia = 8f;              // maior = gruda mais
    public float aderenciaFreioDeMao = 1.2f;  // menor = derrapa mais
    public float suavizacaoAderencia = 6f;

    Rigidbody2D rb;
    float entradaAcel, entradaVirar;
    bool freioDeMao;
    float acelSuave, volante, aderenciaAtual;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;     // o arrasto é feito aqui no código
        rb.angularDamping = 0f;
        aderenciaAtual = aderencia;
    }

    void Update()
    {
        entradaAcel = Input.GetAxisRaw("Vertical");
        entradaVirar = Input.GetAxisRaw("Horizontal");
        freioDeMao = Input.GetKey(KeyCode.Space);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 frente = transform.right;                  // sprite apontando para a direita
        float velFrente = Vector2.Dot(rb.linearVelocity, frente);
        float velAbs = Mathf.Abs(velFrente);

        // ---- Acelerador e volante suavizados ----
        acelSuave = Mathf.MoveTowards(acelSuave, entradaAcel, suavizacaoAcelerador * dt);

        float taxaVolante = Mathf.Approximately(entradaVirar, 0f) ? retornoVolante : suavizacaoVolante;
        volante = Mathf.MoveTowards(volante, entradaVirar, taxaVolante * dt);

        // ---- Motor, freio e ré ----
        float forca = 0f;

        if (acelSuave > 0.01f)
        {
            if (velFrente < -0.5f)
                forca = forcaFreio * acelSuave;   // estava de ré: freia primeiro
            else
                forca = potenciaMotor * acelSuave * (1f - Mathf.Clamp01(velFrente / velocidadeMaxima));
        }
        else if (acelSuave < -0.01f)
        {
            float a = -acelSuave;
            if (velFrente > 0.5f)
                forca = -forcaFreio * a;          // estava indo para frente: freia
            else
                forca = -potenciaMotor * 0.6f * a * (1f - Mathf.Clamp01(-velFrente / velocidadeMaximaRe));
        }

        rb.AddForce(frente * forca);

        // ---- Resistência do ar e freio motor ----
        rb.AddForce(-rb.linearVelocity * resistenciaAr);
        if (Mathf.Abs(entradaAcel) < 0.01f)
            rb.AddForce(-rb.linearVelocity * freioMotor);

        // ---- Freio de mão ----
        float aderenciaDesejada = aderencia;
        if (freioDeMao)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, forcaFreioDeMao * dt);
            aderenciaDesejada = aderenciaFreioDeMao;
        }
        aderenciaAtual = Mathf.Lerp(aderenciaAtual, aderenciaDesejada, suavizacaoAderencia * dt);

        // ---- Virar: só com o carro em movimento, e menos em alta velocidade ----
        float fatorBaixa = Mathf.Clamp01(velAbs / 2.5f);                        // parado não vira
        float fatorAlta = Mathf.Lerp(1f, 0.55f, velAbs / velocidadeMaxima);     // rápido vira menos
        float sentido = velFrente >= 0f ? 1f : -1f;                             // na ré, inverte
        float giro = volante * velocidadeVirada * fatorBaixa * fatorAlta * sentido;
        rb.MoveRotation(rb.rotation - giro * dt);

        // ---- Aderência: reduz o deslize lateral ----
        Vector2 lado = transform.up;
        Vector2 lateral = lado * Vector2.Dot(rb.linearVelocity, lado);
        rb.linearVelocity -= lateral * Mathf.Clamp01(aderenciaAtual * dt);
    }
}