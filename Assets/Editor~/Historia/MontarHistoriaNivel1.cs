using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Nivel1 da história: a casa da família Driver no fim da rua sem saída, o Mercado Bom Preço
// a missão de ir ao mercado e voltar para casa (MissaoMercado) e a Gangue do Escapamento (GangueRebelde,
// que só sai às ruas na volta). Tudo fica dentro do objeto "HistoriaNivel1":
// rodar de novo apaga e refaz só ele (o resto do Nivel1 não é mexido).
// Menu: Mr Driver > História > 8. Montar o Nivel1 (casa do Jason e mercado)
public static partial class MontarHistoria
{
    private const string RaizNivel1 = "HistoriaNivel1";

    [MenuItem("Mr Driver/História/8. Montar o Nivel1 (casa do Jason e mercado)", priority = 27)]
    private static void MenuNivel1()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string resumo = MontarNivel1();
        if (resumo != null) EditorUtility.DisplayDialog("História", resumo + "\nAperte Play para testar!", "OK");
    }

    // Devolve um resumo (ou null se não deu certo)
    private static string MontarNivel1()
    {
        if (!File.Exists(CaminhoNivel2))
        {
            EditorUtility.DisplayDialog("História", "Não encontrei " + CaminhoNivel2 + ".", "OK");
            return null;
        }

        CarregarPersonagens();
        Scene cena = EditorSceneManager.OpenScene(CaminhoNivel2, OpenSceneMode.Single);

        CarroController carro = Object.FindFirstObjectByType<CarroController>(FindObjectsInactive.Include);
        ChegadaNivel chegada = Object.FindFirstObjectByType<ChegadaNivel>(FindObjectsInactive.Include);
        if (carro == null || chegada == null)
        {
            EditorUtility.DisplayDialog("História", "O Nivel1 precisa ter o carro do jogador e a ChegadaNivel (a vaga do mercado).", "OK");
            return null;
        }

        GameObject velho = GameObject.Find(RaizNivel1);
        if (velho != null) Object.DestroyImmediate(velho);
        GameObject raiz = new GameObject(RaizNivel1);

        // ---------- Cenário ----------
        Transform cenario = new GameObject("Cenario").transform;
        cenario.SetParent(raiz.transform, false);

        SpriteFixo(cenario, "CasaJason", CidadeArte.CasaJason(), CidadeArte.TerrenoCasa.center, 1);
        SpriteFixo(cenario, "PredioMercado", CidadeArte.Mercado(), CidadeArte.PredioMercado.center, 1);
        SpriteFixo(cenario, "PinturaEstacionamento", CidadeArte.PinturaEstacionamento(AreaDaVaga(chegada)),
                   CidadeArte.Estacionamento.center, 1);
        SpriteFixo(cenario, "PlacaSemSaida", CidadeArte.PlacaSemSaida(), new Vector2(-25.6f, 5.0f), 4);

        Sprite arvore0 = CidadeArte.Arvore(0), arvore1 = CidadeArte.Arvore(1);
        Vector2[] arvores = { new Vector2(-31.6f, 1.2f), new Vector2(-31.2f, 11.5f), new Vector2(-31.8f, 21f),
                              new Vector2(-23.5f, 29.8f), new Vector2(-16.2f, 31.2f) };
        for (int i = 0; i < arvores.Length; i++)
            SpriteFixo(cenario, "Arvore " + (i + 1), i % 2 == 0 ? arvore0 : arvore1, arvores[i], 3);

        int vizinhas = TrocarCasasVizinhas(cena, cenario);

        // ---------- Missão ----------
        Transform missaoPasta = new GameObject("Missao").transform;
        missaoPasta.SetParent(raiz.transform, false);

        GameObject setaObj = new GameObject("SetaObjetivo");
        setaObj.transform.SetParent(missaoPasta, false);
        SpriteRenderer srSeta = new GameObject("Desenho", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        srSeta.transform.SetParent(setaObj.transform, false);
        srSeta.sprite = HistoriaArte.DesenharSetaObjetivo();
        srSeta.sortingOrder = 60;
        setaObj.transform.localScale = Vector3.one * 1.4f;
        setaObj.transform.position = carro.transform.position;
        SetaObjetivo seta = setaObj.AddComponent<SetaObjetivo>();
        Ligar(seta, "carro", carro.transform);
        Ligar(seta, "desenho", srSeta);

        CriarEventSystem();
        CriarCanvasHistoria(false, out CaixaDialogo dialogo, out ObjetivoHUD objetivo, out _);
        dialogo.transform.SetParent(missaoPasta, false);

        MissaoMercado missao = new GameObject("MissaoMercado").AddComponent<MissaoMercado>();
        missao.transform.SetParent(missaoPasta, false);
        Ligar(missao, "carro", carro);
        Ligar(missao, "carteira", carro.GetComponent<CarteiraMotorista>());
        Ligar(missao, "dialogo", dialogo);
        Ligar(missao, "objetivo", objetivo);
        Ligar(missao, "seta", seta);
        Ligar(missao, "chegada", chegada);
        missao.DefinirFalas(HistoriaTextos.AberturaMercado(), HistoriaTextos.ReacoesBatidaMercado(),
                            HistoriaTextos.ChegadaMercadoPerfeita(), HistoriaTextos.ChegadaMercado());
        missao.DefinirFalasVolta(HistoriaTextos.AberturaVolta(), HistoriaTextos.ReacoesRebelde(),
                                 HistoriaTextos.VoltouParaCasaPerfeito(), HistoriaTextos.VoltouParaCasa());

        // Gangue do Escapamento (carros pichados e motos), solta só na volta do mercado
        GangueRebelde gangue = new GameObject("GangueDoEscapamento").AddComponent<GangueRebelde>();
        gangue.transform.SetParent(missaoPasta, false);
        gangue.Definir(RebeldesArte.CarrosPichados(MontarCarros.Catalogo()), RebeldesArte.Motos());
        EditorUtility.SetDirty(gangue);
        Ligar(missao, "gangue", gangue);
        EditorUtility.SetDirty(missao);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);

        return "Nivel1 (objeto " + RaizNivel1 + "):\n" +
               "• Casa da família Driver no fim da rua sem saída, com placa e árvores\n" +
               "• " + vizinhas + " terrenos de casas vizinhas redesenhados no mesmo estilo (as imagens antigas ficaram desativadas em \"Casas\")\n" +
               "• Mercado Bom Preço e a pintura do estacionamento em volta da vaga de chegada\n" +
               "• Missão: ida ao mercado (entra na cena Historia_Mercado) e volta para casa\n" +
               "• Gangue do Escapamento: carros pichados e motos que aparecem na volta\n";
    }

    // ---------- Casas vizinhas ----------

    // Troca as casas antigas do mapa (imagens Casa1..Casa4 dentro do objeto "Casas") por casas em pixel art
    // no estilo da casa do Jason. Cada casa nova ocupa exatamente o mesmo terreno e fica virada para a mesma rua.
    // As antigas só são desativadas (para voltar, é só marcar o objeto delas de novo).
    private static int TrocarCasasVizinhas(Scene cena, Transform cenario)
    {
        GameObject raizCasas = null;
        foreach (GameObject g in cena.GetRootGameObjects())
            if (g.name == "Casas") { raizCasas = g; break; }
        if (raizCasas == null) return 0;

        // Terrenos antigos: retângulo no mundo, ordem de desenho e para onde estão virados
        List<SpriteRenderer> antigas = new List<SpriteRenderer>();
        foreach (SpriteRenderer sr in raizCasas.GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.sprite != null && sr.sprite.name.StartsWith("Casa")) antigas.Add(sr);

        List<Rect> areas = new List<Rect>();
        foreach (SpriteRenderer sr in antigas) areas.Add(AreaNoMundo(sr));

        // Terrenos que se sobrepõem: o que fica por baixo perde o pedaço coberto
        for (int a = 0; a < antigas.Count; a++)
            for (int b = 0; b < antigas.Count; b++)
            {
                if (a == b || antigas[b].sortingOrder <= antigas[a].sortingOrder) continue;
                Rect ra = areas[a], rb = areas[b];
                float vertical = Mathf.Min(ra.yMax, rb.yMax) - Mathf.Max(ra.yMin, rb.yMin);
                if (vertical < ra.height * 0.5f || rb.xMax <= ra.xMin || rb.xMin >= ra.xMax) continue;
                if (rb.center.x < ra.center.x) ra.xMin = rb.xMax; else ra.xMax = rb.xMin;
                areas[a] = ra;
            }

        Transform pasta = new GameObject("CasasVizinhas").transform;
        pasta.SetParent(cenario, false);

        int feitas = 0;
        for (int i = 0; i < antigas.Count; i++)
        {
            Rect r = areas[i];
            if (r.width < 4f || r.height < 4f) continue;
            float giro = Mathf.Repeat(antigas[i].transform.eulerAngles.z, 360f);
            bool frenteEmBaixo = giro < 90f || giro > 270f;   // a arte antiga tinha o portão embaixo
            int largura = Mathf.RoundToInt(r.width * CidadeArte.PPU), altura = Mathf.RoundToInt(r.height * CidadeArte.PPU);
            int lotes = Mathf.Max(1, Mathf.RoundToInt(r.width / 12.5f));

            Sprite sprite = CidadeArte.Vizinhanca("CasaVizinha_" + (i + 1), largura, altura, lotes, frenteEmBaixo, i + 1);
            SpriteRenderer nova = SpriteFixo(pasta, "CasaVizinha " + (i + 1), sprite, r.center, antigas[i].sortingOrder);
            nova.sortingLayerID = antigas[i].sortingLayerID;

            antigas[i].gameObject.SetActive(false);
            EditorUtility.SetDirty(antigas[i].gameObject);
            feitas++;
        }
        return feitas;
    }

    // Retângulo (alinhado aos eixos) que o sprite ocupa no mundo, mesmo com o objeto desativado
    private static Rect AreaNoMundo(SpriteRenderer sr)
    {
        Bounds b = sr.sprite.bounds;
        Matrix4x4 m = sr.transform.localToWorldMatrix;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        for (int k = 0; k < 4; k++)
        {
            Vector3 canto = new Vector3(k % 2 == 0 ? b.min.x : b.max.x, k < 2 ? b.min.y : b.max.y, 0f);
            Vector2 w = m.MultiplyPoint3x4(canto);
            min = Vector2.Min(min, w);
            max = Vector2.Max(max, w);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static SpriteRenderer SpriteFixo(Transform pai, string nome, Sprite sprite, Vector2 posicao, int ordem)
    {
        SpriteRenderer sr = new GameObject(nome, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        sr.transform.SetParent(pai, false);
        sr.transform.position = posicao;
        sr.sprite = sprite;
        sr.sortingOrder = ordem;
        return sr;
    }

    // Retângulo da vaga de chegada no mundo (para pintar as faixas em volta dela)
    private static Rect AreaDaVaga(ChegadaNivel chegada)
    {
        BoxCollider2D caixa = chegada.GetComponent<BoxCollider2D>();
        Vector2 centro = chegada.transform.TransformPoint(caixa.offset);
        Vector3 escala = chegada.transform.lossyScale;
        Vector2 tamanho = new Vector2(Mathf.Abs(caixa.size.x * escala.x), Mathf.Abs(caixa.size.y * escala.y));
        return new Rect(centro - tamanho / 2f, tamanho);
    }
}
