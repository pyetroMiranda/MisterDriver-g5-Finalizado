using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Monta a história do jogo. Menu: Mr Driver > História
//   1. Personagens (Jason, pai, mãe e instrutor em pixel art, com retratos)
//   2. Cena Historia_Casa (Jason acorda, fala com os pais e sai de casa)
//   3. Cena Historia_Autoescola (aulas com cones no pátio, o "segundo tutorial") - MontarHistoriaAutoescola.cs
//   4. Cena Historia_Exame (cópia do Nivel1: o exame final pelas ruas da cidade)
//   5. Botões JOGAR e TUTORIAL do Menu
//   6. Carros da garagem (catálogo e carro do jogador) - MontarCarros.cs
//   7. Cena Historia_Garagem (volta para casa com a carteira; o pai mostra a garagem) - MontarHistoriaGaragem.cs
//   8. Nivel1 com a casa do Jason, o mercado, a missão e a Gangue do Escapamento - MontarHistoriaNivel1.cs
//   9. Cena Historia_Mercado (as compras, a Jussara e a baderna da gangue) - MontarHistoriaMercado.cs
// "Fazer tudo" roda todos os passos e ainda monta o menu em blocos (MontarMenuBlocos).
public static partial class MontarHistoria
{
    private const string CaminhoCasa = "Assets/Scenes/" + ProgressoHistoria.CenaCasa + ".unity";
    private const string CaminhoExame = "Assets/Scenes/" + ProgressoHistoria.CenaExame + ".unity";
    private const string CaminhoGaragem = "Assets/Scenes/" + ProgressoHistoria.CenaGaragem + ".unity";
    private const string CaminhoNivel2 = "Assets/Scenes/" + ProgressoHistoria.PrimeiroNivel + ".unity";
    private const string CaminhoMenu = "Assets/Scenes/Menu.unity";
    private const float PixelMoldura = 4f;   // cada pixel das molduras vira 4 pixels na tela

    // ======================================================================
    // Menu
    // ======================================================================

    [MenuItem("Mr Driver/História/Fazer tudo (personagens, cenas e menu)", priority = 0)]
    private static void FazerTudo()
    {
        if (!EditorUtility.DisplayDialog("História",
                "Isto vai:\n\n• desenhar os personagens e os carros da garagem\n" +
                "• criar (ou recriar) as cenas Historia_Casa, Historia_Autoescola, Historia_Exame, Historia_Garagem e Historia_Mercado\n" +
                "• colocar a casa do Jason, o mercado e a missão no Nivel1 (salvando a cena Nivel1)\n" +
                "• ligar os botões JOGAR e TUTORIAL e montar o menu em blocos (salvando a cena Menu)\n\nContinuar?", "Fazer tudo", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        CriarPersonagens();
        string carros = MontarCarros.Montar();
        if (carros == null) return;
        if (!CriarCasa(1)) return;
        string autoescola = CriarAutoescola();
        if (autoescola == null) return;
        string nivel1 = MontarNivel1();
        if (nivel1 == null) return;
        string exame = CriarExame();
        if (exame == null) return;
        if (!CriarCasa(2)) return;
        if (!CriarMercado()) return;
        if (!CriarVideogame()) return;
        string menu = ConfigurarMenu(true);
        string blocos = MontarMenuBlocos.Montar();
        string telas = MontarMenuTelas.Montar();
        MontarSons.Montar();

        EditorUtility.DisplayDialog("História",
            "Pronto!\n\n• Personagens em Assets/Historia/Personagens\n• Cenas Historia_Casa, Historia_Garagem e Historia_Mercado criadas\n" +
            autoescola + "\n" + exame + "\n" + nivel1 + "\n" + menu + "\n" + blocos + "\n" + telas +
            "\nAperte Play na cena Menu e clique em JOGAR.", "OK");
    }

    [MenuItem("Mr Driver/História/1. Desenhar personagens", priority = 20)]
    private static void MenuPersonagens()
    {
        CriarPersonagens();
        EditorUtility.DisplayDialog("História",
            "Personagens criados em Assets/Historia/Personagens (desenhos em Assets/Imagens/Historia).\n\n" +
            "Para mudar um desenho, edite os .txt de Assets/Editor/ArteHistoria e rode este passo de novo.", "OK");
    }

    [MenuItem("Mr Driver/História/2. Criar cena da casa", priority = 21)]
    private static void MenuCasa()
    {
        if (File.Exists(CaminhoCasa) && !EditorUtility.DisplayDialog("História",
                "A cena Historia_Casa já existe. Recriar do zero? (o que você mudou nela se perde)", "Recriar", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (CriarCasa())
            EditorUtility.DisplayDialog("História", "Cena Historia_Casa criada e salva. Aperte Play para testar!", "OK");
    }

    [MenuItem("Mr Driver/História/4. Criar cena do exame final (cópia do Nivel1)", priority = 23)]
    private static void MenuExame()
    {
        if (File.Exists(CaminhoExame) && !EditorUtility.DisplayDialog("História",
                "A cena Historia_Exame já existe. Recriar a partir do Nivel1? (o que você mudou nela se perde)", "Recriar", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string resumo = CriarExame();
        if (resumo != null) EditorUtility.DisplayDialog("História", resumo + "\nAperte Play para testar!", "OK");
    }

    [MenuItem("Mr Driver/História/5. Ligar botões JOGAR e TUTORIAL do Menu", priority = 24)]
    private static void MenuBotoes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorUtility.DisplayDialog("História", ConfigurarMenu(true), "OK");
    }

    [MenuItem("Mr Driver/História/Apagar progresso da história", priority = 40)]
    private static void ApagarProgresso()
    {
        ProgressoHistoria.Apagar();
        Debug.Log("História: progresso apagado. O botão JOGAR volta a começar pela casa do Jason.");
    }

    [MenuItem("Mr Driver/História/Marcar história como concluída", priority = 41)]
    private static void ConcluirProgresso()
    {
        ProgressoHistoria.ConcluirTudo();
        Debug.Log("História: marcada como concluída. O botão JOGAR abre a lista de níveis.");
    }

    // ======================================================================
    // 1. Personagens
    // ======================================================================

    private static void CriarPersonagens()
    {
        HistoriaTextos.Jason = HistoriaArte.CriarPersonagem("Jason", "Jason", new Color(1f, 0.5f, 0.1f), 1.1f);
        HistoriaTextos.Pai = HistoriaArte.CriarPersonagem("Pai", "Betão", new Color(0.2f, 0.62f, 0.35f), 0.72f);
        HistoriaTextos.Mae = HistoriaArte.CriarPersonagem("Mae", "Dona Cida", new Color(0.93f, 0.3f, 0.58f), 1.4f);
        HistoriaTextos.Instrutor = HistoriaArte.CriarPersonagem("Instrutor", "Gervásio", new Color(0.18f, 0.38f, 0.85f), 0.9f);
        HistoriaTextos.Atendente = HistoriaArte.CriarPersonagem("Atendente", "Jussara", new Color(0.86f, 0.16f, 0.2f), 1.3f);
        HistoriaTextos.Rebelde = HistoriaArte.CriarPersonagem("RebeldeChefe", "Turbo", new Color(1f, 0.24f, 0.7f), 0.8f);
        HistoriaTextos.Rebelde2 = HistoriaArte.CriarPersonagem("Rebelde", "Capanga", new Color(0.47f, 0.24f, 0.7f), 1.05f, "Rebelde2",
            new Dictionary<char, Color32> { { 'h', new Color32(230, 60, 60, 255) }, { 'i', new Color32(170, 30, 30, 255) },
                                            { 'c', new Color32(46, 46, 54, 255) }, { 'd', new Color32(26, 26, 32, 255) },
                                            { 'a', new Color32(60, 60, 70, 255) } });
        HistoriaTextos.Rebelde3 = HistoriaArte.CriarPersonagem("Rebelde", "Capanga", new Color(0.47f, 0.24f, 0.7f), 1.15f, "Rebelde3",
            new Dictionary<char, Color32> { { 'h', new Color32(60, 120, 230, 255) }, { 'i', new Color32(36, 80, 170, 255) },
                                            { 'c', new Color32(60, 160, 90, 255) }, { 'd', new Color32(40, 120, 60, 255) },
                                            { 'a', new Color32(50, 140, 80, 255) }, { 'p', new Color32(150, 96, 66, 255) },
                                            { 'q', new Color32(120, 74, 50, 255) }, { 'u', new Color32(30, 24, 24, 255) },
                                            { 'b', new Color32(120, 70, 50, 255) } });
        AssetDatabase.SaveAssets();
    }

    private static void CarregarPersonagens()
    {
        HistoriaTextos.Jason = HistoriaArte.Carregar("Jason");
        HistoriaTextos.Pai = HistoriaArte.Carregar("Pai");
        HistoriaTextos.Mae = HistoriaArte.Carregar("Mae");
        HistoriaTextos.Instrutor = HistoriaArte.Carregar("Instrutor");
        HistoriaTextos.Atendente = HistoriaArte.Carregar("Atendente");
        HistoriaTextos.Rebelde = HistoriaArte.Carregar("RebeldeChefe");
        HistoriaTextos.Rebelde2 = HistoriaArte.Carregar("Rebelde2");
        HistoriaTextos.Rebelde3 = HistoriaArte.Carregar("Rebelde3");
        if (HistoriaTextos.Jason == null || HistoriaTextos.Pai == null || HistoriaTextos.Mae == null || HistoriaTextos.Instrutor == null ||
            HistoriaTextos.Atendente == null || HistoriaTextos.Rebelde == null || HistoriaTextos.Rebelde2 == null || HistoriaTextos.Rebelde3 == null)
            CriarPersonagens();
    }

    // ======================================================================
    // 2. Casa
    // ======================================================================

    private static bool CriarCasa() => CriarCasa(1);

    // Capítulo 1 = Historia_Casa (Jason acorda; a garagem fica trancada).
    // Capítulo 2 = Historia_Garagem (volta com a carteira, o pai mostra a garagem e o Jason sai de carro).
    private static bool CriarCasa(int capitulo)
    {
        bool cap2 = capitulo == 2;
        CarregarPersonagens();
        PersonagemHistoria jason = HistoriaTextos.Jason, pai = HistoriaTextos.Pai, mae = HistoriaTextos.Mae;

        CatalogoCarros catalogo = MontarCarros.Catalogo();
        if (catalogo == null && MontarCarros.Montar() != null) catalogo = MontarCarros.Catalogo();

        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Desenhos
        Sprite fundo = HistoriaArte.DesenharFundo(new[] { 2, 14, 19 }, 7, 16, 15);
        Sprite[] emotes = HistoriaArte.DesenharEmotes();
        Sprite sombra = HistoriaArte.DesenharSombra();

        // Grade de colisão: começa com o chão livre e os móveis vão bloqueando
        string[] planta = HistoriaArte.PlantaCasa;
        char[][] colisao = new char[planta.Length][];
        for (int y = 0; y < planta.Length; y++)
        {
            colisao[y] = new char[planta[y].Length];
            for (int x = 0; x < planta[y].Length; x++) colisao[y][x] = HistoriaArte.EhChao(x, y) ? '.' : '#';
        }

        GameObject casa = new GameObject("Casa");
        SpriteRenderer srFundo = new GameObject("Chao e paredes", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        srFundo.transform.SetParent(casa.transform, false);
        srFundo.transform.position = new Vector3(-0.5f, 0.5f, 0f);
        srFundo.sprite = fundo;
        srFundo.sortingOrder = 0;

        Transform moveis = new GameObject("Moveis").transform;
        moveis.SetParent(casa.transform, false);

        // Quarto do Jason
        Movel(moveis, colisao, "Cama", HistoriaArte.Cama(), 1, 2, 1, 2,
              cap2 ? HistoriaTextos.Cama2() : HistoriaTextos.Cama(), cap2 ? null : HistoriaTextos.CamaDepois());
        Movel(moveis, colisao, "GuardaRoupa", HistoriaArte.GuardaRoupa(), 3, 2, 2, 1,
              cap2 ? HistoriaTextos.GuardaRoupa2() : HistoriaTextos.GuardaRoupa());
        Movel(moveis, colisao, "Escrivaninha", HistoriaArte.Escrivaninha(), 5, 2, 2, 1,
              cap2 ? HistoriaTextos.Computador2() : HistoriaTextos.Computador());
        Movel(moveis, colisao, "Estante", HistoriaArte.Estante(), 1, 10, 1, 1,
              cap2 ? HistoriaTextos.Estante2() : HistoriaTextos.Estante());
        Movel(moveis, colisao, "PilhaDeRoupa", HistoriaArte.PilhaDeRoupa(), 7, 10, 1, 1,
              cap2 ? HistoriaTextos.Roupas2() : HistoriaTextos.Roupas());
        NaParede(moveis, "Janela do quarto", 2, cap2 ? HistoriaTextos.JanelaQuarto2() : HistoriaTextos.JanelaQuarto());
        NaParede(moveis, "Poster", 7, cap2 ? HistoriaTextos.Poster2() : HistoriaTextos.Poster());
        // O videogame do Jason (ligado no GAME OVER no começo da história) e o puff onde ele joga
        SpriteRenderer tvQuarto = MovelTurbo(moveis, colisao, HistoriaArte.TVQuarto(!cap2), 7, 2,
                                             cap2 ? HistoriaTextos.TVQuarto2() : HistoriaTextos.TVQuarto());
        NoChao(moveis, "Puff", HistoriaArte.Puff(), 7, 3);

        // Cozinha
        Movel(moveis, colisao, "Geladeira", HistoriaArte.Geladeira(), 9, 2, 1, 1,
              cap2 ? HistoriaTextos.Geladeira2() : HistoriaTextos.Geladeira());
        Movel(moveis, colisao, "Fogao", HistoriaArte.Fogao(), 10, 2, 1, 1, cap2 ? HistoriaTextos.Fogao2() : HistoriaTextos.Fogao());
        Movel(moveis, colisao, "Pia", HistoriaArte.Pia(), 11, 2, 2, 1, cap2 ? HistoriaTextos.Pia2() : HistoriaTextos.Pia());
        Movel(moveis, colisao, "Balcao", HistoriaArte.Balcao(), 13, 2, 1, 1,
              cap2 ? HistoriaTextos.Fruteira2() : HistoriaTextos.Fruteira());
        Movel(moveis, colisao, "Mesa", HistoriaArte.Mesa(), 11, 4, 2, 2,
              cap2 ? HistoriaTextos.Mesa2() : HistoriaTextos.Mesa(), cap2 ? null : HistoriaTextos.MesaDepois());

        // Sala
        Movel(moveis, colisao, "TV", HistoriaArte.TV(), 17, 2, 2, 1, cap2 ? HistoriaTextos.TV2() : HistoriaTextos.TV());
        Movel(moveis, colisao, "Planta", HistoriaArte.Planta(), 20, 2, 1, 1, cap2 ? HistoriaTextos.Planta2() : HistoriaTextos.Planta());
        Movel(moveis, colisao, "Sofa", HistoriaArte.Sofa(), 16, 8, 3, 1, cap2 ? HistoriaTextos.Sofa2() : HistoriaTextos.Sofa());
        Movel(moveis, colisao, "Sapateira", HistoriaArte.Sapateira(), 13, 10, 1, 1,
              cap2 ? HistoriaTextos.Sapateira2() : HistoriaTextos.Sapateira());
        NaParede(moveis, "Quadro do taxi", 15, cap2 ? HistoriaTextos.QuadroTaxi2() : HistoriaTextos.QuadroTaxi());
        NaParede(moveis, "Relogio", 16, cap2 ? HistoriaTextos.Relogio2() : HistoriaTextos.Relogio());

        // Garagem (nos dois capítulos; no 1 a porta fica trancada e o Kicks coberto)
        MoveisGaragem(moveis, colisao);
        VitrineCarro vitrine = CriarVitrine(moveis, catalogo, !cap2, HistoriaTextos.CarroCoberto());

        // Mapa (precisa existir antes dos personagens, que se registram nele)
        MapaCasa mapa = new GameObject("MapaCasa").AddComponent<MapaCasa>();
        string[] linhas = new string[colisao.Length];
        for (int y = 0; y < colisao.Length; y++) linhas[y] = new string(colisao[y]);
        mapa.DefinirColisao(linhas);
        EditorUtility.SetDirty(mapa);

        // Portas
        GameObject portaObj = new GameObject("Porta da frente");
        portaObj.transform.position = MapaCasa.ParaMundo(new Vector2Int(15, 11));
        PortaCasa porta = portaObj.AddComponent<PortaCasa>();
        porta.DefinirBloqueio(true, true);
        porta.Definir(true, cap2 ? HistoriaTextos.PortaFrenteGaragem() : null);

        PortaCasa portaGaragem = CriarPortaGaragem(cap2 ? HistoriaTextos.PortaGaragemTrancadaCap2()
                                                        : HistoriaTextos.PortaGaragemTrancadaCap1());
        PortaCasa portao = CriarPortao(HistoriaTextos.PortaoFechadoCap2());

        // Personagens (no capítulo 2 todo mundo espera o Jason na porta da frente)
        // No capítulo 1 o Jason começa sentado no puff, de frente para o videogame
        GameObject jasonObj = Ator("Jason", jason, cap2 ? new Vector2Int(15, 10) : new Vector2Int(7, 3),
                                 Direcao.Cima, sombra, emotes);
        JogadorCasa jogador = jasonObj.AddComponent<JogadorCasa>();

        GameObject paiObj = Ator("Pai (Betão)", pai, cap2 ? new Vector2Int(15, 8) : new Vector2Int(17, 5),
                                 cap2 ? Direcao.Baixo : Direcao.Cima, sombra, emotes);
        NpcHistoria npcPai = paiObj.AddComponent<NpcHistoria>();
        npcPai.Definir("Pai", cap2 ? Direcao.Baixo : Direcao.Cima,
                       cap2 ? HistoriaTextos.PaiDepoisGaragem() : HistoriaTextos.PaiDepois());

        GameObject maeObj = Ator("Mãe (Dona Cida)", mae, cap2 ? new Vector2Int(14, 8) : new Vector2Int(10, 3),
                                 cap2 ? Direcao.Baixo : Direcao.Cima, sombra, emotes);
        NpcHistoria npcMae = maeObj.AddComponent<NpcHistoria>();
        npcMae.Definir("Mae", cap2 ? Direcao.Baixo : Direcao.Cima,
                       cap2 ? HistoriaTextos.MaeDepoisGaragem() : HistoriaTextos.MaeDepois());

        // Câmera
        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camObj.tag = "MainCamera";
        Camera cam = camObj.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;   // 180 pixels de altura: em 1080p cada pixel vira 6x6
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(24, 14, 30, 255);
        camObj.transform.position = new Vector3(jasonObj.transform.position.x, jasonObj.transform.position.y, -10f);
        CameraCasa cameraCasa = camObj.AddComponent<CameraCasa>();
        Ligar(cameraCasa, "alvo", jasonObj.transform);

        // Interface, pausa e roteiro
        CriarEventSystem();
        CriarCanvasHistoria(false, out CaixaDialogo dialogo, out ObjetivoHUD objetivo, out _);
        TelaGaragem tela = cap2 ? CriarTelaGaragem(dialogo.transform, catalogo) : null;
        CriarPausa();

        if (!cap2)
        {
            RoteiroCasa roteiro = new GameObject("RoteiroCasa").AddComponent<RoteiroCasa>();
            Ligar(roteiro, "jogador", jogador);
            Ligar(roteiro, "pai", npcPai);
            Ligar(roteiro, "mae", npcMae);
            Ligar(roteiro, "porta", porta);
            Ligar(roteiro, "tvQuarto", tvQuarto);
            Ligar(roteiro, "objetivo", objetivo);
            Ligar(roteiro, "dialogo", dialogo);
            roteiro.DefinirFalas(HistoriaTextos.Acordar(), HistoriaTextos.ConversaPai(), HistoriaTextos.PaiDepois(),
                                 HistoriaTextos.MaeAntes(), HistoriaTextos.ConversaMae(), HistoriaTextos.MaeDepois(),
                                 HistoriaTextos.PortaFechada(), HistoriaTextos.Saida());
            EditorUtility.SetDirty(roteiro);
        }
        else
        {
            RoteiroGaragem roteiro = new GameObject("RoteiroGaragem").AddComponent<RoteiroGaragem>();
            Ligar(roteiro, "jogador", jogador);
            Ligar(roteiro, "pai", npcPai);
            Ligar(roteiro, "mae", npcMae);
            Ligar(roteiro, "portaGaragem", portaGaragem);
            Ligar(roteiro, "portao", portao);
            Ligar(roteiro, "objetivo", objetivo);
            Ligar(roteiro, "dialogo", dialogo);
            Ligar(roteiro, "telaGaragem", tela);
            roteiro.DefinirCaminhos(
                new List<RoteiroGaragem.Passo> { new RoteiroGaragem.Passo(Direcao.Cima, 2), new RoteiroGaragem.Passo(Direcao.Direita, 5),
                                                 new RoteiroGaragem.Passo(Direcao.Cima, 1) }, Direcao.Direita,
                new List<RoteiroGaragem.Passo> { new RoteiroGaragem.Passo(Direcao.Cima, 4) }, Direcao.Esquerda,
                new List<RoteiroGaragem.Passo> { new RoteiroGaragem.Passo(Direcao.Direita, 3), new RoteiroGaragem.Passo(Direcao.Baixo, 3) },
                Direcao.Direita, HistoriaArte.InicioGaragem);
            roteiro.DefinirFalas(HistoriaTextos.ChegadaCasa(), HistoriaTextos.PaiPortaGaragem(), HistoriaTextos.AulaGaragem(),
                                 HistoriaTextos.PaiEscolher(), HistoriaTextos.DesistiuGaragem(), HistoriaTextos.EscolheuKicks(),
                                 HistoriaTextos.PintouKicks(), HistoriaTextos.EscolheuOutro(), HistoriaTextos.MissaoMercado(),
                                 HistoriaTextos.TrocouDeNovo(), HistoriaTextos.PaiDepoisGaragem(), HistoriaTextos.MaeAntesGaragem(),
                                 HistoriaTextos.MaeDepoisGaragem(), HistoriaTextos.SaidaGaragem());
            EditorUtility.SetDirty(roteiro);
        }

        if (vitrine != null) EditorUtility.SetDirty(vitrine);

        if (!cap2)
        {
            EditorSceneManager.SaveScene(cena, CaminhoCasa);
            ColocarNoBuild(CaminhoCasa, "Assets/Scenes/" + ProgressoHistoria.CenaTutorial + ".unity");
        }
        else
        {
            EditorSceneManager.SaveScene(cena, CaminhoGaragem);
            ColocarNoBuild(CaminhoGaragem, CaminhoExame);
        }
        return true;
    }

    // Móvel com desenho; bloqueia as células e mostra falas quando o Jason examina
    private static void Movel(Transform pai, char[][] colisao, string nome, Sprite sprite, int x, int y, int w, int h,
                              List<Fala> falas, List<Fala> depois = null)
    {
        GameObject obj = new GameObject(nome, typeof(SpriteRenderer));
        obj.transform.SetParent(pai, false);
        // O desenho tem o ponto de apoio embaixo à esquerda: fica na quina de baixo da última célula
        obj.transform.position = new Vector3(x - 0.5f, -(y + h - 1) - 0.5f, 0f);

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 100 + (y + h - 1) * 10;   // mesma regra do AnimadorPersonagem

        List<Vector2Int> celulas = new List<Vector2Int>();
        for (int cy = y; cy < y + h; cy++)
            for (int cx = x; cx < x + w; cx++)
            {
                celulas.Add(new Vector2Int(cx, cy));
                colisao[cy][cx] = '#';
            }

        ObjetoFala objeto = obj.AddComponent<ObjetoFala>();
        objeto.DefinirCelulas(celulas.ToArray());
        objeto.DefinirBloqueio(true, false);
        objeto.DefinirFalas(falas, depois);
        EditorUtility.SetDirty(objeto);
    }

    // A TV do quarto com o videogame (1 célula): ao examinar, o Jason mostra o recorde do TURBO RACER 3000
    private static SpriteRenderer MovelTurbo(Transform pai, char[][] colisao, Sprite sprite, int x, int y, List<Fala> falas)
    {
        GameObject obj = new GameObject("TV do quarto (videogame)", typeof(SpriteRenderer));
        obj.transform.SetParent(pai, false);
        obj.transform.position = new Vector3(x - 0.5f, -y - 0.5f, 0f);
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 100 + y * 10;
        colisao[y][x] = '#';

        ObjetoTurbo objeto = obj.AddComponent<ObjetoTurbo>();
        objeto.DefinirCelulas(new[] { new Vector2Int(x, y) });
        objeto.DefinirBloqueio(true, false);
        objeto.DefinirFalas(falas);
        EditorUtility.SetDirty(objeto);
        return sr;
    }

    // Enfeite no chão (não bloqueia e fica embaixo dos personagens)
    private static void NoChao(Transform pai, string nome, Sprite sprite, int x, int y)
    {
        SpriteRenderer sr = new GameObject(nome, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        sr.transform.SetParent(pai, false);
        sr.transform.position = new Vector3(x - 0.5f, -y - 0.5f, 0f);
        sr.sprite = sprite;
        sr.sortingOrder = 100 + y * 10 - 2;
    }

    // Coisa pendurada na parede de cima (já desenhada no fundo): só as falas
    private static void NaParede(Transform pai, string nome, int x, List<Fala> falas)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, false);
        obj.transform.position = MapaCasa.ParaMundo(new Vector2Int(x, 1));
        ObjetoFala objeto = obj.AddComponent<ObjetoFala>();
        objeto.DefinirFalas(falas);
        EditorUtility.SetDirty(objeto);
    }

    // Personagem andando pela casa: corpo, sombra e balãozinho
    private static GameObject Ator(string nome, PersonagemHistoria p, Vector2Int celula, Direcao direcao, Sprite sombra, Sprite[] emotes)
    {
        GameObject raiz = new GameObject(nome);
        raiz.transform.position = MapaCasa.ParaMundo(celula);

        SpriteRenderer srSombra = new GameObject("Sombra", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        srSombra.transform.SetParent(raiz.transform, false);
        srSombra.transform.localPosition = new Vector3(0f, -0.38f, 0f);
        srSombra.sprite = sombra;
        srSombra.sortingOrder = 50;

        SpriteRenderer corpo = new GameObject("Corpo", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        corpo.transform.SetParent(raiz.transform, false);
        corpo.sprite = p.Quadro(direcao, 0);
        corpo.flipX = direcao == Direcao.Esquerda;

        SpriteRenderer balao = new GameObject("Balao", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        balao.transform.SetParent(raiz.transform, false);
        balao.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        balao.sortingOrder = 1000;
        balao.gameObject.SetActive(false);

        AnimadorPersonagem animador = raiz.AddComponent<AnimadorPersonagem>();
        SerializedObject dados = new SerializedObject(animador);
        dados.FindProperty("personagem").objectReferenceValue = p;
        dados.FindProperty("corpo").objectReferenceValue = corpo;
        dados.FindProperty("balao").objectReferenceValue = balao;
        dados.FindProperty("direcao").enumValueIndex = (int)direcao;
        SerializedProperty lista = dados.FindProperty("emotes");
        lista.arraySize = emotes.Length;
        for (int i = 0; i < emotes.Length; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = emotes[i];
        dados.ApplyModifiedPropertiesWithoutUndo();

        raiz.AddComponent<MovimentoGrade>();
        return raiz;
    }

    private static void CriarEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
#if ENABLE_INPUT_SYSTEM
        new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
#else
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#endif
    }

    private static void CriarPausa()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HUDPausa.prefab");
        if (prefab != null) PrefabUtility.InstantiatePrefab(prefab);
    }

    // ======================================================================
    // 3. Exame
    // ======================================================================

    // Devolve um resumo (ou null se não deu certo)
    private static string CriarExame()
    {
        if (!File.Exists(CaminhoNivel2))
        {
            EditorUtility.DisplayDialog("História", "Não encontrei " + CaminhoNivel2 + ".", "OK");
            return null;
        }

        CarregarPersonagens();
        if (File.Exists(CaminhoExame)) AssetDatabase.DeleteAsset(CaminhoExame);
        AssetDatabase.CopyAsset(CaminhoNivel2, CaminhoExame);
        Scene cena = EditorSceneManager.OpenScene(CaminhoExame, OpenSceneMode.Single);

        CarroController carro = Object.FindFirstObjectByType<CarroController>(FindObjectsInactive.Include);
        if (carro == null)
        {
            EditorUtility.DisplayDialog("História", "A cena do Nivel1 não tem o carro do jogador (CarroController).", "OK");
            return null;
        }

        string resumo = "Cena Historia_Exame criada a partir do Nivel1:\n";

        // O Nivel1 tem a missão do mercado: no exame fica só o cenário (a casa do Jason e o mercado)
        GameObject historiaNivel1 = GameObject.Find(RaizNivel1);
        Transform missaoMercado = historiaNivel1 != null ? historiaNivel1.transform.Find("Missao") : null;
        if (missaoMercado != null) Object.DestroyImmediate(missaoMercado.gameObject);

        // A chegada antiga do Nivel1 vira a vaga do exame
        bool temVaga = false;
        Vector3 posVaga = Vector3.zero;
        Quaternion rotVaga = Quaternion.identity;
        Vector2 tamVaga = new Vector2(3.5f, 6f);
        ChegadaNivel chegada = Object.FindFirstObjectByType<ChegadaNivel>(FindObjectsInactive.Include);
        if (chegada != null)
        {
            BoxCollider2D caixa = chegada.GetComponent<BoxCollider2D>();
            Vector3 escala = chegada.transform.lossyScale;
            posVaga = chegada.transform.TransformPoint(caixa.offset);
            rotVaga = chegada.transform.rotation;
            tamVaga = new Vector2(Mathf.Abs(caixa.size.x * escala.x), Mathf.Abs(caixa.size.y * escala.y)) + Vector2.one * 0.6f;
            temVaga = true;

            SerializedObject dados = new SerializedObject(chegada);
            foreach (string campo in new[] { "avisoChegada", "marcacaoChegada" })
            {
                GameObject obj = dados.FindProperty(campo).objectReferenceValue as GameObject;
                if (obj != null) obj.SetActive(false);
            }
            chegada.enabled = false;
            resumo += "• A chegada do Nivel1 foi desligada (a vaga do exame fica no mesmo lugar)\n";
        }

        foreach (TutorialNivel t in Object.FindObjectsByType<TutorialNivel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            t.gameObject.SetActive(false);

        // Trânsito mais leve: é a primeira vez do Jason (mas a cidade continua movimentada)
        foreach (GeradorTrafego g in Object.FindObjectsByType<GeradorTrafego>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SerializedObject dados = new SerializedObject(g);
            SerializedProperty qtd = dados.FindProperty("quantidade");
            qtd.intValue = Mathf.Min(qtd.intValue, 10);
            dados.ApplyModifiedPropertiesWithoutUndo();
        }

        // Pontos do percurso
        Sprite area = HistoriaArte.AreaPonto();
        Sprite pare = HistoriaArte.PlacaPare();
        Sprite vaga = HistoriaArte.PlacaVaga();
        Transform pasta = new GameObject("PercursoExame").transform;

        List<PontoExame> pontos = new List<PontoExame>();
        RedeTrafego rede = Object.FindFirstObjectByType<RedeTrafego>(FindObjectsInactive.Include);
        List<Vector2> rota = rede != null ? Rota(rede, carro.transform.position, temVaga ? (Vector2?)(Vector2)posVaga : null) : null;

        if (rota != null && rota.Count >= 2)
        {
            float faixa = rede.Faixa;
            List<(Vector2 pos, Vector2 dir)> lugares = Lugares(rota, temVaga ? 3 : 4);
            for (int i = 0; i < 3 && i < lugares.Count; i++)
            {
                Vector2 dir = lugares[i].dir;
                Vector2 direita = GrafoTrafego.Direita(dir);
                bool parar = i == 2;
                Vector2 centro = parar ? lugares[i].pos + direita * faixa : lugares[i].pos;
                Vector2 tamanho = parar ? new Vector2(faixa * 1.8f, 6f) : new Vector2(faixa * 4f + 1f, 4f);
                pontos.Add(CriarPonto(pasta, parar ? "Ponto PARE" : "Ponto " + (i + 1), centro, AnguloDe(dir), tamanho,
                                      parar ? PontoExame.Tipo.Parar : PontoExame.Tipo.Passar, area,
                                      parar ? pare : null, new Vector2(faixa + 1.2f, 0f)));
            }

            if (!temVaga && lugares.Count >= 4)
            {
                Vector2 dir = lugares[3].dir;
                posVaga = lugares[3].pos + GrafoTrafego.Direita(dir) * faixa;
                rotVaga = Quaternion.Euler(0f, 0f, AnguloDe(dir));
                tamVaga = new Vector2(faixa * 1.6f, 6f);
                temVaga = true;
            }
            resumo += "• Percurso com " + pontos.Count + " pontos seguindo as ruas do trânsito, mais a vaga\n";
        }
        else
        {
            // Sem rede de ruas: pontos em linha reta na frente do carro (mova na aba Scene)
            Vector2 frente = carro.transform.up;
            Vector2 inicio = carro.transform.position;
            for (int i = 0; i < 3; i++)
                pontos.Add(CriarPonto(pasta, i == 2 ? "Ponto PARE" : "Ponto " + (i + 1), inicio + frente * (12f * (i + 1)),
                                      AnguloDe(frente), new Vector2(6f, 4f),
                                      i == 2 ? PontoExame.Tipo.Parar : PontoExame.Tipo.Passar, area, i == 2 ? pare : null,
                                      new Vector2(4f, 0f)));
            if (!temVaga)
            {
                posVaga = inicio + frente * 48f;
                rotVaga = Quaternion.Euler(0f, 0f, AnguloDe(frente));
                temVaga = true;
            }
            resumo += "• Não achei a RedeTrafego: os pontos ficaram em linha reta na frente do carro. " +
                      "MOVA os objetos de PercursoExame para as ruas!\n";
        }

        PontoExame pontoVaga = CriarPonto(pasta, "Vaga", posVaga, rotVaga.eulerAngles.z, tamVaga, PontoExame.Tipo.Parar,
                                          area, vaga, new Vector2(0f, tamVaga.y * 0.5f + 1.5f));

        // Seta amarela em volta do carro
        GameObject setaObj = new GameObject("SetaObjetivo");
        SpriteRenderer srSeta = new GameObject("Desenho", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        srSeta.transform.SetParent(setaObj.transform, false);
        srSeta.sprite = HistoriaArte.DesenharSetaObjetivo();
        srSeta.sortingOrder = 60;
        setaObj.transform.localScale = Vector3.one * 1.4f;
        setaObj.transform.position = carro.transform.position;
        SetaObjetivo seta = setaObj.AddComponent<SetaObjetivo>();
        Ligar(seta, "carro", carro.transform);
        Ligar(seta, "desenho", srSeta);

        // Interface
        CriarEventSystem();
        CriarCanvasHistoria(true, out CaixaDialogo dialogo, out ObjetivoHUD objetivo, out TelaCarteira tela);

        // Missão
        MissaoExame missao = new GameObject("MissaoExame").AddComponent<MissaoExame>();
        Ligar(missao, "carro", carro);
        Ligar(missao, "carteira", carro.GetComponent<CarteiraMotorista>());
        Ligar(missao, "dialogo", dialogo);
        Ligar(missao, "objetivo", objetivo);
        Ligar(missao, "seta", seta);
        Ligar(missao, "telaCarteira", tela);
        Ligar(missao, "jason", HistoriaTextos.Jason);
        Ligar(missao, "instrutor", HistoriaTextos.Instrutor);

        List<MissaoExame.Etapa> etapas = new List<MissaoExame.Etapa>();
        string[] objetivos = { HistoriaTextos.ObjetivoPonto1, HistoriaTextos.ObjetivoPonto2, HistoriaTextos.ObjetivoPare };
        List<Fala>[] aoChegar = { HistoriaTextos.Ponto1(), HistoriaTextos.Ponto2(), HistoriaTextos.Pare() };
        for (int i = 0; i < pontos.Count; i++)
            etapas.Add(new MissaoExame.Etapa { objetivo = objetivos[i], ponto = pontos[i], falasAoChegar = aoChegar[i] });
        etapas.Add(new MissaoExame.Etapa { objetivo = HistoriaTextos.ObjetivoVaga, ponto = pontoVaga });

        missao.DefinirRoteiro(HistoriaTextos.AberturaExame(), etapas, HistoriaTextos.ReacoesBatida(), HistoriaTextos.Reprovado(),
                              HistoriaTextos.AprovadoPerfeito(), HistoriaTextos.Aprovado(), HistoriaTextos.Despedida());
        missao.DefinirFinal(ProgressoHistoria.CenaExame, ProgressoHistoria.CenaGaragem, true, 5, new List<Fala>());
        EditorUtility.SetDirty(missao);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        ColocarNoBuild(CaminhoExame, File.Exists(CaminhoAutoescola) ? CaminhoAutoescola : CaminhoCasa);
        ColocarNoBuild(CaminhoNivel2, File.Exists(CaminhoGaragem) ? CaminhoGaragem : CaminhoExame);

        resumo += "• Trânsito reduzido para no máximo 6 carros\n• Instrutor, objetivos e carteira de motorista prontos\n";
        resumo = resumo.Replace("Cena Historia_Exame criada", "Cena Historia_Exame (exame final) criada");
        return resumo;
    }

    private static PontoExame CriarPonto(Transform pasta, string nome, Vector2 posicao, float angulo, Vector2 tamanho,
                                         PontoExame.Tipo tipo, Sprite area, Sprite placa, Vector2 posPlaca)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pasta, false);
        obj.transform.position = posicao;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, angulo);

        SpriteRenderer marcacao = new GameObject("Marcacao", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        marcacao.transform.SetParent(obj.transform, false);
        marcacao.sprite = area;
        marcacao.drawMode = SpriteDrawMode.Sliced;
        marcacao.size = tamanho;
        marcacao.sortingOrder = 6;   // acima da pista, abaixo dos pedestres (8) e do carro (10)

        PontoExame ponto = obj.AddComponent<PontoExame>();
        ponto.Definir(tipo, tamanho);
        Ligar(ponto, "marcacao", marcacao);

        if (placa != null)
        {
            SpriteRenderer srPlaca = new GameObject("Placa", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            srPlaca.transform.SetParent(obj.transform, false);
            srPlaca.transform.localPosition = posPlaca;
            srPlaca.transform.rotation = Quaternion.identity;   // texto sempre de pé
            srPlaca.transform.localScale = Vector3.one * 1.6f;
            srPlaca.sprite = placa;
            srPlaca.sortingOrder = 7;
            Ligar(ponto, "placa", srPlaca.transform);
        }

        obj.SetActive(false);   // a MissaoExame liga um de cada vez
        return ponto;
    }

    // O desenho e a área apontam para cima: gira para a direção da rua
    private static float AnguloDe(Vector2 dir) => Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

    // Caminho pelas ruas do trânsito: do ponto mais perto do carro até o mais perto da vaga
    // (ou até o mais longe, se não houver vaga). Se ficar curto demais, dá uma volta antes.
    private static List<Vector2> Rota(RedeTrafego rede, Vector2 carro, Vector2? vaga)
    {
        GrafoTrafego g = rede.Grafo;
        if (g == null || g.Quantidade < 2) return null;

        int inicio = MaisPerto(g, carro);
        float[] dist = Distancias(g, inicio, out int[] anterior);

        int fim = vaga.HasValue ? MaisPerto(g, vaga.Value) : MaisLonge(dist);
        List<int> caminho = Reconstruir(anterior, inicio, fim);

        if (caminho.Count < 5)
        {
            // Volta maior: vai até o ponto mais longe e depois até a vaga
            int longe = MaisLonge(dist);
            List<int> ida = Reconstruir(anterior, inicio, longe);
            Distancias(g, longe, out int[] anterior2);
            List<int> volta = Reconstruir(anterior2, longe, fim);
            if (ida.Count > 1 && volta.Count > 0)
            {
                volta.RemoveAt(0);
                ida.AddRange(volta);
                caminho = ida;
            }
        }

        if (caminho.Count < 2) return null;
        List<Vector2> pontos = new List<Vector2>();
        foreach (int i in caminho) pontos.Add(g.Posicao(i));
        return pontos;
    }

    private static int MaisPerto(GrafoTrafego g, Vector2 p)
    {
        int melhor = 0;
        for (int i = 1; i < g.Quantidade; i++)
            if ((g.Posicao(i) - p).sqrMagnitude < (g.Posicao(melhor) - p).sqrMagnitude) melhor = i;
        return melhor;
    }

    private static int MaisLonge(float[] dist)
    {
        int melhor = 0;
        for (int i = 0; i < dist.Length; i++)
            if (!float.IsInfinity(dist[i]) && dist[i] > dist[melhor]) melhor = i;
        return melhor;
    }

    // Dijkstra simples (a rede é pequena)
    private static float[] Distancias(GrafoTrafego g, int origem, out int[] anterior)
    {
        int n = g.Quantidade;
        float[] dist = new float[n];
        anterior = new int[n];
        bool[] feito = new bool[n];
        for (int i = 0; i < n; i++) { dist[i] = float.PositiveInfinity; anterior[i] = -1; }
        dist[origem] = 0f;

        for (int passo = 0; passo < n; passo++)
        {
            int u = -1;
            for (int i = 0; i < n; i++)
                if (!feito[i] && !float.IsInfinity(dist[i]) && (u < 0 || dist[i] < dist[u])) u = i;
            if (u < 0) break;
            feito[u] = true;

            foreach (int v in g.Vizinhos(u))
            {
                float d = dist[u] + Vector2.Distance(g.Posicao(u), g.Posicao(v));
                if (d < dist[v]) { dist[v] = d; anterior[v] = u; }
            }
        }
        return dist;
    }

    private static List<int> Reconstruir(int[] anterior, int inicio, int fim)
    {
        List<int> caminho = new List<int>();
        for (int atual = fim; atual >= 0; atual = anterior[atual])
        {
            caminho.Insert(0, atual);
            if (atual == inicio) return caminho;
        }
        return new List<int> { inicio };
    }

    // Escolhe "quantos" lugares espalhados pela rota, no meio dos trechos de rua (longe dos cruzamentos)
    private static List<(Vector2 pos, Vector2 dir)> Lugares(List<Vector2> rota, int quantos)
    {
        int trechos = rota.Count - 1;
        List<(Vector2, Vector2)> lugares = new List<(Vector2, Vector2)>();
        Dictionary<int, int> porTrecho = new Dictionary<int, int>();
        int[] escolhidos = new int[quantos];

        for (int k = 0; k < quantos; k++)
        {
            int t = Mathf.Clamp(Mathf.RoundToInt((k + 1f) / (quantos + 1f) * trechos - 0.5f), 0, trechos - 1);
            if (k == quantos - 1 && quantos > 1) t = trechos - 1;
            escolhidos[k] = t;
            porTrecho[t] = porTrecho.TryGetValue(t, out int c) ? c + 1 : 1;
        }

        Dictionary<int, int> usados = new Dictionary<int, int>();
        foreach (int t in escolhidos)
        {
            int total = porTrecho[t];
            int ordem = usados.TryGetValue(t, out int u) ? u + 1 : 1;
            usados[t] = ordem;
            Vector2 a = rota[t], b = rota[t + 1];
            float f = (float)ordem / (total + 1);
            lugares.Add((Vector2.Lerp(a, b, f), (b - a).normalized));
        }
        return lugares;
    }

    // ======================================================================
    // 4. Menu
    // ======================================================================

    private static string ConfigurarMenu(bool salvar)
    {
        if (!File.Exists(CaminhoMenu)) return "Não encontrei " + CaminhoMenu + ".";
        Scene cena = EditorSceneManager.OpenScene(CaminhoMenu, OpenSceneMode.Single);

        MenuController menu = Object.FindFirstObjectByType<MenuController>(FindObjectsInactive.Include);
        if (menu == null) return "A cena Menu não tem o MenuController.";

        string resumo = "Menu:\n";
        Button jogar = AcharBotao("BtnJogar");
        Button tutorial = AcharBotao("BtnTutorial");
        resumo += jogar != null ? "• JOGAR: " + Trocar(jogar, "AbrirNiveis", menu, menu.Jogar) + "\n" : "• Não achei o BtnJogar\n";
        resumo += tutorial != null ? "• TUTORIAL: " + Trocar(tutorial, "Tutorial", menu, menu.AbrirTutorial) + "\n"
                                   : "• Não achei o BtnTutorial\n";

        EditorSceneManager.MarkSceneDirty(cena);

        // Botões da lista de níveis: 1º = história, 2º = Nivel1...
        EditorApplication.ExecuteMenuItem("Mr Driver/Níveis/Configurar botões de nível no Menu");

        ColocarNoBuild("Assets/Scenes/" + ProgressoHistoria.CenaTutorial + ".unity", CaminhoMenu);
        if (File.Exists(CaminhoCasa)) ColocarNoBuild(CaminhoCasa, "Assets/Scenes/" + ProgressoHistoria.CenaTutorial + ".unity");
        if (File.Exists(CaminhoAutoescola)) ColocarNoBuild(CaminhoAutoescola, CaminhoCasa);
        if (File.Exists(CaminhoExame)) ColocarNoBuild(CaminhoExame, File.Exists(CaminhoAutoescola) ? CaminhoAutoescola : CaminhoCasa);
        if (File.Exists(CaminhoGaragem)) ColocarNoBuild(CaminhoGaragem, CaminhoExame);
        if (File.Exists(CaminhoMercado)) ColocarNoBuild(CaminhoMercado, CaminhoNivel2);

        if (salvar)
        {
            EditorSceneManager.SaveScene(cena);
            resumo += "• Cena Menu salva\n";
        }
        return resumo;
    }

    private static Button AcharBotao(string nome)
    {
        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (b.name == nome) return b;
        return null;
    }

    // Troca a chamada antiga do botão pela nova (o som do clique continua)
    private static string Trocar(Button botao, string metodoVelho, Object alvo, UnityAction novo)
    {
        Undo.RecordObject(botao, "Ligar botão da história");
        int removidos = 0;
        bool jaTem = false;
        for (int i = botao.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            string metodo = botao.onClick.GetPersistentMethodName(i);
            if (metodo == metodoVelho)
            {
                UnityEventTools.RemovePersistentListener(botao.onClick, i);
                removidos++;
            }
            else if (metodo == novo.Method.Name && botao.onClick.GetPersistentTarget(i) == alvo) jaTem = true;
        }
        if (!jaTem) UnityEventTools.AddPersistentListener(botao.onClick, novo);
        EditorUtility.SetDirty(botao);
        return jaTem && removidos == 0 ? "já estava ligado" : "agora chama MenuController." + novo.Method.Name;
    }

    // Coloca a cena na lista do build logo depois de outra (se ainda não estiver)
    private static void ColocarNoBuild(string caminho, string depoisDe)
    {
        List<EditorBuildSettingsScene> cenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int existente = cenas.FindIndex(c => c.path == caminho);
        if (existente >= 0)
        {
            cenas[existente].enabled = true;
            EditorBuildSettings.scenes = cenas.ToArray();
            return;
        }

        int depois = cenas.FindIndex(c => c.path == depoisDe);
        EditorBuildSettingsScene nova = new EditorBuildSettingsScene(caminho, true);
        if (depois >= 0) cenas.Insert(depois + 1, nova);
        else cenas.Add(nova);
        EditorBuildSettings.scenes = cenas.ToArray();
    }

    // ======================================================================
    // Interface: caixa de diálogo, objetivo e (no exame) a carteira de motorista
    // ======================================================================

    private static void CriarCanvasHistoria(bool comCarteira, out CaixaDialogo dialogo, out ObjetivoHUD objetivo, out TelaCarteira carteira)
    {
        Sprite molduraDialogo = HistoriaArte.MolduraDialogo();
        Sprite molduraRetrato = HistoriaArte.MolduraRetrato();
        Sprite molduraBranca = HistoriaArte.MolduraBranca();
        Sprite molduraEscura = HistoriaArte.MolduraEscura();
        Sprite setaDialogo = HistoriaArte.SetaDialogo();

        GameObject canvasObj = new GameObject("CanvasHistoria", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                                              typeof(GraphicRaycaster), typeof(AudioSource));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;   // acima do HUD e das dicas
        CanvasScaler escala = canvasObj.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;

        AudioSource voz = canvasObj.GetComponent<AudioSource>();
        voz.playOnAwake = false;
        voz.outputAudioMixerGroup = GrupoEfeitos();

        // ---------- Caixa de diálogo ----------
        RectTransform pnl = Ret(canvasObj.transform, "PnlDialogo", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1560f, 290f));
        Imagem(pnl, molduraDialogo, true);
        CanvasGroup grupo = pnl.gameObject.AddComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;

        RectTransform caixaRetrato = Ret(pnl, "CaixaRetrato", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                         new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(230f, 230f));
        Imagem(caixaRetrato, molduraRetrato, true);
        RectTransform retratoRt = Ret(caixaRetrato, "Retrato", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                                      Vector2.zero, Vector2.zero);
        retratoRt.offsetMin = new Vector2(16f, 16f);
        retratoRt.offsetMax = new Vector2(-16f, -16f);
        Image retrato = Imagem(retratoRt, HistoriaTextos.Jason != null ? HistoriaTextos.Jason.retrato : null, false);
        retrato.preserveAspect = true;

        RectTransform placa = Ret(pnl, "PlacaNome", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f),
                                  new Vector2(40f, -18f), new Vector2(330f, 66f));
        Image fundoNome = Imagem(placa, molduraBranca, true);
        fundoNome.color = new Color(1f, 0.5f, 0.1f);
        TMP_Text nome = Texto(placa, "TxtNome", "JASON", 34, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Esticar(nome.rectTransform, 10f);

        TMP_Text texto = Texto(pnl, "TxtFala", "Texto da fala.", 40, FontStyles.Normal, new Color32(42, 14, 56, 255),
                               TextAlignmentOptions.TopLeft);
        RectTransform rtTexto = texto.rectTransform;
        rtTexto.anchorMin = Vector2.zero;
        rtTexto.anchorMax = Vector2.one;
        rtTexto.offsetMin = new Vector2(300f, 40f);
        rtTexto.offsetMax = new Vector2(-70f, -52f);
        texto.lineSpacing = 6f;

        RectTransform seta = Ret(pnl, "Seta", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                                 new Vector2(-56f, 42f), new Vector2(54f, 36f));
        Imagem(seta, setaDialogo, false);

        dialogo = canvasObj.AddComponent<CaixaDialogo>();
        SerializedObject d = new SerializedObject(dialogo);
        d.FindProperty("painel").objectReferenceValue = pnl.gameObject;
        d.FindProperty("grupo").objectReferenceValue = grupo;
        d.FindProperty("janela").objectReferenceValue = pnl;
        d.FindProperty("caixaRetrato").objectReferenceValue = caixaRetrato.gameObject;
        d.FindProperty("retrato").objectReferenceValue = retrato;
        d.FindProperty("placaNome").objectReferenceValue = placa.gameObject;
        d.FindProperty("fundoNome").objectReferenceValue = fundoNome;
        d.FindProperty("textoNome").objectReferenceValue = nome;
        d.FindProperty("texto").objectReferenceValue = texto;
        d.FindProperty("seta").objectReferenceValue = seta;
        d.FindProperty("voz").objectReferenceValue = voz;
        d.ApplyModifiedPropertiesWithoutUndo();
        pnl.gameObject.SetActive(false);

        // ---------- Objetivo ----------
        RectTransform caixaObj = Ret(canvasObj.transform, "Objetivo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                     new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(820f, 100f));
        Imagem(caixaObj, molduraEscura, true);
        CanvasGroup grupoObj = caixaObj.gameObject.AddComponent<CanvasGroup>();
        grupoObj.blocksRaycasts = false;
        grupoObj.alpha = 0f;
        TMP_Text rotulo = Texto(caixaObj, "TxtRotulo", "OBJETIVO", 22, FontStyles.Bold, new Color(1f, 0.12f, 0.8f),
                                TextAlignmentOptions.Center);
        RectTransform rtRotulo = rotulo.rectTransform;
        rtRotulo.anchorMin = new Vector2(0f, 1f);
        rtRotulo.anchorMax = new Vector2(1f, 1f);
        rtRotulo.pivot = new Vector2(0.5f, 1f);
        rtRotulo.anchoredPosition = new Vector2(0f, -14f);
        rtRotulo.sizeDelta = new Vector2(0f, 28f);
        TMP_Text textoObj = Texto(caixaObj, "TxtObjetivo", "Objetivo", 32, FontStyles.Bold, Color.white,
                                  TextAlignmentOptions.Center);
        RectTransform rtObj = textoObj.rectTransform;
        rtObj.anchorMin = Vector2.zero;
        rtObj.anchorMax = Vector2.one;
        rtObj.offsetMin = new Vector2(30f, 12f);
        rtObj.offsetMax = new Vector2(-30f, -40f);

        objetivo = canvasObj.AddComponent<ObjetivoHUD>();
        SerializedObject o = new SerializedObject(objetivo);
        o.FindProperty("caixa").objectReferenceValue = caixaObj;
        o.FindProperty("grupo").objectReferenceValue = grupoObj;
        o.FindProperty("texto").objectReferenceValue = textoObj;
        o.ApplyModifiedPropertiesWithoutUndo();

        carteira = comCarteira ? CriarTelaCarteira(canvasObj.transform, molduraRetrato) : null;
        pnl.SetAsLastSibling();
    }

    private static TelaCarteira CriarTelaCarteira(Transform canvas, Sprite molduraRetrato)
    {
        Sprite molduraCartao = HistoriaArte.MolduraCartao();
        Sprite molduraCarimbo = HistoriaArte.MolduraCarimbo();
        Color verde = new Color32(40, 120, 70, 255);

        RectTransform pnl = Ret(canvas, "PnlCarteira", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image escuro = pnl.gameObject.AddComponent<Image>();
        escuro.color = new Color(0.05f, 0.02f, 0.08f, 0.8f);
        CanvasGroup grupo = pnl.gameObject.AddComponent<CanvasGroup>();

        RectTransform cartao = Ret(pnl, "Cartao", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                   new Vector2(0f, 30f), new Vector2(1000f, 600f));
        Imagem(cartao, molduraCartao, true);

        RectTransform faixa = Ret(cartao, "Faixa", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                                  new Vector2(0f, -28f), new Vector2(-56f, 92f));
        faixa.gameObject.AddComponent<Image>().color = verde;
        TMP_Text titulo = Texto(faixa, "TxtTitulo", "CARTEIRA DE MOTORISTA", 46, FontStyles.Bold, Color.white,
                                TextAlignmentOptions.Center);
        Esticar(titulo.rectTransform, 0f);

        RectTransform caixaFoto = Ret(cartao, "Foto", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                      new Vector2(56f, -40f), new Vector2(290f, 290f));
        Imagem(caixaFoto, molduraRetrato, true);
        RectTransform fotoRt = Ret(caixaFoto, "Retrato", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        fotoRt.offsetMin = new Vector2(18f, 18f);
        fotoRt.offsetMax = new Vector2(-18f, -18f);
        Image foto = Imagem(fotoRt, HistoriaTextos.Jason != null ? HistoriaTextos.Jason.retrato : null, false);
        foto.preserveAspect = true;

        TMP_Text nome = Texto(cartao, "TxtNome", "JASON", 64, FontStyles.Bold, verde, TextAlignmentOptions.TopLeft);
        Posicionar(nome.rectTransform, new Vector2(0f, 1f), new Vector2(390f, -150f), new Vector2(560f, 80f));
        TMP_Text detalhes = Texto(cartao, "TxtDetalhes", "CATEGORIA: B", 32, FontStyles.Normal, new Color32(40, 50, 40, 255),
                                  TextAlignmentOptions.TopLeft);
        Posicionar(detalhes.rectTransform, new Vector2(0f, 1f), new Vector2(390f, -240f), new Vector2(560f, 230f));
        detalhes.lineSpacing = 12f;

        RectTransform carimbo = Ret(cartao, "Carimbo", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                                    new Vector2(-230f, 105f), new Vector2(400f, 120f));
        carimbo.localRotation = Quaternion.Euler(0f, 0f, 12f);
        Imagem(carimbo, molduraCarimbo, true);
        TMP_Text textoCarimbo = Texto(carimbo, "TxtCarimbo", "APROVADO!", 64, FontStyles.Bold, new Color32(220, 30, 50, 255),
                                      TextAlignmentOptions.Center);
        Esticar(textoCarimbo.rectTransform, 0f);

        TMP_Text continuar = Texto(pnl, "TxtContinuar", "Aperte ENTER para continuar", 30, FontStyles.Italic, Color.white,
                                   TextAlignmentOptions.Center);
        Posicionar(continuar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(900f, 50f));
        continuar.rectTransform.pivot = new Vector2(0.5f, 0f);

        TelaCarteira tela = canvas.gameObject.AddComponent<TelaCarteira>();
        SerializedObject t = new SerializedObject(tela);
        t.FindProperty("painel").objectReferenceValue = pnl.gameObject;
        t.FindProperty("grupo").objectReferenceValue = grupo;
        t.FindProperty("cartao").objectReferenceValue = cartao;
        t.FindProperty("retrato").objectReferenceValue = foto;
        t.FindProperty("textoNome").objectReferenceValue = nome;
        t.FindProperty("textoDetalhes").objectReferenceValue = detalhes;
        t.FindProperty("carimbo").objectReferenceValue = carimbo;
        t.FindProperty("textoContinuar").objectReferenceValue = continuar;
        t.ApplyModifiedPropertiesWithoutUndo();

        pnl.gameObject.SetActive(false);
        return tela;
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

    private static void Posicionar(RectTransform rt, Vector2 ancora, Vector2 posicao, Vector2 tamanho)
    {
        rt.anchorMin = rt.anchorMax = ancora;
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
    }

    private static void Esticar(RectTransform rt, float margem)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margem, 0f);
        rt.offsetMax = new Vector2(-margem, 0f);
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

    // Grupo "SFX"/"Efeitos" do mixer do jogo (o volume de efeitos do menu passa a valer para as vozes)
    private static AudioMixerGroup GrupoEfeitos()
    {
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath("Assets/MixerMrDriver.mixer"))
        {
            AudioMixerGroup grupo = obj as AudioMixerGroup;
            if (grupo == null) continue;
            string n = grupo.name.ToLower();
            if (n.Contains("sfx") || n.Contains("efeito")) return grupo;
        }
        return null;
    }

    // Preenche um campo [SerializeField] privado de um componente
    private static void Ligar(Object componente, string campo, Object valor)
    {
        SerializedObject dados = new SerializedObject(componente);
        SerializedProperty prop = dados.FindProperty(campo);
        if (prop == null)
        {
            Debug.LogWarning("MontarHistoria: campo \"" + campo + "\" não existe em " + componente.GetType().Name);
            return;
        }
        prop.objectReferenceValue = valor;
        dados.ApplyModifiedPropertiesWithoutUndo();
    }
}
