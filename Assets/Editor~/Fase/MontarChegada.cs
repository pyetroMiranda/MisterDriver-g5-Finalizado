using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Monta a chegada do nível (vaga + tela "Nível concluído") na cena aberta.
// Menu: Mr Driver > Chegada > Montar chegada nesta cena
public static class MontarChegada
{
    private const string Pasta = "Assets/Imagens/Conclusao";
    private const float PixelMoldura = 4f;

    [MenuItem("Mr Driver/Chegada/Montar chegada nesta cena")]
    private static void Montar()
    {
        CarroController carro = Object.FindFirstObjectByType<CarroController>(FindObjectsInactive.Include);
        if (carro == null)
        {
            EditorUtility.DisplayDialog("Chegada", "Esta cena não tem o carro do jogador.\n\nAbra um nível e tente de novo.", "OK");
            return;
        }

        if (Object.FindFirstObjectByType<ChegadaNivel>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog("Chegada", "Esta cena já tem a chegada nova (objeto com Chegada Nivel).", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Montar chegada");
        int grupo = Undo.GetCurrentGroup();

        ChegadaTutorial antiga = Object.FindFirstObjectByType<ChegadaTutorial>(FindObjectsInactive.Include);
        SerializedObject dadosAntiga = antiga != null ? new SerializedObject(antiga) : null;

        // 1) Onde fica a vaga
        GameObject marcacao = dadosAntiga?.FindProperty("marcacaoChegada").objectReferenceValue as GameObject;
        if (marcacao == null) marcacao = BuscarPorNome("MarcacaoChegada");
        string origem = CalcularVaga(marcacao, antiga, out Vector3 centro, out Vector2 tamanho);

        GameObject chegadaObj = new GameObject("Chegada");
        Undo.RegisterCreatedObjectUndo(chegadaObj, "Criar chegada");
        chegadaObj.transform.position = new Vector3(centro.x, centro.y, 0f);

        // Cena sem vaga desenhada: cria uma, presa à chegada (movem juntas)
        bool vagaNova = false;
        if (marcacao == null)
        {
            marcacao = CriarVagaDesenhada(chegadaObj.transform, out Vector2 tamanhoDesenho);
            if (marcacao != null)
            {
                tamanho = tamanhoDesenho;
                vagaNova = true;
                origem = "vaga nova criada no centro da aba Scene: ARRASTE o objeto Chegada para o fim da pista";
            }
        }

        BoxCollider2D caixa = chegadaObj.AddComponent<BoxCollider2D>();
        caixa.isTrigger = true;
        caixa.size = tamanho;
        ChegadaNivel chegada = chegadaObj.AddComponent<ChegadaNivel>();

        // 2) Tela de conclusão
        GarantirEventSystem();
        TelaNivelConcluido tela = CriarTela(out GameObject painel, out Button botaoProximo, out Button botaoMenu);

        // Som de clique dos botões (o mesmo que o jogo já usa)
        SomBotoes som = Object.FindFirstObjectByType<SomBotoes>(FindObjectsInactive.Include);
        if (som != null)
        {
            UnityEventTools.AddPersistentListener(botaoProximo.onClick, som.TocarCliqueTrocaCena);
            UnityEventTools.AddPersistentListener(botaoMenu.onClick, som.TocarCliqueTrocaCena);
        }

        // 3) Liga tudo
        SerializedObject dados = new SerializedObject(chegada);
        dados.FindProperty("carro").objectReferenceValue = carro;
        dados.FindProperty("tela").objectReferenceValue = tela;
        dados.FindProperty("marcacaoChegada").objectReferenceValue = marcacao;
        if (dadosAntiga != null)
        {
            dados.FindProperty("avisoChegada").objectReferenceValue = dadosAntiga.FindProperty("avisoChegada").objectReferenceValue;
            dados.FindProperty("distanciaAviso").floatValue = dadosAntiga.FindProperty("distanciaAviso").floatValue;
            dados.FindProperty("moedas").intValue = dadosAntiga.FindProperty("moedasAoConcluir").intValue;
        }
        if (vagaNova)
        {
            // Mapa grande: a vaga aparece de mais longe
            dados.FindProperty("distanciaAviso").floatValue = 20f;
            dados.FindProperty("moedas").intValue = 100;
        }
        dados.ApplyModifiedProperties();

        // A pausa (ESC) não abre por cima da tela de conclusão
        PauseController pause = Object.FindFirstObjectByType<PauseController>(FindObjectsInactive.Include);
        if (pause != null)
        {
            SerializedObject dadosPause = new SerializedObject(pause);
            dadosPause.FindProperty("pnlNivelConcluido").objectReferenceValue = painel;
            dadosPause.ApplyModifiedProperties();
        }

        // 4) Desliga a chegada antiga (não apaga)
        if (antiga != null)
        {
            GameObject painelAntigo = dadosAntiga.FindProperty("pnlNivelConcluido").objectReferenceValue as GameObject;
            if (painelAntigo != null)
            {
                Undo.RecordObject(painelAntigo, "Esconder tela antiga");
                painelAntigo.SetActive(false);
            }
            Undo.RecordObject(antiga, "Desligar chegada antiga");
            antiga.enabled = false;
            Collider2D colisorAntigo = antiga.GetComponent<Collider2D>();
            if (colisorAntigo != null)
            {
                Undo.RecordObject(colisorAntigo, "Desligar área antiga");
                colisorAntigo.enabled = false;
            }
        }

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(chegadaObj.scene);
        Selection.activeGameObject = chegadaObj;
        SceneView.lastActiveSceneView?.FrameSelected();

        EditorUtility.DisplayDialog("Chegada",
            "Chegada montada!\n\n" +
            "• Chegada: " + origem + " (retângulo verde na aba Scene)\n" +
            "• CanvasConclusao: tela com PRÓXIMO NÍVEL e MENU\n" +
            (antiga != null ? "• Chegada antiga: desligada (nada foi apagado)\n" : "") +
            "\nAgora salve a cena com Ctrl+S.", "OK");
    }

    // ---------- Vaga ----------

    private static string CalcularVaga(GameObject marcacao, ChegadaTutorial antiga, out Vector3 centro, out Vector2 tamanho)
    {
        SpriteRenderer desenho = marcacao != null ? marcacao.GetComponent<SpriteRenderer>() : null;
        if (desenho != null && desenho.sprite != null)
        {
            // Usa o tamanho do desenho da vaga (funciona mesmo com ela escondida)
            Bounds b = desenho.sprite.bounds;
            centro = desenho.transform.TransformPoint(b.center);
            Vector3 escala = desenho.transform.lossyScale;
            tamanho = new Vector2(Mathf.Abs(b.size.x * escala.x), Mathf.Abs(b.size.y * escala.y));
            return "em cima da vaga desenhada (" + marcacao.name + ")";
        }

        BoxCollider2D caixaAntiga = antiga != null ? antiga.GetComponent<BoxCollider2D>() : null;
        if (caixaAntiga != null)
        {
            centro = antiga.transform.TransformPoint(caixaAntiga.offset);
            Vector3 escala = antiga.transform.lossyScale;
            tamanho = new Vector2(Mathf.Abs(caixaAntiga.size.x * escala.x), Mathf.Abs(caixaAntiga.size.y * escala.y));
            return "no lugar da chegada antiga";
        }

        SceneView vista = SceneView.lastActiveSceneView;
        centro = vista != null ? vista.pivot : Vector3.zero;
        tamanho = new Vector2(6f, 9f);
        return "no centro da aba Scene: MOVA para o fim da pista";
    }

    // Mesmo contorno tracejado da vaga do Nível 1, com o mesmo tamanho
    private static GameObject CriarVagaDesenhada(Transform pai, out Vector2 tamanho)
    {
        tamanho = Vector2.zero;
        Sprite sprite = null;
        foreach (string guid in AssetDatabase.FindAssets("Contorno branco tracejado para estacionamento t:Sprite"))
        {
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
            if (sprite != null) break;
        }
        if (sprite == null) return null;

        GameObject vaga = new GameObject("MarcacaoChegada", typeof(SpriteRenderer));
        Undo.RegisterCreatedObjectUndo(vaga, "Criar vaga");
        vaga.transform.SetParent(pai, false);
        vaga.transform.localScale = new Vector3(0.36f, 0.96f, 1f);

        SpriteRenderer desenho = vaga.GetComponent<SpriteRenderer>();
        desenho.sprite = sprite;
        desenho.sortingOrder = 5;   // acima da pista (0 a 2), abaixo do carro (10)

        tamanho = new Vector2(sprite.bounds.size.x * 0.36f, sprite.bounds.size.y * 0.96f);
        return vaga;
    }

    private static GameObject BuscarPorNome(string nome)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == nome) return t.gameObject;
        return null;
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

    // ---------- Tela ----------

    private static TelaNivelConcluido CriarTela(out GameObject painel, out Button botaoProximo, out Button botaoMenu)
    {
        Sprite moldura = DesenharCaixa("ConclusaoMoldura", 24, 3, PixelArt.Magenta, PixelArt.MagentaEscuro, PixelArt.Fundo);
        Sprite fundoPrincipal = DesenharCaixa("BotaoPrincipal", 16, 2, PixelArt.Branco, PixelArt.Magenta,
                                              new Color32(200, 24, 165, 255));
        Sprite fundoSecundario = DesenharCaixa("BotaoSecundario", 16, 2, PixelArt.Magenta, PixelArt.MagentaEscuro,
                                               new Color32(40, 12, 58, 255));

        GameObject canvasObj = new GameObject("CanvasConclusao", typeof(RectTransform), typeof(Canvas),
                                              typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObj, "Criar tela de conclusão");
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;   // acima de tudo
        CanvasScaler escala = canvasObj.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;

        TelaNivelConcluido tela = canvasObj.AddComponent<TelaNivelConcluido>();

        // Fundo escurecido na tela toda
        painel = new GameObject("PnlConclusao", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        painel.transform.SetParent(canvasObj.transform, false);
        RectTransform rtPainel = painel.GetComponent<RectTransform>();
        rtPainel.anchorMin = Vector2.zero;
        rtPainel.anchorMax = Vector2.one;
        rtPainel.offsetMin = rtPainel.offsetMax = Vector2.zero;
        painel.GetComponent<Image>().color = new Color(0.03f, 0f, 0.06f, 0.7f);

        // Janela no centro
        GameObject janela = new GameObject("Janela", typeof(RectTransform), typeof(Image),
                                           typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        janela.transform.SetParent(painel.transform, false);
        RectTransform rtJanela = janela.GetComponent<RectTransform>();
        rtJanela.anchorMin = rtJanela.anchorMax = rtJanela.pivot = new Vector2(0.5f, 0.5f);
        rtJanela.sizeDelta = new Vector2(980f, 500f);
        Image imgJanela = janela.GetComponent<Image>();
        imgJanela.sprite = moldura;
        imgJanela.type = Image.Type.Sliced;
        imgJanela.pixelsPerUnitMultiplier = 1f / PixelMoldura;

        VerticalLayoutGroup layout = janela.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(70, 70, 60, 60);
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        janela.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text titulo = Texto(janela.transform, "TxtTitulo", "NÍVEL 1 CONCLUÍDO!", 66, FontStyles.Bold, Color.white);
        TMP_Text mensagem = Texto(janela.transform, "TxtMensagem", "Você estacionou certinho na vaga!", 32,
                                  FontStyles.Normal, new Color(0.86f, 0.83f, 0.93f));
        TMP_Text moedas = Texto(janela.transform, "TxtMoedas", "+50 moedas", 34, FontStyles.Bold, new Color(1f, 0.85f, 0.2f));

        // Linha dos botões
        GameObject linha = new GameObject("Botoes", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        linha.transform.SetParent(janela.transform, false);
        HorizontalLayoutGroup layoutLinha = linha.GetComponent<HorizontalLayoutGroup>();
        layoutLinha.spacing = 40;
        layoutLinha.childAlignment = TextAnchor.MiddleCenter;
        layoutLinha.childControlWidth = true;
        layoutLinha.childControlHeight = true;
        layoutLinha.childForceExpandWidth = false;
        layoutLinha.childForceExpandHeight = false;
        LayoutElement alturaLinha = linha.GetComponent<LayoutElement>();
        alturaLinha.minHeight = 110;
        alturaLinha.preferredHeight = 110;

        botaoMenu = Botao(linha.transform, "BtnMenu", "MENU", fundoSecundario, 300f);
        botaoProximo = Botao(linha.transform, "BtnProximo", "PRÓXIMO NÍVEL  >", fundoPrincipal, 420f);

        // Navegação com setas entre os dois botões
        Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
        nav.selectOnRight = botaoProximo;
        botaoMenu.navigation = nav;
        nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = botaoMenu };
        botaoProximo.navigation = nav;

        SerializedObject dados = new SerializedObject(tela);
        dados.FindProperty("painel").objectReferenceValue = painel;
        dados.FindProperty("grupo").objectReferenceValue = painel.GetComponent<CanvasGroup>();
        dados.FindProperty("janela").objectReferenceValue = rtJanela;
        dados.FindProperty("textoTitulo").objectReferenceValue = titulo;
        dados.FindProperty("textoMensagem").objectReferenceValue = mensagem;
        dados.FindProperty("textoMoedas").objectReferenceValue = moedas;
        dados.FindProperty("botaoProximo").objectReferenceValue = botaoProximo;
        dados.FindProperty("botaoMenu").objectReferenceValue = botaoMenu;
        dados.ApplyModifiedPropertiesWithoutUndo();

        painel.SetActive(false);
        return tela;
    }

    private static Button Botao(Transform pai, string nome, string rotulo, Sprite fundo, float largura)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        obj.transform.SetParent(pai, false);

        Image imagem = obj.GetComponent<Image>();
        imagem.sprite = fundo;
        imagem.type = Image.Type.Sliced;
        imagem.pixelsPerUnitMultiplier = 1f / PixelMoldura;

        LayoutElement tamanho = obj.GetComponent<LayoutElement>();
        tamanho.minWidth = tamanho.preferredWidth = largura;
        tamanho.minHeight = tamanho.preferredHeight = 100f;

        Button botao = obj.GetComponent<Button>();
        ColorBlock cores = botao.colors;
        cores.normalColor = Color.white;
        cores.highlightedColor = new Color(1f, 0.8f, 1f);
        cores.selectedColor = new Color(1f, 0.8f, 1f);
        cores.pressedColor = new Color(0.7f, 0.55f, 0.75f);
        cores.fadeDuration = 0.08f;
        botao.colors = cores;

        TMP_Text texto = Texto(obj.transform, "Texto", rotulo, 36, FontStyles.Bold, Color.white);
        texto.alignment = TextAlignmentOptions.Center;
        RectTransform rt = texto.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        return botao;
    }

    private static TMP_Text Texto(Transform pai, string nome, string conteudo, float tamanho, FontStyles estilo, Color cor)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        TextMeshProUGUI texto = obj.AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        texto.color = cor;
        texto.alignment = TextAlignmentOptions.Center;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.raycastTarget = false;
        return texto;
    }

    // Caixa com cantos cortados: contorno escuro, borda, linha interna e preenchimento (imagem "Sliced")
    private static Sprite DesenharCaixa(string nome, int tamanhoCaixa, int chanfro, Color32 borda, Color32 linhaInterna, Color32 preenchimento)
    {
        Color32[] p = PixelArt.NovaTela(tamanhoCaixa, tamanhoCaixa);
        for (int y = 0; y < tamanhoCaixa; y++)
        {
            for (int x = 0; x < tamanhoCaixa; x++)
            {
                int camada = PixelArt.Camada(x, y, tamanhoCaixa, tamanhoCaixa, chanfro);
                if (camada < 0) continue;

                Color32 cor;
                if (camada == 0) cor = PixelArt.Contorno;
                else if (camada <= 2) cor = borda;
                else if (camada == 3) cor = linhaInterna;
                else cor = preenchimento;

                PixelArt.Pintar(p, tamanhoCaixa, tamanhoCaixa, x, y, cor);
            }
        }

        float b = tamanhoCaixa / 3f;
        return PixelArt.Salvar(Pasta, nome, p, tamanhoCaixa, tamanhoCaixa, new Vector4(b, b, b, b));
    }
}
