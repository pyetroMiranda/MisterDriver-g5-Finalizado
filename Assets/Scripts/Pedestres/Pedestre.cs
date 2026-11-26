using System.Collections;
using TMPro;
using UnityEngine;

// Pedestre: anda pela rota, espera para atravessar a rua e, se for atingido,
// pula para o lado, fica piscando (sem colisão) por um tempo e volta a andar.
// Bater numa pessoa tira o DOBRO de pontos de uma batida em carro.
[RequireComponent(typeof(Rigidbody2D))]
public class Pedestre : MonoBehaviour
{
    private enum Estado { Andando, Esperando, Atravessando, Pulando }

    [Header("Caminhada")]
    [SerializeField] private float velocidade = 1.2f;
    [SerializeField] private float multiplicadorTravessia = 1.5f;   // atravessa a rua mais rápido
    [SerializeField] private float giro = 540f;                     // graus por segundo ao virar

    [Header("Atravessar a rua")]
    [Tooltip("Só atravessa se nenhum carro estiver mais perto que isso da travessia (mais o espaço que o carro precisa para frear).")]
    [SerializeField] private float distanciaSegura = 6f;
    [Tooltip("Depois de esperar isso (segundos), só olha se tem carro bem perto (para não ficar plantado na calçada).")]
    [SerializeField] private float esperaMaxima = 8f;

    [Header("Batida")]
    [SerializeField] private float velocidadeMinimaBatida = 0.3f;   // encostar devagar não conta
    [SerializeField] private int multiplicadorPontos = 2;           // dobro de uma batida em carro
    [SerializeField, Range(0f, 1f)] private float velocidadeMantidaPeloCarro = 0.7f;
    [SerializeField] private float distanciaPulo = 1.6f;
    [SerializeField] private float duracaoPulo = 0.35f;
    [SerializeField] private float tempoSemColisao = 2.5f;

    [Header("Animação (montado pela ferramenta)")]
    [SerializeField] private Transform visual;
    [SerializeField] private Transform peEsquerdo;
    [SerializeField] private Transform peDireito;
    [SerializeField] private Transform bracoEsquerdo;
    [SerializeField] private Transform bracoDireito;
    [SerializeField] private float ritmoPassos = 11f;
    [SerializeField] private float amplitudePe = 0.13f;
    [SerializeField] private float amplitudeBraco = 0.07f;

    private Rigidbody2D corpo;
    private RotaPedestres rota;
    private int alvo;
    private int origem;
    private int sentido = 1;
    private Estado estado = Estado.Andando;
    private bool semColisao;
    private float fase;
    private float proximaChecagem;
    private float esperandoDesde;
    private Vector3 basePeE, basePeD, baseBracoE, baseBracoD;

    // Carros da cena (procurados de vez em quando, para não pesar)
    private static CarroController jogador;
    private static CarroTrafego[] trafego = new CarroTrafego[0];
    private static float proximaBusca;

    public void Iniciar(RotaPedestres novaRota, int indiceAlvo, int novoSentido, float novaVelocidade)
    {
        rota = novaRota;
        alvo = indiceAlvo;
        origem = indiceAlvo - novoSentido;
        sentido = novoSentido;
        velocidade = novaVelocidade;
        estado = Estado.Andando;

        // Já começa olhando para onde vai
        Vector2 para = rota.Posicao(alvo) - (Vector2)transform.position;
        if (para.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(para.y, para.x) * Mathf.Rad2Deg - 90f);
    }

    private void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Kinematic;
        corpo.useFullKinematicContacts = true;   // percebe os carros do trânsito (que também são cinemáticos)
        corpo.gravityScale = 0f;

        if (peEsquerdo != null) basePeE = peEsquerdo.localPosition;
        if (peDireito != null) basePeD = peDireito.localPosition;
        if (bracoEsquerdo != null) baseBracoE = bracoEsquerdo.localPosition;
        if (bracoDireito != null) baseBracoD = bracoDireito.localPosition;
    }

    // ---------- Movimento ----------

    private void FixedUpdate()
    {
        if (rota == null || estado == Estado.Pulando) return;
        float dt = Time.fixedDeltaTime;

        if (estado == Estado.Esperando)
        {
            if (Time.time >= proximaChecagem)
            {
                proximaChecagem = Time.time + 0.2f;
                if (CaminhoLivre()) estado = Estado.Atravessando;
            }
            return;
        }

        Vector2 destino = rota.Posicao(alvo);
        Vector2 posicao = corpo.position;
        Vector2 para = destino - posicao;
        float passo = VelocidadeAtual() * dt;

        if (para.magnitude <= passo + 0.02f)
        {
            corpo.MovePosition(destino);
            ChegouNoPonto();
            return;
        }

        Vector2 direcao = para.normalized;
        corpo.MovePosition(posicao + direcao * passo);

        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg - 90f;
        corpo.MoveRotation(Mathf.MoveTowardsAngle(corpo.rotation, angulo, giro * dt));
    }

    // Está no meio da rua (os carros do trânsito só param para quem está atravessando)
    public bool NaRua => estado == Estado.Atravessando;

    private float VelocidadeAtual() => velocidade * (estado == Estado.Atravessando ? multiplicadorTravessia : 1f);

    private void ChegouNoPonto()
    {
        origem = alvo;
        alvo = rota.Proximo(alvo, ref sentido);

        if (rota.Atravessa(origem, alvo))
        {
            proximaChecagem = 0f;
            esperandoDesde = Time.time;
            estado = CaminhoLivre() ? Estado.Atravessando : Estado.Esperando;
        }
        else
        {
            estado = Estado.Andando;
        }
    }

    // Nenhum carro perto da travessia?
    private bool CaminhoLivre()
    {
        AtualizarCarros();
        Vector2 a = rota.Posicao(origem);
        Vector2 b = rota.Posicao(alvo);

        bool cansou = estado == Estado.Esperando && Time.time - esperandoDesde > esperaMaxima;

        if (jogador != null && DistanciaAoTrecho(jogador.transform.position, a, b) < distanciaSegura) return false;
        // Carro do trânsito parado não atrapalha: ele enxerga a pessoa e espera ela passar.
        // Carro andando precisa estar longe o bastante para frear com calma (senão ele para de repente).
        foreach (CarroTrafego carro in trafego)
        {
            if (carro == null || !carro.isActiveAndEnabled || carro.VelocidadeAtual <= 0.5f) continue;
            float v = carro.VelocidadeAtual;
            float precisa = cansou ? distanciaSegura * 0.6f : distanciaSegura + v * v / (2f * 3.6f);
            if (DistanciaAoTrecho(carro.transform.position, a, b) < precisa) return false;
        }

        return true;
    }

    private static void AtualizarCarros()
    {
        if (Time.time < proximaBusca && jogador != null) return;
        proximaBusca = Time.time + 3f;
        jogador = FindFirstObjectByType<CarroController>();
        trafego = FindObjectsByType<CarroTrafego>(FindObjectsSortMode.None);
    }

    private static float DistanciaAoTrecho(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    // ---------- Animação dos passos ----------

    private void Update()
    {
        bool andando = estado == Estado.Andando || estado == Estado.Atravessando;

        if (andando)
        {
            fase += Time.deltaTime * ritmoPassos * (VelocidadeAtual() / 1.2f);
            float s = Mathf.Sin(fase);
            Mover(peEsquerdo, basePeE, s * amplitudePe);
            Mover(peDireito, basePeD, -s * amplitudePe);
            Mover(bracoEsquerdo, baseBracoE, -s * amplitudeBraco);   // braço vai ao contrário do pé
            Mover(bracoDireito, baseBracoD, s * amplitudeBraco);
        }
        else
        {
            float k = 10f * Time.deltaTime;
            Voltar(peEsquerdo, basePeE, k);
            Voltar(peDireito, basePeD, k);
            Voltar(bracoEsquerdo, baseBracoE, k);
            Voltar(bracoDireito, baseBracoD, k);
        }
    }

    private static void Mover(Transform parte, Vector3 baseLocal, float deslocamento)
    {
        if (parte != null) parte.localPosition = baseLocal + Vector3.up * deslocamento;
    }

    private static void Voltar(Transform parte, Vector3 baseLocal, float k)
    {
        if (parte != null) parte.localPosition = Vector3.Lerp(parte.localPosition, baseLocal, k);
    }

    // ---------- Batida ----------

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (semColisao || estado == Estado.Pulando) return;

        CarroController carro = outro.GetComponentInParent<CarroController>();
        if (carro != null)
        {
            if (carro.VelocidadeAtual < velocidadeMinimaBatida) return;
            AtingidoPeloJogador(carro);
            return;
        }

        // Carro do trânsito: a pessoa só sai da frente (sem pontos)
        CarroTrafego carroTrafego = outro.GetComponentInParent<CarroTrafego>();
        if (carroTrafego != null) StartCoroutine(Desviar(carroTrafego.transform));
    }

    private void AtingidoPeloJogador(CarroController carro)
    {
        Sons.TocarEm("Ai", transform.position, 1f, Random.Range(0.85f, 1.3f));
        MolaParaChoque mola = carro.GetComponent<MolaParaChoque>();
        bool protegido = mola != null && mola.Ativa;   // o campo de força protege

        CarteiraMotorista carteira = carro.GetComponent<CarteiraMotorista>();
        if (!protegido && carteira != null && carteira.PodeTomarDano)
        {
            int dano = carteira.PontosPorBatida * multiplicadorPontos;
            carteira.PerderPontos(dano);
            carro.AbsorverBatida(velocidadeMantidaPeloCarro);
            MostrarTexto("-" + dano);
        }

        StartCoroutine(Desviar(carro.transform));
    }

    // Pula para o lado, longe da frente do carro, e fica piscando sem colisão
    private IEnumerator Desviar(Transform carro)
    {
        Sons.TocarEm("Pulo", transform.position, 0.7f, Random.Range(0.9f, 1.2f));
        Estado anterior = estado;
        estado = Estado.Pulando;
        semColisao = true;

        Vector2 frenteCarro = carro.up;
        Vector2 lado = new Vector2(-frenteCarro.y, frenteCarro.x);
        Vector2 longe = corpo.position - (Vector2)carro.position;
        if (Vector2.Dot(lado, longe) < 0f) lado = -lado;

        Vector2 inicio = corpo.position;
        Vector2 fim = inicio + lado * distanciaPulo;

        float t = 0f;
        while (t < duracaoPulo)
        {
            t += Time.fixedDeltaTime;
            float k = Mathf.Clamp01(t / duracaoPulo);
            corpo.MovePosition(Vector2.Lerp(inicio, fim, 1f - (1f - k) * (1f - k)));
            if (visual != null) visual.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(k * Mathf.PI));
            yield return new WaitForFixedUpdate();
        }
        if (visual != null) visual.localScale = Vector3.one;

        estado = anterior == Estado.Esperando ? Estado.Andando : anterior;

        // Pisca enquanto está sem colisão
        float acaba = Time.time + tempoSemColisao;
        while (Time.time < acaba)
        {
            if (visual != null) visual.gameObject.SetActive(!visual.gameObject.activeSelf);
            yield return new WaitForSeconds(0.1f);
        }
        if (visual != null) visual.gameObject.SetActive(true);
        semColisao = false;
    }

    // "-2" vermelho subindo em cima da pessoa
    private void MostrarTexto(string conteudo)
    {
        // A câmera gira junto com o carro: o texto acompanha para ficar sempre "em pé" na tela
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 cima = cam != null ? cam.up : Vector3.up;

        GameObject obj = new GameObject("TextoDano");
        obj.transform.position = transform.position + cima * 0.8f;
        if (cam != null) obj.transform.rotation = cam.rotation;
        TextMeshPro texto = obj.AddComponent<TextMeshPro>();
        texto.text = conteudo;
        texto.fontSize = 6f;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = new Color(1f, 0.25f, 0.35f);
        texto.outlineWidth = 0.25f;
        texto.outlineColor = new Color32(18, 0, 24, 255);
        texto.sortingOrder = 30;
        texto.rectTransform.sizeDelta = new Vector2(4f, 2f);
        StartCoroutine(SubirESumir(texto));
    }

    private static IEnumerator SubirESumir(TextMeshPro texto)
    {
        Vector3 inicio = texto.transform.position;
        Vector3 cima = texto.transform.up;
        float t = 0f;
        while (t < 0.9f && texto != null)
        {
            t += Time.deltaTime;
            texto.transform.position = inicio + cima * (1.2f * t);
            texto.alpha = 1f - Mathf.Clamp01((t - 0.4f) / 0.5f);
            yield return null;
        }
        if (texto != null) Destroy(texto.gameObject);
    }
}
