using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Desenhos da história em pixel art: personagens (lidos dos .txt de Assets/Editor/ArteHistoria),
// a casa do Jason (chão, paredes e móveis), os balõezinhos e os desenhos do exame.
// Usado por MontarHistoria (menu Mr Driver > História).
public static partial class HistoriaArte
{
    public const string PastaArte = "Assets/Editor/ArteHistoria";
    public const string PastaPersonagens = "Assets/Imagens/Historia/Personagens";
    public const string PastaCasa = "Assets/Imagens/Historia/Casa";
    public const string PastaUI = "Assets/Imagens/Historia/UI";
    public const string PastaAssets = "Assets/Historia/Personagens";
    public const int Px = 16;   // pixels por quadradinho da casa

    // ======================================================================
    // Personagens
    // ======================================================================

    private class ArquivoArte
    {
        public readonly Dictionary<char, Color32> paleta = new Dictionary<char, Color32>();
        public readonly Dictionary<string, List<string>> quadros = new Dictionary<string, List<string>>();
        public readonly List<string[]> retoques = new List<string[]>();
    }

    // Formato: blocos "paleta ... fim", "quadro <nome> ... fim" e "retoques ... fim"; '#' = comentário
    private static ArquivoArte Ler(string caminho)
    {
        ArquivoArte arte = new ArquivoArte();
        string bloco = null;
        List<string> quadro = null;

        foreach (string bruta in File.ReadAllLines(caminho))
        {
            string linha = bruta.Trim();
            if (linha.StartsWith("#") || (bloco == null && linha.Length == 0)) continue;

            if (bloco == null)
            {
                string[] partes = linha.Split(' ');
                bloco = partes[0];
                if (bloco == "quadro")
                {
                    quadro = new List<string>();
                    arte.quadros[partes[1]] = quadro;
                }
                continue;
            }

            if (linha == "fim") { bloco = null; continue; }

            if (bloco == "paleta")
            {
                string[] v = linha.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                byte a = v.Length > 4 ? byte.Parse(v[4]) : (byte)255;
                arte.paleta[v[0][0]] = new Color32(byte.Parse(v[1]), byte.Parse(v[2]), byte.Parse(v[3]), a);
            }
            else if (bloco == "quadro") quadro.Add(linha);
            else if (bloco == "retoques") arte.retoques.Add(linha.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));
        }
        return arte;
    }

    private static Sprite DesenharLinhas(string pasta, string nome, IList<string> linhas, Dictionary<char, Color32> paleta,
                                         float ppu, Vector2 pivo)
    {
        int altura = linhas.Count;
        int largura = linhas[0].Length;
        Color32[] p = PixelArt.NovaTela(largura, altura);
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura && x < linhas[y].Length; x++)
                if (paleta.TryGetValue(linhas[y][x], out Color32 cor))
                    PixelArt.Pintar(p, largura, altura, x, y, cor);
        return PixelArt.Salvar(pasta, nome, p, largura, altura, default, ppu, pivo);
    }

    // Lê o .txt do personagem, gera os 9 quadros de andar + o retrato e cria/atualiza o asset
    public static PersonagemHistoria CriarPersonagem(string arquivo, string nome, Color corNome, float tomVoz,
                                                     string salvarComo = null, Dictionary<char, Color32> trocas = null)
    {
        string saida = string.IsNullOrEmpty(salvarComo) ? arquivo : salvarComo;
        ArquivoArte corpos = Ler(PastaArte + "/Corpos.txt");
        ArquivoArte arte = Ler(PastaArte + "/" + arquivo + ".txt");

        Dictionary<char, Color32> paleta = new Dictionary<char, Color32>(corpos.paleta);
        foreach (KeyValuePair<char, Color32> par in arte.paleta) paleta[par.Key] = par.Value;
        if (trocas != null) foreach (KeyValuePair<char, Color32> par in trocas) paleta[par.Key] = par.Value;

        string[] direcoes = { "baixo", "cima", "lado" };
        Sprite[] quadros = new Sprite[9];
        for (int d = 0; d < 3; d++)
        {
            for (int i = 0; i < 3; i++)
            {
                List<string> linhas = new List<string>(arte.quadros["cabeca_" + direcoes[d]]);
                linhas.AddRange(corpos.quadros["corpo_" + direcoes[d] + "_" + i]);
                // Os pés ficam um pouco acima da borda de baixo do quadradinho
                quadros[d * 3 + i] = DesenharLinhas(PastaPersonagens, saida + "_" + direcoes[d] + "_" + i, linhas,
                                                    paleta, Px, new Vector2(0.5f, 7f / 24f));
            }
        }

        Sprite retrato = null;
        if (arte.quadros.TryGetValue("retrato_metade", out List<string> metade))
        {
            List<char[]> cheio = new List<char[]>();
            foreach (string l in metade)
            {
                char[] invertida = l.ToCharArray();
                System.Array.Reverse(invertida);
                cheio.Add((l + new string(invertida)).ToCharArray());
            }
            foreach (string[] r in arte.retoques)
            {
                int lin = int.Parse(r[0]), col = int.Parse(r[1]);
                for (int i = 0; i < r[2].Length; i++) cheio[lin][col + i] = r[2][i];
            }
            List<string> linhas = new List<string>();
            foreach (char[] c in cheio) linhas.Add(new string(c));
            retrato = DesenharLinhas(PastaPersonagens, "Retrato_" + saida, linhas, paleta, 32f, new Vector2(0.5f, 0.5f));
        }

        CriarPasta(PastaAssets);
        string caminho = PastaAssets + "/" + saida + ".asset";
        PersonagemHistoria p = AssetDatabase.LoadAssetAtPath<PersonagemHistoria>(caminho);
        if (p == null)
        {
            p = ScriptableObject.CreateInstance<PersonagemHistoria>();
            AssetDatabase.CreateAsset(p, caminho);
        }
        p.nome = nome;
        p.corNome = corNome;
        p.tomVoz = tomVoz;
        p.quadros = quadros;
        p.retrato = retrato;
        EditorUtility.SetDirty(p);
        return p;
    }

    public static PersonagemHistoria Carregar(string arquivo) =>
        AssetDatabase.LoadAssetAtPath<PersonagemHistoria>(PastaAssets + "/" + arquivo + ".asset");

    public static void CriarPasta(string pasta)
    {
        string[] partes = pasta.Split('/');
        string atual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string proxima = atual + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(proxima)) AssetDatabase.CreateFolder(atual, partes[i]);
            atual = proxima;
        }
    }

    // ======================================================================
    // Balõezinhos (na ordem do enum Emote; o índice 0 = Nenhum fica vazio)
    // ======================================================================

    public static Sprite[] DesenharEmotes()
    {
        Dictionary<char, Color32> pal = new Dictionary<char, Color32>
        {
            { 'k', PixelArt.Contorno }, { 'w', new Color32(255, 255, 255, 255) },
            { 'r', new Color32(230, 40, 60, 255) }, { 'b', new Color32(60, 120, 230, 255) },
            { 'c', new Color32(150, 210, 255, 255) }, { 'p', new Color32(255, 120, 170, 255) },
        };
        string[] moldura =
        {
            ".kkkkkkkkkkk.", "kwwwwwwwwwwwk", "kwwwwwwwwwwwk", "kwwwwwwwwwwwk", "kwwwwwwwwwwwk", "kwwwwwwwwwwwk",
            "kwwwwwwwwwwwk", "kwwwwwwwwwwwk", "kwwwwwwwwwwwk", ".kkkkwwkkkkk.", ".....kwk.....", "......k......",
        };
        string[][] simbolos =
        {
            null,
            new[] { "..rr..", "..rr..", "..rr..", "..rr..", "......", "..rr.." },            // !
            new[] { ".bbbb.", "bb..bb", "...bb.", "..bb..", "......", "..bb.." },            // ?
            new[] { ".pp.pp.", "ppppppp", "ppppppp", ".ppppp.", "..ppp..", "...p..." },     // coração
            new[] { "bbbb.....", "..b......", ".b..bbb..", "bbbb..b..", "....b...b", "...bbbbbb" }, // Zzz
            new[] { "...c...", "..ccc..", ".ccccc.", ".cccwc.", "..ccc.." },                 // gota
        };

        Sprite[] sprites = new Sprite[simbolos.Length];
        string[] nomes = { "", "Exclamacao", "Interrogacao", "Coracao", "Sono", "Gota" };
        for (int i = 1; i < simbolos.Length; i++)
        {
            char[][] tela = new char[moldura.Length][];
            for (int y = 0; y < moldura.Length; y++) tela[y] = moldura[y].ToCharArray();
            string[] s = simbolos[i];
            int ox = (13 - s[0].Length) / 2, oy = 1 + (8 - s.Length) / 2;
            for (int y = 0; y < s.Length; y++)
                for (int x = 0; x < s[y].Length; x++)
                    if (s[y][x] != '.') tela[oy + y][ox + x] = s[y][x];

            List<string> linhas = new List<string>();
            foreach (char[] l in tela) linhas.Add(new string(l));
            sprites[i] = DesenharLinhas(PastaUI, "Emote_" + nomes[i], linhas, pal, Px, new Vector2(0.5f, 0f));
        }
        return sprites;
    }

    public static Sprite SalvarComPpu(Tela tela, string pasta, string nome, float ppu, Vector2 pivo) =>
        PixelArt.Salvar(pasta, nome, tela.p, tela.L, tela.A, default, ppu, pivo);

    public static Sprite DesenharSombra()
    {
        Tela t = new Tela(12, 5);
        Color32 c = new Color32(20, 10, 30, 80);
        t.Ret(2, 0, 8, 5, c);
        t.Ret(0, 1, 12, 3, c);
        return t.Salvar(PastaUI, "Sombra", new Vector2(0.5f, 0.5f));
    }

    // ======================================================================
    // UI (molduras e ícones)
    // ======================================================================

    public static Sprite MolduraDialogo() =>
        PixelArt.Caixa(PastaUI, "MolduraDialogo", 24, 3, PixelArt.Magenta, new Color32(255, 200, 240, 255),
                       new Color32(255, 250, 242, 255));

    public static Sprite MolduraRetrato() =>
        PixelArt.Caixa(PastaUI, "MolduraRetrato", 24, 3, PixelArt.MagentaEscuro, new Color32(120, 190, 240, 255),
                       new Color32(176, 222, 255, 255));

    public static Sprite MolduraBranca() =>
        PixelArt.Caixa(PastaUI, "MolduraBranca", 24, 3, new Color32(225, 225, 225, 255), new Color32(255, 255, 255, 255),
                       new Color32(255, 255, 255, 255));

    public static Sprite MolduraEscura() =>
        PixelArt.Caixa(PastaUI, "MolduraEscura", 24, 3, PixelArt.Magenta, PixelArt.MagentaEscuro, PixelArt.Fundo);

    public static Sprite MolduraCartao() =>
        PixelArt.Caixa(PastaUI, "MolduraCartao", 24, 3, new Color32(50, 140, 90, 255), new Color32(170, 220, 180, 255),
                       new Color32(238, 248, 228, 255));

    public static Sprite MolduraCarimbo() =>
        PixelArt.Caixa(PastaUI, "MolduraCarimbo", 24, 3, new Color32(220, 30, 50, 255), new Color32(220, 30, 50, 0),
                       new Color32(255, 255, 255, 0));

    public static Sprite SetaDialogo()
    {
        Dictionary<char, Color32> pal = new Dictionary<char, Color32> { { 'k', PixelArt.Contorno }, { 'm', PixelArt.Magenta } };
        return DesenharLinhas(PastaUI, "SetaDialogo",
            new[] { "kkkkkkkkk", "kmmmmmmmk", ".kmmmmmk.", "..kmmmk..", "...kmk...", "....k...." },
            pal, 100f, new Vector2(0.5f, 0.5f));
    }

    // Área do ponto do exame (imagem "Sliced": a borda não estica)
    public static Sprite AreaPonto()
    {
        Tela t = new Tela(32, 32);
        Color32 borda = new Color32(255, 255, 255, 255);
        Color32 dentro = new Color32(255, 255, 255, 70);
        t.Ret(0, 0, 32, 32, dentro);
        t.Borda(0, 0, 32, 32, borda);
        t.Borda(1, 1, 30, 30, borda);
        t.Borda(2, 2, 28, 28, borda);
        Sprite sprite = PixelArt.Salvar(PastaUI, "AreaPonto", t.p, 32, 32, new Vector4(6, 6, 6, 6), Px);

        // SpriteRenderer em modo "Sliced" precisa da malha retangular inteira
        string caminho = AssetDatabase.GetAssetPath(sprite);
        TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(caminho);
        TextureImporterSettings ajustes = new TextureImporterSettings();
        importador.ReadTextureSettings(ajustes);
        if (ajustes.spriteMeshType != SpriteMeshType.FullRect)
        {
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            importador.SetTextureSettings(ajustes);
            importador.SaveAndReimport();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
        }
        return sprite;
    }

    public static Sprite PlacaPare()
    {
        Tela t = new Tela(29, 29);
        Color32 vermelho = new Color32(220, 30, 40, 255);
        for (int y = 0; y < 29; y++)
            for (int x = 0; x < 29; x++)
            {
                int camada = PixelArt.Camada(x, y, 29, 29, 8);
                if (camada < 0) continue;
                t.P(x, y, camada == 0 ? PixelArt.Contorno : camada <= 2 ? PixelArt.Branco : vermelho);
            }
        PixelArt.DesenharTexto(t.p, 29, 29, "PARE", (29 - PixelArt.LarguraTexto("PARE")) / 2, 12, PixelArt.Branco);
        return t.Salvar(PastaUI, "PlacaPare", new Vector2(0.5f, 0.5f));
    }

    public static Sprite PlacaVaga()
    {
        Tela t = new Tela(29, 21);
        Color32 azul = new Color32(40, 100, 220, 255);
        for (int y = 0; y < 21; y++)
            for (int x = 0; x < 29; x++)
            {
                int camada = PixelArt.Camada(x, y, 29, 21, 2);
                if (camada < 0) continue;
                t.P(x, y, camada == 0 ? PixelArt.Contorno : camada <= 2 ? PixelArt.Branco : azul);
            }
        PixelArt.DesenharTexto(t.p, 29, 21, "VAGA", (29 - PixelArt.LarguraTexto("VAGA")) / 2, 8, PixelArt.Branco);
        return t.Salvar(PastaUI, "PlacaVaga", new Vector2(0.5f, 0.5f));
    }

    public static Sprite DesenharSetaObjetivo()
    {
        Dictionary<char, Color32> pal = new Dictionary<char, Color32>
        {
            { 'k', PixelArt.Contorno }, { 'y', new Color32(255, 220, 40, 255) }, { 'o', new Color32(255, 160, 20, 255) },
        };
        return DesenharLinhas(PastaUI, "SetaObjetivo", new[]
        {
            ".......k.......", "......kyk......", ".....kyyyk.....", "....kyyyyyk....", "...kyyyyyyyk...",
            "..kyyyyyyyyyk..", ".kyyyyyyyyyyyk.", "kkkkkyyyyykkkkk", "....kyyyyyk....", "....kyoooyk....",
            "....koooook....", "....kkkkkkk....",
        }, pal, Px, new Vector2(0.5f, 0.5f));
    }

    // ======================================================================
    // A casa
    // ======================================================================

    // Cores
    private static readonly Color32 K = PixelArt.Contorno;
    private static readonly Color32 TopoParede = new Color32(84, 56, 78, 255);
    private static readonly Color32 TopoParedeClaro = new Color32(118, 86, 110, 255);
    private static readonly Color32 Rodape = new Color32(140, 92, 64, 255);
    private static readonly Color32 RodapeEscuro = new Color32(104, 66, 46, 255);
    private static readonly Color32 Madeira = new Color32(214, 160, 104, 255);
    private static readonly Color32 MadeiraEscura = new Color32(176, 122, 76, 255);
    private static readonly Color32 MadeiraClara = new Color32(232, 186, 130, 255);
    private static readonly Color32 Movel = new Color32(166, 104, 64, 255);
    private static readonly Color32 MovelEscuro = new Color32(120, 72, 44, 255);
    private static readonly Color32 MovelClaro = new Color32(196, 136, 88, 255);
    private static readonly Color32 Branco = new Color32(244, 244, 248, 255);
    private static readonly Color32 Cinza = new Color32(190, 194, 206, 255);
    private static readonly Color32 CinzaEscuro = new Color32(120, 124, 140, 255);

    // Mapa da casa: '#' parede, 'q' quarto, 's' sala, 'c' cozinha, 'P' porta da frente,
    // 'g' garagem, 'G' porta da garagem (na parede da sala), 'T' portão da garagem (saída do carro)
    public static readonly string[] PlantaCasa =
    {
        "################################",
        "################################",
        "#qqqqqqq#ccccccssssss#ggggggggg#",
        "#qqqqqqq#ccccccssssss#ggggggggg#",
        "#qqqqqqq#ccccccssssss#ggggggggg#",
        "#qqqqqqq#ccccccssssssGggggggggg#",
        "#qqqqqqqqssssssssssss#ggggggggg#",
        "#qqqqqqqqssssssssssss#ggggggggg#",
        "#qqqqqqq#ssssssssssss#ggggggggg#",
        "#qqqqqqq#ssssssssssss#ggggggggg#",
        "#qqqqqqq#ssssssssssss#ggggggggg#",
        "###############P########TTTT####",
    };

    // Lugares da garagem (células da grade)
    public static readonly Vector2Int PortaGaragem = new Vector2Int(21, 5);
    public static readonly RectInt Portao = new RectInt(24, 11, 4, 1);
    public static readonly RectInt VagaCarro = new RectInt(25, 4, 2, 4);
    public const int InicioGaragem = 22;

    public static bool EhChao(int x, int y) =>
        y >= 0 && y < planta.Length && x >= 0 && x < planta[y].Length && planta[y][x] != '#';

    // Planta usada pelos desenhos de chão e parede (a casa ou o mercado)
    private static string[] planta = PlantaCasa;

    // A linha 1 é a "frente" da parede de cima (papel de parede); as outras paredes aparecem só de cima
    private static bool EhFrenteParede(int x, int y) => y == 1 && EhChao(x, 2);

    public static Sprite DesenharFundo(int[] janelas, int poster, int relogio, int quadro)
    {
        planta = PlantaCasa;
        int largura = planta[0].Length, altura = planta.Length;
        Tela t = new Tela(largura * Px, altura * Px);

        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                int px = x * Px, py = y * Px;
                char c = planta[y][x];
                if (EhFrenteParede(x, y)) PapelDeParede(t, px, py, x <= 8 ? 0 : x >= InicioGaragem ? 2 : 1);
                else if (c == '#') TopoDeParede(t, x, y);
                else if (c == 'c') Azulejo(t, px, py);
                else if (c == 'P') Porta(t, px, py);
                else if (c == 'g') Concreto(t, px, py, x, y);
                else if (c == 'G') Soleira(t, px, py);
                else if (c == 'T') LuzDoPortao(t, px, py, x);
                else Assoalho(t, px, py, x, y);
            }

        // Sombra das paredes no chão
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                if (!EhChao(x, y) || planta[y][x] == 'P' || planta[y][x] == 'T') continue;
                int px = x * Px, py = y * Px;
                if (y == 2) t.Escurecer(px, py, Px, 3, 0.75f);
                if (!EhChao(x - 1, y)) t.Escurecer(px, py, 2, Px, 0.8f);
            }

        // Tapetes
        Tapete(t, 3, 5, 3, 3, new Color32(230, 90, 100, 255), new Color32(250, 214, 120, 255));
        Tapete(t, 16, 4, 4, 3, new Color32(110, 160, 120, 255), new Color32(236, 226, 190, 255));
        Capacho(t, 15, 10);

        // Coisas penduradas na parede
        foreach (int j in janelas) Janela(t, j * Px, Px, j <= 8);
        Poster(t, poster * Px, Px);
        Relogio(t, relogio * Px, Px);
        QuadroTaxi(t, quadro * Px, Px);

        // Garagem: chão pintado e coisas na parede
        if (largura > InicioGaragem) DetalhesGaragem(t);

        return t.Salvar(PastaCasa, "CasaFundo", new Vector2(0f, 1f));
    }

    private static void TopoDeParede(Tela t, int x, int y)
    {
        int px = x * Px, py = y * Px;
        t.Ret(px, py, Px, Px, TopoParede);
        // Linha clara no meio dá a ideia de "topo" da parede
        if (EhChao(x, y + 1) || EhFrenteParede(x, y + 1)) t.Ret(px, py + Px - 2, Px, 2, K);
        if (EhChao(x, y - 1)) t.Ret(px, py, Px, 2, K);
        if (EhChao(x + 1, y) || EhFrenteParede(x + 1, y)) t.Ret(px + Px - 2, py, 2, Px, K);
        if (EhChao(x - 1, y) || EhFrenteParede(x - 1, y)) t.Ret(px, py, 2, Px, K);
        if ((x + y) % 2 == 0) t.Ret(px + 5, py + 6, 6, 1, TopoParedeClaro);
    }

    // tipo: 0 = quarto, 1 = sala e cozinha, 2 = garagem (tijolinhos)
    private static void PapelDeParede(Tela t, int px, int py, int tipo)
    {
        if (tipo == 2)
        {
            Tijolos(t, px, py);
            return;
        }
        bool quarto = tipo == 0;
        Color32 fundo = quarto ? new Color32(150, 196, 238, 255) : new Color32(250, 230, 186, 255);
        Color32 listra = quarto ? new Color32(126, 174, 224, 255) : new Color32(234, 206, 158, 255);
        t.Ret(px, py, Px, Px, fundo);
        for (int x = 0; x < Px; x += 4) t.Ret(px + x, py, 1, Px - 4, listra);
        if (!quarto)
            for (int x = 2; x < Px; x += 8) t.P(px + x, py + 5, new Color32(220, 120, 110, 255));
        t.Ret(px, py, Px, 1, K);
        t.Ret(px, py + Px - 4, Px, 4, Rodape);
        t.Ret(px, py + Px - 4, Px, 1, RodapeEscuro);
        t.Ret(px, py + Px - 1, Px, 1, RodapeEscuro);
    }

    private static void Assoalho(Tela t, int px, int py, int x, int y)
    {
        for (int tabua = 0; tabua < 4; tabua++)
        {
            int ty = py + tabua * 4;
            int h = Hash(x, y * 4 + tabua);
            Color32 cor = h % 5 == 0 ? MadeiraClara : Madeira;
            t.Ret(px, ty, Px, 4, cor);
            t.Ret(px, ty + 3, Px, 1, MadeiraEscura);
            int emenda = ((y * 4 + tabua) % 2 == 0 ? 3 : 11) + (x % 2) * 0;
            t.Ret(px + emenda, ty, 1, 3, MadeiraEscura);
            if (h % 7 == 0) t.P(px + (h % 12) + 2, ty + 1, MadeiraEscura);
        }
    }

    private static void Azulejo(Tela t, int px, int py)
    {
        Color32 a = new Color32(244, 244, 236, 255), b = new Color32(196, 222, 238, 255), rejunte = new Color32(170, 176, 188, 255);
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++)
            {
                t.Ret(px + x * 8, py + y * 8, 8, 8, (x + y) % 2 == 0 ? a : b);
                t.Ret(px + x * 8, py + y * 8 + 7, 8, 1, rejunte);
                t.Ret(px + x * 8 + 7, py + y * 8, 1, 8, rejunte);
            }
    }

    private static void Porta(Tela t, int px, int py)
    {
        t.Ret(px, py, Px, Px, new Color32(120, 84, 60, 255));
        t.Ret(px + 2, py + 2, Px - 4, Px - 2, new Color32(255, 236, 170, 255));   // luz de fora entrando
        t.Ret(px + 2, py + 2, Px - 4, 2, new Color32(230, 200, 120, 255));
        t.Ret(px, py, 2, Px, K);
        t.Ret(px + Px - 2, py, 2, Px, K);
    }

    private static void Tapete(Tela t, int cx, int cy, int w, int h, Color32 cor, Color32 borda)
    {
        int px = cx * Px + 3, py = cy * Px + 3, lw = w * Px - 6, lh = h * Px - 6;
        t.Ret(px, py, lw, lh, borda);
        t.Ret(px + 3, py + 3, lw - 6, lh - 6, cor);
        Color32 escuro = Escuro(cor, 0.8f);
        for (int y = py + 6; y < py + lh - 6; y += 6)
            for (int x = px + 6 + (y / 6 % 2) * 3; x < px + lw - 6; x += 6) t.P(x, y, escuro);
        t.Borda(px, py, lw, lh, K);
    }

    private static void Capacho(Tela t, int cx, int cy)
    {
        int px = cx * Px + 2, py = cy * Px + 5;
        t.Ret(px, py, 12, 9, new Color32(150, 90, 60, 255));
        for (int x = px + 1; x < px + 11; x += 2) t.Ret(x, py + 1, 1, 7, new Color32(190, 130, 80, 255));
        t.Borda(px, py, 12, 9, K);
    }

    private static void Janela(Tela t, int px, int py, bool quarto)
    {
        if (px >= InicioGaragem * Px)
        {
            JanelaGaragem(t, px, py);
            return;
        }
        Color32 cortina = quarto ? new Color32(255, 170, 60, 255) : new Color32(230, 90, 110, 255);
        t.Ret(px + 2, py + 2, 12, 9, Branco);
        t.Ret(px + 3, py + 3, 10, 7, new Color32(140, 206, 250, 255));
        t.Ret(px + 4, py + 4, 2, 1, Branco);
        t.Ret(px + 4, py + 5, 1, 1, Branco);
        t.Ret(px + 3, py + 6, 10, 1, Branco);
        t.Ret(px + 7, py + 3, 1, 7, Branco);
        t.Borda(px + 2, py + 2, 12, 9, K);
        t.Ret(px + 1, py + 2, 2, 10, cortina);
        t.Ret(px + 13, py + 2, 2, 10, cortina);
        t.Ret(px + 1, py + 11, 14, 1, MovelClaro);
    }

    private static void Poster(Tela t, int px, int py)
    {
        t.Ret(px + 2, py + 1, 12, 10, new Color32(30, 30, 70, 255));
        t.Borda(px + 2, py + 1, 12, 10, K);
        // Carro esportivo vermelho
        Color32 v = new Color32(230, 40, 50, 255);
        t.Ret(px + 4, py + 6, 8, 2, v);
        t.Ret(px + 6, py + 5, 4, 1, v);
        t.Ret(px + 7, py + 5, 2, 1, new Color32(150, 210, 255, 255));
        t.P(px + 5, py + 8, K);
        t.P(px + 10, py + 8, K);
        t.Ret(px + 4, py + 3, 3, 1, new Color32(255, 220, 60, 255));
        t.P(px + 3, py + 2, Branco);
        t.P(px + 12, py + 3, Branco);
    }

    private static void Relogio(Tela t, int px, int py)
    {
        Color32 c = new Color32(250, 250, 240, 255);
        t.Ret(px + 5, py + 2, 6, 8, c);
        t.Ret(px + 4, py + 3, 8, 6, c);
        t.Borda(px + 4, py + 3, 8, 6, K);
        t.Ret(px + 5, py + 2, 6, 1, K);
        t.Ret(px + 5, py + 9, 6, 1, K);
        t.Ret(px + 7, py + 4, 1, 3, K);
        t.Ret(px + 8, py + 6, 2, 1, new Color32(220, 40, 50, 255));
    }

    private static void QuadroTaxi(Tela t, int px, int py)
    {
        t.Ret(px + 3, py + 2, 10, 8, new Color32(200, 160, 60, 255));
        t.Ret(px + 4, py + 3, 8, 6, new Color32(160, 210, 250, 255));
        t.Ret(px + 5, py + 6, 6, 2, new Color32(255, 210, 40, 255));   // táxi amarelo
        t.Ret(px + 6, py + 5, 4, 1, new Color32(255, 210, 40, 255));
        t.P(px + 6, py + 8, K);
        t.P(px + 9, py + 8, K);
        t.Borda(px + 3, py + 2, 10, 8, K);
    }

    // ---------- Móveis (cada um vira um sprite separado, com o ponto de apoio embaixo à esquerda) ----------

    public static Sprite Cama()
    {
        Tela t = new Tela(16, 36);
        t.Caixa(0, 0, 16, 9, Movel);                      // cabeceira
        t.Ret(2, 2, 12, 2, MovelClaro);
        t.Caixa(1, 8, 14, 28, new Color32(240, 240, 250, 255));
        t.Caixa(3, 10, 10, 6, Branco);                    // travesseiro
        t.Ret(4, 14, 8, 1, Cinza);
        Color32 coberta = new Color32(80, 130, 220, 255), cobertaEscura = new Color32(56, 96, 180, 255);
        t.Caixa(1, 17, 14, 19, coberta);
        t.Ret(2, 18, 12, 2, new Color32(140, 180, 245, 255));   // dobra
        for (int y = 23; y < 34; y += 4) t.Ret(2, y, 12, 1, cobertaEscura);
        t.Ret(1, 35, 14, 1, K);
        return t.Salvar(PastaCasa, "Cama", Vector2.zero);
    }

    public static Sprite GuardaRoupa()
    {
        Tela t = new Tela(32, 32);
        t.Caixa(0, 0, 32, 32, Movel);
        t.Ret(1, 1, 30, 3, MovelClaro);
        t.Ret(15, 4, 2, 26, MovelEscuro);
        t.Borda(2, 5, 13, 24, MovelEscuro);
        t.Borda(17, 5, 13, 24, MovelEscuro);
        t.Ret(12, 15, 2, 4, new Color32(250, 210, 80, 255));
        t.Ret(18, 15, 2, 4, new Color32(250, 210, 80, 255));
        t.Ret(1, 29, 30, 2, MovelEscuro);
        return t.Salvar(PastaCasa, "GuardaRoupa", Vector2.zero);
    }

    public static Sprite Escrivaninha()
    {
        Tela t = new Tela(32, 28);
        // Monitor com o jogo de corrida na tela
        t.Caixa(8, 0, 16, 12, new Color32(40, 40, 50, 255));
        t.Ret(10, 2, 12, 8, new Color32(70, 160, 90, 255));
        t.Ret(14, 2, 4, 8, new Color32(90, 90, 100, 255));
        t.Ret(16, 3, 1, 2, new Color32(255, 230, 80, 255));
        t.Ret(16, 7, 1, 2, new Color32(255, 230, 80, 255));
        t.Ret(15, 6, 2, 2, new Color32(230, 40, 50, 255));
        t.Ret(14, 12, 4, 2, new Color32(40, 40, 50, 255));
        // Tampo
        t.Caixa(0, 13, 32, 6, MovelClaro);
        t.Ret(9, 14, 14, 2, Cinza);                       // teclado
        t.Ret(26, 13, 3, 3, Branco);                      // caneca
        t.Ret(1, 18, 30, 1, MovelEscuro);
        // Pernas e gaveta
        t.Caixa(0, 19, 4, 9, Movel);
        t.Caixa(22, 19, 10, 9, Movel);
        t.Ret(25, 22, 4, 1, MovelEscuro);
        return t.Salvar(PastaCasa, "Escrivaninha", Vector2.zero);
    }

    public static Sprite Estante()
    {
        Tela t = new Tela(16, 28);
        t.Caixa(0, 0, 16, 28, Movel);
        t.Ret(2, 2, 12, 24, MovelEscuro);
        t.Ret(1, 12, 14, 2, MovelClaro);
        // Troféu
        Color32 ouro = new Color32(250, 200, 60, 255);
        t.Ret(5, 3, 6, 4, ouro);
        t.Ret(7, 7, 2, 3, ouro);
        t.Ret(5, 10, 6, 2, ouro);
        t.P(6, 4, Branco);
        // Livros
        Color32[] cores = { new Color32(220, 60, 60, 255), new Color32(60, 140, 220, 255), new Color32(250, 210, 70, 255),
                            new Color32(90, 190, 110, 255), new Color32(180, 90, 200, 255) };
        for (int i = 0; i < 5; i++) t.Ret(3 + i * 2, 16 + (i % 2), 2, 9 - (i % 2), cores[i]);
        t.Ret(1, 26, 14, 1, MovelClaro);
        return t.Salvar(PastaCasa, "Estante", Vector2.zero);
    }

    public static Sprite PilhaDeRoupa()
    {
        Tela t = new Tela(16, 14);
        t.Caixa(1, 6, 14, 8, new Color32(255, 140, 40, 255));
        t.Caixa(3, 3, 10, 6, new Color32(70, 100, 180, 255));
        t.Caixa(5, 0, 7, 5, new Color32(240, 240, 240, 255));
        t.Ret(2, 10, 6, 1, new Color32(210, 92, 26, 255));
        return t.Salvar(PastaCasa, "PilhaDeRoupa", Vector2.zero);
    }

    public static Sprite Geladeira()
    {
        Tela t = new Tela(16, 34);
        t.Caixa(0, 0, 16, 34, Branco);
        t.Ret(1, 11, 14, 1, Cinza);
        t.Ret(12, 4, 2, 5, CinzaEscuro);
        t.Ret(12, 14, 2, 8, CinzaEscuro);
        // Ímãs
        t.Ret(3, 3, 2, 2, new Color32(230, 50, 60, 255));
        t.Ret(6, 6, 2, 2, new Color32(60, 140, 230, 255));
        t.Ret(3, 16, 3, 3, new Color32(250, 210, 60, 255));
        t.Ret(4, 22, 4, 5, new Color32(255, 255, 210, 255));   // bilhete
        t.Ret(5, 23, 2, 1, Cinza);
        t.Ret(1, 32, 14, 1, Cinza);
        return t.Salvar(PastaCasa, "Geladeira", Vector2.zero);
    }

    public static Sprite Fogao()
    {
        Tela t = new Tela(16, 26);
        t.Caixa(0, 6, 16, 20, Branco);
        t.Ret(1, 7, 14, 6, new Color32(60, 60, 70, 255));       // tampo
        t.Ret(2, 8, 4, 4, new Color32(90, 90, 100, 255));
        t.Ret(10, 8, 4, 4, new Color32(90, 90, 100, 255));
        // Panela com fumacinha
        t.Caixa(8, 3, 7, 7, new Color32(200, 204, 214, 255));
        t.Ret(9, 4, 5, 2, new Color32(250, 200, 120, 255));
        t.P(10, 0, Cinza); t.P(11, 1, Cinza); t.P(12, 0, Cinza);
        t.Caixa(2, 15, 12, 8, new Color32(60, 60, 70, 255));   // forno
        t.Ret(4, 17, 8, 3, new Color32(255, 160, 60, 255));
        for (int x = 3; x < 14; x += 4) t.Ret(x, 14, 2, 1, K);
        return t.Salvar(PastaCasa, "Fogao", Vector2.zero);
    }

    public static Sprite Pia()
    {
        Tela t = new Tela(32, 26);
        t.Ret(14, 0, 2, 6, CinzaEscuro);   // torneira
        t.Ret(14, 0, 5, 2, CinzaEscuro);
        t.Caixa(0, 6, 32, 8, Cinza);
        t.Caixa(4, 7, 20, 5, new Color32(150, 170, 196, 255));
        t.Ret(6, 8, 4, 1, Branco);
        t.Caixa(26, 4, 4, 5, new Color32(250, 120, 160, 255));   // detergente
        t.Caixa(0, 13, 32, 13, Movel);
        t.Ret(15, 14, 2, 11, MovelEscuro);
        t.Ret(12, 18, 2, 3, new Color32(250, 210, 80, 255));
        t.Ret(18, 18, 2, 3, new Color32(250, 210, 80, 255));
        return t.Salvar(PastaCasa, "Pia", Vector2.zero);
    }

    public static Sprite Balcao()
    {
        Tela t = new Tela(16, 26);
        t.Caixa(1, 2, 14, 6, new Color32(240, 240, 250, 255));   // fruteira
        t.Ret(3, 0, 4, 4, new Color32(250, 220, 60, 255));       // banana
        t.Ret(8, 1, 4, 4, new Color32(220, 40, 50, 255));        // maçã
        t.P(10, 0, new Color32(80, 160, 80, 255));
        t.Caixa(0, 6, 16, 8, Cinza);
        t.Caixa(0, 13, 16, 13, Movel);
        t.Ret(7, 18, 2, 3, new Color32(250, 210, 80, 255));
        return t.Salvar(PastaCasa, "Balcao", Vector2.zero);
    }

    public static Sprite Mesa()
    {
        Tela t = new Tela(32, 36);
        Color32 toalha = new Color32(250, 250, 250, 255), xadrez = new Color32(230, 80, 90, 255);
        t.Caixa(0, 4, 32, 24, toalha);
        for (int y = 5; y < 27; y++)
            for (int x = 1; x < 31; x++)
                if ((x / 4 + y / 4) % 2 == 0) t.P(x, y, xadrez);
        t.Ret(1, 26, 30, 2, Escuro(xadrez, 0.75f));
        // Café da manhã
        t.Caixa(5, 8, 5, 5, Branco); t.Ret(6, 9, 3, 2, new Color32(110, 60, 30, 255));
        t.Caixa(21, 15, 5, 5, Branco); t.Ret(22, 16, 3, 2, new Color32(110, 60, 30, 255));
        t.Caixa(12, 12, 9, 6, new Color32(240, 240, 240, 255));
        t.Ret(13, 13, 7, 3, new Color32(230, 170, 90, 255));     // pão
        t.Caixa(6, 18, 6, 4, new Color32(255, 255, 210, 255));   // bilhete
        t.P(10, 19, xadrez);
        // Pernas
        t.Caixa(2, 28, 3, 8, Movel);
        t.Caixa(27, 28, 3, 8, Movel);
        return t.Salvar(PastaCasa, "Mesa", Vector2.zero);
    }

    public static Sprite TV()
    {
        Tela t = new Tela(32, 30);
        t.Caixa(3, 0, 26, 18, new Color32(40, 40, 50, 255));
        t.Ret(5, 2, 22, 14, new Color32(90, 150, 220, 255));
        t.Ret(5, 12, 22, 4, new Color32(220, 40, 50, 255));      // tarja do jornal
        t.Ret(7, 13, 12, 1, Branco);
        t.Ret(13, 4, 6, 7, new Color32(240, 200, 150, 255));     // apresentador
        t.Ret(12, 9, 8, 3, new Color32(60, 60, 90, 255));
        t.Ret(14, 18, 4, 2, new Color32(40, 40, 50, 255));
        t.Caixa(0, 19, 32, 11, Movel);
        t.Ret(1, 20, 30, 2, MovelClaro);
        t.Borda(3, 23, 12, 5, MovelEscuro);
        t.Borda(17, 23, 12, 5, MovelEscuro);
        return t.Salvar(PastaCasa, "TV", Vector2.zero);
    }

    public static Sprite Sofa()
    {
        Tela t = new Tela(48, 24);
        Color32 tecido = new Color32(200, 70, 90, 255), escuro = new Color32(150, 44, 64, 255), claro = new Color32(230, 110, 126, 255);
        t.Caixa(0, 4, 48, 20, tecido);                   // encosto visto de trás
        t.Ret(1, 5, 46, 3, claro);
        t.Ret(16, 6, 1, 16, escuro);
        t.Ret(31, 6, 1, 16, escuro);
        t.Caixa(0, 0, 7, 24, escuro);                    // braços
        t.Caixa(41, 0, 7, 24, escuro);
        t.Ret(1, 1, 5, 2, tecido);
        t.Ret(42, 1, 5, 2, tecido);
        t.Ret(2, 22, 44, 1, K);
        return t.Salvar(PastaCasa, "Sofa", Vector2.zero);
    }

    public static Sprite Planta()
    {
        Tela t = new Tela(16, 28);
        Color32 folha = new Color32(70, 170, 80, 255), folhaEscura = new Color32(40, 120, 60, 255);
        int[,] folhas = { { 7, 0, 2, 10 }, { 3, 3, 3, 9 }, { 10, 2, 3, 10 }, { 1, 8, 4, 6 }, { 11, 7, 4, 7 }, { 5, 6, 6, 10 } };
        for (int i = 0; i < folhas.GetLength(0); i++)
            t.Caixa(folhas[i, 0], folhas[i, 1], folhas[i, 2], folhas[i, 3], i % 2 == 0 ? folha : folhaEscura);
        t.Caixa(3, 17, 10, 11, new Color32(206, 112, 70, 255));
        t.Ret(4, 18, 8, 2, new Color32(230, 140, 96, 255));
        return t.Salvar(PastaCasa, "Planta", Vector2.zero);
    }

    public static Sprite Sapateira()
    {
        Tela t = new Tela(16, 18);
        t.Caixa(0, 4, 16, 14, Movel);
        t.Ret(1, 10, 14, 1, MovelEscuro);
        t.Caixa(2, 1, 5, 4, new Color32(60, 160, 90, 255));      // chinelo do pai
        t.Caixa(8, 1, 5, 4, new Color32(60, 160, 90, 255));
        t.Caixa(2, 11, 6, 4, new Color32(220, 44, 44, 255));     // tênis do Jason
        t.Caixa(9, 12, 5, 3, new Color32(140, 70, 170, 255));    // sapato da mãe
        return t.Salvar(PastaCasa, "Sapateira", Vector2.zero);
    }

    // ======================================================================
    // Ajudantes
    // ======================================================================

    private static int Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 73856093 ^ y * 19349663;
            return Mathf.Abs(h % 997);
        }
    }

    private static Color32 Escuro(Color32 c, float f) =>
        new Color32((byte)(c.r * f), (byte)(c.g * f), (byte)(c.b * f), c.a);

    // Tela de desenho (y conta de cima para baixo)
    public class Tela
    {
        public readonly int L, A;
        public readonly Color32[] p;

        public Tela(int largura, int altura)
        {
            L = largura;
            A = altura;
            p = PixelArt.NovaTela(largura, altura);
        }

        public void P(int x, int y, Color32 c) => PixelArt.Pintar(p, L, A, x, y, c);

        public void Ret(int x, int y, int w, int h, Color32 c)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++) P(xx, yy, c);
        }

        public void Borda(int x, int y, int w, int h, Color32 c)
        {
            Ret(x, y, w, 1, c);
            Ret(x, y + h - 1, w, 1, c);
            Ret(x, y, 1, h, c);
            Ret(x + w - 1, y, 1, h, c);
        }

        // Retângulo preenchido com contorno escuro
        public void Caixa(int x, int y, int w, int h, Color32 preenchimento)
        {
            Ret(x, y, w, h, preenchimento);
            Borda(x, y, w, h, K);
        }

        public void Escurecer(int x, int y, int w, int h, float f)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                {
                    if (xx < 0 || xx >= L || yy < 0 || yy >= A) continue;
                    int i = (A - 1 - yy) * L + xx;
                    p[i] = Escuro(p[i], f);
                }
        }

        public Sprite Salvar(string pasta, string nome, Vector2 pivo) =>
            PixelArt.Salvar(pasta, nome, p, L, A, default, Px, pivo);
    }
}
