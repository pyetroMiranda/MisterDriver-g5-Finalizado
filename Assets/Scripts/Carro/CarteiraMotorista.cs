using UnityEngine;
using UnityEngine.Events;

public class CarteiraMotorista : MonoBehaviour
{
    [Header("Pontos")]
    [SerializeField] private int pontosIniciais = 20;
    [SerializeField] private int pontosPorBatida = 1;

    [Header("Batida")]
    [SerializeField] private float velocidadeMinimaBatida = 0.2f;  // encostar devagar não conta
    [SerializeField] private float tempoInvencivel = 1.5f;         // evita perder vários pontos de uma vez

    [Header("Eventos")]
    public UnityEvent<int> aoPerderPontos;      // envia os pontos restantes
    public UnityEvent aoSuspenderCarteira;      // dispara quando chega a 0

    private int pontos;
    private float fimInvencibilidade;
    private bool suspensa;
    private CarroController carro;
    private MolaParaChoque mola;
    private Advogado advogado;
    private int bonusPontos;   // Curso de Direção (melhoria da loja)
    public float TempoInvencivel => tempoInvencivel;

    public bool PodeTomarDano => !suspensa && Time.time >= fimInvencibilidade;

    public int Pontos => pontos;
    public int PontosIniciais => pontosIniciais + bonusPontos;
    public int PontosPorBatida => pontosPorBatida;   // usado pelos pedestres (que tiram o dobro)
    public bool Suspensa => suspensa;

    private void Awake()
    {
        carro = GetComponent<CarroController>();
        mola = GetComponent<MolaParaChoque>();
        advogado = GetComponent<Advogado>();
        pontos = PontosIniciais;
    }

    // Curso de Direção: pontos extras na carteira (pode ser chamado antes ou depois do Awake)
    public void DefinirBonusPontos(int bonus)
    {
        pontos += bonus - bonusPontos;
        bonusPontos = bonus;
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        if (suspensa) return;
        if (Time.time < fimInvencibilidade) return;
        if (colisao.collider.GetComponentInParent<CarroTrafego>() == null) return;
        if (mola != null && mola.Ativa) return;   // o campo de força protege contra batidas
        if (carro != null && carro.VelocidadeAtual < velocidadeMinimaBatida) return;

        PerderPontos(pontosPorBatida);
    }

    public void PerderPontos(int quantidade)
    {
        // Advogado: anula parte do dano
        if (advogado != null) quantidade = advogado.Absorver(quantidade);
        if (quantidade <= 0)
        {
            fimInvencibilidade = Time.time + tempoInvencivel;
            return;
        }

        pontos = Mathf.Max(0, pontos - quantidade);
        fimInvencibilidade = Time.time + tempoInvencivel;
        Debug.Log("Batida! Pontos restantes: " + pontos);

        aoPerderPontos?.Invoke(pontos);

        if (pontos <= 0)
        {
            suspensa = true;
            Debug.Log("Carteira suspensa!");
            aoSuspenderCarteira?.Invoke();
        }
    }
}