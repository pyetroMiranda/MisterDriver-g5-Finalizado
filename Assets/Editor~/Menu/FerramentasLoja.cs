using UnityEditor;
using UnityEngine;

// Ferramentas de teste da loja. Aparecem no menu "Mr Driver" no topo da Unity.
// Ficam na pasta Editor, então não vão para o jogo final.
public static class FerramentasLoja
{
    private const string PastaItens = "Assets/Loja/Itens";

    [MenuItem("Mr Driver/Loja/Dar 1000 moedas")]
    private static void DarMoedas()
    {
        Loja.AdicionarMoedas(1000);
        Debug.Log("Loja: +1000 moedas. Saldo: " + Loja.Moedas);
    }

    [MenuItem("Mr Driver/Loja/Zerar moedas")]
    private static void ZerarMoedas()
    {
        Loja.DefinirMoedas(0);
        Debug.Log("Loja: moedas zeradas.");
    }

    [MenuItem("Mr Driver/Loja/Desfazer todas as compras")]
    private static void DesfazerCompras()
    {
        foreach (ItemLoja item in TodosOsItens()) Loja.RemoverCompra(item);
        Debug.Log("Loja: todas as compras foram desfeitas.");
    }

    [MenuItem("Mr Driver/Loja/Mostrar saldo e compras")]
    private static void MostrarEstado()
    {
        string texto = "Loja: saldo = " + Loja.Moedas;
        foreach (ItemLoja item in TodosOsItens())
            texto += "\n  " + item.Nome + " (" + item.Id + "): " + (item.Comprado ? "comprado" : "não comprado");
        Debug.Log(texto);
    }

    [MenuItem("Mr Driver/Loja/Criar itens dos poderes")]
    private static void CriarItensDosPoderes()
    {
        CriarItem("Nitro", "Nitro", "Aperte Ctrl para um boost de velocidade por 3 segundos.", 300);
        CriarItem("Buzina", "Buzina", "Aperte H para fazer o trânsito próximo parar por alguns segundos.", 150);
        CriarItem("MolaParaChoque", "Mola-de-para-choque",
                  "Aperte M para criar um campo de força que empurra os carros que encostarem.", 250);

        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(PastaItens);
        Debug.Log("Loja: itens criados em " + PastaItens);
    }

    private static void CriarItem(string arquivo, string nome, string descricao, int preco)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Loja")) AssetDatabase.CreateFolder("Assets", "Loja");
        if (!AssetDatabase.IsValidFolder(PastaItens)) AssetDatabase.CreateFolder("Assets/Loja", "Itens");

        string caminho = PastaItens + "/" + arquivo + ".asset";
        if (AssetDatabase.LoadAssetAtPath<ItemLoja>(caminho) != null)
        {
            Debug.Log("Loja: " + caminho + " já existe, não foi alterado.");
            return;
        }

        ItemLoja item = ScriptableObject.CreateInstance<ItemLoja>();
        SerializedObject dados = new SerializedObject(item);
        dados.FindProperty("id").stringValue = arquivo;
        dados.FindProperty("nome").stringValue = nome;
        dados.FindProperty("descricao").stringValue = descricao;
        dados.FindProperty("preco").intValue = preco;
        dados.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(item, caminho);
    }

    private static ItemLoja[] TodosOsItens()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemLoja");
        ItemLoja[] itens = new ItemLoja[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            itens[i] = AssetDatabase.LoadAssetAtPath<ItemLoja>(AssetDatabase.GUIDToAssetPath(guids[i]));
        return itens;
    }
}
