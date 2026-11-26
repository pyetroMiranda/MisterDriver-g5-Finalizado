using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Cria a cena Historia_Videogame: o TURBO RACER 3000 (© 1987 MEGASOFT), o videogame que o Jason está jogando
// quando a história começa. Tudo é interface (uma "tela" de 160x120 pixels ampliada 8x no meio da tela),
// com efeito de TV de tubo (linhas, cantos escuros, brilho no vidro) e o reflexo do rosto do Jason, bem fraquinho.
// Menu: Mr Driver > História > 0. Criar o videogame (TURBO RACER 3000)
public static partial class MontarHistoria
{
    private const string CaminhoVideogame = "Assets/Scenes/" + ProgressoHistoria.CenaVideogame + ".unity";

    [MenuItem("Mr Driver/História/0. Criar o videogame (TURBO RACER 3000)", priority = 19)]
    private static void MenuVideogame()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (CriarVideogame())
            EditorUtility.DisplayDialog("História", "Cena " + ProgressoHistoria.CenaVideogame + " criada!\n\n" +
                "O TURBO RACER 3000 abre pelo JOGAR (antes da casa) e pelo nível 1.\nAperte Play nela para testar.", "OK");
    }

    private static bool CriarVideogame()
    {
        CarregarPersonagens();
        FontePixel fonte = HistoriaArte.CriarFontePixel();
        Sprite[] carros = HistoriaArte.CarrosRuins();
        Sprite caminhao = HistoriaArte.CaminhaoRuim();
        Sprite[] explosao = HistoriaArte.ExplosaoRuim();
        Sprite grama = HistoriaArte.GramaRuim(), faixa = HistoriaArte.FaixaRuim();
        Sprite arvore = HistoriaArte.ArvoreRuim(), arbusto = HistoriaArte.ArbustoRuim(), placa = HistoriaArte.PlacaRuim();
        Sprite linhas = HistoriaArte.LinhasTV(), vinheta = HistoriaArte.VinhetaTV(), brilho = HistoriaArte.BrilhoTV();
        Sprite branco = HistoriaArte.PixelBranco();

        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Câmera (só fundo preto + ouvido para os sons)
        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camObj.tag = "MainCamera";
        Camera cam = camObj.GetComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camObj.transform.position = new Vector3(0f, 0f, -10f);

        // Canvas: 1920x1080, a "tela" do videogame no meio
        GameObject canvasObj = new GameObject("TurboRacer3000", typeof(Canvas), typeof(CanvasScaler));
        canvasObj.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler escala = canvasObj.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 1f;

        Imagem(canvasObj.transform, "Fundo", branco, Color.black, Vector2.zero, Vector2.zero, true);

        RectTransform tela = Ret(canvasObj.transform, "Tela", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                 Vector2.zero, new Vector2(160f, 120f));
        tela.localScale = Vector3.one * 8.4f;   // 160x120 → 1344x1008
        tela.gameObject.AddComponent<RectMask2D>();

        // ---------- Cenário: grama, asfalto, linhas ----------
        RectTransform gramaE = Lado(tela, "GramaEsquerda", grama, 0f, 44f);
        RectTransform gramaD = Lado(tela, "GramaDireita", grama, 116f, 44f);
        Imagem(tela, "Asfalto", branco, new Color32(70, 70, 78, 255), new Vector2(80f, 60f), new Vector2(72f, 120f));
        Imagem(tela, "BordaEsquerda", branco, new Color32(240, 240, 240, 255), new Vector2(45.5f, 60f), new Vector2(1f, 120f));
        Imagem(tela, "BordaDireita", branco, new Color32(240, 240, 240, 255), new Vector2(114.5f, 60f), new Vector2(1f, 120f));
        List<RectTransform> faixas = new List<RectTransform>
        {
            Tracejado(tela, "Faixa1", faixa, 68f),
            Tracejado(tela, "Faixa2", faixa, 92f),
        };

        // Árvores, arbustos e placas que passam pela grama
        List<RectTransform> enfeites = new List<RectTransform>();
        Sprite[] tipos = { arvore, arbusto, placa, arvore, arbusto, arvore, arvore, arbusto };
        float[] xs = { 18f, 30f, 132f, 146f, 10f, 128f, 36f, 150f };
        for (int i = 0; i < tipos.Length; i++)
        {
            Image e = Imagem(tela, "Enfeite " + (i + 1), tipos[i], Color.white, new Vector2(xs[i], 12f + i * 17f),
                             new Vector2(tipos[i].rect.width, tipos[i].rect.height));
            enfeites.Add(e.rectTransform);
        }

        RectTransform objetos = Ret(tela, "Carros", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        objetos.anchorMin = Vector2.zero;
        objetos.anchorMax = Vector2.one;
        objetos.sizeDelta = Vector2.zero;

        Image jogador = Imagem(tela, "Jogador", carros[0], Color.white, new Vector2(80f, 20f), new Vector2(14f, 22f));
        Image boom = Imagem(tela, "Explosao", explosao[0], Color.white, new Vector2(80f, 22f), new Vector2(24f, 24f));
        boom.gameObject.SetActive(false);

        // ---------- Placar ----------
        TextoPixel txtPontos = Texto(tela, "TxtPontos", fonte, "PONTOS 000000", Color.white, 1f, TextoPixel.Alinhamento.Esquerda, new Vector2(3f, 113f));
        TextoPixel txtRecorde = Texto(tela, "TxtRecorde", fonte, "1. TONHÃO 99999", Color.white, 1f, TextoPixel.Alinhamento.Direita, new Vector2(157f, 113f));
        TextoPixel txtCentro = Texto(tela, "TxtCentro", fonte, "", Color.white, 2f, TextoPixel.Alinhamento.Centro, new Vector2(80f, 68f));
        TextoPixel txtSub = Texto(tela, "TxtSub", fonte, "", Color.white, 1f, TextoPixel.Alinhamento.Centro, new Vector2(80f, 50f));
        TextoPixel txtDica = Texto(tela, "TxtDica", fonte, "", Color.white, 1f, TextoPixel.Alinhamento.Centro, new Vector2(80f, 38f));

        // ---------- Título ----------
        GameObject titulo = Ret(tela, "PainelTitulo", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
        Imagem(titulo.transform, "Escurecer", branco, new Color(0f, 0f, 0.05f, 0.62f), Vector2.zero, Vector2.zero, true);
        Texto(titulo.transform, "SombraTurbo", fonte, "TURBO", new Color32(170, 30, 30, 255), 4f, TextoPixel.Alinhamento.Centro, new Vector2(81f, 89f));
        Texto(titulo.transform, "Turbo", fonte, "TURBO", new Color32(255, 220, 60, 255), 4f, TextoPixel.Alinhamento.Centro, new Vector2(80f, 90f));
        Texto(titulo.transform, "Racer", fonte, "RACER 3000", new Color32(255, 120, 40, 255), 2f, TextoPixel.Alinhamento.Centro, new Vector2(80f, 66f));
        Image carroTitulo = Imagem(titulo.transform, "CarroTitulo", carros[0], Color.white, new Vector2(80f, 46f), new Vector2(14f, 22f));
        carroTitulo.rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f);
        TextoPixel txtStart = Texto(titulo.transform, "TxtStart", fonte, "PRESS START", Color.white, 1f, TextoPixel.Alinhamento.Centro, new Vector2(80f, 30f));
        TextoPixel txtRecordeTitulo = Texto(titulo.transform, "TxtJogadores", fonte, "1 JOGADOR", new Color32(90, 230, 255, 255), 1f,
                                            TextoPixel.Alinhamento.Centro, new Vector2(80f, 20f));
        Texto(titulo.transform, "TxtCopyright", fonte, "(C) 1987 MEGASOFT", new Color32(170, 170, 170, 255), 1f,
              TextoPixel.Alinhamento.Centro, new Vector2(80f, 8f));

        // ---------- Carregando ----------
        GameObject carregando = Ret(tela, "PainelCarregando", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
        Imagem(carregando.transform, "Fundo", branco, new Color32(10, 10, 40, 255), Vector2.zero, Vector2.zero, true);
        Texto(carregando.transform, "TxtCarregando", fonte, "CARREGANDO...", Color.white, 1f, TextoPixel.Alinhamento.Centro, new Vector2(80f, 72f));
        Imagem(carregando.transform, "MolduraBarra", branco, Color.white, new Vector2(80f, 56f), new Vector2(82f, 8f));
        Imagem(carregando.transform, "FundoBarra", branco, new Color32(10, 10, 40, 255), new Vector2(80f, 56f), new Vector2(80f, 6f));
        Image barra = Imagem(carregando.transform, "Barra", branco, new Color32(90, 230, 255, 255), new Vector2(41f, 56f), new Vector2(0f, 4f));
        barra.rectTransform.pivot = new Vector2(0f, 0.5f);
        Texto(carregando.transform, "TxtFita", fonte, "NÃO DESLIGUE O APARELHO", new Color32(170, 170, 170, 255), 1f,
              TextoPixel.Alinhamento.Centro, new Vector2(80f, 40f));
        carregando.SetActive(false);

        // ---------- TV de tubo por cima de tudo ----------
        Image reflexo = Imagem(tela, "ReflexoDoJason", HistoriaTextos.Jason != null ? HistoriaTextos.Jason.retrato : null,
                               new Color(0.75f, 0.85f, 1f, 0.03f), new Vector2(84f, 58f), new Vector2(90f, 90f));
        reflexo.rectTransform.localScale = new Vector3(-1f, 1f, 1f);   // espelhado, como reflexo
        if (reflexo.sprite == null) reflexo.gameObject.SetActive(false);

        Image linhasTV = Imagem(tela, "LinhasDaTV", linhas, Color.white, Vector2.zero, Vector2.zero, true);
        linhasTV.type = Image.Type.Tiled;
        linhasTV.pixelsPerUnitMultiplier = 4f;   // linhas finas: 2 por pixel do jogo
        Image cantos = Imagem(tela, "CantosDaTV", vinheta, Color.white, Vector2.zero, Vector2.zero, true);
        cantos.type = Image.Type.Sliced;
        cantos.pixelsPerUnitMultiplier = 1f;
        Image vidro = Imagem(tela, "BrilhoDoVidro", brilho, Color.white, new Vector2(40f, 90f), new Vector2(70f, 60f));
        vidro.preserveAspect = false;

        // ---------- Jogo ----------
        TurboRacer jogo = canvasObj.AddComponent<TurboRacer>();
        SerializedObject d = new SerializedObject(jogo);
        d.FindProperty("gramaEsquerda").objectReferenceValue = gramaE;
        d.FindProperty("gramaDireita").objectReferenceValue = gramaD;
        Lista(d, "faixasPista", faixas.ToArray());
        Lista(d, "enfeites", enfeites.ToArray());
        d.FindProperty("objetos").objectReferenceValue = objetos;
        d.FindProperty("jogador").objectReferenceValue = jogador;
        d.FindProperty("explosao").objectReferenceValue = boom;
        d.FindProperty("reflexo").objectReferenceValue = reflexo;
        d.FindProperty("txtPontos").objectReferenceValue = txtPontos;
        d.FindProperty("txtRecorde").objectReferenceValue = txtRecorde;
        d.FindProperty("txtCentro").objectReferenceValue = txtCentro;
        d.FindProperty("txtSub").objectReferenceValue = txtSub;
        d.FindProperty("txtDica").objectReferenceValue = txtDica;
        d.FindProperty("painelTitulo").objectReferenceValue = titulo;
        d.FindProperty("painelCarregando").objectReferenceValue = carregando;
        d.FindProperty("txtStart").objectReferenceValue = txtStart;
        d.FindProperty("txtRecordeTitulo").objectReferenceValue = txtRecordeTitulo;
        d.FindProperty("barraCarregando").objectReferenceValue = barra.rectTransform;
        Lista(d, "carros", carros);
        d.FindProperty("caminhao").objectReferenceValue = caminhao;
        Lista(d, "quadrosExplosao", explosao);
        d.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(cena, CaminhoVideogame);
        ColocarNoBuild(CaminhoVideogame, "Assets/Scenes/" + ProgressoHistoria.CenaTutorial + ".unity");
        return true;
    }

    // ---------- Peças ----------

    private static RectTransform Lado(RectTransform tela, string nome, Sprite grama, float x, float largura)
    {
        // Mais alta que a tela: o TurboRacer desce ela de 0 a 8 pixels e volta (parece que anda)
        Image img = Imagem(tela, nome, grama, Color.white, new Vector2(x + largura / 2f, 64f), new Vector2(largura, 128f));
        img.type = Image.Type.Tiled;
        img.pixelsPerUnitMultiplier = 1f;   // 1 pixel do desenho = 1 pixel do jogo
        img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        return img.rectTransform;
    }

    private static RectTransform Tracejado(RectTransform tela, string nome, Sprite faixa, float x)
    {
        Image img = Imagem(tela, nome, faixa, Color.white, new Vector2(x, 66f), new Vector2(2f, 132f));
        img.type = Image.Type.Tiled;
        img.pixelsPerUnitMultiplier = 1f;   // 1 pixel do desenho = 1 pixel do jogo
        return img.rectTransform;
    }

    // Imagem na "tela" do videogame: posição e tamanho em pixels do jogo (origem embaixo à esquerda)
    private static Image Imagem(Transform pai, string nome, Sprite sprite, Color cor, Vector2 posicao, Vector2 tamanho, bool esticar = false)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(Image));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(pai, false);
        RectTransform rt = (RectTransform)obj.transform;
        if (esticar)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
        else
        {
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = posicao;
            rt.sizeDelta = tamanho;
        }
        Image img = obj.GetComponent<Image>();
        img.sprite = sprite;
        img.color = cor;
        img.raycastTarget = false;
        return img;
    }

    private static TextoPixel Texto(Transform pai, string nome, FontePixel fonte, string texto, Color cor, float escala,
                                    TextoPixel.Alinhamento alinhamento, Vector2 posicao)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(pai, false);
        RectTransform rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = new Vector2(1f, 1f);
        TextoPixel t = obj.AddComponent<TextoPixel>();
        t.Definir(fonte, texto, cor, escala, alinhamento);
        EditorUtility.SetDirty(t);
        return t;
    }

    private static void Lista(SerializedObject d, string campo, Object[] valores)
    {
        SerializedProperty p = d.FindProperty(campo);
        p.arraySize = valores.Length;
        for (int i = 0; i < valores.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
    }
}
