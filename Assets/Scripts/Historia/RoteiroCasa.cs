using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Roteiro da cena Historia_Casa:
// 1) A imagem do TURBO RACER 3000 (o videogame que o jogador acabou de perder) encolhe até a TV do quarto:
//    era o Jason jogando! Ele reclama e a mãe grita lá da cozinha.
// 2) Ele precisa falar com o pai na sala (o pai conta que ele vai tirar a carteira).
// 3) A porta da frente é liberada; ao sair, a mãe dá o último aviso e o Jason vai para a autoescola.
// Todas as falas podem ser editadas aqui no Inspector.
public class RoteiroCasa : RoteiroBase
{
    [Header("Ligações (montadas pela ferramenta)")]
    [SerializeField] private JogadorCasa jogador;
    [SerializeField] private NpcHistoria pai;
    [SerializeField] private NpcHistoria mae;
    [SerializeField] private PortaCasa porta;
    [SerializeField] private ObjetivoHUD objetivo;
    [SerializeField] private CaixaDialogo dialogo;

    [Header("Objetivos")]
    [SerializeField] private string objetivoInicial = "Fale com o seu pai na sala";
    [SerializeField] private string objetivoSair = "Saia de casa pela porta da frente";

    [Header("Começo no videogame")]
    [SerializeField] private SpriteRenderer tvQuarto;
    [Tooltip("Onde fica a telinha no desenho da TV (pixels; x da esquerda, y de baixo).")]
    [SerializeField] private RectInt telaDaTV = new RectInt(4, 11, 8, 9);

    [Header("Próxima cena")]
    [SerializeField] private string proximaCena = ProgressoHistoria.CenaAutoescola;

    [Header("Falas")]
    [SerializeField] private List<Fala> falasAcordar = new List<Fala>();
    [SerializeField] private List<Fala> falasPai = new List<Fala>();
    [SerializeField] private List<Fala> falasPaiDepois = new List<Fala>();
    [SerializeField] private List<Fala> falasMaeAntes = new List<Fala>();
    [SerializeField] private List<Fala> falasMae = new List<Fala>();
    [SerializeField] private List<Fala> falasMaeDepois = new List<Fala>();
    [SerializeField] private List<Fala> falasPortaFechada = new List<Fala>();
    [SerializeField] private List<Fala> falasSaida = new List<Fala>();

    private bool conversouComPai;
    private bool conversouComMae;
    private bool saindo;

    public override bool ConversouComPai => conversouComPai;

    // Usado pela ferramenta do Editor para preencher as falas
    public void DefinirFalas(List<Fala> acordar, List<Fala> paiPrincipal, List<Fala> paiDepois,
                             List<Fala> maeAntes, List<Fala> maePrincipal, List<Fala> maeDepois,
                             List<Fala> portaFechada, List<Fala> saida)
    {
        falasAcordar = acordar;
        falasPai = paiPrincipal;
        falasPaiDepois = paiDepois;
        falasMaeAntes = maeAntes;
        falasMae = maePrincipal;
        falasMaeDepois = maeDepois;
        falasPortaFechada = portaFechada;
        falasSaida = saida;
    }

    private void Start()
    {
        if (jogador == null || dialogo == null || porta == null)
        {
            Debug.LogError("RoteiroCasa: monte a cena com Mr Driver > História > 2. Criar cena da casa.", this);
            enabled = false;
            return;
        }

        Time.timeScale = 1f;
        StartCoroutine(Abertura());
    }

    private IEnumerator Abertura()
    {
        jogador.Travado = true;
        bool doVideogame = Videogame.VeioDoVideogame && Videogame.UltimaTela != null && tvQuarto != null;
        Videogame.VeioDoVideogame = false;

        if (doVideogame) yield return SairDaTV();
        else
        {
            TransicaoTela.ComecarEscuro();
            yield return new WaitForSecondsRealtime(0.6f);
            yield return TransicaoTela.Clarear(1.4f);
        }

        AnimadorPersonagem jason = jogador.GetComponent<AnimadorPersonagem>();
        if (jason != null) jason.MostrarEmote(Emote.Exclamacao, 1.2f);
        yield return dialogo.Conversar(ComPontos(falasAcordar));

        if (objetivo != null) objetivo.Mostrar(objetivoInicial);
        jogador.Travado = false;
    }

    // ---------- O plot: era só o Jason jogando videogame ----------

    // A última imagem do videogame cobre a tela inteira e vai encolhendo até virar a telinha da TV do quarto
    private IEnumerator SairDaTV()
    {
        GameObject obj = new GameObject("SaindoDaTV", typeof(Canvas));
        Canvas canvas = obj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        RawImage imagem = new GameObject("ImagemDoVideogame", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        imagem.transform.SetParent(obj.transform, false);
        imagem.texture = Videogame.UltimaTela;
        imagem.raycastTarget = false;
        RectTransform rt = imagem.rectTransform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        Rect cheia = new Rect(0f, 0f, Screen.width, Screen.height);
        Aplicar(rt, cheia);

        // Espera a câmera da casa chegar no Jason antes de medir onde a TV aparece
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.8f);

        Sons.Tocar("Transicao");
        Rect alvo = TelaDaTVNaTela(cheia);
        const float duracao = 1.6f;
        for (float t = 0f; t < duracao; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duracao);
            // O tamanho diminui "por multiplicação" (parece a câmera se afastando de verdade)
            float largura = Mathf.Exp(Mathf.Lerp(Mathf.Log(cheia.width), Mathf.Log(Mathf.Max(1f, alvo.width)), k));
            float altura = Mathf.Exp(Mathf.Lerp(Mathf.Log(cheia.height), Mathf.Log(Mathf.Max(1f, alvo.height)), k));
            Vector2 centro = Vector2.Lerp(cheia.center, alvo.center, k);
            Aplicar(rt, new Rect(centro.x - largura / 2f, centro.y - altura / 2f, largura, altura));
            yield return null;
        }
        Aplicar(rt, alvo);

        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            imagem.color = new Color(1f, 1f, 1f, 1f - t / 0.35f);
            yield return null;
        }
        Destroy(obj);
        Videogame.LimparTela();
    }

    private Rect TelaDaTVNaTela(Rect reserva)
    {
        Camera cam = Camera.main;
        if (cam == null || tvQuarto == null || tvQuarto.sprite == null) return reserva;
        float ppu = tvQuarto.sprite.pixelsPerUnit;
        Vector3 origem = tvQuarto.transform.position;
        Vector3 a = cam.WorldToScreenPoint(origem + new Vector3(telaDaTV.x / ppu, telaDaTV.y / ppu, 0f));
        Vector3 b = cam.WorldToScreenPoint(origem + new Vector3(telaDaTV.xMax / ppu, telaDaTV.yMax / ppu, 0f));
        return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    private static void Aplicar(RectTransform rt, Rect r)
    {
        rt.anchoredPosition = r.position;
        rt.sizeDelta = r.size;
    }

    // {pontos} nas falas = os pontos da partida que o jogador acabou de fazer no videogame
    private static List<Fala> ComPontos(List<Fala> falas)
    {
        int pontos = Videogame.UltimosPontos > 0 ? Videogame.UltimosPontos : Videogame.Recorde;
        List<Fala> lista = new List<Fala>();
        foreach (Fala f in falas)
            lista.Add(new Fala(f.quem, f.texto.Replace("{pontos}", pontos.ToString()), f.emote, f.tremer));
        return lista;
    }

    // ---------- Conversas (chamadas pelos NPCs) ----------

    public override IEnumerator ConversarCom(NpcHistoria npc)
    {
        if (npc == pai)
        {
            if (!conversouComPai)
            {
                yield return dialogo.Conversar(falasPai);
                conversouComPai = true;
                porta.Liberar();
                if (objetivo != null) objetivo.Mostrar(objetivoSair);
            }
            else yield return dialogo.Conversar(falasPaiDepois);
        }
        else if (npc == mae)
        {
            if (!conversouComPai) yield return dialogo.Conversar(falasMaeAntes);
            else if (!conversouComMae)
            {
                yield return dialogo.Conversar(falasMae);
                conversouComMae = true;
            }
            else yield return dialogo.Conversar(falasMaeDepois);
        }
    }

    public override IEnumerator PortaFechada(PortaCasa porta)
    {
        yield return dialogo.Conversar(falasPortaFechada);
    }

    // ---------- Saída ----------

    public override void Sair(PortaCasa porta)
    {
        if (saindo) return;
        saindo = true;
        StartCoroutine(RotinaSaida());
    }

    private IEnumerator RotinaSaida()
    {
        jogador.Travado = true;
        if (objetivo != null) objetivo.Esconder();

        // A mãe vira para a porta antes do último aviso
        if (mae != null) mae.Movimento.VirarPara(jogador.Celula);
        jogador.Movimento.Virar(Direcao.Cima);
        yield return new WaitForSeconds(0.2f);

        yield return dialogo.Conversar(falasSaida);

        ProgressoNiveis.Concluir(ProgressoHistoria.CenaCasa);
        TransicaoTela.Carregar(proximaCena);
    }
}
