using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Roteiro da cena Historia_Mercado (dentro do Mercado Bom Preço, no meio do Nivel1):
// 1) O Jason pega as compras da lista nas prateleiras (leite, ovos, pão de forma, café e fermento).
// 2) No caixa, a Jussara conversa e conta da Turbo Peças (a loja de peças do primo dela).
// 3) A Gangue do Escapamento invade o mercado fazendo baderna e sai provocando o Jason.
// 4) O Jason sai pela porta e volta para o Nivel1, agora com os rebeldes nas ruas.
// Todas as falas podem ser editadas aqui no Inspector.
public class RoteiroMercado : RoteiroBase
{
    [Serializable]
    public class Item
    {
        public string id;
        public string nome;
    }

    [Serializable]
    public class Rebelde
    {
        public NpcHistoria npc;
        public List<RoteiroGaragem.Passo> entrada = new List<RoteiroGaragem.Passo>();
        public Direcao olhando = Direcao.Baixo;
        public List<RoteiroGaragem.Passo> saida = new List<RoteiroGaragem.Passo>();
    }

    private enum Etapa { Entrando, Compras, Caixa, Baderna, Livre, Saindo }

    [Header("Ligações (montadas pela ferramenta)")]
    [SerializeField] private JogadorCasa jogador;
    [SerializeField] private NpcHistoria atendente;
    [SerializeField] private PortaCasa porta;
    [SerializeField] private ObjetivoHUD objetivo;
    [SerializeField] private CaixaDialogo dialogo;

    [Header("Compras")]
    [SerializeField] private List<Item> lista = new List<Item>();

    [Header("Baderna")]
    [SerializeField] private List<Rebelde> rebeldes = new List<Rebelde>();
    [SerializeField] private SpriteRenderer latas;
    [SerializeField] private Sprite latasDerrubadas;
    [SerializeField] private ObjetoFala objetoLatas;

    [Header("Objetivos ({n}, {total} e {lista} são trocados)")]
    [SerializeField] private string objetivoCompras = "Pegue as compras ({n} de {total}): {lista}";
    [SerializeField] private string objetivoCaixa = "Vá até o caixa e pague as compras";
    [SerializeField] private string objetivoSair = "Saia do mercado e volte para o carro";

    [Header("Próxima cena")]
    [SerializeField] private string proximaCena = ProgressoHistoria.PrimeiroNivel;

    [Header("Falas")]
    [SerializeField] private List<Fala> falasEntrada = new List<Fala>();
    [SerializeField] private List<Fala> falasJaPegou = new List<Fala>();
    [SerializeField] private List<Fala> falasCaixaAntes = new List<Fala>();
    [SerializeField] private List<Fala> falasCaixa = new List<Fala>();
    [SerializeField] private List<Fala> falasChegadaRebeldes = new List<Fala>();
    [SerializeField] private List<Fala> falasBaderna = new List<Fala>();
    [SerializeField] private List<Fala> falasDepoisBaderna = new List<Fala>();
    [SerializeField] private List<Fala> falasAtendenteDepois = new List<Fala>();
    [SerializeField] private List<Fala> falasLatasDerrubadas = new List<Fala>();
    [SerializeField] private List<Fala> falasSaida = new List<Fala>();

    private readonly HashSet<string> pegos = new HashSet<string>();
    private Etapa etapa = Etapa.Entrando;

    // Usado pela ferramenta do Editor
    public void DefinirCompras(List<Item> itens, List<Rebelde> gangue, SpriteRenderer srLatas, Sprite derrubadas, ObjetoFala falasLatas)
    {
        lista = itens;
        rebeldes = gangue;
        latas = srLatas;
        latasDerrubadas = derrubadas;
        objetoLatas = falasLatas;
    }

    public void DefinirFalas(List<Fala> entrada, List<Fala> jaPegou, List<Fala> caixaAntes, List<Fala> caixa,
                             List<Fala> chegadaRebeldes, List<Fala> baderna, List<Fala> depoisBaderna,
                             List<Fala> atendenteDepois, List<Fala> latasDerrubadasFalas, List<Fala> saida)
    {
        falasEntrada = entrada;
        falasJaPegou = jaPegou;
        falasCaixaAntes = caixaAntes;
        falasCaixa = caixa;
        falasChegadaRebeldes = chegadaRebeldes;
        falasBaderna = baderna;
        falasDepoisBaderna = depoisBaderna;
        falasAtendenteDepois = atendenteDepois;
        falasLatasDerrubadas = latasDerrubadasFalas;
        falasSaida = saida;
    }

    private void Start()
    {
        if (jogador == null || dialogo == null || porta == null)
        {
            Debug.LogError("RoteiroMercado: monte a cena com Mr Driver > História > 9. Criar cena do mercado.", this);
            enabled = false;
            return;
        }

        foreach (Rebelde r in rebeldes)
            if (r.npc != null) r.npc.gameObject.SetActive(false);   // só aparecem na hora da baderna

        Time.timeScale = 1f;
        StartCoroutine(Entrada());
    }

    private IEnumerator Entrada()
    {
        jogador.Travado = true;
        TransicaoTela.ComecarEscuro();
        yield return new WaitForSecondsRealtime(0.4f);
        yield return TransicaoTela.Clarear(1f);

        yield return dialogo.Conversar(falasEntrada);

        etapa = Etapa.Compras;
        AtualizarObjetivo();
        jogador.Travado = false;
    }

    // ---------- Compras ----------

    private bool TudoPego => pegos.Count >= lista.Count;

    private void AtualizarObjetivo()
    {
        if (objetivo == null) return;
        if (etapa == Etapa.Compras && !TudoPego)
        {
            List<string> faltam = new List<string>();
            foreach (Item i in lista) if (!pegos.Contains(i.id)) faltam.Add(i.nome);
            objetivo.Mostrar(objetivoCompras.Replace("{n}", pegos.Count.ToString())
                                            .Replace("{total}", lista.Count.ToString())
                                            .Replace("{lista}", string.Join(", ", faltam)));
        }
        else if (etapa == Etapa.Compras) objetivo.Mostrar(objetivoCaixa);
        else if (etapa == Etapa.Livre) objetivo.Mostrar(objetivoSair);
    }

    private bool EhDaLista(string id)
    {
        foreach (Item i in lista) if (i.id == id) return true;
        return false;
    }

    public override IEnumerator InteragirCom(ObjetoRoteiro objeto)
    {
        if (objeto.Id == "caixa")
        {
            yield return NoCaixa();
            yield break;
        }

        if (!EhDaLista(objeto.Id))
        {
            yield return dialogo.Conversar(objeto.Falas);   // ex.: o pudim em oferta
            yield break;
        }

        if (pegos.Contains(objeto.Id) || etapa != Etapa.Compras)
        {
            yield return dialogo.Conversar(falasJaPegou);
            yield break;
        }

        pegos.Add(objeto.Id);
        objeto.MostrarDestaque(false);
        Sons.Tocar("PegouItem");
        AnimadorPersonagem jason = jogador.GetComponent<AnimadorPersonagem>();
        if (jason != null) jason.MostrarEmote(Emote.Exclamacao, 0.9f);
        yield return dialogo.Conversar(objeto.Falas);
        AtualizarObjetivo();

        if (TudoPego && atendente != null)
        {
            AnimadorPersonagem anim = atendente.GetComponent<AnimadorPersonagem>();
            if (anim != null) anim.MostrarEmote(Emote.Coracao, 1.5f);
        }
    }

    public override IEnumerator ConversarCom(NpcHistoria npc)
    {
        if (npc == atendente) yield return NoCaixa();
        else if (npc != null) yield return dialogo.Conversar(falasAtendenteDepois);
    }

    private IEnumerator NoCaixa()
    {
        if (etapa == Etapa.Compras && !TudoPego) yield return dialogo.Conversar(falasCaixaAntes);
        else if (etapa == Etapa.Compras) yield return Pagar();
        else yield return dialogo.Conversar(falasAtendenteDepois);
    }

    // ---------- Caixa e baderna ----------

    private IEnumerator Pagar()
    {
        etapa = Etapa.Caixa;
        jogador.Travado = true;
        if (objetivo != null) objetivo.Esconder();

        if (atendente != null) atendente.Movimento.VirarPara(jogador.Celula);
        Sons.Tocar("CaixaRegistradora");
        yield return dialogo.Conversar(falasCaixa);

        // A gangue chega fazendo barulho (motos lá fora e a música muda)
        etapa = Etapa.Baderna;
        Sons.Tocar("BuzinaRebelde", 0.8f);
        Sons.MusicaPerseguicao();
        yield return dialogo.Conversar(falasChegadaRebeldes);
        porta.Liberar();
        jogador.Movimento.VirarPara(MapaCasa.ParaCelula(porta.transform.position));

        foreach (Rebelde r in rebeldes)
        {
            if (r.npc == null) continue;
            r.npc.gameObject.SetActive(true);
            yield return Andar(r.npc, r.entrada, 2f);
            r.npc.MudarParado(r.olhando);
            if (r == rebeldes[0]) DerrubarLatas();
        }

        AnimadorPersonagem animAtendente = atendente != null ? atendente.GetComponent<AnimadorPersonagem>() : null;
        if (animAtendente != null) animAtendente.MostrarEmote(Emote.Exclamacao, 1.2f);
        yield return new WaitForSeconds(0.4f);

        yield return dialogo.Conversar(falasBaderna);

        // E vão embora correndo (o último a entrar sai primeiro)
        for (int i = rebeldes.Count - 1; i >= 0; i--)
        {
            Rebelde r = rebeldes[i];
            if (r.npc == null) continue;
            yield return Andar(r.npc, r.saida, 2.2f);
            r.npc.gameObject.SetActive(false);
        }

        if (atendente != null) atendente.Movimento.VirarPara(jogador.Celula);
        jogador.Movimento.VirarPara(atendente != null ? atendente.Movimento.Celula : jogador.Celula);
        Sons.MusicaDaCena();
        yield return dialogo.Conversar(falasDepoisBaderna);

        etapa = Etapa.Livre;
        AtualizarObjetivo();
        jogador.Travado = false;
    }

    private void DerrubarLatas()
    {
        if (latas != null && latasDerrubadas != null) latas.sprite = latasDerrubadas;
        Sons.Tocar("Latas");
        if (objetoLatas != null && falasLatasDerrubadas.Count > 0) objetoLatas.DefinirFalas(falasLatasDerrubadas);
    }

    private static IEnumerator Andar(NpcHistoria npc, List<RoteiroGaragem.Passo> caminho, float velocidade)
    {
        if (npc == null || caminho == null) yield break;
        foreach (RoteiroGaragem.Passo p in caminho)
            for (int i = 0; i < p.quantidade; i++)
                yield return npc.Movimento.Passo(p.direcao, velocidade);
    }

    // ---------- Saída ----------

    public override void Sair(PortaCasa saida)
    {
        if (saida != porta || etapa != Etapa.Livre) return;
        etapa = Etapa.Saindo;
        StartCoroutine(RotinaSaida());
    }

    private IEnumerator RotinaSaida()
    {
        jogador.Travado = true;
        if (objetivo != null) objetivo.Esconder();
        yield return dialogo.Conversar(falasSaida);

        ProgressoNiveis.Concluir(ProgressoHistoria.CenaMercado);
        MissaoMercado.VoltandoDoMercado = true;   // o Nivel1 continua da porta do mercado, com os rebeldes nas ruas
        TransicaoTela.Carregar(proximaCena);
    }
}
