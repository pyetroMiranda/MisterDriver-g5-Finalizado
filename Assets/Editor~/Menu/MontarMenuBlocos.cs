using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Troca o menu principal por um menu em blocos, tudo no centro da tela: cada bloco é um botão
// (JOGAR grande, TUTORIAL, LOJA, OPÇÕES, SOBRE, NÍVEIS e SAIR) e uma barra embaixo descreve o bloco selecionado.
// Os botões antigos só ficam escondidos (nada é apagado) e os blocos copiam o que eles faziam (inclusive o som).
// Menu: Mr Driver > Menu > Montar menu em blocos
public static class MontarMenuBlocos
{
    private const string CaminhoMenu = "Assets/Scenes/Menu.unity";
    private const float PixelMoldura = 4f;
    private static readonly string[] BotoesAntigos = { "BtnJogar", "BtnTutorial", "BtnLoja", "BtnConfiguracoes", "BtnSobre", "BtnSair" };

    [MenuItem("Mr Driver/Menu/Montar menu em blocos")]
    private static void MenuMontar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorUtility.DisplayDialog("Menu em blocos", Montar(), "OK");
    }

    [MenuItem("Mr Driver/Menu/Voltar para o menu antigo")]
    private static void Desfazer()
    {
        MenuController menu = Object.FindFirstObjectByType<MenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.pnlMenu == null)
        {
            EditorUtility.DisplayDialog("Menu em blocos", "Abra a cena Menu primeiro.", "OK");
            return;
        }
        Transform blocos = menu.pnlMenu.transform.Find("MenuBlocos");
        if (blocos != null) Undo.DestroyObjectImmediate(blocos.gameObject);
        foreach (string nome in BotoesAntigos)
        {
            Transform t = menu.pnlMenu.transform.Find(nome);
            if (t != null) { Undo.RecordObject(t.gameObject, "Mostrar botão antigo"); t.gameObject.SetActive(true); }
        }
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        EditorUtility.DisplayDialog("Menu em blocos",
            "Os botões antigos voltaram. (A escala do Canvas do PnlMenu e a posição do título continuam as novas.)\n\nSalve com Ctrl+S.", "OK");
    }

    // Abre a cena Menu, monta os blocos e salva. Devolve um resumo.
    public static string Montar()
    {
        if (!File.Exists(CaminhoMenu)) return "Não encontrei " + CaminhoMenu + ".";
        Scene cena = SceneManager.GetActiveScene();
        if (cena.path != CaminhoMenu) cena = EditorSceneManager.OpenScene(CaminhoMenu, OpenSceneMode.Single);

        MenuController menu = Object.FindFirstObjectByType<MenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.pnlMenu == null) return "A cena Menu não tem o MenuController com o Pnl Menu.";
        Transform pnl = menu.pnlMenu.transform;

        // A tela passa a escalar com a resolução (antes era tamanho fixo em pixels)
        CanvasScaler escala = pnl.GetComponentInParent<CanvasScaler>();
        if (escala != null)
        {
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920, 1080);
            escala.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(escala);
        }

        Transform antigo = pnl.Find("MenuBlocos");
        if (antigo != null) Object.DestroyImmediate(antigo.gameObject);

        Button Velho(string nome)
        {
            Transform t = pnl.Find(nome);
            return t != null ? t.GetComponent<Button>() : null;
        }
        Button jogar = Velho("BtnJogar"), tutorial = Velho("BtnTutorial"), loja = Velho("BtnLoja"),
               opcoes = Velho("BtnConfiguracoes"), sobre = Velho("BtnSobre"), sair = Velho("BtnSair");

        // Imagens
        Sprite fundo = AutoescolaArte.FundoBloco();
        Sprite destaque = AutoescolaArte.DestaqueBloco();

        // Raiz
        RectTransform raiz = Ret(pnl, "MenuBlocos", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        CanvasGroup grupo = raiz.gameObject.AddComponent<CanvasGroup>();

        Image veu = Ret(raiz, "Escurecer", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero)
                    .gameObject.AddComponent<Image>();
        veu.color = new Color(0.03f, 0f, 0.08f, 0.45f);
        veu.raycastTarget = false;

        RectTransform grade = Ret(raiz, "Grade", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                  new Vector2(0f, -95f), new Vector2(1060f, 640f));

        Color magenta = new Color32(255, 30, 160, 255), laranja = new Color32(255, 128, 28, 255), verde = new Color32(40, 170, 90, 255),
              azul = new Color32(30, 150, 230, 255), roxo = new Color32(130, 70, 220, 255), ciano = new Color32(0, 180, 200, 255),
              vermelho = new Color32(210, 40, 70, 255);

        BlocoMenu bJogar = Bloco(grade, "Jogar", "JOGAR", "NOVA HISTÓRIA",
            "Viva a história do Jason: tire a carteira com o Gervásio e conquiste as ruas da cidade.",
            new Rect(0, 0, 520, 520), magenta, fundo, destaque, jogar, menu.Jogar);
        BlocoMenu bTutorial = Bloco(grade, "Tutorial", "TUTORIAL", "PISTA DE TREINO",
            "Pista de treino com todos os comandos do carro e dos poderes.",
            new Rect(540, 0, 250, 250), laranja, fundo, destaque, tutorial, menu.AbrirTutorial);
        BlocoMenu bLoja = Bloco(grade, "Loja", "LOJA", "MOEDAS",
            "Gaste suas moedas em poderes e melhorias para o carro.",
            new Rect(810, 0, 250, 250), verde, fundo, destaque, loja, menu.AbrirLoja);
        Bloco(grade, "Configuracoes", "OPÇÕES", "SOM E MÚSICA", "Ajuste o volume da música e dos efeitos.",
            new Rect(540, 270, 250, 250), azul, fundo, destaque, opcoes, menu.AbrirConfiguracoes);
        Bloco(grade, "Sobre", "SOBRE", "CRÉDITOS", "Quem fez o Mr. Driver.",
            new Rect(810, 270, 250, 250), roxo, fundo, destaque, sobre, menu.AbrirSobre);
        Bloco(grade, "Niveis", "NÍVEIS", "ESCOLHA A FASE", "Jogue de novo qualquer nível que você já liberou.",
            new Rect(0, 540, 520, 100), ciano, fundo, destaque, jogar, menu.AbrirNiveis);   // usa o som do JOGAR
        Bloco(grade, "Sair", "SAIR", "ATÉ LOGO!", "Fechar o jogo.",
            new Rect(540, 540, 520, 100), vermelho, fundo, destaque, sair, menu.Sair);

        // Barra de baixo
        RectTransform rodape = Ret(raiz, "Rodape", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                                   Vector2.zero, new Vector2(0f, 84f));
        rodape.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.01f, 0.09f, 0.82f);
        RectTransform linha = Ret(rodape, "Linha", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                                  Vector2.zero, new Vector2(0f, 4f));
        linha.gameObject.AddComponent<Image>().color = magenta;
        TMP_Text descricao = Texto(rodape, "TxtDescricao", "", 30, FontStyles.Normal, Color.white, TextAlignmentOptions.MidlineLeft);
        Esticar(descricao.rectTransform, 60f, 620f);
        TMP_Text dicas = Texto(rodape, "TxtDicas", "<color=#7CECFF><b>ENTER</b></color>  Selecionar     <color=#7CECFF><b>SETAS</b></color>  Navegar",
                               26, FontStyles.Normal, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.MidlineRight);
        Esticar(dicas.rectTransform, 1300f, 60f);

        // Componente que cuida da seleção
        MenuBlocos blocos = raiz.gameObject.AddComponent<MenuBlocos>();
        SerializedObject d = new SerializedObject(blocos);
        d.FindProperty("primeiro").objectReferenceValue = bJogar;
        d.FindProperty("blocoJogar").objectReferenceValue = bJogar;
        d.FindProperty("blocoLoja").objectReferenceValue = bLoja;
        d.FindProperty("textoDescricao").objectReferenceValue = descricao;
        d.FindProperty("grupo").objectReferenceValue = grupo;
        d.FindProperty("fonte").objectReferenceValue = FonteDeSom(jogar);
        d.ApplyModifiedPropertiesWithoutUndo();

        // Ordem: fundo, blocos, título por cima
        Transform imgFundo = pnl.Find("ImgFundo");
        if (imgFundo != null) imgFundo.SetAsFirstSibling();
        raiz.SetSiblingIndex(imgFundo != null ? 1 : 0);

        string titulo = "";
        Transform imgTitulo = pnl.Find("ImgTitulo");
        if (imgTitulo != null)
        {
            RectTransform rt = (RectTransform)imgTitulo;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -24f);
            rt.sizeDelta = new Vector2(720f, 288f);
            Image img = rt.GetComponent<Image>();
            if (img != null) img.preserveAspect = true;
            rt.SetAsLastSibling();
            titulo = "• Título MR. DRIVER centralizado em cima\n";
        }

        int escondidos = 0;
        foreach (string nome in BotoesAntigos)
        {
            Transform t = pnl.Find(nome);
            if (t != null && t.gameObject.activeSelf) { t.gameObject.SetActive(false); escondidos++; }
        }

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);

        return "Menu em blocos montado e cena Menu salva:\n• 7 blocos no centro (JOGAR, TUTORIAL, LOJA, OPÇÕES, SOBRE, NÍVEIS, SAIR)\n" +
               titulo + "• " + escondidos + " botões antigos escondidos (Mr Driver > Menu > Voltar para o menu antigo desfaz)\n";
    }

    // ---------- Um bloco ----------

    private static BlocoMenu Bloco(RectTransform grade, string id, string titulo, string sub, string descricao, Rect r, Color cor,
                                   Sprite fundo, Sprite destaque, Button antigo, UnityAction semAntigo)
    {
        // A raiz é a área de clique (não cresce); o "Conteudo" é o que cresce quando selecionado
        RectTransform raiz = Ret(grade, "Bloco" + id, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                                 new Vector2(r.x + r.width / 2f, -(r.y + r.height / 2f)), new Vector2(r.width, r.height));
        Image area = raiz.gameObject.AddComponent<Image>();
        area.color = new Color(1f, 1f, 1f, 0f);
        Button botao = raiz.gameObject.AddComponent<Button>();
        botao.targetGraphic = area;
        botao.transition = Selectable.Transition.None;

        RectTransform conteudo = Ret(raiz, "Conteudo", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        RectTransform rtDestaque = Ret(conteudo, "Destaque", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rtDestaque.offsetMin = new Vector2(-12f, -12f);
        rtDestaque.offsetMax = new Vector2(12f, 12f);
        Image imgDestaque = Imagem(rtDestaque, destaque, true);
        imgDestaque.color = new Color(1f, 1f, 1f, 0f);

        RectTransform rtFundo = Ret(conteudo, "Fundo", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image imgFundo = Imagem(rtFundo, fundo, true);
        imgFundo.color = cor;

        // Listras diagonais claras dão um ar de "cartão" (só nos blocos grandes)
        bool deitado = r.height < 160f;
        Sprite icone = AutoescolaArte.Icone(id);
        RectTransform rtIcone;
        if (deitado)
        {
            float lado = r.height * 0.68f;
            rtIcone = Ret(conteudo, "Icone", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(r.height * 0.55f, 0f), new Vector2(lado, lado));
        }
        else
        {
            float lado = Mathf.Min(r.width, r.height) * (r.width > 400f ? 0.42f : 0.4f);
            rtIcone = Ret(conteudo, "Icone", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                          new Vector2(0f, r.height * 0.1f), new Vector2(lado, lado));
        }
        Imagem(rtIcone, icone, false).preserveAspect = true;

        TMP_Text txtTitulo, txtSub;
        if (deitado)
        {
            txtTitulo = Texto(conteudo, "TxtTitulo", titulo, 42, FontStyles.Bold, Color.white, TextAlignmentOptions.BottomLeft);
            Posicionar(txtTitulo.rectTransform, new Vector2(r.height * 1.05f, -14f), new Vector2(r.width - r.height * 1.1f, 46f));
            txtSub = Texto(conteudo, "TxtSub", sub, 22, FontStyles.Bold, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.TopLeft);
            Posicionar(txtSub.rectTransform, new Vector2(r.height * 1.05f, -60f), new Vector2(r.width - r.height * 1.1f, 30f));
        }
        else
        {
            float faixa = r.height * (r.width > 400f ? 0.24f : 0.32f);
            RectTransform rtFaixa = Ret(conteudo, "Faixa", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                                        new Vector2(0f, 10f), new Vector2(-20f, faixa));
            Image imgFaixa = rtFaixa.gameObject.AddComponent<Image>();
            imgFaixa.color = new Color(0.05f, 0.01f, 0.1f, 0.45f);
            imgFaixa.raycastTarget = false;

            float tamTitulo = r.width > 400f ? 72f : 38f;
            txtTitulo = Texto(rtFaixa, "TxtTitulo", titulo, tamTitulo, FontStyles.Bold, Color.white, TextAlignmentOptions.BottomLeft);
            RectTransform rt = txtTitulo.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.38f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(22f, 0f);
            rt.offsetMax = new Vector2(-16f, -4f);
            txtSub = Texto(rtFaixa, "TxtSub", sub, r.width > 400f ? 26f : 20f, FontStyles.Bold, new Color(1f, 1f, 1f, 0.8f),
                           TextAlignmentOptions.TopLeft);
            RectTransform rs = txtSub.rectTransform;
            rs.anchorMin = new Vector2(0f, 0f);
            rs.anchorMax = new Vector2(1f, 0.38f);
            rs.offsetMin = new Vector2(24f, 4f);
            rs.offsetMax = new Vector2(-16f, 0f);
        }
        txtTitulo.enableAutoSizing = true;
        txtTitulo.fontSizeMax = txtTitulo.fontSize;
        txtTitulo.fontSizeMin = 18f;

        // Ações: primeiro o som de clique do botão antigo, depois a ação certa do MenuController
        if (antigo != null) CopiarSom(antigo, botao);
        UnityEventTools.AddPersistentListener(botao.onClick, semAntigo);

        BlocoMenu bloco = raiz.gameObject.AddComponent<BlocoMenu>();
        SerializedObject d = new SerializedObject(bloco);
        d.FindProperty("conteudo").objectReferenceValue = conteudo;
        d.FindProperty("fundo").objectReferenceValue = imgFundo;
        d.FindProperty("destaque").objectReferenceValue = imgDestaque;
        d.FindProperty("icone").objectReferenceValue = rtIcone;
        d.FindProperty("titulo").objectReferenceValue = txtTitulo;
        d.FindProperty("subtitulo").objectReferenceValue = txtSub;
        d.FindProperty("descricao").stringValue = descricao;
        d.ApplyModifiedPropertiesWithoutUndo();
        return bloco;
    }

    // ---------- Ações dos botões ----------

    // Copia só o som (as chamadas que não são do MenuController/MenuPrincipal)
    private static void CopiarSom(Button de, Button para)
    {
        for (int i = 0; i < de.onClick.GetPersistentEventCount(); i++)
        {
            Object alvo = de.onClick.GetPersistentTarget(i);
            if (alvo is MenuController || alvo is MenuPrincipal) continue;
            UnityAction acao = Acao(de, i);
            if (acao != null) UnityEventTools.AddPersistentListener(para.onClick, acao);
        }
    }

    private static UnityAction Acao(Button botao, int i)
    {
        Object alvo = botao.onClick.GetPersistentTarget(i);
        string metodo = botao.onClick.GetPersistentMethodName(i);
        if (alvo == null || string.IsNullOrEmpty(metodo)) return null;
        System.Reflection.MethodInfo info = alvo.GetType().GetMethod(metodo, Type.EmptyTypes);
        if (info == null || info.ReturnType != typeof(void)) return null;
        return (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), alvo, info);
    }

    private static AudioSource FonteDeSom(Button jogar)
    {
        if (jogar != null)
            for (int i = 0; i < jogar.onClick.GetPersistentEventCount(); i++)
            {
                Object alvo = jogar.onClick.GetPersistentTarget(i);
                if (alvo is AudioSource fonte) return fonte;
                if (alvo is Component c && c.GetComponent<AudioSource>() != null) return c.GetComponent<AudioSource>();
            }
        return null;
    }

    // ---------- Ajudantes de interface ----------

    private static RectTransform Ret(Transform pai, string nome, Vector2 ancoraMin, Vector2 ancoraMax, Vector2 pivo,
                                     Vector2 posicao, Vector2 tamanho)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = ancoraMin;
        rt.anchorMax = ancoraMax;
        rt.pivot = pivo;
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
        return rt;
    }

    private static void Posicionar(RectTransform rt, Vector2 posicao, Vector2 tamanho)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
    }

    private static void Esticar(RectTransform rt, float esquerda, float direita)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(esquerda, 0f);
        rt.offsetMax = new Vector2(-direita, -4f);
    }

    private static Image Imagem(RectTransform rt, Sprite sprite, bool fatiada)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        if (fatiada)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / PixelMoldura;
        }
        return img;
    }

    private static TMP_Text Texto(Transform pai, string nome, string conteudo, float tamanho, FontStyles estilo, Color cor,
                                  TextAlignmentOptions alinhamento)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.transform.SetParent(pai, false);
        TextMeshProUGUI texto = obj.AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        texto.color = cor;
        texto.alignment = alinhamento;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.raycastTarget = false;
        return texto;
    }
}
