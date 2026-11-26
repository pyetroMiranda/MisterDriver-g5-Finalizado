using UnityEngine;

// Pixel art das telas do menu (Sobre, Opções, Níveis, Loja), no estilo dos blocos do menu principal:
// roxo-noite com neon magenta/ciano, contorno escuro e pixels grandes.
// - Titulo: texto com a fonte pixel do jogo, degradê branco → magenta, contorno e sombra
// - Cartao: moldura com cantos cortados e borda neon (Sliced)
// - Trilho/Tracejado/Carrinho: o controle de volume é uma estradinha com um carro (o Kicks) que anda nela
// - Tecla: tecla de teclado (Sliced) para a lista de comandos
// - Icone: ícones 32x32 no mesmo jeito dos ícones dos blocos
// Usado por MontarMenuTelas.
public static class MenuArte
{
    public const string Pasta = "Assets/Imagens/Menu";

    private static readonly Color32 K = PixelArt.Contorno;
    private static readonly Color32 Nada = new Color32(0, 0, 0, 0);

    // ======================================================================
    // Título
    // ======================================================================

    public static Sprite Titulo(string texto, string nome)
    {
        int largura = AutoescolaArte.LarguraTexto(texto) + 6;
        const int altura = 14;   // 3 de acento + 5 da letra + 1 de cedilha + contorno e sombra
        const int topo = 4;

        HistoriaArte.Tela letras = new HistoriaArte.Tela(largura, altura);
        AutoescolaArte.EscreverEm(letras, texto, largura / 2, topo, 1, new Color32(255, 255, 255, 255), Nada);

        bool[,] m = new bool[largura, altura];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                m[x, y] = Ler(letras, x, y).a > 0;

        HistoriaArte.Tela t = new HistoriaArte.Tela(largura, altura);
        Color32 sombra = new Color32(110, 16, 100, 255);
        // Sombra (um pixel para baixo do contorno)
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                if (!m[x, y] && y > 0 && Perto(m, x, y - 1)) t.P(x, y, sombra);
        // Contorno
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                if (!m[x, y] && Perto(m, x, y)) t.P(x, y, K);
        // Letras com degradê (linha 0-1 branca, 2 rosa clara, 3-4 magenta); acentos brancos, cedilha magenta
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                if (!m[x, y]) continue;
                int linha = y - topo;
                Color32 c;
                if (linha < 0 || linha <= 1) c = new Color32(255, 255, 255, 255);
                else if (linha == 2) c = new Color32(255, 196, 240, 255);
                else if (linha <= 4) c = new Color32(255, 70, 205, 255);
                else c = new Color32(255, 70, 205, 255);
                t.P(x, y, c);
            }
        return PixelArt.Salvar(Pasta, nome, t.p, largura, altura);
    }

    private static Color32 Ler(HistoriaArte.Tela t, int x, int y) => t.p[(t.A - 1 - y) * t.L + x];

    private static bool Perto(bool[,] m, int x, int y)
    {
        int l = m.GetLength(0), a = m.GetLength(1);
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int xx = x + dx, yy = y + dy;
                if (xx >= 0 && yy >= 0 && xx < l && yy < a && m[xx, yy]) return true;
            }
        return false;
    }

    // ======================================================================
    // Cartões e teclas
    // ======================================================================

    // Moldura neon com cantos cortados (fundo roxo-noite quase opaco)
    public static Sprite Cartao(string nome, Color32 neon)
    {
        const int n = 24;
        HistoriaArte.Tela t = new HistoriaArte.Tela(n, n);
        Color32 escuro = new Color32((byte)(neon.r * 0.45f), (byte)(neon.g * 0.45f), (byte)(neon.b * 0.45f), 255);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int camada = PixelArt.Camada(x, y, n, n, 3);
                if (camada < 0) continue;
                Color32 c;
                if (camada == 0) c = K;
                else if (camada == 1) c = neon;
                else if (camada == 2) c = escuro;
                else c = new Color32(22, 8, 36, 232);
                if (camada == 1 && y <= 1) c = new Color32((byte)Mathf.Min(255, neon.r + 80), (byte)Mathf.Min(255, neon.g + 80),
                                                           (byte)Mathf.Min(255, neon.b + 80), 255);   // brilho em cima
                t.P(x, y, c);
            }
        return PixelArt.Salvar(Pasta, nome, t.p, n, n, new Vector4(8, 8, 8, 8));
    }

    // Faixa lisa (para destaques e para o fundo das linhas selecionáveis)
    public static Sprite Faixa()
    {
        const int n = 12;
        HistoriaArte.Tela t = new HistoriaArte.Tela(n, n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int camada = PixelArt.Camada(x, y, n, n, 2);
                if (camada < 0) continue;
                t.P(x, y, camada == 0 ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 110));
            }
        return PixelArt.Salvar(Pasta, "FaixaSelecao", t.p, n, n, new Vector4(4, 4, 4, 4));
    }

    // Tecla clara com a lateral de baixo mais escura
    public static Sprite Tecla()
    {
        const int n = 16;
        HistoriaArte.Tela t = new HistoriaArte.Tela(n, n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int camada = PixelArt.Camada(x, y, n, n, 2);
                if (camada < 0) continue;
                Color32 c = PixelArt.TeclaFace;
                if (camada == 0) c = K;
                else if (y >= n - 4) c = PixelArt.TeclaLado;
                else if (y <= 2) c = PixelArt.TeclaBrilho;
                t.P(x, y, c);
            }
        return PixelArt.Salvar(Pasta, "Tecla", t.p, n, n, new Vector4(5, 5, 5, 5));
    }

    // ======================================================================
    // Controle de volume: estradinha + carrinho
    // ======================================================================

    public static Sprite Trilho()
    {
        const int w = 24, h = 14;
        HistoriaArte.Tela t = new HistoriaArte.Tela(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int camada = PixelArt.Camada(x, y, w, h, 2);
                if (camada < 0) continue;
                Color32 c = new Color32(52, 46, 66, 255);   // asfalto
                if (camada == 0) c = K;
                else if (y == 1 || y == h - 2) c = new Color32(255, 214, 60, 255);   // faixas amarelas da borda
                else if (y == 2) c = new Color32(40, 34, 52, 255);
                t.P(x, y, c);
            }
        return PixelArt.Salvar(Pasta, "TrilhoVolume", t.p, w, h, new Vector4(6, 6, 6, 6));
    }

    // Rastro neon que vai enchendo a estrada até o carro
    public static Sprite RastroNeon()
    {
        const int w = 12, h = 14;
        HistoriaArte.Tela t = new HistoriaArte.Tela(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int camada = PixelArt.Camada(x, y, w, h, 2);
                if (camada < 1) continue;
                float meio = 1f - Mathf.Abs(y - (h - 1) / 2f) / (h / 2f);
                byte a = (byte)(70 + 150 * meio);
                t.P(x, y, new Color32(255, (byte)(60 + 120 * meio), 210, a));
            }
        return PixelArt.Salvar(Pasta, "RastroVolume", t.p, w, h, new Vector4(4, 4, 4, 4));
    }

    // Tracejado branco do meio da pista (Image "Tiled")
    public static Sprite Tracejado()
    {
        HistoriaArte.Tela t = new HistoriaArte.Tela(8, 2);
        t.Ret(0, 0, 4, 2, new Color32(240, 240, 240, 255));
        return PixelArt.Salvar(Pasta, "TracejadoVolume", t.p, 8, 2);
    }

    // Kicks vermelho de teto preto visto de cima, andando para a direita
    public static Sprite Carrinho()
    {
        const int w = 24, h = 14;
        HistoriaArte.Tela t = new HistoriaArte.Tela(w, h);
        Color32 lataria = new Color32(214, 34, 48, 255), claro = new Color32(255, 96, 100, 255), escuro = new Color32(140, 16, 30, 255);
        Color32 teto = new Color32(30, 30, 38, 255), vidro = new Color32(60, 90, 140, 255), brilho = new Color32(160, 210, 255, 255);
        // Sombra
        for (int y = 3; y < h; y++) for (int x = 2; x < w; x++) t.P(x, y, new Color32(0, 0, 0, 90));
        for (int y = 1; y < h - 2; y++)
            for (int x = 0; x < w - 2; x++)
            {
                bool canto = (x == 0 || x == w - 3) && (y == 1 || y == h - 3);
                if (canto) continue;
                Color32 c = lataria;
                int frente = w - 3 - x;   // 0 = para-choque da frente
                if (frente <= 1) c = escuro;
                else if (frente <= 6) c = claro;                     // capô
                else if (frente <= 9) c = vidro;                     // para-brisa
                else if (frente <= 16) c = teto;                     // teto preto (dois tons)
                else if (frente <= 18) c = vidro;                    // vigia
                else if (x <= 1) c = escuro;
                if (y == 1 || y == h - 3) c = K;
                if (x == 0) c = K;
                if (x == w - 3) c = K;
                if (frente == 8 && y == 3) c = brilho;
                t.P(x, y, c);
            }
        // Faróis (frente) e lanternas (trás)
        t.P(w - 3, 3, new Color32(255, 246, 180, 255)); t.P(w - 3, h - 5, new Color32(255, 246, 180, 255));
        t.P(0, 3, new Color32(255, 40, 60, 255)); t.P(0, h - 5, new Color32(255, 40, 60, 255));
        // Retrovisores
        t.P(w - 13, 0, escuro); t.P(w - 13, h - 2, escuro);
        return PixelArt.Salvar(Pasta, "CarrinhoVolume", t.p, w, h);
    }

    // ======================================================================
    // Ícones (32x32: branco + amarelo, contornados)
    // ======================================================================

    public static Sprite Icone(string nome)
    {
        const int n = 32;
        bool[,] w = new bool[n, n];
        bool[,] a = new bool[n, n];

        switch (nome)
        {
            case "Musica":   // duas colcheias ligadas
                Elipse(w, 9, 24, 5, 4); Elipse(w, 23, 21, 5, 4);
                Ret(w, 12, 6, 3, 18); Ret(w, 26, 3, 3, 18);
                for (int x = 12; x < 29; x++) { int y = 6 - (x - 12) * 3 / 16; Ret(w, x, y, 1, 4); }
                Elipse(a, 8, 23, 2, 1);
                Elipse(a, 22, 20, 2, 1);
                break;
            case "Efeitos":   // alto-falante com ondas
                Ret(w, 3, 12, 7, 9);
                for (int x = 10; x < 18; x++) Ret(w, x, 12 - (x - 10), 1, 9 + (x - 10) * 2);
                Arco(a, 17, 16, 6, 7.5f); Arco(a, 17, 16, 10, 11.5f); Arco(a, 17, 16, 14, 15f);
                break;
            case "TelaCheia":   // monitor com setas para fora
                Ret(w, 2, 4, 28, 19); Limpar(w, 4, 6, 24, 15);
                Ret(w, 13, 23, 6, 3); Ret(w, 8, 26, 16, 3);
                Ret(a, 6, 8, 6, 2); Ret(a, 6, 8, 2, 6);       // cantos amarelos
                Ret(a, 20, 8, 6, 2); Ret(a, 24, 8, 2, 6);
                Ret(a, 6, 17, 2, 2); Ret(a, 6, 19, 6, 0);
                Ret(a, 6, 17, 2, 3); Ret(a, 6, 19, 6, 1);
                Ret(a, 24, 17, 2, 3); Ret(a, 20, 19, 6, 1);
                break;
            case "Recomecar":   // seta circular
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = x - 15.5f, dy = y - 16.5f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float ang = Mathf.Atan2(-dy, dx) * Mathf.Rad2Deg;
                        if (d >= 8f && d <= 12.5f && !(ang > 50f && ang < 110f)) w[x, y] = true;
                    }
                for (int i = 0; i < 7; i++) Ret(a, 15 + i, 1 + i, 1, 13 - i * 2 > 0 ? 13 - i * 2 : 1);
                break;
            case "Voltar":   // seta para a esquerda
                Ret(w, 12, 12, 17, 8);
                for (int i = 0; i < 12; i++) Ret(w, 2 + i, 16 - i, 1, i * 2 + 1);
                Ret(a, 14, 14, 13, 4);
                break;
            case "Moeda":   // moeda dourada com $
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = (x - 15.5f) * (x - 15.5f) + (y - 15.5f) * (y - 15.5f);
                        if (d <= 196f) a[x, y] = true;
                        if (d <= 110f && d >= 72f) { w[x, y] = true; a[x, y] = false; }
                    }
                Ret(w, 13, 8, 6, 2); Ret(w, 11, 10, 3, 4); Ret(w, 13, 14, 6, 2); Ret(w, 18, 16, 3, 4); Ret(w, 13, 20, 6, 2);
                Ret(w, 15, 6, 2, 20);
                break;
            case "Escola":   // capelo de formatura
                for (int y = 4; y < 18; y++)
                {
                    int meia = 14 - Mathf.Abs(y - 11) * 2;
                    if (meia > 0) Ret(w, 16 - meia, y, meia * 2, 1);
                }
                Ret(w, 8, 16, 16, 8); Limpar(w, 10, 18, 12, 4);
                Ret(a, 15, 10, 2, 2); Ret(a, 24, 11, 2, 10); Ret(a, 23, 21, 4, 4);
                break;
            case "Teclado":   // teclado com teclas
                Ret(w, 1, 8, 30, 17); Limpar(w, 3, 10, 26, 13);
                for (int y = 11; y < 22; y += 4)
                    for (int x = 4; x < 28; x += 4) Ret(y == 19 && x > 8 && x < 24 ? a : w, x, y, 3, 3);
                Ret(a, 9, 19, 14, 3);
                break;
            case "Carro":   // carrinho visto de lado
                Ret(w, 2, 15, 28, 8); Ret(w, 8, 8, 15, 8); Limpar(w, 10, 10, 5, 5); Limpar(w, 16, 10, 5, 5);
                Elipse(a, 9, 23, 4, 4); Elipse(a, 23, 23, 4, 4);
                Ret(a, 27, 17, 3, 2);
                break;
            case "Controle":   // controle de videogame
                Elipse(w, 8, 17, 7, 8); Elipse(w, 24, 17, 7, 8); Ret(w, 8, 10, 16, 13);
                Ret(a, 5, 15, 7, 2); Ret(a, 7, 13, 2, 7);           // direcional
                Elipse(a, 22, 14, 2, 2); Elipse(a, 26, 18, 2, 2);   // botões
                Limpar(w, 5, 15, 7, 2); Limpar(w, 7, 13, 2, 7);
                Limpar(w, 20, 12, 4, 4); Limpar(w, 24, 16, 4, 4);
                break;
            case "Trofeu":   // troféu
                Ret(a, 9, 4, 14, 12); Elipse(a, 16, 15, 7, 5);
                Arco(w, 8, 10, 3, 5f); Arco(w, 24, 10, 3, 5f);
                Ret(a, 14, 19, 4, 5); Ret(w, 9, 24, 14, 4);
                Ret(w, 12, 6, 2, 7);
                break;
        }

        HistoriaArte.Tela c = new HistoriaArte.Tela(n, n);
        Color32 branco = new Color32(255, 255, 255, 255), amarelo = new Color32(255, 214, 60, 255);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                if (w[x, y]) c.P(x, y, branco);
                else if (a[x, y]) c.P(x, y, amarelo);
                else if (Perto(w, x, y) || Perto(a, x, y)) c.P(x, y, K);
            }
        return PixelArt.Salvar(Pasta, "Icone" + nome, c.p, n, n);
    }

    private static void Ret(bool[,] m, int x, int y, int w, int h)
    {
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                if (xx >= 0 && yy >= 0 && xx < m.GetLength(0) && yy < m.GetLength(1)) m[xx, yy] = true;
    }

    private static void Limpar(bool[,] m, int x, int y, int w, int h)
    {
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                if (xx >= 0 && yy >= 0 && xx < m.GetLength(0) && yy < m.GetLength(1)) m[xx, yy] = false;
    }

    private static void Elipse(bool[,] m, float cx, float cy, float rx, float ry)
    {
        for (int y = 0; y < m.GetLength(1); y++)
            for (int x = 0; x < m.GetLength(0); x++)
            {
                float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f) m[x, y] = true;
            }
    }

    // Arco virado para a direita (ondas do alto-falante, alças do troféu)
    private static void Arco(bool[,] m, float cx, float cy, float r0, float r1)
    {
        for (int y = 0; y < m.GetLength(1); y++)
            for (int x = 0; x < m.GetLength(0); x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d >= r0 && d <= r1 && Mathf.Abs(dy) <= d * 0.75f && (dx > 0f || r1 < 6f)) m[x, y] = true;
            }
    }
}
