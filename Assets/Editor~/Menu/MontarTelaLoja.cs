using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Monta a tela da loja na cena Menu automaticamente.
// Menu: Mr Driver > Loja > Montar tela da loja no Menu
public static class MontarTelaLoja
{
    [MenuItem("Mr Driver/Loja/Montar tela da loja no Menu")]
    private static void Montar()
    {
        MenuController menu = Object.FindFirstObjectByType<MenuController>(FindObjectsInactive.Include);
        if (menu == null)
        {
            EditorUtility.DisplayDialog("Montar tela da loja",
                "Não encontrei o MenuController.\n\nAbra a cena Menu (Assets/Scenes/Menu) e tente de novo.", "OK");
            return;
        }

        if (menu.pnlLoja != null)
        {
            Selection.activeGameObject = menu.pnlLoja;
            EditorUtility.DisplayDialog("Montar tela da loja",
                "A loja já existe (" + menu.pnlLoja.name + "). Ela foi selecionada na Hierarchy.", "OK");
            return;
        }

        if (menu.pnlSobre == null || menu.pnlMenu == null)
        {
            EditorUtility.DisplayDialog("Montar tela da loja",
                "O MenuController precisa ter Pnl Menu e Pnl Sobre preenchidos.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Montar tela da loja");
        int grupo = Undo.GetCurrentGroup();

        GameObject loja = CriarPainel(menu);
        CriarConteudo(loja);
        CriarBotaoLojaNoMenu(menu);

        Undo.RecordObject(menu, "Ligar PnlLoja");
        menu.pnlLoja = loja;
        EditorUtility.SetDirty(menu);

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);

        Selection.activeGameObject = loja;
        EditorUtility.DisplayDialog("Montar tela da loja",
            "Pronto! Foram criados:\n\n" +
            "• PnlLoja (a tela da loja, começa escondida)\n" +
            "• BtnLoja (botão LOJA no canto do menu principal)\n\n" +
            "Agora salve a cena com Ctrl+S.", "OK");
    }

    // ---------- Painel: cópia do PnlSobre (mesmo fundo e mesmo botão Voltar) ----------

    private static GameObject CriarPainel(MenuController menu)
    {
        GameObject sobre = menu.pnlSobre;
        GameObject loja = Object.Instantiate(sobre, sobre.transform.parent);
        loja.name = "PnlLoja";
        loja.transform.SetSiblingIndex(sobre.transform.GetSiblingIndex() + 1);
        Undo.RegisterCreatedObjectUndo(loja, "Criar PnlLoja");

        // Mantém só o fundo e o botão Voltar; o resto era conteúdo do "Sobre"
        for (int i = loja.transform.childCount - 1; i >= 0; i--)
        {
            GameObject filho = loja.transform.GetChild(i).gameObject;
            bool manter = filho.name.Contains("Fundo") || filho.name.Contains("Voltar");
            if (!manter) Object.DestroyImmediate(filho);
        }

        // O fundo fica atrás de tudo
        Transform fundo = loja.transform.Find("ImgFundo");
        if (fundo != null) fundo.SetAsFirstSibling();

        loja.SetActive(false);
        return loja;
    }

    // ---------- Título, saldo, lista e cartão modelo ----------

    private static void CriarConteudo(GameObject loja)
    {
        // Título
        TMP_Text titulo = CriarTexto(loja.transform, "TxtTitulo", "LOJA", 56, FontStyles.Bold,
            TextAlignmentOptions.Center, new Vector2(0.3f, 0.82f), new Vector2(0.7f, 0.95f));
        titulo.color = Color.white;

        // Saldo de moedas
        TMP_Text saldo = CriarTexto(loja.transform, "TxtMoedas", "Moedas: 0", 32, FontStyles.Bold,
            TextAlignmentOptions.Right, new Vector2(0.6f, 0.84f), new Vector2(0.95f, 0.94f));
        saldo.color = new Color(1f, 0.85f, 0.2f);
        TextoMoedas textoMoedas = saldo.gameObject.AddComponent<TextoMoedas>();
        DefinirCampo(textoMoedas, "prefixo", "Moedas: ");

        // Lista com rolagem
        GameObject scroll = DefaultControls.CreateScrollView(RecursosUI());
        scroll.name = "ScrollItens";
        scroll.transform.SetParent(loja.transform, false);
        Esticar(scroll.GetComponent<RectTransform>(), new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.8f));
        scroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

        ScrollRect rolagem = scroll.GetComponent<ScrollRect>();
        rolagem.horizontal = false;
        rolagem.movementType = ScrollRect.MovementType.Clamped;
        if (rolagem.horizontalScrollbar != null)
        {
            Object.DestroyImmediate(rolagem.horizontalScrollbar.gameObject);
            rolagem.horizontalScrollbar = null;
        }

        Transform conteudo = rolagem.content;
        VerticalLayoutGroup layout = conteudo.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter ajuste = conteudo.gameObject.AddComponent<ContentSizeFitter>();
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Cartão modelo (a LojaUI esconde este e cria uma cópia para cada item)
        ItemLojaUI modelo = CriarCartaoModelo(conteudo);

        // LojaUI no painel, já com todos os itens da pasta
        LojaUI lojaUI = loja.AddComponent<LojaUI>();
        SerializedObject dados = new SerializedObject(lojaUI);
        dados.FindProperty("modeloCartao").objectReferenceValue = modelo;
        dados.FindProperty("conteudo").objectReferenceValue = conteudo;

        string[] guids = AssetDatabase.FindAssets("t:ItemLoja");
        SerializedProperty lista = dados.FindProperty("itens");
        lista.arraySize = guids.Length;
        for (int i = 0; i < guids.Length; i++)
        {
            ItemLoja item = AssetDatabase.LoadAssetAtPath<ItemLoja>(AssetDatabase.GUIDToAssetPath(guids[i]));
            lista.GetArrayElementAtIndex(i).objectReferenceValue = item;
        }
        dados.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ItemLojaUI CriarCartaoModelo(Transform conteudo)
    {
        GameObject cartao = new GameObject("ModeloCartao", typeof(RectTransform), typeof(Image));
        cartao.transform.SetParent(conteudo, false);
        cartao.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.15f, 0.85f);
        LayoutElement tamanho = cartao.AddComponent<LayoutElement>();
        tamanho.minHeight = 120;
        tamanho.preferredHeight = 120;

        // Ícone à esquerda
        GameObject icone = new GameObject("ImgIcone", typeof(RectTransform), typeof(Image));
        icone.transform.SetParent(cartao.transform, false);
        RectTransform rtIcone = icone.GetComponent<RectTransform>();
        rtIcone.anchorMin = rtIcone.anchorMax = new Vector2(0f, 0.5f);
        rtIcone.pivot = new Vector2(0f, 0.5f);
        rtIcone.anchoredPosition = new Vector2(15f, 0f);
        rtIcone.sizeDelta = new Vector2(90f, 90f);
        icone.GetComponent<Image>().preserveAspect = true;

        // Textos
        TMP_Text nome = CriarTexto(cartao.transform, "TxtNome", "Nome do item", 30, FontStyles.Bold,
            TextAlignmentOptions.BottomLeft, new Vector2(0f, 0.5f), new Vector2(0.68f, 1f), 120f, 8f);
        TMP_Text descricao = CriarTexto(cartao.transform, "TxtDescricao", "Descrição do item", 18, FontStyles.Normal,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 0f), new Vector2(0.68f, 0.5f), 120f, 8f);
        descricao.color = new Color(0.85f, 0.85f, 0.85f);
        TMP_Text preco = CriarTexto(cartao.transform, "TxtPreco", "100 moedas", 26, FontStyles.Bold,
            TextAlignmentOptions.Center, new Vector2(0.7f, 0.52f), new Vector2(0.98f, 0.95f));
        preco.color = new Color(1f, 0.85f, 0.2f);

        // Botão comprar
        GameObject botao = TMP_DefaultControls.CreateButton(RecursosTMP());
        botao.name = "BtnComprar";
        botao.transform.SetParent(cartao.transform, false);
        Esticar(botao.GetComponent<RectTransform>(), new Vector2(0.72f, 0.1f), new Vector2(0.96f, 0.48f));
        TMP_Text textoBotao = botao.GetComponentInChildren<TMP_Text>();
        textoBotao.text = "Comprar";
        textoBotao.fontSize = 24;
        textoBotao.fontStyle = FontStyles.Bold;

        ItemLojaUI ui = cartao.AddComponent<ItemLojaUI>();
        SerializedObject dados = new SerializedObject(ui);
        dados.FindProperty("textoNome").objectReferenceValue = nome;
        dados.FindProperty("textoDescricao").objectReferenceValue = descricao;
        dados.FindProperty("textoPreco").objectReferenceValue = preco;
        dados.FindProperty("imagemIcone").objectReferenceValue = icone.GetComponent<Image>();
        dados.FindProperty("botaoComprar").objectReferenceValue = botao.GetComponent<Button>();
        dados.FindProperty("textoBotao").objectReferenceValue = textoBotao;
        dados.ApplyModifiedPropertiesWithoutUndo();

        return ui;
    }

    // ---------- Botão "LOJA" no menu principal ----------

    private static void CriarBotaoLojaNoMenu(MenuController menu)
    {
        GameObject botao = TMP_DefaultControls.CreateButton(RecursosTMP());
        botao.name = "BtnLoja";
        botao.transform.SetParent(menu.pnlMenu.transform, false);
        Undo.RegisterCreatedObjectUndo(botao, "Criar BtnLoja");

        RectTransform rt = botao.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-30f, -30f);
        rt.sizeDelta = new Vector2(200f, 70f);

        TMP_Text texto = botao.GetComponentInChildren<TMP_Text>();
        texto.text = "LOJA";
        texto.fontSize = 32;
        texto.fontStyle = FontStyles.Bold;

        Button componente = botao.GetComponent<Button>();
        CopiarSomDeClique(menu, componente);
        UnityEventTools.AddPersistentListener(componente.onClick, menu.AbrirLoja);
    }

    // Usa o mesmo som de clique dos outros botões do menu (o AudioSource.Play que eles chamam)
    private static void CopiarSomDeClique(MenuController menu, Button destino)
    {
        foreach (Button outro in menu.pnlMenu.GetComponentsInChildren<Button>(true))
        {
            if (outro == destino) continue;
            UnityEvent evento = outro.onClick;
            for (int i = 0; i < evento.GetPersistentEventCount(); i++)
            {
                if (evento.GetPersistentTarget(i) is AudioSource som && evento.GetPersistentMethodName(i) == "Play")
                {
                    UnityEventTools.AddVoidPersistentListener(destino.onClick, som.Play);
                    return;
                }
            }
        }
    }

    // ---------- Ajudantes ----------

    private static TMP_Text CriarTexto(Transform pai, string nome, string conteudo, float tamanho, FontStyles estilo,
        TextAlignmentOptions alinhamento, Vector2 ancoraMin, Vector2 ancoraMax, float margemEsq = 0f, float margemDir = 0f)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        TextMeshProUGUI texto = obj.AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        texto.alignment = alinhamento;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.raycastTarget = false;

        RectTransform rt = obj.GetComponent<RectTransform>();
        Esticar(rt, ancoraMin, ancoraMax);
        rt.offsetMin = new Vector2(margemEsq, 0f);
        rt.offsetMax = new Vector2(-margemDir, 0f);
        return texto;
    }

    private static void Esticar(RectTransform rt, Vector2 ancoraMin, Vector2 ancoraMax)
    {
        rt.anchorMin = ancoraMin;
        rt.anchorMax = ancoraMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void DefinirCampo(Object alvo, string campo, string valor)
    {
        SerializedObject dados = new SerializedObject(alvo);
        dados.FindProperty(campo).stringValue = valor;
        dados.ApplyModifiedPropertiesWithoutUndo();
    }

    private static DefaultControls.Resources RecursosUI()
    {
        return new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
        };
    }

    private static TMP_DefaultControls.Resources RecursosTMP()
    {
        DefaultControls.Resources r = RecursosUI();
        return new TMP_DefaultControls.Resources
        {
            standard = r.standard,
            background = r.background,
            inputField = r.inputField,
            knob = r.knob,
            checkmark = r.checkmark,
            dropdown = r.dropdown,
            mask = r.mask,
        };
    }
}
