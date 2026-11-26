using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Desenhos do TURBO RACER 3000 (o videogame "de 1987" do Jason) e da TV do quarto onde ele joga.
// O videogame é de propósito pobre: poucas cores, sem contorno, blocos grandes... mas os carros
// continuam com cara de carro (rodas, para-brisa, faróis, lanternas).
public static partial class HistoriaArte
{
    public const string PastaVideogame = "Assets/Imagens/Videogame";
    public const string CaminhoFontePixel = PastaVideogame + "/FontePixel.asset";

    // Onde fica a telinha no desenho da TV do quarto (pixels, x da esquerda, y de BAIXO para cima)
    public static readonly RectInt TelaDaTV = new RectInt(4, 11, 8, 9);

    private static Sprite SalvarVideogame(Tela t, string nome, Vector4 borda = default) =>
        PixelArt.Salvar(PastaVideogame, nome, t.p, t.L, t.A, borda, 100f, new Vector2(0.5f, 0.5f));

    // ======================================================================
    // Carros do videogame
    // ======================================================================

    public static readonly Color32[] CoresCarroRuim =
    {
        new Color32(220, 40, 40, 255),    // o do jogador (vermelho com faixa branca)
        new Color32(50, 100, 220, 255), new Color32(240, 200, 40, 255), new Color32(40, 170, 80, 255),
        new Color32(150, 70, 200, 255), new Color32(240, 130, 40, 255), new Color32(225, 225, 225, 255),
        new Color32(40, 190, 200, 255),
    };

    public static Sprite[] CarrosRuins()
    {
        Sprite[] r = new Sprite[CoresCarroRuim.Length];
        for (int i = 0; i < r.Length; i++) r[i] = CarroRuim("CarroRuim_" + i, CoresCarroRuim[i], i == 0);
        return r;
    }

    // 14x22, frente para CIMA
    private static Sprite CarroRuim(string nome, Color32 cor, bool faixa)
    {
        Tela t = new Tela(14, 22);
        Color32 pneu = new Color32(20, 20, 24, 255), roda = new Color32(110, 110, 110, 255);
        Color32 vidro = new Color32(40, 50, 90, 255), reflexo = new Color32(150, 200, 255, 255);
        Color32 claro = Mais(cor, 50), escuro = Vezes(cor, 0.72f), lateral = Vezes(cor, 0.85f);

        // Rodas saindo dos lados
        foreach (int y0 in new[] { 3, 14 })
            for (int y = y0; y < y0 + 5; y++)
            {
                t.P(0, y, pneu); t.P(1, y, pneu); t.P(12, y, pneu); t.P(13, y, pneu);
            }
        t.P(0, 5, roda); t.P(13, 5, roda); t.P(0, 16, roda); t.P(13, 16, roda);

        for (int y = 0; y < 22; y++)
            for (int x = 2; x < 12; x++)
            {
                bool canto = (x == 2 || x == 11) && (y == 0 || y == 21);
                if (canto) continue;
                Color32 c = cor;
                if (y <= 1) c = claro;                                   // para-choque da frente
                else if (y >= 20) c = escuro;                            // para-choque de trás
                else if (y >= 5 && y <= 7 && x >= 3 && x <= 10) c = vidro;          // para-brisa
                else if (y >= 8 && y <= 13 && x >= 3 && x <= 10) c = escuro;        // teto
                else if (y >= 14 && y <= 15 && x >= 3 && x <= 10) c = vidro;        // vigia
                if (x == 2 || x == 11) c = lateral;
                if (faixa && (x == 6 || x == 7) && !(y >= 5 && y <= 7) && !(y >= 14 && y <= 15)) c = new Color32(245, 245, 245, 255);
                t.P(x, y, c);
            }
        t.P(4, 5, reflexo); t.P(5, 5, reflexo);
        // Faróis e lanternas
        t.P(3, 0, new Color32(255, 240, 120, 255)); t.P(4, 0, new Color32(255, 240, 120, 255));
        t.P(9, 0, new Color32(255, 240, 120, 255)); t.P(10, 0, new Color32(255, 240, 120, 255));
        t.P(3, 20, new Color32(230, 30, 30, 255)); t.P(4, 20, new Color32(230, 30, 30, 255));
        t.P(9, 20, new Color32(230, 30, 30, 255)); t.P(10, 20, new Color32(230, 30, 30, 255));
        // Retrovisores
        t.P(1, 6, lateral); t.P(12, 6, lateral);
        return SalvarVideogame(t, nome);
    }

    // Caminhão baú de 40x52 (ocupa duas faixas)
    public static Sprite CaminhaoRuim()
    {
        Tela t = new Tela(40, 52);
        Color32 cabine = new Color32(200, 50, 40, 255), cabineEscura = new Color32(150, 30, 26, 255);
        Color32 bau = new Color32(205, 205, 210, 255), bauEscuro = new Color32(160, 160, 168, 255);
        Color32 pneu = new Color32(20, 20, 24, 255), vidro = new Color32(40, 50, 90, 255);

        // Rodas
        for (int y = 8; y < 13; y++) { t.P(5, y, pneu); t.P(6, y, pneu); t.P(33, y, pneu); t.P(34, y, pneu); }
        foreach (int y0 in new[] { 36, 43 })
            for (int y = y0; y < y0 + 5; y++) { t.P(0, y, pneu); t.P(1, y, pneu); t.P(38, y, pneu); t.P(39, y, pneu); }

        // Cabine (na frente, em cima)
        t.Ret(7, 0, 26, 14, cabine);
        t.Ret(7, 0, 26, 2, Mais(cabine, 40));
        t.Ret(9, 3, 22, 4, vidro);
        t.P(10, 3, new Color32(150, 200, 255, 255)); t.P(11, 3, new Color32(150, 200, 255, 255));
        t.Ret(9, 8, 22, 5, cabineEscura);
        t.P(8, 0, new Color32(255, 240, 120, 255)); t.P(9, 0, new Color32(255, 240, 120, 255));
        t.P(30, 0, new Color32(255, 240, 120, 255)); t.P(31, 0, new Color32(255, 240, 120, 255));
        t.Ret(18, 14, 4, 2, new Color32(40, 40, 44, 255));   // engate

        // Baú
        t.Ret(2, 16, 36, 36, bau);
        for (int y = 18; y < 52; y += 6) t.Ret(3, y, 34, 1, bauEscuro);
        t.Ret(2, 16, 1, 36, bauEscuro); t.Ret(37, 16, 1, 36, bauEscuro);
        t.Ret(8, 28, 24, 6, new Color32(240, 180, 40, 255));   // faixa da transportadora
        t.Ret(2, 50, 36, 2, bauEscuro);
        t.P(4, 50, new Color32(230, 30, 30, 255)); t.P(35, 50, new Color32(230, 30, 30, 255));
        return SalvarVideogame(t, "CaminhaoRuim");
    }

    // Explosão em blocos (3 quadros, cada vez maior)
    public static Sprite[] ExplosaoRuim()
    {
        Color32 amarelo = new Color32(255, 230, 60, 255), laranja = new Color32(255, 130, 30, 255),
                vermelho = new Color32(220, 40, 30, 255), fumaca = new Color32(90, 90, 96, 255);
        Sprite[] r = new Sprite[3];
        for (int q = 0; q < 3; q++)
        {
            Tela t = new Tela(24, 24);
            System.Random rnd = new System.Random(40 + q);
            float raio = 4f + q * 3.5f;
            for (int by = 0; by < 12; by++)
                for (int bx = 0; bx < 12; bx++)
                {
                    float dx = bx - 5.5f, dy = by - 5.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) + (float)rnd.NextDouble() * 1.6f;
                    if (d > raio) continue;
                    Color32 c = d < raio * 0.35f ? amarelo : d < raio * 0.65f ? laranja : q == 2 && d > raio * 0.85f ? fumaca : vermelho;
                    t.Ret(bx * 2, by * 2, 2, 2, c);   // "pixels" de 2x2: bem de videogame velho
                }
            r[q] = SalvarVideogame(t, "Explosao_" + q);
        }
        return r;
    }

    // ======================================================================
    // Cenário do videogame
    // ======================================================================

    public static Sprite GramaRuim()
    {
        Tela t = new Tela(8, 8);
        t.Ret(0, 0, 8, 8, new Color32(40, 140, 40, 255));
        t.P(1, 1, new Color32(30, 110, 30, 255)); t.P(5, 3, new Color32(30, 110, 30, 255));
        t.P(2, 6, new Color32(70, 170, 60, 255)); t.P(6, 6, new Color32(30, 110, 30, 255));
        return SalvarVideogame(t, "GramaRuim");
    }

    // Tracejado do meio da pista (repete a cada 12 pixels)
    public static Sprite FaixaRuim()
    {
        Tela t = new Tela(2, 12);
        t.Ret(0, 0, 2, 6, new Color32(240, 240, 240, 255));
        return SalvarVideogame(t, "FaixaRuim");
    }

    public static Sprite ArvoreRuim()
    {
        Tela t = new Tela(10, 14);
        Color32 escuro = new Color32(20, 100, 30, 255), claro = new Color32(60, 170, 60, 255);
        for (int y = 0; y < 10; y++)
            for (int x = 0; x < 10; x++)
            {
                float dx = x - 4.5f, dy = y - 4.5f;
                if (dx * dx + dy * dy <= 22f) t.P(x, y, dx + dy < -2f ? claro : escuro);
            }
        t.Ret(4, 10, 2, 4, new Color32(110, 70, 30, 255));
        return SalvarVideogame(t, "ArvoreRuim");
    }

    public static Sprite ArbustoRuim()
    {
        Tela t = new Tela(8, 6);
        t.Ret(1, 1, 6, 5, new Color32(30, 120, 40, 255));
        t.Ret(0, 2, 8, 3, new Color32(30, 120, 40, 255));
        t.P(2, 2, new Color32(70, 170, 60, 255)); t.P(3, 2, new Color32(70, 170, 60, 255));
        return SalvarVideogame(t, "ArbustoRuim");
    }

    public static Sprite PlacaRuim()
    {
        Tela t = new Tela(7, 11);
        t.Ret(3, 4, 1, 7, new Color32(150, 150, 150, 255));
        t.Ret(0, 0, 7, 5, new Color32(30, 120, 60, 255));
        t.Ret(1, 2, 5, 1, new Color32(240, 240, 240, 255));
        return SalvarVideogame(t, "PlacaRuim");
    }

    // ---------- Efeito de TV de tubo ----------

    // Linhas escuras (Image "Tiled")
    public static Sprite LinhasTV()
    {
        Tela t = new Tela(1, 2);
        t.P(0, 1, new Color32(0, 0, 0, 90));
        return SalvarVideogame(t, "LinhasTV");
    }

    // Cantos arredondados escuros e bordas sombreadas (Image "Sliced")
    public static Sprite VinhetaTV()
    {
        const int n = 24;
        Tela t = new Tela(n, n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int dx = Mathf.Min(x, n - 1 - x), dy = Mathf.Min(y, n - 1 - y);
                float a = 0f;
                // canto arredondado
                if (dx < 7 && dy < 7)
                {
                    float cx = 7 - dx - 0.5f, cy = 7 - dy - 0.5f;
                    if (cx * cx + cy * cy > 42f) a = 1f;
                }
                // sombra nas bordas
                int borda = Mathf.Min(dx, dy);
                if (borda < 3) a = Mathf.Max(a, (3 - borda) / 3f * 0.45f);
                if (a > 0f) t.P(x, y, new Color32(0, 0, 0, (byte)(a * 255)));
            }
        return SalvarVideogame(t, "VinhetaTV", new Vector4(9, 9, 9, 9));
    }

    // Brilho do vidro (faixa diagonal bem fraquinha)
    public static Sprite BrilhoTV()
    {
        Tela t = new Tela(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                int d = x + y - 22;
                if (d >= 0 && d < 4) t.P(x, y, new Color32(255, 255, 255, 26));
                else if (d >= 6 && d < 7) t.P(x, y, new Color32(255, 255, 255, 16));
            }
        return SalvarVideogame(t, "BrilhoTV");
    }

    // Retângulo branco 1x1 (asfalto, linhas, barras)
    public static Sprite PixelBranco()
    {
        Tela t = new Tela(1, 1);
        t.P(0, 0, new Color32(255, 255, 255, 255));
        return SalvarVideogame(t, "PixelBranco");
    }

    // ---------- Fonte ----------

    public const string CaracteresFonte = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 !.-:?,<>()/+ÃÁÉÍÓÇÕÂÊÔÚÀ";

    // Uma imagem por letra (9 de altura: 3 para o acento, 5 da letra, 1 da cedilha), na fonte pixel do jogo
    public static FontePixel CriarFontePixel()
    {
        List<Sprite> glifos = new List<Sprite>();
        foreach (char c in CaracteresFonte)
        {
            int w = Mathf.Max(1, AutoescolaArte.LarguraTexto(c.ToString()));
            Tela t = new Tela(w, 9);
            AutoescolaArte.EscreverEm(t, c.ToString(), w / 2, 3, 1, new Color32(255, 255, 255, 255), new Color32(0, 0, 0, 0));
            glifos.Add(PixelArt.Salvar(PastaVideogame + "/Fonte", "Letra_" + (int)c, t.p, t.L, t.A, default, 100f, Vector2.zero));
        }

        FontePixel fonte = AssetDatabase.LoadAssetAtPath<FontePixel>(CaminhoFontePixel);
        if (fonte == null)
        {
            fonte = ScriptableObject.CreateInstance<FontePixel>();
            AssetDatabase.CreateAsset(fonte, CaminhoFontePixel);
        }
        fonte.caracteres = CaracteresFonte;
        fonte.glifos = glifos.ToArray();
        fonte.altura = 9;
        fonte.espaco = 1;
        EditorUtility.SetDirty(fonte);
        AssetDatabase.SaveAssets();
        return fonte;
    }

    // ======================================================================
    // Quarto do Jason: a TV com o videogame e o puff
    // ======================================================================

    // TV de tubo com antena em cima de um rack com o console (16x26, apoio embaixo à esquerda)
    public static Sprite TVQuarto(bool ligada)
    {
        Tela t = new Tela(16, 26);
        Color32 plastico = new Color32(196, 186, 168, 255), plasticoEscuro = new Color32(150, 140, 124, 255);
        Color32 antena = new Color32(150, 150, 160, 255);

        // Antena "orelha de coelho"
        for (int i = 0; i < 4; i++) { t.P(8 - i, 4 - i, antena); t.P(9 + i, 4 - i, antena); }
        t.P(4, 0, Branco); t.P(13, 0, Branco);

        // Corpo da TV
        t.Caixa(2, 4, 13, 14, plastico);
        t.Ret(3, 15, 11, 2, plasticoEscuro);
        // Botões e alto-falante
        t.P(13, 7, new Color32(60, 60, 60, 255)); t.P(13, 9, new Color32(60, 60, 60, 255));
        t.P(13, 12, plasticoEscuro); t.P(13, 13, plasticoEscuro);
        t.P(13, 15, ligada ? new Color32(255, 60, 60, 255) : new Color32(90, 30, 30, 255));   // luzinha

        // Telinha (x 4..11, y 6..14)
        RectInt tela = new RectInt(4, 6, 8, 9);
        for (int y = tela.y; y < tela.y + tela.height; y++)
            for (int x = tela.x; x < tela.x + tela.width; x++)
            {
                Color32 c;
                if (ligada)
                {
                    c = new Color32(12, 12, 30, 255);
                    if (x == 7 || x == 8) c = new Color32(70, 70, 78, 255);                 // a estradinha
                    if (x <= 5 || x >= 10) c = new Color32(30, 110, 30, 255);               // grama
                    if (y == 8 && x >= 5 && x <= 10) c = new Color32(230, 40, 40, 255);     // "GAME OVER"
                    if ((x == 7 || x == 8) && y == 12) c = new Color32(255, 140, 30, 255);  // a explosão
                    if (y % 2 == 1) c = Vezes(c, 0.82f);                                    // linhas da TV
                }
                else
                {
                    c = new Color32(40, 50, 48, 255);
                    if (x - y == -4 || x - y == -3) c = new Color32(70, 84, 80, 255);       // reflexo no vidro
                }
                t.P(x, y, c);
            }

        // Rack de madeira com uma prateleira aberta e o console dentro
        t.Caixa(0, 18, 16, 8, Movel);
        t.Ret(1, 19, 14, 1, MovelClaro);
        t.Ret(2, 21, 12, 4, new Color32(70, 44, 28, 255));
        t.Ret(3, 22, 7, 3, new Color32(80, 80, 90, 255));                   // console
        t.P(4, 22, new Color32(255, 60, 60, 255));                          // luzinha do console
        t.Ret(6, 21, 2, 1, new Color32(240, 190, 40, 255));                 // cartucho do TURBO RACER
        t.Ret(11, 22, 2, 3, new Color32(50, 50, 56, 255));                  // fita/caixinha de jogo
        return t.Salvar(PastaCasa, ligada ? "TVQuartoLigada" : "TVQuarto", Vector2.zero);
    }

    // Puff no chão, com o controle e o fio indo até o videogame (16x12, apoio embaixo à esquerda)
    public static Sprite Puff()
    {
        Tela t = new Tela(16, 12);
        Color32 tecido = new Color32(240, 130, 40, 255), escuro = new Color32(190, 90, 30, 255), claro = new Color32(255, 176, 90, 255);
        for (int y = 0; y < 12; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = (x - 7.5f) / 7.5f, dy = (y - 7f) / 5f;
                float d = dx * dx + dy * dy;
                if (d > 1f) continue;
                Color32 c = d > 0.75f ? escuro : tecido;
                if (dx < -0.2f && dy < -0.2f && d < 0.5f) c = claro;
                t.P(x, y, c);
            }
        t.Ret(5, 6, 6, 1, escuro);                                  // vinco do assento
        // Fio do controle (sobe até a TV) e o controle
        for (int y = 0; y < 8; y++) t.P(13, y, new Color32(40, 40, 44, 255));
        t.Ret(11, 8, 4, 2, new Color32(30, 30, 34, 255));
        t.P(12, 8, new Color32(230, 40, 40, 255));
        return t.Salvar(PastaCasa, "Puff", Vector2.zero);
    }

    private static Color32 Mais(Color32 c, int v) =>
        new Color32((byte)Mathf.Min(255, c.r + v), (byte)Mathf.Min(255, c.g + v), (byte)Mathf.Min(255, c.b + v), c.a);

    private static Color32 Vezes(Color32 c, float f) =>
        new Color32((byte)(c.r * f), (byte)(c.g * f), (byte)(c.b * f), c.a);
}
