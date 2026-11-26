using System.Collections.Generic;
using UnityEngine;

// Carro do trânsito. Anda pela rede de ruas (RedeTrafego) na faixa da DIREITA, faz curvas suaves dentro dos
// cruzamentos, escolhe um caminho diferente a cada esquina, espera a vez, mantém distância do carro da frente
// e para para pedestres e para o jogador. Cada carro tem uma "personalidade" (pressa, distância, reação).
// Ainda funciona com as rotas antigas (campo "rota": objeto com pontos filhos, que vira uma volta fechada).
[RequireComponent(typeof(Rigidbody2D))]
public class CarroTrafego : MonoBehaviour
{
    [Header("Ruas")]
    [Tooltip("Rede de ruas por onde o carro anda. O GeradorTrafego preenche sozinho.")]
    [SerializeField] private RedeTrafego rede;
    [Tooltip("Modo antigo: objeto pai cujos filhos são os pontos de uma volta fechada.")]
    [SerializeField] private Transform rota;
    [Tooltip("Modo antigo: distância do meio da rua até o meio da faixa da direita (0 = anda no meio da rua).")]
    [SerializeField, Min(0f)] private float faixaRotaAntiga = 2.75f;
    [Tooltip("Modo antigo: a que distância de cada ponto o carro começa a curva.")]
    [SerializeField, Min(0f)] private float raioCurvaRotaAntiga = 5.5f;

    [Header("Velocidade")]
    [SerializeField] private float velocidadeMaxima = 6f;
    [SerializeField] private float aceleracao = 3f;
    [SerializeField] private float frenagem = 6f;
    [Tooltip("Maior = faz as curvas mais rápido.")]
    [SerializeField] private float aceleracaoNaCurva = 3.5f;

    [Header("Personalidade (sorteada ao nascer)")]
    [Tooltip("Multiplica a velocidade máxima (mínimo e máximo).")]
    [SerializeField] private Vector2 pressa = new Vector2(0.8f, 1.15f);
    [Tooltip("Espaço que deixa até o carro (ou pessoa) da frente.")]
    [SerializeField] private Vector2 distanciaSeguranca = new Vector2(1.2f, 2.6f);
    [Tooltip("Quanto demora para arrancar depois de parado (segundos).")]
    [SerializeField] private Vector2 tempoReacao = new Vector2(0.15f, 0.6f);

    [Header("Direção")]
    [SerializeField] private float velocidadeGiro = 220f;   // graus por segundo
    [Tooltip("Quanto o carro puxa de volta para o meio da faixa.")]
    [SerializeField] private float correcaoFaixa = 2.5f;

    [Header("Cruzamentos")]
    [Tooltip("Onde para esperando a vez, contando da entrada do cruzamento (antes da faixa de pedestres).")]
    [SerializeField] private float paradaAntesDoCruzamento = 5f;
    [Tooltip("Depois de esperar isso (segundos), entra de qualquer jeito (evita trânsito travado).")]
    [SerializeField] private float esperaMaxima = 5f;

    [Header("Buzina")]
    [SerializeField] private float frenagemParada = 8f;   // quão forte ele freia ao ouvir a buzina

    [Header("Empurrão (mola-de-para-choque)")]
    [SerializeField] private float amortecimentoEmpurrao = 4f;   // maior = o empurrão acaba mais rápido
    [SerializeField] private float tempoEntreEmpurroes = 0.5f;   // evita empurrar várias vezes seguidas

    // ---------- Caminho planejado ----------

    private struct Ponto
    {
        public Vector2 p;
        public float s;          // distância percorrida até aqui (ao longo do caminho)
        public float vMax;       // velocidade máxima neste ponto (curvas)
        public int no;           // ponto da rede cujo cruzamento este trecho atravessa (-1 = rua normal)
        public int conversao;    // Cruzamento.Reto / Esquerda / Direita / Retorno
    }

    private const float SemLimite = 999f;
    private const float Planejar = 50f;      // quanto de caminho manter planejado à frente
    private const float GuardarAtras = 15f;  // quanto de caminho manter atrás (para saber quando saiu do cruzamento)

    private readonly List<Ponto> caminho = new List<Ponto>();
    private GrafoTrafego grafo;
    private int noAnterior = -1;   // o caminho termina na entrada do cruzamento "noAlvo", vindo de "noAnterior"
    private int noAlvo = -1;
    private int indice;            // trecho do caminho onde o carro está
    private float sAtual;
    private float erroLateral;     // + = carro à esquerda do caminho

    // ---------- Estado ----------

    private Rigidbody2D corpo;
    private float velocidade;
    private float giroAtual;
    private float fimParada;
    private Vector2 empurrao;
    private float proximoEmpurrao;

    private float fatorPressa = 1f;
    private float seguranca = 1.8f;
    private float reacao = 0.3f;
    private float meioComprimento = 2f;
    private float meioLargura = 1.1f;

    private float distanciaLivre = SemLimite;   // espaço livre à frente do para-choque
    private bool bloqueadoPorCarro;
    private bool bloqueadoPeloJogador;
    private float jogadorNaFrenteDesde = -1f;
    private float proximaBuzina;
    private float tomBuzina = 1f;
    private float bloqueadoDesde = -1f;
    private float ignorarCarrosAte;
    private float proximaVisao;
    private float alcanceExtra;
    private float paradoDesde;
    private float ultimoMovimento;   // última vez que andou de verdade (o GeradorTrafego recicla quem fica travado)

    private readonly List<Cruzamento> reservas = new List<Cruzamento>();
    private Cruzamento esperando;
    private float esperandoDesde = -1f;
    private float limiteCruzamento = SemLimite;   // distância até a linha de parada (se ainda não tem a vez)

    // Todos os carros do trânsito ativos (para enxergar uns aos outros sem procurar na cena)
    private static readonly List<CarroTrafego> todos = new List<CarroTrafego>();

    // ---------- Para os outros scripts ----------

    public Transform Rota => rota;   // usado pelo GPS (modo antigo)
    public RedeTrafego Rede => rede;
    public float VelocidadeAtual => velocidade;
    public bool Parado => Time.time < fimParada;
    public bool PodeSerEmpurrado => Time.time >= proximoEmpurrao;
    public bool Freando { get; private set; }
    public int Seta { get; private set; }            // -1 esquerda, 0 nenhuma, 1 direita
    public float FatorModelo { get; set; } = 1f;     // ônibus é mais lento, esportivo mais rápido (AparenciaCarro)
    public float TempoParado => Time.time - ultimoMovimento;   // há quantos segundos está sem sair do lugar

    // Chamado pela mola-de-para-choque: joga o carro para longe do jogador
    public void Empurrar(Vector2 impulso)
    {
        if (!PodeSerEmpurrado) return;

        empurrao = impulso;
        velocidade = 0f;
        proximoEmpurrao = Time.time + tempoEntreEmpurroes;
    }

    // Chamado pela buzina do jogador: o carro freia e fica parado por alguns segundos
    public void Parar(float segundos)
    {
        fimParada = Mathf.Max(fimParada, Time.time + segundos);
    }

    // Chamado pelo GeradorTrafego: começa na faixa que vai do ponto "de" para o ponto "para"
    public void Iniciar(RedeTrafego novaRede, int de, int para, Vector2 posicao)
    {
        rede = novaRede;
        rota = null;
        grafo = rede.Grafo;
        Comecar(de, para, posicao);
    }

    // ---------- Ciclo ----------

    private void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Kinematic;
        corpo.gravityScale = 0f;

        fatorPressa = Random.Range(pressa.x, pressa.y);
        tomBuzina = Random.Range(0.85f, 1.15f);   // cada carro tem a sua buzina
        seguranca = Random.Range(distanciaSeguranca.x, distanciaSeguranca.y);
        reacao = Random.Range(tempoReacao.x, tempoReacao.y);
    }

    private void OnEnable()
    {
        if (!todos.Contains(this)) todos.Add(this);
    }

    private void OnDisable()
    {
        todos.Remove(this);
        foreach (Cruzamento c in reservas) c.Sair(this);
        reservas.Clear();
        if (esperando != null) esperando.Desistir(this);
        esperando = null;
    }

    private void Start()
    {
        MedirTamanho();
        if (grafo != null) return;   // já iniciado pelo gerador

        if (rede != null) grafo = rede.Grafo;
        else if (rota != null) grafo = GrafoDaRotaAntiga(rota, faixaRotaAntiga, raioCurvaRotaAntiga);

        if (grafo == null || grafo.Quantidade < 2)
        {
            Debug.LogWarning("CarroTrafego: sem rede de ruas (RedeTrafego) nem rota.", this);
            enabled = false;
            return;
        }

        ProcurarLugar();
    }

    private void MedirTamanho()
    {
        BoxCollider2D caixa = GetComponent<BoxCollider2D>();
        if (caixa == null) caixa = GetComponentInChildren<BoxCollider2D>();
        if (caixa == null) return;

        Vector3 escala = caixa.transform.lossyScale;
        meioLargura = Mathf.Max(0.3f, Mathf.Abs(caixa.size.x * escala.x) * 0.5f);
        meioComprimento = Mathf.Max(0.5f, Mathf.Abs(caixa.size.y * escala.y) * 0.5f);
    }

    // ---------- Começo ----------

    // Carro colocado à mão na cena: encaixa na faixa mais perto que vai para onde ele está virado
    private void ProcurarLugar()
    {
        Vector2 posicao = corpo.position;
        Vector2 frente = transform.up;
        float melhor = float.MaxValue;
        int melhorDe = -1, melhorPara = -1;
        Vector2 melhorPonto = posicao;

        for (int a = 0; a < grafo.Quantidade; a++)
        {
            foreach (int b in grafo.Vizinhos(a))
            {
                if (!grafo.TrechoDaFaixa(a, b, out Vector2 inicio, out Vector2 fim)) continue;
                Vector2 ponto = MaisPertoNoSegmento(posicao, inicio, fim);
                float custo = Vector2.Distance(posicao, ponto);
                if (Vector2.Dot((fim - inicio).normalized, frente) < 0.3f) custo += 100f;   // sentido errado
                if (custo < melhor)
                {
                    melhor = custo;
                    melhorDe = a;
                    melhorPara = b;
                    melhorPonto = ponto;
                }
            }
        }

        if (melhorDe < 0)
        {
            Debug.LogWarning("CarroTrafego: a rede de ruas não tem nenhuma rua ligada.", this);
            enabled = false;
            return;
        }

        Comecar(melhorDe, melhorPara, melhorPonto);
    }

    private void Comecar(int de, int para, Vector2 posicao)
    {
        if (corpo == null) corpo = GetComponent<Rigidbody2D>();

        foreach (Cruzamento c in reservas) c.Sair(this);
        reservas.Clear();
        Desistir();
        caminho.Clear();
        empurrao = Vector2.zero;
        fimParada = 0f;
        ignorarCarrosAte = 0f;
        bloqueadoDesde = -1f;
        distanciaLivre = SemLimite;
        limiteCruzamento = SemLimite;
        ultimoMovimento = Time.time;

        grafo.TrechoDaFaixa(de, para, out Vector2 inicio, out Vector2 fim);
        Vector2 d = (fim - inicio).sqrMagnitude > 0.0001f ? (fim - inicio).normalized : (Vector2)transform.up;
        posicao = MaisPertoNoSegmento(posicao, inicio, fim);

        noAnterior = de;
        noAlvo = para;
        Adicionar(posicao, SemLimite, -1, Cruzamento.Reto);
        AdicionarReta(posicao, fim, -1);

        indice = 0;
        sAtual = 0f;
        float angulo = Angulo(d);
        transform.SetPositionAndRotation(posicao, Quaternion.Euler(0f, 0f, angulo));
        corpo.position = posicao;
        corpo.rotation = angulo;
        velocidade = 0f;
        paradoDesde = Time.time - Random.Range(0f, 0.5f);
    }

    // ---------- Planejamento ----------

    // Escolhe a próxima rua no cruzamento "noAlvo" e acrescenta a curva + a reta até o cruzamento seguinte
    private void PlanejarProximo()
    {
        int b = noAlvo;
        List<int> opcoes = grafo.Vizinhos(b);
        Vector2 dEntrada = (grafo.Posicao(b) - grafo.Posicao(noAnterior)).normalized;

        // Sorteio com preferência por seguir reto (nunca volta, a não ser em rua sem saída)
        int c = noAnterior;
        float total = 0f;
        foreach (int op in opcoes)
            if (op != noAnterior) total += Peso(dEntrada, b, op);
        float sorteio = Random.value * total;
        foreach (int op in opcoes)
        {
            if (op == noAnterior) continue;
            sorteio -= Peso(dEntrada, b, op);
            c = op;
            if (sorteio <= 0f) break;
        }

        Vector2 dSaida = (grafo.Posicao(c) - grafo.Posicao(b)).normalized;
        Vector2 entrada = caminho[caminho.Count - 1].p;
        grafo.TrechoDaFaixa(b, c, out Vector2 saida, out Vector2 fimProximo);

        int conversao = TipoDeConversao(dEntrada, dSaida, c == noAnterior);
        int noCruzamento = grafo.CruzamentoDe(b) != null ? b : -1;
        AdicionarCurva(entrada, dEntrada, saida, dSaida, noCruzamento, conversao);

        // A reta continua "dentro" do cruzamento se os dois pontos fazem parte do mesmo (ruas muito curtas)
        Cruzamento cb = grafo.CruzamentoDe(b);
        int noReta = cb != null && cb == grafo.CruzamentoDe(c) ? b : -1;
        AdicionarReta(saida, fimProximo, noReta);

        noAnterior = b;
        noAlvo = c;
    }

    private float Peso(Vector2 dEntrada, int b, int op)
    {
        Vector2 dSaida = (grafo.Posicao(op) - grafo.Posicao(b)).normalized;
        return 1f + 0.7f * Mathf.Max(0f, Vector2.Dot(dEntrada, dSaida));   // reto: 1,7  curva: 1
    }

    private static int TipoDeConversao(Vector2 dEntrada, Vector2 dSaida, bool voltando)
    {
        float dot = Vector2.Dot(dEntrada, dSaida);
        if (voltando || dot < -0.9f) return Cruzamento.Retorno;
        if (dot > 0.9f) return Cruzamento.Reto;
        float cruz = dEntrada.x * dSaida.y - dEntrada.y * dSaida.x;
        return cruz > 0f ? Cruzamento.Esquerda : Cruzamento.Direita;
    }

    // Curva suave (Bézier) da entrada à saída do cruzamento
    private void AdicionarCurva(Vector2 inicio, Vector2 dEntrada, Vector2 fim, Vector2 dSaida, int no, int conversao)
    {
        float corda = Vector2.Distance(inicio, fim);
        float angulo = Vector2.Angle(dEntrada, dSaida) * Mathf.Deg2Rad;
        if (corda < 0.05f) return;

        if (angulo < 0.15f)
        {
            AdicionarReta(inicio, fim, no);
            return;
        }

        // Raio da curva e velocidade segura nela
        float seno = Mathf.Max(0.05f, Mathf.Sin(angulo * 0.5f));
        float raio = corda / (2f * seno);
        float vCurva = Mathf.Max(1.5f, Mathf.Sqrt(aceleracaoNaCurva * raio));

        // Alças da Bézier para parecer um arco de círculo
        float k = corda * (2f / 3f) * Mathf.Tan(angulo / 4f) / seno;
        Vector2 p1 = inicio + dEntrada * k;
        Vector2 p2 = fim - dSaida * k;

        int partes = Mathf.Max(6, Mathf.CeilToInt(corda * 1.6f / 0.4f));
        for (int i = 1; i <= partes; i++)
        {
            float t = i / (float)partes;
            float u = 1f - t;
            Vector2 p = u * u * u * inicio + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * fim;
            Adicionar(p, vCurva, no, conversao);
        }
    }

    private void AdicionarReta(Vector2 inicio, Vector2 fim, int no)
    {
        float comprimento = Vector2.Distance(inicio, fim);
        if (caminho.Count > 0 && Vector2.Distance(caminho[caminho.Count - 1].p, inicio) > 0.01f)
            Adicionar(inicio, SemLimite, no, Cruzamento.Reto);

        int partes = Mathf.Max(1, Mathf.CeilToInt(comprimento / 0.75f));
        for (int i = 1; i <= partes; i++)
            Adicionar(Vector2.Lerp(inicio, fim, i / (float)partes), SemLimite, no, Cruzamento.Reto);
    }

    private void Adicionar(Vector2 p, float vMax, int no, int conversao)
    {
        float s = 0f;
        if (caminho.Count > 0)
        {
            Ponto ultimo = caminho[caminho.Count - 1];
            float d = Vector2.Distance(ultimo.p, p);
            if (d < 0.01f) return;
            s = ultimo.s + d;
        }
        caminho.Add(new Ponto { p = p, s = s, vMax = vMax, no = no, conversao = conversao });
    }

    // ---------- Movimento ----------

    private void FixedUpdate()
    {
        if (grafo == null || caminho.Count < 2) return;
        float dt = Time.fixedDeltaTime;
        if (dt <= 0f) return;

        for (int i = 0; i < 6 && caminho[caminho.Count - 1].s - sAtual < Planejar; i++) PlanejarProximo();
        AtualizarProgresso();

        if (Time.time >= proximaVisao)
        {
            proximaVisao = Time.time + 0.1f;
            Olhar();
        }
        AtualizarCruzamentos();
        AtualizarSeta();

        // ---------- Velocidade ----------
        float alvo = VelocidadeAlvo();
        float taxa = alvo > velocidade ? aceleracao : (alvo < velocidade - 2f ? frenagem * 1.6f : frenagem);

        if (Parado)
        {
            alvo = 0f;
            taxa = frenagemParada;
        }

        // Tempo de reação: parado, demora um pouco para arrancar
        if (velocidade < 0.05f && alvo > 0.3f && Time.time - paradoDesde < reacao) alvo = 0f;
        if (velocidade > 0.05f || alvo < 0.3f) paradoDesde = Time.time;

        velocidade = Mathf.MoveTowards(velocidade, alvo, taxa * dt);
        if (velocidade > 0.5f) ultimoMovimento = Time.time;
        Freando = velocidade < 0.15f || alvo < velocidade - 0.15f;

        // ---------- Direção: segue a tangente do caminho um pouco à frente e corrige o desvio da faixa ----------
        Vector2 tangente = DirecaoEm(sAtual + 0.2f + velocidade * 0.12f);
        float correcao = Mathf.Atan2(correcaoFaixa * erroLateral, Mathf.Max(velocidade, 1.5f)) * Mathf.Rad2Deg;
        float desejado = Angulo(tangente) - correcao;
        float diferenca = Mathf.DeltaAngle(corpo.rotation, desejado);
        float movimento = Mathf.Clamp01(velocidade / 0.4f);
        float giroDesejado = Mathf.Clamp(diferenca * 10f, -velocidadeGiro, velocidadeGiro) * movimento;
        giroAtual = Mathf.Lerp(giroAtual, giroDesejado, 25f * dt);

        float novoAngulo = corpo.rotation + giroAtual * dt;
        Vector2 frente = (Vector2)(Quaternion.Euler(0f, 0f, novoAngulo) * Vector3.up);
        corpo.MoveRotation(novoAngulo);
        corpo.MovePosition(corpo.position + (frente * velocidade + empurrao) * dt);

        // O empurrão vai perdendo força aos poucos
        empurrao *= Mathf.Exp(-amortecimentoEmpurrao * dt);
        if (empurrao.sqrMagnitude < 0.0001f) empurrao = Vector2.zero;
    }

    private float VelocidadeAlvo()
    {
        float b = frenagem * 0.6f;   // frenagem confortável para planejar
        float alvo = velocidadeMaxima * fatorPressa * FatorModelo;

        // Curvas à frente: chega nelas já devagar
        float alcance = alvo * alvo / (2f * b) + 2f;
        for (int i = indice; i < caminho.Count && caminho[i].s - sAtual < alcance; i++)
        {
            if (caminho[i].vMax >= SemLimite) continue;
            float d = Mathf.Max(0f, caminho[i].s - sAtual);
            alvo = Mathf.Min(alvo, Mathf.Sqrt(caminho[i].vMax * caminho[i].vMax + 2f * b * d));
        }

        // Carro, pessoa ou jogador na frente
        alvo = Mathf.Min(alvo, VelocidadeParaParar(distanciaLivre - seguranca, b));

        // Esperando a vez no cruzamento
        if (limiteCruzamento < SemLimite) alvo = Mathf.Min(alvo, VelocidadeParaParar(limiteCruzamento, b));

        return alvo;
    }

    private static float VelocidadeParaParar(float distancia, float frenagemConforto)
    {
        if (distancia <= 0.05f) return 0f;
        return Mathf.Sqrt(2f * frenagemConforto * distancia);
    }

    // Acha onde o carro está no caminho e quanto ele saiu da faixa
    private void AtualizarProgresso()
    {
        Vector2 pos = corpo.position;
        float melhor = float.MaxValue;
        int melhorI = indice;
        float melhorT = 0f;

        int fim = Mathf.Min(caminho.Count - 2, indice + 40);
        for (int i = Mathf.Max(0, indice - 2); i <= fim; i++) Projetar(pos, i, ref melhor, ref melhorI, ref melhorT);

        // Foi empurrado para longe: procura no caminho todo
        if (melhor > 36f)
            for (int i = 0; i < caminho.Count - 1; i++) Projetar(pos, i, ref melhor, ref melhorI, ref melhorT);

        indice = melhorI;
        Ponto a = caminho[indice], b = caminho[indice + 1];
        sAtual = Mathf.Lerp(a.s, b.s, melhorT);
        Vector2 dir = (b.p - a.p).normalized;
        Vector2 rel = pos - Vector2.Lerp(a.p, b.p, melhorT);
        erroLateral = dir.x * rel.y - dir.y * rel.x;

        // Joga fora o caminho que já ficou bem para trás
        int remover = 0;
        while (remover < caminho.Count - 2 && remover < indice - 1 && caminho[remover + 1].s < sAtual - GuardarAtras) remover++;
        if (remover > 0)
        {
            caminho.RemoveRange(0, remover);
            indice -= remover;
        }
    }

    private void Projetar(Vector2 pos, int i, ref float melhor, ref int melhorI, ref float melhorT)
    {
        Vector2 a = caminho[i].p, b = caminho[i + 1].p;
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(pos - a, ab) / ab.sqrMagnitude);
        float d = (pos - (a + ab * t)).sqrMagnitude;
        if (d < melhor - 0.0001f)
        {
            melhor = d;
            melhorI = i;
            melhorT = t;
        }
    }

    private Vector2 DirecaoEm(float s)
    {
        int i = indice;
        while (i < caminho.Count - 2 && caminho[i + 1].s < s) i++;
        Vector2 d = caminho[i + 1].p - caminho[i].p;
        return d.sqrMagnitude > 0.0001f ? d.normalized : (Vector2)transform.up;
    }

    // ---------- Enxergar quem está na frente ----------

    private struct Obstaculo
    {
        public Vector2 a, b;      // segmento do comprimento (pessoa: a = b)
        public float raio;        // metade da largura
        public float extra;       // pedaço do para-choque além do segmento
        public bool carro;        // carro do trânsito (pode ser ignorado se travar)
        public bool jogador;      // o carro do jogador
    }

    private static readonly List<Obstaculo> obstaculos = new List<Obstaculo>();
    private static CarroController jogador;
    private static Vector2 tamanhoJogador = new Vector2(1.25f, 2.1f);
    private static Pedestre[] pedestres = new Pedestre[0];
    private static float proximaBusca;

    private static void AtualizarCena()
    {
        if (Time.time < proximaBusca) return;
        proximaBusca = Time.time + 1.5f;

        pedestres = FindObjectsByType<Pedestre>(FindObjectsSortMode.None);
        CarroController novo = FindFirstObjectByType<CarroController>();
        if (novo != null && novo != jogador)
        {
            BoxCollider2D caixa = novo.GetComponent<BoxCollider2D>();
            if (caixa != null)
            {
                Vector3 e = caixa.transform.lossyScale;
                tamanhoJogador = new Vector2(Mathf.Abs(caixa.size.x * e.x) * 0.5f, Mathf.Abs(caixa.size.y * e.y) * 0.5f);
            }
        }
        jogador = novo;
    }

    private void Olhar()
    {
        AtualizarCena();

        Vector2 eu = corpo.position;
        Vector2 frente = transform.up;
        float b = frenagem * 0.6f;
        float alcance = Mathf.Clamp(velocidade * velocidade / (2f * b) + 12f, 14f, 45f);
        alcance = Mathf.Max(alcance, Mathf.Min(alcanceExtra, 60f));
        float alcance2 = (alcance + 6f) * (alcance + 6f);
        bool ignorarCarros = Time.time < ignorarCarrosAte;

        obstaculos.Clear();
        if (!ignorarCarros)
        {
            foreach (CarroTrafego outro in todos)
            {
                if (outro == this) continue;
                Vector2 pos = outro.corpo.position;
                Vector2 rel = pos - eu;
                if (rel.sqrMagnitude > alcance2 || Vector2.Dot(rel, frente) < 0f) continue;
                Vector2 f = outro.transform.up * (outro.meioComprimento * 0.85f);
                obstaculos.Add(new Obstaculo
                {
                    a = pos - f, b = pos + f, raio = outro.meioLargura,
                    extra = outro.meioComprimento * 0.15f, carro = true,
                });
            }
        }

        if (jogador != null && jogador.isActiveAndEnabled)
        {
            Vector2 pos = jogador.transform.position;
            Vector2 rel = pos - eu;
            if (rel.sqrMagnitude <= alcance2 && Vector2.Dot(rel, frente) > 0f)
            {
                Vector2 f = jogador.transform.up * (tamanhoJogador.y * 0.85f);
                obstaculos.Add(new Obstaculo
                {
                    a = pos - f, b = pos + f, raio = tamanhoJogador.x, extra = tamanhoJogador.y * 0.15f, jogador = true,
                });
            }
        }

        foreach (Pedestre pessoa in pedestres)
        {
            // Só quem está atravessando: quem espera ou anda na calçada não trava o trânsito
            if (pessoa == null || !pessoa.isActiveAndEnabled || !pessoa.NaRua) continue;
            Vector2 pos = pessoa.transform.position;
            Vector2 rel = pos - eu;
            if (rel.sqrMagnitude > alcance2 || Vector2.Dot(rel, frente) < 0f) continue;
            // Conta também o pedaço que a pessoa vai andar logo em seguida (o carro freia com antecedência)
            Vector2 adiante = pos + (Vector2)pessoa.transform.up * 1.4f;
            obstaculos.Add(new Obstaculo { a = pos, b = adiante, raio = 0.7f, extra = 0.35f });
        }

        // Anda pelo caminho à frente procurando o primeiro obstáculo que encosta na faixa
        distanciaLivre = SemLimite;
        bloqueadoPorCarro = false;
        bloqueadoPeloJogador = false;
        foreach (Obstaculo o in obstaculos)
        {
            for (int i = indice + 1; i < caminho.Count; i++)
            {
                Ponto pt = caminho[i];
                if (pt.s - sAtual > alcance) break;

                Vector2 tan = (pt.p - caminho[i - 1].p).normalized;
                Vector2 q = MaisPertoNoSegmento(pt.p, o.a, o.b);
                Vector2 d = q - pt.p;
                float ao = Vector2.Dot(d, tan);
                float lado = Mathf.Abs(tan.x * d.y - tan.y * d.x);
                if (Mathf.Abs(ao) > 0.8f || lado > meioLargura + o.raio + 0.15f) continue;

                float livre = pt.s + ao - sAtual - meioComprimento - o.extra;
                if (livre < distanciaLivre)
                {
                    distanciaLivre = livre;
                    bloqueadoPorCarro = o.carro;
                    bloqueadoPeloJogador = o.jogador;
                }
                break;
            }
        }

        // Travado por outro carro do trânsito há muito tempo: passa por cima (evita trânsito travado para sempre)
        if (bloqueadoPorCarro && velocidade < 0.1f && distanciaLivre < seguranca + 0.5f)
        {
            if (bloqueadoDesde < 0f) bloqueadoDesde = Time.time;
            else if (Time.time - bloqueadoDesde > esperaMaxima)
            {
                ignorarCarrosAte = Time.time + 2.5f;
                bloqueadoDesde = -1f;
            }
        }
        else bloqueadoDesde = -1f;

        // O jogador parado na frente: depois de um tempinho o motorista buzina (e de novo se ele continuar)
        if (bloqueadoPeloJogador && velocidade < 1f && distanciaLivre < seguranca + 3f)
        {
            if (jogadorNaFrenteDesde < 0f) jogadorNaFrenteDesde = Time.time;
            else if (Time.time - jogadorNaFrenteDesde > 1.5f && Time.time >= proximaBuzina)
            {
                proximaBuzina = Time.time + Random.Range(3.5f, 7f);
                Sons.TocarEm(Random.value < 0.5f ? "BuzinaTrafego" : "BuzinaTrafego2", corpo.position, 1f, tomBuzina, 26f);
            }
        }
        else jogadorNaFrenteDesde = -1f;
    }

    // ---------- Vez nos cruzamentos ----------

    private void AtualizarCruzamentos()
    {
        limiteCruzamento = SemLimite;
        alcanceExtra = 0f;
        int traseira = indice;
        while (traseira > 0 && caminho[traseira].s > sAtual - meioComprimento - 0.5f) traseira--;

        // Sai dos cruzamentos que já ficaram para trás
        for (int r = reservas.Count - 1; r >= 0; r--)
        {
            bool aindaDentro = false;
            for (int i = traseira; i < caminho.Count && caminho[i].s < sAtual + 35f; i++)
                if (grafo.CruzamentoDe(caminho[i].no) == reservas[r]) { aindaDentro = true; break; }

            if (!aindaDentro)
            {
                reservas[r].Sair(this);
                reservas.RemoveAt(r);
            }
        }

        // Próximo cruzamento à frente em que ainda não tenho a vez
        Cruzamento proximo = null;
        int inicio = -1;
        for (int i = indice; i < caminho.Count && caminho[i].s - sAtual < 45f; i++)
        {
            Cruzamento c = grafo.CruzamentoDe(caminho[i].no);
            if (c == null || reservas.Contains(c)) continue;
            proximo = c;
            inicio = i;
            break;
        }

        if (proximo == null)
        {
            Desistir();
            return;
        }

        // Onde o carro sai do cruzamento e qual conversão vai fazer
        int conversao = Cruzamento.Reto;
        int fim = inicio;
        while (fim + 1 < caminho.Count && grafo.CruzamentoDe(caminho[fim + 1].no) == proximo)
        {
            fim++;
            if (caminho[fim].conversao != Cruzamento.Reto && conversao == Cruzamento.Reto) conversao = caminho[fim].conversao;
        }
        float sEntrada = caminho[inicio].s;
        float sSaida = caminho[fim].s;
        alcanceExtra = sSaida - sAtual + meioComprimento * 2f + 2f;

        float b = frenagem * 0.6f;
        float ateParada = sEntrada - paradaAntesDoCruzamento - meioComprimento - sAtual;
        if (ateParada > velocidade * velocidade / (2f * b) + 4f)
        {
            Desistir();   // ainda longe
            return;
        }

        // Tem carro na frente antes da linha de parada: só espera na fila (sem pedir a vez ainda)
        if (distanciaLivre < ateParada - 0.5f)
        {
            Desistir();
            limiteCruzamento = Mathf.Max(0f, ateParada);
            return;
        }

        // Só entra se tiver espaço do outro lado (não fica parado no meio do cruzamento)
        bool espacoNaSaida = distanciaLivre >= sSaida - sAtual + meioComprimento + 0.5f;
        if (esperandoDesde < 0f) esperandoDesde = Time.time;
        bool forcar = ateParada < -1.5f || Time.time - esperandoDesde > esperaMaxima;

        Vector2 direcaoEntrada = (caminho[Mathf.Min(inicio + 1, caminho.Count - 1)].p - caminho[Mathf.Max(inicio - 1, 0)].p).normalized;
        Vector2 direcaoSaida = fim > inicio ? (caminho[fim].p - caminho[fim - 1].p).normalized : direcaoEntrada;
        bool pode = (espacoNaSaida || forcar) && proximo.PedirEntrada(this, direcaoEntrada, direcaoSaida, conversao, forcar);
        if (!espacoNaSaida && !forcar) proximo.Desistir(this);

        if (pode)
        {
            reservas.Add(proximo);
            esperando = null;
            esperandoDesde = -1f;
        }
        else
        {
            esperando = proximo;
            limiteCruzamento = Mathf.Max(0f, ateParada);
        }
    }

    private void Desistir()
    {
        if (esperando != null) esperando.Desistir(this);
        esperando = null;
        esperandoDesde = -1f;
    }

    // Liga a seta um pouco antes da curva e mantém até terminar
    private void AtualizarSeta()
    {
        Seta = 0;
        for (int i = indice; i < caminho.Count && caminho[i].s - sAtual < 16f; i++)
        {
            int c = caminho[i].conversao;
            if (c == Cruzamento.Reto) continue;
            Seta = c == Cruzamento.Direita ? 1 : -1;
            return;
        }
    }

    // ---------- Rotas antigas ----------

    private static readonly Dictionary<Transform, GrafoTrafego> rotasAntigas = new Dictionary<Transform, GrafoTrafego>();

    private static GrafoTrafego GrafoDaRotaAntiga(Transform rota, float faixa, float raio)
    {
        if (rotasAntigas.TryGetValue(rota, out GrafoTrafego pronto) && pronto != null) return pronto;

        // Limpa rotas de cenas que já foram fechadas
        List<Transform> velhas = new List<Transform>();
        foreach (Transform t in rotasAntigas.Keys) if (t == null) velhas.Add(t);
        foreach (Transform t in velhas) rotasAntigas.Remove(t);

        GrafoTrafego g = new GrafoTrafego(faixa);
        for (int i = 0; i < rota.childCount; i++) g.Adicionar(rota.GetChild(i).position, raio);
        for (int i = 0; i < rota.childCount; i++) g.Ligar(i, (i + 1) % rota.childCount);
        g.Finalizar();
        rotasAntigas[rota] = g;
        return g;
    }

    // ---------- Ajudantes ----------

    private static float Angulo(Vector2 d) => Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;   // frente = transform.up

    private static Vector2 MaisPertoNoSegmento(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return a + ab * t;
    }

    private void OnDrawGizmosSelected()
    {
        // Caminho planejado (só aparece durante o jogo)
        Gizmos.color = new Color(1f, 0.3f, 0.8f);
        for (int i = 1; i < caminho.Count; i++) Gizmos.DrawLine(caminho[i - 1].p, caminho[i].p);

        if (rota == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < rota.childCount; i++)
        {
            Transform atual = rota.GetChild(i);
            Transform proximo = rota.GetChild((i + 1) % rota.childCount);
            Gizmos.DrawSphere(atual.position, 0.1f);
            Gizmos.DrawLine(atual.position, proximo.position);
        }
    }
}
