using System;
using System.Collections.Generic;
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

// Refaz as telas SOBRE, OPÇÕES, NÍVEIS e LOJA do menu no mesmo estilo do menu em blocos:
// o mesmo fundo da cidade à noite, cabeçalho com o bloco colorido + título em pixel art, cartões com borda neon,
// botão VOLTAR em bloco, barra de baixo com a descrição do item selecionado e navegação pelo teclado (ESC volta).
// Os elementos antigos que tinham texto "desenhado" na imagem só ficam escondidos (nada é apagado);
// os que funcionam (lista de níveis, itens da loja, moedas) são reaproveitados e só mudam de lugar e de visual.
// Menu: Mr Driver > Menu > Montar telas do menu (Sobre, Opções, Níveis, Loja)
public static class MontarMenuTelas
{
    private const string CaminhoMenu = "Assets/Scenes/Menu.unity";
    private const float PixelMoldura = 4f;   // cada pixel das molduras vira 4x4 na tela (1920x1080)

    // Mesmas cores dos blocos do menu principal
    private static readonly Color Magenta = new Color32(255, 30, 160, 255), Laranja = new Color32(255, 128, 28, 255),
                                  Verde = new Color32(40, 170, 90, 255), Azul = new Color32(30, 150, 230, 255),
                                  Roxo = new Color32(130, 70, 220, 255), Ciano = new Color32(0, 180, 200, 255),
                                  Vermelho = new Color32(210, 40, 70, 255), Amarelo = new Color32(255, 214, 60, 255);

    // Imagens compartilhadas (geradas a cada montagem)
    private static Sprite fundoBloco, destaqueBloco, cartaoMagenta, cartaoCiano, cartaoAmarelo, cartaoRoxo, faixa, tecla;
    private static Sprite fundoCidade;
    private static AudioSource fonteSom;
    private static MenuController menu;

    [MenuItem("Mr Driver/Menu/Montar telas do menu (Sobre, Opções, Níveis, Loja)")]
    private static void MenuMontar()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorUtility.DisplayDialog("Telas do menu", Montar(), "OK");
    }

    // Abre a cena Menu, refaz as telas e salva. Devolve um resumo.
    public static string Montar()
    {
        if (!File.Exists(CaminhoMenu)) return "Não encontrei " + CaminhoMenu + ".";
        Scene cena = SceneManager.GetActiveScene();
        if (cena.path != CaminhoMenu) cena = EditorSceneManager.OpenScene(CaminhoMenu, OpenSceneMode.Single);

        menu = Object.FindFirstObjectByType<MenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.pnlMenu == null) return "A cena Menu não tem o MenuController com o Pnl Menu.";

        // Imagens
        fundoBloco = AutoescolaArte.FundoBloco();
        destaqueBloco = AutoescolaArte.DestaqueBloco();
        cartaoMagenta = MenuArte.Cartao("CartaoMagenta", new Color32(255, 30, 205, 255));
        cartaoCiano = MenuArte.Cartao("CartaoCiano", new Color32(40, 220, 240, 255));
        cartaoAmarelo = MenuArte.Cartao("CartaoAmarelo", new Color32(255, 200, 40, 255));
        cartaoRoxo = MenuArte.Cartao("CartaoRoxo", new Color32(150, 90, 255, 255));
        faixa = MenuArte.Faixa();
        tecla = MenuArte.Tecla();

        Transform imgFundo = menu.pnlMenu.transform.Find("ImgFundo");
        fundoCidade = imgFundo != null && imgFundo.GetComponent<Image>() != null ? imgFundo.GetComponent<Image>().sprite : null;

        fonteSom = null;
        MenuBlocos blocos = menu.pnlMenu.GetComponentInChildren<MenuBlocos>(true);
        if (blocos != null) fonteSom = new SerializedObject(blocos).FindProperty("fonte").objectReferenceValue as AudioSource;

        List<string> feitas = new List<string>();
        if (menu.pnlSobre != null) { MontarSobre(menu.pnlSobre); feitas.Add("SOBRE"); }
        if (menu.PnlTeste != null) { MontarOpcoes(menu.PnlTeste); feitas.Add("OPÇÕES"); }
        if (menu.pnlNiveis != null) { MontarNiveis(menu.pnlNiveis); feitas.Add("NÍVEIS"); }
        if (menu.pnlLoja != null) { MontarLoja(menu.pnlLoja); feitas.Add("LOJA"); }

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);

        return "Telas do menu refeitas e cena Menu salva: " + string.Join(", ", feitas) + "\n" +
               "• Mesmo fundo, cores e blocos do menu principal; títulos em pixel art\n" +
               "• ESC/BACKSPACE voltam, setas navegam, ENTER confirma; a barra de baixo explica cada item\n" +
               "• Opções: volume de música e efeitos (estradinha com o Kicks), tela cheia e recomeçar a história\n" +
               "• As imagens antigas só foram escondidas\n";
    }

    // ======================================================================
    // Base de toda tela
    // ======================================================================

    private class Base
    {
        public TelaMenu tela;
        public RectTransform raiz;       // tudo o que a ferramenta cria fica aqui
        public RectTransform miolo;      // área do conteúdo (entra deslizando)
        public Button voltar;
        public TMP_Text destaque;        // título grande opcional (Níveis)
    }

    private static Base CriarBase(GameObject painel, string titulo, string nomeTitulo, string icone, Color cor, string subtitulo,
                                  string descricaoInicial, string[] esconder)
    {
        Transform pnl = painel.transform;

        // Tela escala com a resolução, como o menu principal
        CanvasScaler escala = painel.GetComponent<CanvasScaler>();
        if (escala != null)
        {
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920, 1080);
            escala.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(escala);
        }

        // Botão voltar antigo (antes de esconder: o som e a ação dele são copiados)
        Transform velhoVoltar = pnl.Find("BtnVoltar");
        Button antigoVoltar = velhoVoltar != null ? velhoVoltar.GetComponent<Button>() : null;

        foreach (string nome in esconder)
        {
            Transform t = pnl.Find(nome);
            if (t != null && t.gameObject.activeSelf) { t.gameObject.SetActive(false); EditorUtility.SetDirty(t.gameObject); }
        }

        Transform antiga = pnl.Find("Tela");
        if (antiga != null) Object.DestroyImmediate(antiga.gameObject);

        Base b = new Base();
        b.raiz = Ret(pnl, "Tela", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        b.raiz.SetAsFirstSibling();

        // Fundo: a mesma cidade do menu principal, escurecida, com faixas mais escuras em cima e embaixo
        if (fundoCidade != null)
        {
            Image f = Esticada(b.raiz, "FundoCidade").gameObject.AddComponent<Image>();
            f.sprite = fundoCidade;
            f.raycastTarget = false;
        }
        Image veu = Esticada(b.raiz, "Escurecer").gameObject.AddComponent<Image>();
        veu.color = new Color(0.03f, 0f, 0.08f, 0.62f);
        veu.raycastTarget = true;   // bloqueia cliques no que estiver atrás
        Image faixaCima = Ret(b.raiz, "SombraCima", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                              Vector2.zero, new Vector2(0f, 250f)).gameObject.AddComponent<Image>();
        faixaCima.color = new Color(0.02f, 0f, 0.05f, 0.45f);
        faixaCima.raycastTarget = false;

        // Cabeçalho: bloco colorido com o ícone + título em pixel art + subtítulo
        Sprite spriteTitulo = MenuArte.Titulo(titulo, nomeTitulo);
        float alturaTitulo = 112f;
        float larguraTitulo = spriteTitulo.rect.width / spriteTitulo.rect.height * alturaTitulo;
        float total = 132f + 30f + larguraTitulo;
        RectTransform cabecalho = Ret(b.raiz, "Cabecalho", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                      new Vector2(0f, -36f), new Vector2(total, 150f));

        RectTransform blocoIcone = Ret(cabecalho, "BlocoIcone", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                       Vector2.zero, new Vector2(132f, 132f));
        Imagem(blocoIcone, fundoBloco, true).color = cor;
        RectTransform ic = Ret(blocoIcone, "Icone", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                               Vector2.zero, new Vector2(84f, 84f));
        Imagem(ic, IconeQualquer(icone), false).preserveAspect = true;

        RectTransform rtTitulo = Ret(cabecalho, "Titulo", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                     new Vector2(162f, 14f), new Vector2(larguraTitulo, alturaTitulo));
        Imagem(rtTitulo, spriteTitulo, false).preserveAspect = true;

        TMP_Text sub = Texto(cabecalho, "TxtSubtitulo", subtitulo, 26, FontStyles.Bold, new Color(1f, 1f, 1f, 0.78f),
                             TextAlignmentOptions.TopLeft);
        Posicionar(sub.rectTransform, new Vector2(170f, -112f), new Vector2(larguraTitulo + 200f, 36f));
        sub.characterSpacing = 4f;

        RectTransform linha = Ret(b.raiz, "LinhaNeon", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                  new Vector2(0f, -200f), new Vector2(1500f, 4f));
        Image imgLinha = linha.gameObject.AddComponent<Image>();
        imgLinha.color = cor;
        imgLinha.raycastTarget = false;

        // Miolo (onde cada tela coloca o conteúdo): entre o cabeçalho e o botão voltar
        b.miolo = Ret(b.raiz, "Conteudo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                      new Vector2(0f, -30f), new Vector2(1600f, 640f));

        // Barra de baixo (igual à do menu principal)
        RectTransform rodape = Ret(b.raiz, "Rodape", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                                   Vector2.zero, new Vector2(0f, 84f));
        rodape.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.01f, 0.09f, 0.88f);
        RectTransform linhaRodape = Ret(rodape, "Linha", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                                        Vector2.zero, new Vector2(0f, 4f));
        linhaRodape.gameObject.AddComponent<Image>().color = cor;
        TMP_Text descricao = Texto(rodape, "TxtDescricao", descricaoInicial, 30, FontStyles.Normal, Color.white,
                                   TextAlignmentOptions.MidlineLeft);
        Esticar(descricao.rectTransform, 400f, 720f);
        TMP_Text dicas = Texto(rodape, "TxtDicas",
            "<color=#7CECFF><b>ESC</b></color>  Voltar     <color=#7CECFF><b>ENTER</b></color>  Selecionar     <color=#7CECFF><b>SETAS</b></color>  Navegar",
            26, FontStyles.Normal, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.MidlineRight);
        Esticar(dicas.rectTransform, 1200f, 60f);

        // Botão VOLTAR em bloco, no canto de baixo (em cima da barra)
        BlocoMenu blocoVoltar = BotaoBloco(b.raiz, "Voltar", "VOLTAR", "MENU PRINCIPAL", "Voltar para o menu principal.",
            new Vector2(0f, 0f), new Vector2(40f, 98f), new Vector2(300f, 86f), Vermelho, MenuArte.Icone("Voltar"));
        b.voltar = blocoVoltar.GetComponent<Button>();
        if (antigoVoltar != null) CopiarSom(antigoVoltar, b.voltar);
        UnityEventTools.AddPersistentListener(b.voltar.onClick, menu.VoltarMenu);

        // Componente da tela (no próprio painel)
        b.tela = painel.GetComponent<TelaMenu>();
        if (b.tela == null) b.tela = painel.AddComponent<TelaMenu>();
        CanvasGroup grupo = painel.GetComponent<CanvasGroup>();
        if (grupo == null) grupo = painel.AddComponent<CanvasGroup>();

        SerializedObject d = new SerializedObject(b.tela);
        d.FindProperty("botaoVoltar").objectReferenceValue = b.voltar;
        d.FindProperty("grupo").objectReferenceValue = grupo;
        d.FindProperty("animado").objectReferenceValue = b.miolo;
        d.FindProperty("textoDescricao").objectReferenceValue = descricao;
        d.FindProperty("descricaoInicial").stringValue = descricaoInicial;
        d.FindProperty("fonte").objectReferenceValue = fonteSom;
        d.FindProperty("primeiro").objectReferenceValue = null;
        d.FindProperty("textoDestaque").objectReferenceValue = null;
        d.ApplyModifiedPropertiesWithoutUndo();
        return b;
    }

    private static void DefinirPrimeiro(Base b, Selectable primeiro, TMP_Text destaque = null)
    {
        SerializedObject d = new SerializedObject(b.tela);
        d.FindProperty("primeiro").objectReferenceValue = primeiro;
        d.FindProperty("textoDestaque").objectReferenceValue = destaque;
        d.ApplyModifiedPropertiesWithoutUndo();
    }

    // ======================================================================
    // SOBRE
    // ======================================================================

    private static void MontarSobre(GameObject painel)
    {
        Base b = CriarBase(painel, "SOBRE", "TituloSobre", "Sobre", Roxo, "O JOGO, A HISTÓRIA E OS COMANDOS",
                           "Mr. Driver: aprender a dirigir com segurança também pode ser divertido!", new[] { "ImgFundo", "BtnVoltar", "Image" });

        // ---------- Cartão grande: o jogo ----------
        RectTransform jogo = Cartao(b.miolo, "CartaoJogo", cartaoMagenta, new Vector2(0f, 0f), new Vector2(880f, 640f));
        TituloCartao(jogo, "O JOGO", "Escola", Magenta);
        TMP_Text texto = Texto(jogo, "TxtJogo",
            "<b><color=#FF5ADC>Mr. Driver</color></b> é um jogo educativo desenvolvido por estudantes do curso " +
            "<b>Técnico em Programação de Jogos Digitais do SENAI</b>.\n\n" +
            "Em desafios inspirados em situações reais de trânsito, você explora as <b>tecnologias de assistência dos veículos " +
            "<color=#7CECFF>Nissan</color></b> e aprende como elas podem ajudar o motorista.\n\n" +
            "Com visual em pixel art, o projeto une diversão, tecnologia e aprendizado para incentivar uma direção mais " +
            "<b>consciente, segura e respeitosa</b>.",
            29, FontStyles.Normal, new Color(1f, 1f, 1f, 0.94f), TextAlignmentOptions.TopLeft);
        Margens(texto.rectTransform, 44f, 112f, 44f, 190f);
        texto.lineSpacing = 6f;

        // Frase de destaque embaixo, numa faixa magenta
        RectTransform frase = Ret(jogo, "Lema", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                                  new Vector2(0f, 30f), new Vector2(-60f, 136f));
        Image imgFrase = Imagem(frase, faixa, true);
        imgFrase.color = new Color(1f, 0.12f, 0.75f, 0.28f);
        TMP_Text lema = Texto(frase, "TxtLema",
            "\"Cada desafio é uma oportunidade de aprender.\nCada escolha no trânsito faz a diferença.\"",
            29, FontStyles.Bold | FontStyles.Italic, Color.white, TextAlignmentOptions.Center);
        Margens(lema.rectTransform, 20f, 10f, 20f, 10f);

        // ---------- Cartão: a história (com o Kicks do pai) ----------
        RectTransform historia = Cartao(b.miolo, "CartaoHistoria", cartaoCiano, new Vector2(900f, 0f), new Vector2(700f, 280f));
        TituloCartao(historia, "A HISTÓRIA", "Carro", Ciano);
        TMP_Text txtHistoria = Texto(historia, "TxtHistoria",
            "Ajude o <b>Jason</b> a tirar a carteira: aprenda com o instrutor <b>Gervásio</b>, passe no exame e dirija pela " +
            "cidade com o <b><color=#7CECFF>Nissan Kicks</color></b> novo do pai, cheio de tecnologia. " +
            "Mas cuidado com a <b><color=#FF5A6E>Gangue do Escapamento</color></b>!",
            24, FontStyles.Normal, new Color(1f, 1f, 1f, 0.94f), TextAlignmentOptions.TopLeft);
        Margens(txtHistoria.rectTransform, 36f, 100f, 210f, 24f);
        KicksDecorativo(historia);

        // ---------- Cartão: comandos ----------
        RectTransform comandos = Cartao(b.miolo, "CartaoComandos", cartaoAmarelo, new Vector2(900f, 300f), new Vector2(700f, 340f));
        TituloCartao(comandos, "COMANDOS", "Teclado", Amarelo);
        string[,] lista =
        {
            { "W A S D", "Dirigir" }, { "ESPAÇO", "Freio de mão" }, { "CTRL", "Nitro" }, { "H", "Buzina" },
            { "G", "GPS" }, { "M", "Campo de força" }, { "E", "Conversar" }, { "ESC", "Pausar" },
        };
        for (int i = 0; i < lista.GetLength(0); i++)
        {
            int coluna = i / 4, linha = i % 4;
            Comando(comandos, lista[i, 0], lista[i, 1], new Vector2(36f + coluna * 330f, -104f - linha * 56f));
        }

        DefinirPrimeiro(b, b.voltar);
    }

    // Kicks vermelho (o mesmo desenho do carro do jogo), deitado, no canto do cartão da história
    private static void KicksDecorativo(RectTransform cartao)
    {
        CatalogoCarros catalogo = MontarCarros.Catalogo();
        CatalogoCarros.Modelo kicks = catalogo != null ? catalogo.ModeloDe(GaragemJogador.ModeloPadrao) : null;
        if (kicks == null || kicks.lataria == null) return;

        RectTransform area = Ret(cartao, "Kicks", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
                                 new Vector2(-104f, -24f), new Vector2(90f, 180f));
        area.localEulerAngles = new Vector3(0f, 0f, -90f);   // frente para a direita
        Image lataria = Imagem(area, kicks.lataria, false);
        lataria.preserveAspect = true;
        lataria.color = catalogo.CorOu(0);
        if (kicks.detalhes != null)
        {
            Image detalhes = Imagem(Esticada(area, "Detalhes"), kicks.detalhes, false);
            detalhes.preserveAspect = true;
        }
    }

    private static void Comando(RectTransform pai, string teclas, string acao, Vector2 pos)
    {
        float x = 0f;
        foreach (string t in teclas.Split(' '))
        {
            float largura = Mathf.Max(48f, 22f + t.Length * 18f);
            RectTransform rt = Ret(pai, "Tecla " + t, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                                   pos + new Vector2(x, 0f), new Vector2(largura, 46f));
            Imagem(rt, tecla, true);
            TMP_Text letra = Texto(rt, "Txt", t, 22, FontStyles.Bold, new Color32(42, 14, 56, 255), TextAlignmentOptions.Center);
            Margens(letra.rectTransform, 2f, 2f, 2f, 8f);
            x += largura + 6f;
        }
        TMP_Text txt = Texto(pai, "Acao " + acao, acao, 24, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
        Posicionar(txt.rectTransform, pos + new Vector2(Mathf.Max(x, 130f) + 10f, 0f), new Vector2(200f, 46f));
    }

    // ======================================================================
    // OPÇÕES
    // ======================================================================

    private static void MontarOpcoes(GameObject painel)
    {
        Base b = CriarBase(painel, "OPÇÕES", "TituloOpcoes", "Configuracoes", Azul, "SOM, MÚSICA E TELA",
                           "Ajuste o jogo do seu jeito.",
                           new[] { "ImgFundo", "BtnVoltar", "txtMusica", "txtMusica (1)", "SliderMusica", "SliderSFX" });

        RectTransform cartao = Cartao(b.miolo, "CartaoSom", cartaoCiano, new Vector2(200f, 0f), new Vector2(1200f, 360f));
        TituloCartao(cartao, "SOM", "Efeitos", Ciano);

        Slider musica = LinhaVolume(cartao, "Musica", "MÚSICA", "Musica", -110f,
                                    "Volume da música. Use as setas para a esquerda e para a direita para ajustar.", out TMP_Text pctMusica);
        Slider efeitos = LinhaVolume(cartao, "Efeitos", "EFEITOS", "Efeitos", -230f,
                                     "Volume dos efeitos: motor, buzina, batidas e cliques. Use as setas para os lados para ajustar.", out TMP_Text pctEfeitos);

        BlocoMenu telaCheia = BotaoBloco(b.miolo, "TelaCheia", "TELA CHEIA", "LIGADA",
            "Liga ou desliga a tela cheia.", new Vector2(0f, 1f), new Vector2(200f, -400f), new Vector2(590f, 110f), Roxo,
            MenuArte.Icone("TelaCheia"));
        BlocoMenu recomecar = BotaoBloco(b.miolo, "Recomecar", "RECOMEÇAR HISTÓRIA", "VOLTA PARA A CASA DO JASON",
            "Apaga o progresso da história (as moedas e as compras da loja continuam). Aperte duas vezes para confirmar.",
            new Vector2(0f, 1f), new Vector2(810f, -400f), new Vector2(590f, 110f), Laranja, MenuArte.Icone("Recomecar"));

        // Comportamento
        OpcoesMenu opcoes = painel.GetComponent<OpcoesMenu>();
        if (opcoes == null) opcoes = painel.AddComponent<OpcoesMenu>();
        SerializedObject d = new SerializedObject(opcoes);
        d.FindProperty("sliderMusica").objectReferenceValue = musica;
        d.FindProperty("sliderEfeitos").objectReferenceValue = efeitos;
        d.FindProperty("textoMusica").objectReferenceValue = pctMusica;
        d.FindProperty("textoEfeitos").objectReferenceValue = pctEfeitos;
        d.FindProperty("blocoTelaCheia").objectReferenceValue = telaCheia;
        d.FindProperty("blocoRecomecar").objectReferenceValue = recomecar;
        d.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(telaCheia.GetComponent<Button>().onClick, opcoes.AlternarTelaCheia);
        UnityEventTools.AddPersistentListener(recomecar.GetComponent<Button>().onClick, opcoes.RecomecarHistoria);
        Button antigo = painel.transform.Find("BtnVoltar") != null ? painel.transform.Find("BtnVoltar").GetComponent<Button>() : null;
        if (antigo != null) { CopiarSom(antigo, telaCheia.GetComponent<Button>()); CopiarSom(antigo, recomecar.GetComponent<Button>()); }

        // Os volumes de verdade continuam no VolumesJogo (no painel do menu): ele passa a usar os controles novos
        VolumesJogo volumes = Object.FindFirstObjectByType<VolumesJogo>(FindObjectsInactive.Include);
        if (volumes != null)
        {
            SerializedObject v = new SerializedObject(volumes);
            v.FindProperty("sliderMusica").objectReferenceValue = musica;
            v.FindProperty("sliderSFX").objectReferenceValue = efeitos;
            v.ApplyModifiedPropertiesWithoutUndo();
        }

        // Navegação: música ↕ efeitos ↕ (tela cheia ↔ recomeçar) ↕ voltar
        Button bTela = telaCheia.GetComponent<Button>(), bRecomecar = recomecar.GetComponent<Button>();
        Navegar(musica, b.voltar, efeitos, null, null);
        Navegar(efeitos, musica, bTela, null, null);
        Navegar(bTela, efeitos, b.voltar, null, bRecomecar);
        Navegar(bRecomecar, efeitos, b.voltar, bTela, null);
        Navegar(b.voltar, bTela, musica, null, bRecomecar);

        DefinirPrimeiro(b, musica);
    }

    // Uma linha de volume: ícone, nome, estradinha com o carro e a porcentagem
    private static Slider LinhaVolume(RectTransform cartao, string id, string nome, string icone, float y, string descricao,
                                      out TMP_Text porcentagem)
    {
        RectTransform linha = Ret(cartao, "Linha" + id, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                                  new Vector2(0f, y), new Vector2(-60f, 100f));
        Image brilho = Imagem(linha, faixa, true);
        brilho.color = new Color(0.25f, 0.9f, 1f, 0.35f);

        RectTransform ic = Ret(linha, "Icone", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                               new Vector2(24f, 0f), new Vector2(64f, 64f));
        Imagem(ic, MenuArte.Icone(icone), false).preserveAspect = true;
        TMP_Text txt = Texto(linha, "TxtNome", nome, 34, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
        Posicionar(txt.rectTransform, new Vector2(104f, -22f), new Vector2(200f, 56f));
        txt.rectTransform.anchorMin = txt.rectTransform.anchorMax = new Vector2(0f, 1f);

        // Controle: a estrada (fundo), o tracejado, o rastro neon (preenchimento) e o carrinho (alça)
        RectTransform rtSlider = Ret(linha, "Slider" + id, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                     new Vector2(320f, 0f), new Vector2(640f, 56f));
        Slider slider = rtSlider.gameObject.AddComponent<Slider>();

        Image estrada = Imagem(Esticada(rtSlider, "Estrada"), MenuArte.Trilho(), true);
        estrada.raycastTarget = true;
        RectTransform tracejado = Ret(rtSlider, "Tracejado", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
                                      Vector2.zero, new Vector2(-40f, 8f));
        Image imgTracejado = tracejado.gameObject.AddComponent<Image>();
        imgTracejado.sprite = MenuArte.Tracejado();
        imgTracejado.type = Image.Type.Tiled;
        imgTracejado.pixelsPerUnitMultiplier = 1f / PixelMoldura;
        imgTracejado.raycastTarget = false;
        imgTracejado.color = new Color(1f, 1f, 1f, 0.75f);

        RectTransform area = Ret(rtSlider, "Fill Area", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Margens(area, 8f, 8f, 26f, 8f);
        RectTransform fill = Ret(area, "Fill", Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(20f, 0f));
        Imagem(fill, MenuArte.RastroNeon(), true);

        RectTransform areaAlca = Ret(rtSlider, "Handle Slide Area", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Margens(areaAlca, 34f, 0f, 34f, 0f);
        RectTransform alca = Ret(areaAlca, "Carro", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                                 Vector2.zero, new Vector2(96f, 0f));
        Image carro = Imagem(alca, MenuArte.Carrinho(), false);
        carro.preserveAspect = true;
        carro.raycastTarget = true;

        slider.fillRect = fill;
        slider.handleRect = alca;
        slider.targetGraphic = carro;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.8f;
        slider.transition = Selectable.Transition.None;

        porcentagem = Texto(linha, "TxtPorcentagem", "80%", 34, FontStyles.Bold, new Color32(124, 236, 255, 255),
                            TextAlignmentOptions.MidlineRight);
        RectTransform rp = porcentagem.rectTransform;
        rp.anchorMin = rp.anchorMax = new Vector2(1f, 0.5f);
        rp.pivot = new Vector2(1f, 0.5f);
        rp.anchoredPosition = new Vector2(-30f, 0f);
        rp.sizeDelta = new Vector2(150f, 56f);

        SelecaoTela sel = rtSlider.gameObject.AddComponent<SelecaoTela>();
        sel.Definir(nome, descricao, brilho, rtSlider);
        return slider;
    }

    // ======================================================================
    // NÍVEIS
    // ======================================================================

    private static void MontarNiveis(GameObject painel)
    {
        Base b = CriarBase(painel, "NÍVEIS", "TituloNiveis", "Niveis", Ciano, "ESCOLHA A FASE",
                           "Escolha um nível. Termine um nível para liberar o próximo.",
                           new[] { "ImgFundo", "BtnVoltar", "txtNiveis" });

        RectTransform cartao = Cartao(b.miolo, "CartaoNiveis", cartaoCiano, new Vector2(250f, 120f), new Vector2(1100f, 440f));
        TituloCartao(cartao, "MAPA DA CIDADE", "Niveis", Ciano);

        TMP_Text destaque = Texto(b.miolo, "TxtNivelSelecionado", "", 44, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Posicionar(destaque.rectTransform, new Vector2(250f, -10f), new Vector2(1100f, 70f));

        // A lista de níveis que já existe vai para dentro do cartão, maior
        Transform scroll = painel.transform.Find("ScrollNiveis");
        Selectable primeiro = null;
        if (scroll != null)
        {
            RectTransform rs = (RectTransform)scroll;
            rs.SetAsLastSibling();
            rs.anchorMin = rs.anchorMax = new Vector2(0.5f, 0.5f);
            rs.pivot = new Vector2(0.5f, 0.5f);
            rs.anchoredPosition = new Vector2(0f, -86f);
            rs.sizeDelta = new Vector2(480f, 156f);
            rs.localScale = Vector3.one * 2f;
            EditorUtility.SetDirty(rs);

            foreach (BotaoNivel bn in scroll.GetComponentsInChildren<BotaoNivel>(true))
            {
                string cena = new SerializedObject(bn).FindProperty("cena").stringValue;
                InfoNivel(cena, out string titulo, out string desc);
                SelecaoTela sel = bn.GetComponent<SelecaoTela>();
                if (sel == null) sel = bn.gameObject.AddComponent<SelecaoTela>();
                sel.Definir(titulo, desc, null, (RectTransform)bn.transform);
                EditorUtility.SetDirty(sel);
                if (cena == ProgressoHistoria.CenaCasa) primeiro = bn.GetComponent<Button>();
            }
        }

        TMP_Text dica = Texto(b.miolo, "TxtDica", "Os níveis com cadeado são liberados quando você termina o anterior.",
                              24, FontStyles.Italic, new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.Center);
        Posicionar(dica.rectTransform, new Vector2(250f, -580f), new Vector2(1100f, 40f));

        // Minijogo secreto: o TURBO RACER 3000 do Jason (aparece no lugar da dica depois de terminar a história)
        BlocoMenu turbo = BotaoBloco(b.miolo, "TurboRacer", "TURBO RACER 3000", "MINIJOGO SECRETO LIBERADO!",
            "O videogame do Jason! Desvie dos carros e tente bater o recorde do Tonhão: 99999 pontos.",
            new Vector2(0f, 1f), new Vector2(500f, -566f), new Vector2(600f, 74f), Laranja, MenuArte.Icone("Controle"));
        AlternarMinijogo alternar = b.miolo.gameObject.AddComponent<AlternarMinijogo>();
        SerializedObject dm = new SerializedObject(alternar);
        dm.FindProperty("minijogo").objectReferenceValue = turbo.gameObject;
        dm.FindProperty("dica").objectReferenceValue = dica.gameObject;
        dm.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(turbo.GetComponent<Button>().onClick, alternar.Abrir);

        DefinirPrimeiro(b, primeiro, destaque);
    }

    private static void InfoNivel(string cena, out string titulo, out string descricao)
    {
        if (cena == ProgressoHistoria.CenaCasa)
        {
            titulo = "NÍVEL 1 - A CARTEIRA";
            descricao = "A história do Jason desde o começo: a casa, as aulas na autoescola e o exame de direção.";
        }
        else if (cena == "Nivel1")
        {
            titulo = "NÍVEL 2 - IDA AO MERCADO";
            descricao = "Leve o Kicks até o mercado e volte para casa em segurança. Cuidado com os rebeldes!";
        }
        else
        {
            titulo = "EM BREVE";
            descricao = "Novos desafios estão chegando à cidade.";
        }
    }

    // ======================================================================
    // LOJA
    // ======================================================================

    private static void MontarLoja(GameObject painel)
    {
        Base b = CriarBase(painel, "LOJA", "TituloLoja", "Loja", Verde, "PODERES E MELHORIAS PARA O CARRO",
                           "Gaste as moedas que você ganha nos níveis em poderes e melhorias.",
                           new[] { "ImgFundo", "BtnVoltar", "TxtTitulo" });

        Cartao(b.miolo, "CartaoItens", cartaoMagenta, new Vector2(170f, 0f), new Vector2(1260f, 640f));

        // Saldo de moedas no canto de cima (o texto que já existe vai para dentro de uma plaquinha)
        RectTransform placa = Ret(b.raiz, "PlacaMoedas", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                                  new Vector2(-50f, -58f), new Vector2(340f, 100f));
        Imagem(placa, cartaoAmarelo, true);
        RectTransform moeda = Ret(placa, "Moeda", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                  new Vector2(22f, 0f), new Vector2(64f, 64f));
        Imagem(moeda, MenuArte.Icone("Moeda"), false).preserveAspect = true;

        Transform txtMoedas = painel.transform.Find("TxtMoedas");
        if (txtMoedas != null)
        {
            RectTransform rt = (RectTransform)txtMoedas;
            rt.SetAsLastSibling();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-80f, -58f);
            rt.sizeDelta = new Vector2(220f, 100f);
            rt.localScale = Vector3.one;
            TMP_Text t = rt.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.fontSize = 48f;
                t.fontStyle = FontStyles.Bold;
                t.color = new Color32(255, 214, 60, 255);
                t.alignment = TextAlignmentOptions.MidlineRight;
                t.enableAutoSizing = true;
                t.fontSizeMin = 24f;
                t.fontSizeMax = 48f;
                EditorUtility.SetDirty(t);
            }
            TextoMoedas tm = rt.GetComponent<TextoMoedas>();
            if (tm != null)
            {
                SerializedObject d = new SerializedObject(tm);
                d.FindProperty("prefixo").stringValue = "";
                d.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // Lista de itens dentro do cartão, ocupando a largura toda
        Transform scroll = painel.transform.Find("ScrollItens");
        if (scroll != null)
        {
            RectTransform rs = (RectTransform)scroll;
            rs.SetAsLastSibling();
            rs.anchorMin = rs.anchorMax = new Vector2(0.5f, 0.5f);
            rs.pivot = new Vector2(0.5f, 0.5f);
            rs.anchoredPosition = new Vector2(0f, -30f);
            rs.sizeDelta = new Vector2(1200f, 590f);
            rs.localScale = Vector3.one;
            Image fundoScroll = rs.GetComponent<Image>();
            if (fundoScroll != null) fundoScroll.color = new Color(0f, 0f, 0f, 0f);
            EditorUtility.SetDirty(rs);

            VerticalLayoutGroup lista = rs.GetComponentInChildren<VerticalLayoutGroup>(true);
            if (lista != null)
            {
                lista.childControlWidth = true;
                lista.childForceExpandWidth = true;
                lista.spacing = 16f;
                lista.padding = new RectOffset(16, 16, 12, 12);
                EditorUtility.SetDirty(lista);
            }

            ItemLojaUI modelo = rs.GetComponentInChildren<ItemLojaUI>(true);
            if (modelo != null) EstilizarCartaoLoja(modelo);
        }

        DefinirPrimeiro(b, null);
    }

    private static void EstilizarCartaoLoja(ItemLojaUI modelo)
    {
        Image fundo = modelo.GetComponent<Image>();
        if (fundo != null)
        {
            fundo.sprite = cartaoCiano;
            fundo.type = Image.Type.Sliced;
            fundo.pixelsPerUnitMultiplier = 1f / PixelMoldura;
            fundo.color = Color.white;
            EditorUtility.SetDirty(fundo);
        }
        LayoutElement le = modelo.GetComponent<LayoutElement>();
        if (le != null) { le.minHeight = 136f; le.preferredHeight = 136f; EditorUtility.SetDirty(le); }

        void Estilo(string nome, float tamanho, FontStyles estilo, Color cor)
        {
            Transform t = modelo.transform.Find(nome);
            TMP_Text txt = t != null ? t.GetComponent<TMP_Text>() : null;
            if (txt == null) return;
            txt.fontSize = tamanho;
            txt.fontStyle = estilo;
            txt.color = cor;
            txt.enableAutoSizing = true;
            txt.fontSizeMax = tamanho;
            txt.fontSizeMin = tamanho * 0.6f;
            EditorUtility.SetDirty(txt);
        }
        Estilo("TxtNome", 32f, FontStyles.Bold, Color.white);
        Estilo("TxtDescricao", 22f, FontStyles.Normal, new Color(1f, 1f, 1f, 0.82f));
        Estilo("TxtPreco", 28f, FontStyles.Bold, new Color32(255, 214, 60, 255));

        Transform btn = modelo.transform.Find("BtnComprar");
        if (btn != null)
        {
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = fundoBloco;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f / PixelMoldura;
                img.color = Verde;
                EditorUtility.SetDirty(img);
            }
            Button botao = btn.GetComponent<Button>();
            if (botao != null)
            {
                ColorBlock cores = botao.colors;
                cores.normalColor = Color.white;
                cores.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                cores.selectedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
                cores.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                cores.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.8f);
                botao.colors = cores;
                EditorUtility.SetDirty(botao);
            }
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>(true);
            if (txt != null)
            {
                txt.fontStyle = FontStyles.Bold;
                txt.color = Color.white;
                txt.fontSize = 26f;
                EditorUtility.SetDirty(txt);
            }

            // Selecionado: um destaque claro em volta do botão
            Transform velho = btn.Find("Destaque");
            if (velho != null) Object.DestroyImmediate(velho.gameObject);
            RectTransform rtDestaque = Ret(btn, "Destaque", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Margens(rtDestaque, -8f, -8f, -8f, -8f);
            rtDestaque.SetAsFirstSibling();
            Image imgDestaque = Imagem(rtDestaque, destaqueBloco, true);
            imgDestaque.color = new Color(1f, 1f, 1f, 1f);

            SelecaoTela sel = btn.GetComponent<SelecaoTela>();
            if (sel == null) sel = btn.gameObject.AddComponent<SelecaoTela>();
            sel.Definir(null, "Comprar este item com as suas moedas.", imgDestaque, (RectTransform)btn);
            EditorUtility.SetDirty(sel);
        }
    }

    // ======================================================================
    // Peças
    // ======================================================================

    private static RectTransform Cartao(RectTransform pai, string nome, Sprite sprite, Vector2 posCantoSuperior, Vector2 tamanho)
    {
        RectTransform rt = Ret(pai, nome, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(posCantoSuperior.x, -posCantoSuperior.y), tamanho);
        Image img = Imagem(rt, sprite, true);
        img.raycastTarget = false;
        return rt;
    }

    private static void TituloCartao(RectTransform cartao, string titulo, string icone, Color cor)
    {
        RectTransform ic = Ret(cartao, "IconeTitulo", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                               new Vector2(32f, -26f), new Vector2(52f, 52f));
        Imagem(ic, IconeQualquer(icone), false).preserveAspect = true;
        TMP_Text txt = Texto(cartao, "TxtTituloCartao", titulo, 34, FontStyles.Bold, cor, TextAlignmentOptions.MidlineLeft);
        Posicionar(txt.rectTransform, new Vector2(98f, -24f), new Vector2(560f, 56f));
        txt.characterSpacing = 6f;
        RectTransform linha = Ret(cartao, "LinhaTitulo", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                                  new Vector2(0f, -88f), new Vector2(-64f, 3f));
        Image img = linha.gameObject.AddComponent<Image>();
        img.color = new Color(cor.r, cor.g, cor.b, 0.5f);
        img.raycastTarget = false;
    }

    // Ícone dos blocos do menu (Jogar, Loja, Sobre...) ou um dos ícones novos das telas
    private static Sprite IconeQualquer(string nome)
    {
        switch (nome)
        {
            case "Jogar": case "Tutorial": case "Loja": case "Configuracoes": case "Sobre": case "Niveis": case "Sair":
                return AutoescolaArte.Icone(nome);
            default:
                return MenuArte.Icone(nome);
        }
    }

    // Botão em bloco deitado (ícone à esquerda, título e subtítulo), com o mesmo comportamento dos blocos do menu
    private static BlocoMenu BotaoBloco(RectTransform pai, string id, string titulo, string sub, string descricao, Vector2 ancora,
                                        Vector2 posicao, Vector2 tamanho, Color cor, Sprite icone)
    {
        RectTransform raiz = Ret(pai, "Bloco" + id, ancora, ancora, new Vector2(0f, ancora.y), posicao, tamanho);
        Image area = raiz.gameObject.AddComponent<Image>();
        area.color = new Color(1f, 1f, 1f, 0f);
        Button botao = raiz.gameObject.AddComponent<Button>();
        botao.targetGraphic = area;
        botao.transition = Selectable.Transition.None;

        RectTransform conteudo = Esticada(raiz, "Conteudo");
        RectTransform rtDestaque = Esticada(conteudo, "Destaque");
        Margens(rtDestaque, -12f, -12f, -12f, -12f);
        Image imgDestaque = Imagem(rtDestaque, destaqueBloco, true);
        imgDestaque.color = new Color(1f, 1f, 1f, 0f);
        Image imgFundo = Imagem(Esticada(conteudo, "Fundo"), fundoBloco, true);
        imgFundo.color = cor;

        float lado = tamanho.y * 0.66f;
        RectTransform rtIcone = Ret(conteudo, "Icone", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                                    new Vector2(tamanho.y * 0.55f, 0f), new Vector2(lado, lado));
        Imagem(rtIcone, icone, false).preserveAspect = true;

        TMP_Text txtTitulo = Texto(conteudo, "TxtTitulo", titulo, tamanho.y * 0.36f, FontStyles.Bold, Color.white,
                                   TextAlignmentOptions.BottomLeft);
        Posicionar(txtTitulo.rectTransform, new Vector2(tamanho.y * 1.05f, -tamanho.y * 0.1f),
                   new Vector2(tamanho.x - tamanho.y * 1.15f, tamanho.y * 0.5f));
        txtTitulo.enableAutoSizing = true;
        txtTitulo.fontSizeMax = tamanho.y * 0.36f;
        txtTitulo.fontSizeMin = 18f;
        txtTitulo.textWrappingMode = TextWrappingModes.NoWrap;
        TMP_Text txtSub = Texto(conteudo, "TxtSub", sub, tamanho.y * 0.2f, FontStyles.Bold, new Color(1f, 1f, 1f, 0.82f),
                                TextAlignmentOptions.TopLeft);
        Posicionar(txtSub.rectTransform, new Vector2(tamanho.y * 1.05f, -tamanho.y * 0.6f),
                   new Vector2(tamanho.x - tamanho.y * 1.15f, tamanho.y * 0.32f));
        txtSub.textWrappingMode = TextWrappingModes.NoWrap;
        txtSub.enableAutoSizing = true;
        txtSub.fontSizeMax = tamanho.y * 0.2f;
        txtSub.fontSizeMin = 12f;

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

    private static void Navegar(Selectable s, Selectable cima, Selectable baixo, Selectable esquerda, Selectable direita)
    {
        Navigation n = new Navigation { mode = Navigation.Mode.Explicit };
        n.selectOnUp = cima;
        n.selectOnDown = baixo;
        n.selectOnLeft = esquerda;
        n.selectOnRight = direita;
        s.navigation = n;
        EditorUtility.SetDirty(s);
    }

    // ---------- Som dos botões antigos ----------

    // Copia só o som (as chamadas que não são do MenuController/MenuPrincipal)
    private static void CopiarSom(Button de, Button para)
    {
        for (int i = 0; i < de.onClick.GetPersistentEventCount(); i++)
        {
            Object alvo = de.onClick.GetPersistentTarget(i);
            if (alvo == null || alvo is MenuController || alvo is MenuPrincipal) continue;
            string metodo = de.onClick.GetPersistentMethodName(i);
            System.Reflection.MethodInfo info = alvo.GetType().GetMethod(metodo, Type.EmptyTypes);
            if (info == null || info.ReturnType != typeof(void)) continue;
            UnityEventTools.AddPersistentListener(para.onClick, (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), alvo, info));
        }
    }

    // ---------- Ajudantes de interface ----------

    private static RectTransform Ret(Transform pai, string nome, Vector2 ancoraMin, Vector2 ancoraMax, Vector2 pivo,
                                     Vector2 posicao, Vector2 tamanho)
    {
        GameObject obj = new GameObject(nome, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(pai, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = ancoraMin;
        rt.anchorMax = ancoraMax;
        rt.pivot = pivo;
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
        return rt;
    }

    private static RectTransform Esticada(Transform pai, string nome) =>
        Ret(pai, nome, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

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

    // Estica dentro do pai com margens (esquerda, cima, direita, baixo)
    private static void Margens(RectTransform rt, float esquerda, float cima, float direita, float baixo)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(esquerda, baixo);
        rt.offsetMax = new Vector2(-direita, -cima);
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
        obj.layer = LayerMask.NameToLayer("UI");
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
