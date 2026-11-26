using System.Collections.Generic;
using UnityEngine;

// Desenhos da autoescola "Freio de Mão" (o pátio de treino) e dos blocos do menu principal.
// O pátio é UMA imagem grande (160 x 120 unidades, 8 pixels por unidade) com as aulas pintadas no chão.
// As posições abaixo (em unidades do mundo) também são usadas por MontarHistoria para colocar
// cones, colisões e pontos das aulas: mude aqui e tudo acompanha.
public static class AutoescolaArte
{
    public const string Pasta = "Assets/Imagens/Historia/Autoescola";
    public const int PPU = 8;
    public const int Largura = 160, Altura = 120;

    // ---------- Planta do pátio (unidades do mundo; x para a direita, y para cima) ----------
    public static readonly Rect Asfalto = Rect.MinMaxRect(6, 6, 154, 114);
    public static readonly Rect Predio = Rect.MinMaxRect(8, 96, 52, 114);          // telhado + fachada (colisão)
    public static readonly Rect Calcada = Rect.MinMaxRect(8, 93, 52, 96);
    public const float VagasY0 = 82, VagasY1 = 91, VagasX0 = 10, VagaLargura = 6;
    public const int Vagas = 7, VagaFinal = 3;                                     // a 4ª vaga é a do final
    public static readonly Rect Reta = Rect.MinMaxRect(10, 10, 20, 78);              // aula 1
    public const float PareY = 23f;                                                  // linha de PARE da reta
    public static readonly Rect Circuito = Rect.MinMaxRect(28, 8, 102, 62);         // aula 2 (borda de fora)
    public const float RaioCircuito = 10f, LarguraPista = 12f, RaioIlha = 4f;
    public static Rect Ilha => Rect.MinMaxRect(Circuito.xMin + LarguraPista, Circuito.yMin + LarguraPista,
                                               Circuito.xMax - LarguraPista, Circuito.yMax - LarguraPista);
    public static readonly Rect Slalom = Rect.MinMaxRect(110, 12, 126, 76);          // aula 3
    public static readonly float[] SlalomCones = { 26, 38, 50, 62 };
    public static readonly Rect Garagem = Rect.MinMaxRect(140, 95.5f, 146, 104.5f);  // aula 4 (entrada embaixo)
    public static readonly Rect CalcadaBaliza = Rect.MinMaxRect(58, 108, 122, 114);
    public static readonly Rect Baliza = Rect.MinMaxRect(85, 103.5f, 95, 107.5f);     // aula 5
    public static readonly Vector2 Inicio = new Vector2(15f, 72f);                   // carro começa virado para baixo

    public static Rect VagaRect(int i) => Rect.MinMaxRect(VagasX0 + i * VagaLargura, VagasY0,
                                                          VagasX0 + (i + 1) * VagaLargura, VagasY1);

    // ---------- Cores ----------
    private static readonly Color32 K = PixelArt.Contorno;
    private static readonly Color32 Branco = new Color32(236, 236, 236, 255);
    private static readonly Color32 Amarelo = new Color32(242, 200, 40, 255);
    private static readonly Color32 Vermelho = new Color32(214, 44, 52, 255);
    private static readonly Color32 Magenta = PixelArt.Magenta;

    private static HistoriaArte.Tela t;

    // ======================================================================
    // O pátio
    // ======================================================================

    public static Sprite DesenharPatio()
    {
        t = new HistoriaArte.Tela(Largura * PPU, Altura * PPU);

        Grama(new Rect(0, 0, Largura, Altura));
        Asfaltar(Asfalto);
        MeioFio(Asfalto);
        Cerca(Asfalto);
        Arvores();

        // Prédio da autoescola
        Calcadao(Calcada);
        PredioAutoescola();

        // Estacionamento
        for (int i = 0; i <= Vagas; i++)
            LinhaV(VagasX0 + i * VagaLargura, VagasY0, VagasY1, 0.35f, Branco);
        LinhaH(VagasX0, VagasX0 + Vagas * VagaLargura, VagasY0, 0.35f, Branco);
        Rect vaga = VagaRect(VagaFinal);
        Contorno(Encolher(vaga, 0.15f), 0.3f, Amarelo);
        Escrever("VAGA", vaga.center.x, vaga.center.y, 2, Amarelo);

        // Aula 1: reta e PARE
        LinhaV(Reta.xMin, Reta.yMin, Reta.yMax, 0.4f, Branco);
        LinhaV(Reta.xMax, Reta.yMin, Reta.yMax, 0.4f, Branco);
        R(Reta.xMin, PareY - 0.6f, Reta.xMax, PareY + 0.6f, Branco);
        Escrever("PARE", Reta.center.x, PareY + 3f, 2, Branco);
        Escrever("1 RETA", Reta.center.x, 64f, 2, Amarelo);
        for (float y = 32; y < 60; y += 8) Seta(Reta.center.x, y, 0, -1, Branco);

        // Aula 2: circuito em volta da ilha
        Circuito2();

        // Aula 3: slalom
        LinhaV(Slalom.xMin, Slalom.yMin, Slalom.yMax, 0.4f, Branco);
        LinhaV(Slalom.xMax, Slalom.yMin, Slalom.yMax, 0.4f, Branco);
        Quadriculado(Slalom.xMin, Slalom.yMin + 2f, Slalom.xMax, Slalom.yMin + 3f);
        Quadriculado(Slalom.xMin, Slalom.yMax - 3f, Slalom.xMax, Slalom.yMax - 2f);
        Escrever("3 SLALOM", Slalom.center.x, 8.6f, 2, Amarelo);
        foreach (float y in SlalomCones) Circulo(Slalom.center.x + (Lado(y) * 2.5f), y, 0.9f, new Color32(40, 40, 40, 255));

        // Aula 4: garagem de ré
        Contorno(Garagem, 0.3f, Branco);
        R(Garagem.xMin, Garagem.yMin - 0.2f, Garagem.xMax, Garagem.yMin + 0.2f, new Color32(56, 56, 56, 255)); // entrada aberta
        Escrever("4 GARAGEM", Garagem.center.x, Garagem.yMin - 3.5f, 2, Amarelo);
        Escrever("DE RÉ", Garagem.center.x, Garagem.yMin - 6.5f, 2, Branco);
        Seta(Garagem.center.x, Garagem.center.y, 0, 1, new Color32(255, 255, 255, 140));

        // Aula 5: baliza
        Calcadao(CalcadaBaliza);
        R(CalcadaBaliza.xMin, CalcadaBaliza.yMin - 0.4f, CalcadaBaliza.xMax, CalcadaBaliza.yMin, new Color32(200, 200, 200, 255));
        Contorno(Baliza, 0.3f, Amarelo);
        Escrever("5 BALIZA", Baliza.center.x, Baliza.yMin - 3.2f, 2, Amarelo);
        for (float x = CalcadaBaliza.xMin + 4; x < CalcadaBaliza.xMax - 2; x += 12) Poste(x, 111f);

        // Detalhes: manchas de óleo, marcas de pneu e pilha de cones
        Mancha(60, 72, 2.2f); Mancha(104, 92, 1.6f); Mancha(24, 40, 1.4f); Mancha(132, 30, 2f); Mancha(70, 96, 1.2f);
        MarcasPneu();
        PilhaCones(55, 101);

        return HistoriaArte.SalvarComPpu(t, Pasta, "PatioAutoescola", PPU, Vector2.zero);
    }

    private static int Lado(float y)
    {
        int i = System.Array.IndexOf(SlalomCones, y);
        return i % 2 == 0 ? -1 : 1;
    }

    // Posição dos cones do slalom (no meio, em zigue-zague)
    public static Vector2 ConeSlalom(int i) => new Vector2(Slalom.center.x + (i % 2 == 0 ? -2.5f : 2.5f), SlalomCones[i]);

    private static void Circuito2()
    {
        Rect fora = Circuito;
        Rect ilha = Ilha;

        // Linha branca de fora
        RetArredondado(fora, RaioCircuito, Branco);
        RetArredondado(Encolher(fora, 0.4f), RaioCircuito - 0.4f, new Color32(56, 56, 56, 255), true);

        // Faixa amarela tracejada no meio da pista (nas retas)
        float meio = LarguraPista * 0.5f;
        for (float x = fora.xMin + RaioCircuito + 2; x < fora.xMax - RaioCircuito - 2; x += 4)
        {
            R(x, fora.yMin + meio - 0.2f, x + 2, fora.yMin + meio + 0.2f, Amarelo);
            R(x, fora.yMax - meio - 0.2f, x + 2, fora.yMax - meio + 0.2f, Amarelo);
        }
        for (float y = fora.yMin + RaioCircuito + 2; y < fora.yMax - RaioCircuito - 2; y += 4)
        {
            R(fora.xMin + meio - 0.2f, y, fora.xMin + meio + 0.2f, y + 2, Amarelo);
            R(fora.xMax - meio - 0.2f, y, fora.xMax - meio + 0.2f, y + 2, Amarelo);
        }

        // Setas do sentido (anti-horário)
        Seta(fora.center.x - 14, fora.yMin + meio, 1, 0, Branco);
        Seta(fora.center.x + 14, fora.yMin + meio, 1, 0, Branco);
        Seta(fora.xMax - meio, fora.center.y, 0, 1, Branco);
        Seta(fora.center.x, fora.yMax - meio, -1, 0, Branco);
        Seta(fora.xMin + meio, fora.center.y, 0, -1, Branco);

        // Ilha: zebra vermelha e branca (como nas pistas de corrida), grama e um volante gigante
        RetArredondadoZebra(ilha, RaioIlha);
        Rect grama = Encolher(ilha, 1.2f);
        for (int py = Py(grama.yMax); py < Py(grama.yMin); py++)
            for (int px = Px(grama.xMin); px < Px(grama.xMax); px++)
                if (DentroArredondado(px, py, grama, RaioIlha - 1.2f)) t.P(px, py, CorGrama(px, py));

        Vector2 c = ilha.center;
        Anel(c.x, c.y, 8.5f, 6.8f, new Color32(250, 250, 250, 255));
        Circulo(c.x, c.y, 2.2f, new Color32(250, 250, 250, 255));
        R(c.x - 7, c.y - 0.7f, c.x + 7, c.y + 0.7f, new Color32(250, 250, 250, 255));
        R(c.x - 0.7f, c.y - 7, c.x + 0.7f, c.y, new Color32(250, 250, 250, 255));
        Circulo(c.x, c.y, 1.2f, Magenta);
        Escrever("2 CURVAS", c.x, ilha.yMin + 2.6f, 2, Branco);
        Arvore(ilha.xMin + 5, ilha.yMax - 5, 2.6f);
        Arvore(ilha.xMax - 5, ilha.yMax - 5, 2.4f);
        Arvore(ilha.xMax - 5, ilha.yMin + 5, 2.2f);
        Arvore(ilha.xMin + 5, ilha.yMin + 5, 2.5f);
    }

    private static void PredioAutoescola()
    {
        Rect p = Predio;
        // Fachada (parede da frente com porta e janelas)
        Rect fachada = Rect.MinMaxRect(p.xMin, p.yMin, p.xMax, p.yMin + 3f);
        R(fachada.xMin, fachada.yMin, fachada.xMax, fachada.yMax, new Color32(246, 232, 200, 255));
        R(fachada.xMin, fachada.yMin, fachada.xMax, fachada.yMin + 0.5f, new Color32(190, 170, 140, 255));
        for (float x = p.xMin + 3; x < p.xMax - 3; x += 7)
        {
            bool porta = Mathf.Abs(x + 2.5f - p.center.x) < 3.5f;
            if (porta)
            {
                R(x, fachada.yMin + 0.5f, x + 5, fachada.yMax - 0.4f, new Color32(130, 80, 50, 255));
                R(x + 2.4f, fachada.yMin + 0.5f, x + 2.6f, fachada.yMax - 0.4f, K);
            }
            else
            {
                R(x, fachada.yMin + 1f, x + 5, fachada.yMax - 0.6f, new Color32(140, 200, 245, 255));
                R(x, fachada.yMin + 1f, x + 5, fachada.yMin + 1.3f, Branco);
                R(x + 0.5f, fachada.yMax - 1.2f, x + 1.5f, fachada.yMax - 0.9f, Branco);
            }
        }

        // Telhado de telhas
        Rect telhado = Rect.MinMaxRect(p.xMin, fachada.yMax, p.xMax, p.yMax);
        for (int py = Py(telhado.yMax); py < Py(telhado.yMin); py++)
            for (int px = Px(telhado.xMin); px < Px(telhado.xMax); px++)
            {
                int linha = (py - Py(telhado.yMax)) % 6;
                int desloc = ((py - Py(telhado.yMax)) / 6) % 2 * 4;
                Color32 cor = new Color32(196, 92, 60, 255);
                if (linha == 5) cor = new Color32(140, 58, 40, 255);
                else if (linha == 0) cor = new Color32(222, 124, 86, 255);
                else if ((px + desloc) % 8 == 0) cor = new Color32(160, 70, 48, 255);
                t.P(px, py, cor);
            }
        R(telhado.xMin, telhado.yMin, telhado.xMax, telhado.yMin + 0.6f, new Color32(110, 44, 30, 255));
        Contorno(Rect.MinMaxRect(p.xMin, p.yMin, p.xMax, p.yMax), 0.25f, K);

        // Placa da autoescola
        Rect placa = Rect.MinMaxRect(p.xMin + 6, telhado.yMin + 3.5f, p.xMax - 6, telhado.yMin + 12f);
        R(placa.xMin - 0.4f, placa.yMin - 0.4f, placa.xMax + 0.4f, placa.yMax + 0.4f, K);
        R(placa.xMin, placa.yMin, placa.xMax, placa.yMax, new Color32(40, 14, 60, 255));
        Contorno(placa, 0.5f, Magenta);
        Escrever("AUTOESCOLA", placa.center.x, placa.yMax - 2.6f, 3, Branco);
        Escrever("FREIO DE MÃO", placa.center.x, placa.yMin + 2.4f, 2, new Color32(120, 236, 255, 255));
        // Volantinho dos lados da placa
        Anel(placa.xMin + 2.5f, placa.center.y, 1.6f, 1.0f, Amarelo);
        Anel(placa.xMax - 2.5f, placa.center.y, 1.6f, 1.0f, Amarelo);

        // Vasos de flores na calçada
        for (float x = Calcada.xMin + 2; x < Calcada.xMax - 1; x += 9)
        {
            if (Mathf.Abs(x - p.center.x) < 4) continue;
            R(x - 1, Calcada.yMin + 0.6f, x + 1, Calcada.yMax - 0.6f, new Color32(170, 90, 60, 255));
            Circulo(x - 0.4f, Calcada.center.y + 0.3f, 0.5f, new Color32(240, 80, 120, 255));
            Circulo(x + 0.5f, Calcada.center.y - 0.2f, 0.45f, new Color32(255, 220, 60, 255));
        }
    }

    // ======================================================================
    // Cone (visto de cima)
    // ======================================================================

    public static Sprite DesenharCone()
    {
        const int n = 11;
        HistoriaArte.Tela c = new HistoriaArte.Tela(n, n);
        Color32 laranja = new Color32(255, 120, 20, 255), escuro = new Color32(200, 70, 10, 255);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int camada = PixelArt.Camada(x, y, n, n, 2);
                if (camada < 0) continue;
                c.P(x, y, camada == 0 ? K : y >= n - 3 ? escuro : laranja);
            }
        float m = (n - 1) / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - m) * (x - m) + (y - m) * (y - m));
                if (d <= 3.2f && d > 2.0f) c.P(x, y, Branco);
                else if (d <= 2.0f) c.P(x, y, laranja);
            }
        c.P(4, 4, new Color32(255, 200, 150, 255));
        return HistoriaArte.SalvarComPpu(c, Pasta, "Cone", 10f, new Vector2(0.5f, 0.5f));
    }

    // ======================================================================
    // Menu em blocos
    // ======================================================================

    // Fundo do bloco: branco (a cor vem do Image), com brilho em cima e sombra embaixo
    public static Sprite FundoBloco()
    {
        const int n = 24;
        HistoriaArte.Tela c = new HistoriaArte.Tela(n, n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int camada = PixelArt.Camada(x, y, n, n, 2);
                if (camada < 0) continue;
                Color32 cor = new Color32(255, 255, 255, 235);
                if (camada == 0) cor = new Color32(20, 6, 30, 255);
                else if (y <= 2) cor = new Color32(255, 255, 255, 255);
                else if (y >= n - 4) cor = new Color32(170, 170, 180, 240);
                c.P(x, y, cor);
            }
        return PixelArt.Salvar(Pasta, "BlocoFundo", c.p, n, n, new Vector4(8, 8, 8, 8));
    }

    public static Sprite DestaqueBloco()
    {
        const int n = 24;
        HistoriaArte.Tela c = new HistoriaArte.Tela(n, n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int camada = PixelArt.Camada(x, y, n, n, 3);
                if (camada >= 0 && camada <= 1) c.P(x, y, new Color32(255, 255, 255, 255));
                else if (camada == 2) c.P(x, y, new Color32(120, 236, 255, 255));
            }
        return PixelArt.Salvar(Pasta, "BlocoDestaque", c.p, n, n, new Vector4(8, 8, 8, 8));
    }

    // Ícones 32x32 desenhados com formas e depois contornados
    public static Sprite Icone(string nome)
    {
        const int n = 32;
        bool[,] w = new bool[n, n];   // branco
        bool[,] a = new bool[n, n];   // cor de destaque (amarelo)
        float m = 15.5f;

        switch (nome)
        {
            case "Jogar":   // volante
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Mathf.Sqrt((x - m) * (x - m) + (y - m) * (y - m));
                        if (d <= 13.5f && d >= 9.5f) w[x, y] = true;
                        if (d <= 3.6f) w[x, y] = true;
                        if (Mathf.Abs(y - m) <= 1.6f && d < 10f) w[x, y] = true;
                        if (Mathf.Abs(x - m) <= 1.6f && y > m && d < 10f) w[x, y] = true;
                        if (d <= 1.6f) a[x, y] = true;
                    }
                break;
            case "Tutorial":   // cone
                for (int y = 3; y < 26; y++)
                {
                    float meia = 2f + (y - 3) * 0.42f;
                    for (int x = 0; x < n; x++)
                        if (Mathf.Abs(x - m) <= meia)
                        {
                            bool faixa = (y >= 10 && y <= 12) || (y >= 18 && y <= 20);
                            if (faixa) w[x, y] = true; else a[x, y] = true;
                        }
                }
                for (int y = 26; y < 30; y++) for (int x = 3; x < 29; x++) a[x, y] = true;
                break;
            case "Loja":   // sacola com moeda
                for (int y = 11; y < 29; y++) for (int x = 5; x < 25; x++) w[x, y] = true;
                for (int y = 4; y < 12; y++)
                    for (int x = 8; x < 22; x++)
                    {
                        float d = Mathf.Sqrt((x - 15f) * (x - 15f) + (y - 11f) * (y - 11f));
                        if (d >= 4.5f && d <= 6.5f && y < 11) w[x, y] = true;
                    }
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                        if ((x - 23f) * (x - 23f) + (y - 23f) * (y - 23f) <= 42f) { a[x, y] = true; w[x, y] = false; }
                for (int y = 19; y < 28; y++) w[23, y] = true;
                break;
            case "Configuracoes":   // engrenagem
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = x - m, dy = y - m;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float ang = Mathf.Atan2(dy, dx);
                        bool dente = Mathf.Cos(ang * 8f) > 0.35f && d <= 14f;
                        if ((d <= 10.5f || dente) && d >= 4.5f) w[x, y] = true;
                        if (d < 4.5f && d >= 2.5f) a[x, y] = true;
                    }
                break;
            case "Sobre":   // balão com "i"
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                        if ((x - m) * (x - m) + (y - m) * (y - m) <= 182f) w[x, y] = true;
                for (int y = 7; y < 11; y++) for (int x = 14; x < 18; x++) { w[x, y] = false; a[x, y] = true; }
                for (int y = 13; y < 25; y++) for (int x = 14; x < 18; x++) { w[x, y] = false; a[x, y] = true; }
                break;
            case "Niveis":   // bandeira quadriculada
                for (int y = 3; y < 30; y++) for (int x = 5; x < 8; x++) a[x, y] = true;
                for (int y = 4; y < 20; y++)
                    for (int x = 8; x < 29; x++)
                    {
                        int onda = Mathf.RoundToInt(Mathf.Sin(x * 0.35f) * 1.5f);
                        int yy = y + onda;
                        if (yy < 0 || yy >= n) continue;
                        if (((x - 8) / 4 + (y - 4) / 4) % 2 == 0) w[x, yy] = true;
                    }
                break;
            case "Sair":   // porta com seta
                for (int y = 3; y < 29; y++) for (int x = 4; x < 17; x++) w[x, y] = true;
                for (int y = 5; y < 27; y++) for (int x = 6; x < 15; x++) w[x, y] = false;
                for (int y = 15; y < 17; y++) for (int x = 13; x < 25; x++) a[x, y] = true;
                for (int i = 0; i < 6; i++) { a[24 - i, 15 - i] = true; a[24 - i, 16 + i] = true; a[23 - i, 15 - i] = true; a[23 - i, 16 + i] = true; }
                break;
        }

        HistoriaArte.Tela c = new HistoriaArte.Tela(n, n);
        Color32 branco = new Color32(255, 255, 255, 255), amarelo = new Color32(255, 214, 60, 255);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                if (w[x, y]) c.P(x, y, branco);
                else if (a[x, y]) c.P(x, y, amarelo);
                else if (Vizinho(w, x, y) || Vizinho(a, x, y)) c.P(x, y, new Color32(20, 6, 30, 255));
            }
        return PixelArt.Salvar(Pasta, "Icone" + nome, c.p, n, n);
    }

    private static bool Vizinho(bool[,] m, int x, int y)
    {
        int n = m.GetLength(0);
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int xx = x + dx, yy = y + dy;
                if (xx >= 0 && yy >= 0 && xx < n && yy < n && m[xx, yy]) return true;
            }
        return false;
    }

    // ======================================================================
    // Fonte (5 linhas; acentos ficam acima, cedilha abaixo)
    // ======================================================================

    private static readonly Dictionary<char, string[]> Fonte = new Dictionary<char, string[]>
    {
        { 'A', new[] { ".XX.", "X..X", "XXXX", "X..X", "X..X" } }, { 'B', new[] { "XXX.", "X..X", "XXX.", "X..X", "XXX." } },
        { 'C', new[] { ".XXX", "X...", "X...", "X...", ".XXX" } }, { 'D', new[] { "XXX.", "X..X", "X..X", "X..X", "XXX." } },
        { 'E', new[] { "XXXX", "X...", "XXX.", "X...", "XXXX" } }, { 'F', new[] { "XXXX", "X...", "XXX.", "X...", "X..." } },
        { 'G', new[] { ".XXX", "X...", "X.XX", "X..X", ".XXX" } }, { 'H', new[] { "X..X", "X..X", "XXXX", "X..X", "X..X" } },
        { 'I', new[] { "XXX", ".X.", ".X.", ".X.", "XXX" } },      { 'J', new[] { "..XX", "...X", "...X", "X..X", ".XX." } },
        { 'K', new[] { "X..X", "X.X.", "XX..", "X.X.", "X..X" } }, { 'L', new[] { "X...", "X...", "X...", "X...", "XXXX" } },
        { 'M', new[] { "X...X", "XX.XX", "X.X.X", "X...X", "X...X" } }, { 'N', new[] { "X..X", "XX.X", "X.XX", "X..X", "X..X" } },
        { 'O', new[] { ".XX.", "X..X", "X..X", "X..X", ".XX." } }, { 'P', new[] { "XXX.", "X..X", "XXX.", "X...", "X..." } },
        { 'Q', new[] { ".XX.", "X..X", "X..X", "X.XX", ".XXX" } }, { 'R', new[] { "XXX.", "X..X", "XXX.", "X.X.", "X..X" } },
        { 'S', new[] { ".XXX", "X...", ".XX.", "...X", "XXX." } }, { 'T', new[] { "XXX", ".X.", ".X.", ".X.", ".X." } },
        { 'U', new[] { "X..X", "X..X", "X..X", "X..X", ".XX." } }, { 'V', new[] { "X..X", "X..X", "X..X", ".XX.", ".XX." } },
        { 'W', new[] { "X...X", "X...X", "X.X.X", "XX.XX", "X...X" } }, { 'X', new[] { "X..X", "X..X", ".XX.", "X..X", "X..X" } },
        { 'Y', new[] { "X.X", "X.X", ".X.", ".X.", ".X." } },      { 'Z', new[] { "XXXX", "...X", ".XX.", "X...", "XXXX" } },
        { '0', new[] { "XXX", "X.X", "X.X", "X.X", "XXX" } },      { '1', new[] { ".X.", "XX.", ".X.", ".X.", "XXX" } },
        { '2', new[] { "XXX", "..X", "XXX", "X..", "XXX" } },      { '3', new[] { "XXX", "..X", "XXX", "..X", "XXX" } },
        { '4', new[] { "X.X", "X.X", "XXX", "..X", "..X" } },      { '5', new[] { "XXX", "X..", "XXX", "..X", "XXX" } },
        { '6', new[] { "XXX", "X..", "XXX", "X.X", "XXX" } },      { '7', new[] { "XXX", "..X", ".X.", ".X.", ".X." } },
        { '8', new[] { "XXX", "X.X", "XXX", "X.X", "XXX" } },      { '9', new[] { "XXX", "X.X", "XXX", "..X", "XXX" } },
        { '!', new[] { "X", "X", "X", ".", "X" } },                  { '.', new[] { ".", ".", ".", ".", "X" } },
        { '-', new[] { "...", "...", "XXX", "...", "..." } },      { ':', new[] { ".", "X", ".", "X", "." } },
        { '?', new[] { "XXX", "..X", ".XX", "...", ".X." } },      { ',', new[] { ".", ".", ".", "X", "X" } },
        { '<', new[] { "..X", ".X.", "X..", ".X.", "..X" } },      { '>', new[] { "X..", ".X.", "..X", ".X.", "X.." } },
        { '(', new[] { ".X", "X.", "X.", "X.", ".X" } },          { ')', new[] { "X.", ".X", ".X", ".X", "X." } },
        { '/', new[] { "..X", "..X", ".X.", "X..", "X.." } },      { '+', new[] { "...", ".X.", "XXX", ".X.", "..." } },
        { ' ', new[] { "..", "..", "..", "..", ".." } },
    };

    private static string[] Glifo(char c, out string[] acento, out bool cedilha)
    {
        acento = null;
        cedilha = false;
        switch (c)
        {
            case 'Ã': acento = new[] { ".X.X", "X.X." }; return Fonte['A'];
            case 'Á': acento = new[] { "..X.", ".X.." }; return Fonte['A'];
            case 'É': acento = new[] { "..X.", ".X.." }; return Fonte['E'];
            case 'Í': acento = new[] { ".X.", "X.." }; return Fonte['I'];
            case 'Ó': acento = new[] { "..X.", ".X.." }; return Fonte['O'];
            case 'Ç': cedilha = true; return Fonte['C'];
            case 'Õ': acento = new[] { ".X.X", "X.X." }; return Fonte['O'];
            case 'Â': acento = new[] { ".XX.", "X..X" }; return Fonte['A'];
            case 'Ê': acento = new[] { ".XX.", "X..X" }; return Fonte['E'];
            case 'Ô': acento = new[] { ".XX.", "X..X" }; return Fonte['O'];
            case 'Ú': acento = new[] { "..X.", ".X.." }; return Fonte['U'];
            case 'À': acento = new[] { ".X..", "..X." }; return Fonte['A'];
        }
        return Fonte.TryGetValue(c, out string[] g) ? g : Fonte[' '];
    }

    public static int LarguraTexto(string texto)
    {
        int l = 0;
        foreach (char c in texto) l += Glifo(c, out _, out _)[0].Length + 1;
        return Mathf.Max(0, l - 1);
    }

    // Escreve centralizado em (ux, uy) (unidades do mundo); escala = tamanho de cada pixel da letra
    private static void Escrever(string texto, float ux, float uy, int escala, Color32 cor)
    {
        int largura = LarguraTexto(texto) * escala;
        int x = Px(ux) - largura / 2;
        int y = Py(uy) - 5 * escala / 2;
        foreach (char c in texto)
        {
            string[] g = Glifo(c, out string[] acento, out bool cedilha);
            Bloco(g, x, y, escala, cor);
            if (acento != null) Bloco(acento, x, y - (acento.Length + 1) * escala, escala, cor);
            if (cedilha) Bloco(new[] { "..X." }, x, y + 5 * escala, escala, cor);
            x += (g[0].Length + 1) * escala;
        }
    }

    // Mesma fonte (com acentos) para outros desenhos: escreve centralizado em xCentro, com o topo das letras em yTopo
    public static void EscreverEm(HistoriaArte.Tela tela, string texto, int xCentro, int yTopo, int escala, Color32 cor,
                                  Color32 sombra)
    {
        int x = xCentro - LarguraTexto(texto) * escala / 2;
        foreach (char c in texto)
        {
            string[] g = Glifo(c, out string[] acento, out bool cedilha);
            BlocoEm(tela, g, x, yTopo, escala, cor, sombra);
            if (acento != null) BlocoEm(tela, acento, x, yTopo - (acento.Length + 1) * escala, escala, cor, sombra);
            if (cedilha) BlocoEm(tela, new[] { "..X." }, x, yTopo + 5 * escala, escala, cor, sombra);
            x += (g[0].Length + 1) * escala;
        }
    }

    private static void BlocoEm(HistoriaArte.Tela tela, string[] linhas, int x, int y, int escala, Color32 cor, Color32 sombra)
    {
        for (int ly = 0; ly < linhas.Length; ly++)
            for (int lx = 0; lx < linhas[ly].Length; lx++)
                if (linhas[ly][lx] == 'X')
                {
                    if (sombra.a > 0) tela.Ret(x + lx * escala + 1, y + ly * escala + 1, escala, escala, sombra);
                }
        for (int ly = 0; ly < linhas.Length; ly++)
            for (int lx = 0; lx < linhas[ly].Length; lx++)
                if (linhas[ly][lx] == 'X') tela.Ret(x + lx * escala, y + ly * escala, escala, escala, cor);
    }

    private static void Bloco(string[] linhas, int x, int y, int escala, Color32 cor)
    {
        for (int ly = 0; ly < linhas.Length; ly++)
            for (int lx = 0; lx < linhas[ly].Length; lx++)
                if (linhas[ly][lx] == 'X')
                {
                    t.Ret(x + lx * escala + 1, y + ly * escala + 1, escala, escala, new Color32(0, 0, 0, 90));   // sombrinha
                    t.Ret(x + lx * escala, y + ly * escala, escala, escala, cor);
                }
    }

    // ======================================================================
    // Ajudantes de desenho (coordenadas em unidades do mundo)
    // ======================================================================

    private static int Px(float ux) => Mathf.RoundToInt(ux * PPU);
    private static int Py(float uy) => Mathf.RoundToInt((Altura - uy) * PPU);   // y da tela conta de cima

    private static void R(float x0, float y0, float x1, float y1, Color32 c)
    {
        int px0 = Px(x0), px1 = Px(x1), py0 = Py(y1), py1 = Py(y0);
        for (int py = py0; py < py1; py++)
            for (int px = px0; px < px1; px++) Misturar(px, py, c);
    }

    private static void Misturar(int px, int py, Color32 c)
    {
        if (px < 0 || py < 0 || px >= t.L || py >= t.A) return;
        if (c.a == 255) { t.P(px, py, c); return; }
        int i = (t.A - 1 - py) * t.L + px;
        Color32 b = t.p[i];
        float k = c.a / 255f;
        t.p[i] = new Color32((byte)(b.r + (c.r - b.r) * k), (byte)(b.g + (c.g - b.g) * k), (byte)(b.b + (c.b - b.b) * k), 255);
    }

    private static void LinhaV(float x, float y0, float y1, float esp, Color32 c) => R(x - esp / 2, y0, x + esp / 2, y1, c);
    private static void LinhaH(float x0, float x1, float y, float esp, Color32 c) => R(x0, y - esp / 2, x1, y + esp / 2, c);

    private static void Contorno(Rect r, float esp, Color32 c)
    {
        R(r.xMin, r.yMin, r.xMax, r.yMin + esp, c);
        R(r.xMin, r.yMax - esp, r.xMax, r.yMax, c);
        R(r.xMin, r.yMin, r.xMin + esp, r.yMax, c);
        R(r.xMax - esp, r.yMin, r.xMax, r.yMax, c);
    }

    private static Rect Encolher(Rect r, float m) => Rect.MinMaxRect(r.xMin + m, r.yMin + m, r.xMax - m, r.yMax - m);

    private static void Circulo(float cx, float cy, float raio, Color32 c) => Anel(cx, cy, raio, -1f, c);

    private static void Anel(float cx, float cy, float raioFora, float raioDentro, Color32 c)
    {
        for (int py = Py(cy + raioFora) - 1; py <= Py(cy - raioFora) + 1; py++)
            for (int px = Px(cx - raioFora) - 1; px <= Px(cx + raioFora) + 1; px++)
            {
                float ux = (px + 0.5f) / PPU - cx, uy = (Altura - (py + 0.5f) / PPU) - cy;
                float d = Mathf.Sqrt(ux * ux + uy * uy);
                if (d <= raioFora && d >= raioDentro) Misturar(px, py, c);
            }
    }

    private static bool DentroArredondado(int px, int py, Rect r, float raio)
    {
        float ux = (px + 0.5f) / PPU, uy = Altura - (py + 0.5f) / PPU;
        if (ux < r.xMin || ux > r.xMax || uy < r.yMin || uy > r.yMax) return false;
        float cx = Mathf.Clamp(ux, r.xMin + raio, r.xMax - raio);
        float cy = Mathf.Clamp(uy, r.yMin + raio, r.yMax - raio);
        return (ux - cx) * (ux - cx) + (uy - cy) * (uy - cy) <= raio * raio;
    }

    private static void RetArredondado(Rect r, float raio, Color32 c, bool asfalto = false)
    {
        for (int py = Py(r.yMax); py < Py(r.yMin); py++)
            for (int px = Px(r.xMin); px < Px(r.xMax); px++)
                if (DentroArredondado(px, py, r, raio)) t.P(px, py, asfalto ? CorAsfalto(px, py) : c);
    }

    private static void RetArredondadoZebra(Rect r, float raio)
    {
        for (int py = Py(r.yMax); py < Py(r.yMin); py++)
            for (int px = Px(r.xMin); px < Px(r.xMax); px++)
                if (DentroArredondado(px, py, r, raio))
                    t.P(px, py, ((px + py) / 12) % 2 == 0 ? Vermelho : new Color32(245, 245, 245, 255));
    }

    // Seta pintada no chão apontando para (dx, dy): haste + ponta triangular
    private static void Seta(float cx, float cy, int dx, int dy, Color32 c)
    {
        const float meio = 3f;
        for (int py = Py(cy + meio) - 1; py <= Py(cy - meio) + 1; py++)
            for (int px = Px(cx - meio) - 1; px <= Px(cx + meio) + 1; px++)
            {
                float ux = (px + 0.5f) / PPU - cx, uy = (Altura - (py + 0.5f) / PPU) - cy;
                float ao = ux * dx + uy * dy;          // ao longo da seta
                float lado = Mathf.Abs(-ux * dy + uy * dx);
                bool haste = ao >= -2.6f && ao <= 0.4f && lado <= 0.45f;
                bool ponta = ao > 0.4f && ao <= 2.6f && lado <= (2.6f - ao) * 0.75f;
                if (haste || ponta) Misturar(px, py, c);
            }
    }

    private static void Quadriculado(float x0, float y0, float x1, float y1)
    {
        for (int py = Py(y1); py < Py(y0); py++)
            for (int px = Px(x0); px < Px(x1); px++)
                t.P(px, py, ((px / 4) + (py / 4)) % 2 == 0 ? Branco : new Color32(30, 30, 30, 255));
    }

    private static int Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h ^ (h >> 16)) & 0x7fffffff;
        }
    }

    private static Color32 CorAsfalto(int px, int py)
    {
        int h = Hash(px, py) % 100;
        int v = 54 + (h % 5);
        if (h > 96) v = 66;
        else if (h < 2) v = 46;
        return new Color32((byte)v, (byte)v, (byte)v, 255);
    }

    private static Color32 CorGrama(int px, int py)
    {
        int h = Hash(px, py);
        int mancha = Hash(px / 6, py / 6) % 3;
        int g = 140 + (h % 12) + mancha * 4;
        Color32 c = new Color32((byte)(56 + h % 10), (byte)g, (byte)(54 + h % 8), 255);
        if (h % 97 == 0) c = new Color32(250, 240, 120, 255);   // florzinha
        else if (h % 89 == 0) c = new Color32(90, 176, 80, 255);
        return c;
    }

    private static void Grama(Rect r)
    {
        for (int py = Py(r.yMax); py < Py(r.yMin); py++)
            for (int px = Px(r.xMin); px < Px(r.xMax); px++) t.P(px, py, CorGrama(px, py));
    }

    private static void Asfaltar(Rect r)
    {
        for (int py = Py(r.yMax); py < Py(r.yMin); py++)
            for (int px = Px(r.xMin); px < Px(r.xMax); px++) t.P(px, py, CorAsfalto(px, py));
    }

    private static void MeioFio(Rect r)
    {
        Color32 claro = new Color32(176, 176, 176, 255);
        Contorno(Rect.MinMaxRect(r.xMin - 0.6f, r.yMin - 0.6f, r.xMax + 0.6f, r.yMax + 0.6f), 0.6f, claro);
    }

    private static void Calcadao(Rect r)
    {
        for (int py = Py(r.yMax); py < Py(r.yMin); py++)
            for (int px = Px(r.xMin); px < Px(r.xMax); px++)
            {
                int v = 112 + Hash(px, py) % 6;
                if (px % 16 == 0 || py % 16 == 0) v = 96;
                t.P(px, py, new Color32((byte)v, (byte)v, (byte)v, 255));
            }
    }

    private static void Cerca(Rect r)
    {
        Color32 tela = new Color32(150, 160, 170, 255), poste = new Color32(90, 96, 110, 255);
        Rect f = Rect.MinMaxRect(r.xMin - 1.5f, r.yMin - 1.5f, r.xMax + 1.5f, r.yMax + 1.5f);
        Contorno(f, 0.25f, tela);
        for (float x = f.xMin; x <= f.xMax; x += 3f) { Circulo(x, f.yMin + 0.12f, 0.35f, poste); Circulo(x, f.yMax - 0.12f, 0.35f, poste); }
        for (float y = f.yMin; y <= f.yMax; y += 3f) { Circulo(f.xMin + 0.12f, y, 0.35f, poste); Circulo(f.xMax - 0.12f, y, 0.35f, poste); }
    }

    private static void Arvores()
    {
        for (float x = 3; x < Largura; x += 7.5f)
        {
            Arvore(x + (Hash((int)x, 1) % 3) * 0.4f, 2.6f, 2.2f + Hash((int)x, 2) % 3 * 0.25f);
            Arvore(x + (Hash((int)x, 3) % 3) * 0.4f, Altura - 2.6f, 2.2f + Hash((int)x, 4) % 3 * 0.25f);
        }
        for (float y = 10; y < Altura - 6; y += 7.5f)
        {
            Arvore(2.6f, y, 2.3f);
            Arvore(Largura - 2.6f, y + 3, 2.3f);
        }
    }

    private static void Arvore(float cx, float cy, float r)
    {
        Circulo(cx + 0.6f, cy - 0.6f, r, new Color32(0, 0, 0, 80));
        Circulo(cx, cy, r, new Color32(34, 96, 46, 255));
        Circulo(cx - r * 0.25f, cy + r * 0.2f, r * 0.72f, new Color32(48, 128, 58, 255));
        Circulo(cx - r * 0.4f, cy + r * 0.35f, r * 0.35f, new Color32(84, 170, 80, 255));
        Circulo(cx + r * 0.3f, cy - r * 0.1f, r * 0.25f, new Color32(60, 142, 64, 255));
    }

    private static void Poste(float x, float y)
    {
        Circulo(x + 0.3f, y - 0.3f, 0.7f, new Color32(0, 0, 0, 70));
        Circulo(x, y, 0.6f, new Color32(70, 74, 86, 255));
        Circulo(x, y, 0.3f, new Color32(255, 240, 160, 255));
    }

    private static void Mancha(float cx, float cy, float r)
    {
        Circulo(cx, cy, r, new Color32(30, 28, 36, 45));
        Circulo(cx + r * 0.5f, cy - r * 0.3f, r * 0.55f, new Color32(30, 28, 36, 40));
        Circulo(cx - r * 0.6f, cy + r * 0.2f, r * 0.4f, new Color32(30, 28, 36, 40));
        Circulo(cx - r * 0.2f, cy + r * 0.3f, r * 0.22f, new Color32(110, 80, 150, 50));   // brilho de óleo
    }

    private static void MarcasPneu()
    {
        Color32 c = new Color32(20, 20, 20, 70);
        Rect fora = Circuito;
        Vector2[] cantos =
        {
            new Vector2(fora.xMax - RaioCircuito, fora.yMin + RaioCircuito),
            new Vector2(fora.xMax - RaioCircuito, fora.yMax - RaioCircuito),
            new Vector2(fora.xMin + RaioCircuito, fora.yMax - RaioCircuito),
        };
        float[] inicio = { -90, 0, 90 };
        for (int k = 0; k < cantos.Length; k++)
            for (float a = inicio[k] + 10; a < inicio[k] + 80; a += 1.5f)
            {
                float rad = a * Mathf.Deg2Rad;
                foreach (float r in new[] { 5.2f, 6.6f })
                    Circulo(cantos[k].x + Mathf.Cos(rad) * r, cantos[k].y + Mathf.Sin(rad) * r, 0.18f, c);
            }
    }

    private static void PilhaCones(float cx, float cy)
    {
        Color32 laranja = new Color32(255, 120, 20, 255);
        Vector2[] pos = { new Vector2(0, 0), new Vector2(1.2f, 0.3f), new Vector2(0.5f, 1.2f), new Vector2(-0.8f, 1f), new Vector2(1.6f, 1.6f) };
        foreach (Vector2 p in pos)
        {
            Circulo(cx + p.x + 0.2f, cy + p.y - 0.2f, 0.55f, new Color32(0, 0, 0, 80));
            Circulo(cx + p.x, cy + p.y, 0.5f, laranja);
            Anel(cx + p.x, cy + p.y, 0.32f, 0.2f, Branco);
        }
    }
}
