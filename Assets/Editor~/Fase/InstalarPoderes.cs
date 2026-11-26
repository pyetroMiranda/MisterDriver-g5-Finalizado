using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Cria os itens da loja dos poderes/melhorias novos e instala os componentes no carro (prefab CarroNissan_0).
// Menu: Mr Driver > Poderes > Instalar poderes e melhorias
public static class InstalarPoderes
{
    private const string PastaItens = "Assets/Loja/Itens";
    private const string PrefabCarro = "Assets/Prefabs/CarroNissan_0.prefab";

    [MenuItem("Mr Driver/Poderes/Instalar poderes e melhorias (loja + carro)")]
    private static void Instalar()
    {
        // 1) Itens da loja
        List<string> criados = new List<string>();

        ItemLoja advogado = Item("Advogado", "Advogado", ItemLoja.Categoria.Poder, 250, 1, 0,
            "Passivo: em cada nível, anula os primeiros 3 pontos de dano na carteira.", criados);
        ItemLoja abs = Item("FreioABS", "Freios ABS", ItemLoja.Categoria.Poder, 180, 1, 0,
            "Aperte B para parar o carro na hora, sem derrapar. Recarrega em 8 segundos.", criados);
        ItemLoja suspensao = Item("SuspensaoReforcada", "Suspensão Reforçada", ItemLoja.Categoria.Poder, 160, 1, 0,
            "Aperte J: por 8 segundos os buracos não freiam o carro nem tiram pontos. Recarrega em 25 s.", criados);
        ItemLoja lenta = Item("CameraLenta", "Câmera Lenta", ItemLoja.Categoria.Poder, 350, 1, 0,
            "Aperte K: o jogo inteiro fica mais devagar por 4 segundos. Recarrega em 30 s.", criados);
        ItemLoja gps = Item("GPS", "GPS", ItemLoja.Categoria.Poder, 120, 1, 0,
            "Aperte G para ligar/desligar: mostra na pista a rota mais curta até a chegada.", criados);

        ItemLoja curso = Item("CursoDirecao", "Curso de Direção", ItemLoja.Categoria.Melhoria, 150, 5, 100,
            "+1 ponto na carteira por nível.", criados);
        ItemLoja motor = Item("Motor", "Motor", ItemLoja.Categoria.Melhoria, 200, 5, 150,
            "+8% de velocidade máxima por nível.", criados);
        ItemLoja aceleracao = Item("Aceleracao", "Aceleração", ItemLoja.Categoria.Melhoria, 150, 5, 100,
            "+15% de aceleração por nível.", criados);
        ItemLoja freios = Item("Freios", "Freios", ItemLoja.Categoria.Melhoria, 120, 3, 80,
            "+25% de força no freio por nível.", criados);
        ItemLoja pneus = Item("Pneus", "Pneus Esportivos", ItemLoja.Categoria.Melhoria, 150, 3, 100,
            "+8% de curva por nível.", criados);
        ItemLoja nitroLongo = Item("NitroLongo", "Nitro Mais Longo", ItemLoja.Categoria.Melhoria, 200, 3, 150,
            "+1 segundo de nitro por nível.", criados);
        ItemLoja nitroRecarga = Item("NitroRecarga", "Recarga do Nitro", ItemLoja.Categoria.Melhoria, 200, 3, 150,
            "O nitro recarrega 5 segundos mais rápido por nível.", criados);

        AssetDatabase.SaveAssets();

        // 2) Componentes no carro
        GameObject raiz = PrefabUtility.LoadPrefabContents(PrefabCarro);
        List<string> instalados = new List<string>();
        try
        {
            Componente<Advogado>(raiz, instalados, d => d.FindProperty("itemLoja").objectReferenceValue = advogado);
            Poder<FreioABS>(raiz, instalados, abs, 0f, 8f, "B");
            Poder<SuspensaoReforcada>(raiz, instalados, suspensao, 8f, 25f, "J");
            Poder<CameraLenta>(raiz, instalados, lenta, 4f, 30f, "K");
            Componente<GPS>(raiz, instalados, d => d.FindProperty("itemLoja").objectReferenceValue = gps);
            Componente<MelhoriasCarro>(raiz, instalados, d =>
            {
                d.FindProperty("motor").objectReferenceValue = motor;
                d.FindProperty("aceleracao").objectReferenceValue = aceleracao;
                d.FindProperty("freios").objectReferenceValue = freios;
                d.FindProperty("pneus").objectReferenceValue = pneus;
                d.FindProperty("nitroLongo").objectReferenceValue = nitroLongo;
                d.FindProperty("nitroRecarga").objectReferenceValue = nitroRecarga;
                d.FindProperty("cursoDirecao").objectReferenceValue = curso;
            });

            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabCarro);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        EditorUtility.DisplayDialog("Poderes",
            "Pronto!\n\n" +
            "Itens novos na loja (Assets/Loja/Itens): " + (criados.Count > 0 ? string.Join(", ", criados) : "já existiam") +
            "\n\nInstalado no carro: " + (instalados.Count > 0 ? string.Join(", ", instalados) : "já estava tudo") +
            "\n\nPróximo passo: abra a cena Menu e use\nMr Driver > Loja > Atualizar itens da loja no Menu.", "OK");
    }

    [MenuItem("Mr Driver/Loja/Atualizar itens da loja no Menu")]
    private static void AtualizarLoja()
    {
        LojaUI loja = Object.FindFirstObjectByType<LojaUI>(FindObjectsInactive.Include);
        if (loja == null)
        {
            EditorUtility.DisplayDialog("Loja", "Não encontrei a LojaUI.\n\nAbra a cena Menu e tente de novo.", "OK");
            return;
        }

        // Poderes primeiro, depois melhorias e visuais; cada grupo do mais barato ao mais caro
        List<ItemLoja> itens = new List<ItemLoja>();
        foreach (string guid in AssetDatabase.FindAssets("t:ItemLoja"))
        {
            ItemLoja item = AssetDatabase.LoadAssetAtPath<ItemLoja>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null) itens.Add(item);
        }
        itens.Sort((a, b) => a.Tipo != b.Tipo ? a.Tipo.CompareTo(b.Tipo) : a.Preco.CompareTo(b.Preco));

        SerializedObject dados = new SerializedObject(loja);
        SerializedProperty lista = dados.FindProperty("itens");
        lista.arraySize = itens.Count;
        for (int i = 0; i < itens.Count; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = itens[i];
        dados.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(loja.gameObject.scene);
        EditorUtility.DisplayDialog("Loja", "A loja agora tem " + itens.Count + " itens.\n\nSalve a cena com Ctrl+S.", "OK");
    }

    // ---------- Ajudantes ----------

    private static ItemLoja Item(string id, string nome, ItemLoja.Categoria categoria, int preco, int niveis, int aumento,
                                 string descricao, List<string> criados)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Loja")) AssetDatabase.CreateFolder("Assets", "Loja");
        if (!AssetDatabase.IsValidFolder(PastaItens)) AssetDatabase.CreateFolder("Assets/Loja", "Itens");

        string caminho = PastaItens + "/" + id + ".asset";
        ItemLoja existente = AssetDatabase.LoadAssetAtPath<ItemLoja>(caminho);
        if (existente != null) return existente;

        ItemLoja item = ScriptableObject.CreateInstance<ItemLoja>();
        SerializedObject dados = new SerializedObject(item);
        dados.FindProperty("id").stringValue = id;
        dados.FindProperty("nome").stringValue = nome;
        dados.FindProperty("descricao").stringValue = descricao;
        dados.FindProperty("categoria").enumValueIndex = (int)categoria;
        dados.FindProperty("preco").intValue = preco;
        dados.FindProperty("niveisMaximos").intValue = niveis;
        dados.FindProperty("aumentoPorNivel").intValue = aumento;
        dados.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(item, caminho);
        criados.Add(nome);
        return item;
    }

    private static void Componente<T>(GameObject raiz, List<string> instalados, System.Action<SerializedObject> configurar)
        where T : Component
    {
        T componente = raiz.GetComponent<T>();
        if (componente == null)
        {
            componente = raiz.AddComponent<T>();
            instalados.Add(typeof(T).Name);
        }

        SerializedObject dados = new SerializedObject(componente);
        configurar(dados);
        dados.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Poder<T>(GameObject raiz, List<string> instalados, ItemLoja item, float duracao, float recarga,
                                 string tecla) where T : PoderAtivo
    {
        bool novo = raiz.GetComponent<T>() == null;
        Componente<T>(raiz, instalados, d =>
        {
            d.FindProperty("itemLoja").objectReferenceValue = item;
            d.FindProperty("nomeTecla").stringValue = tecla;
            if (!novo) return;   // já existia: não mexe nos tempos que você ajustou

            d.FindProperty("duracao").floatValue = duracao;
            d.FindProperty("recarga").floatValue = recarga;
#if ENABLE_INPUT_SYSTEM
            d.FindProperty("tecla").intValue = (int)System.Enum.Parse(typeof(Key), tecla);
#else
            d.FindProperty("tecla").intValue = (int)System.Enum.Parse(typeof(KeyCode), tecla);
#endif
        });
    }
}
