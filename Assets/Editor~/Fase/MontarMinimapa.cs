using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Minimapa estilo GTA: desenha as peças do radar (anel neon, máscara, seta, chegada, norte...) e monta o prefab.
// Menu: Mr Driver > Minimapa
public static class MontarMinimapa
{
    private const string Pasta = "Assets/Imagens/Minimapa";
    private const string CaminhoPrefab = "Assets/Prefabs/HUDMinimapa.prefab";
    private const string Moldura = "Assets/Imagens/HUD/HUDMoldura.png";
    private const float PixelMoldura = 4f;   // igual ao MontarHUD

    private static readonly Color32 Escuro = new Color32(24, 8, 36, 255);
    private static readonly Color32 Contorno = new Color32(18, 0, 24, 255);
    private static readonly Color32 Magenta = new Color32(255, 30, 205, 255);
    private static readonly Color32 Ciano = new Color32(45, 226, 230, 255);
    private static readonly Color32 Branco = new Color32(255, 255, 255, 255);

    // =====================================================================
    // Menu
    // =====================================================================

    [MenuItem("Mr Driver/Minimapa/Colocar neste nível")]
    private static void ColocarAqui()
    {
        if (Object.FindFirstObjectByType<Minimapa>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog("Minimapa", "Esta cena já tem um minimapa.", "OK");
            return;
        }

        GameObject obj = Colocar();
        EditorSceneManager.MarkSceneDirty(obj.scene);
        Selection.activeGameObject = obj;
        EditorUtility.DisplayDialog("Minimapa",
            "Minimapa colocado (canto inferior esquerdo)!\n\n" +
            "O mapa é montado sozinho quando o nível começa: ruas, calçadas, grama e casas.\n" +
            "Salve a cena com Ctrl+S.", "OK");
    }

    [MenuItem("Mr Driver/Minimapa/Colocar em todos os níveis")]
    private static void ColocarEmTodos()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string atual = EditorSceneManager.GetActiveScene().path;

        List<string> feitos = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            var cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);

            // Só níveis jogáveis (com o carro do jogador) e que ainda não têm minimapa
            if (Object.FindFirstObjectByType<CarroController>(FindObjectsInactive.Include) == null) continue;
            if (Object.FindFirstObjectByType<Minimapa>(FindObjectsInactive.Include) != null) continue;

            Colocar();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            feitos.Add(Path.GetFileNameWithoutExtension(caminho));
        }

        if (!string.IsNullOrEmpty(atual)) EditorSceneManager.OpenScene(atual, OpenSceneMode.Single);

        EditorUtility.DisplayDialog("Minimapa",
            feitos.Count == 0
                ? "Todos os níveis já tinham minimapa."
                : "Minimapa colocado e salvo em:\n• " + string.Join("\n• ", feitos), "OK");
    }

    [MenuItem("Mr Driver/Minimapa/Recriar prefab e imagens")]
    private static void Recriar()
    {
        if (EditorUtility.DisplayDialog("Minimapa",
                "Recria o prefab HUDMinimapa e as imagens do radar do zero (o que você mudou nele se perde).",
                "Recriar", "Cancelar"))
            CriarPrefab();
    }

    private static GameObject Colocar()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
        if (prefab == null) prefab = CriarPrefab();
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(obj, "Colocar minimapa");
        return obj;
    }

    // =====================================================================
    // Prefab
    // =====================================================================

    private static GameObject CriarPrefab()
    {
        Sprite mascaraSp = Salvar("RadarMascara", 512, DesenharCirculo);
        Sprite anelSp = Salvar("RadarAnel", 512, DesenharAnel);
        Sprite sombraSp = Salvar("RadarSombra", 512, DesenharSombra);
        Sprite vinhetaSp = Salvar("RadarVinheta", 512, DesenharVinheta);
        Sprite setaSp = Salvar("RadarJogador", 128, DesenharSeta);
        Sprite destinoSp = Salvar("RadarDestino", 128, DesenharDestino);
        Sprite brilhoSp = Salvar("RadarBrilho", 128, DesenharBrilho);
        Sprite norteSp = Salvar("RadarNorte", 128, DesenharNorte);
        Sprite pontoSp = Salvar("RadarPonto", 32, DesenharPonto);

        // Canvas próprio, abaixo do velocímetro (10) e dos outros HUDs
        GameObject canvasObj = new GameObject("HUDMinimapa", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9;
        CanvasScaler escala = canvasObj.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;

        // Raiz: canto inferior esquerdo (o velocímetro fica no direito)
        RectTransform raiz = Novo("Minimapa", canvasObj.transform);
        raiz.anchorMin = raiz.anchorMax = raiz.pivot = Vector2.zero;
        raiz.anchoredPosition = new Vector2(30f, 24f);
        raiz.sizeDelta = new Vector2(320f, 360f);
        CanvasGroup grupo = raiz.gameObject.AddComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        // Sombra suave embaixo do radar (um pouco deslocada)
        Imagem("Sombra", raiz, sombraSp, new Vector2(0.5f, 1f), new Vector2(6f, -168f), new Vector2(360f, 360f), Color.white);

        // Radar
        RectTransform radar = Novo("Radar", raiz);
        radar.anchorMin = radar.anchorMax = new Vector2(0.5f, 1f);
        radar.pivot = new Vector2(0.5f, 0.5f);
        radar.anchoredPosition = new Vector2(0f, -160f);
        radar.sizeDelta = new Vector2(300f, 300f);

        Image mascaraImg = Imagem("Mascara", radar, mascaraSp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(276f, 276f),
                                  new Color32(14, 8, 24, 255));
        Mask mascara = mascaraImg.gameObject.AddComponent<Mask>();
        mascara.showMaskGraphic = true;

        RectTransform giro = Novo("Giro", mascaraImg.rectTransform);
        giro.sizeDelta = Vector2.zero;

        GameObject mapaObj = new GameObject("Mapa", typeof(RectTransform), typeof(RawImage));
        mapaObj.transform.SetParent(giro, false);
        RawImage mapa = mapaObj.GetComponent<RawImage>();
        mapa.raycastTarget = false;
        mapa.rectTransform.sizeDelta = new Vector2(276f, 276f);

        GameObject rotaObj = new GameObject("RotaGPS", typeof(RectTransform), typeof(CanvasRenderer), typeof(LinhaUI));
        rotaObj.transform.SetParent(mapaObj.transform, false);
        LinhaUI rota = rotaObj.GetComponent<LinhaUI>();
        rota.color = Ciano;
        rota.raycastTarget = false;
        rota.rectTransform.sizeDelta = Vector2.zero;

        RectTransform pontos = Novo("Trafego", mapaObj.transform);
        pontos.sizeDelta = Vector2.zero;
        Image modeloPonto = Imagem("ModeloCarro", pontos, pontoSp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f),
                                   new Color32(235, 232, 250, 230));

        Image vinheta = Imagem("Vinheta", mascaraImg.rectTransform, vinhetaSp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(276f, 276f), Color.white);
        vinheta.raycastTarget = false;

        // Jogador: seta branca com brilho ciano por baixo
        RectTransform jogador = Novo("Jogador", mascaraImg.rectTransform);
        jogador.sizeDelta = new Vector2(34f, 34f);
        Imagem("Brilho", jogador, brilhoSp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f), new Color32(45, 226, 230, 140));
        Imagem("Seta", jogador, setaSp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f), Color.white);

        // Anel por cima do mapa
        Imagem("Anel", radar, anelSp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 300f), Color.white);

        // Chegada (fica presa na borda quando está longe)
        RectTransform destino = Novo("Chegada", radar);
        destino.sizeDelta = new Vector2(30f, 30f);
        Imagem("Brilho", destino, brilhoSp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f), new Color32(255, 30, 205, 150));
        Imagem("Marcador", destino, destinoSp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f), Color.white);

        // Norte
        Image norte = Imagem("Norte", radar, norteSp, new Vector2(0.5f, 0.5f), new Vector2(0f, 141f), new Vector2(34f, 34f), Color.white);
        TMP_Text n = Texto(norte.rectTransform, "N", 20f);
        Esticar(n.rectTransform);

        // Placa com o nome do nível e a distância até a chegada
        Image placa = Imagem("Placa", raiz, null, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(270f, 48f), Color.white);
        placa.rectTransform.pivot = new Vector2(0.5f, 0f);
        Sprite moldura = AssetDatabase.LoadAssetAtPath<Sprite>(Moldura);
        if (moldura != null)
        {
            placa.sprite = moldura;
            placa.type = Image.Type.Sliced;
            placa.pixelsPerUnitMultiplier = 1f / PixelMoldura;
        }
        else placa.color = new Color32(24, 8, 36, 235);
        TMP_Text texto = Texto(placa.rectTransform, "NÍVEL  120 m", 22f);
        Esticar(texto.rectTransform);
        texto.rectTransform.offsetMin = new Vector2(12f, 2f);
        texto.rectTransform.offsetMax = new Vector2(-12f, 0f);
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 14f;
        texto.fontSizeMax = 22f;

        // Script
        Minimapa minimapa = canvasObj.AddComponent<Minimapa>();
        SerializedObject dados = new SerializedObject(minimapa);
        dados.FindProperty("raiz").objectReferenceValue = raiz;
        dados.FindProperty("grupo").objectReferenceValue = grupo;
        dados.FindProperty("mascara").objectReferenceValue = mascaraImg.rectTransform;
        dados.FindProperty("giro").objectReferenceValue = giro;
        dados.FindProperty("mapa").objectReferenceValue = mapa;
        dados.FindProperty("rota").objectReferenceValue = rota;
        dados.FindProperty("pontos").objectReferenceValue = pontos;
        dados.FindProperty("modeloPonto").objectReferenceValue = modeloPonto;
        dados.FindProperty("jogador").objectReferenceValue = jogador;
        dados.FindProperty("destino").objectReferenceValue = destino;
        dados.FindProperty("norte").objectReferenceValue = norte.rectTransform;
        dados.FindProperty("raioNorte").floatValue = 141f;
        dados.FindProperty("texto").objectReferenceValue = texto;
        dados.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject dadosRota = new SerializedObject(rota);
        dadosRota.FindProperty("largura").floatValue = 5f;
        dadosRota.FindProperty("larguraContorno").floatValue = 2f;
        dadosRota.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasObj, CaminhoPrefab);
        Object.DestroyImmediate(canvasObj);
        return prefab;
    }

    private static RectTransform Novo(string nome, Transform pai)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        return obj.GetComponent<RectTransform>();
    }

    private static Image Imagem(string nome, Transform pai, Sprite sprite, Vector2 ancora, Vector2 posicao, Vector2 tamanho, Color cor)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(pai, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = ancora;
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
        Image img = obj.GetComponent<Image>();
        img.sprite = sprite;
        img.color = cor;
        img.raycastTarget = false;
        return img;
    }

    private static TMP_Text Texto(Transform pai, string conteudo, float tamanho)
    {
        GameObject obj = new GameObject("Texto", typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        TextMeshProUGUI t = obj.AddComponent<TextMeshProUGUI>();
        t.text = conteudo;
        t.fontSize = tamanho;
        t.fontStyle = FontStyles.Bold;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    private static void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // =====================================================================
    // Desenhos (suaves, sem pixel art: o radar gira e precisa de bordas lisas)
    // =====================================================================

    private delegate Color Pintor(float x, float y, float tamanho);

    private static Sprite Salvar(string nome, int tamanho, Pintor pintor)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Imagens")) AssetDatabase.CreateFolder("Assets", "Imagens");
        if (!AssetDatabase.IsValidFolder(Pasta)) AssetDatabase.CreateFolder("Assets/Imagens", "Minimapa");

        Texture2D tex = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
        Color[] px = new Color[tamanho * tamanho];
        for (int y = 0; y < tamanho; y++)
            for (int x = 0; x < tamanho; x++)
                px[y * tamanho + x] = pintor(x + 0.5f, y + 0.5f, tamanho);
        tex.SetPixels(px);
        tex.Apply();

        string caminho = Pasta + "/" + nome + ".png";
        File.WriteAllBytes(caminho, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);

        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.spritePixelsPerUnit = 100f;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
    }

    private static Color Cor(Color32 c, float alfa) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, Mathf.Clamp01(alfa));

    // Coloca "cima" por cima de "baixo" (mistura com transparência)
    private static Color Sobre(Color baixo, Color cima)
    {
        float a = cima.a + baixo.a * (1f - cima.a);
        if (a <= 0.0001f) return new Color(0f, 0f, 0f, 0f);
        Color c = (cima * cima.a + baixo * baixo.a * (1f - cima.a)) / a;
        c.a = a;
        return c;
    }

    private static float Faixa(float d, float de, float ate) => Mathf.Clamp01(d - de + 0.5f) * Mathf.Clamp01(ate - d + 0.5f);

    private static Color DesenharCirculo(float x, float y, float t)
    {
        float c = t * 0.5f, d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
        return Cor(Branco, Mathf.Clamp01(c - 1f - d + 0.5f));
    }

    // Anel escuro com borda neon (ciano em cima, magenta embaixo), risquinhos a cada 30° e brilho por fora
    private static Color DesenharAnel(float x, float y, float t)
    {
        float c = t * 0.5f;
        Vector2 v = new Vector2(x - c, y - c);
        float d = v.magnitude;
        float k = t / 512f;   // escala (desenhado pensando em 512)
        float angulo = Mathf.Atan2(v.y, v.x);
        Color neon = Color.Lerp(Cor(Magenta, 1f), Cor(Ciano, 1f), (Mathf.Sin(angulo) + 1f) * 0.5f);

        Color cor = new Color(0f, 0f, 0f, 0f);

        // Brilho para fora
        if (d > 250f * k)
        {
            float g = 1f - Mathf.Clamp01((d - 250f * k) / (6f * k));
            cor = new Color(neon.r, neon.g, neon.b, 0.45f * g * g);
        }

        // Corpo do anel
        float corpo = Faixa(d, 229f * k, 250f * k);
        if (corpo > 0f) cor = Sobre(cor, Cor(Escuro, corpo));

        // Borda neon de fora
        float borda = Faixa(d, 243f * k, 250f * k);
        if (borda > 0f) cor = Sobre(cor, new Color(neon.r, neon.g, neon.b, borda));

        // Linha fina de dentro
        float fina = Faixa(d, 229f * k, 231.5f * k);
        if (fina > 0f) cor = Sobre(cor, Cor(Ciano, fina * 0.85f));

        // Risquinhos a cada 30 graus
        float graus = Mathf.Repeat(angulo * Mathf.Rad2Deg, 30f);
        float arco = Mathf.Min(graus, 30f - graus) * Mathf.Deg2Rad * d;
        float risco = Faixa(d, 234f * k, 240f * k) * Mathf.Clamp01(2.2f * k - arco + 0.5f);
        if (risco > 0f) cor = Sobre(cor, new Color(0.8f, 0.76f, 0.92f, risco));

        return cor;
    }

    private static Color DesenharSombra(float x, float y, float t)
    {
        float c = t * 0.5f, d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
        float a = 1f - Mathf.SmoothStep(0.72f, 1f, d);
        return new Color(0f, 0f, 0.02f, 0.6f * a);
    }

    // Escurece as bordas do mapa (profundidade), linhas de "tela" bem leves e um reflexo de vidro em cima
    private static Color DesenharVinheta(float x, float y, float t)
    {
        float c = t * 0.5f;
        float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
        if (d > 1f) return new Color(0f, 0f, 0f, 0f);

        Color cor = new Color(0.04f, 0f, 0.08f, 0.6f * Mathf.SmoothStep(0.55f, 1f, d));
        if (Mathf.FloorToInt(y) % 4 == 0) cor = Sobre(cor, new Color(0f, 0f, 0f, 0.06f));

        // Reflexo: meia-lua clara no alto
        float reflexo = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(c * 0.75f, c * 1.65f)) / (c * 0.75f));
        float corte = Mathf.Clamp01((Vector2.Distance(new Vector2(x, y), new Vector2(c * 0.95f, c * 1.25f)) - c * 0.62f) / (c * 0.08f));
        cor = Sobre(cor, new Color(1f, 1f, 1f, 0.09f * reflexo * corte));
        return cor;
    }

    // Seta do jogador (ponta para cima), branca com contorno escuro
    private static readonly Vector2[] FormaSeta =
    {
        new Vector2(64f, 120f), new Vector2(110f, 14f), new Vector2(64f, 40f), new Vector2(18f, 14f),
    };

    private static Color DesenharSeta(float x, float y, float t) =>
        Poligono(new Vector2(x, y) * (128f / t), FormaSeta, Cor(Branco, 1f), Cor(Contorno, 1f), 9f);

    private static readonly Vector2[] FormaLosango =
    {
        new Vector2(64f, 122f), new Vector2(122f, 64f), new Vector2(64f, 6f), new Vector2(6f, 64f),
    };

    // Chegada: losango magenta com borda branca e "alvo" no meio
    private static Color DesenharDestino(float x, float y, float t)
    {
        Vector2 p = new Vector2(x, y) * (128f / t);
        Color cor = Poligono(p, FormaLosango, Cor(Magenta, 1f), Cor(Branco, 1f), 11f);
        float d = Vector2.Distance(p, new Vector2(64f, 64f));
        cor = Sobre(cor, Cor(Branco, Mathf.Clamp01(16f - d + 0.5f)));
        cor = Sobre(cor, Cor(Contorno, Mathf.Clamp01(8f - d + 0.5f)));
        return cor;
    }

    private static Color DesenharBrilho(float x, float y, float t)
    {
        float c = t * 0.5f, d = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c);
        float a = (1f - d) * (1f - d);
        return new Color(1f, 1f, 1f, a);
    }

    private static Color DesenharNorte(float x, float y, float t)
    {
        float c = t * 0.5f, d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) * (128f / t);
        Color cor = Cor(Magenta, Mathf.Clamp01(60f - d + 0.5f));
        return Sobre(cor, Cor(Escuro, Mathf.Clamp01(51f - d + 0.5f)));
    }

    private static Color DesenharPonto(float x, float y, float t)
    {
        float c = t * 0.5f, d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) * (32f / t);
        Color cor = Cor(Contorno, Mathf.Clamp01(15f - d + 0.5f));
        return Sobre(cor, Cor(Branco, Mathf.Clamp01(11f - d + 0.5f)));
    }

    // Polígono com contorno: dentro = preenchimento, perto da borda = contorno, com borda suave
    private static Color Poligono(Vector2 p, Vector2[] forma, Color preenchimento, Color contorno, float grossura)
    {
        bool dentro = false;
        float menor = float.MaxValue;
        for (int i = 0, j = forma.Length - 1; i < forma.Length; j = i++)
        {
            Vector2 a = forma[i], b = forma[j];
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) dentro = !dentro;

            Vector2 ab = b - a;
            float tt = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            menor = Mathf.Min(menor, Vector2.Distance(p, a + ab * tt));
        }

        if (!dentro)
        {
            // Contorno também um pouco para fora (deixa a forma "gordinha" e legível)
            float fora = Mathf.Clamp01(grossura * 0.35f - menor + 0.5f);
            return new Color(contorno.r, contorno.g, contorno.b, fora);
        }

        float mistura = Mathf.Clamp01(menor - grossura * 0.65f + 0.5f);
        return Color.Lerp(contorno, preenchimento, mistura);
    }
}
