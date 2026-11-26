using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Cria o prefab do velocímetro (uma vez) e coloca na cena aberta.
// Menu: Mr Driver > HUD > Colocar velocímetro nesta cena
public static class MontarVelocimetro
{
    private const string CaminhoPrefab = "Assets/Prefabs/HUDVelocimetro.prefab";
    private const string CaminhoMostrador = "Assets/Imagens/IconeVelocimetro.png";
    private const string PastaImagens = "Assets/Imagens/HUD";

    // Centro do arco dentro da imagem IconeVelocimetro.png (0 a 1)
    private static readonly Vector2 CentroDoArco = new Vector2(0.509f, 0.508f);

    [MenuItem("Mr Driver/HUD/Colocar velocímetro nesta cena")]
    private static void Colocar()
    {
        if (Object.FindFirstObjectByType<CarroController>(FindObjectsInactive.Include) == null)
        {
            EditorUtility.DisplayDialog("Velocímetro",
                "Esta cena não tem o carro do jogador.\n\nAbra o Tutorial ou o Nivel1 e tente de novo.", "OK");
            return;
        }

        if (Object.FindFirstObjectByType<Velocimetro>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog("Velocímetro", "Esta cena já tem o velocímetro novo.", "OK");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
        if (prefab == null) prefab = CriarPrefab();
        if (prefab == null) return;

        Undo.SetCurrentGroupName("Colocar velocímetro");
        int grupo = Undo.GetCurrentGroup();

        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instancia, "Colocar velocímetro");

        // Esconde o velocímetro antigo (não apaga)
        int antigos = 0;
        foreach (RectTransform rt in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (rt.name != "pnlVelocimetro" || !rt.gameObject.activeSelf) continue;
            Undo.RecordObject(rt.gameObject, "Esconder velocímetro antigo");
            rt.gameObject.SetActive(false);
            antigos++;
        }

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(instancia.scene);
        Selection.activeGameObject = instancia;

        EditorUtility.DisplayDialog("Velocímetro",
            "Velocímetro colocado no canto inferior direito!" +
            (antigos > 0 ? "\n\nO velocímetro antigo (pnlVelocimetro) foi desligado." : "") +
            "\n\nAgora salve a cena com Ctrl+S.", "OK");
    }

    [MenuItem("Mr Driver/HUD/Recriar prefab do velocímetro")]
    private static void Recriar()
    {
        if (!EditorUtility.DisplayDialog("Velocímetro",
                "Isso recria o prefab HUDVelocimetro do zero (o que você mudou nele se perde). " +
                "As cenas que já usam o velocímetro recebem a versão nova.", "Recriar", "Cancelar"))
            return;

        CriarPrefab();
    }

    // ---------- Prefab ----------

    private static GameObject CriarPrefab()
    {
        Sprite mostrador = CarregarSprite(CaminhoMostrador);
        if (mostrador == null)
        {
            EditorUtility.DisplayDialog("Velocímetro", "Não encontrei a imagem " + CaminhoMostrador + ".", "OK");
            return null;
        }

        Sprite agulha = DesenharPonteiro();
        Sprite centro = DesenharCentro();

        // Canvas próprio: fica igual em qualquer nível, não importa como o HUD da cena foi feito
        GameObject raiz = new GameObject("HUDVelocimetro", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;   // abaixo das dicas do tutorial (50)
        CanvasScaler escala = raiz.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;

        // Mostrador no canto inferior direito
        GameObject painel = new GameObject("Velocimetro", typeof(RectTransform), typeof(Image));
        painel.transform.SetParent(raiz.transform, false);
        RectTransform rtPainel = painel.GetComponent<RectTransform>();
        rtPainel.anchorMin = rtPainel.anchorMax = rtPainel.pivot = new Vector2(1f, 0f);
        rtPainel.anchoredPosition = new Vector2(-30f, 24f);
        float largura = 300f;
        rtPainel.sizeDelta = new Vector2(largura, largura * mostrador.rect.height / mostrador.rect.width);
        Image imgMostrador = painel.GetComponent<Image>();
        imgMostrador.sprite = mostrador;
        imgMostrador.preserveAspect = true;
        imgMostrador.raycastTarget = false;

        // Ponteiro (gira em volta do centro do arco)
        GameObject ponteiro = Imagem(painel.transform, "Ponteiro", agulha, 2.75f);
        RectTransform rtPonteiro = ponteiro.GetComponent<RectTransform>();
        rtPonteiro.pivot = new Vector2(0.5f, 6f / agulha.rect.height);
        rtPonteiro.localRotation = Quaternion.Euler(0f, 0f, 135f);

        Imagem(painel.transform, "Centro", centro, 3f);

        // Textos (por cima do ponteiro)
        TMP_Text numero = Texto(painel.transform, "TxtVelocidade", "0", 56, FontStyles.Bold, Color.white,
                                new Vector2(0.5f, 0.27f), new Vector2(200f, 64f));
        TMP_Text unidade = Texto(painel.transform, "TxtUnidade", "km/h", 22, FontStyles.Bold,
                                 new Color(0.86f, 0.83f, 0.93f), new Vector2(0.5f, 0.14f), new Vector2(200f, 30f));

        Velocimetro velocimetro = painel.AddComponent<Velocimetro>();
        SerializedObject dados = new SerializedObject(velocimetro);
        dados.FindProperty("ponteiro").objectReferenceValue = rtPonteiro;
        dados.FindProperty("textoVelocidade").objectReferenceValue = numero;
        dados.FindProperty("textoUnidade").objectReferenceValue = unidade;
        dados.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    private static GameObject Imagem(Transform pai, string nome, Sprite sprite, float escala)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(pai, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = CentroDoArco;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(sprite.rect.width * escala, sprite.rect.height * escala);
        Image imagem = obj.GetComponent<Image>();
        imagem.sprite = sprite;
        imagem.raycastTarget = false;
        return obj;
    }

    private static TMP_Text Texto(Transform pai, string nome, string conteudo, float tamanho, FontStyles estilo,
                                  Color cor, Vector2 ancora, Vector2 tamanhoCaixa)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        TextMeshProUGUI texto = obj.AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        texto.color = cor;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = ancora;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = tamanhoCaixa;
        return texto;
    }

    private static Sprite CarregarSprite(string caminho)
    {
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(caminho))
            if (obj is Sprite sprite) return sprite;
        return null;
    }

    // ---------- Desenhos em pixel art ----------

    // Agulha: ponta magenta, corpo branco, contorno escuro; a base fica 6 pixels acima do fundo
    private static Sprite DesenharPonteiro()
    {
        const int largura = 7, altura = 40, meio = 3;
        Color32[] p = PixelArt.NovaTela(largura, altura);

        for (int y = 0; y < altura; y++)
        {
            int metade = y < 6 ? 1 : (y < 32 ? 2 : 3);
            for (int x = meio - metade; x <= meio + metade; x++)
            {
                bool borda = Mathf.Abs(x - meio) == metade || y == 0 || y == altura - 1;
                Color32 cor = borda ? PixelArt.Contorno : (y < 6 ? PixelArt.Magenta : PixelArt.Branco);
                PixelArt.Pintar(p, largura, altura, x, y, cor);
            }
        }

        return PixelArt.Salvar(PastaImagens, "PonteiroVelocimetro", p, largura, altura);
    }

    // Tampinha redonda no centro do ponteiro
    private static Sprite DesenharCentro()
    {
        const int tamanho = 11;
        Color32[] p = PixelArt.NovaTela(tamanho, tamanho);
        float c = (tamanho - 1) / 2f;

        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                if (d > 5.3f) continue;
                Color32 cor = d > 4.3f ? PixelArt.Contorno : (d > 2.6f ? PixelArt.Magenta : PixelArt.Fundo);
                PixelArt.Pintar(p, tamanho, tamanho, x, y, cor);
            }
        }

        return PixelArt.Salvar(PastaImagens, "CentroVelocimetro", p, tamanho, tamanho);
    }
}
