using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// TURBO RACER 3000 (© 1987 MEGASOFT): estrada reta infinita, desviar dos carros, cada vez mais rápido.
// É de propósito simples e difícil: a primeira partida é o "jogo" que o Jason está jogando no quarto.
// Tudo acontece numa "tela" de 160x120 pixels (coordenadas virtuais, origem embaixo à esquerda).
// - Título (PRESS START) → carregando (a barra trava no meio) → jogo → batida → GAME OVER + "INSIRA UMA FICHA".
// - História: no fim da contagem a imagem da tela é guardada e a cena Historia_Casa mostra a TV encolhendo até o quarto.
// - Minijogo (menu, depois de terminar a história): ENTER joga de novo, ESC volta ao menu.
public class TurboRacer : MonoBehaviour
{
    private enum Estado { Titulo, Carregando, Jogando, Batida, FimDeJogo, Saindo }

    private class Inimigo
    {
        public RectTransform rt;
        public float x, y, w, h, velocidade;   // velocidade = quanto desce por segundo (relativo ao jogador)
        public float fatorVelocidade, extra;
        public bool caminhao, contramao, trocaFaixa, trocou;
        public int faixa;
        public float alvoX, gatilhoY;
    }

    [Header("Tela (montada pela ferramenta)")]
    [SerializeField] private RectTransform gramaEsquerda;
    [SerializeField] private RectTransform gramaDireita;
    [SerializeField] private RectTransform[] faixasPista;
    [SerializeField] private RectTransform[] enfeites;
    [SerializeField] private RectTransform objetos;          // onde os carros inimigos são criados
    [SerializeField] private Image jogador;
    [SerializeField] private Image explosao;
    [SerializeField] private Image reflexo;                  // o rosto do Jason refletido na tela (quase invisível)
    [SerializeField] private TextoPixel txtPontos, txtRecorde, txtCentro, txtSub, txtDica;
    [SerializeField] private GameObject painelTitulo, painelCarregando;
    [SerializeField] private TextoPixel txtStart, txtRecordeTitulo;
    [SerializeField] private RectTransform barraCarregando;

    [Header("Desenhos")]
    [SerializeField] private Sprite[] carros;
    [SerializeField] private Sprite caminhao;
    [SerializeField] private Sprite[] quadrosExplosao;

    [Header("Dificuldade")]
    [SerializeField] private float velocidadeInicial = 70f;
    [SerializeField] private float velocidadeMaxima = 235f;
    [SerializeField] private float segundosAteMaxima = 50f;
    [SerializeField] private float intervaloInicial = 1.1f;
    [SerializeField] private float intervaloMinimo = 0.34f;
    [SerializeField] private float velocidadeLateral = 95f;
    [SerializeField] private float aceleracaoLateral = 520f;
    [SerializeField] private int segundosFicha = 10;

    private const float Largura = 160f, Altura = 120f;
    private const float PistaEsquerda = 44f, PistaDireita = 116f, JogadorY = 20f;
    private static readonly float[] Faixas = { 56f, 80f, 104f };
    private static readonly Color Amarelo = new Color32(255, 220, 60, 255), Vermelho = new Color32(255, 60, 60, 255),
                                  Branco = Color.white, Cinza = new Color32(170, 170, 170, 255), Ciano = new Color32(90, 230, 255, 255);

    private Estado estado = Estado.Titulo;
    private bool minijogo;
    private readonly List<Inimigo> inimigos = new List<Inimigo>();
    private readonly Stack<RectTransform> sobras = new Stack<RectTransform>();
    private float tempo, pontos, rolagem, velocidade, xJogador = 80f, vx, proximoCarro, proximaOnda;
    private int ultimoMarco, pontosMostrados = -1;
    private float contagem, fichaNegadaAte;
    private bool novoRecorde;
    private AudioSource motor;

    // ---------- Ciclo ----------

    private void Start()
    {
        minijogo = Videogame.ModoMinijogo;
        Videogame.ModoMinijogo = false;   // a próxima vez que alguém abrir esta cena pelo JOGAR, é a história
        Time.timeScale = 1f;

        Limpar();
        MostrarTitulo();
        motor = Sons.CriarLoop(gameObject, "RetroMotorLoop");
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        switch (estado)
        {
            case Estado.Titulo: Titulo(); break;
            case Estado.Jogando: Jogar(dt); break;
            case Estado.FimDeJogo: FimDeJogo(dt); break;
        }

        if (motor != null)
        {
            bool roncando = estado == Estado.Jogando;
            motor.volume = Mathf.MoveTowards(motor.volume, roncando ? Sons.VolumeDe("RetroMotorLoop") : 0f, dt * 3f);
            motor.pitch = 0.8f + 0.9f * Mathf.InverseLerp(velocidadeInicial, velocidadeMaxima, velocidade);
        }
    }

    // ---------- Título ----------

    private void MostrarTitulo()
    {
        estado = Estado.Titulo;
        if (painelTitulo != null) painelTitulo.SetActive(true);
        if (painelCarregando != null) painelCarregando.SetActive(false);
        Mostrar(txtCentro, ""); Mostrar(txtSub, ""); Mostrar(txtDica, "");
        jogador.gameObject.SetActive(true);
        explosao.gameObject.SetActive(false);
        if (txtRecordeTitulo != null)
            txtRecordeTitulo.Texto = minijogo ? "SEU RECORDE: " + Videogame.Recorde : "1 JOGADOR";
        AtualizarHud(true);
        DefinirReflexo(0.025f);
    }

    private void Titulo()
    {
        if (txtStart != null) txtStart.gameObject.SetActive(Mathf.Repeat(Time.time, 1f) < 0.6f);
        Rolar(25f * Time.deltaTime);   // a estrada anda devagarinho atrás do título

        if (Confirmou())
        {
            Sons.Tocar("RetroStart");
            StartCoroutine(Carregar());
        }
        else if (minijogo && Saiu()) VoltarAoMenu();
    }

    // "Carregando" com a barra travando no meio (como fita cassete velha)
    private IEnumerator Carregar()
    {
        estado = Estado.Carregando;
        if (painelTitulo != null) painelTitulo.SetActive(false);
        if (painelCarregando != null) painelCarregando.SetActive(true);
        Sons.Tocar("RetroCarregando");

        float total = 0f;
        while (total < 0.5f)
        {
            total = Mathf.Min(0.5f, total + Time.deltaTime * 0.8f);
            AtualizarBarra(total);
            yield return null;
        }
        yield return new WaitForSeconds(1.1f);   // ...travou? ...não, voltou
        Sons.Tocar("RetroCarregando");
        while (total < 1f)
        {
            total = Mathf.Min(1f, total + Time.deltaTime * 1.1f);
            AtualizarBarra(total);
            yield return null;
        }
        yield return new WaitForSeconds(0.25f);
        if (painelCarregando != null) painelCarregando.SetActive(false);
        ComecarPartida();
    }

    private void AtualizarBarra(float k)
    {
        if (barraCarregando != null) barraCarregando.sizeDelta = new Vector2(Mathf.Floor(78f * k), barraCarregando.sizeDelta.y);
    }

    // ---------- Partida ----------

    private void ComecarPartida()
    {
        Limpar();
        estado = Estado.Jogando;
        tempo = 0f;
        pontos = 0f;
        ultimoMarco = 0;
        velocidade = velocidadeInicial;
        xJogador = Faixas[1];
        vx = 0f;
        proximoCarro = 1.2f;
        proximaOnda = 31f;
        novoRecorde = false;
        jogador.gameObject.SetActive(true);
        explosao.gameObject.SetActive(false);
        Mostrar(txtCentro, ""); Mostrar(txtSub, "");
        StartCoroutine(PiscarDica("< >  DESVIAR", 2.6f, Branco));
        Sons.MusicaDaCena();
    }

    private void Jogar(float dt)
    {
        tempo += dt;
        float k = Mathf.Clamp01(tempo / segundosAteMaxima);
        velocidade = Mathf.Lerp(velocidadeInicial, velocidadeMaxima, k * k * (3f - 2f * k)) + Mathf.Max(0f, tempo - segundosAteMaxima) * 1.5f;

        float distancia = velocidade * dt;
        Rolar(distancia);
        pontos += distancia * 0.5f;

        // Marcos de pontos: bipe e "MAIS RÁPIDO!"
        int marco = (int)(pontos / 1000f);
        if (marco > ultimoMarco)
        {
            ultimoMarco = marco;
            Sons.Tocar("RetroPonto");
            StartCoroutine(PiscarDica("MAIS RÁPIDO!", 1.4f, Amarelo));
        }

        // Jogador: um pouco de inércia nos lados (deixa tudo mais difícil, como nos jogos antigos)
        float entrada = (Direita() ? 1f : 0f) - (Esquerda() ? 1f : 0f);
        vx = Mathf.MoveTowards(vx, entrada * velocidadeLateral, aceleracaoLateral * dt);
        xJogador += vx * dt;
        float min = PistaEsquerda + 8f, max = PistaDireita - 8f;
        if (xJogador < min || xJogador > max) { xJogador = Mathf.Clamp(xJogador, min, max); vx = 0f; }
        Posicionar(jogador.rectTransform, xJogador, JogadorY);

        // Novos carros
        proximoCarro -= dt;
        if (proximoCarro <= 0f)
        {
            Gerar();
            proximoCarro = Mathf.Lerp(intervaloInicial, intervaloMinimo, k) * Random.Range(0.8f, 1.2f);
        }
        if (tempo >= proximaOnda)
        {
            StartCoroutine(OndaContramao());
            proximaOnda = tempo + Random.Range(5.5f, 8.5f);
        }

        MoverInimigos(dt);
        AtualizarHud(false);
        DefinirReflexo(Mathf.Lerp(0.025f, 0.13f, pontos / 2500f));

        if (Bateu()) StartCoroutine(Batida());
    }

    private void Rolar(float distancia)
    {
        rolagem += distancia;
        // A grama e as faixas "andam" (o desenho repete a cada 8 e a cada 12 pixels)
        if (gramaEsquerda != null) gramaEsquerda.anchoredPosition = new Vector2(gramaEsquerda.anchoredPosition.x, -Mathf.Repeat(rolagem, 8f));
        if (gramaDireita != null) gramaDireita.anchoredPosition = new Vector2(gramaDireita.anchoredPosition.x, -Mathf.Repeat(rolagem, 8f));
        if (faixasPista != null)
            foreach (RectTransform f in faixasPista)
                if (f != null) f.anchoredPosition = new Vector2(f.anchoredPosition.x, -Mathf.Repeat(rolagem, 12f));

        // Árvores e placas descendo pela grama
        if (enfeites == null) return;
        foreach (RectTransform e in enfeites)
        {
            if (e == null) continue;
            Vector2 p = e.anchoredPosition;
            p.y -= distancia;
            if (p.y < -14f)
            {
                p.y = Altura + Random.Range(8f, 70f);
                bool esquerda = Random.value < 0.5f;
                p.x = esquerda ? Random.Range(8f, PistaEsquerda - 10f) : Random.Range(PistaDireita + 10f, Largura - 8f);
            }
            e.anchoredPosition = new Vector2(Mathf.Round(p.x), p.y);
        }
    }

    // ---------- Inimigos ----------

    private void Gerar()
    {
        float r = Random.value;
        bool ehCaminhao = tempo > 17f && r < 0.17f;
        bool troca = !ehCaminhao && tempo > 8f && r < 0.5f;

        bool[] ocupadas = FaixasOcupadas(60f);
        int livres = 0;
        foreach (bool o in ocupadas) if (!o) livres++;

        if (ehCaminhao)
        {
            // Caminhão ocupa duas faixas: só entra se as três estiverem livres (sempre sobra uma)
            if (livres < 3) { ehCaminhao = false; }
            else
            {
                int par = Random.value < 0.5f ? 0 : 1;
                Criar(par, true, false, false);
                return;
            }
        }

        // Nunca fecha as três faixas de uma vez (difícil, mas sempre dá para passar)
        if (livres <= 1) return;
        List<int> opcoes = new List<int>();
        for (int i = 0; i < 3; i++) if (!ocupadas[i]) opcoes.Add(i);
        int faixa = opcoes[Random.Range(0, opcoes.Count)];
        Criar(faixa, false, false, troca);
    }

    // Os carros na contramão vêm em fila, rapidinho, numa faixa só
    private IEnumerator OndaContramao()
    {
        int faixa = Random.Range(0, 3);
        int quantos = Random.Range(3, 5);
        StartCoroutine(PiscarDica("CONTRAMÃO!", 1.2f, Vermelho));
        for (int i = 0; i < quantos && estado == Estado.Jogando; i++)
        {
            bool[] ocupadas = FaixasOcupadas(40f);
            int livres = 0;
            foreach (bool o in ocupadas) if (!o) livres++;
            if (!ocupadas[faixa] && livres >= 2) Criar(faixa, false, true, false);
            yield return new WaitForSeconds(0.32f);
        }
    }

    private bool[] FaixasOcupadas(float faixaDeCima)
    {
        bool[] o = new bool[3];
        foreach (Inimigo e in inimigos)
        {
            if (e.y + e.h / 2f < Altura - faixaDeCima) continue;
            for (int i = 0; i < 3; i++)
                if (Mathf.Abs(e.x - Faixas[i]) < e.w / 2f + 6f) o[i] = true;
            if (e.trocaFaixa && !e.trocou)   // vai mudar de faixa: conta as duas
                for (int i = 0; i < 3; i++)
                    if (Mathf.Abs(e.alvoX - Faixas[i]) < 1f) o[i] = true;
        }
        return o;
    }

    private void Criar(int faixa, bool ehCaminhao, bool contramao, bool troca)
    {
        Inimigo e = new Inimigo { caminhao = ehCaminhao, contramao = contramao, trocaFaixa = troca, faixa = faixa };
        e.rt = NovoCarro();
        Image img = e.rt.GetComponent<Image>();
        img.sprite = ehCaminhao ? caminhao : carros[Random.Range(1, carros.Length)];   // o 0 é o do jogador
        e.w = img.sprite.rect.width;
        e.h = img.sprite.rect.height;
        e.rt.sizeDelta = new Vector2(e.w, e.h);
        e.rt.localEulerAngles = new Vector3(0f, 0f, contramao ? 180f : 0f);

        e.x = ehCaminhao ? (Faixas[faixa] + Faixas[faixa + 1]) / 2f : Faixas[faixa];
        e.y = Altura + e.h / 2f + 2f;
        e.fatorVelocidade = contramao ? 1.35f : ehCaminhao ? 0.32f : Random.Range(0.38f, 0.55f);
        e.extra = contramao ? 25f : 0f;

        if (troca)
        {
            // Muda de faixa SEM DAR SETA (de preferência para a faixa do jogador... que maldade)
            int para;
            if (faixa == 0) para = 1;
            else if (faixa == 2) para = 1;
            else para = xJogador < Faixas[1] ? 0 : 2;
            e.alvoX = Faixas[para];
            e.gatilhoY = Random.Range(55f, 85f);
        }
        Posicionar(e.rt, e.x, e.y);
        inimigos.Add(e);
    }

    private RectTransform NovoCarro()
    {
        RectTransform rt;
        if (sobras.Count > 0) rt = sobras.Pop();
        else
        {
            GameObject obj = new GameObject("Carro", typeof(RectTransform), typeof(Image));
            obj.layer = gameObject.layer;
            obj.transform.SetParent(objetos, false);
            rt = (RectTransform)obj.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            obj.GetComponent<Image>().raycastTarget = false;
        }
        rt.gameObject.SetActive(true);
        return rt;
    }

    private void MoverInimigos(float dt)
    {
        for (int i = inimigos.Count - 1; i >= 0; i--)
        {
            Inimigo e = inimigos[i];
            e.y -= (velocidade * e.fatorVelocidade + e.extra) * dt;
            if (e.trocaFaixa && !e.trocou && e.y < e.gatilhoY) e.trocou = true;
            if (e.trocou) e.x = Mathf.MoveTowards(e.x, e.alvoX, 38f * dt);
            Posicionar(e.rt, e.x, e.y);
            if (e.y < -e.h)
            {
                e.rt.gameObject.SetActive(false);
                sobras.Push(e.rt);
                inimigos.RemoveAt(i);
            }
        }
    }

    private bool Bateu()
    {
        Rect eu = new Rect(xJogador - 5f, JogadorY - 8.5f, 10f, 17f);
        foreach (Inimigo e in inimigos)
        {
            Rect r = new Rect(e.x - e.w / 2f + 2f, e.y - e.h / 2f + 2f, e.w - 4f, e.h - 4f);
            if (eu.Overlaps(r)) return true;
        }
        return false;
    }

    // ---------- Batida e fim de jogo ----------

    private IEnumerator Batida()
    {
        estado = Estado.Batida;
        Sons.Tocar("RetroBatida");
        Sons.PararMusica();

        explosao.gameObject.SetActive(true);
        Posicionar(explosao.rectTransform, xJogador, JogadorY + 2f);
        for (int i = 0; i < 12; i++)
        {
            explosao.sprite = quadrosExplosao[i % quadrosExplosao.Length];
            explosao.rectTransform.sizeDelta = new Vector2(explosao.sprite.rect.width, explosao.sprite.rect.height);
            if (i == 2) jogador.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.08f);
        }
        explosao.sprite = quadrosExplosao[quadrosExplosao.Length - 1];

        novoRecorde = Videogame.RegistrarPontos((int)pontos);
        AtualizarHud(true);
        Sons.Tocar("RetroGameOver");
        DefinirReflexo(0.17f);   // a tela "escurece" e o reflexo do Jason aparece mais

        estado = Estado.FimDeJogo;
        contagem = segundosFicha + 0.99f;
        fichaNegadaAte = -1f;
        Mostrar(txtCentro, "GAME OVER", Vermelho);
        if (minijogo)
        {
            Mostrar(txtSub, novoRecorde ? "NOVO RECORDE!" : "PONTOS: " + (int)pontos, novoRecorde ? Amarelo : Branco);
            Mostrar(txtDica, "ENTER: DE NOVO   ESC: SAIR", Ciano);
        }
    }

    private void FimDeJogo(float dt)
    {
        bool aceso = Mathf.Repeat(Time.time, 0.8f) < 0.55f;
        if (txtCentro != null) txtCentro.gameObject.SetActive(aceso);

        if (minijogo)
        {
            if (Confirmou()) { Sons.Tocar("RetroStart"); ComecarPartida(); }
            else if (Saiu()) VoltarAoMenu();
            return;
        }

        // História: "INSIRA UMA FICHA" com contagem... e o Jason não tem ficha nenhuma
        int antes = Mathf.FloorToInt(contagem);
        contagem -= dt;
        int agora = Mathf.FloorToInt(contagem);
        if (agora != antes && agora >= 0) Sons.Tocar("RetroTique");

        if (Confirmou())
        {
            fichaNegadaAte = Time.time + 1.6f;
            Sons.Tocar("RetroFicha");
        }

        if (Time.time < fichaNegadaAte)
            Mostrar(txtSub, "SEM FICHAS!", Mathf.Repeat(Time.time, 0.3f) < 0.15f ? Vermelho : Amarelo);
        else
            Mostrar(txtSub, "INSIRA UMA FICHA  " + Mathf.Max(0, agora), Amarelo);

        if (contagem <= 0f) StartCoroutine(Sair());
    }

    // A partida da história acabou: guarda a imagem da tela e vai para o quarto do Jason
    private IEnumerator Sair()
    {
        estado = Estado.Saindo;
        if (txtCentro != null) txtCentro.gameObject.SetActive(true);
        Mostrar(txtSub, "INSIRA UMA FICHA  0", Amarelo);
        yield return new WaitForSeconds(0.5f);
        yield return new WaitForEndOfFrame();
        Videogame.GuardarTela(ScreenCapture.CaptureScreenshotAsTexture());
        Videogame.VeioDoVideogame = true;
        SceneManager.LoadScene(ProgressoHistoria.CenaCasa);
    }

    private void VoltarAoMenu()
    {
        estado = Estado.Saindo;
        Time.timeScale = 1f;
        SceneManager.LoadScene(ProgressoHistoria.CenaMenu);
    }

    // ---------- Tela ----------

    private void Limpar()
    {
        foreach (Inimigo e in inimigos)
        {
            e.rt.gameObject.SetActive(false);
            sobras.Push(e.rt);
        }
        inimigos.Clear();
        StopAllCoroutines();
        Posicionar(jogador.rectTransform, Faixas[1], JogadorY);
        pontosMostrados = -1;
    }

    private void AtualizarHud(bool forcar)
    {
        int p = (int)pontos;
        if (!forcar && p == pontosMostrados) return;
        pontosMostrados = p;
        if (txtPontos != null) txtPontos.Texto = "PONTOS " + p.ToString("000000");
        if (txtRecorde != null) txtRecorde.Texto = "1. TONHÃO " + Videogame.RecordeTonhao;
    }

    private IEnumerator PiscarDica(string texto, float duracao, Color cor)
    {
        for (float t = 0f; t < duracao && (estado == Estado.Jogando || estado == Estado.Carregando); t += 0.15f)
        {
            Mostrar(txtDica, Mathf.Repeat(t, 0.6f) < 0.4f ? texto : "", cor);
            yield return new WaitForSeconds(0.15f);
        }
        if (estado == Estado.Jogando) Mostrar(txtDica, "");
    }

    private static void Mostrar(TextoPixel t, string texto, Color? cor = null)
    {
        if (t == null) return;
        t.gameObject.SetActive(true);
        t.Texto = texto;
        if (cor.HasValue) t.Cor = cor.Value;
    }

    private void DefinirReflexo(float alfa)
    {
        if (reflexo == null) return;
        Color c = reflexo.color;
        c.a = alfa * (0.9f + 0.1f * Mathf.Sin(Time.time * 7f));
        reflexo.color = c;
    }

    private static void Posicionar(RectTransform rt, float x, float y)
    {
        // Sempre em pixels inteiros: nada de movimento "liso" num videogame de 1987
        rt.anchoredPosition = new Vector2(Mathf.Round(x), Mathf.Round(y));
    }

    // ---------- Teclado ----------

    private static bool Esquerda()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        return k != null && (k.leftArrowKey.isPressed || k.aKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
#endif
    }

    private static bool Direita()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        return k != null && (k.rightArrowKey.isPressed || k.dKey.isPressed);
#else
        return Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
#endif
    }

    private static bool Confirmou()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        return k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private static bool Saiu()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        return k != null && k.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
