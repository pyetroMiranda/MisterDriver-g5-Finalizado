using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Passo 3 da história: o pátio da Autoescola Freio de Mão (o "segundo tutorial").
// Cria a cena Historia_Autoescola do zero: o desenho do pátio, as colisões (cerca, prédio, ilha, calçada),
// os cones que caem, o carro, a câmera, o HUD e as 5 aulas do Gervásio.
// As medidas do pátio ficam em AutoescolaArte.
public static partial class MontarHistoria
{
    private const string CaminhoAutoescola = "Assets/Scenes/" + ProgressoHistoria.CenaAutoescola + ".unity";

    [MenuItem("Mr Driver/História/3. Criar cena da autoescola", priority = 22)]
    private static void MenuAutoescola()
    {
        if (File.Exists(CaminhoAutoescola) && !EditorUtility.DisplayDialog("História",
                "A cena Historia_Autoescola já existe. Recriar do zero? (o que você mudou nela se perde)", "Recriar", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string resumo = CriarAutoescola();
        if (resumo != null) EditorUtility.DisplayDialog("História", resumo + "\nAperte Play para testar!", "OK");
    }

    // Devolve um resumo (ou null se não deu certo)
    private static string CriarAutoescola()
    {
        GameObject prefabCarro = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CarroNissan_0.prefab");
        GameObject prefabCamera = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Main Camera.prefab");
        if (prefabCarro == null)
        {
            EditorUtility.DisplayDialog("História", "Não encontrei Assets/Prefabs/CarroNissan_0.prefab.", "OK");
            return null;
        }

        CarregarPersonagens();
        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------- Chão ----------
        SpriteRenderer patio = new GameObject("Patio da autoescola", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        patio.sprite = AutoescolaArte.DesenharPatio();
        patio.sortingOrder = -20;

        // ---------- Colisões (bater conta falta) ----------
        Transform colisoes = new GameObject("Colisoes").transform;
        Rect a = AutoescolaArte.Asfalto;
        Parede(colisoes, "Cerca esquerda", Rect.MinMaxRect(a.xMin - 6, a.yMin - 6, a.xMin, a.yMax + 6));
        Parede(colisoes, "Cerca direita", Rect.MinMaxRect(a.xMax, a.yMin - 6, a.xMax + 6, a.yMax + 6));
        Parede(colisoes, "Cerca de baixo", Rect.MinMaxRect(a.xMin, a.yMin - 6, a.xMax, a.yMin));
        Parede(colisoes, "Cerca de cima", Rect.MinMaxRect(a.xMin, a.yMax, a.xMax, a.yMax + 6));
        Parede(colisoes, "Predio da autoescola", AutoescolaArte.Predio);
        Parede(colisoes, "Calcada da baliza", AutoescolaArte.CalcadaBaliza);
        IlhaDoCircuito(colisoes);

        // ---------- Cones ----------
        Sprite cone = AutoescolaArte.DesenharCone();
        Transform cones = new GameObject("Cones").transform;
        int quantosCones = 0;

        Rect s = AutoescolaArte.Slalom;
        for (float y = s.yMin + 6; y <= s.yMax - 6; y += 8)
        {
            Cone(cones, cone, new Vector2(s.xMin + 0.7f, y)); Cone(cones, cone, new Vector2(s.xMax - 0.7f, y));
            quantosCones += 2;
        }
        for (int i = 0; i < AutoescolaArte.SlalomCones.Length; i++) { Cone(cones, cone, AutoescolaArte.ConeSlalom(i)); quantosCones++; }

        Rect g = AutoescolaArte.Garagem;
        for (float y = g.yMin + 0.6f; y <= g.yMax + 0.1f; y += 2f)
        {
            Cone(cones, cone, new Vector2(g.xMin - 0.5f, y)); Cone(cones, cone, new Vector2(g.xMax + 0.5f, y));
            quantosCones += 2;
        }
        for (float x = g.xMin + 1f; x <= g.xMax - 0.9f; x += 2f) { Cone(cones, cone, new Vector2(x, g.yMax + 0.5f)); quantosCones++; }

        Rect b = AutoescolaArte.Baliza;
        foreach (Vector2 p in new[] { new Vector2(b.xMin - 0.6f, b.yMin + 0.4f), new Vector2(b.xMin - 0.6f, b.yMax - 0.4f),
                                      new Vector2(b.xMax + 0.6f, b.yMin + 0.4f), new Vector2(b.xMax + 0.6f, b.yMax - 0.4f) })
        {
            Cone(cones, cone, p);
            quantosCones++;
        }

        Rect r = AutoescolaArte.Reta;
        Cone(cones, cone, new Vector2(r.xMin - 1.2f, AutoescolaArte.PareY)); Cone(cones, cone, new Vector2(r.xMax + 1.2f, AutoescolaArte.PareY));
        quantosCones += 2;

        // ---------- Carro e câmera ----------
        GameObject carroObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabCarro);
        carroObj.transform.position = AutoescolaArte.Inicio;
        carroObj.transform.rotation = Quaternion.Euler(0f, 0f, 180f);   // virado para baixo, de frente para a reta
        CarroController carro = carroObj.GetComponent<CarroController>();

        if (prefabCamera != null)
        {
            GameObject cam = (GameObject)PrefabUtility.InstantiatePrefab(prefabCamera);
            cam.transform.position = new Vector3(AutoescolaArte.Inicio.x, AutoescolaArte.Inicio.y, -10f);
            cam.transform.rotation = carroObj.transform.rotation;
            CameraSegue segue = cam.GetComponent<CameraSegue>();
            if (segue != null) segue.alvo = carroObj.transform;
        }
        else
        {
            GameObject cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraSegue));
            cam.tag = "MainCamera";
            Camera c = cam.GetComponent<Camera>();
            c.orthographic = true;
            c.orthographicSize = 6f;
            cam.transform.position = new Vector3(AutoescolaArte.Inicio.x, AutoescolaArte.Inicio.y, -10f);
            cam.GetComponent<CameraSegue>().alvo = carroObj.transform;
        }

        foreach (string hud in new[] { "HUDVelocimetro", "HUDPausa" })
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + hud + ".prefab");
            if (prefab != null) PrefabUtility.InstantiatePrefab(prefab);
        }

        // ---------- Aulas ----------
        Sprite area = HistoriaArte.AreaPonto();
        Sprite pare = HistoriaArte.PlacaPare();
        Sprite vaga = HistoriaArte.PlacaVaga();
        Transform pasta = new GameObject("Aulas").transform;
        Vector2 baixo = Vector2.down, cima = Vector2.up, direita = Vector2.right, esquerda = Vector2.left;
        Rect ci = AutoescolaArte.Circuito;
        float meio = AutoescolaArte.LarguraPista * 0.5f;

        PontoExame Passar(string nome, Vector2 pos, Vector2 dir, Vector2 tam) =>
            CriarPonto(pasta, nome, pos, AnguloDe(dir), tam, PontoExame.Tipo.Passar, area, null, Vector2.zero);

        PontoExame Parar(string nome, Vector2 pos, Vector2 dir, Vector2 tam, PontoExame.Orientacao o, Sprite placa, Vector2 posPlaca)
        {
            PontoExame p = CriarPonto(pasta, nome, pos, AnguloDe(dir), tam, PontoExame.Tipo.Parar, area, placa, posPlaca);
            p.Definir(PontoExame.Tipo.Parar, tam, o);
            return p;
        }

        PontoExame retaEntrada = Passar("Aula 1 - Entrada da reta", new Vector2(r.center.x, 58f), baixo, new Vector2(r.width, 4f));
        PontoExame retaPare = Parar("Aula 1 - PARE", new Vector2(r.center.x, AutoescolaArte.PareY + 2.6f), baixo,
                                    new Vector2(r.width - 1f, 5.2f), PontoExame.Orientacao.Qualquer, pare, new Vector2(r.width * 0.5f + 2.5f, 0f));

        PontoExame c1 = Passar("Aula 2 - Curva 1", new Vector2(ci.center.x, ci.yMin + meio), direita, new Vector2(12f, 4f));
        PontoExame c2 = Passar("Aula 2 - Curva 2", new Vector2(ci.xMax - meio, ci.center.y), cima, new Vector2(12f, 4f));
        PontoExame c3 = Passar("Aula 2 - Curva 3", new Vector2(ci.center.x, ci.yMax - meio), esquerda, new Vector2(12f, 4f));
        PontoExame c4 = Passar("Aula 2 - Curva 4", new Vector2(ci.xMin + meio, ci.center.y), baixo, new Vector2(12f, 4f));
        PontoExame c5 = Passar("Aula 2 - Saida", new Vector2(ci.xMax - 16f, ci.yMin + meio), direita, new Vector2(12f, 4f));

        PontoExame s1 = Passar("Aula 3 - Largada", new Vector2(s.center.x, s.yMin + 4f), cima, new Vector2(s.width, 4f));
        PontoExame s2 = Passar("Aula 3 - Chegada", new Vector2(s.center.x, s.yMax - 4f), cima, new Vector2(s.width, 4f));

        PontoExame garagem = Parar("Aula 4 - Garagem de re", g.center, cima, new Vector2(g.width - 0.4f, g.height - 0.6f),
                                   PontoExame.Orientacao.Contrario, null, Vector2.zero);
        PontoExame baliza = Parar("Aula 5 - Baliza", b.center, direita, new Vector2(b.height, b.width),
                                  PontoExame.Orientacao.Alinhado, null, Vector2.zero);
        Rect v = AutoescolaArte.VagaRect(AutoescolaArte.VagaFinal);
        PontoExame vagaFinal = Parar("Vaga final", v.center, cima, new Vector2(v.width - 0.4f, v.height - 0.4f),
                                     PontoExame.Orientacao.Alinhado, vaga, new Vector2(0f, v.height * 0.5f + 2.2f));

        // ---------- Seta, interface e missão ----------
        GameObject setaObj = new GameObject("SetaObjetivo");
        SpriteRenderer srSeta = new GameObject("Desenho", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        srSeta.transform.SetParent(setaObj.transform, false);
        srSeta.sprite = HistoriaArte.DesenharSetaObjetivo();
        srSeta.sortingOrder = 60;
        setaObj.transform.localScale = Vector3.one * 1.4f;
        setaObj.transform.position = carroObj.transform.position;
        SetaObjetivo seta = setaObj.AddComponent<SetaObjetivo>();
        Ligar(seta, "carro", carroObj.transform);
        Ligar(seta, "desenho", srSeta);

        CriarEventSystem();
        CriarCanvasHistoria(false, out CaixaDialogo dialogo, out ObjetivoHUD objetivo, out _);

        MissaoExame missao = new GameObject("AulasAutoescola").AddComponent<MissaoExame>();
        Ligar(missao, "carro", carro);
        Ligar(missao, "carteira", carroObj.GetComponent<CarteiraMotorista>());
        Ligar(missao, "dialogo", dialogo);
        Ligar(missao, "objetivo", objetivo);
        Ligar(missao, "seta", seta);
        Ligar(missao, "jason", HistoriaTextos.Jason);
        Ligar(missao, "instrutor", HistoriaTextos.Instrutor);

        List<MissaoExame.Etapa> etapas = new List<MissaoExame.Etapa>
        {
            Etapa(HistoriaTextos.ObjetivoAula1, retaEntrada, HistoriaTextos.Aula1Entrada()),
            Etapa(HistoriaTextos.ObjetivoAula1Pare, retaPare, HistoriaTextos.Aula1Pare()),
            Etapa(HistoriaTextos.ObjetivoAula2 + " (1/5)", c1, null),
            Etapa(HistoriaTextos.ObjetivoAula2 + " (2/5)", c2, HistoriaTextos.Aula2Meio()),
            Etapa(HistoriaTextos.ObjetivoAula2 + " (3/5)", c3, null),
            Etapa(HistoriaTextos.ObjetivoAula2 + " (4/5)", c4, null),
            Etapa(HistoriaTextos.ObjetivoAula2 + " (5/5)", c5, HistoriaTextos.Aula2Fim()),
            Etapa(HistoriaTextos.ObjetivoAula3, s1, null),
            Etapa(HistoriaTextos.ObjetivoAula3Fim, s2, HistoriaTextos.Aula3()),
            Etapa(HistoriaTextos.ObjetivoAula4, garagem, HistoriaTextos.Aula4()),
            Etapa(HistoriaTextos.ObjetivoAula5, baliza, HistoriaTextos.Aula5()),
            Etapa(HistoriaTextos.ObjetivoVagaAutoescola, vagaFinal, null),
        };

        missao.DefinirRoteiro(HistoriaTextos.AberturaAutoescola(), etapas, HistoriaTextos.ReacoesMuro(),
                              HistoriaTextos.ReprovadoAutoescola(), HistoriaTextos.AulasPerfeitas(),
                              HistoriaTextos.AulasConcluidas(), HistoriaTextos.FimAutoescola());
        missao.DefinirFinal(ProgressoHistoria.CenaAutoescola, ProgressoHistoria.CenaExame, false, 10, HistoriaTextos.ReacoesCone());
        EditorUtility.SetDirty(missao);

        EditorSceneManager.SaveScene(cena, CaminhoAutoescola);
        ColocarNoBuild(CaminhoAutoescola, CaminhoCasa);
        if (File.Exists(CaminhoExame)) ColocarNoBuild(CaminhoExame, CaminhoAutoescola);

        return "Cena Historia_Autoescola criada:\n• 5 aulas (reta, curvas, slalom, garagem de ré e baliza) + vaga final\n" +
               "• " + quantosCones + " cones que caem e voltam para o lugar\n";
    }

    private static MissaoExame.Etapa Etapa(string objetivo, PontoExame ponto, List<Fala> aoChegar) =>
        new MissaoExame.Etapa { objetivo = objetivo, ponto = ponto, falasAoChegar = aoChegar ?? new List<Fala>() };

    private static void Parede(Transform pai, string nome, Rect r)
    {
        GameObject obj = new GameObject(nome, typeof(BoxCollider2D));
        obj.transform.SetParent(pai, false);
        obj.transform.position = r.center;
        obj.GetComponent<BoxCollider2D>().size = r.size;
        obj.AddComponent<ObstaculoAutoescola>();
    }

    // A ilha do circuito tem cantos arredondados: colisão em polígono
    private static void IlhaDoCircuito(Transform pai)
    {
        Rect ilha = AutoescolaArte.Ilha;
        float raio = AutoescolaArte.RaioIlha;
        List<Vector2> pontos = new List<Vector2>();
        Vector2[] centros =
        {
            new Vector2(ilha.xMax - raio, ilha.yMin + raio), new Vector2(ilha.xMax - raio, ilha.yMax - raio),
            new Vector2(ilha.xMin + raio, ilha.yMax - raio), new Vector2(ilha.xMin + raio, ilha.yMin + raio),
        };
        for (int c = 0; c < 4; c++)
            for (int i = 0; i <= 6; i++)
            {
                float ang = (-90f + c * 90f + i * 15f) * Mathf.Deg2Rad;
                pontos.Add(centros[c] + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio - ilha.center);
            }

        GameObject obj = new GameObject("Ilha do circuito", typeof(PolygonCollider2D));
        obj.transform.SetParent(pai, false);
        obj.transform.position = ilha.center;
        obj.GetComponent<PolygonCollider2D>().points = pontos.ToArray();
        obj.AddComponent<ObstaculoAutoescola>();
    }

    private static void Cone(Transform pai, Sprite sprite, Vector2 posicao)
    {
        GameObject obj = new GameObject("Cone", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D));
        obj.transform.SetParent(pai, false);
        obj.transform.position = posicao;

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 9;   // acima das marcações (6), abaixo do carro (10)

        Rigidbody2D corpo = obj.GetComponent<Rigidbody2D>();
        corpo.gravityScale = 0f;
        corpo.mass = 0.15f;
        corpo.linearDamping = 3f;
        corpo.angularDamping = 3f;

        obj.GetComponent<CircleCollider2D>().radius = 0.45f;
        obj.AddComponent<ConeAutoescola>();
    }
}
