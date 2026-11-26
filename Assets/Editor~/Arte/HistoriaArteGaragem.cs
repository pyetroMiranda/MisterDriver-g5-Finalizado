using UnityEngine;

// Desenhos da garagem da casa do Jason (chão, parede, móveis, portas e o portão) e da tela da garagem.
// Continuação de HistoriaArte (mesma paleta e mesmos ajudantes).
public static partial class HistoriaArte
{
    private static readonly Color32 AmareloFaixa = new Color32(242, 200, 40, 255);
    private static readonly Color32 PneuCor = new Color32(40, 40, 46, 255);
    private static readonly Color32 PneuClaro = new Color32(74, 74, 84, 255);

    // ======================================================================
    // Chão e paredes
    // ======================================================================

    private static void Concreto(Tela t, int px, int py, int x, int y)
    {
        for (int yy = 0; yy < Px; yy++)
            for (int xx = 0; xx < Px; xx++)
            {
                int h = Hash(px + xx, py + yy);
                int v = 168 + h % 7 - 3;
                if (h % 61 == 0) v = 146;          // pedrinha escura
                else if (h % 53 == 0) v = 186;     // pedrinha clara
                t.P(px + xx, py + yy, new Color32((byte)v, (byte)v, (byte)(v - 4), 255));
            }

        // Juntas de dilatação a cada 3 quadradinhos
        Color32 junta = new Color32(140, 140, 136, 255);
        if ((x - InicioGaragem) % 3 == 0) t.Ret(px, py, 1, Px, junta);
        if (y % 3 == 2) t.Ret(px, py + Px - 1, Px, 1, junta);
    }

    // Passagem da sala para a garagem (a porta é um desenho separado)
    private static void Soleira(Tela t, int px, int py)
    {
        t.Ret(px, py, Px, Px, Madeira);
        for (int x = 3; x < Px; x += 4) t.Ret(px + x, py + 2, 1, Px - 4, MadeiraEscura);
        t.Ret(px, py, Px, 2, MovelEscuro);             // batente de cima
        t.Ret(px, py + Px - 2, Px, 2, MovelEscuro);    // batente de baixo
    }

    // Debaixo do portão: a luz da rua
    private static void LuzDoPortao(Tela t, int px, int py, int x)
    {
        t.Ret(px, py, Px, Px, new Color32(255, 236, 170, 255));
        t.Ret(px, py, Px, 3, new Color32(230, 200, 120, 255));
        if (x == Portao.xMin) t.Ret(px, py, 2, Px, K);
        if (x == Portao.xMax - 1) t.Ret(px + Px - 2, py, 2, Px, K);
    }

    private static void Tijolos(Tela t, int px, int py)
    {
        Color32 tijolo = new Color32(200, 192, 182, 255), tijoloEscuro = new Color32(182, 172, 162, 255);
        Color32 massa = new Color32(150, 144, 138, 255);
        t.Ret(px, py, Px, Px, massa);
        for (int linha = 0; linha < 3; linha++)
        {
            int y = py + 1 + linha * 4;
            int desloca = linha % 2 == 0 ? 0 : 4;
            for (int x = -8; x < Px; x += 8)
            {
                int x0 = Mathf.Max(0, x + desloca), x1 = Mathf.Min(Px, x + desloca + 7);
                if (x1 <= x0) continue;
                t.Ret(px + x0, y, x1 - x0, 3, (Hash(px + x, y) % 3 == 0) ? tijoloEscuro : tijolo);
            }
        }
        t.Ret(px, py, Px, 1, K);
        t.Ret(px, py + Px - 4, Px, 4, new Color32(110, 110, 118, 255));   // rodapé de cimento
        t.Ret(px, py + Px - 4, Px, 1, new Color32(84, 84, 92, 255));
    }

    private static void JanelaGaragem(Tela t, int px, int py)
    {
        Color32 metal = new Color32(150, 156, 168, 255), vidro = new Color32(140, 206, 250, 255);
        t.Ret(px + 2, py + 2, 12, 9, metal);
        t.Ret(px + 3, py + 3, 4, 3, vidro); t.Ret(px + 9, py + 3, 4, 3, vidro);
        t.Ret(px + 3, py + 7, 4, 3, vidro); t.Ret(px + 9, py + 7, 4, 3, vidro);
        t.P(px + 4, py + 4, Branco); t.P(px + 10, py + 4, Branco);
        t.Borda(px + 2, py + 2, 12, 9, K);
        t.Ret(px + 1, py + 11, 14, 1, CinzaEscuro);
    }

    private static void DetalhesGaragem(Tela t)
    {
        int y1 = Px;   // linha da parede de cima

        // Quadro de ferramentas atrás da bancada (com o contorno vazio da chave de fenda sumida)
        int qx = InicioGaragem * Px + 2;
        Color32 eucatex = new Color32(204, 160, 110, 255), furo = new Color32(170, 128, 84, 255);
        t.Ret(qx, y1 + 1, 28, 10, eucatex);
        for (int y = y1 + 2; y < y1 + 11; y += 2)
            for (int x = qx + 1; x < qx + 28; x += 3) t.P(x, y, furo);
        t.Borda(qx, y1 + 1, 28, 10, K);
        // martelo
        t.Ret(qx + 3, y1 + 4, 1, 6, MovelEscuro); t.Ret(qx + 2, y1 + 3, 3, 2, CinzaEscuro);
        // chave inglesa
        t.Ret(qx + 8, y1 + 3, 1, 7, Cinza); t.Ret(qx + 7, y1 + 3, 3, 1, Cinza); t.Ret(qx + 7, y1 + 9, 3, 1, Cinza);
        // serrote
        t.Ret(qx + 12, y1 + 4, 6, 3, Cinza); t.Ret(qx + 17, y1 + 4, 2, 5, MovelEscuro);
        // contorno vazio da chave de fenda
        Color32 giz = new Color32(250, 250, 250, 255);
        t.Ret(qx + 22, y1 + 3, 1, 7, giz); t.Ret(qx + 24, y1 + 3, 1, 7, giz);
        t.Ret(qx + 22, y1 + 3, 3, 1, giz); t.Ret(qx + 22, y1 + 9, 3, 1, giz);

        // Janela
        JanelaGaragem(t, 24 * Px, y1);

        // Placa "VAGA" do Kicks (com um coraçãozinho)
        int vx = VagaCarro.xMin * Px + 3;
        t.Ret(vx, y1 + 2, 26, 9, Branco);
        t.Ret(vx, y1 + 2, 26, 2, new Color32(220, 40, 50, 255));
        t.Borda(vx, y1 + 2, 26, 9, K);
        PixelArt.DesenharTexto(t.p, t.L, t.A, "VAGA", vx + 2, y1 + 5, K);
        Color32 coracao = new Color32(230, 40, 80, 255);
        t.P(vx + 22, y1 + 5, coracao); t.P(vx + 24, y1 + 5, coracao);
        t.Ret(vx + 22, y1 + 6, 3, 1, coracao); t.P(vx + 23, y1 + 7, coracao);

        // Calendário de 1998
        int cx = 27 * Px + 3;
        t.Ret(cx, y1 + 1, 10, 11, Branco);
        t.Ret(cx, y1 + 1, 10, 3, new Color32(220, 40, 50, 255));
        t.Ret(cx + 2, y1 + 5, 6, 3, new Color32(230, 50, 60, 255));     // fotinho do Kicks
        t.Ret(cx + 3, y1 + 4, 4, 1, new Color32(140, 200, 240, 255));
        for (int y = y1 + 9; y < y1 + 12; y += 2)
            for (int x = cx + 1; x < cx + 9; x += 2) t.P(x, y, Cinza);
        t.Borda(cx, y1 + 1, 10, 11, K);

        // Faixa amarela tracejada em volta da vaga do carro
        int fx = VagaCarro.xMin * Px - 3, fy = VagaCarro.yMin * Px - 3;
        int fw = VagaCarro.width * Px + 6, fh = VagaCarro.height * Px + 6;
        for (int i = 0; i < fw; i++)
            if (i % 9 < 6) { t.Ret(fx + i, fy, 1, 2, AmareloFaixa); t.Ret(fx + i, fy + fh - 2, 1, 2, AmareloFaixa); }
        for (int i = 0; i < fh; i++)
            if (i % 9 < 6) { t.Ret(fx, fy + i, 2, 1, AmareloFaixa); t.Ret(fx + fw - 2, fy + i, 2, 1, AmareloFaixa); }

        // Marcas de pneu da vaga até o portão
        for (int y = (VagaCarro.yMax) * Px; y < Portao.yMin * Px - 4; y++)
        {
            t.Escurecer(VagaCarro.xMin * Px + 4, y, 3, 1, 0.86f);
            t.Escurecer(VagaCarro.xMax * Px - 7, y, 3, 1, 0.86f);
        }

        // Faixa zebrada amarela e preta antes do portão
        int zy = Portao.yMin * Px - 4;
        for (int x = Portao.xMin * Px; x < Portao.xMax * Px; x++)
            for (int y = zy; y < zy + 4; y++)
                t.P(x, y, ((x + y) / 3) % 2 == 0 ? AmareloFaixa : new Color32(30, 30, 34, 255));

        // Mancha de óleo e ralo
        Mancha(t, 24 * Px + 6, 9 * Px + 4, 5);
        Mancha(t, 29 * Px + 3, 7 * Px + 8, 3);
        int rx = 23 * Px + 3, ry = 9 * Px + 5;
        t.Ret(rx, ry, 9, 6, new Color32(90, 92, 100, 255));
        for (int x = rx + 1; x < rx + 9; x += 2) t.Ret(x, ry + 1, 1, 4, new Color32(40, 40, 48, 255));
        t.Borda(rx, ry, 9, 6, K);
    }

    private static void Mancha(Tela t, int cx, int cy, int r)
    {
        for (int y = -r; y <= r; y++)
            for (int x = -r - 2; x <= r + 2; x++)
            {
                float d = (x * x) / (float)((r + 2) * (r + 2)) + (y * y) / (float)(r * r);
                if (d <= 1f) t.Escurecer(cx + x, cy + y, 1, 1, d < 0.4f ? 0.62f : 0.75f);
            }
        t.P(cx - 1, cy - 1, new Color32(150, 120, 190, 255));   // brilho de óleo
    }

    // ======================================================================
    // Móveis da garagem (ponto de apoio embaixo à esquerda, como os outros)
    // ======================================================================

    public static Sprite Bancada()
    {
        Tela t = new Tela(32, 28);
        // Tampo com as coisas em cima
        t.Caixa(0, 9, 32, 6, MovelClaro);
        t.Ret(1, 10, 30, 1, Madeira);
        t.Caixa(2, 4, 7, 6, CinzaEscuro);                      // morsa
        t.Ret(3, 5, 5, 1, Cinza);
        t.Ret(4, 2, 3, 2, CinzaEscuro);
        t.Ret(11, 10, 9, 2, Movel);                             // martelo deitado
        t.Caixa(18, 7, 4, 5, Cinza);
        t.Caixa(24, 3, 5, 7, new Color32(170, 220, 240, 255));  // pote de parafusos
        t.Ret(25, 6, 3, 3, CinzaEscuro);
        t.Ret(24, 3, 5, 1, new Color32(220, 60, 60, 255));
        // Gaveteiro
        t.Caixa(0, 14, 32, 14, Movel);
        t.Borda(2, 16, 13, 5, MovelEscuro);
        t.Borda(17, 16, 13, 5, MovelEscuro);
        t.Borda(2, 22, 13, 4, MovelEscuro);
        t.Borda(17, 22, 13, 4, MovelEscuro);
        t.Ret(7, 18, 3, 1, new Color32(250, 210, 80, 255));
        t.Ret(22, 18, 3, 1, new Color32(250, 210, 80, 255));
        t.Ret(7, 23, 3, 1, new Color32(250, 210, 80, 255));
        t.Ret(22, 23, 3, 1, new Color32(250, 210, 80, 255));
        return t.Salvar(PastaCasa, "Bancada", Vector2.zero);
    }

    public static Sprite PrateleiraTintas()
    {
        Tela t = new Tela(32, 34);
        Color32 metal = new Color32(130, 136, 150, 255);
        t.Ret(0, 0, 2, 34, metal); t.Ret(30, 0, 2, 34, metal);
        t.Borda(0, 0, 2, 34, K); t.Borda(30, 0, 2, 34, K);
        Color32[][] cores =
        {
            new[] { new Color32(220, 40, 50, 255), new Color32(255, 128, 26, 255), new Color32(255, 200, 30, 255), new Color32(52, 158, 90, 255) },
            new[] { new Color32(45, 200, 220, 255), new Color32(46, 97, 217, 255), new Color32(130, 60, 190, 255), new Color32(237, 77, 148, 255) },
            new[] { new Color32(245, 245, 248, 255), new Color32(192, 196, 204, 255), new Color32(60, 62, 70, 255), new Color32(120, 78, 46, 255) },
        };
        int[] prateleiras = { 11, 22, 33 };
        for (int p = 0; p < 3; p++)
        {
            int ys = prateleiras[p];
            for (int i = 0; i < 4; i++)
            {
                int x = 3 + i * 7;
                Color32 c = cores[p][i];
                t.Caixa(x, ys - 8, 6, 8, Cinza);                   // lata
                t.Ret(x + 1, ys - 6, 4, 4, c);                     // rótulo com a cor
                t.Ret(x + 1, ys - 7, 4, 1, CinzaEscuro);           // tampa
                if ((p + i) % 3 == 0) t.Ret(x + 2, ys - 1, 2, 2, c);   // pingo de tinta
            }
            t.Ret(1, ys - 1, 30, 2, metal);
            t.Ret(1, ys - 1, 30, 1, Cinza);
        }
        return t.Salvar(PastaCasa, "PrateleiraTintas", Vector2.zero);
    }

    public static Sprite PilhaPneus()
    {
        Tela t = new Tela(16, 28);
        for (int i = 0; i < 3; i++)
        {
            int y = 20 - i * 7;
            t.Caixa(1, y, 14, 8, PneuCor);
            t.Ret(2, y + 1, 12, 1, PneuClaro);
            for (int x = 3; x < 14; x += 3) t.Ret(x, y + 3, 1, 4, new Color32(26, 26, 30, 255));
        }
        t.Caixa(1, 1, 14, 7, PneuCor);   // pneu de cima, visto de cima
        t.Ret(2, 2, 12, 1, PneuClaro);
        t.Ret(5, 3, 6, 3, new Color32(16, 16, 20, 255));
        t.Ret(6, 4, 4, 1, new Color32(120, 120, 130, 255));
        return t.Salvar(PastaCasa, "PilhaPneus", Vector2.zero);
    }

    public static Sprite Tambor()
    {
        Tela t = new Tela(16, 24);
        Color32 azul = new Color32(40, 90, 180, 255), azulEscuro = new Color32(28, 64, 140, 255);
        t.Caixa(1, 3, 14, 21, azul);
        t.Ret(2, 3, 12, 3, new Color32(80, 130, 220, 255));
        t.Ret(4, 3, 3, 1, CinzaEscuro);
        t.Ret(2, 9, 12, 1, azulEscuro);
        t.Ret(2, 18, 12, 1, azulEscuro);
        t.Ret(4, 11, 8, 5, Branco);
        t.Ret(7, 12, 2, 1, K); t.Ret(6, 13, 4, 2, K);   // gota de óleo
        t.Ret(2, 4, 1, 18, new Color32(90, 140, 230, 255));
        return t.Salvar(PastaCasa, "Tambor", Vector2.zero);
    }

    public static Sprite Bicicleta()
    {
        Tela t = new Tela(16, 32);
        Color32 quadro = new Color32(210, 40, 50, 255);
        t.Ret(7, 1, 2, 10, K);  t.Ret(7, 2, 2, 8, PneuClaro);      // roda da frente (vista de cima)
        t.Ret(7, 21, 2, 10, K); t.Ret(7, 22, 2, 8, PneuClaro);     // roda de trás
        t.Ret(7, 9, 2, 14, quadro);
        t.Ret(6, 9, 1, 14, K); t.Ret(9, 9, 1, 14, K);
        t.Ret(2, 6, 12, 2, CinzaEscuro);                           // guidão
        t.Ret(1, 6, 2, 2, K); t.Ret(13, 6, 2, 2, K);
        t.Ret(6, 18, 4, 4, new Color32(60, 40, 30, 255));          // banco
        t.Ret(4, 15, 2, 2, K); t.Ret(10, 15, 2, 2, K);             // pedais
        t.P(8, 12, new Color32(250, 210, 210, 255));                // o arranhão...
        t.P(7, 13, new Color32(250, 210, 210, 255));
        t.P(8, 14, new Color32(250, 210, 210, 255));
        return t.Salvar(PastaCasa, "Bicicleta", Vector2.zero);
    }

    public static Sprite CaixaFerramentas()
    {
        Tela t = new Tela(16, 14);
        Color32 vermelho = new Color32(210, 40, 40, 255);
        t.Ret(5, 1, 6, 1, K); t.Ret(5, 1, 1, 3, K); t.Ret(10, 1, 1, 3, K);   // alça
        t.Caixa(1, 4, 14, 10, vermelho);
        t.Ret(2, 5, 12, 1, new Color32(240, 90, 90, 255));
        t.Ret(2, 8, 12, 1, new Color32(150, 24, 24, 255));
        t.Ret(7, 8, 2, 2, Cinza);
        t.Ret(3, 10, 4, 2, Branco);   // adesivo "NÃO MEXA"
        return t.Salvar(PastaCasa, "CaixaFerramentas", Vector2.zero);
    }

    // Capa por cima do Kicks (capítulo 1), com o ponto de apoio no meio
    public static Sprite Capa()
    {
        Tela t = new Tela(34, 64);
        Color32 pano = new Color32(96, 116, 156, 255), dobra = new Color32(72, 88, 124, 255), luz = new Color32(130, 150, 190, 255);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 34; x++)
            {
                if (!Arredondado(x, y, 34, 64, 8)) continue;
                bool borda = !Arredondado(x - 1, y, 34, 64, 8) || !Arredondado(x + 1, y, 34, 64, 8) ||
                             !Arredondado(x, y - 1, 34, 64, 8) || !Arredondado(x, y + 1, 34, 64, 8);
                Color32 c = borda ? K : pano;
                if (!borda && (x + y * 2) % 23 < 2) c = dobra;
                if (!borda && x < 6) c = luz;
                t.P(x, y, c);
            }
        Color32 corda = new Color32(220, 196, 140, 255);
        t.Ret(1, 18, 32, 2, corda);
        t.Ret(1, 46, 32, 2, corda);
        t.Ret(14, 30, 6, 4, Branco);   // etiqueta
        t.Borda(14, 30, 6, 4, K);
        return t.Salvar(PastaCasa, "CapaKicks", new Vector2(0.5f, 0.5f));
    }

    public static Sprite SombraCarro()
    {
        Tela t = new Tela(34, 64);
        Color32 c = new Color32(20, 10, 30, 70);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 34; x++)
                if (Arredondado(x, y, 34, 64, 10)) t.P(x, y, c);
        return t.Salvar(PastaCasa, "SombraCarro", new Vector2(0.5f, 0.5f));
    }

    private static bool Arredondado(int x, int y, int w, int h, int r)
    {
        if (x < 0 || y < 0 || x >= w || y >= h) return false;
        int cx = Mathf.Clamp(x, r, w - 1 - r), cy = Mathf.Clamp(y, r, h - 1 - r);
        int dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= r * r;
    }

    // ---------- Portas ----------

    // Porta da sala para a garagem, fechada (na parede vertical) e aberta (encostada para dentro da garagem)
    public static Sprite PortaGaragemFechada()
    {
        Tela t = new Tela(16, 16);
        t.Caixa(5, 0, 6, 16, Movel);
        t.Ret(6, 1, 1, 14, MovelClaro);
        t.Ret(4, 7, 1, 2, new Color32(250, 210, 80, 255));   // maçaneta do lado da sala
        return t.Salvar(PastaCasa, "PortaGaragemFechada", new Vector2(0.5f, 0.5f));
    }

    public static Sprite PortaGaragemAberta()
    {
        Tela t = new Tela(32, 16);
        t.Caixa(8, 0, 16, 5, Movel);
        t.Ret(9, 1, 14, 1, MovelClaro);
        t.Ret(21, 5, 2, 1, new Color32(250, 210, 80, 255));
        return t.Salvar(PastaCasa, "PortaGaragemAberta", new Vector2(0.25f, 0.5f));
    }

    // Portão de enrolar (o ponto de apoio fica em cima: ele "sobe" encolhendo)
    public static Sprite DesenharPortao()
    {
        Tela t = new Tela(Portao.width * Px, 18);
        int w = t.L;
        Color32 chapa = new Color32(176, 180, 188, 255), vinco = new Color32(132, 136, 146, 255), brilho = new Color32(210, 213, 220, 255);
        t.Ret(0, 0, w, 18, chapa);
        for (int y = 2; y < 15; y += 3)
        {
            t.Ret(1, y, w - 2, 1, vinco);
            t.Ret(1, y + 1, w - 2, 1, brilho);
        }
        t.Ret(0, 15, w, 3, new Color32(60, 60, 68, 255));     // borracha de baixo
        t.Ret(w / 2 - 5, 12, 10, 2, CinzaEscuro);               // puxador
        t.Borda(0, 0, w, 18, K);
        t.Ret(0, 0, 2, 18, CinzaEscuro);
        t.Ret(w - 2, 0, 2, 18, CinzaEscuro);
        return t.Salvar(PastaCasa, "PortaoGaragem", new Vector2(0.5f, 1f));
    }

    // ======================================================================
    // Tela da garagem (interface)
    // ======================================================================

    // Plataforma giratória (círculo visto de cima)
    public static Sprite PlataformaGiratoria()
    {
        const int n = 96;
        Tela t = new Tela(n, n);
        float c = (n - 1) / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x - c, dy = y - c, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > n / 2f - 0.5f) continue;
                Color32 cor;
                if (d > n / 2f - 2.5f) cor = K;
                else if (d > n / 2f - 6f) cor = new Color32(255, 30, 205, 255);
                else if (d > n / 2f - 8f) cor = K;
                else
                {
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 180f;
                    bool raio = ((int)(ang / 15f)) % 2 == 0;
                    cor = raio ? new Color32(56, 46, 76, 255) : new Color32(66, 56, 90, 255);
                    if (d < 10f) cor = new Color32(90, 80, 116, 255);
                }
                t.P(x, y, cor);
            }
        return t.Salvar(PastaUI, "PlataformaGiratoria", new Vector2(0.5f, 0.5f));
    }

    // Luz suave do holofote (branco que some nas bordas)
    public static Sprite Holofote()
    {
        const int n = 64;
        Tela t = new Tela(n, n);
        float c = (n - 1) / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / (n / 2f);
                if (d >= 1f) continue;
                byte a = (byte)(Mathf.Pow(1f - d, 1.6f) * 255f);
                t.P(x, y, new Color32(255, 255, 255, a));
            }
        return t.Salvar(PastaUI, "Holofote", new Vector2(0.5f, 0.5f));
    }

    public static Sprite MolduraSelecao() =>
        PixelArt.Caixa(PastaUI, "MolduraSelecao", 24, 3, new Color32(255, 225, 77, 255), new Color32(255, 225, 77, 255),
                       new Color32(255, 255, 255, 0));
}
