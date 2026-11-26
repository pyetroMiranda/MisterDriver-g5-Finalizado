using UnityEngine;

// Desenhos de dentro do Mercado Bom Preço (cena Historia_Mercado), no mesmo estilo da casa do Jason:
// 16 pixels por quadradinho, móveis com o ponto de apoio embaixo à esquerda.
public static partial class HistoriaArte
{
    public const string PastaMercado = "Assets/Imagens/Historia/Mercado";

    // '#' parede, 'm' chão do mercado, 'P' porta automática (saída)
    public static readonly string[] PlantaMercado =
    {
        "########################",
        "########################",
        "#mmmmmmmmmmmmmmmmmmmmmm#",
        "#mmmmmmmmmmmmmmmmmmmmmm#",
        "#mmmmmmmmmmmmmmmmmmmmmm#",
        "#mmmmmmmmmmmmmmmmmmmmmm#",
        "#mmmmmmmmmmmmmmmmmmmmmm#",
        "#mmmmmmmmmmmmmmmmmmmmmm#",
        "#mmmmmmmmmmmmmmmmmmmmmm#",
        "#mmmmmmmmmmmmmmmmmmmmmm#",
        "#mmmmmmmmmmmmmmmmmmmmmm#",
        "#mmmmmmmmmmmmmmmmmmmmmm#",
        "#mmmmmmmmmmmmmmmmmmmmmm#",
        "##########PP############",
    };

    private static readonly Color32 VermelhoMercado = new Color32(220, 40, 50, 255);
    private static readonly Color32 Metal = new Color32(206, 210, 220, 255);
    private static readonly Color32 MetalEscuro = new Color32(146, 152, 166, 255);
    private static readonly Color32 VidroGeladeira = new Color32(170, 222, 246, 255);
    private static readonly Color32 FrioDentro = new Color32(120, 180, 222, 255);

    // ======================================================================
    // Chão e paredes
    // ======================================================================

    public static Sprite DesenharFundoMercado()
    {
        planta = PlantaMercado;
        int largura = planta[0].Length, altura = planta.Length;
        Tela t = new Tela(largura * Px, altura * Px);

        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                int px = x * Px, py = y * Px;
                char c = planta[y][x];
                if (EhFrenteParede(x, y)) ParedeMercado(t, px, py);
                else if (c == '#') TopoDeParede(t, x, y);
                else if (c == 'P') PortaAutomatica(t, px, py, x);
                else PisoMercado(t, px, py);
            }

        // Sombra das paredes no chão
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                if (!EhChao(x, y) || planta[y][x] == 'P') continue;
                int px = x * Px, py = y * Px;
                if (y == 2) t.Escurecer(px, py, Px, 3, 0.8f);
                if (!EhChao(x - 1, y)) t.Escurecer(px, py, 2, Px, 0.85f);
            }

        // Placas dos setores na parede do fundo
        Placa(t, "LATICÍNIOS", 2, 4, new Color32(40, 110, 210, 255));
        Placa(t, "FRIOS", 6, 2, new Color32(30, 160, 190, 255));
        Placa(t, "PADARIA", 9, 4, new Color32(220, 120, 30, 255));
        Placa(t, "FRUTAS", 14, 3, new Color32(50, 150, 70, 255));
        Placa(t, "BEBIDAS", 18, 3, VermelhoMercado);

        // Tapete da entrada e adesivos de oferta no chão
        int tx = 9 * Px + 4, ty = 12 * Px + 3;
        t.Ret(tx, ty, Px * 4 - 8, Px - 4, new Color32(150, 40, 50, 255));
        t.Borda(tx, ty, Px * 4 - 8, Px - 4, new Color32(110, 24, 34, 255));
        for (int x = tx + 3; x < tx + Px * 4 - 11; x += 4) t.Ret(x, ty + 3, 2, Px - 10, new Color32(176, 60, 70, 255));
        AdesivoOferta(t, 4 * Px + 4, 10 * Px + 4);
        AdesivoOferta(t, 16 * Px + 4, 12 * Px + 4);

        return t.Salvar(PastaMercado, "MercadoFundo", new Vector2(0f, 1f));
    }

    private static void PisoMercado(Tela t, int px, int py)
    {
        Color32 a = new Color32(242, 242, 238, 255), b = new Color32(222, 226, 232, 255), rejunte = new Color32(202, 204, 210, 255);
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++)
            {
                t.Ret(px + x * 8, py + y * 8, 8, 8, ((px / 8 + x) + (py / 8 + y)) % 2 == 0 ? a : b);
                t.Ret(px + x * 8, py + y * 8 + 7, 8, 1, rejunte);
                t.Ret(px + x * 8 + 7, py + y * 8, 1, 8, rejunte);
            }
        if (Hash(px, py) % 9 == 0) t.P(px + 3, py + 4, Branco);   // brilho do chão encerado
    }

    private static void ParedeMercado(Tela t, int px, int py)
    {
        t.Ret(px, py, Px, Px, new Color32(196, 232, 206, 255));
        t.Ret(px, py, Px, 1, K);
        t.Ret(px, py + 11, Px, 2, VermelhoMercado);
        t.Ret(px, py + Px - 3, Px, 3, new Color32(110, 150, 120, 255));
        t.Ret(px, py + Px - 3, Px, 1, new Color32(84, 120, 94, 255));
    }

    private static void PortaAutomatica(Tela t, int px, int py, int x)
    {
        t.Ret(px, py, Px, Px, new Color32(232, 246, 255, 255));
        t.Ret(px, py, Px, 3, MetalEscuro);
        if (planta[13][x - 1] != 'P') t.Ret(px, py, 2, Px, K);
        if (planta[13][x + 1] != 'P') t.Ret(px + Px - 2, py, 2, Px, K);
        t.Ret(px + 3, py + 6, 2, 1, Branco);
    }

    private static void Placa(Tela t, string texto, int celula, int largura, Color32 cor)
    {
        int w = Mathf.Min(largura * Px - 2, AutoescolaArte.LarguraTexto(texto) + 6);
        int x0 = celula * Px + (largura * Px - w) / 2, y0 = 3;   // no alto da parede, acima das geladeiras
        t.Ret(x0, y0, w, 9, cor);
        t.Borda(x0, y0, w, 9, K);
        AutoescolaArte.EscreverEm(t, texto, x0 + w / 2, y0 + 2, 1, Branco, new Color32(0, 0, 0, 0));
    }

    private static void AdesivoOferta(Tela t, int x, int y)
    {
        Color32 amarelo = new Color32(255, 220, 60, 255);
        for (int yy = 0; yy < 8; yy++)
            for (int xx = 0; xx < 8; xx++)
            {
                int dx = xx - 4, dy = yy - 4;
                if (dx * dx + dy * dy <= 14) t.P(x + xx, y + yy, amarelo);
            }
        t.Ret(x + 2, y + 3, 4, 2, VermelhoMercado);
    }

    // ======================================================================
    // Móveis do mercado
    // ======================================================================

    // Geladeira vertical com portas de vidro (produtos: 0 = leite, 1 = frios, 2 = bebidas)
    public static Sprite Geladeira(string nome, int portas, int produtos)
    {
        int w = portas * 20 + 4;
        Tela t = new Tela(w, 34);
        t.Caixa(0, 0, w, 34, new Color32(240, 244, 248, 255));
        t.Ret(1, 1, w - 2, 4, produtos == 2 ? VermelhoMercado : new Color32(60, 120, 200, 255));
        for (int p = 0; p < portas; p++)
        {
            int x0 = 2 + p * 20;
            t.Ret(x0, 6, 19, 25, FrioDentro);
            for (int s = 0; s < 3; s++)
            {
                int ys = 13 + s * 8;
                t.Ret(x0, ys, 19, 1, Branco);
                for (int i = 0; i < 4; i++) Produto(t, x0 + 1 + i * 5, ys - 6, produtos, i + s + p);
            }
            for (int i = 0; i < 6; i++) t.P(x0 + 3 + i, 8 + i, new Color32(230, 246, 255, 255));   // reflexo no vidro
            t.Borda(x0, 6, 19, 25, MetalEscuro);
            t.Ret(x0 + 16, 14, 1, 8, CinzaEscuro);   // puxador
        }
        t.Ret(1, 31, w - 2, 2, MetalEscuro);
        return t.Salvar(PastaMercado, nome, Vector2.zero);
    }

    private static void Produto(Tela t, int x, int y, int tipo, int i)
    {
        switch (tipo)
        {
            case 0:   // caixinhas de leite
                t.Ret(x, y + 1, 4, 5, Branco);
                t.Ret(x, y, 4, 2, new Color32(60, 120, 220, 255));
                t.Ret(x + 1, y + 3, 2, 1, new Color32(60, 120, 220, 255));
                break;
            case 1:   // queijos e iogurtes
                if (i % 2 == 0) { t.Ret(x, y + 2, 4, 4, new Color32(250, 210, 70, 255)); t.P(x + 1, y + 3, new Color32(220, 170, 40, 255)); }
                else { t.Ret(x, y + 2, 3, 4, new Color32(255, 170, 200, 255)); t.Ret(x, y + 2, 3, 1, Branco); }
                break;
            default:  // garrafas
                Color32[] cores = { new Color32(220, 40, 50, 255), new Color32(255, 140, 30, 255), new Color32(60, 170, 80, 255),
                                    new Color32(110, 60, 30, 255) };
                Color32 c = cores[i % cores.Length];
                t.Ret(x + 1, y, 2, 1, CinzaEscuro);
                t.Ret(x, y + 1, 4, 5, c);
                t.Ret(x, y + 3, 4, 1, Branco);
                break;
        }
    }

    public static Sprite Padaria()
    {
        Tela t = new Tela(64, 30);
        // Cestos de pão francês em cima
        t.Caixa(0, 6, 64, 6, MadeiraClara);
        for (int c = 0; c < 4; c++)
        {
            int x0 = 3 + c * 15;
            t.Caixa(x0, 2, 13, 6, new Color32(200, 150, 90, 255));
            for (int i = 0; i < 3; i++) Pao(t, x0 + 1 + i * 4, 1);
        }
        // Prateleiras com pão de forma embalado
        t.Caixa(0, 11, 64, 19, Movel);
        for (int s = 0; s < 2; s++)
        {
            int ys = 13 + s * 8;
            t.Ret(1, ys + 6, 62, 1, MovelEscuro);
            for (int i = 0; i < 9; i++)
            {
                int x = 3 + i * 7;
                t.Ret(x, ys, 6, 6, new Color32(240, 240, 236, 200));
                t.Ret(x + 1, ys + 1, 4, 4, new Color32(214, 160, 96, 255));
                t.Ret(x, ys + 3, 6, 1, i % 2 == 0 ? VermelhoMercado : new Color32(60, 120, 220, 255));
                t.Borda(x, ys, 6, 6, MovelEscuro);
            }
        }
        return t.Salvar(PastaMercado, "Padaria", Vector2.zero);
    }

    private static void Pao(Tela t, int x, int y)
    {
        Color32 casca = new Color32(214, 150, 70, 255);
        t.Ret(x, y + 1, 4, 2, casca);
        t.Ret(x + 1, y, 2, 1, casca);
        t.P(x + 1, y + 1, new Color32(240, 190, 110, 255));
    }

    public static Sprite Hortifruti()
    {
        Tela t = new Tela(48, 26);
        Color32 caixote = new Color32(176, 120, 70, 255);
        Color32[] frutas = { new Color32(250, 220, 60, 255), new Color32(220, 40, 50, 255), new Color32(255, 150, 30, 255) };
        for (int c = 0; c < 3; c++)
        {
            int x0 = 1 + c * 16;
            t.Caixa(x0, 10, 14, 16, caixote);
            t.Ret(x0 + 1, 15, 12, 1, MovelEscuro);
            t.Ret(x0 + 1, 20, 12, 1, MovelEscuro);
            for (int fy = 0; fy < 3; fy++)
                for (int fx = 0; fx < 4; fx++)
                {
                    int x = x0 + 1 + fx * 3 + (fy % 2), y = 3 + fy * 3;
                    t.Ret(x, y, 3, 3, frutas[c]);
                    t.P(x, y, Branco);
                    t.P(x + 2, y + 2, Escuro(frutas[c], 0.7f));
                }
        }
        // Abacaxi com cara de bravo
        t.Ret(21, 0, 1, 3, new Color32(60, 160, 70, 255)); t.Ret(23, 0, 1, 3, new Color32(60, 160, 70, 255));
        t.Ret(20, 3, 6, 6, new Color32(230, 170, 50, 255));
        t.P(21, 5, K); t.P(24, 5, K); t.Ret(22, 7, 2, 1, K);
        return t.Salvar(PastaMercado, "Hortifruti", Vector2.zero);
    }

    // Gôndola de prateleiras (tipo: 0 = matinais/café, 1 = mercearia/ovos, 2 = limpeza, 3 = confeitaria/fermento)
    public static Sprite Gondola(string nome, int tipo)
    {
        Tela t = new Tela(80, 26);
        t.Caixa(0, 0, 80, 5, Metal);                // tampo visto de cima
        t.Ret(1, 1, 78, 1, Branco);
        t.Caixa(0, 4, 80, 22, MetalEscuro);
        t.Ret(1, 5, 78, 20, new Color32(186, 192, 204, 255));
        for (int s = 0; s < 2; s++)
        {
            int ys = 14 + s * 10;
            t.Ret(1, ys, 78, 2, CinzaEscuro);
            for (int x = 6; x < 76; x += 12) t.Ret(x, ys, 3, 1, new Color32(255, 220, 60, 255));   // etiqueta de preço
            for (int i = 0; i < 11; i++) ProdutoGondola(t, 2 + i * 7, ys - 8, tipo, i + s * 3);
        }
        return t.Salvar(PastaMercado, nome, Vector2.zero);
    }

    private static void ProdutoGondola(Tela t, int x, int y, int tipo, int i)
    {
        switch (tipo)
        {
            case 0:   // café e cereais
                if (i % 3 == 0) { t.Ret(x, y + 1, 6, 7, new Color32(110, 60, 30, 255)); t.Ret(x, y + 3, 6, 2, VermelhoMercado); }
                else if (i % 3 == 1) { t.Ret(x, y, 6, 8, new Color32(250, 210, 60, 255)); t.Ret(x + 1, y + 2, 4, 3, VermelhoMercado); }
                else { t.Ret(x + 1, y + 2, 4, 6, new Color32(130, 80, 40, 255)); t.Ret(x + 1, y + 2, 4, 1, new Color32(250, 210, 60, 255)); }
                break;
            case 1:   // ovos, arroz e feijão
                if (i % 3 == 0)
                {
                    t.Ret(x, y + 4, 6, 4, new Color32(230, 214, 180, 255));
                    t.P(x + 1, y + 4, Branco); t.P(x + 3, y + 4, Branco); t.P(x + 5, y + 4, Branco);
                }
                else if (i % 3 == 1) { t.Ret(x, y + 1, 6, 7, Branco); t.Ret(x, y + 4, 6, 2, new Color32(60, 120, 220, 255)); }
                else { t.Ret(x, y + 2, 6, 6, new Color32(80, 46, 30, 255)); t.Ret(x + 1, y + 3, 4, 2, new Color32(250, 210, 60, 255)); }
                break;
            case 2:   // limpeza
                Color32[] cores = { new Color32(60, 130, 230, 255), new Color32(80, 200, 120, 255), new Color32(255, 120, 190, 255) };
                Color32 c = cores[i % 3];
                t.Ret(x + 2, y, 2, 2, Branco);
                t.Ret(x + 1, y + 2, 4, 6, c);
                t.Ret(x + 2, y + 4, 2, 2, Branco);
                break;
            default:  // confeitaria: farinha, açúcar e fermento
                if (i % 3 == 0) { t.Ret(x, y + 1, 6, 7, Branco); t.Ret(x, y + 2, 6, 1, new Color32(220, 120, 30, 255)); }
                else if (i % 3 == 1) { t.Ret(x, y + 1, 6, 7, Branco); t.Ret(x, y + 4, 6, 2, new Color32(60, 120, 220, 255)); }
                else { t.Ret(x + 1, y + 3, 4, 5, VermelhoMercado); t.Ret(x + 1, y + 3, 4, 1, new Color32(250, 210, 60, 255)); }
                break;
        }
    }

    // Caixa (balcão com esteira e registradora). "fechado" = sem luz e com um gato dormindo na esteira.
    public static Sprite CaixaMercado(bool fechado)
    {
        Tela t = new Tela(32, 28);
        t.Caixa(0, 10, 32, 18, new Color32(90, 96, 110, 255));
        t.Ret(1, 12, 30, 1, Metal);
        t.Ret(2, 18, 28, 1, new Color32(70, 74, 86, 255));
        t.Caixa(0, 7, 21, 5, new Color32(40, 40, 46, 255));           // esteira
        for (int x = 3; x < 20; x += 4) t.Ret(x, 8, 1, 3, new Color32(70, 70, 80, 255));
        t.Caixa(21, 2, 10, 9, Metal);                                  // registradora
        t.Ret(22, 3, 8, 4, fechado ? new Color32(40, 50, 50, 255) : new Color32(90, 220, 140, 255));
        t.Ret(22, 8, 8, 2, CinzaEscuro);
        t.Ret(4, 0, 1, 7, CinzaEscuro);                                // poste da luzinha do caixa
        t.Caixa(2, 0, 5, 3, fechado ? VermelhoMercado : new Color32(60, 200, 90, 255));
        if (fechado)
        {
            Color32 gato = new Color32(240, 150, 60, 255);
            t.Ret(8, 6, 8, 4, gato);
            t.P(8, 5, gato); t.P(10, 5, gato);
            t.Ret(15, 8, 3, 1, gato);
            t.P(9, 7, K);
        }
        return t.Salvar(PastaMercado, fechado ? "CaixaFechado" : "CaixaAberto", Vector2.zero);
    }

    // Pirâmide de latas de milho (e a versão derrubada pela gangue)
    public static Sprite Latas(bool derrubadas)
    {
        Tela t = new Tela(32, 26);
        if (!derrubadas)
        {
            t.Caixa(1, 22, 30, 4, MadeiraClara);
            for (int fila = 0; fila < 4; fila++)
            {
                int n = 4 - fila, y = 16 - fila * 5;
                int x0 = 16 - n * 7 / 2;
                for (int i = 0; i < n; i++) Lata(t, x0 + i * 7, y, false);
            }
        }
        else
        {
            t.Caixa(1, 22, 30, 4, MadeiraClara);
            int[,] pos = { { 2, 17 }, { 9, 18 }, { 18, 16 }, { 24, 18 }, { 5, 12 }, { 14, 20 }, { 21, 11 }, { 27, 14 }, { 11, 9 }, { 0, 20 } };
            for (int i = 0; i < pos.GetLength(0); i++) Lata(t, pos[i, 0], pos[i, 1], i % 2 == 0);
        }
        return t.Salvar(PastaMercado, derrubadas ? "LatasDerrubadas" : "Latas", Vector2.zero);
    }

    private static void Lata(Tela t, int x, int y, bool deitada)
    {
        Color32 rotulo = new Color32(250, 210, 60, 255), verde = new Color32(60, 150, 70, 255);
        if (!deitada)
        {
            t.Caixa(x, y, 6, 6, rotulo);
            t.Ret(x + 1, y + 2, 4, 2, verde);
            t.Ret(x + 1, y + 1, 4, 1, Branco);
        }
        else
        {
            t.Caixa(x, y, 7, 5, rotulo);
            t.Ret(x + 3, y + 1, 2, 3, verde);
            t.Ret(x + 1, y + 1, 1, 3, Cinza);
        }
    }

    public static Sprite PudimOferta()
    {
        Tela t = new Tela(32, 32);
        // Plaquinha "OFERTA"
        t.Ret(15, 2, 2, 12, CinzaEscuro);
        t.Caixa(3, 0, 26, 9, new Color32(255, 220, 60, 255));
        AutoescolaArte.EscreverEm(t, "OFERTA", 16, 2, 1, VermelhoMercado, new Color32(0, 0, 0, 0));
        // Mesinha com os pudins
        t.Caixa(0, 18, 32, 14, VermelhoMercado);
        t.Ret(1, 19, 30, 2, new Color32(250, 250, 250, 255));
        for (int i = 0; i < 4; i++)
        {
            int x = 2 + i * 7;
            t.Caixa(x, 12, 6, 7, new Color32(250, 214, 110, 255));
            t.Ret(x + 1, 13, 4, 2, new Color32(150, 80, 30, 255));   // calda
        }
        AutoescolaArte.EscreverEm(t, "PUDIM", 16, 23, 1, Branco, new Color32(0, 0, 0, 0));
        return t.Salvar(PastaMercado, "PudimOferta", Vector2.zero);
    }

    public static Sprite Carrinhos()
    {
        Tela t = new Tela(32, 22);
        for (int c = 0; c < 2; c++)
        {
            int x0 = 2 + c * 12;
            for (int y = 4; y < 16; y++)
                for (int x = x0; x < x0 + 16; x++)
                {
                    bool borda = y == 4 || y == 15 || x == x0 || x == x0 + 15;
                    if (borda || (x - x0) % 3 == 0 || (y - 4) % 3 == 0) t.P(x, y, borda ? MetalEscuro : Metal);
                }
            t.Ret(x0 + 12, 1, 6, 3, VermelhoMercado);   // alça
            t.Ret(x0 + 2, 17, 2, 3, K); t.Ret(x0 + 12, 17, 2, 3, K);
        }
        return t.Salvar(PastaMercado, "Carrinhos", Vector2.zero);
    }

    // Placa no pé: proibido entrar de moto
    public static Sprite PlacaProibidoMoto()
    {
        Tela t = new Tela(16, 28);
        t.Ret(7, 12, 2, 16, CinzaEscuro);
        for (int y = 0; y < 14; y++)
            for (int x = 1; x < 15; x++)
            {
                int dx = x - 7, dy = y - 7;
                int d = dx * dx + dy * dy;
                if (d > 44) continue;
                t.P(x, y, d > 30 ? VermelhoMercado : Branco);
            }
        // motinha
        t.Ret(4, 8, 2, 2, K); t.Ret(10, 8, 2, 2, K); t.Ret(5, 6, 6, 2, K); t.Ret(9, 4, 1, 2, K);
        for (int i = 0; i < 9; i++) t.P(3 + i, 3 + i, VermelhoMercado);   // risco de proibido
        return t.Salvar(PastaMercado, "PlacaProibidoMoto", Vector2.zero);
    }

    // Brilhinho que pula em cima das prateleiras das compras
    public static Sprite Brilho()
    {
        Tela t = new Tela(9, 9);
        Color32 amarelo = new Color32(255, 230, 70, 255);
        t.Ret(4, 0, 1, 9, amarelo);
        t.Ret(0, 4, 9, 1, amarelo);
        t.Ret(3, 3, 3, 3, amarelo);
        t.P(4, 4, Branco);
        t.P(2, 2, amarelo); t.P(6, 6, amarelo); t.P(2, 6, amarelo); t.P(6, 2, amarelo);
        return t.Salvar(PastaMercado, "BrilhoItem", new Vector2(0.5f, 0.5f));
    }
}
