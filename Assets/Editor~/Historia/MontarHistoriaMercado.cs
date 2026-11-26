using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Cena Historia_Mercado: dentro do Mercado Bom Preço, no meio do Nivel1 (estilo Pokémon, igual à casa).
// O Jason pega as compras, conversa com a Jussara no caixa e a Gangue do Escapamento invade o mercado.
// Menu: Mr Driver > História > 9. Criar cena do mercado (compras e a gangue)
public static partial class MontarHistoria
{
    private const string CaminhoMercado = "Assets/Scenes/" + ProgressoHistoria.CenaMercado + ".unity";

    [MenuItem("Mr Driver/História/9. Criar cena do mercado (compras e a gangue)", priority = 28)]
    private static void MenuMercado()
    {
        if (File.Exists(CaminhoMercado) && !EditorUtility.DisplayDialog("História",
                "A cena Historia_Mercado já existe. Recriar do zero? (o que você mudou nela se perde)", "Recriar", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (CriarMercado())
            EditorUtility.DisplayDialog("História", "Cena Historia_Mercado criada e salva. Aperte Play para testar!", "OK");
    }

    private static bool CriarMercado()
    {
        CarregarPersonagens();
        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Desenhos (o fundo define a planta usada pela grade de colisão)
        Sprite fundo = HistoriaArte.DesenharFundoMercado();
        Sprite[] emotes = HistoriaArte.DesenharEmotes();
        Sprite sombra = HistoriaArte.DesenharSombra();
        Sprite brilho = HistoriaArte.Brilho();

        string[] planta = HistoriaArte.PlantaMercado;
        char[][] colisao = new char[planta.Length][];
        for (int y = 0; y < planta.Length; y++)
        {
            colisao[y] = new char[planta[y].Length];
            for (int x = 0; x < planta[y].Length; x++) colisao[y][x] = HistoriaArte.EhChao(x, y) ? '.' : '#';
        }

        GameObject mercado = new GameObject("Mercado");
        SpriteRenderer srFundo = new GameObject("Chao e paredes", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        srFundo.transform.SetParent(mercado.transform, false);
        srFundo.transform.position = new Vector3(-0.5f, 0.5f, 0f);
        srFundo.sprite = fundo;

        Transform moveis = new GameObject("Moveis").transform;
        moveis.SetParent(mercado.transform, false);

        // Prateleiras com as compras da lista (brilham até o Jason pegar)
        ItemMercado(moveis, colisao, "Geladeira de laticinios", HistoriaArte.Geladeira("GeladeiraLaticinios", 3, 0), 2, 2, 4, 1,
                    "leite", HistoriaTextos.PegouLeite(), brilho);
        ItemMercado(moveis, colisao, "Padaria", HistoriaArte.Padaria(), 9, 2, 4, 1, "pao", HistoriaTextos.PegouPao(), brilho);
        ItemMercado(moveis, colisao, "Gondola de cafe", HistoriaArte.Gondola("GondolaMatinais", 0), 3, 5, 5, 1,
                    "cafe", HistoriaTextos.PegouCafe(), brilho);
        ItemMercado(moveis, colisao, "Gondola de ovos", HistoriaArte.Gondola("GondolaMercearia", 1), 10, 5, 5, 1,
                    "ovos", HistoriaTextos.PegouOvos(), brilho);
        ItemMercado(moveis, colisao, "Gondola de confeitaria", HistoriaArte.Gondola("GondolaConfeitaria", 3), 10, 8, 5, 1,
                    "fermento", HistoriaTextos.PegouFermento(), brilho);

        // O resto do mercado
        Movel(moveis, colisao, "Geladeira de frios", HistoriaArte.Geladeira("GeladeiraFrios", 1, 1), 6, 2, 2, 1, HistoriaTextos.GeladeiraFrios());
        Movel(moveis, colisao, "Hortifruti", HistoriaArte.Hortifruti(), 14, 2, 3, 1, HistoriaTextos.Hortifruti());
        Movel(moveis, colisao, "Geladeira de bebidas", HistoriaArte.Geladeira("GeladeiraBebidas", 2, 2), 18, 2, 3, 1, HistoriaTextos.Bebidas());
        Movel(moveis, colisao, "Gondola de limpeza", HistoriaArte.Gondola("GondolaLimpeza", 2), 3, 8, 5, 1, HistoriaTextos.Limpeza());
        Movel(moveis, colisao, "Caixa fechado", HistoriaArte.CaixaMercado(true), 18, 11, 2, 1, HistoriaTextos.CaixaFechado());
        Movel(moveis, colisao, "Latas", HistoriaArte.Latas(false), 14, 11, 2, 1, HistoriaTextos.Latas());
        Movel(moveis, colisao, "Carrinhos", HistoriaArte.Carrinhos(), 1, 12, 2, 1, HistoriaTextos.Carrinhos());
        Movel(moveis, colisao, "Placa proibido moto", HistoriaArte.PlacaProibidoMoto(), 8, 12, 1, 1, HistoriaTextos.AvisoMercado());
        Sprite latasDerrubadas = HistoriaArte.Latas(true);
        ItemMercado(moveis, colisao, "Pudim em oferta", HistoriaArte.PudimOferta(), 6, 11, 2, 1, "pudim", HistoriaTextos.Pudim(), null);
        ItemMercado(moveis, colisao, "Caixa", HistoriaArte.CaixaMercado(false), 18, 9, 2, 1, "caixa", null, null);

        Transform latas = moveis.Find("Latas");

        // Mapa
        MapaCasa mapa = new GameObject("MapaCasa").AddComponent<MapaCasa>();
        string[] linhas = new string[colisao.Length];
        for (int y = 0; y < colisao.Length; y++) linhas[y] = new string(colisao[y]);
        mapa.DefinirColisao(linhas);
        EditorUtility.SetDirty(mapa);

        // Porta automática (saída para o estacionamento)
        GameObject portaObj = new GameObject("Porta do mercado");
        portaObj.transform.position = new Vector3(10.5f, -13f, 0f);
        PortaCasa porta = portaObj.AddComponent<PortaCasa>();
        porta.DefinirCelulas(new[] { new Vector2Int(10, 13), new Vector2Int(11, 13) });
        porta.DefinirBloqueio(true, true);
        porta.Definir(true, HistoriaTextos.PortaMercadoAntes());

        // Personagens
        GameObject jasonObj = Ator("Jason", HistoriaTextos.Jason, new Vector2Int(10, 12), Direcao.Cima, sombra, emotes);
        JogadorCasa jogador = jasonObj.AddComponent<JogadorCasa>();

        GameObject atendenteObj = Ator("Atendente (Jussara)", HistoriaTextos.Atendente, new Vector2Int(20, 9), Direcao.Esquerda, sombra, emotes);
        NpcHistoria atendente = atendenteObj.AddComponent<NpcHistoria>();
        atendente.Definir("Atendente", Direcao.Esquerda, HistoriaTextos.AtendenteDepois());

        // A gangue espera do lado de fora (na porta) e entra na hora da baderna
        List<RoteiroMercado.Rebelde> gangue = new List<RoteiroMercado.Rebelde>
        {
            Rebelde("Rebelde (Turbo)", HistoriaTextos.Rebelde, new Vector2Int(11, 13), sombra, emotes,
                    new[] { P(Direcao.Cima, 2), P(Direcao.Direita, 2) }, Direcao.Direita,
                    new[] { P(Direcao.Esquerda, 2), P(Direcao.Baixo, 2) }),
            Rebelde("Rebelde (capanga 1)", HistoriaTextos.Rebelde2, new Vector2Int(10, 13), sombra, emotes,
                    new[] { P(Direcao.Cima, 3), P(Direcao.Esquerda, 3) }, Direcao.Baixo,
                    new[] { P(Direcao.Direita, 3), P(Direcao.Baixo, 3) }),
            Rebelde("Rebelde (capanga 2)", HistoriaTextos.Rebelde3, new Vector2Int(11, 13), sombra, emotes,
                    new[] { P(Direcao.Cima, 3), P(Direcao.Direita, 4) }, Direcao.Direita,
                    new[] { P(Direcao.Esquerda, 4), P(Direcao.Baixo, 3) }),
        };

        // Câmera
        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camObj.tag = "MainCamera";
        Camera cam = camObj.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(24, 14, 30, 255);
        camObj.transform.position = new Vector3(jasonObj.transform.position.x, jasonObj.transform.position.y, -10f);
        Ligar(camObj.AddComponent<CameraCasa>(), "alvo", jasonObj.transform);

        // Interface, pausa e roteiro
        CriarEventSystem();
        CriarCanvasHistoria(false, out CaixaDialogo dialogo, out ObjetivoHUD objetivo, out _);
        CriarPausa();

        RoteiroMercado roteiro = new GameObject("RoteiroMercado").AddComponent<RoteiroMercado>();
        Ligar(roteiro, "jogador", jogador);
        Ligar(roteiro, "atendente", atendente);
        Ligar(roteiro, "porta", porta);
        Ligar(roteiro, "objetivo", objetivo);
        Ligar(roteiro, "dialogo", dialogo);
        roteiro.DefinirCompras(new List<RoteiroMercado.Item>
            {
                new RoteiroMercado.Item { id = "leite", nome = "leite" },
                new RoteiroMercado.Item { id = "ovos", nome = "ovos" },
                new RoteiroMercado.Item { id = "pao", nome = "pão de forma" },
                new RoteiroMercado.Item { id = "cafe", nome = "café" },
                new RoteiroMercado.Item { id = "fermento", nome = "fermento" },
            }, gangue, latas != null ? latas.GetComponent<SpriteRenderer>() : null, latasDerrubadas,
            latas != null ? latas.GetComponent<ObjetoFala>() : null);
        roteiro.DefinirFalas(HistoriaTextos.EntradaMercado(), HistoriaTextos.JaPegou(), HistoriaTextos.CaixaAntes(),
                             HistoriaTextos.Caixa(), HistoriaTextos.ChegadaRebeldes(), HistoriaTextos.Baderna(),
                             HistoriaTextos.DepoisBaderna(), HistoriaTextos.AtendenteDepois(), HistoriaTextos.LatasDerrubadas(),
                             HistoriaTextos.SaidaMercado());
        EditorUtility.SetDirty(roteiro);

        EditorSceneManager.SaveScene(cena, CaminhoMercado);
        ColocarNoBuild(CaminhoMercado, CaminhoNivel2);
        return true;
    }

    private static RoteiroGaragem.Passo P(Direcao d, int n) => new RoteiroGaragem.Passo(d, n);

    private static RoteiroMercado.Rebelde Rebelde(string nome, PersonagemHistoria p, Vector2Int celula, Sprite sombra, Sprite[] emotes,
                                                  RoteiroGaragem.Passo[] entrada, Direcao olhando, RoteiroGaragem.Passo[] saida)
    {
        GameObject obj = Ator(nome, p, celula, Direcao.Cima, sombra, emotes);
        NpcHistoria npc = obj.AddComponent<NpcHistoria>();
        npc.Definir("Rebelde", Direcao.Cima, new List<Fala>());
        return new RoteiroMercado.Rebelde
        {
            npc = npc,
            entrada = new List<RoteiroGaragem.Passo>(entrada),
            olhando = olhando,
            saida = new List<RoteiroGaragem.Passo>(saida),
        };
    }

    // Prateleira (ou caixa) que o roteiro do mercado controla
    private static ObjetoRoteiro ItemMercado(Transform pai, char[][] colisao, string nome, Sprite sprite, int x, int y, int w, int h,
                                             string id, List<Fala> falas, Sprite brilho)
    {
        GameObject obj = new GameObject(nome, typeof(SpriteRenderer));
        obj.transform.SetParent(pai, false);
        obj.transform.position = new Vector3(x - 0.5f, -(y + h - 1) - 0.5f, 0f);
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 100 + (y + h - 1) * 10;

        List<Vector2Int> celulas = new List<Vector2Int>();
        for (int cy = y; cy < y + h; cy++)
            for (int cx = x; cx < x + w; cx++)
            {
                celulas.Add(new Vector2Int(cx, cy));
                colisao[cy][cx] = '#';
            }

        Transform destaque = null;
        if (brilho != null)
        {
            SpriteRenderer srBrilho = new GameObject("Brilho", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            srBrilho.transform.SetParent(obj.transform, false);
            srBrilho.transform.localPosition = new Vector3(w / 2f, sprite.bounds.size.y + 0.3f, 0f);
            srBrilho.sprite = brilho;
            srBrilho.sortingOrder = 900;
            destaque = srBrilho.transform;
        }

        ObjetoRoteiro objeto = obj.AddComponent<ObjetoRoteiro>();
        objeto.DefinirCelulas(celulas.ToArray());
        objeto.DefinirBloqueio(true, false);
        objeto.Definir(id, falas, destaque);
        EditorUtility.SetDirty(objeto);
        return objeto;
    }
}
