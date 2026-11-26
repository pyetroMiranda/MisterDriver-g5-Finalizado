using UnityEngine;

// Ponto do percurso do exame prático. Fica escondido até a MissaoExame ativar.
// Passar: basta o carro entrar na área.  Parar: o carro tem que ficar parado dentro da área por um tempo.
// A área é o retângulo "tamanho", girado junto com o objeto (o desenho verde na aba Scene).
public class PontoExame : MonoBehaviour
{
    public enum Tipo { Passar, Parar }

    // Para "Parar": como o carro tem que estar virado em relação à seta do ponto (o eixo Y dele)
    public enum Orientacao { Qualquer, MesmoSentido, Contrario, Alinhado }

    [SerializeField] private Tipo tipo = Tipo.Passar;
    [SerializeField] private Vector2 tamanho = new Vector2(5f, 5f);
    [SerializeField] private float tempoParado = 1f;
    [SerializeField] private float velocidadeParado = 0.05f;
    [Tooltip("Contrario = o carro tem que entrar de ré. Alinhado = paralelo ao ponto, em qualquer sentido.")]
    [SerializeField] private Orientacao orientacao = Orientacao.Qualquer;

    [Header("Visual (opcional)")]
    [SerializeField] private SpriteRenderer marcacao;
    [SerializeField] private Transform placa;           // ex.: "PARE" ou "VAGA", fica pulando
    [SerializeField] private Color corNormal = new Color(1f, 0.12f, 0.8f, 0.9f);
    [SerializeField] private Color corDentro = new Color(0.3f, 1f, 0.45f, 0.95f);
    [SerializeField] private Color corVirado = new Color(1f, 0.85f, 0.2f, 0.95f);   // dentro, mas virado errado

    private CarroController carro;
    private float tempoDentro;
    private Vector3 escalaPlaca = Vector3.one;

    public bool Concluido { get; private set; }
    public Tipo TipoPonto => tipo;

    public void Definir(Tipo novoTipo, Vector2 novoTamanho, Orientacao novaOrientacao = Orientacao.Qualquer)   // usado pela ferramenta do Editor
    {
        tipo = novoTipo;
        tamanho = novoTamanho;
        orientacao = novaOrientacao;
    }

    public bool ViradoCerto
    {
        get
        {
            if (carro == null || orientacao == Orientacao.Qualquer || tipo == Tipo.Passar) return true;
            float d = Vector2.Dot(carro.transform.up, transform.up);
            switch (orientacao)
            {
                case Orientacao.MesmoSentido: return d > 0.8f;
                case Orientacao.Contrario: return d < -0.8f;
                default: return Mathf.Abs(d) > 0.8f;
            }
        }
    }

    private void Awake()
    {
        if (placa != null) escalaPlaca = placa.localScale;
    }

    public void Ativar()
    {
        Concluido = false;
        tempoDentro = 0f;
        if (carro == null) carro = FindFirstObjectByType<CarroController>();
        gameObject.SetActive(true);
    }

    public void Desativar() => gameObject.SetActive(false);

    private void Update()
    {
        // Pulsa para chamar a atenção
        float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
        bool dentro = carro != null && Dentro(carro.transform.position);

        if (marcacao != null)
        {
            Color c = !dentro ? corNormal : ViradoCerto ? corDentro : corVirado;
            c.a *= Mathf.Lerp(0.55f, 1f, pulso);
            marcacao.color = c;
        }
        if (placa != null) placa.localScale = escalaPlaca * (1f + 0.08f * pulso);
    }

    private void FixedUpdate()
    {
        if (Concluido || carro == null) return;

        if (!Dentro(carro.transform.position))
        {
            tempoDentro = 0f;
            return;
        }

        if (tipo == Tipo.Passar)
        {
            Concluido = true;
            return;
        }

        if (carro.VelocidadeAtual > velocidadeParado || !ViradoCerto)
        {
            tempoDentro = 0f;
            return;
        }

        tempoDentro += Time.fixedDeltaTime;
        if (tempoDentro >= tempoParado) Concluido = true;
    }

    private bool Dentro(Vector3 posicao)
    {
        Vector3 local = transform.InverseTransformPoint(posicao);
        Vector3 escala = transform.lossyScale;
        return Mathf.Abs(local.x * escala.x) <= tamanho.x * 0.5f && Mathf.Abs(local.y * escala.y) <= tamanho.y * 0.5f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = tipo == Tipo.Parar ? new Color(1f, 0.3f, 0.3f, 0.9f) : new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(tamanho.x, tamanho.y, 0.01f));
        Gizmos.matrix = Matrix4x4.identity;
    }
}
