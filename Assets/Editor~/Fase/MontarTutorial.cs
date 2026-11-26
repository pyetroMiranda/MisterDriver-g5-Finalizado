using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Monta o tutorial novo na cena aberta (Tutorial):
// desenha as dicas e as teclas em pixel art, cria a lista de dicas e liga os gatilhos da pista.
// Menu: Mr Driver > Tutorial > Montar tutorial novo nesta cena
public static class MontarTutorial
{
    private const string Pasta = "Assets/Imagens/Tutorial";
    private const float PixelMoldura = 4f;   // cada pixel da moldura vira 4 pixels na tela

    [MenuItem("Mr Driver/Tutorial/Montar tutorial novo nesta cena")]
    private static void Montar()
    {
        TutorialInicio antigo = Object.FindFirstObjectByType<TutorialInicio>(FindObjectsInactive.Include);
        ChegadaTutorial chegada = Object.FindFirstObjectByType<ChegadaTutorial>(FindObjectsInactive.Include);
        if (antigo == null && chegada == null)
        {
            EditorUtility.DisplayDialog("Tutorial",
                "Esta cena não parece ser a do tutorial.\n\nAbra Assets/Scenes/Tutorial e tente de novo.", "OK");
            return;
        }

        TutorialNivel existente = Object.FindFirstObjectByType<TutorialNivel>(FindObjectsInactive.Include);
        if (existente != null)
        {
            bool refazer = EditorUtility.DisplayDialog("Tutorial",
                "Esta cena já tem o tutorial novo.\n\nQuer apagar e montar de novo? " +
                "As dicas voltam ao padrão (o que você editou nelas se perde).", "Montar de novo", "Cancelar");
            if (!refazer) return;
        }

        Undo.SetCurrentGroupName("Montar tutorial novo");
        int grupo = Undo.GetCurrentGroup();

        if (existente != null) Undo.DestroyObjectImmediate(existente.gameObject);

        // 1) Imagens
        Sprite moldura = DesenharMoldura();
        Sprite[] teclas = DesenharTodasAsTeclas();

        // 2) Interface + lista de dicas
        TutorialNivel tutorial = CriarInterface(moldura, teclas, out GameObject painel);
        tutorial.DefinirPassos(DicasPadrao());
        EditorUtility.SetDirty(tutorial);

        // 3) Gatilhos da pista
        string relatorio = TrocarGatilhos(tutorial);

        // 4) Pausa: não deixa pausar com a dica aberta
        PauseController pause = Object.FindFirstObjectByType<PauseController>(FindObjectsInactive.Include);
        if (pause != null)
        {
            SerializedObject dados = new SerializedObject(pause);
            dados.FindProperty("pnlTutorialInicio").objectReferenceValue = painel;
            dados.ApplyModifiedProperties();

            SerializedObject dadosTutorial = new SerializedObject(tutorial);
            dadosTutorial.FindProperty("pause").objectReferenceValue = pause;
            dadosTutorial.ApplyModifiedProperties();
        }

        // 5) Desliga o tutorial antigo (não apaga nada)
        if (antigo != null)
        {
            SerializedObject dadosAntigo = new SerializedObject(antigo);
            GameObject painelAntigo = dadosAntigo.FindProperty("pnlTutorialInicio").objectReferenceValue as GameObject;
            if (painelAntigo != null)
            {
                Undo.RecordObject(painelAntigo, "Esconder tutorial antigo");
                painelAntigo.SetActive(false);
            }
            Undo.RecordObject(antigo, "Desligar tutorial antigo");
            antigo.enabled = false;
        }

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(tutorial.gameObject.scene);
        Selection.activeGameObject = tutorial.gameObject;

        EditorUtility.DisplayDialog("Tutorial",
            "Tutorial novo montado!\n\n" +
            "• CanvasDicas: as dicas (edite a lista no componente Tutorial Nivel)\n" +
            "• Tutorial antigo: desligado (nada foi apagado)\n" +
            relatorio + "\nAgora salve a cena com Ctrl+S.", "OK");
    }

    // ---------- Dicas dos poderes novos (sem apagar o que já existe) ----------

    [MenuItem("Mr Driver/Tutorial/Adicionar dicas dos poderes novos")]
    private static void AdicionarDicasNovas()
    {
        TutorialNivel tutorial = Object.FindFirstObjectByType<TutorialNivel>(FindObjectsInactive.Include);
        if (tutorial == null)
        {
            EditorUtility.DisplayDialog("Tutorial",
                "Esta cena não tem o tutorial novo.\n\nAbra a cena Tutorial (e, se precisar, monte o tutorial antes).", "OK");
            return;
        }

        Undo.RecordObject(tutorial, "Adicionar dicas dos poderes novos");

        List<PassoTutorial> novas = new List<PassoTutorial>
        {
            ComEnter(Poder(Dica("Poder: Advogado",
                "Passivo: em cada nível, o advogado anula os primeiros 3 pontos de dano na sua carteira. " +
                "Não precisa apertar nada!", 1f), BuscarItem("Advogado"))),
            Poder(Dica("Poder: Freios ABS",
                "Aperte B para parar o carro NA HORA, sem derrapar. Ótimo para não bater! Recarrega em 8 segundos.",
                1f, TeclaTutorial.B), BuscarItem("FreioABS")),
            Poder(Dica("Poder: Suspensão Reforçada",
                "Aperte J: por 8 segundos os buracos não freiam o carro nem tiram pontos. Recarrega em 25 segundos.",
                1f, TeclaTutorial.J), BuscarItem("SuspensaoReforcada")),
            Poder(Dica("Poder: Câmera Lenta",
                "Aperte K: o jogo inteiro fica mais devagar por 4 segundos, para você desviar com calma. " +
                "Recarrega em 30 segundos.",
                1f, TeclaTutorial.K), BuscarItem("CameraLenta")),
            Poder(Dica("Poder: GPS",
                "Aperte G para ligar ou desligar o GPS: uma linha na pista mostra o caminho mais curto até a chegada.",
                1f, TeclaTutorial.G), BuscarItem("GPS")),
            ComEnter(Dica("Melhorias na loja",
                "Na LOJA do menu principal você também pode melhorar o carro: motor, aceleração, freios, pneus, " +
                "nitro e o Curso de Direção (+1 ponto na carteira). Cada melhoria tem vários níveis!", 1f)),
        };

        // Coloca logo depois da última dica "EmSequencia" (antes das dicas de gatilho)
        int posicao = 0;
        for (int i = 0; i < tutorial.Passos.Count; i++)
            if (tutorial.Passos[i].quando == PassoTutorial.Quando.EmSequencia) posicao = i + 1;

        int adicionadas = 0;
        foreach (PassoTutorial nova in novas)
        {
            if (tutorial.Passos.Exists(p => p.titulo == nova.titulo)) continue;
            tutorial.Passos.Insert(posicao++, nova);
            adicionadas++;
        }
        EditorUtility.SetDirty(tutorial);

        // Desenhos das teclas novas (B, J, K, G)
        Sprite[] teclas = DesenharTodasAsTeclas();
        SerializedObject dados = new SerializedObject(tutorial);
        SerializedProperty lista = dados.FindProperty("spritesTeclas");
        lista.arraySize = teclas.Length;
        for (int i = 0; i < teclas.Length; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = teclas[i];
        dados.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(tutorial.gameObject.scene);
        Selection.activeGameObject = tutorial.gameObject;
        EditorUtility.DisplayDialog("Tutorial",
            adicionadas + " dica(s) nova(s) adicionada(s) depois da dica da Mola.\n\nSalve a cena com Ctrl+S.", "OK");
    }

    // ---------- Lista de dicas padrão ----------

    private static List<PassoTutorial> DicasPadrao()
    {
        ItemLoja nitro = BuscarItem("Nitro");
        ItemLoja buzina = BuscarItem("Buzina");
        ItemLoja mola = BuscarItem("MolaParaChoque");

        return new List<PassoTutorial>
        {
            Dica("Bem-vindo à autoescola!",
                "Aqui você vai aprender todos os comandos do carro. Siga a pista e, no final, " +
                "estacione dentro da vaga marcada e fique parado por 1 segundo.",
                0.6f, TeclaTutorial.Enter),

            Dica("Acelerar",
                "Segure a SETA PARA CIMA ou W para acelerar. Quando soltar, o carro vai parando sozinho.",
                0.3f, TeclaTutorial.Cima, TeclaTutorial.W),

            Dica("Frear e dar ré",
                "Segure a SETA PARA BAIXO ou S para frear. Se continuar segurando com o carro parado, ele anda de ré.",
                2f, TeclaTutorial.Baixo, TeclaTutorial.S),

            Dica("Virar",
                "SETA PARA A ESQUERDA ou A vira para a esquerda. SETA PARA A DIREITA ou D vira para a direita. " +
                "O carro só vira quando está andando.",
                2f, TeclaTutorial.Esquerda, TeclaTutorial.A, TeclaTutorial.Direita, TeclaTutorial.D),

            Rapida(Dica("Freio de mão",
                "Aperte ESPAÇO para puxar o freio de mão. Em alta velocidade, segure ESPAÇO durante uma curva " +
                "para derrapar (drift)!",
                4f, TeclaTutorial.Espaco)),

            Rapida(Dica("Faróis",
                "Aperte L para ligar ou desligar os faróis.",
                4f, TeclaTutorial.L)),

            ComEnter(Dica("Pausar o jogo",
                "Depois de fechar esta dica, aperte ESC a qualquer momento para pausar o jogo.",
                4f, TeclaTutorial.Esc)),

            Poder(Dica("Poder: Nitro",
                "Aperte CTRL para ativar o nitro: o carro fica muito mais rápido por 3 segundos. " +
                "Depois ele leva 30 segundos para recarregar.",
                3f, TeclaTutorial.Ctrl), nitro),

            Poder(Dica("Poder: Buzina",
                "Aperte H para buzinar: os carros do trânsito perto de você param por alguns segundos. " +
                "Recarrega em 15 segundos.",
                1f, TeclaTutorial.H), buzina),

            Poder(Dica("Poder: Mola-de-para-choque",
                "Aperte M para ligar um campo de força por 5 segundos: carros que encostarem em você são " +
                "empurrados para longe e você não perde pontos. Recarrega em 20 segundos.",
                1f, TeclaTutorial.M), mola),

            NoGatilho(Dica("Curva à esquerda!",
                "Tire o pé do acelerador e vire com a SETA PARA A ESQUERDA ou A.",
                0f, TeclaTutorial.Esquerda, TeclaTutorial.A), "CurvaEsquerda"),

            NoGatilho(Dica("Curva à direita!",
                "Tire o pé do acelerador e vire com a SETA PARA A DIREITA ou D.",
                0f, TeclaTutorial.Direita, TeclaTutorial.D), "CurvaDireita"),

            NoGatilho(Dica("Cuidado com os cones!",
                "Desvie dos cones virando para os lados. Se bater em um cone, o tutorial termina " +
                "e você precisa começar de novo.",
                0f, TeclaTutorial.Esquerda, TeclaTutorial.A, TeclaTutorial.Direita, TeclaTutorial.D), "Cones"),
        };
    }

    private static PassoTutorial Dica(string titulo, string mensagem, float espera, params TeclaTutorial[] teclas)
    {
        return new PassoTutorial
        {
            titulo = titulo,
            mensagem = mensagem,
            espera = espera,
            teclas = teclas,
            quando = PassoTutorial.Quando.EmSequencia,
            fecharComAsTeclas = true,
            pausarJogo = true,
        };
    }

    // Não pausa o jogo e some sozinha se o jogador não apertar nada
    private static PassoTutorial Rapida(PassoTutorial p) { p.pausarJogo = false; p.fecharSozinhoApos = 10f; return p; }
    private static PassoTutorial ComEnter(PassoTutorial p) { p.fecharComAsTeclas = false; return p; }
    private static PassoTutorial Poder(PassoTutorial p, ItemLoja item) { p.poder = item; return p; }
    private static PassoTutorial NoGatilho(PassoTutorial p, string id)
    {
        p.quando = PassoTutorial.Quando.AoPassarNoGatilho;
        p.idGatilho = id;
        return p;
    }

    private static ItemLoja BuscarItem(string id)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:ItemLoja"))
        {
            ItemLoja item = AssetDatabase.LoadAssetAtPath<ItemLoja>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null && item.Id == id) return item;
        }
        return null;
    }

    // ---------- Gatilhos da pista ----------

    private static string TrocarGatilhos(TutorialNivel tutorial)
    {
        int esquerda = Trocar<GatilhoTutorialEsquerda>("CurvaEsquerda", tutorial, out GameObject modelo);
        int direita = Trocar<GatilhoTutorialDireita>("CurvaDireita", tutorial, out _);
        int cones = Trocar<GatilhoTutorialCones>("Cones", tutorial, out _);

        string texto = "• Gatilhos ligados: curva esquerda (" + esquerda + "), curva direita (" + direita +
                       "), cones (" + cones + ")\n";

        // Não existia gatilho da curva à direita: cria um desligado para você posicionar
        if (direita == 0 && modelo != null)
        {
            GameObject copia = Object.Instantiate(modelo, modelo.transform.parent);
            copia.name = "GatilhoCurvaDireita (MOVA PARA A CURVA)";
            Undo.RegisterCreatedObjectUndo(copia, "Criar gatilho da direita");

            foreach (MonoBehaviour velho in copia.GetComponents<MonoBehaviour>())
                if (!(velho is GatilhoDica)) Object.DestroyImmediate(velho);

            SerializedObject dados = new SerializedObject(copia.GetComponent<GatilhoDica>());
            dados.FindProperty("id").stringValue = "CurvaDireita";
            dados.ApplyModifiedPropertiesWithoutUndo();

            copia.SetActive(false);
            texto += "• Criado \"GatilhoCurvaDireita (MOVA PARA A CURVA)\", DESLIGADO: mova para a curva e ligue\n";
        }

        return texto;
    }

    private static int Trocar<T>(string id, TutorialNivel tutorial, out GameObject ultimo) where T : MonoBehaviour
    {
        ultimo = null;
        T[] antigos = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (T antigo in antigos)
        {
            Undo.RecordObject(antigo, "Desligar gatilho antigo");
            antigo.enabled = false;

            GatilhoDica novo = antigo.GetComponent<GatilhoDica>();
            if (novo == null) novo = Undo.AddComponent<GatilhoDica>(antigo.gameObject);

            SerializedObject dados = new SerializedObject(novo);
            dados.FindProperty("id").stringValue = id;
            dados.FindProperty("tutorial").objectReferenceValue = tutorial;
            dados.ApplyModifiedProperties();

            ultimo = antigo.gameObject;
        }

        return antigos.Length;
    }

    // ---------- Interface ----------

    private static TutorialNivel CriarInterface(Sprite moldura, Sprite[] teclas, out GameObject painel)
    {
        GameObject canvasObj = new GameObject("CanvasDicas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(canvasObj, "Criar CanvasDicas");

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;   // acima do HUD

        CanvasScaler escala = canvasObj.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;

        TutorialNivel tutorial = canvasObj.AddComponent<TutorialNivel>();

        // Painel da dica (embaixo, no centro, para não cobrir o carro)
        painel = new GameObject("PnlDica", typeof(RectTransform), typeof(Image), typeof(CanvasGroup),
                                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        painel.transform.SetParent(canvasObj.transform, false);

        RectTransform rt = painel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 40f);
        rt.sizeDelta = new Vector2(1150f, 300f);

        Image fundo = painel.GetComponent<Image>();
        fundo.sprite = moldura;
        fundo.type = Image.Type.Sliced;
        fundo.pixelsPerUnitMultiplier = 1f / PixelMoldura;
        fundo.raycastTarget = false;

        VerticalLayoutGroup layout = painel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(60, 60, 44, 40);
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        painel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text progresso = Texto(painel.transform, "TxtProgresso", "DICA 1", 24, FontStyles.Bold,
                                   new Color(1f, 0.12f, 0.8f), TextAlignmentOptions.Left);
        TMP_Text titulo = Texto(painel.transform, "TxtTitulo", "Título da dica", 46, FontStyles.Bold,
                                Color.white, TextAlignmentOptions.Left);
        TMP_Text mensagem = Texto(painel.transform, "TxtMensagem", "Mensagem da dica.", 30, FontStyles.Normal,
                                  new Color(0.86f, 0.83f, 0.93f), TextAlignmentOptions.Left);

        // Linha com os desenhos das teclas
        GameObject linha = new GameObject("Teclas", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        linha.transform.SetParent(painel.transform, false);
        HorizontalLayoutGroup layoutLinha = linha.GetComponent<HorizontalLayoutGroup>();
        layoutLinha.spacing = 18;
        layoutLinha.childAlignment = TextAnchor.MiddleLeft;
        layoutLinha.childControlWidth = true;
        layoutLinha.childControlHeight = true;
        layoutLinha.childForceExpandWidth = false;
        layoutLinha.childForceExpandHeight = false;
        linha.GetComponent<LayoutElement>().minHeight = 76;

        GameObject modelo = new GameObject("ModeloTecla", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        modelo.transform.SetParent(linha.transform, false);
        Image imagemModelo = modelo.GetComponent<Image>();
        imagemModelo.sprite = teclas[0];
        imagemModelo.raycastTarget = false;
        LayoutElement tamanhoModelo = modelo.GetComponent<LayoutElement>();
        tamanhoModelo.preferredWidth = teclas[0].rect.width * 4f;
        tamanhoModelo.preferredHeight = teclas[0].rect.height * 4f;

        TMP_Text aviso = Texto(painel.transform, "TxtAvisoLoja", "Você ainda não tem este poder.", 28,
                               FontStyles.Bold, new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Left);
        TMP_Text rodape = Texto(painel.transform, "TxtRodape", "Aperte a tecla para continuar", 24,
                                FontStyles.Italic, new Color(0.75f, 0.7f, 0.85f), TextAlignmentOptions.Right);

        // Liga tudo no TutorialNivel
        SerializedObject dados = new SerializedObject(tutorial);
        dados.FindProperty("painel").objectReferenceValue = painel;
        dados.FindProperty("grupo").objectReferenceValue = painel.GetComponent<CanvasGroup>();
        dados.FindProperty("textoProgresso").objectReferenceValue = progresso;
        dados.FindProperty("textoTitulo").objectReferenceValue = titulo;
        dados.FindProperty("textoMensagem").objectReferenceValue = mensagem;
        dados.FindProperty("textoAviso").objectReferenceValue = aviso;
        dados.FindProperty("textoRodape").objectReferenceValue = rodape;
        dados.FindProperty("linhaTeclas").objectReferenceValue = linha.transform;
        dados.FindProperty("modeloTecla").objectReferenceValue = imagemModelo;

        SerializedProperty lista = dados.FindProperty("spritesTeclas");
        lista.arraySize = teclas.Length;
        for (int i = 0; i < teclas.Length; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = teclas[i];
        dados.ApplyModifiedPropertiesWithoutUndo();

        painel.SetActive(false);
        return tutorial;
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
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.raycastTarget = false;
        return texto;
    }

    // ---------- Desenhos ----------

    // Moldura de 24x24 com cantos cortados; as 8 bordas não esticam (imagem "Sliced")
    private static Sprite DesenharMoldura()
    {
        const int tamanho = 24;
        Color32[] p = PixelArt.NovaTela(tamanho, tamanho);

        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                int camada = PixelArt.Camada(x, y, tamanho, tamanho, 3);
                if (camada < 0) continue;

                Color32 cor;
                if (camada == 0) cor = PixelArt.Contorno;
                else if (camada <= 2) cor = PixelArt.Magenta;
                else if (camada == 3) cor = PixelArt.MagentaEscuro;
                else cor = PixelArt.Fundo;

                PixelArt.Pintar(p, tamanho, tamanho, x, y, cor);
            }
        }

        return PixelArt.Salvar(Pasta, "DicaMoldura", p, tamanho, tamanho, new Vector4(8, 8, 8, 8));
    }

    private static Sprite[] DesenharTodasAsTeclas()
    {
        Array valores = Enum.GetValues(typeof(TeclaTutorial));
        Sprite[] sprites = new Sprite[valores.Length];
        foreach (TeclaTutorial tecla in valores)
            sprites[(int)tecla] = DesenharTecla(tecla);
        return sprites;
    }

    private static string Rotulo(TeclaTutorial tecla)
    {
        switch (tecla)
        {
            case TeclaTutorial.Enter: return "ENTER";
            case TeclaTutorial.Espaco: return "ESPAÇO";
            case TeclaTutorial.Esc: return "ESC";
            case TeclaTutorial.Cima: return "^";
            case TeclaTutorial.Baixo: return "v";
            case TeclaTutorial.Esquerda: return "<";
            case TeclaTutorial.Direita: return ">";
            case TeclaTutorial.Ctrl: return "CTRL";
            default: return tecla.ToString();   // W, A, S, D, L, H, M
        }
    }

    // Tecla de teclado em pixel art: face clara, lateral mais escura embaixo (efeito 3D)
    private static Sprite DesenharTecla(TeclaTutorial tecla)
    {
        string rotulo = Rotulo(tecla);
        int larguraRotulo = PixelArt.LarguraTexto(rotulo);
        int largura = Mathf.Max(17, larguraRotulo + 10);
        const int altura = 17;
        Color32[] p = PixelArt.NovaTela(largura, altura);

        for (int y = 0; y < altura; y++)
        {
            for (int x = 0; x < largura; x++)
            {
                int camada = PixelArt.Camada(x, y, largura, altura, 2);
                if (camada < 0) continue;

                Color32 cor;
                if (camada == 0) cor = PixelArt.Contorno;
                else if (y >= altura - 4) cor = PixelArt.TeclaLado;
                else if (y == 1) cor = PixelArt.TeclaBrilho;
                else cor = PixelArt.TeclaFace;

                PixelArt.Pintar(p, largura, altura, x, y, cor);
            }
        }

        PixelArt.DesenharTexto(p, largura, altura, rotulo, (largura - larguraRotulo) / 2, 4, PixelArt.TeclaTexto);
        return PixelArt.Salvar(Pasta, "Tecla_" + tecla, p, largura, altura);
    }
}
