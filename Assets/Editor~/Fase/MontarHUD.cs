using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Cria os prefabs do HUD (moedas, carteira, pausa) e coloca na cena aberta o que estiver faltando.
// Menu: Mr Driver > HUD > Montar HUD nesta cena
public static class MontarHUD
{
    private const string PastaImagens = "Assets/Imagens/HUD";
    private const string PrefabMoedas = "Assets/Prefabs/HUDMoedas.prefab";
    private const string PrefabCarteira = "Assets/Prefabs/HUDCarteira.prefab";
    private const string PrefabPausa = "Assets/Prefabs/HUDPausa.prefab";
    private const string PrefabPoderes = "Assets/Prefabs/HUDPoderes.prefab";
    private const float PixelMoldura = 4f;

    private static readonly Color32 Ouro = new Color32(255, 200, 40, 255);
    private static readonly Color32 OuroClaro = new Color32(255, 240, 150, 255);
    private static readonly Color32 OuroEscuro = new Color32(190, 120, 20, 255);
    private static readonly Color32 Ciano = new Color32(45, 226, 230, 255);
    private static readonly Color32 Cinza = new Color32(150, 140, 170, 255);
    private static readonly Color32 Pele = new Color32(240, 190, 150, 255);

    // ---------- Comandos do menu ----------

    [MenuItem("Mr Driver/HUD/Montar HUD nesta cena (moedas, carteira, pausa)")]
    private static void Montar()
    {
        if (Object.FindFirstObjectByType<CarroController>(FindObjectsInactive.Include) == null)
        {
            EditorUtility.DisplayDialog("HUD", "Esta cena não tem o carro do jogador.\n\nAbra um nível e tente de novo.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Montar HUD");
        int grupo = Undo.GetCurrentGroup();
        List<string> feito = new List<string>();

        // Moedas: em todos os níveis
        if (Object.FindFirstObjectByType<ContadorMoedas>(FindObjectsInactive.Include) == null)
        {
            Colocar(PrefabMoedas, CriarPrefabMoedas);
            feito.Add("• Contador de moedas (canto superior direito)");
        }
        else feito.Add("• Contador de moedas: já existia");

        // Carteira: só onde dá para perder pontos (trânsito ou buracos)
        bool temPerigo = Object.FindFirstObjectByType<CarroTrafego>(FindObjectsInactive.Include) != null ||
                         Object.FindFirstObjectByType<RedeTrafego>(FindObjectsInactive.Include) != null ||
                         Object.FindFirstObjectByType<Buraco>(FindObjectsInactive.Include) != null;
        if (!temPerigo)
            feito.Add("• Carteira: não colocada (este nível não tem trânsito nem buracos)");
        else if (Object.FindFirstObjectByType<CarteiraHUD>(FindObjectsInactive.Include) == null)
        {
            Colocar(PrefabCarteira, CriarPrefabCarteira);
            int antigas = EsconderCarteiraAntiga();
            feito.Add("• Carteira nova (canto superior esquerdo)" +
                      (antigas > 0 ? "; a carteira antiga foi desligada" : ""));
        }
        else feito.Add("• Carteira: já existia");

        // Barra de poderes: em todos os níveis
        if (Object.FindFirstObjectByType<HUDPoderes>(FindObjectsInactive.Include) == null)
        {
            Colocar(PrefabPoderes, CriarPrefabPoderes);
            feito.Add("• Barra de poderes (lado esquerdo; só mostra os poderes comprados)");
        }
        else feito.Add("• Barra de poderes: já existia");

        // Pausa: só se o nível ainda não tiver
        if (Object.FindFirstObjectByType<PauseController>(FindObjectsInactive.Include) == null)
        {
            Colocar(PrefabPausa, CriarPrefabPausa);
            GarantirEventSystem();
            feito.Add("• Pausa (botão no canto superior esquerdo + tecla ESC)");
        }
        else feito.Add("• Pausa: o nível já tinha");

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("HUD", "HUD montado!\n\n" + string.Join("\n", feito) +
                                           "\n\nAgora salve a cena com Ctrl+S.", "OK");
    }

    [MenuItem("Mr Driver/HUD/Recriar prefabs do HUD (moedas, carteira, pausa)")]
    private static void Recriar()
    {
        if (!EditorUtility.DisplayDialog("HUD",
                "Recria os prefabs HUDMoedas, HUDCarteira e HUDPausa do zero (o que você mudou neles se perde). " +
                "Os níveis que já usam recebem a versão nova.", "Recriar", "Cancelar"))
            return;

        CriarPrefabMoedas();
        CriarPrefabCarteira();
        CriarPrefabPausa();
        CriarPrefabPoderes();
    }

    // ---------- Prefab: barra de poderes ----------

    private static GameObject CriarPrefabPoderes()
    {
        Sprite moldura = Moldura();
        Sprite teclaFundo = PixelArt.Caixa(PastaImagens, "SlotTecla", 12, 2, PixelArt.Branco, PixelArt.MagentaEscuro,
                                           new Color32(40, 12, 58, 255));

        GameObject raiz = NovoCanvas("HUDPoderes", 11, false);
        HUDPoderes hud = raiz.AddComponent<HUDPoderes>();

        // Coluna no lado esquerdo, abaixo da carteira
        GameObject lista = new GameObject("Lista", typeof(RectTransform), typeof(VerticalLayoutGroup));
        lista.transform.SetParent(raiz.transform, false);
        RectTransform rtLista = lista.GetComponent<RectTransform>();
        rtLista.anchorMin = rtLista.anchorMax = rtLista.pivot = new Vector2(0f, 1f);
        rtLista.anchoredPosition = new Vector2(30f, -180f);
        rtLista.sizeDelta = new Vector2(250f, 800f);
        VerticalLayoutGroup layout = lista.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Modelo de slot: [TECLA] Nome / barra de recarga
        GameObject slot = new GameObject("ModeloSlot", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        slot.transform.SetParent(lista.transform, false);
        Fatiada(slot.GetComponent<Image>(), moldura);
        slot.GetComponent<Image>().raycastTarget = false;
        LayoutElement alturaSlot = slot.GetComponent<LayoutElement>();
        alturaSlot.minHeight = alturaSlot.preferredHeight = 70f;

        GameObject tecla = new GameObject("FundoTecla", typeof(RectTransform), typeof(Image));
        tecla.transform.SetParent(slot.transform, false);
        Fatiada(tecla.GetComponent<Image>(), teclaFundo);
        tecla.GetComponent<Image>().raycastTarget = false;
        RectTransform rtTecla = tecla.GetComponent<RectTransform>();
        rtTecla.anchorMin = rtTecla.anchorMax = rtTecla.pivot = new Vector2(0f, 0.5f);
        rtTecla.anchoredPosition = new Vector2(14f, 0f);
        rtTecla.sizeDelta = new Vector2(62f, 46f);

        TMP_Text txtTecla = Texto(slot.transform, "TxtTecla", "B", 24, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        RectTransform rtTxtTecla = txtTecla.rectTransform;
        rtTxtTecla.anchorMin = rtTxtTecla.anchorMax = rtTxtTecla.pivot = new Vector2(0f, 0.5f);
        rtTxtTecla.anchoredPosition = new Vector2(14f, 0f);
        rtTxtTecla.sizeDelta = new Vector2(62f, 46f);

        TMP_Text nome = Texto(slot.transform, "TxtNome", "Poder", 20, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
        Topo(nome.rectTransform, 88f, -10f, 146f, 26f);
        nome.enableAutoSizing = true;
        nome.fontSizeMin = 12f;
        nome.fontSizeMax = 20f;

        GameObject barra = new GameObject("Barra", typeof(RectTransform), typeof(Image));
        barra.transform.SetParent(slot.transform, false);
        barra.GetComponent<Image>().color = new Color(0.08f, 0.03f, 0.12f, 1f);
        Topo(barra.GetComponent<RectTransform>(), 88f, -42f, 146f, 14f);

        GameObject preenchimento = new GameObject("Preenchimento", typeof(RectTransform), typeof(Image));
        preenchimento.transform.SetParent(barra.transform, false);
        Esticar(preenchimento.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        Image imgPreenchimento = preenchimento.GetComponent<Image>();
        imgPreenchimento.sprite = PixelArt.Desenho(PastaImagens, "Branco", new[] { "WWWW", "WWWW", "WWWW", "WWWW" },
                                                   new Dictionary<char, Color32> { { 'W', PixelArt.Branco } });
        imgPreenchimento.type = Image.Type.Filled;
        imgPreenchimento.fillMethod = Image.FillMethod.Horizontal;
        imgPreenchimento.fillAmount = 1f;

        slot.SetActive(false);

        SerializedObject dados = new SerializedObject(hud);
        dados.FindProperty("lista").objectReferenceValue = lista.transform;
        dados.FindProperty("modeloSlot").objectReferenceValue = slot;
        dados.ApplyModifiedPropertiesWithoutUndo();

        return Salvar(raiz, PrefabPoderes);
    }

    private static void Colocar(string caminho, System.Func<GameObject> criar)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        if (prefab == null) prefab = criar();
        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instancia, "Colocar " + prefab.name);
    }

    private static int EsconderCarteiraAntiga()
    {
        int quantas = 0;
        foreach (CarteiraUI antiga in Object.FindObjectsByType<CarteiraUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SerializedObject dados = new SerializedObject(antiga);
            foreach (string campo in new[] { "cartao", "textoPontos" })
            {
                Component parte = dados.FindProperty(campo).objectReferenceValue as Component;
                if (parte == null) continue;
                Undo.RecordObject(parte.gameObject, "Esconder carteira antiga");
                parte.gameObject.SetActive(false);
            }
            Undo.RecordObject(antiga, "Desligar carteira antiga");
            antiga.enabled = false;
            quantas++;
        }
        return quantas;
    }

    private static void GarantirEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
        Undo.RegisterCreatedObjectUndo(es, "Criar EventSystem");
    }

    // ---------- Prefab: moedas ----------

    private static GameObject CriarPrefabMoedas()
    {
        Sprite moldura = Moldura();
        Sprite moeda = PixelArt.Desenho(PastaImagens, "Moeda", new[]
        {
            "...kkkkkk...",
            "..kOOOOOOk..",
            ".kOCCOOOOOk.",
            "kOCCOOOOOOEk",
            "kOCOOOEEOOEk",
            "kOOOOOEEOOEk",
            "kOOOOOEEOOEk",
            "kOOOOOEEOOEk",
            "kOOOOOOOOEEk",
            ".kOOOOOOEEk.",
            "..kEEEEEEk..",
            "...kkkkkk...",
        }, new Dictionary<char, Color32>
        {
            { 'k', PixelArt.Contorno }, { 'O', Ouro }, { 'C', OuroClaro }, { 'E', OuroEscuro },
        });

        GameObject raiz = NovoCanvas("HUDMoedas", 65, false);   // acima da tela de conclusão: dá para ver as moedas subindo

        GameObject caixa = Caixa(raiz.transform, "Moedas", moldura, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(250f, 96f));

        GameObject icone = Imagem(caixa.transform, "ImgMoeda", moeda, 5f);
        RectTransform rtIcone = icone.GetComponent<RectTransform>();
        rtIcone.anchorMin = rtIcone.anchorMax = new Vector2(0f, 0.5f);
        rtIcone.pivot = new Vector2(0f, 0.5f);
        rtIcone.anchoredPosition = new Vector2(24f, 0f);

        TMP_Text numero = Texto(caixa.transform, "TxtMoedas", "0", 46, FontStyles.Bold, new Color(1f, 0.92f, 0.6f),
                                TextAlignmentOptions.Right);
        Esticar(numero.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(96f, 0f), new Vector2(-28f, 0f));

        TMP_Text ganho = Texto(raiz.transform, "TxtGanho", "+50", 38, FontStyles.Bold, new Color(1f, 0.85f, 0.2f),
                               TextAlignmentOptions.Right);
        RectTransform rtGanho = ganho.rectTransform;
        rtGanho.anchorMin = rtGanho.anchorMax = rtGanho.pivot = new Vector2(1f, 1f);
        rtGanho.anchoredPosition = new Vector2(-58f, -134f);
        rtGanho.sizeDelta = new Vector2(200f, 50f);
        ganho.gameObject.SetActive(false);

        ContadorMoedas contador = caixa.AddComponent<ContadorMoedas>();
        SerializedObject dados = new SerializedObject(contador);
        dados.FindProperty("texto").objectReferenceValue = numero;
        dados.FindProperty("caixa").objectReferenceValue = caixa.GetComponent<RectTransform>();
        dados.FindProperty("textoGanho").objectReferenceValue = ganho;
        dados.ApplyModifiedPropertiesWithoutUndo();

        return Salvar(raiz, PrefabMoedas);
    }

    // ---------- Prefab: carteira ----------

    private static GameObject CriarPrefabCarteira()
    {
        Sprite moldura = Moldura();
        Sprite cartao = PixelArt.Desenho(PastaImagens, "CarteiraIcone", new[]
        {
            "kkkkkkkkkkkkkkkkkk",
            "kMMMMMMMMMMMMMMMMk",
            "kMWWMMMMMMMMMMMMMk",
            "kMMMMMMMMMMMMMMMMk",
            "kBBBBBBBBBBBBBBBBk",
            "kBCCCCCBBGGGGGGGBk",
            "kBCCPPCBBBBBBBBBBk",
            "kBCPPPPCBGGGGGBBBk",
            "kBCCPPCBBBBBBBBBBk",
            "kBCPPPPCBGGGGGGBBk",
            "kBCPPPPCBBBBBBBBBk",
            "kBBBBBBBBBBBBBBBBk",
            "kkkkkkkkkkkkkkkkkk",
        }, new Dictionary<char, Color32>
        {
            { 'k', PixelArt.Contorno }, { 'M', PixelArt.Magenta }, { 'W', PixelArt.Branco },
            { 'B', PixelArt.TeclaFace }, { 'C', Ciano }, { 'P', Pele }, { 'G', Cinza },
        });
        Sprite bloco = PixelArt.Desenho(PastaImagens, "BlocoVida", new[]
        {
            "kkkk",
            "kWWk",
            "kWWk",
            "kWWk",
            "kWWk",
            "kSSk",
            "kkkk",
        }, new Dictionary<char, Color32>
        {
            { 'k', PixelArt.Contorno }, { 'W', PixelArt.Branco }, { 'S', new Color32(200, 200, 210, 255) },
        });

        GameObject raiz = NovoCanvas("HUDCarteira", 12, false);

        // Fica à direita do botão de pausa (que ocupa o canto)
        GameObject caixa = Caixa(raiz.transform, "Carteira", moldura, new Vector2(0f, 1f), new Vector2(160f, -30f), new Vector2(470f, 124f));

        GameObject icone = Imagem(caixa.transform, "ImgCarteira", cartao, 5f);
        RectTransform rtIcone = icone.GetComponent<RectTransform>();
        rtIcone.anchorMin = rtIcone.anchorMax = new Vector2(0f, 0.5f);
        rtIcone.pivot = new Vector2(0f, 0.5f);
        rtIcone.anchoredPosition = new Vector2(24f, 0f);

        TMP_Text titulo = Texto(caixa.transform, "TxtTitulo", "CARTEIRA DE MOTORISTA", 20, FontStyles.Bold,
                                new Color(1f, 0.25f, 0.85f), TextAlignmentOptions.Left);
        Topo(titulo.rectTransform, 132f, -18f, 310f, 26f);

        TMP_Text pontos = Texto(caixa.transform, "TxtPontos", "20/20 PONTOS", 30, FontStyles.Bold, Color.white,
                                TextAlignmentOptions.Left);
        Topo(pontos.rectTransform, 132f, -42f, 310f, 36f);

        GameObject barra = new GameObject("Barra", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        barra.transform.SetParent(caixa.transform, false);
        Topo(barra.GetComponent<RectTransform>(), 132f, -84f, 310f, 22f);
        HorizontalLayoutGroup layout = barra.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GameObject modelo = new GameObject("ModeloBloco", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        modelo.transform.SetParent(barra.transform, false);
        Image imgModelo = modelo.GetComponent<Image>();
        imgModelo.sprite = bloco;
        imgModelo.raycastTarget = false;
        LayoutElement tamanho = modelo.GetComponent<LayoutElement>();
        tamanho.preferredWidth = 13f;
        tamanho.preferredHeight = 22f;
        modelo.SetActive(false);

        CarteiraHUD hud = caixa.AddComponent<CarteiraHUD>();
        SerializedObject dados = new SerializedObject(hud);
        dados.FindProperty("cartao").objectReferenceValue = caixa.GetComponent<RectTransform>();
        dados.FindProperty("textoPontos").objectReferenceValue = pontos;
        dados.FindProperty("barra").objectReferenceValue = barra.transform;
        dados.FindProperty("modeloBloco").objectReferenceValue = imgModelo;
        dados.ApplyModifiedPropertiesWithoutUndo();

        return Salvar(raiz, PrefabCarteira);
    }

    // ---------- Prefab: pausa ----------

    private static GameObject CriarPrefabPausa()
    {
        Sprite moldura = Moldura();
        Sprite principal = PixelArt.Caixa(PastaImagens, "BotaoPrincipal", 16, 2, PixelArt.Branco, PixelArt.Magenta,
                                          new Color32(200, 24, 165, 255));
        Sprite secundario = PixelArt.Caixa(PastaImagens, "BotaoSecundario", 16, 2, PixelArt.Magenta, PixelArt.MagentaEscuro,
                                           new Color32(40, 12, 58, 255));
        Sprite iconePausa = CarregarSprite("Assets/Imagens/BtnPause.png");

        GameObject raiz = NovoCanvas("HUDPausa", 55, true);   // acima das dicas (50), abaixo da conclusão (60)
        PauseController pause = raiz.AddComponent<PauseController>();

        // Botão de pausa no canto
        GameObject btnPause = new GameObject("BtnPause", typeof(RectTransform), typeof(Image), typeof(Button));
        btnPause.transform.SetParent(raiz.transform, false);
        RectTransform rtBtn = btnPause.GetComponent<RectTransform>();
        rtBtn.anchorMin = rtBtn.anchorMax = rtBtn.pivot = new Vector2(0f, 1f);
        rtBtn.anchoredPosition = new Vector2(30f, -30f);
        rtBtn.sizeDelta = new Vector2(120f, 120f);
        Image imgBtn = btnPause.GetComponent<Image>();
        imgBtn.sprite = iconePausa;
        imgBtn.preserveAspect = true;
        Button botaoPause = btnPause.GetComponent<Button>();
        Cores(botaoPause);
        UnityEventTools.AddPersistentListener(botaoPause.onClick, pause.Pausar);

        // Painel de pausa (fundo escurecido + janela)
        GameObject painel = new GameObject("PnlPause", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        painel.transform.SetParent(raiz.transform, false);
        Esticar(painel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        painel.GetComponent<Image>().color = new Color(0.03f, 0f, 0.06f, 0.7f);

        GameObject janela = new GameObject("Janela", typeof(RectTransform), typeof(Image),
                                           typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        janela.transform.SetParent(painel.transform, false);
        RectTransform rtJanela = janela.GetComponent<RectTransform>();
        rtJanela.anchorMin = rtJanela.anchorMax = rtJanela.pivot = new Vector2(0.5f, 0.5f);
        rtJanela.sizeDelta = new Vector2(680f, 500f);
        Fatiada(janela.GetComponent<Image>(), moldura);

        VerticalLayoutGroup layout = janela.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(70, 70, 56, 56);
        layout.spacing = 18;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        janela.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Texto(janela.transform, "TxtTitulo", "JOGO PAUSADO", 64, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Texto(janela.transform, "TxtDica", "Aperte ESC para continuar", 26, FontStyles.Italic,
              new Color(0.75f, 0.7f, 0.85f), TextAlignmentOptions.Center);

        Button continuar = Botao(janela.transform, "BtnContinuar", "CONTINUAR", principal);
        Button reiniciar = Botao(janela.transform, "BtnReiniciar", "REINICIAR NÍVEL", secundario);
        Button menu = Botao(janela.transform, "BtnMenu", "MENU", secundario);
        UnityEventTools.AddPersistentListener(continuar.onClick, pause.Continuar);
        UnityEventTools.AddPersistentListener(reiniciar.onClick, pause.Reiniciar);
        UnityEventTools.AddPersistentListener(menu.onClick, pause.VoltarMenu);

        SerializedObject dados = new SerializedObject(pause);
        dados.FindProperty("pnlPause").objectReferenceValue = painel;
        dados.FindProperty("grupoPause").objectReferenceValue = painel.GetComponent<CanvasGroup>();
        dados.FindProperty("janelaPause").objectReferenceValue = rtJanela;
        dados.FindProperty("primeiroBotao").objectReferenceValue = continuar;
        dados.ApplyModifiedPropertiesWithoutUndo();

        painel.SetActive(false);
        return Salvar(raiz, PrefabPausa);
    }

    // ---------- Ajudantes ----------

    private static Sprite Moldura()
    {
        return PixelArt.Caixa(PastaImagens, "HUDMoldura", 24, 3, PixelArt.Magenta, PixelArt.MagentaEscuro, PixelArt.Fundo);
    }

    private static GameObject NovoCanvas(string nome, int ordem, bool clicavel)
    {
        GameObject raiz = clicavel
            ? new GameObject(nome, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            : new GameObject(nome, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordem;
        CanvasScaler escala = raiz.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;
        return raiz;
    }

    private static GameObject Caixa(Transform pai, string nome, Sprite moldura, Vector2 canto, Vector2 posicao, Vector2 tamanho)
    {
        GameObject caixa = new GameObject(nome, typeof(RectTransform), typeof(Image));
        caixa.transform.SetParent(pai, false);
        RectTransform rt = caixa.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = canto;
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
        Fatiada(caixa.GetComponent<Image>(), moldura);
        caixa.GetComponent<Image>().raycastTarget = false;
        return caixa;
    }

    private static void Fatiada(Image imagem, Sprite sprite)
    {
        imagem.sprite = sprite;
        imagem.type = Image.Type.Sliced;
        imagem.pixelsPerUnitMultiplier = 1f / PixelMoldura;
    }

    private static GameObject Imagem(Transform pai, string nome, Sprite sprite, float escala)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(pai, false);
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(sprite.rect.width * escala, sprite.rect.height * escala);
        Image imagem = obj.GetComponent<Image>();
        imagem.sprite = sprite;
        imagem.raycastTarget = false;
        return obj;
    }

    private static Button Botao(Transform pai, string nome, string rotulo, Sprite fundo)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        obj.transform.SetParent(pai, false);
        Fatiada(obj.GetComponent<Image>(), fundo);
        LayoutElement tamanho = obj.GetComponent<LayoutElement>();
        tamanho.minHeight = tamanho.preferredHeight = 92f;

        Button botao = obj.GetComponent<Button>();
        Cores(botao);

        TMP_Text texto = Texto(obj.transform, "Texto", rotulo, 34, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Esticar(texto.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return botao;
    }

    private static void Cores(Button botao)
    {
        ColorBlock cores = botao.colors;
        cores.normalColor = Color.white;
        cores.highlightedColor = new Color(1f, 0.8f, 1f);
        cores.selectedColor = new Color(1f, 0.8f, 1f);
        cores.pressedColor = new Color(0.7f, 0.55f, 0.75f);
        cores.fadeDuration = 0.08f;
        botao.colors = cores;
    }

    private static TMP_Text Texto(Transform pai, string nome, string conteudo, float tamanho, FontStyles estilo,
                                  Color cor, TextAlignmentOptions alinhamento)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        TextMeshProUGUI texto = obj.AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        texto.color = cor;
        texto.alignment = alinhamento;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.raycastTarget = false;
        return texto;
    }

    private static void Esticar(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    // Posiciona a partir do canto superior esquerdo do pai
    private static void Topo(RectTransform rt, float x, float y, float largura, float altura)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(largura, altura);
    }

    private static Sprite CarregarSprite(string caminho)
    {
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(caminho))
            if (obj is Sprite sprite) return sprite;
        return null;
    }

    private static GameObject Salvar(GameObject raiz, string caminho)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
        Object.DestroyImmediate(raiz);
        return prefab;
    }
}
