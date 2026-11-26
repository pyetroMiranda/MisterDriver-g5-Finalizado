using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Desenha imagens em pixel art (no estilo dos botões do jogo: roxo escuro + borda magenta neon)
// e aplica nos botões de nível do Menu.
// Menu: Mr Driver > Níveis > Deixar botões de nível bonitos
public static class EstilizarBotoesNivel
{
    private const string Pasta = "Assets/Imagens/Niveis";

    // Tamanho da arte em pixels e quantos pixels de tela cada pixel da arte ocupa
    private const int Largura = 44;
    private const int Altura = 52;
    private const float Escala = 2.5f;          // botão final: 110 x 130
    private const float EscalaNumero = 7f;      // números bem grandes e pixelados

    // Cores (tiradas do BtnJogar / BotãoLoja)
    private static readonly Color32 Transparente = new Color32(0, 0, 0, 0);
    private static readonly Color32 Contorno = new Color32(18, 0, 24, 255);
    private static readonly Color32 Magenta = new Color32(255, 30, 205, 255);
    private static readonly Color32 MagentaEscuro = new Color32(110, 16, 100, 255);
    private static readonly Color32 FundoTopo = new Color32(42, 14, 56, 255);
    private static readonly Color32 FundoBase = new Color32(20, 6, 30, 255);
    private static readonly Color32 Branco = new Color32(255, 255, 255, 255);
    private static readonly Color32 Ciano = new Color32(45, 226, 230, 255);

    private static readonly Color32 CinzaBorda = new Color32(105, 88, 125, 255);
    private static readonly Color32 CinzaEscuro = new Color32(55, 45, 70, 255);
    private static readonly Color32 CinzaFundoTopo = new Color32(26, 20, 36, 255);
    private static readonly Color32 CinzaFundoBase = new Color32(14, 10, 20, 255);
    private static readonly Color32 CinzaTexto = new Color32(140, 128, 160, 255);
    private static readonly Color32 CinzaClaro = new Color32(205, 196, 220, 255);

    // ---------- Fonte pixel ----------

    private static readonly Dictionary<char, string[]> Letras = new Dictionary<char, string[]>
    {
        { 'N', new[] { "X..X", "XX.X", "X.XX", "X..X", "X..X" } },
        { 'I', new[] { "XXX", ".X.", ".X.", ".X.", "XXX" } },
        { 'V', new[] { "X..X", "X..X", "X..X", ".XX.", ".XX." } },
        { 'E', new[] { "XXXX", "X...", "XXX.", "X...", "XXXX" } },
        { 'L', new[] { "X...", "X...", "X...", "X...", "XXXX" } },
    };

    private static readonly string[][] Digitos =
    {
        new[] { ".XXX.", "X...X", "X..XX", "X.X.X", "XX..X", "X...X", ".XXX." },
        new[] { "..X..", ".XX..", "..X..", "..X..", "..X..", "..X..", ".XXX." },
        new[] { ".XXX.", "X...X", "....X", "...X.", "..X..", ".X...", "XXXXX" },
        new[] { "XXXXX", "...X.", "..X..", "...X.", "....X", "X...X", ".XXX." },
        new[] { "...X.", "..XX.", ".X.X.", "X..X.", "XXXXX", "...X.", "...X." },
        new[] { "XXXXX", "X....", "XXXX.", "....X", "....X", "X...X", ".XXX." },
        new[] { "..XX.", ".X...", "X....", "XXXX.", "X...X", "X...X", ".XXX." },
        new[] { "XXXXX", "....X", "...X.", "..X..", ".X...", ".X...", ".X..." },
        new[] { ".XXX.", "X...X", "X...X", ".XXX.", "X...X", "X...X", ".XXX." },
        new[] { ".XXX.", "X...X", "X...X", ".XXXX", "....X", "...X.", ".XX.." },
    };

    private static readonly string[] DesenhoCadeado =
    {
        "...XXXXX...",
        "..XX...XX..",
        "..X.....X..",
        "..X.....X..",
        "XXXXXXXXXXX",
        "XXXXXXXXXXX",
        "XXXXXXXXXXX",
        "XXXXX.XXXXX",
        "XXXX...XXXX",
        "XXXXX.XXXXX",
        "XXXXX.XXXXX",
        "XXXXXXXXXXX",
        "XXXXXXXXXXX",
    };

    private static readonly string[] DesenhoConcluido =
    {
        "........X",
        ".......XX",
        "X.....XX.",
        "XX...XX..",
        ".XX.XX...",
        "..XXX....",
        "...X.....",
    };

    // ---------- Comando do menu ----------

    [MenuItem("Mr Driver/Níveis/Deixar botões de nível bonitos")]
    private static void Estilizar()
    {
        MenuController menu = Object.FindFirstObjectByType<MenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.pnlNiveis == null)
        {
            EditorUtility.DisplayDialog("Botões de nível",
                "Não encontrei o MenuController com o Pnl Niveis.\n\nAbra a cena Menu e tente de novo.", "OK");
            return;
        }

        List<BotaoNivel> botoes = new List<BotaoNivel>();
        ScrollRect lista = null;
        foreach (ScrollRect scroll in menu.pnlNiveis.GetComponentsInChildren<ScrollRect>(true))
        {
            if (scroll.content == null) continue;
            foreach (Transform filho in scroll.content)
            {
                BotaoNivel b = filho.GetComponent<BotaoNivel>();
                if (b != null) { botoes.Add(b); lista = scroll; }
            }
            if (botoes.Count > 0) break;
        }

        if (botoes.Count == 0)
        {
            EditorUtility.DisplayDialog("Botões de nível",
                "Os botões ainda não têm o componente Botao Nivel.\n\n" +
                "Rode primeiro: Mr Driver > Níveis > Configurar botões de nível no Menu.", "OK");
            return;
        }

        // 1) Desenha e salva as imagens
        Sprite molduraLiberada = SalvarSprite("NivelMoldura", DesenharMoldura(false));
        Sprite molduraBloqueada = SalvarSprite("NivelMolduraBloqueada", DesenharMoldura(true));
        Sprite cadeado = SalvarSprite("NivelCadeado", DesenharIcone(DesenhoCadeado, CinzaClaro, Contorno));
        Sprite concluido = SalvarSprite("NivelConcluido", DesenharIcone(DesenhoConcluido, Ciano, Contorno));

        Undo.SetCurrentGroupName("Estilizar botões de nível");
        int grupo = Undo.GetCurrentGroup();

        // 2) Arruma a lista para os botões terem o tamanho certo
        AjustarLista(lista);

        // 3) Aplica em cada botão
        for (int i = 0; i < botoes.Count; i++)
        {
            Sprite numero = SalvarSprite("NivelNumero_" + (i + 1), DesenharNumero(i + 1));
            AplicarNoBotao(botoes[i], molduraLiberada, molduraBloqueada, cadeado, concluido, numero);
        }

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);

        Selection.activeGameObject = lista.gameObject;
        EditorUtility.DisplayDialog("Botões de nível",
            botoes.Count + " botões atualizados!\n\nAs imagens foram salvas em " + Pasta +
            ".\n\nAgora salve a cena com Ctrl+S.", "OK");
    }

    // ---------- Montagem na cena ----------

    private static void AjustarLista(ScrollRect lista)
    {
        RectTransform conteudo = lista.content;
        Undo.RecordObject(conteudo, "Ajustar lista");

        HorizontalLayoutGroup layout = conteudo.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = Undo.AddComponent<HorizontalLayoutGroup>(conteudo.gameObject);
        Undo.RecordObject(layout, "Ajustar layout");
        layout.spacing = 24;
        layout.padding = new RectOffset(12, 12, 4, 4);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter ajuste = conteudo.GetComponent<ContentSizeFitter>();
        if (ajuste == null) ajuste = Undo.AddComponent<ContentSizeFitter>(conteudo.gameObject);
        Undo.RecordObject(ajuste, "Ajustar tamanho");
        ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajuste.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Conteúdo preso à esquerda e com a altura toda da lista
        conteudo.anchorMin = new Vector2(0f, 0f);
        conteudo.anchorMax = new Vector2(0f, 1f);
        conteudo.pivot = new Vector2(0f, 0.5f);
        conteudo.anchoredPosition = Vector2.zero;
        conteudo.sizeDelta = new Vector2(conteudo.sizeDelta.x, 0f);

        Undo.RecordObject(lista, "Ajustar rolagem");
        lista.horizontal = true;
        lista.vertical = false;
        lista.movementType = ScrollRect.MovementType.Clamped;
    }

    private static void AplicarNoBotao(BotaoNivel nivel, Sprite liberada, Sprite bloqueada,
                                       Sprite cadeado, Sprite concluido, Sprite numero)
    {
        GameObject botao = nivel.gameObject;

        // Tamanho
        LayoutElement tamanho = botao.GetComponent<LayoutElement>();
        if (tamanho == null) tamanho = Undo.AddComponent<LayoutElement>(botao);
        Undo.RecordObject(tamanho, "Tamanho do botão");
        tamanho.minWidth = tamanho.preferredWidth = Largura * Escala;
        tamanho.minHeight = tamanho.preferredHeight = Altura * Escala;

        // Imagem de fundo
        Image imagem = botao.GetComponent<Image>();
        Undo.RecordObject(imagem, "Moldura");
        imagem.sprite = liberada;
        imagem.type = Image.Type.Simple;
        imagem.color = Color.white;
        imagem.preserveAspect = false;

        // Cores ao passar o mouse / clicar (o bloqueio já é mostrado pela moldura cinza)
        Button componente = botao.GetComponent<Button>();
        Undo.RecordObject(componente, "Cores do botão");
        ColorBlock cores = componente.colors;
        cores.normalColor = Color.white;
        cores.highlightedColor = new Color(1f, 0.82f, 1f);
        cores.pressedColor = new Color(0.75f, 0.55f, 0.75f);
        cores.selectedColor = Color.white;
        cores.disabledColor = Color.white;
        cores.fadeDuration = 0.08f;
        componente.colors = cores;
        componente.transition = Selectable.Transition.ColorTint;

        // Remove o texto antigo ("Level 1") e filhos criados antes por esta ferramenta
        for (int i = botao.transform.childCount - 1; i >= 0; i--)
        {
            GameObject filho = botao.transform.GetChild(i).gameObject;
            if (filho.GetComponent<TMP_Text>() != null || filho.name.StartsWith("Img"))
                Undo.DestroyObjectImmediate(filho);
        }

        // Número, cadeado e marca de concluído
        GameObject objNumero = CriarImagem(botao.transform, "ImgNumero", numero, EscalaNumero,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -14f));
        GameObject objCadeado = CriarImagem(botao.transform, "ImgCadeado", cadeado, 4.5f,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -14f));
        GameObject objConcluido = CriarImagem(botao.transform, "ImgConcluido", concluido, 3f,
            new Vector2(1f, 0f), new Vector2(-24f, 22f));

        objCadeado.SetActive(false);
        objConcluido.SetActive(false);

        // Liga tudo no BotaoNivel
        SerializedObject dados = new SerializedObject(nivel);
        dados.FindProperty("cadeado").objectReferenceValue = objCadeado;
        dados.FindProperty("marcaConcluido").objectReferenceValue = objConcluido;
        dados.FindProperty("numero").objectReferenceValue = objNumero;
        dados.FindProperty("moldura").objectReferenceValue = imagem;
        dados.FindProperty("molduraLiberada").objectReferenceValue = liberada;
        dados.FindProperty("molduraBloqueada").objectReferenceValue = bloqueada;
        dados.ApplyModifiedProperties();
    }

    private static GameObject CriarImagem(Transform pai, string nome, Sprite sprite, float escala,
                                          Vector2 ancora, Vector2 posicao)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(obj, "Criar " + nome);
        obj.transform.SetParent(pai, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = ancora;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = new Vector2(sprite.rect.width * escala, sprite.rect.height * escala);

        Image imagem = obj.GetComponent<Image>();
        imagem.sprite = sprite;
        imagem.raycastTarget = false;   // o clique vai para o botão
        return obj;
    }

    // ---------- Desenhos ----------

    private static Color32[] NovaTela(int largura, int altura)
    {
        Color32[] pixels = new Color32[largura * altura];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Transparente;
        return pixels;
    }

    // y conta de cima para baixo (mais fácil de pensar no desenho)
    private static void Pintar(Color32[] pixels, int largura, int altura, int x, int y, Color32 cor)
    {
        if (x < 0 || x >= largura || y < 0 || y >= altura) return;
        pixels[(altura - 1 - y) * largura + x] = cor;
    }

    private static Texture2D DesenharMoldura(bool bloqueada)
    {
        Color32[] p = NovaTela(Largura, Altura);
        Color32 borda = bloqueada ? CinzaBorda : Magenta;
        Color32 linhaInterna = bloqueada ? CinzaEscuro : MagentaEscuro;
        Color32 topo = bloqueada ? CinzaFundoTopo : FundoTopo;
        Color32 baseCor = bloqueada ? CinzaFundoBase : FundoBase;
        Color32 texto = bloqueada ? CinzaTexto : Branco;
        const int chanfro = 3;

        for (int y = 0; y < Altura; y++)
        {
            for (int x = 0; x < Largura; x++)
            {
                int dx = Mathf.Min(x, Largura - 1 - x);
                int dy = Mathf.Min(y, Altura - 1 - y);
                int camada = Mathf.Min(Mathf.Min(dx, dy), dx + dy - chanfro);
                if (camada < 0) continue;

                Color32 cor;
                if (camada == 0) cor = Contorno;
                else if (camada <= 2) cor = borda;
                else if (camada == 3) cor = linhaInterna;
                else cor = Color32.Lerp(topo, baseCor, y / (float)Altura);

                Pintar(p, Largura, Altura, x, y, cor);
            }
        }

        // "NÍVEL" no topo
        int larguraTexto = 0;
        foreach (char c in "NIVEL") larguraTexto += Letras[c][0].Length + 1;
        larguraTexto -= 1;
        int cursor = (Largura - larguraTexto) / 2;
        const int linhaTexto = 7;

        foreach (char c in "NIVEL")
        {
            string[] letra = Letras[c];
            for (int y = 0; y < letra.Length; y++)
                for (int x = 0; x < letra[y].Length; x++)
                    if (letra[y][x] == 'X') Pintar(p, Largura, Altura, cursor + x, linhaTexto + y, texto);

            if (c == 'I') Pintar(p, Largura, Altura, cursor + 2, linhaTexto - 2, texto);   // acento do Í
            cursor += letra[0].Length + 1;
        }

        // Linha separadora abaixo do texto
        for (int x = 7; x < Largura - 7; x++)
            Pintar(p, Largura, Altura, x, linhaTexto + 7, linhaInterna);

        return CriarTextura(p, Largura, Altura);
    }

    private static Texture2D DesenharNumero(int numero)
    {
        string texto = numero.ToString();
        int largura = texto.Length * 6 - 1 + 1;   // 5 por dígito + 1 de espaço, +1 da sombra
        int altura = 7 + 1;
        Color32[] p = NovaTela(largura, altura);

        // Primeiro a sombra magenta (deslocada 1 pixel), depois o número branco por cima
        for (int passada = 0; passada < 2; passada++)
        {
            int desloc = passada == 0 ? 1 : 0;
            Color32 cor = passada == 0 ? Magenta : Branco;

            for (int d = 0; d < texto.Length; d++)
            {
                string[] digito = Digitos[texto[d] - '0'];
                for (int y = 0; y < 7; y++)
                    for (int x = 0; x < 5; x++)
                        if (digito[y][x] == 'X')
                            Pintar(p, largura, altura, d * 6 + x + desloc, y + desloc, cor);
            }
        }

        return CriarTextura(p, largura, altura);
    }

    // Ícone com contorno escuro de 1 pixel em volta
    private static Texture2D DesenharIcone(string[] desenho, Color32 cor, Color32 contorno)
    {
        int largura = desenho[0].Length + 2;
        int altura = desenho.Length + 2;
        Color32[] p = NovaTela(largura, altura);

        for (int y = 0; y < desenho.Length; y++)
            for (int x = 0; x < desenho[y].Length; x++)
                if (desenho[y][x] == 'X')
                    for (int oy = -1; oy <= 1; oy++)
                        for (int ox = -1; ox <= 1; ox++)
                            Pintar(p, largura, altura, x + 1 + ox, y + 1 + oy, contorno);

        for (int y = 0; y < desenho.Length; y++)
            for (int x = 0; x < desenho[y].Length; x++)
                if (desenho[y][x] == 'X')
                    Pintar(p, largura, altura, x + 1, y + 1, cor);

        // Buraco da fechadura / vãos ficam escuros dentro do ícone
        for (int y = 0; y < desenho.Length; y++)
            for (int x = 0; x < desenho[y].Length; x++)
                if (desenho[y][x] == '.' && DentroDoCorpo(desenho, x, y))
                    Pintar(p, largura, altura, x + 1, y + 1, contorno);

        return CriarTextura(p, largura, altura);
    }

    // Um '.' cercado de 'X' na mesma linha (ex.: buraco da fechadura)
    private static bool DentroDoCorpo(string[] desenho, int x, int y)
    {
        string linha = desenho[y];
        return linha.IndexOf('X') < x && linha.LastIndexOf('X') > x && y >= 4;
    }

    private static Texture2D CriarTextura(Color32[] pixels, int largura, int altura)
    {
        Texture2D tex = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    // ---------- Salvar como sprite pixelado ----------

    private static Sprite SalvarSprite(string nome, Texture2D textura)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Imagens")) AssetDatabase.CreateFolder("Assets", "Imagens");
        if (!AssetDatabase.IsValidFolder(Pasta)) AssetDatabase.CreateFolder("Assets/Imagens", "Niveis");

        string caminho = Pasta + "/" + nome + ".png";
        File.WriteAllBytes(caminho, textura.EncodeToPNG());
        Object.DestroyImmediate(textura);
        AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);

        TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(caminho);
        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.filterMode = FilterMode.Point;                      // pixels nítidos
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.mipmapEnabled = false;
        importador.alphaIsTransparency = true;
        importador.spritePixelsPerUnit = 100;
        importador.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
    }
}
