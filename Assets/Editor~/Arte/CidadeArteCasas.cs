using System.Collections.Generic;
using UnityEngine;

// Casas vizinhas do Nivel1, no mesmo estilo da casa do Jason e do mercado (8 pixels = 1 unidade, vistas de cima,
// telhado de telhas com a faixa da fachada embaixo). Cada imagem é um terreno com 1 ou 2 lotes lado a lado.
// Cada lote sorteia (sempre igual para a mesma semente): tipo de telhado, cores, garagem com carro, cerca
// (madeira, muro ou cerca viva) e o quintal (piscina, horta, varal, mesa com guarda-sol, cama elástica,
// casinha de cachorro, árvores...). "frenteEmBaixo" diz de que lado fica a rua (o portão e a garagem).
public static partial class CidadeArte
{
    private enum TipoTelhado { QuatroAguas, DuasAguas, Laje }

    private struct Paleta
    {
        public Color32 cima, baixo, esquerda, direita, cumeeira;
    }

    private static Paleta PaletaDe(Color32 b)
    {
        return new Paleta
        {
            cima = Escurecer(b, 0.74f),
            baixo = Clarear(b, 32),
            esquerda = b,
            direita = Escurecer(b, 0.88f),
            cumeeira = Escurecer(b, 0.56f),
        };
    }

    // Cores de telhado da vizinhança (a do Jason é a terracota)
    private static readonly Color32[] CoresTelhado =
    {
        new Color32(198, 94, 54, 255),    // terracota
        new Color32(84, 104, 140, 255),   // ardósia azul
        new Color32(120, 84, 62, 255),    // marrom
        new Color32(70, 120, 92, 255),    // verde
        new Color32(150, 60, 70, 255),    // vinho
        new Color32(104, 104, 112, 255),  // cinza
        new Color32(196, 128, 60, 255),   // telha clara
    };

    private static readonly Color32[] CoresParede =
    {
        new Color32(238, 216, 172, 255),  // creme (igual à do Jason)
        new Color32(196, 222, 238, 255),  // azul bebê
        new Color32(240, 196, 200, 255),  // rosa
        new Color32(200, 232, 204, 255),  // menta
        new Color32(250, 236, 150, 255),  // amarelo
        new Color32(222, 206, 240, 255),  // lilás
        new Color32(244, 244, 240, 255),  // branca
        new Color32(232, 184, 140, 255),  // pêssego
    };

    private static readonly Color32[] CoresCarro =
    {
        new Color32(200, 40, 50, 255), new Color32(40, 90, 190, 255), new Color32(240, 240, 240, 255),
        new Color32(40, 40, 48, 255), new Color32(170, 176, 186, 255), new Color32(250, 200, 40, 255),
        new Color32(40, 150, 90, 255), new Color32(120, 60, 160, 255),
    };

    private static bool[,] ocupado;   // por lote (coordenadas da imagem inteira)
    private static System.Random rnd;

    private static int R(int min, int maxExclusivo) => rnd.Next(min, maxExclusivo);
    private static bool Chance(float p) => rnd.NextDouble() < p;
    private static T Sorteio<T>(T[] opcoes) => opcoes[rnd.Next(opcoes.Length)];

    // Desenha um terreno (W x H pixels) com "lotes" casas lado a lado e salva com o nome dado
    public static Sprite Vizinhanca(string nome, int W, int H, int lotes, bool frenteEmBaixo, int semente)
    {
        t = new HistoriaArte.Tela(W, H);
        ocupado = new bool[W, H];
        rnd = new System.Random(semente * 7919 + 17);
        lotes = Mathf.Max(1, lotes);

        int x0 = 0;
        for (int i = 0; i < lotes; i++)
        {
            int x1 = i == lotes - 1 ? W : Mathf.RoundToInt(W * (i + 1) / (float)lotes);
            Lote(x0, x1 - x0, H, frenteEmBaixo, i == 0, i == lotes - 1);
            x0 = x1;
        }

        return HistoriaArte.SalvarComPpu(t, Pasta, nome, PPU, new Vector2(0.5f, 0.5f));
    }

    // ======================================================================
    // Um lote
    // ======================================================================

    private static void Lote(int ox, int Lw, int H, bool frenteEmBaixo, bool primeiro, bool ultimo)
    {
        bool listras = Chance(0.55f);
        for (int y = 0; y < H; y++)
            for (int x = ox; x < ox + Lw; x++)
            {
                Color32 c = CorGrama(x, y);
                if (listras && (y / 8) % 2 == 0) c = Clarear(c, 6);
                t.P(x, y, c);
            }

        // ---------- Sorteios ----------
        TipoTelhado tipo = Chance(0.45f) ? TipoTelhado.QuatroAguas : Chance(0.6f) ? TipoTelhado.DuasAguas : TipoTelhado.Laje;
        Paleta telhado = PaletaDe(Sorteio(CoresTelhado));
        Color32 parede = Sorteio(CoresParede);
        if (tipo == TipoTelhado.Laje && Chance(0.6f)) parede = new Color32(244, 244, 240, 255);   // casa moderna branca
        int cerca = Chance(0.5f) ? 0 : Chance(0.5f) ? 1 : 2;   // 0 madeira, 1 muro, 2 cerca viva

        bool garagem = Lw >= 66 && Chance(0.8f);
        bool garagemEsquerda = Chance(0.5f);

        // ---------- Medidas da casa ----------
        int margem = 6 + R(0, 3);
        int fundo = 7 + R(0, 4);                     // espaço entre o fundo do lote e a casa
        int hTelhado = Mathf.Clamp(Mathf.RoundToInt(H * (0.34f + (float)rnd.NextDouble() * 0.08f)), 34, 84);
        const int hParede = 8;
        int larguraUtil = Lw - margem * 2;

        int gw = garagem ? Mathf.Clamp(26 + R(0, 4), 24, larguraUtil / 2) : 0;
        int mw = garagem ? larguraUtil - gw + 6 : Mathf.Min(larguraUtil, Mathf.Max(40, larguraUtil - R(0, 12)));
        int mx = garagem ? (garagemEsquerda ? ox + margem + gw - 6 : ox + margem) : ox + (Lw - mw) / 2;
        int gx = garagem ? (garagemEsquerda ? ox + margem : ox + margem + mw - 6) : 0;

        // y de cima do telhado principal e da garagem (a garagem fica mais perto da rua)
        int blocoPrincipal = hTelhado + hParede + 1;
        int mTopo = frenteEmBaixo ? fundo : H - fundo - blocoPrincipal;
        int hGaragem = Mathf.RoundToInt(hTelhado * 0.62f);
        int gTopo = frenteEmBaixo ? mTopo + hTelhado - hGaragem + 10 : mTopo - 10;

        // ---------- Casa ----------
        // Sombras na grama
        Sombra(mx + 3, mTopo + 3, mw, blocoPrincipal);
        if (garagem) Sombra(gx + 3, gTopo + 3, gw, hGaragem + hParede + 1);

        int portaX = mx + mw / 2 - 4 + (garagem ? (garagemEsquerda ? 6 : -6) : R(-6, 7));
        int janela1 = mx + 5, janela2 = mx + mw - 14;

        if (frenteEmBaixo)
        {
            // Telhado principal, depois a garagem na frente dele
            TelhadoTipo(tipo, mx, mTopo, mx + mw - 1, mTopo + hTelhado - 1, telhado);
            if (garagem) TelhadoTipo(tipo == TipoTelhado.Laje ? TipoTelhado.Laje : TipoTelhado.QuatroAguas,
                                     gx, gTopo, gx + gw - 1, gTopo + hGaragem - 1, telhado);
            EnfeitesTelhado(tipo, mx, mTopo, mw, hTelhado);

            // Fachada da casa (sem a parte escondida atrás da garagem)
            int fx0 = mx, fx1 = mx + mw;
            if (garagem && garagemEsquerda) fx0 = gx + gw;
            if (garagem && !garagemEsquerda) fx1 = gx;
            int fy = mTopo + hTelhado;
            ParedeCor(fx0, fy, fx1 - fx0, hParede, parede);
            Janelas(fx0, fx1, fy, portaX, tipo == TipoTelhado.Laje);
            PortaCor(portaX, fy, tipo == TipoTelhado.Laje ? new Color32(70, 70, 80, 255) : new Color32(130, 80, 46, 255));
            Ret(portaX - 2, fy + hParede, 12, 3, new Color32(206, 200, 188, 255));   // degrau
            Ret(portaX - 2, fy + hParede + 2, 12, 1, new Color32(150, 144, 132, 255));
            Ocupar(mx - 1, mTopo - 1, mw + 5, blocoPrincipal + 6);

            if (garagem)
            {
                int gy = gTopo + hGaragem;
                ParedeCor(gx, gy, gw, hParede, parede);
                PortaoGaragem(gx + 3, gy, gw - 6, hParede);
                Ret(gx + 1, gy + 1, 1, 2, new Color32(255, 230, 120, 255));    // luminárias
                Ret(gx + gw - 2, gy + 1, 1, 2, new Color32(255, 230, 120, 255));
                Ocupar(gx - 1, gTopo - 1, gw + 5, hGaragem + hParede + 6);
            }
        }
        else
        {
            // Rua em cima: a garagem fica atrás (em cima) do telhado principal, que aparece na frente
            if (garagem)
            {
                TelhadoTipo(tipo == TipoTelhado.Laje ? TipoTelhado.Laje : TipoTelhado.QuatroAguas,
                            gx, gTopo, gx + gw - 1, gTopo + hGaragem + 8, telhado);
                Ocupar(gx - 1, gTopo - 1, gw + 5, hGaragem + 14);
            }
            TelhadoTipo(tipo, mx, mTopo, mx + mw - 1, mTopo + hTelhado - 1, telhado);
            EnfeitesTelhado(tipo, mx, mTopo, mw, hTelhado);
            int fy = mTopo + hTelhado;
            ParedeCor(mx, fy, mw, hParede, parede);   // fundos da casa: só janelas
            Janelas(mx, mx + mw, fy, -100, tipo == TipoTelhado.Laje);
            Ocupar(mx - 1, mTopo - 1, mw + 5, blocoPrincipal + 6);

            // Marquise da porta (a porta fica virada para a rua, embaixo do beiral)
            Ret(portaX - 3, mTopo - 4, 14, 4, new Color32(150, 150, 160, 255));
            Ret(portaX - 3, mTopo - 4, 14, 1, new Color32(190, 190, 200, 255));
            Borda(portaX - 4, mTopo - 5, 16, 6, K);
        }

        // ---------- Caminho, entrada do carro e portão ----------
        int portaoX = Mathf.Clamp(portaX - 4 + R(-3, 4), ox + 5, ox + Lw - 21);
        int gentX0 = garagem ? gx + 2 : 0, gentX1 = garagem ? gx + gw - 2 : 0;

        if (garagem)
        {
            int ya = frenteEmBaixo ? gTopo + hGaragem + hParede : 0;
            int yb = frenteEmBaixo ? H : gTopo;
            EntradaCarro(gentX0, ya, gentX1 - gentX0, yb - ya);
            Ocupar(gentX0 - 1, ya, gentX1 - gentX0 + 2, yb - ya);
            if (Chance(0.7f))
            {
                int cy = frenteEmBaixo ? ya + 3 : yb - 36;
                CarroEstacionado((gentX0 + gentX1) / 2, cy, Sorteio(CoresCarro), frenteEmBaixo);
            }
            // Lixeiras perto da rua
            int lx = garagemEsquerda ? gentX1 + 2 : gentX0 - 8;
            int ly = frenteEmBaixo ? H - 10 : 4;
            if (Cabe(lx, ly, 6, 5)) { Lixeiras(lx, ly); Ocupar(lx, ly, 6, 5); }
        }

        // Caminho da porta até o portão
        bool pedras = Chance(0.55f);
        int yPortaFim = frenteEmBaixo ? mTopo + blocoPrincipal + 2 : 0;
        int yPortaIni = frenteEmBaixo ? H : mTopo - 5;
        Caminho(portaX + 4, portaoX + 8, Mathf.Min(yPortaFim, yPortaIni), Mathf.Max(yPortaFim, yPortaIni), frenteEmBaixo, pedras);

        // Caixa de correio ao lado do portão
        int cxCorreio = portaoX + 18;
        int cyCorreio = frenteEmBaixo ? H - 12 : 4;
        if (Cabe(cxCorreio, cyCorreio, 6, 8)) { CaixaCorreio(cxCorreio, cyCorreio); Ocupar(cxCorreio, cyCorreio, 6, 8); }

        // ---------- Quintal ----------
        Quintal(ox, Lw, H, frenteEmBaixo, mTopo, blocoPrincipal);

        // ---------- Cerca ----------
        CercaLote(ox, Lw, H, frenteEmBaixo, cerca, primeiro, ultimo, portaoX, garagem ? gentX0 : -1, garagem ? gentX1 : -1);
    }

    // ======================================================================
    // Telhados
    // ======================================================================

    private static void TelhadoTipo(TipoTelhado tipo, int x0, int y0, int x1, int y1, Paleta p)
    {
        switch (tipo)
        {
            case TipoTelhado.DuasAguas: TelhadoDuasAguas(x0, y0, x1, y1, p); break;
            case TipoTelhado.Laje: Laje(x0, y0, x1, y1); break;
            default: TelhadoCor(x0, y0, x1, y1, p); break;
        }
    }

    // Quatro águas (igual ao do Jason, com outra cor)
    private static void TelhadoCor(int x0, int y0, int x1, int y1, Paleta p)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int dl = x - x0, dr = x1 - x, dt = y - y0, db = y1 - y;
                int m = Mathf.Min(Mathf.Min(dl, dr), Mathf.Min(dt, db));
                Color32 c;
                bool vertical;
                if (m == dt) { c = p.cima; vertical = false; }
                else if (m == db) { c = p.baixo; vertical = false; }
                else if (m == dl) { c = p.esquerda; vertical = true; }
                else { c = p.direita; vertical = true; }

                int fila = vertical ? dl < dr ? dl : dr : dt < db ? dt : db;
                int ao = vertical ? y : x;
                if (fila % 4 == 3) c = Escurecer(c, 0.8f);
                else if ((ao + (fila / 4) * 3) % 6 == 0) c = Escurecer(c, 0.88f);
                else if (fila % 4 == 0) c = Clarear(c, 14);

                int iguais = 0;
                if (dl == m) iguais++;
                if (dr == m) iguais++;
                if (dt == m) iguais++;
                if (db == m) iguais++;
                if (iguais >= 2) c = p.cumeeira;

                if (m == 0) c = K;
                t.P(x, y, c);
            }
    }

    // Duas águas: cumeeira deitada no meio, metade de cima na sombra e a de baixo no sol
    private static void TelhadoDuasAguas(int x0, int y0, int x1, int y1, Paleta p)
    {
        int meio = (y0 + y1) / 2;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                bool cima = y < meio;
                Color32 c = cima ? p.cima : p.baixo;
                int fila = cima ? meio - y : y - meio;
                if (fila % 4 == 3) c = Escurecer(c, 0.8f);
                else if ((x + (fila / 4) * 3) % 6 == 0) c = Escurecer(c, 0.88f);
                else if (fila % 4 == 0) c = Clarear(c, 12);
                if (y == meio || y == meio - 1) c = p.cumeeira;
                if (x == x0 + 1 || x == x1 - 1) c = Escurecer(c, 0.75f);   // beiral das pontas
                if (x == x0 || x == x1 || y == y0 || y == y1) c = K;
                t.P(x, y, c);
            }
    }

    // Laje de casa moderna: concreto claro com platibanda
    private static void Laje(int x0, int y0, int x1, int y1)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int v = 188 + Hash(x, y) % 8 - 4;
                Color32 c = new Color32((byte)v, (byte)v, (byte)(v + 4), 255);
                int borda = Mathf.Min(Mathf.Min(x - x0, x1 - x), Mathf.Min(y - y0, y1 - y));
                if (borda <= 2) c = new Color32(226, 226, 230, 255);
                if (borda == 2) c = new Color32(150, 150, 158, 255);
                if (borda == 0) c = K;
                t.P(x, y, c);
            }
        // Ralos e manchinhas
        Ret(x0 + 5, y1 - 6, 2, 2, new Color32(110, 110, 120, 255));
        Ret(x1 - 6, y1 - 6, 2, 2, new Color32(110, 110, 120, 255));
    }

    // Chaminé, antena, painel solar, caixa d'água e ar-condicionado (cada casa ganha alguns)
    private static void EnfeitesTelhado(TipoTelhado tipo, int x, int y, int w, int h)
    {
        if (tipo == TipoTelhado.Laje)
        {
            // Caixa d'água azul e painéis solares (casa moderna e sustentável)
            CaixaDagua(x + w - 14, y + 5);
            int paineis = Mathf.Clamp((w - 24) / 12, 1, 4);
            for (int i = 0; i < paineis; i++) PainelSolar(x + 6 + i * 12, y + h / 2 - 2, 10, 14);
            if (Chance(0.6f)) ArCondicionado(x + 6, y + 5);
            return;
        }

        // Telhado de telha: o painel fica na água de baixo (a que pega sol)
        if (Chance(0.55f))
        {
            int paineis = Mathf.Clamp((w - 30) / 12, 1, 3);
            int py = tipo == TipoTelhado.DuasAguas ? y + h / 2 + 4 : y + h - 18;
            int px = x + (w - paineis * 12) / 2;
            for (int i = 0; i < paineis; i++) PainelSolar(px + i * 12, py, 10, 12);
        }
        if (Chance(0.6f)) Chamine(x + 6 + R(0, 6), y + 6 + R(0, 4));
        if (Chance(0.5f)) Antena(x + w - 10, y + 6);
    }

    private static void PainelSolar(int x, int y, int w, int h)
    {
        Ret(x + 1, y + 1, w, h, new Color32(0, 0, 0, 80), true);
        Ret(x, y, w, h, new Color32(34, 52, 104, 255));
        for (int yy = y + 1; yy < y + h; yy += 3) Ret(x + 1, yy, w - 2, 1, new Color32(70, 110, 180, 255));
        Ret(x + w / 2, y, 1, h, new Color32(150, 160, 180, 255));
        t.P(x + 1, y + 1, new Color32(170, 210, 255, 255));   // reflexo
        Borda(x, y, w, h, new Color32(200, 204, 214, 255));
    }

    private static void CaixaDagua(int x, int y)
    {
        Circulo(x + 5.5f, y + 5.5f, 5.5f, K);
        Circulo(x + 5.5f, y + 5.5f, 4.5f, new Color32(60, 120, 200, 255));
        Circulo(x + 4.5f, y + 4.5f, 2.5f, new Color32(110, 170, 240, 255));
        t.P(x + 5, y + 5, new Color32(220, 240, 255, 255));
    }

    // ======================================================================
    // Fachada
    // ======================================================================

    private static void ParedeCor(int x, int y, int w, int h, Color32 cor)
    {
        if (w <= 0) return;
        Ret(x, y, w, h, cor);
        Ret(x, y, w, 1, Escurecer(cor, 0.5f));                 // sombra do beiral
        Ret(x, y + h - 2, w, 2, Escurecer(cor, 0.86f));
        Borda(x, y - 1, w, h + 1, K);
    }

    private static void Janelas(int x0, int x1, int y, int portaX, bool moderna)
    {
        for (int x = x0 + 4; x + 9 <= x1 - 3; x += moderna ? 15 : 13)
        {
            if (x + 9 > portaX - 2 && x < portaX + 10) continue;   // não em cima da porta
            if (moderna) JanelaGrande(x, y + 1);
            else Janela(x, y + 1);
        }
    }

    private static void JanelaGrande(int x, int y)
    {
        Ret(x, y, 12, 5, new Color32(60, 64, 76, 255));
        Ret(x + 1, y + 1, 10, 3, new Color32(140, 200, 236, 255));
        Ret(x + 1, y + 1, 3, 1, Color32Branco);
        Ret(x + 6, y + 1, 1, 3, new Color32(60, 64, 76, 255));
        Borda(x, y, 12, 5, K);
    }

    private static void PortaCor(int x, int y, Color32 c)
    {
        Ret(x, y, 8, 8, c);
        Ret(x + 1, y + 1, 6, 1, Clarear(c, 30));
        t.P(x + 6, y + 4, new Color32(250, 210, 80, 255));
        Borda(x, y, 8, 8, K);
    }

    // ======================================================================
    // Quintal
    // ======================================================================

    private static void EntradaCarro(int x, int y, int w, int h)
    {
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
            {
                int v = 198 + Hash(xx, yy) % 6 - 3;
                if ((yy - y) % 16 == 15) v = 168;
                if (xx == x || xx == x + w - 1) v = 176;
                t.P(xx, yy, new Color32((byte)v, (byte)v, (byte)(v - 8), 255));
            }
        if (h > 20) Mancha(x + w / 2, y + h / 2, 2);
    }

    private static void Caminho(int xa, int xb, int y0, int y1, bool frenteEmBaixo, bool pedras)
    {
        // Desce (ou sobe) da porta, faz a curva e chega no portão
        int meio = frenteEmBaixo ? y1 - 14 : y0 + 14;
        for (int y = y0; y < y1; y += pedras ? 6 : 5)
        {
            bool antes = frenteEmBaixo ? y < meio : y >= meio;
            int xc = antes ? xa : xb;
            if (pedras) PedraCaminho(xc - 3 + ((y / 6) % 2 == 0 ? -1 : 1), y, 6 + (y / 6) % 2, 4);
            else LajotaCaminho(xc - 3, y, 7, 4);
        }
        // Trecho deitado entre a porta e o portão
        int x0 = Mathf.Min(xa, xb) - 3, x1 = Mathf.Max(xa, xb) + 3;
        for (int x = x0; x < x1; x += pedras ? 7 : 8)
            if (pedras) PedraCaminho(x, meio, 6, 4); else LajotaCaminho(x, meio, 7, 4);
        Ocupar(xa - 5, y0, 10, y1 - y0);
        Ocupar(xb - 5, y0, 10, y1 - y0);
        Ocupar(x0 - 1, meio - 1, x1 - x0 + 2, 6);
    }

    private static void PedraCaminho(int x, int y, int w, int h)
    {
        Ret(x, y, w, h, new Color32(184, 176, 158, 255));
        Ret(x + 1, y, w - 2, 1, new Color32(208, 200, 182, 255));
        Ret(x + 1, y + h - 1, w - 2, 1, new Color32(130, 122, 106, 255));
        t.P(x, y, CorGrama(x, y)); t.P(x + w - 1, y, CorGrama(x + w - 1, y));
        t.P(x, y + h - 1, CorGrama(x, y + h - 1)); t.P(x + w - 1, y + h - 1, CorGrama(x + w - 1, y + h - 1));
    }

    private static void LajotaCaminho(int x, int y, int w, int h)
    {
        Ret(x, y, w, h, new Color32(206, 206, 200, 255));
        Ret(x, y, w, 1, new Color32(228, 228, 222, 255));
        Ret(x, y + h - 1, w, 1, new Color32(156, 156, 150, 255));
    }

    // Distribui enfeites pelo terreno, sem encostar em nada que já está desenhado
    private static void Quintal(int ox, int Lw, int H, bool frenteEmBaixo, int mTopo, int bloco)
    {
        // Ordem de preferência embaralhada (cada lote fica diferente)
        List<int> itens = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        for (int i = itens.Count - 1; i > 0; i--) { int j = R(0, i + 1); int a = itens[i]; itens[i] = itens[j]; itens[j] = a; }

        int colocados = 0;
        int maximo = Mathf.Clamp(Lw * H / 2600, 3, 7);

        // Sempre tenta uma árvore grande e alguns arbustos
        TentarColocar(ox, Lw, H, 26, 26, (x, y) => Arvore(x + 13, y + 13, 9f + R(0, 4)), 2);

        foreach (int item in itens)
        {
            if (colocados >= maximo) break;
            bool ok = false;
            switch (item)
            {
                case 0: if (Lw >= 70) ok = TentarColocar(ox, Lw, H, 34, 22, (x, y) => Piscina(x, y, 34, 22), 4); break;
                case 1: ok = TentarColocar(ox, Lw, H, 21, 26, (x, y) => { Horta(x, y, 9, 12); Horta(x + 11, y, 9, 12); Horta(x, y + 14, 9, 12); Horta(x + 11, y + 14, 9, 12); }, 2); break;
                case 2: ok = TentarColocar(ox, Lw, H, 10, 40, (x, y) => VaralCores(x + 4, y + 1, y + 39), 1); break;
                case 3: ok = TentarColocar(ox, Lw, H, 18, 18, (x, y) => MesaGuardaSol(x + 9, y + 9), 2); break;
                case 4: ok = TentarColocar(ox, Lw, H, 22, 22, (x, y) => CamaElastica(x + 11, y + 11), 2); break;
                case 5: ok = TentarColocar(ox, Lw, H, 20, 14, (x, y) => CasinhaCachorro(x, y), 2); break;
                case 6: ok = TentarColocar(ox, Lw, H, 16, 6, (x, y) => Canteiro(x + 1, y + 1, 14, 4), 1); break;
                case 7: ok = TentarColocar(ox, Lw, H, 12, 12, (x, y) => Bebedouro(x + 6, y + 6), 1); break;
                case 8: ok = TentarColocar(ox, Lw, H, 14, 6, (x, y) => Banco(x + 1, y + 1), 1); break;
            }
            if (ok) colocados++;
        }

        // Arbustos para completar
        for (int i = 0; i < 6; i++)
            TentarColocar(ox, Lw, H, 10, 10, (x, y) => ArbustoCheio(x + 5, y + 5, 3.5f + R(0, 2)), 0);

        // Canteiro de flores colado na fachada (só quando ela aparece)
        if (frenteEmBaixo)
        {
            int y = mTopo + bloco + 1;
            for (int x = ox + 4; x < ox + Lw - 8; x += 9)
                if (Cabe(x, y, 8, 3)) { Canteiro(x, y, 7, 2); Ocupar(x, y, 8, 3); }
        }
    }

    private delegate void Desenho(int x, int y);

    // Procura um lugar livre (com folga) dentro do lote e desenha
    private static bool TentarColocar(int ox, int Lw, int H, int w, int h, Desenho desenhar, int folga)
    {
        for (int tentativa = 0; tentativa < 40; tentativa++)
        {
            int x = ox + 4 + R(0, Mathf.Max(1, Lw - w - 8));
            int y = 4 + R(0, Mathf.Max(1, H - h - 8));
            if (!Cabe(x - folga, y - folga, w + folga * 2, h + folga * 2)) continue;
            desenhar(x, y);
            Ocupar(x - 1, y - 1, w + 2, h + 2);
            return true;
        }
        return false;
    }

    private static bool Cabe(int x, int y, int w, int h)
    {
        if (x < 0 || y < 0 || x + w > t.L || y + h > t.A) return false;
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                if (ocupado[xx, yy]) return false;
        return true;
    }

    private static void Ocupar(int x, int y, int w, int h)
    {
        for (int yy = Mathf.Max(0, y); yy < Mathf.Min(t.A, y + h); yy++)
            for (int xx = Mathf.Max(0, x); xx < Mathf.Min(t.L, x + w); xx++)
                ocupado[xx, yy] = true;
    }

    // ---------- Enfeites ----------

    private static void Piscina(int x, int y, int w, int h)
    {
        Ret(x + 2, y + 2, w, h, new Color32(0, 0, 0, 60), true);
        // Borda de pedra clara com juntas
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
            {
                bool borda = xx < x + 3 || xx >= x + w - 3 || yy < y + 3 || yy >= y + h - 3;
                if (!borda) continue;
                Color32 c = new Color32(232, 228, 214, 255);
                if ((xx + yy) % 4 == 0) c = new Color32(206, 200, 184, 255);
                t.P(xx, yy, c);
            }
        // Água: mais funda (escura) de um lado, com reflexos
        for (int yy = y + 3; yy < y + h - 3; yy++)
            for (int xx = x + 3; xx < x + w - 3; xx++)
            {
                float k = (xx - x) / (float)w;
                Color32 c = Color32.Lerp(new Color32(110, 210, 240, 255), new Color32(40, 130, 210, 255), k);
                if (yy == y + 3) c = new Color32(30, 100, 170, 255);   // sombra da borda
                if ((xx * 3 + yy * 5) % 23 == 0) c = new Color32(220, 250, 255, 255);
                t.P(xx, yy, c);
            }
        // Escadinha e uma boia
        Ret(x + w - 8, y + 3, 1, 5, new Color32(200, 204, 214, 255));
        Ret(x + w - 6, y + 3, 1, 5, new Color32(200, 204, 214, 255));
        Ret(x + w - 8, y + 5, 3, 1, new Color32(200, 204, 214, 255));
        if (Chance(0.7f))
        {
            float bx = x + 8 + R(0, w - 18), by = y + 8 + R(0, Mathf.Max(1, h - 16));
            Circulo(bx, by, 3f, Chance(0.5f) ? new Color32(255, 90, 90, 255) : new Color32(255, 210, 40, 255));
            Circulo(bx, by, 1.2f, new Color32(60, 160, 220, 255));
        }
        Borda(x, y, w, h, new Color32(150, 144, 130, 255));
    }

    private static void VaralCores(int x, int y0, int y1)
    {
        Ret(x - 1, y0 - 1, 3, 3, new Color32(90, 60, 40, 255));
        Ret(x - 1, y1 - 1, 3, 3, new Color32(90, 60, 40, 255));
        Ret(x, y0, 1, y1 - y0, new Color32(236, 236, 226, 255));
        Color32[] roupas = { new Color32(220, 60, 70, 255), new Color32(60, 90, 170, 255), new Color32(250, 250, 250, 255),
                             new Color32(250, 200, 50, 255), new Color32(110, 190, 110, 255), new Color32(200, 120, 220, 255) };
        int y = y0 + 4;
        while (y < y1 - 6)
        {
            int w = Chance(0.5f) ? 7 : 5, h = Chance(0.5f) ? 6 : 4;
            if (y + h > y1 - 2) break;
            Roupa(x, y, w, h, Sorteio(roupas));
            y += h + 3;
        }
    }

    private static void MesaGuardaSol(int cx, int cy)
    {
        // Cadeiras aparecendo por baixo do guarda-sol
        Color32 cadeira = new Color32(250, 250, 250, 255);
        Ret(cx - 9, cy - 1, 3, 3, cadeira); Ret(cx + 7, cy - 1, 3, 3, cadeira);
        Ret(cx - 1, cy - 9, 3, 3, cadeira); Ret(cx - 1, cy + 7, 3, 3, cadeira);
        Circulo(cx + 2, cy + 2, 7f, new Color32(0, 0, 0, 70));
        Color32 a = Chance(0.5f) ? new Color32(230, 60, 60, 255) : new Color32(40, 140, 210, 255);
        for (int y = cy - 8; y <= cy + 8; y++)
            for (int x = cx - 8; x <= cx + 8; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float d = dx * dx + dy * dy;
                if (d > 56f) continue;
                float ang = Mathf.Atan2(dy, dx);
                int gomo = Mathf.FloorToInt((ang + Mathf.PI) / (Mathf.PI / 4f));
                Color32 c = gomo % 2 == 0 ? a : new Color32(250, 250, 244, 255);
                if (d > 44f) c = Escurecer(c, 0.75f);
                t.P(x, y, c);
            }
        t.P(cx, cy, new Color32(90, 70, 50, 255));
    }

    private static void CamaElastica(int cx, int cy)
    {
        Circulo(cx + 2, cy + 2, 10f, new Color32(0, 0, 0, 60));
        Circulo(cx, cy, 10f, K);
        Circulo(cx, cy, 9f, new Color32(40, 120, 210, 255));       // proteção azul
        Circulo(cx, cy, 6.5f, new Color32(30, 30, 38, 255));       // lona
        Circulo(cx - 2, cy - 2, 2f, new Color32(60, 60, 72, 255));
    }

    private static void CasinhaCachorro(int x, int y)
    {
        Ret(x + 2, y + 2, 11, 10, new Color32(0, 0, 0, 60), true);
        // Telhadinho de duas águas e a portinha
        for (int yy = 0; yy < 7; yy++) Ret(x, y + yy, 11, 1, yy < 3 ? new Color32(170, 50, 40, 255) : new Color32(210, 80, 60, 255));
        Ret(x, y + 3, 11, 1, new Color32(110, 30, 26, 255));
        Ret(x, y + 7, 11, 4, new Color32(196, 150, 96, 255));
        Ret(x + 4, y + 8, 3, 3, new Color32(50, 30, 20, 255));
        Borda(x, y, 11, 11, K);
        // Cachorro deitado e a tigela
        Color32 pelo = Chance(0.5f) ? new Color32(190, 130, 70, 255) : new Color32(60, 50, 46, 255);
        Ret(x + 13, y + 6, 6, 4, pelo);
        Ret(x + 17, y + 5, 3, 3, pelo);
        t.P(x + 19, y + 6, K);
        Ret(x + 12, y + 8, 2, 1, pelo);   // rabo
        Ret(x + 14, y + 11, 3, 2, new Color32(220, 60, 60, 255));
    }

    // Arbusto redondo e cheio (com flores às vezes)
    private static void ArbustoCheio(int cx, int cy, float r)
    {
        Circulo(cx + 1.5f, cy + 1.5f, r, new Color32(0, 0, 0, 60));
        Circulo(cx, cy, r, new Color32(26, 80, 36, 255));
        Circulo(cx - 0.3f, cy - 0.3f, r - 1f, new Color32(48, 124, 56, 255));
        Circulo(cx - r * 0.3f, cy - r * 0.3f, r * 0.45f, new Color32(84, 166, 76, 255));
        if (Chance(0.4f))
        {
            Color32 flor = Sorteio(new[] { new Color32(255, 110, 160, 255), new Color32(255, 230, 80, 255), new Color32(250, 250, 250, 255) });
            t.P(cx + 1, cy - 1, flor); t.P(cx - 2, cy + 1, flor); t.P(cx + 2, cy + 2, flor);
        }
    }

    private static void Bebedouro(int cx, int cy)
    {
        Circulo(cx + 1, cy + 1, 4.5f, new Color32(0, 0, 0, 60));
        Circulo(cx, cy, 4.5f, new Color32(170, 170, 176, 255));
        Circulo(cx, cy, 3f, new Color32(80, 170, 230, 255));
        t.P(cx - 1, cy - 1, new Color32(220, 250, 255, 255));
        t.P(cx + 2, cy, new Color32(120, 80, 60, 255));   // passarinho
    }

    private static void Banco(int x, int y)
    {
        Ret(x + 1, y + 1, 12, 4, new Color32(0, 0, 0, 60), true);
        Ret(x, y, 12, 4, new Color32(160, 108, 64, 255));
        Ret(x, y + 1, 12, 1, new Color32(120, 78, 46, 255));
        Ret(x, y + 3, 12, 1, new Color32(120, 78, 46, 255));
        Borda(x, y, 12, 4, K);
    }

    private static void CaixaCorreio(int x, int y)
    {
        Ret(x + 2, y + 3, 2, 5, new Color32(100, 64, 40, 255));
        Color32 c = Sorteio(new[] { new Color32(70, 110, 170, 255), new Color32(200, 50, 50, 255), new Color32(60, 60, 66, 255) });
        Ret(x, y, 6, 4, c);
        Borda(x, y, 6, 4, K);
        t.P(x + 5, y - 1, new Color32(220, 40, 50, 255));
    }

    private static void Lixeiras(int x, int y)
    {
        Ret(x, y, 3, 4, new Color32(40, 120, 60, 255));
        Ret(x + 3, y, 3, 4, new Color32(70, 90, 160, 255));
        Borda(x, y, 3, 4, K);
        Borda(x + 3, y, 3, 4, K);
    }

    // Carro parado visto de cima (frente para cima ou para baixo)
    private static void CarroEstacionado(int cx, int y, Color32 cor, bool frenteParaCima)
    {
        const int w = 14, h = 30;
        int x = cx - w / 2;
        Ret(x + 2, y + 2, w, h, new Color32(0, 0, 0, 80), true);
        Color32 escuro = Escurecer(cor, 0.7f), claro = Clarear(cor, 40);
        Color32 vidro = new Color32(40, 60, 90, 255), vidroBrilho = new Color32(120, 170, 220, 255);
        for (int yy = 0; yy < h; yy++)
            for (int xx = 0; xx < w; xx++)
            {
                // cantos arredondados
                bool canto = (xx == 0 || xx == w - 1) && (yy <= 1 || yy >= h - 2);
                if (canto) continue;
                int r = frenteParaCima ? yy : h - 1 - yy;   // 0 = frente
                Color32 c = cor;
                if (r <= 1) c = escuro;                                  // para-choque
                else if (r <= 7) c = claro;                              // capô
                else if (r <= 10) c = (xx > 1 && xx < w - 2) ? vidro : escuro;   // para-brisa
                else if (r <= 20) c = (xx > 1 && xx < w - 2) ? cor : escuro;     // teto
                else if (r <= 23) c = (xx > 1 && xx < w - 2) ? vidro : escuro;   // vidro de trás
                else if (r >= h - 2) c = escuro;
                if (r == 9 && xx == 3) c = vidroBrilho;
                if (xx == 0 || xx == w - 1) c = K;
                t.P(x + xx, y + yy, c);
            }
        // Faróis e lanternas
        int yf = frenteParaCima ? y + 1 : y + h - 2;
        int yt = frenteParaCima ? y + h - 2 : y + 1;
        Ret(x + 2, yf, 2, 1, new Color32(255, 240, 160, 255)); Ret(x + w - 4, yf, 2, 1, new Color32(255, 240, 160, 255));
        Ret(x + 2, yt, 2, 1, new Color32(220, 30, 40, 255)); Ret(x + w - 4, yt, 2, 1, new Color32(220, 30, 40, 255));
        // Retrovisores
        int yr = frenteParaCima ? y + 9 : y + h - 10;
        t.P(x - 1, yr, escuro); t.P(x + w, yr, escuro);
    }

    // ======================================================================
    // Cercas
    // ======================================================================

    // estilo: 0 madeira, 1 muro baixo, 2 cerca viva. Na frente ficam o portãozinho e a entrada do carro.
    private static void CercaLote(int ox, int Lw, int H, bool frenteEmBaixo, int estilo, bool primeiro, bool ultimo,
                                  int portaoX, int entradaX0, int entradaX1)
    {
        int yFrente = frenteEmBaixo ? H - 2 : 0;
        int yFundo = frenteEmBaixo ? 0 : H - 2;

        for (int x = ox; x < ox + Lw; x++)
        {
            bool portao = x >= portaoX && x < portaoX + 16;
            bool entrada = entradaX0 >= 0 && x >= entradaX0 - 1 && x <= entradaX1;
            PedacoCerca(estilo, x, yFundo, true);
            if (entrada) continue;
            if (portao) { PortaozinhoPixel(estilo, x, yFrente, x - portaoX); continue; }
            PedacoCerca(estilo, x, yFrente, true);
        }
        for (int y = 0; y < H; y++)
        {
            if (primeiro) PedacoCerca(estilo, ox, y, false);
            PedacoCerca(estilo, ox + Lw - 2, y, false);   // divisa com o vizinho (ou o fim do terreno)
        }

        // Postes / pilares
        if (estilo != 2)
        {
            for (int x = ox; x < ox + Lw - 2; x += 10)
            {
                Pilar(estilo, x, yFundo);
                bool livre = !(x >= portaoX - 2 && x < portaoX + 16) && !(entradaX0 >= 0 && x >= entradaX0 - 3 && x <= entradaX1);
                if (livre) Pilar(estilo, x, frenteEmBaixo ? H - 3 : 0);
            }
            for (int y = 0; y < H - 2; y += 10)
            {
                if (primeiro) Pilar(estilo, ox, y);
                Pilar(estilo, ox + Lw - 3, y);
            }
            Pilar(estilo, portaoX - 2, frenteEmBaixo ? H - 3 : 0);
            Pilar(estilo, portaoX + 16, frenteEmBaixo ? H - 3 : 0);
            if (entradaX0 >= 0)
            {
                Pilar(estilo, entradaX0 - 4, frenteEmBaixo ? H - 3 : 0);
                Pilar(estilo, entradaX1 + 1, frenteEmBaixo ? H - 3 : 0);
            }
        }
    }

    private static void PedacoCerca(int estilo, int x, int y, bool deitado)
    {
        int w = deitado ? 1 : 2, h = deitado ? 2 : 1;
        switch (estilo)
        {
            case 1:   // muro baixo de blocos com capa clara
                Ret(x, y, w, h, (x + y) % 6 == 0 ? new Color32(170, 166, 160, 255) : new Color32(204, 200, 192, 255));
                if (deitado) t.P(x, y, new Color32(232, 230, 224, 255));
                else t.P(x, y, new Color32(232, 230, 224, 255));
                break;
            case 2:   // cerca viva
                Color32 c = Hash(x, y) % 3 == 0 ? new Color32(40, 110, 48, 255) : new Color32(30, 90, 40, 255);
                if (Hash(x, y) % 7 == 0) c = new Color32(80, 160, 70, 255);
                Ret(x, y, w + (deitado ? 0 : 1), h + (deitado ? 1 : 0), c);
                break;
            default:   // madeira
                Ret(x, y, w, h, new Color32(110, 70, 42, 255));
                break;
        }
    }

    private static void Pilar(int estilo, int x, int y)
    {
        if (estilo == 1)
        {
            Ret(x, y, 3, 3, new Color32(150, 146, 140, 255));
            t.P(x, y, new Color32(236, 234, 228, 255));
        }
        else Poste(x, y);
    }

    private static void PortaozinhoPixel(int estilo, int x, int y, int i)
    {
        Color32 c = estilo == 1 ? new Color32(60, 60, 70, 255) : new Color32(156, 108, 66, 255);
        Ret(x, y, 1, 2, i % 3 == 0 ? (estilo == 1 ? new Color32(30, 30, 36, 255) : new Color32(90, 56, 34, 255)) : c);
    }
}
