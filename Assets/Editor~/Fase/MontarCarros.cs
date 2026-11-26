using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Carros da garagem do Jason. Menu: Mr Driver > História > 6. Carros da garagem
// 1) Separa o desenho original do carro (o Nissan Kicks do pai) em lataria pintável + detalhes.
// 2) Usa os modelos do trânsito (Mr Driver > Trânsito) como outros carros para escolher.
// 3) Faz versões pequenas de cada um para o carro parado na garagem da casa.
// 4) Salva o catálogo (Assets/Historia/CatalogoCarros.asset) e instala o VisualCarroJogador no prefab CarroNissan_0.
public static class MontarCarros
{
    public const string CaminhoCatalogo = "Assets/Historia/CatalogoCarros.asset";
    private const string CaminhoCarro = "Assets/Prefabs/CarroNissan_0.prefab";
    private const string CaminhoTrafego = "Assets/Prefabs/CarroTrafegoVariado.prefab";
    private const string Pasta = "Assets/Imagens/Historia/Garagem";

    // Nome e descrição de cada modelo do trânsito (pelo nome do modelo na ferramenta do trânsito)
    private static readonly Dictionary<string, string[]> Apelidos = new Dictionary<string, string[]>
    {
        { "Compacto", new[] { "Pipoca", "Pequeno, econômico e estaciona em qualquer cantinho. Até na vaga da Dona Cida." } },
        { "Hatch", new[] { "Faísca", "Esperto e animado. Perfeito para o dia a dia (e para ir ao mercado)." } },
        { "Seda", new[] { "Executivo", "Confortável e elegante. Ar de quem está atrasado para uma reunião importante." } },
        { "SUV", new[] { "Trovão", "Grandão e valente. Passa por cima de qualquer buraco. (Por favor, não tente.)" } },
        { "Picape", new[] { "Pé de Boi", "Caçamba para levar as compras do mês inteiro. E a Gertrudes." } },
        { "Van", new[] { "Perua da Família", "Cabe a família toda, a vizinhança e o cachorro da vizinhança." } },
        { "Esportivo", new[] { "Relâmpago", "Faz VRUUUM mais alto. Pelo menos na sua imaginação." } },
        { "Taxi", new[] { "Táxi Júnior", "Igualzinho ao táxi do pai nos velhos tempos. Bandeira 1 liberada!" } },
    };

    private static readonly (string nome, Color32 cor)[] Cores =
    {
        ("Vermelho Original", new Color32(220, 36, 44, 255)),   // substituída pela cor de verdade do desenho
        ("Laranja Jason", new Color32(255, 128, 26, 255)),
        ("Amarelo Táxi", new Color32(255, 196, 30, 255)),
        ("Verde Betão", new Color32(52, 158, 90, 255)),
        ("Azul Gervásio", new Color32(46, 97, 217, 255)),
        ("Rosa Dona Cida", new Color32(237, 77, 148, 255)),
        ("Roxo Uva", new Color32(130, 60, 190, 255)),
        ("Ciano Neon", new Color32(45, 216, 226, 255)),
        ("Branco Neve", new Color32(245, 245, 248, 255)),
        ("Prata", new Color32(192, 196, 204, 255)),
        ("Grafite", new Color32(78, 82, 92, 255)),
        ("Preto Noite", new Color32(40, 40, 48, 255)),
    };

    [MenuItem("Mr Driver/História/6. Carros da garagem (catálogo e carro do jogador)", priority = 25)]
    private static void Menu()
    {
        string resumo = Montar();
        if (resumo != null) EditorUtility.DisplayDialog("Carros da garagem", resumo, "OK");
    }

    public static CatalogoCarros Catalogo() => AssetDatabase.LoadAssetAtPath<CatalogoCarros>(CaminhoCatalogo);

    // Devolve um resumo (ou null se não deu certo)
    public static string Montar()
    {
        GameObject prefabCarro = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoCarro);
        SpriteRenderer corpo = prefabCarro != null ? prefabCarro.GetComponent<SpriteRenderer>() : null;
        if (corpo == null || corpo.sprite == null)
        {
            EditorUtility.DisplayDialog("Carros da garagem", "Não encontrei o desenho do carro em " + CaminhoCarro + ".", "OK");
            return null;
        }

        List<CatalogoCarros.Modelo> modelos = new List<CatalogoCarros.Modelo>();
        string resumo = "";

        // ---------- O Nissan Kicks (desenho original, separado em duas camadas) ----------
        Sprite original = corpo.sprite;
        Color32[] pixels = LerPixels(original, out int w, out int h);
        Vector2 pivo = new Vector2(original.pivot.x / original.rect.width, original.pivot.y / original.rect.height);
        Separar(pixels, out Color32[] lat, out Color32[] det, out Color corOriginal);
        CatalogoCarros.Modelo kicks = new CatalogoCarros.Modelo
        {
            id = GaragemJogador.ModeloPadrao,
            nome = "Nissan Kicks",
            descricao = "O orgulho do Betão! Câmera 360, alerta de ponto cego, sensor de estacionamento e frenagem de emergência.",
            lataria = PixelArt.Salvar(Pasta, "Kicks_Lataria", lat, w, h, default, original.pixelsPerUnit, pivo),
            detalhes = PixelArt.Salvar(Pasta, "Kicks_Detalhes", det, w, h, default, original.pixelsPerUnit, pivo),
            mostrarRodas = true,
        };
        Miniaturas(kicks, lat, det, w, h);
        modelos.Add(kicks);
        resumo += "• Nissan Kicks (o carro do pai, agora pintável)\n";

        // ---------- Os modelos do trânsito ----------
        int doTrafego = 0;
        GameObject trafego = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoTrafego);
        AparenciaCarro aparencia = trafego != null ? trafego.GetComponent<AparenciaCarro>() : null;
        if (aparencia != null)
        {
            SerializedProperty lista = new SerializedObject(aparencia).FindProperty("modelos");
            for (int i = 0; i < lista.arraySize; i++)
            {
                SerializedProperty m = lista.GetArrayElementAtIndex(i);
                string nome = m.FindPropertyRelative("nome").stringValue;
                Sprite sLat = m.FindPropertyRelative("lataria").objectReferenceValue as Sprite;
                Sprite sDet = m.FindPropertyRelative("detalhes").objectReferenceValue as Sprite;
                if (!m.FindPropertyRelative("pintavel").boolValue || sLat == null || !Apelidos.ContainsKey(nome)) continue;

                CatalogoCarros.Modelo modelo = new CatalogoCarros.Modelo
                {
                    id = nome,
                    nome = Apelidos[nome][0],
                    descricao = Apelidos[nome][1],
                    lataria = sLat,
                    detalhes = sDet,
                };
                Color32[] pl = LerPixels(sLat, out int lw, out int lh);
                Color32[] pd = sDet != null ? LerPixels(sDet, out _, out _) : PixelArt.NovaTela(lw, lh);
                Miniaturas(modelo, pl, pd, lw, lh);
                modelos.Add(modelo);
                doTrafego++;
            }
        }
        resumo += doTrafego > 0
            ? "• Mais " + doTrafego + " carros dos amigos do táxi (modelos do trânsito)\n"
            : "• Não achei os modelos do trânsito: rode Mr Driver > Trânsito e depois este passo de novo para ter mais carros\n";

        // ---------- Catálogo ----------
        HistoriaArte.CriarPasta("Assets/Historia");
        CatalogoCarros catalogo = Catalogo();
        if (catalogo == null)
        {
            catalogo = ScriptableObject.CreateInstance<CatalogoCarros>();
            AssetDatabase.CreateAsset(catalogo, CaminhoCatalogo);
        }
        catalogo.modelos = modelos;
        catalogo.cores = new List<CatalogoCarros.Cor>();
        for (int i = 0; i < Cores.Length; i++)
            catalogo.cores.Add(new CatalogoCarros.Cor { nome = Cores[i].nome, cor = i == 0 ? corOriginal : (Color)Cores[i].cor });
        EditorUtility.SetDirty(catalogo);
        AssetDatabase.SaveAssets();
        resumo += "• " + catalogo.cores.Count + " cores de tinta\n";

        resumo += InstalarNoCarro(catalogo);
        return "Catálogo salvo em " + CaminhoCatalogo + ":\n" + resumo;
    }

    // ---------- Carro do jogador ----------

    private static string InstalarNoCarro(CatalogoCarros catalogo)
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(CaminhoCarro);
        try
        {
            SpriteRenderer corpo = raiz.GetComponent<SpriteRenderer>();
            Transform tLat = raiz.transform.Find("VisualLataria");
            Transform tDet = raiz.transform.Find("VisualDetalhes");
            bool novo = tLat == null || tDet == null;

            if (novo)
            {
                // Os detalhes ficam logo acima da lataria: as luzes sobem uma camada
                foreach (SpriteRenderer sr in raiz.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr != corpo && sr.sortingOrder > corpo.sortingOrder) sr.sortingOrder++;
                if (tLat == null) tLat = Parte(raiz.transform, "VisualLataria");
                if (tDet == null) tDet = Parte(raiz.transform, "VisualDetalhes");
            }

            SpriteRenderer lat = tLat.GetComponent<SpriteRenderer>();
            SpriteRenderer det = tDet.GetComponent<SpriteRenderer>();
            foreach (SpriteRenderer sr in new[] { lat, det })
            {
                sr.sortingLayerID = corpo.sortingLayerID;
                sr.sharedMaterial = corpo.sharedMaterial;
            }
            lat.sortingOrder = corpo.sortingOrder;
            det.sortingOrder = corpo.sortingOrder + 1;

            List<SpriteRenderer> rodas = new List<SpriteRenderer>();
            foreach (SpriteRenderer sr in raiz.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.name.StartsWith("Pneu")) rodas.Add(sr);

            VisualCarroJogador visual = raiz.GetComponent<VisualCarroJogador>();
            if (visual == null) visual = raiz.AddComponent<VisualCarroJogador>();
            SerializedObject dados = new SerializedObject(visual);
            dados.FindProperty("catalogo").objectReferenceValue = catalogo;
            dados.FindProperty("corpoOriginal").objectReferenceValue = corpo;
            dados.FindProperty("lataria").objectReferenceValue = lat;
            dados.FindProperty("detalhes").objectReferenceValue = det;
            SerializedProperty lista = dados.FindProperty("rodas");
            lista.arraySize = rodas.Count;
            for (int i = 0; i < rodas.Count; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = rodas[i];
            dados.FindProperty("tamanhoBase").vector2Value = corpo.sprite.bounds.size;
            dados.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoCarro);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
        return "• O carro do jogador (CarroNissan_0) agora usa o carro e a cor escolhidos na garagem, em todas as cenas\n";
    }

    private static Transform Parte(Transform pai, string nome)
    {
        GameObject obj = new GameObject(nome, typeof(SpriteRenderer));
        obj.transform.SetParent(pai, false);
        obj.layer = pai.gameObject.layer;
        return obj.transform;
    }

    // ---------- Pixels ----------

    // Lê os pixels de um sprite direto do arquivo (não precisa marcar "Read/Write" na textura). Linhas de baixo para cima.
    public static Color32[] LerPixels(Sprite sprite, out int w, out int h)
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
        Rect r = sprite.rect;
        // Se a textura foi reduzida na importação, ajusta o recorte
        float k = tex.width / (float)sprite.texture.width;
        int rx = Mathf.RoundToInt(r.x * k), ry = Mathf.RoundToInt(r.y * k);
        w = Mathf.RoundToInt(r.width * k);
        h = Mathf.RoundToInt(r.height * k);
        Color32[] todos = tex.GetPixels32();
        Color32[] p = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                p[y * w + x] = todos[(ry + y) * tex.width + rx + x];
        Object.DestroyImmediate(tex);
        return p;
    }

    // Separa o carro vermelho: a parte vermelha vira cinza (recebe a cor), o resto fica nos detalhes
    private static void Separar(Color32[] p, out Color32[] lataria, out Color32[] detalhes, out Color corOriginal)
    {
        lataria = new Color32[p.Length];
        detalhes = new Color32[p.Length];
        List<int> vermelhos = new List<int>();
        List<float> brilhos = new List<float>();

        for (int i = 0; i < p.Length; i++)
            if (EhVermelho(p[i])) { vermelhos.Add(i); brilhos.Add(p[i].r); }

        brilhos.Sort();
        float topo = brilhos.Count > 0 ? Mathf.Max(1f, brilhos[Mathf.Clamp((int)(brilhos.Count * 0.95f), 0, brilhos.Count - 1)]) : 255f;

        Vector3 soma = Vector3.zero;
        HashSet<int> conjunto = new HashSet<int>(vermelhos);
        for (int i = 0; i < p.Length; i++)
        {
            Color32 c = p[i];
            if (conjunto.Contains(i))
            {
                byte v = (byte)Mathf.Clamp(c.r / topo * 255f, 0f, 255f);
                lataria[i] = new Color32(v, v, v, c.a);
                detalhes[i] = new Color32(0, 0, 0, 0);
                soma += new Vector3(1f, c.g / (float)Mathf.Max(1, c.r), c.b / (float)Mathf.Max(1, c.r));
            }
            else
            {
                lataria[i] = new Color32(0, 0, 0, 0);
                detalhes[i] = c;
            }
        }

        if (vermelhos.Count == 0) corOriginal = new Color32(220, 36, 44, 255);
        else
        {
            Vector3 media = soma / vermelhos.Count * (topo / 255f);
            corOriginal = new Color(media.x, media.y, media.z, 1f);
        }
    }

    private static bool EhVermelho(Color32 c) => c.a > 20 && c.r >= 60 && c.r > c.g * 1.6f && c.r > c.b * 1.5f;

    // Versões pequenas (cabem em 30 x 58 pixels da casa = um pouco menos que 2 x 4 quadradinhos)
    private static void Miniaturas(CatalogoCarros.Modelo m, Color32[] lat, Color32[] det, int w, int h)
    {
        float k = Mathf.Min(30f / w, 58f / h);
        int tw = Mathf.Max(1, Mathf.RoundToInt(w * k)), th = Mathf.Max(1, Mathf.RoundToInt(h * k));
        m.latariaCasa = PixelArt.Salvar(Pasta, m.id + "_LatariaCasa", Reduzir(lat, w, h, tw, th), tw, th, default,
                                        HistoriaArte.Px, new Vector2(0.5f, 0.5f));
        m.detalhesCasa = PixelArt.Salvar(Pasta, m.id + "_DetalhesCasa", Reduzir(det, w, h, tw, th), tw, th, default,
                                         HistoriaArte.Px, new Vector2(0.5f, 0.5f));
    }

    private static Color32[] Reduzir(Color32[] p, int w, int h, int tw, int th)
    {
        Color32[] r = new Color32[tw * th];
        for (int ty = 0; ty < th; ty++)
            for (int tx = 0; tx < tw; tx++)
            {
                int x0 = tx * w / tw, x1 = Mathf.Max(x0 + 1, (tx + 1) * w / tw);
                int y0 = ty * h / th, y1 = Mathf.Max(y0 + 1, (ty + 1) * h / th);
                float sr = 0, sg = 0, sb = 0, sa = 0;
                int n = 0;
                for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        Color32 c = p[y * w + x];
                        float a = c.a / 255f;
                        sr += c.r * a; sg += c.g * a; sb += c.b * a; sa += a;
                        n++;
                    }
                if (n == 0 || sa / n < 0.4f) { r[ty * tw + tx] = new Color32(0, 0, 0, 0); continue; }
                r[ty * tw + tx] = new Color32((byte)(sr / sa), (byte)(sg / sa), (byte)(sb / sa), 255);
            }
        return r;
    }
}
