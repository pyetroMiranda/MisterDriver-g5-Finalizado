using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Ajudantes para desenhar imagens em pixel art pelo código e salvar como sprite.
public static class PixelArt
{
    // ---------- Cores do jogo (tiradas do BtnJogar / BotãoLoja) ----------
    public static readonly Color32 Transparente = new Color32(0, 0, 0, 0);
    public static readonly Color32 Contorno = new Color32(18, 0, 24, 255);
    public static readonly Color32 Magenta = new Color32(255, 30, 205, 255);
    public static readonly Color32 MagentaEscuro = new Color32(110, 16, 100, 255);
    public static readonly Color32 Fundo = new Color32(24, 8, 36, 240);
    public static readonly Color32 Branco = new Color32(255, 255, 255, 255);
    public static readonly Color32 TeclaFace = new Color32(242, 236, 250, 255);
    public static readonly Color32 TeclaBrilho = new Color32(255, 255, 255, 255);
    public static readonly Color32 TeclaLado = new Color32(150, 135, 175, 255);
    public static readonly Color32 TeclaTexto = new Color32(42, 14, 56, 255);

    // ---------- Fonte pixel (5 linhas; uma 6ª linha fica abaixo da base, ex.: cedilha) ----------
    public static readonly Dictionary<char, string[]> Fonte = new Dictionary<char, string[]>
    {
        { 'A', new[] { ".XX.", "X..X", "XXXX", "X..X", "X..X" } },
        { 'B', new[] { "XXX.", "X..X", "XXX.", "X..X", "XXX." } },
        { 'C', new[] { ".XXX", "X...", "X...", "X...", ".XXX" } },
        { 'G', new[] { ".XXX", "X...", "X.XX", "X..X", ".XXX" } },
        { 'J', new[] { "..XX", "...X", "...X", "X..X", ".XX." } },
        { 'K', new[] { "X..X", "X.X.", "XX..", "X.X.", "X..X" } },
        { 'Ç', new[] { ".XXX", "X...", "X...", "X...", ".XXX", "..X." } },
        { 'D', new[] { "XXX.", "X..X", "X..X", "X..X", "XXX." } },
        { 'E', new[] { "XXXX", "X...", "XXX.", "X...", "XXXX" } },
        { 'H', new[] { "X..X", "X..X", "XXXX", "X..X", "X..X" } },
        { 'I', new[] { "XXX", ".X.", ".X.", ".X.", "XXX" } },
        { 'L', new[] { "X...", "X...", "X...", "X...", "XXXX" } },
        { 'M', new[] { "X...X", "XX.XX", "X.X.X", "X...X", "X...X" } },
        { 'N', new[] { "X..X", "XX.X", "X.XX", "X..X", "X..X" } },
        { 'O', new[] { ".XX.", "X..X", "X..X", "X..X", ".XX." } },
        { 'P', new[] { "XXX.", "X..X", "XXX.", "X...", "X..." } },
        { 'R', new[] { "XXX.", "X..X", "XXX.", "X.X.", "X..X" } },
        { 'S', new[] { ".XXX", "X...", ".XX.", "...X", "XXX." } },
        { 'T', new[] { "XXX", ".X.", ".X.", ".X.", ".X." } },
        { 'V', new[] { "X..X", "X..X", "X..X", ".XX.", ".XX." } },
        { 'W', new[] { "X...X", "X...X", "X.X.X", "XX.XX", "X...X" } },
        { '^', new[] { "..X..", ".XXX.", "X.X.X", "..X..", "..X.." } },   // seta para cima
        { 'v', new[] { "..X..", "..X..", "X.X.X", ".XXX.", "..X.." } },   // seta para baixo
        { '<', new[] { "..X..", ".X...", "XXXXX", ".X...", "..X.." } },   // seta para a esquerda
        { '>', new[] { "..X..", "...X.", "XXXXX", "...X.", "..X.." } },   // seta para a direita
    };

    public static int LarguraTexto(string texto)
    {
        int largura = 0;
        foreach (char c in texto) largura += Fonte[c][0].Length + 1;
        return Mathf.Max(0, largura - 1);
    }

    public static void DesenharTexto(Color32[] p, int largura, int altura, string texto, int x, int y, Color32 cor)
    {
        foreach (char c in texto)
        {
            string[] letra = Fonte[c];
            for (int ly = 0; ly < letra.Length; ly++)
                for (int lx = 0; lx < letra[ly].Length; lx++)
                    if (letra[ly][lx] == 'X') Pintar(p, largura, altura, x + lx, y + ly, cor);
            x += letra[0].Length + 1;
        }
    }

    // ---------- Tela ----------

    public static Color32[] NovaTela(int largura, int altura)
    {
        Color32[] pixels = new Color32[largura * altura];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Transparente;
        return pixels;
    }

    // y conta de cima para baixo
    public static void Pintar(Color32[] p, int largura, int altura, int x, int y, Color32 cor)
    {
        if (x < 0 || x >= largura || y < 0 || y >= altura) return;
        p[(altura - 1 - y) * largura + x] = cor;
    }

    // Distância até a borda, com os cantos cortados (chanfro). Negativo = fora da forma.
    public static int Camada(int x, int y, int largura, int altura, int chanfro)
    {
        int dx = Mathf.Min(x, largura - 1 - x);
        int dy = Mathf.Min(y, altura - 1 - y);
        return Mathf.Min(Mathf.Min(dx, dy), dx + dy - chanfro);
    }

    // Caixa com cantos cortados (contorno, borda de 2px, linha interna, preenchimento), pronta para "Sliced"
    public static Sprite Caixa(string pasta, string nome, int tamanho, int chanfro,
                               Color32 borda, Color32 linhaInterna, Color32 preenchimento)
    {
        Color32[] p = NovaTela(tamanho, tamanho);
        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                int camada = Camada(x, y, tamanho, tamanho, chanfro);
                if (camada < 0) continue;

                Color32 cor;
                if (camada == 0) cor = Contorno;
                else if (camada <= 2) cor = borda;
                else if (camada == 3) cor = linhaInterna;
                else cor = preenchimento;

                Pintar(p, tamanho, tamanho, x, y, cor);
            }
        }

        float b = Mathf.Floor(tamanho / 3f);
        return Salvar(pasta, nome, p, tamanho, tamanho, new Vector4(b, b, b, b));
    }

    // Desenho a partir de linhas de texto: cada letra é uma cor da paleta (' ' ou '.' = transparente)
    public static Sprite Desenho(string pasta, string nome, string[] linhas, Dictionary<char, Color32> paleta,
                                 float pixelsPorUnidade = 100f)
    {
        int altura = linhas.Length;
        int largura = linhas[0].Length;
        Color32[] p = NovaTela(largura, altura);

        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura && x < linhas[y].Length; x++)
                if (paleta.TryGetValue(linhas[y][x], out Color32 cor))
                    Pintar(p, largura, altura, x, y, cor);

        return Salvar(pasta, nome, p, largura, altura, default, pixelsPorUnidade);
    }

    // ---------- Salvar ----------

    // borda = pixels que não esticam (esquerda, baixo, direita, cima), para imagens "Sliced"
    // pivo = ponto de apoio do sprite de 0 a 1 (vazio = centro)
    public static Sprite Salvar(string pasta, string nome, Color32[] pixels, int largura, int altura,
                                Vector4 borda = default, float pixelsPorUnidade = 100f, Vector2? pivo = null)
    {
        string[] partes = pasta.Split('/');
        string atual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string proxima = atual + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(proxima)) AssetDatabase.CreateFolder(atual, partes[i]);
            atual = proxima;
        }

        Texture2D tex = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();

        string caminho = pasta + "/" + nome + ".png";
        File.WriteAllBytes(caminho, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);

        TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(caminho);
        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.filterMode = FilterMode.Point;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.mipmapEnabled = false;
        importador.alphaIsTransparency = true;
        importador.spritePixelsPerUnit = pixelsPorUnidade;
        importador.spriteBorder = borda;
        if (pivo.HasValue)
        {
            TextureImporterSettings ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);
            ajustes.spriteAlignment = (int)SpriteAlignment.Custom;
            ajustes.spritePivot = pivo.Value;
            importador.SetTextureSettings(ajustes);
        }
        importador.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
    }
}
