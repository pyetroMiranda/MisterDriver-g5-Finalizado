using System.Collections;
using UnityEngine;

// Anda de quadradinho em quadradinho na MapaCasa (usado pelo Jason e pelos NPCs).
[RequireComponent(typeof(AnimadorPersonagem))]
public class MovimentoGrade : MonoBehaviour
{
    [SerializeField] private float quadradosPorSegundo = 4.5f;

    private MapaCasa mapa;
    private AnimadorPersonagem animador;

    public Vector2Int Celula { get; private set; }
    public Vector2Int Destino { get; private set; }
    public bool Andando { get; private set; }
    public Direcao Olhando => animador.Direcao;
    public Vector2Int Frente => Celula + MapaCasa.Vetor(Olhando);

    private void Awake()
    {
        animador = GetComponent<AnimadorPersonagem>();
        mapa = FindFirstObjectByType<MapaCasa>();
        Celula = Destino = MapaCasa.ParaCelula(transform.position);
        transform.position = MapaCasa.ParaMundo(Celula);
    }

    private void OnEnable() { if (mapa != null) mapa.Registrar(this); }
    private void OnDisable() { if (mapa != null) mapa.Remover(this); }

    public void Virar(Direcao d) => animador.Direcao = d;

    public void VirarPara(Vector2Int alvo)
    {
        Vector2Int d = alvo - Celula;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) Virar(d.x > 0 ? Direcao.Direita : Direcao.Esquerda);
        else if (d.y != 0) Virar(d.y > 0 ? Direcao.Baixo : Direcao.Cima);
    }

    public bool PodeAndar(Direcao d) => mapa != null && mapa.Livre(Celula + MapaCasa.Vetor(d), this);

    // Um passo; se o caminho estiver fechado só vira para aquele lado
    private bool passoAlternado;

    public IEnumerator Passo(Direcao d, float multiplicador = 1f)
    {
        Virar(d);
        if (Andando || !PodeAndar(d)) yield break;

        Andando = true;
        Destino = Celula + MapaCasa.Vetor(d);
        Vector3 inicio = MapaCasa.ParaMundo(Celula);
        Vector3 fim = MapaCasa.ParaMundo(Destino);

        float t = 0f;
        bool pisou = false;
        while (t < 1f)
        {
            t += Time.deltaTime * quadradosPorSegundo * multiplicador;
            if (!pisou && t >= 0.5f)
            {
                pisou = true;
                // O Jason pisa mais alto; os outros personagens bem baixinho
                bool jogador = GetComponent<JogadorCasa>() != null;
                Sons.TocarEm(passoAlternado ? "Passo" : "Passo2", transform.position, jogador ? 1f : 0.45f, multiplicador > 1.2f ? 1.1f : 1f);
                passoAlternado = !passoAlternado;
            }
            transform.position = Vector3.Lerp(inicio, fim, Mathf.Clamp01(t));
            animador.ProgressoPasso(t);
            yield return null;
        }

        Celula = Destino;
        transform.position = fim;
        animador.TerminouPasso();
        Andando = false;
    }

    // Anda vários passos numa direção (para as cenas)
    public IEnumerator Andar(Direcao d, int passos, float multiplicador = 1f)
    {
        for (int i = 0; i < passos; i++)
        {
            if (!PodeAndar(d)) { Virar(d); yield break; }
            yield return Passo(d, multiplicador);
        }
    }
}
