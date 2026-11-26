using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Missão do Nivel1, em duas partes:
// 1) IDA: a primeira saída do Jason como motorista. Ir ao Mercado Bom Preço, do outro lado da cidade, sem bater.
//    Estacionou na vaga do mercado? O Jason entra no mercado (cena Historia_Mercado) e o nível NÃO termina.
// 2) VOLTA: depois do mercado o Nivel1 recomeça com o carro na vaga do mercado e a Gangue do Escapamento
//    solta pelas ruas. A vaga de chegada vai para a frente da casa do Jason: estacionou lá, o nível termina.
// A seta amarela e o radar apontam o destino. Cada batida tira pontos da carteira.
// Montado por Mr Driver > História > 8. Montar o Nivel1 (casa do Jason e mercado).
public class MissaoMercado : MonoBehaviour, IFinalNivel
{
    // Ligado pelo mercado quando o Jason sai de lá. Fica ligado até ele chegar em casa
    // (se a carteira for suspensa na volta, o nível recomeça já na volta).
    public static bool VoltandoDoMercado;

    [Header("Ligações (procura sozinho se ficar vazio)")]
    [SerializeField] private CarroController carro;
    [SerializeField] private CarteiraMotorista carteira;
    [SerializeField] private CaixaDialogo dialogo;
    [SerializeField] private ObjetivoHUD objetivo;
    [SerializeField] private SetaObjetivo seta;
    [Tooltip("A vaga de chegada (vazio = a ChegadaNivel da cena). Na ida ela fica no mercado; na volta, na frente de casa.")]
    [SerializeField] private ChegadaNivel chegada;
    [SerializeField] private GangueRebelde gangue;

    [Header("Volta para casa")]
    [Tooltip("Onde a vaga de chegada fica na volta: na rua sem saída, em frente à garagem do Jason.")]
    [SerializeField] private Vector2 vagaCasa = new Vector2(-19.1f, 2.3f);
    [Tooltip("Para onde o carro aponta ao sair do mercado (graus; -90 = para a direita).")]
    [SerializeField] private float anguloSaidaMercado = -90f;
    [SerializeField] private string cenaMercado = ProgressoHistoria.CenaMercado;

    [Header("Objetivos")]
    [SerializeField] private string objetivoIr = "Vá até o Mercado Bom Preço (siga a seta amarela)";
    [SerializeField] private string objetivoEstacionar = "Estacione na vaga em frente ao mercado";
    [SerializeField] private string objetivoVolta = "Volte para casa com segurança. Cuidado com os rebeldes!";
    [SerializeField] private string objetivoEstacionarCasa = "Estacione na frente da sua casa";
    [SerializeField] private float distanciaPerto = 16f;

    [Header("Falas da ida")]
    [SerializeField] private List<Fala> falasAbertura = new List<Fala>();
    [Tooltip("O Jason comenta quando bate (uma é sorteada, sem parar o jogo).")]
    [SerializeField] private List<Fala> reacoesBatida = new List<Fala>();
    [SerializeField] private float intervaloComentarios = 4f;
    [SerializeField] private List<Fala> falasChegadaPerfeita = new List<Fala>();
    [SerializeField] private List<Fala> falasChegada = new List<Fala>();

    [Header("Falas da volta")]
    [SerializeField] private List<Fala> falasAberturaVolta = new List<Fala>();
    [Tooltip("Quando um rebelde bate no Jason.")]
    [SerializeField] private List<Fala> reacoesRebelde = new List<Fala>();
    [SerializeField] private List<Fala> falasCasaPerfeita = new List<Fala>();
    [SerializeField] private List<Fala> falasCasa = new List<Fala>();

    private bool volta, comecou, terminou, perto;
    private float proximoComentario;
    private float ultimoRebelde = -10f;

    // Usado pela ferramenta do Editor
    public void DefinirFalas(List<Fala> abertura, List<Fala> reacoes, List<Fala> perfeita, List<Fala> chegadaFalas)
    {
        falasAbertura = abertura;
        reacoesBatida = reacoes;
        falasChegadaPerfeita = perfeita;
        falasChegada = chegadaFalas;
    }

    public void DefinirFalasVolta(List<Fala> abertura, List<Fala> rebelde, List<Fala> perfeita, List<Fala> casa)
    {
        falasAberturaVolta = abertura;
        reacoesRebelde = rebelde;
        falasCasaPerfeita = perfeita;
        falasCasa = casa;
    }

    private void Start()
    {
        if (carro == null) carro = FindFirstObjectByType<CarroController>();
        if (carteira == null && carro != null) carteira = carro.GetComponent<CarteiraMotorista>();
        if (dialogo == null) dialogo = CaixaDialogo.Instancia;
        if (objetivo == null) objetivo = FindFirstObjectByType<ObjetivoHUD>(FindObjectsInactive.Include);
        if (seta == null) seta = FindFirstObjectByType<SetaObjetivo>(FindObjectsInactive.Include);
        if (chegada == null) chegada = FindFirstObjectByType<ChegadaNivel>();
        if (gangue == null) gangue = FindFirstObjectByType<GangueRebelde>();

        if (carro == null || dialogo == null || chegada == null)
        {
            Debug.LogError("MissaoMercado: não encontrei o carro, a CaixaDialogo ou a ChegadaNivel. " +
                           "Monte com Mr Driver > História > 8. Montar o Nivel1.", this);
            enabled = false;
            return;
        }

        volta = VoltandoDoMercado;
        if (volta) PrepararVolta();

        if (carteira != null) carteira.aoPerderPontos.AddListener(Bateu);
        CarroRebelde.AoBaterNoJogador += BateuRebelde;
        StartCoroutine(Roteiro());
    }

    private void OnDestroy()
    {
        if (carteira != null) carteira.aoPerderPontos.RemoveListener(Bateu);
        CarroRebelde.AoBaterNoJogador -= BateuRebelde;
    }

    // O carro sai da vaga do mercado e a vaga de chegada vai para a frente de casa
    private void PrepararVolta()
    {
        Vector3 noMercado = chegada.transform.position;
        Quaternion giro = Quaternion.Euler(0f, 0f, anguloSaidaMercado);
        carro.transform.SetPositionAndRotation(new Vector3(noMercado.x, noMercado.y, carro.transform.position.z), giro);
        Rigidbody2D corpo = carro.GetComponent<Rigidbody2D>();
        if (corpo != null)
        {
            corpo.position = noMercado;
            corpo.rotation = anguloSaidaMercado;
            corpo.linearVelocity = Vector2.zero;
            corpo.angularVelocity = 0f;
        }

        Camera cam = Camera.main;
        if (cam != null)
            cam.transform.SetPositionAndRotation(new Vector3(noMercado.x, noMercado.y, cam.transform.position.z), giro);

        chegada.transform.position = new Vector3(vagaCasa.x, vagaCasa.y, chegada.transform.position.z);
        chegada.transform.rotation = Quaternion.identity;

        if (gangue != null) gangue.Soltar();
    }

    private IEnumerator Roteiro()
    {
        yield return null;                               // espera todo mundo terminar o Start()
        yield return new WaitForSecondsRealtime(0.8f);   // a tela clareando
        yield return dialogo.Conversar(volta ? falasAberturaVolta : falasAbertura, true);

        comecou = true;
        if (objetivo != null) objetivo.Mostrar(volta ? objetivoVolta : objetivoIr);
        if (seta != null) seta.Alvo = chegada.transform;
    }

    private void Update()
    {
        if (!comecou || terminou || objetivo == null) return;

        // O objetivo muda quando o Jason chega perto do destino (e volta se ele se afastar)
        bool agora = Vector2.Distance(carro.transform.position, chegada.transform.position) <= distanciaPerto;
        if (agora == perto) return;
        perto = agora;
        if (volta) objetivo.Mostrar(perto ? objetivoEstacionarCasa : objetivoVolta);
        else objetivo.Mostrar(perto ? objetivoEstacionar : objetivoIr);
    }

    private void BateuRebelde(CarroRebelde rebelde)
    {
        ultimoRebelde = Time.time;
        Comentar(reacoesRebelde, true);
    }

    private void Bateu(int pontosRestantes)
    {
        if (Time.time - ultimoRebelde < 0.3f) return;   // já comentou a batida do rebelde
        Comentar(reacoesBatida, false);
    }

    private void Comentar(List<Fala> lista, bool forcar)
    {
        if (!comecou || terminou || lista == null || lista.Count == 0 || dialogo.Aberta) return;
        if (!forcar && Time.unscaledTime < proximoComentario) return;

        Fala f = lista[Random.Range(0, lista.Count)];
        dialogo.Comentar(new Fala(f.quem, f.texto, f.emote, true), 2.4f);
        proximoComentario = Time.unscaledTime + intervaloComentarios;
    }

    // Chamado pela ChegadaNivel quando o carro estaciona (o jogo já está parado)
    public IEnumerator AntesDaTela(ChegadaNivel vaga)
    {
        terminou = true;
        if (seta != null) seta.Alvo = null;
        if (objetivo != null) objetivo.Esconder();

        bool semArranhao = carteira == null || carteira.Pontos >= carteira.PontosIniciais;
        while (dialogo.Aberta) yield return null;

        if (!volta)
        {
            // Ainda não acabou: o Jason entra no mercado
            yield return dialogo.Conversar(semArranhao ? falasChegadaPerfeita : falasChegada);
            vaga.Interromper();
            TransicaoTela.Carregar(cenaMercado);
            yield break;
        }

        yield return dialogo.Conversar(semArranhao ? falasCasaPerfeita : falasCasa);
        VoltandoDoMercado = false;
    }
}
