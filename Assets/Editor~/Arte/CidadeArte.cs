using UnityEngine;

// Desenhos que entram no mapa do Nivel1 (8 pixels = 1 unidade, como o pátio da autoescola):
// a casa da família Driver (no estilo das casas do mapa), o Mercado Bom Preço, a pintura do estacionamento,
// árvores e a placa de rua sem saída. Usado por MontarHistoriaNivel1.
public static partial class CidadeArte
{
    public const string Pasta = "Assets/Imagens/Historia/Cidade";
    public const float PPU = 8f;

    // Terreno da casa do Jason (no fim da rua sem saída, do lado de cima) e área do mercado, em unidades do mundo
    public static readonly Rect TerrenoCasa = Rect.MinMaxRect(-27.2f, 6.9f, -13.2f, 25.9f);
    public static readonly Rect PredioMercado = Rect.MinMaxRect(130.7f, 73.0f, 173.7f, 80.9f);
    public static readonly Rect Estacionamento = Rect.MinMaxRect(130.7f, 45.9f, 173.7f, 73.0f);

    private static readonly Color32 K = PixelArt.Contorno;
    private static HistoriaArte.Tela t;

    // ======================================================================
    // Casa da família Driver
    // ======================================================================

    public static Sprite CasaJason()
    {
        int W = Mathf.RoundToInt(TerrenoCasa.width * PPU), H = Mathf.RoundToInt(TerrenoCasa.height * PPU);   // 112 x 152
        t = new HistoriaArte.Tela(W, H);

        // Grama cortada (listras do cortador)
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                Color32 c = CorGrama(x, y);
                if ((y / 8) % 2 == 0) c = Clarear(c, 6);
                t.P(x, y, c);
            }

        // Sombras da casa na grama
        Sombra(9, 13, 56, 74);
        Sombra(49, 53, 40, 58);

        // Entrada do carro (do portão da garagem até a calçada)
        for (int y = 108; y < H; y++)
            for (int x = 52; x < 80; x++)
            {
                int v = 198 + Hash(x, y) % 6 - 3;
                if (y == 124 || y == 140) v = 168;
                if (x == 52 || x == 79) v = 176;
                t.P(x, y, new Color32((byte)v, (byte)v, (byte)(v - 8), 255));
            }
        Mancha(64, 114, 3);
        Mancha(68, 131, 2);

        // Caminho de pedras da porta até o portãozinho
        int[,] pedras = { { 26, 97, 6, 4 }, { 22, 103, 7, 4 }, { 27, 109, 6, 4 }, { 21, 115, 7, 4 }, { 26, 121, 6, 4 },
                          { 20, 127, 7, 4 }, { 25, 133, 6, 4 }, { 20, 139, 7, 4 }, { 24, 145, 6, 3 } };
        for (int i = 0; i < pedras.GetLength(0); i++) Pedra(pedras[i, 0], pedras[i, 1], pedras[i, 2], pedras[i, 3]);

        // Canteiros de flores
        Canteiro(8, 92, 13, 5);
        Canteiro(81, 110, 4, 38);

        // Horta (igual às casas do mapa)
        Horta(86, 112, 9, 12); Horta(97, 112, 9, 12);
        Horta(86, 127, 9, 12); Horta(97, 127, 9, 12);

        // Varal com os moletons laranja do Jason
        Varal(99, 48, 92);

        // Casa: telhado principal e o da garagem
        Telhado(6, 10, 61, 83);
        Telhado(46, 50, 85, 99);
        Chamine(16, 22);
        Antena(54, 26);

        // Fachadas (a faixa de parede que aparece embaixo dos telhados)
        Parede(6, 84, 40, 8);
        Janela(10, 85); Janela(36, 85);
        Porta(25, 84);
        Ret(23, 92, 12, 4, new Color32(206, 200, 188, 255));      // degrau
        Ret(23, 95, 12, 1, new Color32(150, 144, 132, 255));
        Parede(46, 100, 40, 8);
        PortaoGaragem(52, 100, 28, 8);
        Ret(48, 101, 2, 2, new Color32(255, 230, 120, 255));        // luminária
        Ret(82, 101, 2, 2, new Color32(255, 230, 120, 255));

        // Árvores e arbustos
        Arvore(95, 24, 14f);
        Arvore(11, 124, 9f);
        Arbusto(40, 100, 4f);
        Arbusto(44, 145, 4f);
        Arbusto(104, 146, 4f);
        Arbusto(5, 99, 3.5f);

        // Caixa de correio
        Ret(37, 138, 2, 9, new Color32(100, 64, 40, 255));
        Ret(33, 134, 9, 5, new Color32(70, 110, 170, 255));
        Borda(33, 134, 9, 5, K);
        Ret(42, 132, 1, 4, new Color32(220, 40, 50, 255));
        Ret(42, 132, 2, 2, new Color32(220, 40, 50, 255));

        // Cerca de madeira em volta (com o portãozinho e a entrada do carro abertos)
        Cerca(W, H);

        return HistoriaArte.SalvarComPpu(t, Pasta, "CasaJason", PPU, new Vector2(0.5f, 0.5f));
    }

    private static void Telhado(int x0, int y0, int x1, int y1)
    {
        Color32 cima = new Color32(150, 64, 38, 255), baixo = new Color32(228, 126, 78, 255);
        Color32 esquerda = new Color32(198, 94, 54, 255), direita = new Color32(176, 80, 46, 255);
        Color32 cumeeira = new Color32(112, 44, 26, 255);

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int dl = x - x0, dr = x1 - x, dt = y - y0, db = y1 - y;
                int m = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(dt, db));
                Color32 c;
                bool vertical;
                if (m == dt) { c = cima; vertical = false; }
                else if (m == db) { c = baixo; vertical = false; }
                else if (m == dl) { c = esquerda; vertical = true; }
                else { c = direita; vertical = true; }

                // Telhas: fileiras e juntas desencontradas
                int fila = vertical ? dl < dr ? dl : dr : dt < db ? dt : db;
                int ao = vertical ? y : x;
                if (fila % 4 == 3) c = Escurecer(c, 0.8f);
                else if ((ao + (fila / 4) * 3) % 6 == 0) c = Escurecer(c, 0.88f);
                else if (fila % 4 == 0) c = Clarear(c, 14);

                // Espigões (onde duas águas se encontram) e a cumeeira
                int iguais = 0;
                if (dl == m) iguais++;
                if (dr == m) iguais++;
                if (dt == m) iguais++;
                if (db == m) iguais++;
                if (iguais >= 2) c = cumeeira;

                if (m == 0) c = K;
                t.P(x, y, c);
            }
    }

    private static void Chamine(int x, int y)
    {
        Ret(x + 2, y + 2, 7, 8, new Color32(0, 0, 0, 255), true);
        Ret(x, y, 7, 8, new Color32(170, 70, 50, 255));
        for (int yy = y + 2; yy < y + 8; yy += 3) Ret(x + 1, yy, 5, 1, new Color32(130, 50, 36, 255));
        Ret(x + 1, y + 1, 5, 2, new Color32(40, 30, 30, 255));
        Borda(x, y, 7, 8, K);
    }

    private static void Antena(int x, int y)
    {
        Color32 metal = new Color32(150, 156, 168, 255);
        Ret(x, y, 1, 10, metal);
        Ret(x - 3, y + 2, 7, 1, metal);
        Ret(x - 2, y + 5, 5, 1, metal);
        Ret(x + 1, y + 1, 1, 10, new Color32(90, 40, 30, 255));   // sombrinha
    }

    private static void Parede(int x, int y, int w, int h)
    {
        Ret(x, y, w, h, new Color32(238, 216, 172, 255));
        Ret(x, y, w, 1, new Color32(120, 80, 50, 255));             // sombra do beiral
        Ret(x, y + h - 2, w, 2, new Color32(204, 178, 136, 255));
        Borda(x, y - 1, w, h + 1, K);
    }

    private static void Janela(int x, int y)
    {
        Ret(x, y, 9, 5, Color32Branco);
        Ret(x + 1, y + 1, 3, 3, new Color32(120, 190, 230, 255));
        Ret(x + 5, y + 1, 3, 3, new Color32(120, 190, 230, 255));
        t.P(x + 1, y + 1, Color32Branco);
        Borda(x, y, 9, 5, K);
    }

    private static readonly Color32 Color32Branco = new Color32(250, 250, 250, 255);

    private static void Porta(int x, int y)
    {
        Ret(x, y, 8, 8, new Color32(130, 80, 46, 255));
        Ret(x + 1, y + 1, 6, 1, new Color32(160, 104, 64, 255));
        t.P(x + 6, y + 4, new Color32(250, 210, 80, 255));
        Borda(x, y, 8, 8, K);
    }

    private static void PortaoGaragem(int x, int y, int w, int h)
    {
        Ret(x, y, w, h, new Color32(196, 198, 204, 255));
        for (int yy = y + 1; yy < y + h; yy += 2) Ret(x + 1, yy, w - 2, 1, new Color32(150, 152, 160, 255));
        Ret(x + w / 2 - 3, y + h - 2, 6, 1, new Color32(70, 70, 80, 255));
        Borda(x, y, w, h, K);
    }

    private static void Pedra(int x, int y, int w, int h)
    {
        Ret(x, y, w, h, new Color32(184, 176, 158, 255));
        Ret(x + 1, y, w - 2, 1, new Color32(208, 200, 182, 255));
        t.P(x, y, CorGrama(x, y)); t.P(x + w - 1, y, CorGrama(x + w - 1, y));
        t.P(x, y + h - 1, CorGrama(x, y + h - 1)); t.P(x + w - 1, y + h - 1, CorGrama(x + w - 1, y + h - 1));
        Ret(x + 1, y + h - 1, w - 2, 1, new Color32(130, 122, 106, 255));
    }

    private static void Canteiro(int x, int y, int w, int h)
    {
        Ret(x, y, w, h, new Color32(110, 72, 46, 255));
        Color32[] flores = { new Color32(255, 110, 160, 255), new Color32(255, 220, 60, 255), new Color32(170, 110, 230, 255),
                             new Color32(255, 255, 255, 255), new Color32(240, 70, 60, 255) };
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
            {
                int hsh = Hash(xx, yy);
                if (hsh % 5 == 0) t.P(xx, yy, new Color32(60, 150, 64, 255));
                else if (hsh % 7 == 0) t.P(xx, yy, flores[hsh % flores.Length]);
            }
        Borda(x - 1, y - 1, w + 2, h + 2, new Color32(150, 104, 64, 255));
    }

    private static void Horta(int x, int y, int w, int h)
    {
        Ret(x, y, w, h, new Color32(104, 66, 42, 255));
        for (int yy = y + 2; yy < y + h - 1; yy += 3)
            for (int xx = x + 1; xx < x + w - 1; xx += 2)
            {
                t.P(xx, yy, new Color32(70, 170, 70, 255));
                t.P(xx, yy - 1, new Color32(100, 200, 90, 255));
                if (Hash(xx, yy) % 9 == 0) t.P(xx, yy, new Color32(230, 50, 50, 255));   // tomatinho
                else if (Hash(xx, yy) % 11 == 0) t.P(xx, yy, new Color32(255, 140, 30, 255));   // cenoura
            }
        Borda(x, y, w, h, new Color32(150, 104, 64, 255));
    }

    private static void Varal(int x, int y0, int y1)
    {
        Ret(x - 1, y0 - 1, 3, 3, new Color32(90, 60, 40, 255));
        Ret(x - 1, y1 - 1, 3, 3, new Color32(90, 60, 40, 255));
        Ret(x, y0, 1, y1 - y0, new Color32(236, 236, 226, 255));
        Roupa(x, y0 + 5, 7, 6, new Color32(255, 128, 26, 255));    // moletom laranja
        Roupa(x, y0 + 16, 7, 6, new Color32(255, 128, 26, 255));   // outro moletom laranja
        Roupa(x, y0 + 26, 5, 8, new Color32(60, 90, 170, 255));    // calça jeans
        Roupa(x, y0 + 37, 3, 3, Color32Branco);                    // meia
    }

    private static void Roupa(int cx, int y, int w, int h, Color32 c)
    {
        int x = cx - w / 2;
        Ret(x + 1, y + 1, w, h, new Color32(40, 90, 40, 255));   // sombra na grama
        Ret(x, y, w, h, c);
        Ret(x, y, w, 1, Clarear(c, 30));
        Borda(x, y, w, h, Escurecer(c, 0.6f));
    }

    private static void Cerca(int W, int H)
    {
        Color32 madeira = new Color32(110, 70, 42, 255), clara = new Color32(156, 108, 66, 255);
        for (int x = 0; x < W; x++)
        {
            bool portaozinho = x >= 17 && x <= 32;
            bool entrada = x >= 50 && x <= 81;
            Ret(x, 0, 1, 2, madeira);
            if (entrada) continue;
            Ret(x, H - 2, 1, 2, portaozinho ? clara : madeira);
        }
        for (int y = 0; y < H; y++) { Ret(0, y, 2, 1, madeira); Ret(W - 2, y, 2, 1, madeira); }
        for (int x = 0; x < W; x += 10) { Poste(x, 0); if (!(x >= 50 && x <= 81)) Poste(x, H - 3); }
        for (int y = 0; y < H; y += 10) { Poste(0, y); Poste(W - 3, y); }
        Poste(16, H - 3); Poste(33, H - 3); Poste(48, H - 3); Poste(82, H - 3);
        // Portãozinho de ripas
        for (int x = 18; x < 32; x += 3) Ret(x, H - 3, 1, 3, new Color32(90, 56, 34, 255));
    }

    private static void Poste(int x, int y)
    {
        Ret(x, y, 3, 3, new Color32(150, 104, 64, 255));
        t.P(x, y, new Color32(190, 140, 90, 255));
    }

    // ======================================================================
    // Mercado Bom Preço (prédio em cima do estacionamento)
    // ======================================================================

    public static Sprite Mercado()
    {
        int W = Mathf.RoundToInt(PredioMercado.width * PPU), H = Mathf.RoundToInt(PredioMercado.height * PPU);   // 344 x 63
        t = new HistoriaArte.Tela(W, H);
        int teto = H - 24;            // telhado até aqui
        int toldo = teto, fachada = teto + 8, calcada = teto + 14;

        // Telhado (laje)
        for (int y = 0; y < teto; y++)
            for (int x = 0; x < W; x++)
            {
                int v = 184 + Hash(x, y) % 5 - 2;
                if (x % 28 == 0 || y == 13 || y == 26) v = 168;
                t.P(x, y, new Color32((byte)(v - 4), (byte)v, (byte)(v + 8), 255));
            }
        Borda(0, 0, W, teto, new Color32(150, 154, 164, 255));
        Borda(1, 1, W - 2, teto - 2, new Color32(150, 154, 164, 255));
        Borda(0, 0, W, teto + 1, K);

        // Ar-condicionado e claraboias
        foreach (int x in new[] { 14, 44, 282, 312 }) ArCondicionado(x, 4);
        Claraboia(W / 2 - 128, 5); Claraboia(W / 2 + 108, 5);

        // Letreiro no telhado
        const string nome = "MERCADO BOM PREÇO";
        int escala = 2;
        int larguraTexto = AutoescolaArte.LarguraTexto(nome) * escala;
        int lx0 = W / 2 - larguraTexto / 2 - 26, lx1 = W / 2 + larguraTexto / 2 + 22;
        Ret(lx0 + 2, 5, lx1 - lx0, 25, new Color32(90, 92, 104, 255));     // sombra do letreiro
        Ret(lx0, 3, lx1 - lx0, 25, Color32Branco);
        Borda(lx0, 3, lx1 - lx0, 25, new Color32(220, 40, 50, 255));
        Borda(lx0 + 1, 4, lx1 - lx0 - 2, 23, new Color32(220, 40, 50, 255));
        Borda(lx0 - 1, 2, lx1 - lx0 + 2, 27, K);
        Carrinho(lx0 + 5, 9, new Color32(220, 40, 50, 255), 2);
        AutoescolaArte.EscreverEm(t, nome, W / 2 + 8, 10, escala, new Color32(220, 40, 50, 255), new Color32(255, 190, 190, 255));

        // Toldo listrado
        for (int y = toldo; y < fachada; y++)
            for (int x = 0; x < W; x++)
            {
                bool vermelho = (x / 8) % 2 == 0;
                int dentro = x % 8;
                if (y >= fachada - 2 && (dentro == 0 || dentro == 7) && y == fachada - 1) continue;   // bico do toldo
                t.P(x, y, vermelho ? new Color32(220, 40, 50, 255) : new Color32(245, 245, 245, 255));
            }
        Ret(0, toldo, W, 1, K);

        // Fachada com vitrines e as duas portas
        Ret(0, fachada, W, calcada - fachada, new Color32(234, 228, 212, 255));
        for (int x = 4; x < W - 4; x += 20) Vitrine(x, fachada + 1, 16, 4);
        int portaA = Mathf.RoundToInt((136f - PredioMercado.xMin) * PPU);   // em frente à vaga de chegada
        int portaB = Mathf.RoundToInt((166f - PredioMercado.xMin) * PPU);
        PortaVidro(portaA - 12, fachada, 24, calcada - fachada);
        PortaVidro(portaB - 12, fachada, 24, calcada - fachada);
        Ret(0, calcada - 1, W, 1, new Color32(150, 144, 130, 255));

        // Calçada com carrinhos, banco, lixeiras e vasos
        for (int y = calcada; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int v = 206 + Hash(x, y) % 4;
                if (x % 8 == 0 || (y - calcada) % 8 == 0) v = 190;
                t.P(x, y, new Color32((byte)v, (byte)v, (byte)(v - 6), 255));
            }
        for (int i = 0; i < 6; i++) Carrinho(portaA + 26 + i * 6, calcada + 2, new Color32(220, 40, 50, 255), 1);
        Ret(portaB - 44, calcada + 3, 16, 3, new Color32(150, 100, 60, 255));    // banco
        Borda(portaB - 44, calcada + 3, 16, 3, K);
        Lixeira(portaB - 22, calcada + 4, new Color32(60, 150, 70, 255));
        Lixeira(portaB - 17, calcada + 4, new Color32(60, 110, 200, 255));
        Vaso(6, calcada + 4); Vaso(W - 7, calcada + 4); Vaso(portaA + 18, calcada + 4);
        Ret(0, H - 1, W, 1, new Color32(150, 150, 150, 255));    // meio-fio

        return HistoriaArte.SalvarComPpu(t, Pasta, "PredioMercado", PPU, new Vector2(0.5f, 0.5f));
    }

    private static void ArCondicionado(int x, int y)
    {
        Ret(x + 2, y + 2, 16, 12, new Color32(120, 124, 134, 255));
        Ret(x, y, 16, 12, new Color32(206, 210, 216, 255));
        Borda(x, y, 16, 12, K);
        for (int yy = y + 2; yy < y + 11; yy += 2) Ret(x + 2, yy, 5, 1, new Color32(150, 154, 164, 255));
        int cx = x + 11, cy = y + 6;
        for (int dy = -3; dy <= 3; dy++)
            for (int dx = -3; dx <= 3; dx++)
                if (dx * dx + dy * dy <= 10) t.P(cx + dx, cy + dy, (dx + dy) % 2 == 0 ? new Color32(60, 64, 74, 255) : new Color32(100, 104, 114, 255));
    }

    private static void Claraboia(int x, int y)
    {
        Ret(x, y, 20, 10, new Color32(140, 196, 236, 255));
        Ret(x + 2, y + 2, 5, 2, Color32Branco);
        Ret(x + 9, y, 1, 10, new Color32(220, 226, 236, 255));
        Borda(x, y, 20, 10, K);
    }

    private static void Vitrine(int x, int y, int w, int h)
    {
        Ret(x, y, w, h, new Color32(150, 206, 236, 255));
        Ret(x + 2, y + 1, 5, 2, new Color32(255, 220, 60, 255));    // cartaz de oferta
        t.P(x + 4, y + 1, new Color32(220, 40, 50, 255));
        Borda(x, y, w, h, new Color32(70, 74, 86, 255));
    }

    private static void PortaVidro(int x, int y, int w, int h)
    {
        Ret(x, y, w, h, new Color32(110, 170, 210, 255));
        Ret(x + w / 2, y, 1, h, new Color32(70, 74, 86, 255));
        Ret(x + 2, y + 1, 3, 1, Color32Branco);
        Borda(x, y, w, h, new Color32(70, 74, 86, 255));
        Ret(x + 4, y - 7, w - 8, 3, new Color32(40, 160, 80, 255));   // plaquinha ENTRADA
    }

    private static void Carrinho(int x, int y, Color32 alca, int escala)
    {
        Color32 metal = new Color32(150, 156, 170, 255), escuro = new Color32(80, 86, 100, 255);
        for (int yy = 0; yy < 4; yy++)
            for (int xx = 0; xx < 5; xx++)
            {
                bool borda = yy == 0 || yy == 3 || xx == 0 || xx == 4;
                bool grade = (xx + yy) % 2 == 0;
                if (borda || grade) Ret(x + xx * escala, y + yy * escala, escala, escala, borda ? escuro : metal);
            }
        Ret(x - escala, y, escala, escala * 2, alca);
        Ret(x + escala, y + 4 * escala, escala, escala, K);
        Ret(x + 3 * escala, y + 4 * escala, escala, escala, K);
    }

    private static void Lixeira(int x, int y, Color32 c)
    {
        Ret(x, y, 4, 4, c);
        Ret(x + 1, y + 1, 2, 2, Escurecer(c, 0.6f));
        Borda(x, y, 4, 4, K);
    }

    private static void Vaso(int cx, int y)
    {
        Ret(cx - 2, y + 2, 5, 3, new Color32(190, 100, 60, 255));
        Circulo(cx, y + 1, 2.6f, new Color32(60, 150, 64, 255));
        t.P(cx - 1, y, new Color32(255, 110, 160, 255));
    }

    // ======================================================================
    // Pintura do estacionamento do mercado (por cima do asfalto)
    // ======================================================================

    // Vagas de carro deitado (como a vaga de chegada do Nivel1): linhas de y em y, de xIni até xFim
    private static readonly float[] LinhasVagasEsquerda = { 49.5f, 53.5f, 57.5f, 61.5f, 65.5f };
    private static readonly float[] LinhasVagasDireita = { 49.5f, 53.5f, 57.5f, 61.3f };

    public static Sprite PinturaEstacionamento(Rect vagaChegada)
    {
        Rect r = Estacionamento;
        int W = Mathf.RoundToInt(r.width * PPU), H = Mathf.RoundToInt(r.height * PPU);
        t = new HistoriaArte.Tela(W, H);
        Color32 branco = new Color32(236, 236, 236, 235), amarelo = new Color32(242, 200, 40, 240);

        foreach (float y in LinhasVagasEsquerda)
        {
            bool daChegada = Mathf.Abs(y - vagaChegada.yMin) < 0.6f || Mathf.Abs(y - vagaChegada.yMax) < 0.6f;
            LinhaH(r, 131.2f, 140.6f, y, daChegada ? amarelo : branco);
        }
        foreach (float y in LinhasVagasDireita) LinhaH(r, 149.8f, 158.4f, y, branco);

        // A vaga de chegada ganha um "M" amarelo do mercado no fundo
        int mx = Mathf.RoundToInt((vagaChegada.xMin + 0.9f - r.xMin) * PPU);
        int my = Mathf.RoundToInt((r.yMax - vagaChegada.center.y) * PPU) - 5;
        AutoescolaArte.EscreverEm(t, "M", mx + 4, my, 2, amarelo, new Color32(0, 0, 0, 0));

        // Faixa de pedestres até a porta do mercado
        for (float y = 66.4f; y < 72.6f; y += 1f) Faixa(r, 131.8f, 140.0f, y, y + 0.5f, branco);

        // Setas do caminho: sobe pelo meio e entra pela direita
        Seta(r, 145.5f, 51.5f, 0, 1, branco);
        Seta(r, 145.5f, 60.5f, 0, 1, branco);
        Seta(r, 167f, 67.5f, -1, 0, branco);
        Seta(r, 153f, 67.5f, -1, 0, branco);

        return HistoriaArte.SalvarComPpu(t, Pasta, "PinturaEstacionamento", PPU, new Vector2(0.5f, 0.5f));
    }

    private static void LinhaH(Rect r, float x0, float x1, float y, Color32 c) => Faixa(r, x0, x1, y - 0.12f, y + 0.12f, c);

    private static void Faixa(Rect r, float x0, float x1, float y0, float y1, Color32 c)
    {
        int px0 = Mathf.RoundToInt((x0 - r.xMin) * PPU), px1 = Mathf.RoundToInt((x1 - r.xMin) * PPU);
        int py0 = Mathf.RoundToInt((r.yMax - y1) * PPU), py1 = Mathf.RoundToInt((r.yMax - y0) * PPU);
        for (int y = py0; y < Mathf.Max(py1, py0 + 1); y++)
            for (int x = px0; x < px1; x++) t.P(x, y, c);
    }

    private static void Seta(Rect r, float cx, float cy, int dx, int dy, Color32 c)
    {
        int W = t.L, H = t.A;
        for (int py = 0; py < H; py++)
            for (int px = 0; px < W; px++)
            {
                float ux = r.xMin + (px + 0.5f) / PPU - cx, uy = r.yMax - (py + 0.5f) / PPU - cy;
                if (Mathf.Abs(ux) > 3f || Mathf.Abs(uy) > 3f) continue;
                float ao = ux * dx + uy * dy, lado = Mathf.Abs(-ux * dy + uy * dx);
                bool haste = ao >= -2.2f && ao <= 0.3f && lado <= 0.35f;
                bool ponta = ao > 0.3f && ao <= 2.0f && lado <= (2.0f - ao) * 0.7f;
                if (haste || ponta) t.P(px, py, c);
            }
    }

    // ======================================================================
    // Árvore e placa
    // ======================================================================

    public static Sprite Arvore(int variacao)
    {
        const int n = 48;
        t = new HistoriaArte.Tela(n, n);
        float r = 18f - variacao * 2f;
        Circulo(n / 2f + 3, n / 2f + 3, r, new Color32(0, 0, 0, 70));
        Copa(n / 2f, n / 2f, r);
        return HistoriaArte.SalvarComPpu(t, Pasta, "Arvore" + variacao, PPU, new Vector2(0.5f, 0.5f));
    }

    // Placa azul de rua sem saída (barra branca com a ponta vermelha)
    public static Sprite PlacaSemSaida()
    {
        t = new HistoriaArte.Tela(14, 22);
        Ret(6, 9, 2, 13, new Color32(130, 134, 146, 255));
        Ret(1, 0, 12, 11, new Color32(40, 90, 200, 255));
        Borda(1, 0, 12, 11, Color32Branco);
        Borda(0, -1, 14, 13, K);
        Ret(6, 4, 2, 6, Color32Branco);
        Ret(4, 2, 6, 2, new Color32(220, 40, 50, 255));
        return HistoriaArte.SalvarComPpu(t, Pasta, "PlacaSemSaida", PPU, new Vector2(0.5f, 0.05f));
    }

    // ======================================================================
    // Ajudantes (y conta de cima para baixo)
    // ======================================================================

    private static void Arvore(int cx, int cy, float r)
    {
        Circulo(cx + 3, cy + 3, r, new Color32(0, 0, 0, 70));
        Copa(cx, cy, r);
    }

    private static void Copa(float cx, float cy, float r)
    {
        Circulo(cx, cy, r, new Color32(24, 70, 34, 255));
        Circulo(cx, cy, r - 1f, new Color32(34, 96, 46, 255));
        Circulo(cx - r * 0.2f, cy - r * 0.2f, r * 0.72f, new Color32(48, 128, 58, 255));
        Circulo(cx - r * 0.38f, cy - r * 0.36f, r * 0.36f, new Color32(84, 170, 80, 255));
        Circulo(cx + r * 0.35f, cy + r * 0.1f, r * 0.26f, new Color32(60, 142, 64, 255));
        Circulo(cx + r * 0.05f, cy + r * 0.45f, r * 0.2f, new Color32(42, 112, 52, 255));
    }

    private static void Arbusto(int cx, int cy, float r)
    {
        Circulo(cx + 1.5f, cy + 1.5f, r, new Color32(0, 0, 0, 70));
        Circulo(cx, cy, r, new Color32(30, 90, 40, 255));
        Circulo(cx - r * 0.3f, cy - r * 0.3f, r * 0.55f, new Color32(70, 156, 70, 255));
    }

    private static void Sombra(int x, int y, int w, int h) => Ret(x, y, w, h, new Color32(0, 0, 0, 60), true);

    private static void Mancha(int cx, int cy, int r)
    {
        Circulo(cx, cy, r + 0.5f, new Color32(30, 28, 36, 60));
        Circulo(cx - 0.5f, cy - 0.5f, r * 0.4f, new Color32(110, 80, 150, 60));
    }

    private static void Ret(int x, int y, int w, int h, Color32 c, bool misturar = false)
    {
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                if (misturar || c.a < 255) Misturar(xx, yy, c);
                else t.P(xx, yy, c);
    }

    private static void Borda(int x, int y, int w, int h, Color32 c)
    {
        Ret(x, y, w, 1, c);
        Ret(x, y + h - 1, w, 1, c);
        Ret(x, y, 1, h, c);
        Ret(x + w - 1, y, 1, h, c);
    }

    private static void Circulo(float cx, float cy, float r, Color32 c)
    {
        for (int y = Mathf.FloorToInt(cy - r) - 1; y <= Mathf.CeilToInt(cy + r) + 1; y++)
            for (int x = Mathf.FloorToInt(cx - r) - 1; x <= Mathf.CeilToInt(cx + r) + 1; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                if (dx * dx + dy * dy <= r * r)
                {
                    if (c.a < 255) Misturar(x, y, c);
                    else t.P(x, y, c);
                }
            }
    }

    // Mistura a cor por cima (sombra transparente escurece o que já está desenhado)
    private static void Misturar(int x, int y, Color32 c)
    {
        if (x < 0 || y < 0 || x >= t.L || y >= t.A) return;
        int i = (t.A - 1 - y) * t.L + x;
        Color32 b = t.p[i];
        if (b.a == 0) { t.p[i] = c; return; }
        float k = c.a / 255f;
        t.p[i] = new Color32((byte)(b.r + (c.r - b.r) * k), (byte)(b.g + (c.g - b.g) * k), (byte)(b.b + (c.b - b.b) * k), b.a);
    }

    private static Color32 CorGrama(int x, int y)
    {
        int h = Hash(x, y);
        int mancha = Hash(x / 6, y / 6) % 3;
        int g = 140 + h % 12 + mancha * 4;
        Color32 c = new Color32((byte)(56 + h % 10), (byte)g, (byte)(54 + h % 8), 255);
        if (h % 97 == 0) c = new Color32(250, 240, 120, 255);   // florzinha
        else if (h % 89 == 0) c = new Color32(90, 176, 80, 255);
        return c;
    }

    private static Color32 Escurecer(Color32 c, float f) => new Color32((byte)(c.r * f), (byte)(c.g * f), (byte)(c.b * f), c.a);

    private static Color32 Clarear(Color32 c, int v) =>
        new Color32((byte)Mathf.Min(255, c.r + v), (byte)Mathf.Min(255, c.g + v), (byte)Mathf.Min(255, c.b + v), c.a);

    private static int Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h ^ (h >> 16)) & 0x7fffffff;
        }
    }
}
