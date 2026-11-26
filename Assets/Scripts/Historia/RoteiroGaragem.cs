using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Roteiro da cena Historia_Garagem (capítulo 2: de volta para casa com a carteira):
// 1) Jason chega em casa e os pais comemoram. O pai vai esperar na porta da garagem.
// 2) Livre pela casa. Falando com o pai, ele abre a garagem e entra.
// 3) Quando o Jason entra na garagem, o pai ensina a escolher o carro e a cor.
// 4) Escolheu? A mãe pede para ir ao mercado, o pai abre o portão e o Jason sai de carro (Nivel1).
// Todas as falas podem ser editadas aqui no Inspector.
public class RoteiroGaragem : RoteiroBase
{
    [Serializable]
    public class Passo
    {
        public Direcao direcao;
        [Min(1)] public int quantidade = 1;

        public Passo() { }
        public Passo(Direcao d, int n) { direcao = d; quantidade = n; }
    }

    private enum Etapa { Chegada, Livre, PortaAberta, Escolher, Mercado, Saindo }

    [Header("Ligações (montadas pela ferramenta)")]
    [SerializeField] private JogadorCasa jogador;
    [SerializeField] private NpcHistoria pai;
    [SerializeField] private NpcHistoria mae;
    [SerializeField] private PortaCasa portaGaragem;
    [SerializeField] private PortaCasa portao;
    [SerializeField] private ObjetivoHUD objetivo;
    [SerializeField] private CaixaDialogo dialogo;
    [SerializeField] private TelaGaragem telaGaragem;

    [Header("Caminhos")]
    [SerializeField] private List<Passo> paiAtePorta = new List<Passo>();
    [SerializeField] private Direcao paiNaPorta = Direcao.Direita;
    [SerializeField] private List<Passo> maeAteCozinha = new List<Passo>();
    [SerializeField] private Direcao maeNaCozinha = Direcao.Esquerda;
    [SerializeField] private List<Passo> paiAteGaragem = new List<Passo>();
    [SerializeField] private Direcao paiNaGaragem = Direcao.Direita;
    [Tooltip("Coluna da grade onde a garagem começa (o Jason entrou = a aula começa).")]
    [SerializeField] private int inicioGaragemX = 22;

    [Header("Objetivos")]
    [SerializeField] private string objetivoPai = "Encontre o seu pai na porta da garagem";
    [SerializeField] private string objetivoEntrar = "Entre na garagem";
    [SerializeField] private string objetivoEscolher = "Chegue perto do carro e aperte ENTER";
    [SerializeField] private string objetivoSair = "Saia de carro pelo portão da garagem";

    [Header("Próxima cena")]
    [SerializeField] private string proximaCena = ProgressoHistoria.PrimeiroNivel;

    [Header("Falas")]
    [SerializeField] private List<Fala> falasChegada = new List<Fala>();
    [SerializeField] private List<Fala> falasPaiPorta = new List<Fala>();
    [SerializeField] private List<Fala> falasAulaGaragem = new List<Fala>();
    [SerializeField] private List<Fala> falasPaiEscolher = new List<Fala>();
    [SerializeField] private List<Fala> falasDesistiu = new List<Fala>();
    [SerializeField] private List<Fala> falasEscolheuKicks = new List<Fala>();
    [SerializeField] private List<Fala> falasPintouKicks = new List<Fala>();
    [SerializeField] private List<Fala> falasEscolheuOutro = new List<Fala>();
    [SerializeField] private List<Fala> falasMercado = new List<Fala>();
    [SerializeField] private List<Fala> falasTrocouDeNovo = new List<Fala>();
    [SerializeField] private List<Fala> falasPaiDepois = new List<Fala>();
    [SerializeField] private List<Fala> falasMaeAntes = new List<Fala>();
    [SerializeField] private List<Fala> falasMaeDepois = new List<Fala>();
    [SerializeField] private List<Fala> falasSaida = new List<Fala>();

    private Etapa etapa = Etapa.Chegada;
    private bool emAula;

    // Usado pela ferramenta do Editor
    public void DefinirCaminhos(List<Passo> paiPorta, Direcao paiParadoPorta, List<Passo> maeCozinha, Direcao maeParada,
                                List<Passo> paiGaragem, Direcao paiParadoGaragem, int garagemX)
    {
        paiAtePorta = paiPorta;
        paiNaPorta = paiParadoPorta;
        maeAteCozinha = maeCozinha;
        maeNaCozinha = maeParada;
        paiAteGaragem = paiGaragem;
        paiNaGaragem = paiParadoGaragem;
        inicioGaragemX = garagemX;
    }

    public void DefinirFalas(List<Fala> chegada, List<Fala> paiPorta, List<Fala> aula, List<Fala> paiEscolher,
                             List<Fala> desistiu, List<Fala> kicks, List<Fala> pintou, List<Fala> outro,
                             List<Fala> mercado, List<Fala> trocouDeNovo, List<Fala> paiDepois,
                             List<Fala> maeAntes, List<Fala> maeDepois, List<Fala> saida)
    {
        falasChegada = chegada;
        falasPaiPorta = paiPorta;
        falasAulaGaragem = aula;
        falasPaiEscolher = paiEscolher;
        falasDesistiu = desistiu;
        falasEscolheuKicks = kicks;
        falasPintouKicks = pintou;
        falasEscolheuOutro = outro;
        falasMercado = mercado;
        falasTrocouDeNovo = trocouDeNovo;
        falasPaiDepois = paiDepois;
        falasMaeAntes = maeAntes;
        falasMaeDepois = maeDepois;
        falasSaida = saida;
    }

    private void Start()
    {
        if (jogador == null || dialogo == null || portaGaragem == null || portao == null || telaGaragem == null)
        {
            Debug.LogError("RoteiroGaragem: monte a cena com Mr Driver > História > 7. Criar cena da volta para casa (garagem).", this);
            enabled = false;
            return;
        }

        Time.timeScale = 1f;
        StartCoroutine(Chegada());
    }

    // ---------- 1) Chegada ----------

    private IEnumerator Chegada()
    {
        jogador.Travado = true;
        TransicaoTela.ComecarEscuro();
        yield return new WaitForSecondsRealtime(0.5f);
        yield return TransicaoTela.Clarear(1.2f);

        AnimadorPersonagem animMae = mae != null ? mae.GetComponent<AnimadorPersonagem>() : null;
        if (animMae != null) animMae.MostrarEmote(Emote.Exclamacao, 1.4f);
        yield return new WaitForSeconds(0.5f);

        yield return dialogo.Conversar(falasChegada);

        // Cada um vai para o seu lugar
        Coroutine a = StartCoroutine(Caminhar(pai, paiAtePorta, paiNaPorta));
        Coroutine b = StartCoroutine(Caminhar(mae, maeAteCozinha, maeNaCozinha));
        yield return a;
        yield return b;

        if (objetivo != null) objetivo.Mostrar(objetivoPai);
        etapa = Etapa.Livre;
        jogador.Travado = false;
    }

    private IEnumerator Caminhar(NpcHistoria npc, List<Passo> caminho, Direcao fim)
    {
        if (npc == null) yield break;
        if (caminho != null)
            foreach (Passo p in caminho)
                yield return npc.Movimento.Andar(p.direcao, p.quantidade);
        npc.MudarParado(fim);
    }

    // ---------- Conversas ----------

    public override IEnumerator ConversarCom(NpcHistoria npc)
    {
        if (npc == pai)
        {
            switch (etapa)
            {
                case Etapa.Livre:
                    yield return AbrirGaragem();
                    break;
                case Etapa.PortaAberta:
                case Etapa.Escolher:
                    yield return dialogo.Conversar(falasPaiEscolher);
                    break;
                default:
                    yield return dialogo.Conversar(falasPaiDepois);
                    break;
            }
        }
        else if (npc == mae)
        {
            yield return dialogo.Conversar(etapa >= Etapa.Mercado ? falasMaeDepois : falasMaeAntes);
        }
    }

    // ---------- 2) O pai abre a garagem ----------

    private IEnumerator AbrirGaragem()
    {
        if (objetivo != null) objetivo.Esconder();
        yield return dialogo.Conversar(falasPaiPorta);

        portaGaragem.Liberar();
        yield return new WaitForSeconds(0.3f);
        yield return Caminhar(pai, paiAteGaragem, paiNaGaragem);

        etapa = Etapa.PortaAberta;
        if (objetivo != null) objetivo.Mostrar(objetivoEntrar);
    }

    // ---------- 3) Aula da garagem (quando o Jason entra) ----------

    private void Update()
    {
        if (etapa != Etapa.PortaAberta || emAula || jogador == null) return;
        if (jogador.Celula.x >= inicioGaragemX && !jogador.Movimento.Andando && !CaixaDialogo.AlgumaAberta)
            StartCoroutine(AulaGaragem());
    }

    private IEnumerator AulaGaragem()
    {
        emAula = true;
        jogador.Travado = true;
        if (objetivo != null) objetivo.Esconder();

        if (pai != null)
        {
            pai.Movimento.VirarPara(jogador.Celula);
            jogador.Movimento.VirarPara(pai.Movimento.Celula);
            AnimadorPersonagem animPai = pai.GetComponent<AnimadorPersonagem>();
            if (animPai != null) animPai.MostrarEmote(Emote.Exclamacao, 1.1f);
        }
        yield return new WaitForSeconds(0.4f);

        yield return dialogo.Conversar(falasAulaGaragem);

        etapa = Etapa.Escolher;
        if (objetivo != null) objetivo.Mostrar(objetivoEscolher);
        jogador.Travado = false;
        emAula = false;
    }

    // ---------- 4) Escolher o carro ----------

    public override IEnumerator UsarGaragem(VitrineCarro vitrine)
    {
        if (etapa < Etapa.Escolher || etapa == Etapa.Saindo) yield break;

        if (objetivo != null) objetivo.Esconder();
        bool confirmou = false;
        yield return telaGaragem.Abrir(r => confirmou = r);

        if (etapa == Etapa.Escolher)
        {
            if (!confirmou)
            {
                yield return dialogo.Conversar(falasDesistiu);
                if (objetivo != null) objetivo.Mostrar(objetivoEscolher);
                yield break;
            }

            yield return dialogo.Conversar(ReacaoDoPai());
            yield return dialogo.Conversar(falasMercado);

            portao.Liberar();
            yield return new WaitForSeconds(0.9f);
            etapa = Etapa.Mercado;
        }
        else if (confirmou)
        {
            yield return dialogo.Conversar(falasTrocouDeNovo);
        }

        if (objetivo != null) objetivo.Mostrar(objetivoSair);
    }

    private List<Fala> ReacaoDoPai()
    {
        if (GaragemJogador.ModeloId != GaragemJogador.ModeloPadrao) return falasEscolheuOutro;
        return GaragemJogador.CorIndice == 0 ? falasEscolheuKicks : falasPintouKicks;
    }

    // ---------- Saída ----------

    public override void Sair(PortaCasa porta)
    {
        if (porta != portao || etapa != Etapa.Mercado) return;
        etapa = Etapa.Saindo;
        StartCoroutine(RotinaSaida());
    }

    private IEnumerator RotinaSaida()
    {
        jogador.Travado = true;
        if (objetivo != null) objetivo.Esconder();
        jogador.Movimento.Virar(Direcao.Baixo);
        yield return new WaitForSeconds(0.2f);

        yield return dialogo.Conversar(falasSaida);

        ProgressoNiveis.Concluir(ProgressoHistoria.CenaGaragem);
        TransicaoTela.Carregar(proximaCena);
    }
}
