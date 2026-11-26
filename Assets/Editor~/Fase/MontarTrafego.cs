using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

// Trânsito novo: descobre as ruas pelo Tilemap, desenha os modelos de carro em pixel art (vistos de cima)
// e cria o prefab do carro do trânsito com modelo, cor, luz de freio e seta sorteados.
// Menu: Mr Driver > Trânsito
public static class MontarTrafego
{
    private const string Pasta = "Assets/Imagens/CarrosTrafego";
    private const string CaminhoPrefab = "Assets/Prefabs/CarroTrafegoVariado.prefab";
    private const float PixelsPorUnidade = 20f;   // igual aos pedestres (1 pixel = 0,05)

    // =====================================================================
    // Comandos do menu
    // =====================================================================

    [MenuItem("Mr Driver/Trânsito/1. Gerar ruas automaticamente pelo mapa")]
    private static void GerarRuas()
    {
        Tilemap mapa = AcharMapaDeRuas();
        if (mapa == null)
        {
            EditorUtility.DisplayDialog("Trânsito",
                "Não achei nenhum Tilemap com ruas nesta cena.\n\n" +
                "As peças de rua são reconhecidas pelo nome (\"Rua...\", \"Faixa...\").\n" +
                "Sem Tilemap, use: Mr Driver > Trânsito > Criar rede manual aqui.", "OK");
            return;
        }

        RedeTrafego existente = Object.FindFirstObjectByType<RedeTrafego>();
        if (existente != null)
        {
            if (!EditorUtility.DisplayDialog("Trânsito",
                    "Esta cena já tem uma rede de ruas (\"" + existente.name + "\").\nApagar e gerar de novo?",
                    "Gerar de novo", "Cancelar")) return;
            Undo.DestroyObjectImmediate(existente.gameObject);
        }

        if (!DetectarRuas(mapa, out List<Vector2> pontos, out List<Vector2Int> ligacoes, out float largura))
        {
            EditorUtility.DisplayDialog("Trânsito",
                "Achei o Tilemap \"" + mapa.name + "\", mas não consegui reconhecer ruas retas nele.\n" +
                "Use: Mr Driver > Trânsito > Criar rede manual aqui.", "OK");
            return;
        }

        // Células -> mundo
        Vector3 origem = mapa.CellToWorld(Vector3Int.zero);
        Vector3 eixoX = mapa.CellToWorld(Vector3Int.right) - origem;
        Vector3 eixoY = mapa.CellToWorld(Vector3Int.up) - origem;
        float celula = eixoX.magnitude;

        GameObject redeObj = new GameObject("RedeTrafego");
        Undo.RegisterCreatedObjectUndo(redeObj, "Gerar ruas do trânsito");
        RedeTrafego rede = redeObj.AddComponent<RedeTrafego>();
        SerializedObject dadosRede = new SerializedObject(rede);
        dadosRede.FindProperty("faixa").floatValue = Arredondar(largura * celula / 4f);
        dadosRede.FindProperty("raioCruzamento").floatValue = Arredondar(largura * celula / 2f);
        dadosRede.ApplyModifiedPropertiesWithoutUndo();

        List<PontoTrafego> criados = new List<PontoTrafego>();
        for (int i = 0; i < pontos.Count; i++)
        {
            Vector3 mundo = origem + eixoX * (pontos[i].x + 0.5f) + eixoY * (pontos[i].y + 0.5f);
            mundo.z = 0f;
            criados.Add(CriarPonto(redeObj.transform, "Ponto " + (i + 1), mundo));
        }

        float comprimentoTotal = 0f;
        foreach (Vector2Int l in ligacoes)
        {
            Ligar(criados[l.x], criados[l.y]);
            comprimentoTotal += Vector3.Distance(criados[l.x].transform.position, criados[l.y].transform.position);
        }

        int quantidade = Mathf.Clamp(Mathf.RoundToInt(comprimentoTotal / 20f), 8, 40);
        ColocarGerador(redeObj, quantidade);

        int antigos = DesativarCarrosAntigos(true);

        EditorSceneManager.MarkSceneDirty(redeObj.scene);
        Selection.activeGameObject = redeObj;
        SceneView.lastActiveSceneView?.FrameSelected();

        EditorUtility.DisplayDialog("Trânsito",
            "Rede de ruas criada a partir do mapa \"" + mapa.name + "\"!\n\n" +
            "• " + pontos.Count + " pontos e " + ligacoes.Count + " ruas (mão dupla)\n" +
            "• " + quantidade + " carros vão nascer ao jogar (mude em GeradorTrafego > Quantidade)\n" +
            (antigos > 0 ? "• " + antigos + " carros antigos (rotas fixas) foram desativados\n" : "") +
            "\nNa aba Scene: setas ciano = faixas, amarelo = cruzamento, laranja = fim de rua (retorno).\n" +
            "Pode arrastar os pontos e ligar/desligar ruas pelo menu Mr Driver > Trânsito.\n\n" +
            "Salve a cena com Ctrl+S.", "OK");
    }

    [MenuItem("Mr Driver/Trânsito/Criar rede manual aqui")]
    private static void CriarRedeManual()
    {
        Vector3 centro = CentroDaVista();
        GameObject redeObj = new GameObject("RedeTrafego");
        Undo.RegisterCreatedObjectUndo(redeObj, "Criar rede do trânsito");
        redeObj.AddComponent<RedeTrafego>();

        PontoTrafego a = CriarPonto(redeObj.transform, "Ponto 1", centro + new Vector3(-12f, 0f, 0f));
        PontoTrafego b = CriarPonto(redeObj.transform, "Ponto 2", centro + new Vector3(12f, 0f, 0f));
        Ligar(a, b);
        ColocarGerador(redeObj, 4);

        EditorSceneManager.MarkSceneDirty(redeObj.scene);
        Selection.activeGameObject = redeObj;
        SceneView.lastActiveSceneView?.FrameSelected();

        EditorUtility.DisplayDialog("Trânsito",
            "Rede criada no centro da aba Scene!\n\n" +
            "• Coloque cada ponto no MEIO da rua (nos cruzamentos, curvas e fins de rua).\n" +
            "• Mais pontos: selecione um ponto e use \"Adicionar ponto ligado ao selecionado\".\n" +
            "• Para ligar dois pontos: selecione os dois e use \"Ligar ou desligar os 2 pontos selecionados\".\n\n" +
            "Depois salve a cena com Ctrl+S.", "OK");
    }

    [MenuItem("Mr Driver/Trânsito/Adicionar ponto ligado ao selecionado")]
    private static void AdicionarPonto()
    {
        PontoTrafego selecionado = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<PontoTrafego>()
            : null;
        RedeTrafego rede = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<RedeTrafego>()
            : null;
        if (selecionado == null || rede == null)
        {
            EditorUtility.DisplayDialog("Trânsito", "Selecione um ponto (PontoTrafego) de uma RedeTrafego na Hierarchy.", "OK");
            return;
        }

        // Continua na direção de onde a rua vinha
        Vector3 direcao = Vector3.right;
        foreach (PontoTrafego p in selecionado.ligacoes)
        {
            if (p == null) continue;
            Vector3 d = selecionado.transform.position - p.transform.position;
            if (d.sqrMagnitude > 0.01f) { direcao = d.normalized; break; }
        }

        int numero = rede.GetComponentsInChildren<PontoTrafego>().Length + 1;
        PontoTrafego novo = CriarPonto(rede.transform, "Ponto " + numero, selecionado.transform.position + direcao * 15f);
        Undo.RegisterCreatedObjectUndo(novo.gameObject, "Adicionar ponto");
        Undo.RecordObject(selecionado, "Adicionar ponto");
        Ligar(selecionado, novo);

        EditorSceneManager.MarkSceneDirty(rede.gameObject.scene);
        Selection.activeGameObject = novo.gameObject;
    }

    [MenuItem("Mr Driver/Trânsito/Ligar ou desligar os 2 pontos selecionados")]
    private static void LigarDesligar()
    {
        List<PontoTrafego> pontos = new List<PontoTrafego>();
        foreach (GameObject obj in Selection.gameObjects)
        {
            PontoTrafego p = obj.GetComponent<PontoTrafego>();
            if (p != null) pontos.Add(p);
        }

        if (pontos.Count != 2)
        {
            EditorUtility.DisplayDialog("Trânsito", "Selecione exatamente 2 pontos (segure Ctrl para selecionar o segundo).", "OK");
            return;
        }

        PontoTrafego a = pontos[0], b = pontos[1];
        Undo.RecordObjects(new Object[] { a, b }, "Ligar pontos");
        if (a.ligacoes.Contains(b) || b.ligacoes.Contains(a))
        {
            a.ligacoes.Remove(b);
            b.ligacoes.Remove(a);
        }
        else Ligar(a, b);

        EditorUtility.SetDirty(a);
        EditorUtility.SetDirty(b);
        EditorSceneManager.MarkSceneDirty(a.gameObject.scene);
    }

    [MenuItem("Mr Driver/Trânsito/Desativar carros antigos (rotas fixas)")]
    private static void DesativarAntigos()
    {
        int n = DesativarCarrosAntigos(false);
        if (n == 0) EditorUtility.DisplayDialog("Trânsito", "Nenhum carro antigo (com rota fixa) ativo nesta cena.", "OK");
    }

    [MenuItem("Mr Driver/Trânsito/Mostrar amostra dos modelos")]
    private static void Amostra()
    {
        AparenciaCarro prefab = PegarPrefab();
        SerializedProperty lista = new SerializedObject(prefab).FindProperty("modelos");
        int quantos = lista.arraySize;
        Vector3 centro = CentroDaVista();

        GameObject pasta = new GameObject("AmostraCarros (APAGUE depois de ver)");
        Undo.RegisterCreatedObjectUndo(pasta, "Amostra de carros");
        pasta.transform.position = centro;

        // Cada modelo duas vezes (cores diferentes)
        for (int i = 0; i < quantos * 2; i++)
        {
            GameObject carro = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject);
            carro.transform.SetParent(pasta.transform, false);
            carro.transform.localPosition = new Vector3((i % quantos - (quantos - 1) * 0.5f) * 3.4f, i < quantos ? 5.5f : -5.5f, 0f);
            carro.GetComponent<AparenciaCarro>().Sortear(i % quantos);
        }

        EditorSceneManager.MarkSceneDirty(pasta.scene);
        Selection.activeGameObject = pasta;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    [MenuItem("Mr Driver/Trânsito/Recriar prefab e desenhos dos carros")]
    private static void Recriar()
    {
        if (EditorUtility.DisplayDialog("Trânsito",
                "Recria o prefab CarroTrafegoVariado e os desenhos dos carros do zero (o que você mudou nele se perde).",
                "Recriar", "Cancelar"))
            CriarPrefab();
    }

    // =====================================================================
    // Ajudantes da cena
    // =====================================================================

    private static Vector3 CentroDaVista()
    {
        SceneView vista = SceneView.lastActiveSceneView;
        Vector3 centro = vista != null ? vista.pivot : Vector3.zero;
        centro.z = 0f;
        return centro;
    }

    private static float Arredondar(float v) => Mathf.Round(v * 100f) / 100f;

    private static PontoTrafego CriarPonto(Transform rede, string nome, Vector3 posicao)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(rede, false);
        obj.transform.position = posicao;
        return obj.AddComponent<PontoTrafego>();
    }

    private static void Ligar(PontoTrafego a, PontoTrafego b)
    {
        if (!a.ligacoes.Contains(b)) a.ligacoes.Add(b);
        if (!b.ligacoes.Contains(a)) b.ligacoes.Add(a);
        EditorUtility.SetDirty(a);
        EditorUtility.SetDirty(b);
    }

    private static void ColocarGerador(GameObject redeObj, int quantidade)
    {
        GeradorTrafego gerador = redeObj.AddComponent<GeradorTrafego>();
        SerializedObject dados = new SerializedObject(gerador);
        dados.FindProperty("prefab").objectReferenceValue = PegarPrefab().GetComponent<CarroTrafego>();
        dados.FindProperty("quantidade").intValue = quantidade;
        dados.ApplyModifiedPropertiesWithoutUndo();
    }

    // Carros com rota fixa (o trânsito antigo). Só desativa (dá para ligar de novo marcando o objeto).
    private static int DesativarCarrosAntigos(bool perguntar)
    {
        List<CarroTrafego> antigos = new List<CarroTrafego>();
        foreach (CarroTrafego c in Object.FindObjectsByType<CarroTrafego>(FindObjectsSortMode.None))
            if (c.Rota != null && c.Rede == null) antigos.Add(c);
        if (antigos.Count == 0) return 0;

        if (!EditorUtility.DisplayDialog("Trânsito",
                "Achei " + antigos.Count + " carros do trânsito antigo (andando em rotas fixas).\n\n" +
                "Desativar eles? (Recomendado: o trânsito novo já cobre as ruas. Eles não são apagados, só desligados.)",
                "Desativar", perguntar ? "Manter" : "Cancelar")) return 0;

        foreach (CarroTrafego c in antigos)
        {
            Undo.RecordObject(c.gameObject, "Desativar carros antigos");
            c.gameObject.SetActive(false);
        }
        EditorSceneManager.MarkSceneDirty(antigos[0].gameObject.scene);
        return antigos.Count;
    }

    // =====================================================================
    // Descobrir as ruas no Tilemap
    // =====================================================================

    private const int LarguraMinima = 6, LarguraMaxima = 16;   // em células

    private struct Segmento
    {
        public float centro;   // x (rua vertical) ou y (rua horizontal) do meio da rua
        public int a, b;       // onde começa e termina (no outro eixo)
        public float largura;
    }

    private static bool EhRua(TileBase peca)
    {
        if (peca == null) return false;
        string n = peca.name.ToLowerInvariant();
        if (n.Contains("cal") || n.Contains("grama") || n.Contains("deco")) return false;
        return n.Contains("rua") || n.Contains("faixa") || n.Contains("asfalto") || n.Contains("road");
    }

    private static Tilemap AcharMapaDeRuas()
    {
        Tilemap melhor = null;
        int maisRuas = 0;
        foreach (Tilemap mapa in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            int ruas = 0;
            foreach (TileBase peca in mapa.GetTilesBlock(mapa.cellBounds))
                if (EhRua(peca)) ruas++;
            if (ruas > maisRuas)
            {
                maisRuas = ruas;
                melhor = mapa;
            }
        }
        return melhor;
    }

    // Acha o meio das ruas retas (verticais e horizontais), os cruzamentos, as curvas e os fins de rua.
    // "pontos" saem em coordenadas de célula (com fração quando a rua tem largura par).
    private static bool DetectarRuas(Tilemap mapa, out List<Vector2> pontos, out List<Vector2Int> ligacoes, out float largura)
    {
        pontos = new List<Vector2>();
        ligacoes = new List<Vector2Int>();
        largura = 11f;

        BoundsInt limites = mapa.cellBounds;
        int w = limites.size.x, h = limites.size.y;
        if (w <= 0 || h <= 0) return false;

        TileBase[] pecas = mapa.GetTilesBlock(limites);
        bool[,] rua = new bool[w, h];
        for (int z = 0; z < limites.size.z; z++)
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (EhRua(pecas[x + y * w + z * w * h])) rua[x, y] = true;

        bool Rua(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && rua[x, y];

        // Meio de cada rua: em cada linha, sequências de rua com largura de rua (nem estreita, nem uma praça)
        Dictionary<Vector2Int, Vector2> meioVertical = new Dictionary<Vector2Int, Vector2>();
        Dictionary<Vector2Int, Vector2> meioHorizontal = new Dictionary<Vector2Int, Vector2>();
        List<float> larguras = new List<float>();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!Rua(x, y)) continue;
                int a = x;
                while (Rua(x + 1, y)) x++;
                int largo = x - a + 1;
                if (largo < LarguraMinima || largo > LarguraMaxima) continue;
                meioVertical[new Vector2Int((a + x) / 2, y)] = new Vector2((a + x) / 2f, largo);
                larguras.Add(largo);
            }
        }

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (!Rua(x, y)) continue;
                int a = y;
                while (Rua(x, y + 1)) y++;
                int largo = y - a + 1;
                if (largo < LarguraMinima || largo > LarguraMaxima) continue;
                meioHorizontal[new Vector2Int(x, (a + y) / 2)] = new Vector2((a + y) / 2f, largo);
                larguras.Add(largo);
            }
        }

        List<Segmento> verticais = Segmentos(meioVertical, true);
        List<Segmento> horizontais = Segmentos(meioHorizontal, false);
        if (verticais.Count + horizontais.Count == 0 || larguras.Count == 0) return false;

        larguras.Sort();
        largura = larguras[larguras.Count / 2];
        float alcance = largura * 0.75f;   // quanto um meio de rua pode "faltar" até o cruzamento

        List<Vector2> nos = new List<Vector2>();
        int AdicionarNo(Vector2 p)
        {
            for (int i = 0; i < nos.Count; i++)
                if (Mathf.Abs(nos[i].x - p.x) < 1f && Mathf.Abs(nos[i].y - p.y) < 1f) return i;
            nos.Add(p);
            return nos.Count - 1;
        }

        // Cruzamentos e curvas: onde o meio de uma rua vertical encontra o de uma horizontal
        foreach (Segmento v in verticais)
            foreach (Segmento hz in horizontais)
                if (Fora(hz.centro, v.a, v.b) <= alcance && Fora(v.centro, hz.a, hz.b) <= alcance &&
                    Rua(Mathf.RoundToInt(v.centro), Mathf.RoundToInt(hz.centro)))
                    AdicionarNo(new Vector2(v.centro, hz.centro));

        // Fins de rua (o carro faz o retorno)
        foreach (Segmento v in verticais)
            foreach (int e in new[] { v.a, v.b })
                if (!nos.Exists(n => Mathf.Abs(n.x - v.centro) < 1f && Mathf.Abs(n.y - e) <= alcance))
                    AdicionarNo(new Vector2(v.centro, e));
        foreach (Segmento hz in horizontais)
            foreach (int e in new[] { hz.a, hz.b })
                if (!nos.Exists(n => Mathf.Abs(n.y - hz.centro) < 1f && Mathf.Abs(n.x - e) <= alcance))
                    AdicionarNo(new Vector2(e, hz.centro));

        // Ruas: pontos vizinhos na mesma linha, com asfalto o caminho todo entre eles
        HashSet<Vector2Int> feitas = new HashSet<Vector2Int>();
        for (int eixo = 0; eixo < 2; eixo++)
        {
            bool vertical = eixo == 0;
            for (int i = 0; i < nos.Count; i++)
            {
                List<int> mesmaLinha = new List<int>();
                for (int j = 0; j < nos.Count; j++)
                    if (Mathf.Abs((vertical ? nos[j].x : nos[j].y) - (vertical ? nos[i].x : nos[i].y)) < 0.6f)
                        mesmaLinha.Add(j);
                mesmaLinha.Sort((p, q) => (vertical ? nos[p].y : nos[p].x).CompareTo(vertical ? nos[q].y : nos[q].x));

                for (int k = 0; k + 1 < mesmaLinha.Count; k++)
                {
                    int a = mesmaLinha[k], b = mesmaLinha[k + 1];
                    Vector2Int par = new Vector2Int(Mathf.Min(a, b), Mathf.Max(a, b));
                    if (feitas.Contains(par) || !AsfaltoEntre(nos[a], nos[b], Rua)) continue;
                    feitas.Add(par);
                }
            }
        }

        // Tira pontos soltos e passa para coordenadas do Tilemap
        int[] grau = new int[nos.Count];
        foreach (Vector2Int l in feitas) { grau[l.x]++; grau[l.y]++; }
        int[] novoIndice = new int[nos.Count];
        for (int i = 0; i < nos.Count; i++)
        {
            novoIndice[i] = -1;
            if (grau[i] == 0) continue;
            novoIndice[i] = pontos.Count;
            pontos.Add(nos[i] + new Vector2(limites.xMin, limites.yMin));
        }
        foreach (Vector2Int l in feitas) ligacoes.Add(new Vector2Int(novoIndice[l.x], novoIndice[l.y]));

        return ligacoes.Count > 0;
    }

    private static List<Segmento> Segmentos(Dictionary<Vector2Int, Vector2> celulas, bool vertical)
    {
        List<Segmento> lista = new List<Segmento>();
        HashSet<Vector2Int> vistas = new HashSet<Vector2Int>();
        List<Vector2Int> chaves = new List<Vector2Int>(celulas.Keys);
        chaves.Sort((p, q) => p.x != q.x ? p.x.CompareTo(q.x) : p.y.CompareTo(q.y));

        foreach (Vector2Int c in chaves)
        {
            if (vistas.Contains(c)) continue;
            vistas.Add(c);
            Vector2 dado = celulas[c];
            float soma = dado.y;
            int k = 1, fim = vertical ? c.y : c.x;

            while (true)
            {
                Vector2Int p = vertical ? new Vector2Int(c.x, c.y + k) : new Vector2Int(c.x + k, c.y);
                if (!celulas.TryGetValue(p, out Vector2 outro) || Mathf.Abs(outro.x - dado.x) >= 0.6f) break;
                vistas.Add(p);
                soma += outro.y;
                fim = vertical ? p.y : p.x;
                k++;
            }

            if (k >= 3)
                lista.Add(new Segmento { centro = dado.x, a = vertical ? c.y : c.x, b = fim, largura = soma / k });
        }
        return lista;
    }

    private static float Fora(float v, int a, int b) => Mathf.Max(a - v, 0f, v - b);

    private static bool AsfaltoEntre(Vector2 p, Vector2 q, System.Func<int, int, bool> rua)
    {
        int passos = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(p.x - q.x), Mathf.Abs(p.y - q.y)));
        for (int k = 0; k <= passos; k++)
        {
            Vector2 c = Vector2.Lerp(p, q, passos == 0 ? 0f : k / (float)passos);
            if (!rua(Mathf.RoundToInt(c.x), Mathf.RoundToInt(c.y))) return false;
        }
        return true;
    }

    // =====================================================================
    // Prefab do carro
    // =====================================================================

    private static AparenciaCarro PegarPrefab()
    {
        GameObject existente = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefab);
        if (existente == null || existente.GetComponent<AparenciaCarro>() == null) existente = CriarPrefab();
        return existente.GetComponent<AparenciaCarro>();
    }

    private enum Tipo { Comum, SUV, Picape, Van, Onibus, Esportivo, Taxi }

    private struct Spec
    {
        public string nome;
        public Tipo tipo;
        public int largura, comprimento;                        // lataria, em pixels
        public int capo, parabrisa, teto, vidroTras;            // tamanhos ao longo do carro, da frente para trás
        public int recuo;                                       // cabine mais estreita que a lataria (cada lado)
        public int raioFrente, raioTras;
        public float chance, fatorVelocidade;
        public Color32[] cores;                                 // vazio = cores comuns

        public Spec(string nome, Tipo tipo, int largura, int comprimento, int capo, int parabrisa, int teto,
                    int vidroTras, int recuo, int raioFrente, int raioTras, float chance, float fatorVelocidade,
                    params Color32[] cores)
        {
            this.nome = nome; this.tipo = tipo; this.largura = largura; this.comprimento = comprimento;
            this.capo = capo; this.parabrisa = parabrisa; this.teto = teto; this.vidroTras = vidroTras;
            this.recuo = recuo; this.raioFrente = raioFrente; this.raioTras = raioTras;
            this.chance = chance; this.fatorVelocidade = fatorVelocidade; this.cores = cores;
        }
    }

    private static Spec[] Modelos() => new[]
    {
        new Spec("Compacto", Tipo.Comum, 36, 60, 11, 9, 22, 8, 4, 7, 6, 0.8f, 1f),
        new Spec("Hatch", Tipo.Comum, 40, 72, 15, 10, 26, 10, 4, 7, 6, 1.2f, 1f),
        new Spec("Seda", Tipo.Comum, 42, 84, 20, 11, 24, 10, 4, 6, 6, 1.2f, 1f),
        new Spec("SUV", Tipo.SUV, 46, 88, 18, 10, 34, 9, 4, 6, 5, 0.9f, 0.97f),
        new Spec("Picape", Tipo.Picape, 46, 98, 20, 10, 18, 4, 4, 6, 4, 0.7f, 0.95f),
        new Spec("Van", Tipo.Van, 46, 98, 8, 9, 70, 6, 3, 8, 4, 0.5f, 0.9f),
        new Spec("Esportivo", Tipo.Esportivo, 44, 80, 28, 10, 12, 10, 6, 9, 7, 0.35f, 1.15f,
                 new Color32(220, 30, 40, 255), new Color32(255, 200, 30, 255), new Color32(255, 30, 205, 255),
                 new Color32(45, 226, 230, 255), new Color32(255, 120, 20, 255), new Color32(40, 40, 46, 255)),
        new Spec("Taxi", Tipo.Taxi, 42, 84, 20, 11, 24, 10, 4, 6, 6, 0.4f, 1.05f, new Color32(255, 196, 30, 255)),
        new Spec("Onibus", Tipo.Onibus, 54, 176, 2, 10, 156, 5, 1, 5, 4, 0.25f, 0.75f,
                 new Color32(245, 245, 245, 255), new Color32(255, 196, 30, 255), new Color32(210, 40, 40, 255),
                 new Color32(40, 110, 200, 255), new Color32(40, 150, 80, 255)),
    };

    // Os 3 desenhos de carro que já existiam no projeto também entram no sorteio (sem pintar)
    private static readonly string[] ImagensAntigas =
    {
        "Assets/Imagens/carros/carro1.png", "Assets/Imagens/carros/carro2.png", "Assets/Imagens/carros/picape_carro3.png",
    };

    private static GameObject CriarPrefab()
    {
        Spec[] specs = Modelos();

        // Luzes e sombra
        Dictionary<char, Color32> paletaLuz = new Dictionary<char, Color32>
        {
            { 'R', new Color32(255, 50, 60, 255) }, { 'r', new Color32(255, 40, 50, 110) },
            { 'A', new Color32(255, 180, 40, 255) }, { 'a', new Color32(255, 170, 30, 120) },
        };
        Sprite freio = PixelArt.Desenho(Pasta, "LuzFreio", new[] { ".rrrrr.", "rRRRRRr", "rRRRRRr", ".rrrrr." }, paletaLuz, PixelsPorUnidade);
        Sprite seta = PixelArt.Desenho(Pasta, "Seta", new[] { ".aaa.", "aAAAa", "aAAAa", "aAAAa", ".aaa." }, paletaLuz, PixelsPorUnidade);
        Sprite sombra = DesenharSombra();

        // Montagem
        GameObject raiz = new GameObject("CarroTrafegoVariado", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SortingGroup));
        int camada = LayerMask.NameToLayer("veiculos");
        if (camada >= 0) raiz.layer = camada;

        Rigidbody2D rb = raiz.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        raiz.GetComponent<BoxCollider2D>().size = new Vector2(2.1f, 4.2f);
        raiz.GetComponent<SortingGroup>().sortingOrder = 7;   // acima da pista e da linha do GPS (6), abaixo dos pedestres (8) e do jogador

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform, false);

        SpriteRenderer sombraSR = Parte(visual, "Sombra", sombra, 0, new Vector2(0.1f, -0.15f));
        SpriteRenderer lataria = Parte(visual, "Lataria", null, 1, Vector2.zero);
        SpriteRenderer detalhes = Parte(visual, "Detalhes", null, 2, Vector2.zero);
        SpriteRenderer freioE = Parte(visual, "FreioEsquerdo", freio, 3, Vector2.zero);
        SpriteRenderer freioD = Parte(visual, "FreioDireito", freio, 3, Vector2.zero);
        SpriteRenderer setaFE = Parte(visual, "SetaFrenteEsquerda", seta, 3, Vector2.zero);
        SpriteRenderer setaFD = Parte(visual, "SetaFrenteDireita", seta, 3, Vector2.zero);
        SpriteRenderer setaTE = Parte(visual, "SetaTrasEsquerda", seta, 3, Vector2.zero);
        SpriteRenderer setaTD = Parte(visual, "SetaTrasDireita", seta, 3, Vector2.zero);

        raiz.AddComponent<CarroTrafego>();
        AparenciaCarro aparencia = raiz.AddComponent<AparenciaCarro>();
        SerializedObject dados = new SerializedObject(aparencia);
        dados.FindProperty("lataria").objectReferenceValue = lataria;
        dados.FindProperty("detalhes").objectReferenceValue = detalhes;
        dados.FindProperty("sombra").objectReferenceValue = sombraSR;
        dados.FindProperty("freioEsquerdo").objectReferenceValue = freioE;
        dados.FindProperty("freioDireito").objectReferenceValue = freioD;
        dados.FindProperty("setaFrenteEsquerda").objectReferenceValue = setaFE;
        dados.FindProperty("setaFrenteDireita").objectReferenceValue = setaFD;
        dados.FindProperty("setaTrasEsquerda").objectReferenceValue = setaTE;
        dados.FindProperty("setaTrasDireita").objectReferenceValue = setaTD;
        dados.FindProperty("tamanhoSombra").vector2Value = new Vector2(2.6f, 4.6f);

        SerializedProperty lista = dados.FindProperty("modelos");
        lista.arraySize = 0;

        foreach (Spec s in specs)
        {
            DesenharCarro(s, out Sprite lat, out Sprite det);
            Vector2 tamanho = new Vector2(s.largura, s.comprimento) / PixelsPorUnidade;
            AdicionarModelo(lista, s.nome, lat, det, true, s.cores, s.chance, tamanho, s.fatorVelocidade);
        }

        foreach (string caminho in ImagensAntigas)
        {
            Sprite antiga = PrimeiraSprite(caminho);
            if (antiga == null) continue;
            string nome = System.IO.Path.GetFileNameWithoutExtension(caminho);
            AdicionarModelo(lista, nome, antiga, null, false, null, 0.5f, new Vector2(2.45f, 4.15f), 1f);
        }

        dados.ApplyModifiedPropertiesWithoutUndo();

        // Primeiro modelo já aparece no prefab
        lataria.sprite = specs.Length > 0 ? AssetDatabase.LoadAssetAtPath<Sprite>(Pasta + "/" + specs[2].nome + "_Lataria.png") : null;
        lataria.color = new Color32(200, 36, 44, 255);
        detalhes.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Pasta + "/" + specs[2].nome + "_Detalhes.png");
        foreach (SpriteRenderer luz in new[] { freioE, freioD, setaFE, setaFD, setaTE, setaTD }) luz.enabled = false;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    private static void AdicionarModelo(SerializedProperty lista, string nome, Sprite lataria, Sprite detalhes, bool pintavel,
                                        Color32[] cores, float chance, Vector2 tamanho, float fatorVelocidade)
    {
        int i = lista.arraySize;
        lista.arraySize = i + 1;
        SerializedProperty m = lista.GetArrayElementAtIndex(i);
        m.FindPropertyRelative("nome").stringValue = nome;
        m.FindPropertyRelative("lataria").objectReferenceValue = lataria;
        m.FindPropertyRelative("detalhes").objectReferenceValue = detalhes;
        m.FindPropertyRelative("pintavel").boolValue = pintavel;
        m.FindPropertyRelative("chance").floatValue = chance;
        m.FindPropertyRelative("tamanho").vector2Value = tamanho;
        m.FindPropertyRelative("fatorVelocidade").floatValue = fatorVelocidade;

        SerializedProperty listaCores = m.FindPropertyRelative("cores");
        int n = cores != null ? cores.Length : 0;
        listaCores.arraySize = n;
        for (int c = 0; c < n; c++) listaCores.GetArrayElementAtIndex(c).colorValue = cores[c];
    }

    private static Sprite PrimeiraSprite(string caminho)
    {
        foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(caminho))
            if (obj is Sprite s) return s;
        return null;
    }

    private static SpriteRenderer Parte(GameObject pai, string nome, Sprite sprite, int ordem, Vector2 posicao)
    {
        GameObject obj = new GameObject(nome, typeof(SpriteRenderer));
        obj.transform.SetParent(pai.transform, false);
        obj.transform.localPosition = posicao;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = ordem;
        return sr;
    }

    // =====================================================================
    // Desenho dos carros (frente para CIMA)
    // A lataria sai em tons de cinza (recebe a cor no jogo); vidros, faróis e pneus vão em "Detalhes".
    // =====================================================================

    private static readonly Color32 Contorno = PixelArt.Contorno;
    private static readonly Color32 Base = new Color32(232, 232, 236, 255);
    private static readonly Color32 Brilho = new Color32(255, 255, 255, 255);
    private static readonly Color32 Sombreado = new Color32(186, 186, 194, 255);
    private static readonly Color32 Vinco = new Color32(140, 140, 150, 255);
    private static readonly Color32 Vidro = new Color32(34, 40, 58, 255);
    private static readonly Color32 VidroBrilho = new Color32(92, 112, 150, 255);
    private static readonly Color32 Farol = new Color32(255, 246, 200, 255);
    private static readonly Color32 Lanterna = new Color32(150, 24, 36, 255);
    private static readonly Color32 Pneu = new Color32(26, 26, 30, 255);
    private static readonly Color32 Listra = new Color32(250, 250, 250, 255);
    private static readonly Color32 Placa = new Color32(255, 236, 120, 255);

    private sealed class Tela
    {
        public readonly int L, A;
        private readonly Color32[] lat, det;

        public Tela(int l, int a)
        {
            L = l;
            A = a;
            lat = PixelArt.NovaTela(l, a);
            det = PixelArt.NovaTela(l, a);
        }

        public void Lat(int x, int y, Color32 c) => PixelArt.Pintar(lat, L, A, x, y, c);
        public void Det(int x, int y, Color32 c) => PixelArt.Pintar(det, L, A, x, y, c);
        public bool TemLat(int x, int y) => x >= 0 && y >= 0 && x < L && y < A && lat[(A - 1 - y) * L + x].a > 0;
        public Sprite SalvarLat(string nome) => PixelArt.Salvar(Pasta, nome, lat, L, A, default, PixelsPorUnidade);
        public Sprite SalvarDet(string nome) => PixelArt.Salvar(Pasta, nome, det, L, A, default, PixelsPorUnidade);
    }

    // Retângulo de cantos arredondados (raios diferentes na frente e atrás)
    private static bool Dentro(int x, int y, int largura, int comprimento, int raioFrente, int raioTras)
    {
        if (x < 0 || y < 0 || x >= largura || y >= comprimento) return false;
        bool frente = y < comprimento / 2;
        int r = frente ? raioFrente : raioTras;
        if (r <= 0) return true;

        bool cantoY = frente ? y < r : y > comprimento - 1 - r;
        bool cantoX = x < r || x > largura - 1 - r;
        if (!cantoY || !cantoX) return true;

        float cx = x < r ? r : largura - 1 - r;
        float cy = frente ? r : comprimento - 1 - r;
        float dx = x - cx, dy = y - cy;
        return dx * dx + dy * dy <= r * r + r * 0.6f;
    }

    private static void DesenharCarro(Spec s, out Sprite lataria, out Sprite detalhes)
    {
        const int margem = 3;   // espaço dos lados para retrovisores e pneus
        int W = s.largura, C = s.comprimento;
        Tela t = new Tela(W + margem * 2, C + 2);
        int x0 = margem, y0 = 1;

        bool Corpo(int bx, int by) => Dentro(bx, by, W, C, s.raioFrente, s.raioTras);
        bool Interior(int bx, int by) => Corpo(bx, by) && Corpo(bx - 1, by) && Corpo(bx + 1, by) && Corpo(bx, by - 1) && Corpo(bx, by + 1);

        // ---------- Lataria ----------
        for (int by = 0; by < C; by++)
        {
            for (int bx = 0; bx < W; bx++)
            {
                if (!Corpo(bx, by)) continue;
                Color32 cor = !Interior(bx, by) ? Contorno
                            : bx <= 2 ? Brilho
                            : bx >= W - 3 || by >= C - 3 ? Sombreado
                            : Base;
                t.Lat(x0 + bx, y0 + by, cor);
            }
        }

        // Cabine (vidros + teto), mais estreita que a lataria
        int cx0 = s.recuo, cx1 = W - 1 - s.recuo;
        int cy0 = s.capo, cy1 = s.capo + s.parabrisa + s.teto + s.vidroTras - 1;
        int cabL = cx1 - cx0 + 1, cabC = cy1 - cy0 + 1;
        bool Cabine(int bx, int by) => Dentro(bx - cx0, by - cy0, cabL, cabC, 3, 3);
        bool CabineInterior(int bx, int by) =>
            Cabine(bx, by) && Cabine(bx - 1, by) && Cabine(bx + 1, by) && Cabine(bx, by - 1) && Cabine(bx, by + 1);

        for (int by = cy0; by <= cy1; by++)
            for (int bx = cx0; bx <= cx1; bx++)
                if (Cabine(bx, by))
                    t.Lat(x0 + bx, y0 + by, !CabineInterior(bx, by) ? Vinco : bx - cx0 <= 3 ? Brilho : Base);

        // Vincos do capô
        if (s.tipo != Tipo.Van && s.tipo != Tipo.Onibus)
        {
            for (int by = 4; by < s.capo - 3; by++)
            {
                if (Interior(W / 3, by)) t.Lat(x0 + W / 3, y0 + by, Sombreado);
                if (Interior(W - 1 - W / 3, by)) t.Lat(x0 + W - 1 - W / 3, y0 + by, Sombreado);
            }
        }

        // Retrovisores
        for (int my = 0; my < 4; my++)
        {
            for (int mx = 0; mx < 3; mx++)
            {
                bool borda = my == 0 || my == 3 || mx == 0;
                Color32 cor = borda ? Contorno : Base;
                int y = y0 + cy0 + 1 + my;
                t.Lat(x0 - 3 + mx, y, cor);
                t.Lat(x0 + W + 2 - mx, y, cor);
            }
        }

        // Extras de cada tipo
        int tetoInicio = cy0 + s.parabrisa, tetoFim = cy1 - s.vidroTras;
        switch (s.tipo)
        {
            case Tipo.SUV:   // barras no teto
                for (int by = tetoInicio + 2; by <= tetoFim - 2; by++)
                {
                    t.Lat(x0 + cx0 + 4, y0 + by, Vinco);
                    t.Lat(x0 + cx1 - 4, y0 + by, Vinco);
                }
                break;

            case Tipo.Picape:   // caçamba com frisos
                for (int by = cy1 + 3; by <= C - 4; by++)
                {
                    for (int bx = 3; bx <= W - 4; bx++)
                    {
                        bool borda = by == cy1 + 3 || by == C - 4 || bx == 3 || bx == W - 4;
                        t.Lat(x0 + bx, y0 + by, borda ? Vinco : (bx - 3) % 4 == 2 ? Vinco : Sombreado);
                    }
                }
                break;

            case Tipo.Van:   // porta traseira dupla
                for (int by = cy1 + 1; by <= C - 3; by++) t.Lat(x0 + W / 2, y0 + by, Vinco);
                break;

            case Tipo.Onibus:   // ar-condicionado e alçapão no teto
                int arY0 = C * 45 / 100, arY1 = C * 60 / 100;
                for (int by = arY0; by <= arY1; by++)
                {
                    for (int bx = W / 4; bx <= W - 1 - W / 4; bx++)
                    {
                        bool borda = by == arY0 || by == arY1 || bx == W / 4 || bx == W - 1 - W / 4;
                        t.Lat(x0 + bx, y0 + by, borda ? Vinco : (by - arY0) % 3 == 0 ? Vinco : Sombreado);
                    }
                }
                for (int by = tetoInicio + 8; by <= tetoInicio + 15; by++)
                    for (int bx = W / 2 - 4; bx <= W / 2 + 3; bx++)
                        t.Lat(x0 + bx, y0 + by, by == tetoInicio + 8 || by == tetoInicio + 15 || bx == W / 2 - 4 || bx == W / 2 + 3 ? Vinco : Sombreado);
                break;

            case Tipo.Esportivo:   // aerofólio
                for (int bx = 2; bx <= W - 3; bx++)
                {
                    t.Lat(x0 + bx, y0 + C - 7, Vinco);
                    t.Lat(x0 + bx, y0 + C - 6, Vinco);
                }
                break;
        }

        // ---------- Detalhes (não recebem cor) ----------

        // Pneus aparecendo dos lados
        int rodaTamanho = Mathf.Max(8, C / 9);
        int[] rodas = s.tipo == Tipo.Onibus
            ? new[] { C * 12 / 100, C * 72 / 100 }
            : new[] { C * 15 / 100, C * 76 / 100 - rodaTamanho / 2 };
        foreach (int r in rodas)
        {
            for (int by = r; by < r + rodaTamanho; by++)
            {
                if (!t.TemLat(x0 - 1, y0 + by)) t.Det(x0 - 1, y0 + by, Pneu);
                if (!t.TemLat(x0 + W, y0 + by)) t.Det(x0 + W, y0 + by, Pneu);
            }
        }

        // Listras do esportivo (por baixo dos vidros)
        if (s.tipo == Tipo.Esportivo)
        {
            for (int by = 0; by < C; by++)
            {
                for (int bx = W / 2 - 5; bx <= W / 2 + 4; bx++)
                {
                    bool listra = bx <= W / 2 - 3 || bx >= W / 2 + 2;
                    if (listra && Interior(bx, by)) t.Det(x0 + bx, y0 + by, Listra);
                }
            }
        }

        // Para-brisa (mais estreito perto do teto)
        for (int r = 1; r < s.parabrisa; r++)
        {
            int by = cy0 + r, recuo = 1 + r / 3;
            for (int bx = cx0 + recuo; bx <= cx1 - recuo; bx++)
                if (CabineInterior(bx, by)) t.Det(x0 + bx, y0 + by, (bx + by) % 9 == 0 ? VidroBrilho : Vidro);
        }

        // Vidro traseiro (mais estreito perto do teto)
        for (int r = 1; r < s.vidroTras; r++)
        {
            int by = cy1 - r, recuo = 1 + r / 3;
            for (int bx = cx0 + recuo; bx <= cx1 - recuo; bx++)
                if (CabineInterior(bx, by)) t.Det(x0 + bx, y0 + by, (bx + by) % 9 == 0 ? VidroBrilho : Vidro);
        }

        // Janelas dos lados, com colunas
        int coluna = s.tipo == Tipo.Onibus ? 14 : Mathf.Max(6, s.teto / 2);
        for (int by = tetoInicio; by <= tetoFim; by++)
        {
            int k = by - tetoInicio;
            if (k > 1 && k % coluna < 2) continue;
            for (int lado = 1; lado <= 2; lado++)
            {
                if (CabineInterior(cx0 + lado, by)) t.Det(x0 + cx0 + lado, y0 + by, Vidro);
                if (CabineInterior(cx1 - lado, by)) t.Det(x0 + cx1 - lado, y0 + by, Vidro);
            }
        }

        // Faróis e lanternas
        for (int by = 1; by <= 2; by++)
        {
            for (int bx = 2; bx <= 7; bx++)
            {
                if (Interior(bx, by)) t.Det(x0 + bx, y0 + by, Farol);
                if (Interior(W - 1 - bx, by)) t.Det(x0 + W - 1 - bx, y0 + by, Farol);
            }
        }
        for (int by = C - 3; by <= C - 2; by++)
        {
            for (int bx = 2; bx <= 6; bx++)
            {
                if (Interior(bx, by)) t.Det(x0 + bx, y0 + by, Lanterna);
                if (Interior(W - 1 - bx, by)) t.Det(x0 + W - 1 - bx, y0 + by, Lanterna);
            }
        }

        // Placa luminosa do táxi
        if (s.tipo == Tipo.Taxi)
        {
            int meio = (tetoInicio + tetoFim) / 2;
            for (int by = meio - 2; by <= meio + 1; by++)
                for (int bx = W / 2 - 5; bx <= W / 2 + 4; bx++)
                {
                    bool borda = by == meio - 2 || by == meio + 1 || bx == W / 2 - 5 || bx == W / 2 + 4;
                    t.Det(x0 + bx, y0 + by, borda ? Contorno : Placa);
                }
        }

        lataria = t.SalvarLat(s.nome + "_Lataria");
        detalhes = t.SalvarDet(s.nome + "_Detalhes");
    }

    // Sombra de cantos arredondados, transparente (é esticada para o tamanho de cada modelo)
    private static Sprite DesenharSombra()
    {
        const int largura = 52, altura = 92;
        Color32[] p = PixelArt.NovaTela(largura, altura);
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                if (Dentro(x, y, largura, altura, 10, 10))
                    PixelArt.Pintar(p, largura, altura, x, y, new Color32(0, 0, 0, 80));
        return PixelArt.Salvar(Pasta, "Sombra", p, largura, altura, default, PixelsPorUnidade);
    }
}
