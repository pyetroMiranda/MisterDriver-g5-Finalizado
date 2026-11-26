using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Veículo da Gangue do Escapamento (carro pichado ou moto). Anda pelas ruas da RedeTrafego
// SEM respeitar as leis: corre demais, não para nos cruzamentos, entra na contramão para ultrapassar
// e as motos ainda ficam ziguezagueando. Bater nele (ou ser batido) tira pontos da carteira do jogador.
// Criado pela GangueRebelde quando o Jason sai do mercado.
[RequireComponent(typeof(Rigidbody2D))]
public class CarroRebelde : MonoBehaviour
{
    [SerializeField] private float velocidadeMaxima = 11f;
    [SerializeField] private float aceleracao = 7f;
    [SerializeField] private float giroPorSegundo = 210f;
    [Tooltip("Motos: quanto ziguezagueiam para os lados (unidades).")]
    [SerializeField] private float zigueZague;
    [Tooltip("Chance por segundo de entrar na contramão sem motivo nenhum.")]
    [SerializeField] private float chanceContramao = 0.08f;

    private static readonly List<CarroRebelde> todos = new List<CarroRebelde>();
    public static IReadOnlyList<CarroRebelde> Todos => todos;

    // Avisa a missão quando um rebelde bate no jogador (para o Jason reclamar)
    public static event Action<CarroRebelde> AoBaterNoJogador;

    private readonly Collider2D[] achados = new Collider2D[8];
    private Rigidbody2D corpo;
    private GrafoTrafego grafo;
    private int de, para;
    private int lado = 1;            // 1 = faixa da direita, -1 = contramão
    private float fimContramao;
    private float velocidade;
    private float fase;

    // Som: ronco em loop (moto ou carro de escapamento furado) e a buzina musical quando passa perto
    private AudioSource ronco;
    private float volumeRonco;
    private float proximaProvocacao;

    public void Configurar(float maxima, float giro, float zigue, float contramao)
    {
        velocidadeMaxima = maxima;
        giroPorSegundo = giro;
        zigueZague = zigue;
        chanceContramao = contramao;
    }

    // Começa no ponto "inicio" do grafo, indo para um vizinho
    public void Iniciar(GrafoTrafego g, int inicio)
    {
        grafo = g;
        de = inicio;
        para = Proximo(inicio, -1);
        fase = Random.value * 10f;
        velocidade = velocidadeMaxima * 0.6f;

        Vector2 dir = Direcao();
        Vector2 pos = grafo.Posicao(de) + GrafoTrafego.Direita(dir) * grafo.Faixa + dir * 2f;
        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 0f, angulo));
        if (corpo == null) corpo = GetComponent<Rigidbody2D>();
        corpo.position = pos;
        corpo.rotation = angulo;

        if (ronco == null)
        {
            string som = zigueZague > 0f ? "MotoLoop" : "CarroRebeldeLoop";
            ronco = Sons.CriarLoop(gameObject, som);
            volumeRonco = Sons.VolumeDe(som);
        }
        proximaProvocacao = Time.time + Random.Range(2f, 6f);
    }

    private void Update()
    {
        if (ronco == null) return;
        if (Time.timeScale <= 0.001f) { ronco.volume = 0f; return; }
        float fator = Sons.FatorDistancia(transform.position, 32f, out float lado);
        float forca = Mathf.Clamp01(velocidade / Mathf.Max(1f, velocidadeMaxima));
        ronco.volume = volumeRonco * fator * (0.55f + 0.45f * forca);
        ronco.panStereo = lado;
        ronco.pitch = (zigueZague > 0f ? 0.85f : 0.8f) + 0.5f * forca;

        // Passou perto da câmera: buzina provocando
        if (fator > 0.45f && Time.time >= proximaProvocacao)
        {
            proximaProvocacao = Time.time + Random.Range(7f, 13f);
            Sons.TocarEm("BuzinaRebelde", transform.position, 1f, zigueZague > 0f ? 1.15f : 0.95f, 28f);
        }
    }

    private void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Kinematic;
        corpo.useFullKinematicContacts = true;
        corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void OnEnable() => todos.Add(this);
    private void OnDisable() => todos.Remove(this);

    private Vector2 Direcao()
    {
        Vector2 d = grafo.Posicao(para) - grafo.Posicao(de);
        return d.sqrMagnitude > 0.001f ? d.normalized : Vector2.up;
    }

    // Escolhe a próxima esquina sem pensar duas vezes (só volta se for rua sem saída)
    private int Proximo(int ponto, int anterior)
    {
        List<int> v = grafo.Vizinhos(ponto);
        if (v == null || v.Count == 0) return ponto;
        if (v.Count == 1) return v[0];
        int escolha;
        int tentativas = 0;
        do { escolha = v[Random.Range(0, v.Count)]; } while (escolha == anterior && ++tentativas < 10);
        return escolha;
    }

    private void FixedUpdate()
    {
        if (grafo == null || de == para) return;
        float dt = Time.fixedDeltaTime;

        Vector2 pos = corpo.position;
        Vector2 a = grafo.Posicao(de), b = grafo.Posicao(para);
        Vector2 dir = Direcao();
        float comprimento = Vector2.Distance(a, b);
        float andado = Vector2.Dot(pos - a, dir);

        // Chegou na esquina: segue para qualquer lado (sem parar, sem olhar)
        if (andado >= comprimento - 1.2f)
        {
            int anterior = de;
            de = para;
            para = Proximo(para, anterior);
            return;
        }

        // Contramão: para ultrapassar ou só por bagunça
        bool bloqueado = TemAlguemNaFrente(pos);
        if (bloqueado && lado == 1) { lado = -1; fimContramao = Time.time + Random.Range(2f, 3.5f); }
        else if (lado == 1 && Random.value < chanceContramao * dt) { lado = -1; fimContramao = Time.time + Random.Range(1.5f, 3f); }
        else if (lado == -1 && Time.time > fimContramao && !bloqueado) lado = 1;

        Vector2 lateral = GrafoTrafego.Direita(dir) * (grafo.Faixa * lado + Mathf.Sin(Time.time * 4f + fase) * zigueZague);
        Vector2 alvo = a + dir * Mathf.Min(comprimento, andado + 4.5f) + lateral;
        Vector2 desejada = alvo - pos;
        float anguloAlvo = Mathf.Atan2(desejada.y, desejada.x) * Mathf.Rad2Deg - 90f;
        float angulo = Mathf.MoveTowardsAngle(corpo.rotation, anguloAlvo, giroPorSegundo * dt);

        // Só tira o pé quando a curva é fechada (ou quando não tem como passar)
        float curva = Mathf.Abs(Mathf.DeltaAngle(corpo.rotation, anguloAlvo));
        float alvoVelocidade = velocidadeMaxima * (curva > 50f ? 0.55f : 1f) * (bloqueado && lado == -1 ? 0.4f : 1f);
        velocidade = Mathf.MoveTowards(velocidade, alvoVelocidade, aceleracao * dt);

        Vector2 frente = Quaternion.Euler(0f, 0f, angulo) * Vector3.up;
        corpo.MoveRotation(angulo);
        corpo.MovePosition(pos + frente * velocidade * dt);
    }

    private bool TemAlguemNaFrente(Vector2 pos)
    {
        Vector2 frente = transform.up;
        int n = Physics2D.OverlapCircleNonAlloc(pos + frente * 3.2f, 1.2f, achados);
        for (int i = 0; i < n; i++)
        {
            Collider2D c = achados[i];
            if (c == null || c.isTrigger || c.attachedRigidbody == corpo) continue;
            if (c.GetComponentInParent<CarroTrafego>() != null) return true;
            CarroRebelde outro = c.GetComponentInParent<CarroRebelde>();
            if (outro != null && outro != this) return true;
        }
        return false;
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        CarteiraMotorista carteira = colisao.collider.GetComponentInParent<CarteiraMotorista>();
        if (carteira == null || carteira.Suspensa || !carteira.PodeTomarDano) return;

        AoBaterNoJogador?.Invoke(this);
        carteira.PerderPontos(carteira.PontosPorBatida);
        velocidade *= 0.3f;
    }
}
