using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Desenha as pessoas em pixel art (vistas de cima), cria o prefab do pedestre e as rotas nas calçadas.
// Menu: Mr Driver > Pedestres
public static class MontarPedestres
{
    private const string Pasta = "Assets/Imagens/Pedestres";
    private const string CaminhoPrefab = "Assets/Prefabs/Pedestre.prefab";
    private const float PixelsPorUnidade = 20f;   // pessoa com ~0,7 de largura (o carro tem ~2,4)

    // Cinzas: cada parte recebe a cor (pele, camisa...) no jogo
    private static readonly Dictionary<char, Color32> Cinzas = new Dictionary<char, Color32>
    {
        { 'k', PixelArt.Contorno },
        { 'W', new Color32(235, 235, 235, 255) },
        { 'H', new Color32(255, 255, 255, 255) },
        { 'S', new Color32(170, 170, 178, 255) },
    };

    // ---------- Comandos do menu ----------

    [MenuItem("Mr Driver/Pedestres/Criar rota de pedestres aqui")]
    private static void CriarRota()
    {
        Pedestre prefab = PegarPrefab();
        Vector3 centro = CentroDaVista();

        GameObject rotaObj = new GameObject("RotaPedestres");
        Undo.RegisterCreatedObjectUndo(rotaObj, "Criar rota de pedestres");
        rotaObj.transform.position = centro;
        rotaObj.AddComponent<RotaPedestres>();

        // Exemplo: anda na calçada, atravessa a rua (amarelo) e continua na outra calçada
        CriarPonto(rotaObj.transform, "Ponto 1", new Vector2(-6f, 2f), false);
        CriarPonto(rotaObj.transform, "Ponto 2", new Vector2(0f, 2f), true);
        CriarPonto(rotaObj.transform, "Ponto 3", new Vector2(0f, -2f), false);
        CriarPonto(rotaObj.transform, "Ponto 4", new Vector2(6f, -2f), false);

        GeradorPedestres gerador = rotaObj.AddComponent<GeradorPedestres>();
        SerializedObject dados = new SerializedObject(gerador);
        dados.FindProperty("prefab").objectReferenceValue = prefab;
        dados.FindProperty("quantidade").intValue = 4;
        dados.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(rotaObj.scene);
        Selection.activeGameObject = rotaObj;
        SceneView.lastActiveSceneView?.FrameSelected();

        EditorUtility.DisplayDialog("Pedestres",
            "Rota criada no centro da aba Scene!\n\n" +
            "• Linha CIANO = calçada\n• Linha AMARELA = travessia da rua\n\n" +
            "Arraste os pontos (Ponto 1, 2, 3...) para as calçadas.\n" +
            "Para mais pontos: Mr Driver > Pedestres > Adicionar ponto na rota selecionada.\n\n" +
            "Depois salve a cena com Ctrl+S.", "OK");
    }

    [MenuItem("Mr Driver/Pedestres/Adicionar ponto na rota selecionada")]
    private static void AdicionarPonto()
    {
        RotaPedestres rota = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<RotaPedestres>()
            : null;
        if (rota == null)
        {
            EditorUtility.DisplayDialog("Pedestres", "Selecione uma RotaPedestres (ou um ponto dela) na Hierarchy.", "OK");
            return;
        }

        List<Transform> pontos = new List<Transform>();
        foreach (Transform filho in rota.transform)
            if (filho.GetComponent<PontoPedestre>() != null) pontos.Add(filho);

        Vector3 posicao = rota.transform.position;
        if (pontos.Count >= 2)
        {
            Vector3 ultimo = pontos[pontos.Count - 1].position;
            Vector3 penultimo = pontos[pontos.Count - 2].position;
            Vector3 direcao = (ultimo - penultimo).sqrMagnitude > 0.01f ? (ultimo - penultimo).normalized : Vector3.right;
            posicao = ultimo + direcao * 4f;
        }
        else if (pontos.Count == 1) posicao = pontos[0].position + Vector3.right * 4f;

        GameObject novo = CriarPonto(rota.transform, "Ponto " + (pontos.Count + 1),
                                     rota.transform.InverseTransformPoint(posicao), false);
        Undo.RegisterCreatedObjectUndo(novo, "Adicionar ponto");
        EditorSceneManager.MarkSceneDirty(rota.gameObject.scene);
        Selection.activeGameObject = novo;
    }

    [MenuItem("Mr Driver/Pedestres/Mostrar amostra de visuais (12 pessoas)")]
    private static void Amostra()
    {
        Pedestre prefab = PegarPrefab();
        Vector3 centro = CentroDaVista();

        GameObject pasta = new GameObject("AmostraPedestres (APAGUE depois de ver)");
        Undo.RegisterCreatedObjectUndo(pasta, "Amostra de pedestres");
        pasta.transform.position = centro;

        for (int i = 0; i < 12; i++)
        {
            GameObject pessoa = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject);
            pessoa.transform.SetParent(pasta.transform, false);
            pessoa.transform.localPosition = new Vector3((i % 6 - 2.5f) * 1.3f, (i / 6 - 0.5f) * -1.6f, 0f);
            pessoa.GetComponent<AparenciaPedestre>().Sortear();
        }

        EditorSceneManager.MarkSceneDirty(pasta.scene);
        Selection.activeGameObject = pasta;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    [MenuItem("Mr Driver/Pedestres/Recriar prefab do pedestre")]
    private static void Recriar()
    {
        if (EditorUtility.DisplayDialog("Pedestres",
                "Recria o prefab Pedestre do zero (o que você mudou nele se perde).", "Recriar", "Cancelar"))
            CriarPrefab();
    }

    // ---------- Ajudantes ----------

    private static Pedestre PegarPrefab()
    {
        GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
        if (existente == null) existente = CriarPrefab();
        return existente.GetComponent<Pedestre>();
    }

    private static Vector3 CentroDaVista()
    {
        SceneView vista = SceneView.lastActiveSceneView;
        Vector3 centro = vista != null ? vista.pivot : Vector3.zero;
        centro.z = 0f;
        return centro;
    }

    private static GameObject CriarPonto(Transform rota, string nome, Vector2 local, bool travessia)
    {
        GameObject ponto = new GameObject(nome);
        ponto.transform.SetParent(rota, false);
        ponto.transform.localPosition = local;
        ponto.AddComponent<PontoPedestre>().atravessarAteOProximo = travessia;
        return ponto;
    }

    // ---------- Prefab ----------

    private static GameObject CriarPrefab()
    {
        // Imagens (todas vistas de cima, com a frente da pessoa para CIMA)
        Sprite corpo = Desenho("Corpo", new[]
        {
            "...kkkkkkkk...",
            ".kkWWWWWWWWkk.",
            "kWWHHWWWWWWWWk",
            "kWHWWWWWWWWWWk",
            "kWWWWWWWWWWWWk",
            "kWWWWWWWWWWWSk",
            "kSWWWWWWWWWWSk",
            ".kSSSSSSSSSSk.",
            "..kkkkkkkkkk..",
        });

        string[] vazio = { "..........", "..........", "..........", "..........", ".........." };
        Sprite cabeca = Desenho("Cabeca", Juntar(new[]
        {
            "..........",
            "...kkkk...",
            "..kWWWWk..",
            ".kWHHWWWk.",
            ".kWHWWWWk.",
            ".kWWWWWWk.",
            ".kWWWWWSk.",
            "..kSSSSk..",
            "...kkkk...",
        }, vazio));

        string[] topoCurto =
        {
            "..........",
            "..........",
            "..........",
            ".kkkkkkkk.",
            "kWHHWWWWWk",
            "kWHWWWWWWk",
            "kWWWWWWWSk",
            ".kWWWWWSk.",
        };

        Sprite curto = Desenho("CabeloCurto", Juntar(topoCurto, new[]
        {
            "..kkkkkk..", "..........", "..........", "..........", "..........", "..........",
        }));
        Sprite longo = Desenho("CabeloLongo", new[]
        {
            "..........",
            "..........",
            "..........",
            ".kkkkkkkk.",
            "kWHHWWWWWk",
            "kWHWWWWWWk",
            "kWWWWWWWWk",
            "kWWWWWWWSk",
            "kWWWWWWWSk",
            "kSWWWWWWSk",
            ".kSWWWWSk.",
            "..kSSSSk..",
            "...kkkk...",
            "..........",
        });
        Sprite rabo = Desenho("CabeloRaboDeCavalo", Juntar(topoCurto, new[]
        {
            "..kkWWkk..", "...kWSk...", "...kWSk...", "...kSSk...", "....kk....", "..........",
        }));
        Sprite coque = Desenho("CabeloCoque", Juntar(topoCurto, new[]
        {
            "..kkWWkk..", "..kWHWSk..", "..kWWSSk..", "...kkkk...", "..........", "..........",
        }));
        Sprite bone = Desenho("Bone", Juntar(new[]
        {
            "...kkkk...",
            "..kWWWWk..",
            "..kSSSSk..",
            ".kkkkkkkk.",
            "kWHHWWWWWk",
            "kWHWWHWWWk",
            "kWWWWWWWSk",
            ".kWWWWWSk.",
            "..kkkkkk..",
        }, vazio));

        Sprite braco = Desenho("Braco", new[] { ".kk.", "kWWk", "kWWk", "kWSk", "kSSk", ".kk." });
        Sprite pe = Desenho("Pe", new[] { ".kk.", "kWWk", "kWSk", "kSSk", ".kk." });
        Sprite mochila = Desenho("Mochila", new[]
        {
            "kkkkkkkk",
            "kWHWWWWk",
            "kWWWWWWk",
            "kWkkkkWk",
            "kSWWWWSk",
            "kkkkkkkk",
        });
        Sprite sombra = DesenharSombra();

        // Montagem
        GameObject raiz = new GameObject("Pedestre", typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SortingGroup));
        Rigidbody2D rb = raiz.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.gravityScale = 0f;
        CircleCollider2D colisor = raiz.GetComponent<CircleCollider2D>();
        colisor.isTrigger = true;
        colisor.radius = 0.35f;
        raiz.GetComponent<SortingGroup>().sortingOrder = 8;   // acima da pista e da vaga, abaixo do carro (10)

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform, false);

        Color pele = new Color32(224, 172, 105, 255);
        Color camisa = new Color32(255, 30, 205, 255);
        Color cabeloCor = new Color32(62, 40, 26, 255);
        Color sapato = new Color32(30, 30, 35, 255);

        Parte(visual, "Sombra", sombra, 0, new Vector2(0.03f, -0.05f), Color.white);
        SpriteRenderer peE = Parte(visual, "PeEsquerdo", pe, 1, new Vector2(-0.15f, 0f), sapato);
        SpriteRenderer peD = Parte(visual, "PeDireito", pe, 1, new Vector2(0.15f, 0f), sapato);
        SpriteRenderer bracoE = Parte(visual, "BracoEsquerdo", braco, 2, new Vector2(-0.38f, 0f), pele);
        SpriteRenderer bracoD = Parte(visual, "BracoDireito", braco, 2, new Vector2(0.38f, 0f), pele);
        SpriteRenderer corpoSR = Parte(visual, "Corpo", corpo, 3, Vector2.zero, camisa);
        SpriteRenderer mochilaSR = Parte(visual, "Mochila", mochila, 4, new Vector2(0f, -0.17f), new Color32(45, 226, 230, 255));
        SpriteRenderer cabecaSR = Parte(visual, "Cabeca", cabeca, 5, new Vector2(0f, -0.07f), pele);
        SpriteRenderer cabeloSR = Parte(visual, "Cabelo", curto, 6, new Vector2(0f, -0.07f), cabeloCor);
        mochilaSR.enabled = false;

        Pedestre pedestre = raiz.AddComponent<Pedestre>();
        SerializedObject dadosPedestre = new SerializedObject(pedestre);
        dadosPedestre.FindProperty("visual").objectReferenceValue = visual.transform;
        dadosPedestre.FindProperty("peEsquerdo").objectReferenceValue = peE.transform;
        dadosPedestre.FindProperty("peDireito").objectReferenceValue = peD.transform;
        dadosPedestre.FindProperty("bracoEsquerdo").objectReferenceValue = bracoE.transform;
        dadosPedestre.FindProperty("bracoDireito").objectReferenceValue = bracoD.transform;
        dadosPedestre.FindProperty("amplitudePe").floatValue = 0.17f;
        dadosPedestre.ApplyModifiedPropertiesWithoutUndo();

        AparenciaPedestre aparencia = raiz.AddComponent<AparenciaPedestre>();
        SerializedObject dadosAparencia = new SerializedObject(aparencia);
        dadosAparencia.FindProperty("corpo").objectReferenceValue = corpoSR;
        dadosAparencia.FindProperty("cabeca").objectReferenceValue = cabecaSR;
        dadosAparencia.FindProperty("cabelo").objectReferenceValue = cabeloSR;
        dadosAparencia.FindProperty("mochila").objectReferenceValue = mochilaSR;
        dadosAparencia.FindProperty("bracoEsquerdo").objectReferenceValue = bracoE;
        dadosAparencia.FindProperty("bracoDireito").objectReferenceValue = bracoD;
        dadosAparencia.FindProperty("peEsquerdo").objectReferenceValue = peE;
        dadosAparencia.FindProperty("peDireito").objectReferenceValue = peD;
        dadosAparencia.FindProperty("bone").objectReferenceValue = bone;

        // Curto aparece mais vezes; o item vazio = careca
        Sprite[] estilos = { curto, curto, longo, rabo, coque, null };
        SerializedProperty lista = dadosAparencia.FindProperty("estilosCabelo");
        lista.arraySize = estilos.Length;
        for (int i = 0; i < estilos.Length; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = estilos[i];
        dadosAparencia.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    private static SpriteRenderer Parte(GameObject pai, string nome, Sprite sprite, int ordem, Vector2 posicao, Color cor)
    {
        GameObject obj = new GameObject(nome, typeof(SpriteRenderer));
        obj.transform.SetParent(pai.transform, false);
        obj.transform.localPosition = posicao;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = ordem;
        sr.color = cor;
        return sr;
    }

    private static Sprite Desenho(string nome, string[] linhas)
    {
        return PixelArt.Desenho(Pasta, nome, linhas, Cinzas, PixelsPorUnidade);
    }

    private static string[] Juntar(string[] a, string[] b)
    {
        string[] r = new string[a.Length + b.Length];
        a.CopyTo(r, 0);
        b.CopyTo(r, a.Length);
        return r;
    }

    // Sombra oval e transparente embaixo da pessoa
    private static Sprite DesenharSombra()
    {
        const int largura = 14, altura = 8;
        Color32[] p = PixelArt.NovaTela(largura, altura);
        float cx = (largura - 1) / 2f, cy = (altura - 1) / 2f;

        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                float dx = (x - cx) / (largura / 2f), dy = (y - cy) / (altura / 2f);
                if (dx * dx + dy * dy <= 1f) PixelArt.Pintar(p, largura, altura, x, y, new Color32(0, 0, 0, 90));
            }

        return PixelArt.Salvar(Pasta, "Sombra", p, largura, altura, default, PixelsPorUnidade);
    }
}
