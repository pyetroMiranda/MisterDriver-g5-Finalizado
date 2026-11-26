using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Ferramentas do menu de fases. Aparecem em "Mr Driver > Níveis" no topo da Unity.
public static class FerramentasNiveis
{
    // ---------- Configurar os botões do PnlNiveis ----------

    [MenuItem("Mr Driver/Níveis/Configurar botões de nível no Menu")]
    private static void ConfigurarBotoes()
    {
        MenuController menu = Object.FindFirstObjectByType<MenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.pnlNiveis == null)
        {
            EditorUtility.DisplayDialog("Configurar níveis",
                "Não encontrei o MenuController com o Pnl Niveis preenchido.\n\n" +
                "Abra a cena Menu (Assets/Scenes/Menu) e tente de novo.", "OK");
            return;
        }

        Transform painel = menu.pnlNiveis.transform;
        Transform listaComBotoes = null;
        List<Transform> listasVazias = new List<Transform>();

        // Procura as listas (Scroll Views) do painel: usa a que tem botões e esconde as vazias
        foreach (ScrollRect lista in painel.GetComponentsInChildren<ScrollRect>(true))
        {
            bool temBotoes = lista.content != null && lista.content.GetComponentsInChildren<Button>(true).Length > 0;
            if (temBotoes && listaComBotoes == null) listaComBotoes = lista.transform;
            else if (!temBotoes) listasVazias.Add(lista.transform);
        }

        if (listaComBotoes == null)
        {
            EditorUtility.DisplayDialog("Configurar níveis",
                "Não encontrei nenhum botão de nível dentro do " + painel.name + ".", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Configurar botões de nível");
        int grupo = Undo.GetCurrentGroup();

        Undo.RecordObject(listaComBotoes.gameObject, "Ativar lista");
        listaComBotoes.gameObject.SetActive(true);
        foreach (Transform vazia in listasVazias)
        {
            Undo.RecordObject(vazia.gameObject, "Esconder lista vazia");
            vazia.gameObject.SetActive(false);
        }

        // Botões na ordem em que aparecem: 1º = história (casa + autoescola + exame), 2º = Nivel1, 3º = Nivel2...
        // A cena Tutorial é aberta pelo botão TUTORIAL do menu.
        ScrollRect scroll = listaComBotoes.GetComponent<ScrollRect>();
        string resumo = "";
        int numero = 0;

        foreach (Transform filho in scroll.content)
        {
            Button botao = filho.GetComponent<Button>();
            if (botao == null) continue;
            numero++;

            BotaoNivel nivel = filho.GetComponent<BotaoNivel>();
            if (nivel == null) nivel = Undo.AddComponent<BotaoNivel>(filho.gameObject);

            string cena = numero == 1 ? ProgressoHistoria.CenaCasa : "Nivel" + (numero - 1);
            string necessaria = numero == 1 ? "" : numero == 2 ? ProgressoHistoria.CenaGaragem : "Nivel" + (numero - 2);

            SerializedObject dados = new SerializedObject(nivel);
            dados.FindProperty("cena").stringValue = cena;
            dados.FindProperty("cenaNecessaria").stringValue = necessaria;
            dados.FindProperty("cenaParaAbrir").stringValue = numero == 1 ? ProgressoHistoria.CenaVideogame : "";
            dados.ApplyModifiedProperties();

            bool existe = File.Exists("Assets/Scenes/" + cena + ".unity");
            resumo += "• " + filho.name + " → " + cena +
                      (necessaria == "" ? " (sempre liberado)" : " (precisa terminar " + necessaria + ")") +
                      (existe ? "" : "  [cena ainda não existe: fica bloqueado]") + "\n";
        }

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);

        string build = ColocarNiveisNoBuild();

        Selection.activeGameObject = listaComBotoes.gameObject;
        EditorUtility.DisplayDialog("Configurar níveis",
            "Botões configurados:\n\n" + resumo + "\n" + build + "\nAgora salve a cena com Ctrl+S.", "OK");
    }

    // Coloca todas as cenas Assets/Scenes/Nivel*.unity na lista de cenas do jogo (Build Profiles)
    private static string ColocarNiveisNoBuild()
    {
        List<EditorBuildSettingsScene> cenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        string adicionadas = "";

        string[] arquivos = Directory.GetFiles("Assets/Scenes", "Nivel*.unity");
        System.Array.Sort(arquivos, (a, b) => NumeroDoNivel(a).CompareTo(NumeroDoNivel(b)));

        foreach (string arquivo in arquivos)
        {
            string caminho = arquivo.Replace('\\', '/');
            EditorBuildSettingsScene existente = cenas.Find(c => c.path == caminho);

            if (existente == null)
            {
                cenas.Add(new EditorBuildSettingsScene(caminho, true));
                adicionadas += Path.GetFileNameWithoutExtension(caminho) + " ";
            }
            else if (!existente.enabled)
            {
                existente.enabled = true;
                adicionadas += Path.GetFileNameWithoutExtension(caminho) + " ";
            }
        }

        EditorBuildSettings.scenes = cenas.ToArray();

        return adicionadas == ""
            ? "Todas as cenas de nível já estavam no build.\n"
            : "Adicionadas ao build: " + adicionadas + "\n";
    }

    // ---------- Testes ----------

    [MenuItem("Mr Driver/Níveis/Desbloquear todos os níveis")]
    private static void DesbloquearTodos()
    {
        foreach (string cena in NomesDosNiveis()) ProgressoNiveis.Concluir(cena);
        ProgressoHistoria.ConcluirTudo();
        Debug.Log("Progresso: todos os níveis marcados como concluídos.");
    }

    [MenuItem("Mr Driver/Níveis/Apagar progresso dos níveis")]
    private static void ApagarProgresso()
    {
        foreach (string cena in NomesDosNiveis()) ProgressoNiveis.Desfazer(cena);
        ProgressoHistoria.Apagar();
        Debug.Log("Progresso: apagado (níveis e história).");
    }

    [MenuItem("Mr Driver/Níveis/Mostrar progresso")]
    private static void MostrarProgresso()
    {
        string texto = "Progresso dos níveis:";
        foreach (string cena in NomesDosNiveis())
            texto += "\n  " + cena + ": " + (ProgressoNiveis.Concluido(cena) ? "concluído" : "não concluído");
        Debug.Log(texto);
    }

    private static int NumeroDoNivel(string arquivo)
    {
        int.TryParse(Path.GetFileNameWithoutExtension(arquivo).Substring(5), out int n);
        return n;
    }

    private static List<string> NomesDosNiveis()
    {
        List<string> nomes = new List<string>();
        foreach (string arquivo in Directory.GetFiles("Assets/Scenes", "Nivel*.unity"))
            nomes.Add(Path.GetFileNameWithoutExtension(arquivo));
        nomes.Sort();
        return nomes;
    }
}
