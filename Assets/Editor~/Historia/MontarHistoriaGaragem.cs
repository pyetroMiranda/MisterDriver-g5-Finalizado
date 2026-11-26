using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Garagem da casa do Jason e cena Historia_Garagem (capítulo 2: volta para casa com a carteira).
// Menu: Mr Driver > História > 7. Criar cena da volta para casa (garagem)
public static partial class MontarHistoria
{
    [MenuItem("Mr Driver/História/7. Criar cena da volta para casa (garagem)", priority = 26)]
    private static void MenuGaragem()
    {
        if (File.Exists(CaminhoGaragem) && !EditorUtility.DisplayDialog("História",
                "A cena Historia_Garagem já existe. Recriar do zero? (o que você mudou nela se perde)", "Recriar", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (CriarCasa(2))
            EditorUtility.DisplayDialog("História", "Cena Historia_Garagem criada e salva. Aperte Play para testar!", "OK");
    }

    // ---------- Móveis, portas e o carro ----------

    private static void MoveisGaragem(Transform moveis, char[][] colisao)
    {
        Movel(moveis, colisao, "Bancada", HistoriaArte.Bancada(), 22, 2, 2, 1, HistoriaTextos.Bancada());
        Movel(moveis, colisao, "PrateleiraTintas", HistoriaArte.PrateleiraTintas(), 28, 2, 2, 1, HistoriaTextos.Tintas());
        Movel(moveis, colisao, "PilhaPneus", HistoriaArte.PilhaPneus(), 30, 2, 1, 1, HistoriaTextos.Pneus());
        Movel(moveis, colisao, "Tambor", HistoriaArte.Tambor(), 30, 6, 1, 1, HistoriaTextos.Tambor());
        Movel(moveis, colisao, "Bicicleta", HistoriaArte.Bicicleta(), 30, 9, 1, 2, HistoriaTextos.Bicicleta());
        Movel(moveis, colisao, "CaixaFerramentas", HistoriaArte.CaixaFerramentas(), 22, 10, 1, 1, HistoriaTextos.CaixaFerramentas());
        NaParede(moveis, "Janela da garagem", 24, HistoriaTextos.JanelaGaragem());
        NaParede(moveis, "Placa da vaga", 25, HistoriaTextos.PlacaVagaGaragem());
        NaParede(moveis, "Placa da vaga (2)", 26, HistoriaTextos.PlacaVagaGaragem());
        NaParede(moveis, "Calendario", 27, HistoriaTextos.CalendarioGaragem());
    }

    private static VitrineCarro CriarVitrine(Transform pai, CatalogoCarros catalogo, bool coberta, List<Fala> falasCoberta)
    {
        RectInt v = HistoriaArte.VagaCarro;
        GameObject obj = new GameObject("Carro da garagem");
        obj.transform.SetParent(pai, false);
        obj.transform.position = new Vector3(v.x + v.width / 2f - 0.5f, -(v.y + v.height / 2f - 0.5f), 0f);
        int ordem = 100 + (v.yMax - 1) * 10;   // mesma regra dos móveis

        SpriteRenderer sombra = Filho(obj.transform, "Sombra", HistoriaArte.SombraCarro(), ordem - 1);
        sombra.transform.localPosition = new Vector3(0.1f, -0.12f, 0f);
        SpriteRenderer lataria = Filho(obj.transform, "Lataria", null, ordem);
        SpriteRenderer detalhes = Filho(obj.transform, "Detalhes", null, ordem + 1);
        lataria.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);    // de frente para o portão
        detalhes.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        SpriteRenderer capa = Filho(obj.transform, "Capa", HistoriaArte.Capa(), ordem + 2);

        CatalogoCarros doDisco = MontarCarros.Catalogo();
        if (doDisco != null) catalogo = doDisco;

        // Já mostra o desenho no Editor
        CatalogoCarros.Modelo m = catalogo != null ? catalogo.ModeloDe(GaragemJogador.ModeloPadrao) : null;
        if (m != null)
        {
            lataria.sprite = m.latariaCasa;
            lataria.color = catalogo.CorOu(0);
            detalhes.sprite = m.detalhesCasa;
        }
        lataria.enabled = detalhes.enabled = !coberta;
        capa.enabled = coberta;

        List<Vector2Int> celulas = new List<Vector2Int>();
        for (int y = v.yMin; y < v.yMax; y++)
            for (int x = v.xMin; x < v.xMax; x++) celulas.Add(new Vector2Int(x, y));

        VitrineCarro vitrine = obj.AddComponent<VitrineCarro>();
        vitrine.DefinirCelulas(celulas.ToArray());
        vitrine.DefinirBloqueio(true, false);
        vitrine.Definir(catalogo, lataria, detalhes, capa, coberta, falasCoberta);
        return vitrine;
    }

    private static PortaCasa CriarPortaGaragem(List<Fala> trancada)
    {
        GameObject obj = new GameObject("Porta da garagem");
        obj.transform.position = MapaCasa.ParaMundo(HistoriaArte.PortaGaragem);
        SpriteRenderer sr = Filho(obj.transform, "Desenho", HistoriaArte.PortaGaragemFechada(),
                                  100 + HistoriaArte.PortaGaragem.y * 10);
        PortaCasa porta = obj.AddComponent<PortaCasa>();
        porta.DefinirBloqueio(true, true);
        porta.Definir(false, trancada, sr, HistoriaArte.PortaGaragemAberta(), false);
        return porta;
    }

    private static PortaCasa CriarPortao(List<Fala> trancada)
    {
        RectInt r = HistoriaArte.Portao;
        GameObject obj = new GameObject("Portão da garagem");
        obj.transform.position = new Vector3(r.x + r.width / 2f - 0.5f, -r.y + 0.5f, 0f);   // em cima da linha do portão
        SpriteRenderer sr = Filho(obj.transform, "Desenho", HistoriaArte.DesenharPortao(), 100 + r.y * 10 + 6);

        List<Vector2Int> celulas = new List<Vector2Int>();
        for (int x = r.xMin; x < r.xMax; x++) celulas.Add(new Vector2Int(x, r.y));

        PortaCasa portao = obj.AddComponent<PortaCasa>();
        portao.DefinirCelulas(celulas.ToArray());
        portao.DefinirBloqueio(true, true);
        portao.Definir(true, trancada, sr, null, true);
        return portao;
    }

    private static SpriteRenderer Filho(Transform pai, string nome, Sprite sprite, int ordem)
    {
        SpriteRenderer sr = new GameObject(nome, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        sr.transform.SetParent(pai, false);
        sr.sprite = sprite;
        sr.sortingOrder = ordem;
        return sr;
    }

    // ---------- Tela da garagem (escolher carro e cor) ----------

    private static TelaGaragem CriarTelaGaragem(Transform canvas, CatalogoCarros catalogo)
    {
        Vector2 meio = new Vector2(0.5f, 0.5f);
        Color32 roxo = new Color32(42, 14, 56, 255);

        RectTransform pnl = Ret(canvas, "PnlGaragem", Vector2.zero, Vector2.one, meio, Vector2.zero, Vector2.zero);
        Image escuro = pnl.gameObject.AddComponent<Image>();
        escuro.color = new Color(0.04f, 0.02f, 0.07f, 0.93f);
        CanvasGroup grupo = pnl.gameObject.AddComponent<CanvasGroup>();

        RectTransform janela = Ret(pnl, "Janela", meio, meio, meio, new Vector2(0f, 20f), new Vector2(1640f, 900f));
        Imagem(janela, HistoriaArte.MolduraEscura(), true);

        // Título
        RectTransform titulo = Ret(janela, "Titulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), meio,
                                   new Vector2(0f, -6f), new Vector2(820f, 110f));
        Imagem(titulo, HistoriaArte.MolduraDialogo(), true);
        TMP_Text txtTitulo = Texto(titulo, "TxtTitulo", "GARAGEM DO BETÃO", 58, FontStyles.Bold, roxo, TextAlignmentOptions.Center);
        Esticar(txtTitulo.rectTransform, 10f);

        // Palco do carro: holofote, plataforma giratória e o carro
        Vector2 palco = new Vector2(-380f, -20f);
        Image luz = Imagem(Ret(janela, "Holofote", meio, meio, meio, palco, new Vector2(780f, 780f)), HistoriaArte.Holofote(), false);
        luz.color = new Color(1f, 0.85f, 0.98f, 0.25f);
        Imagem(Ret(janela, "Plataforma", meio, meio, meio, palco, new Vector2(560f, 560f)), HistoriaArte.PlataformaGiratoria(), false);
        RectTransform giro = Ret(janela, "Giro", meio, meio, meio, palco, new Vector2(10f, 10f));
        Image lataria = Imagem(Ret(giro, "Lataria", meio, meio, meio, Vector2.zero, new Vector2(250f, 420f)), null, false);
        Image detalhes = Imagem(Ret(giro, "Detalhes", meio, meio, meio, Vector2.zero, new Vector2(250f, 420f)), null, false);
        lataria.preserveAspect = detalhes.preserveAspect = true;

        Sprite seta = HistoriaArte.SetaDialogo();
        Button anterior = BotaoSeta(janela, "BtnAnterior", palco + new Vector2(-350f, 0f), seta, -90f);
        Button proximo = BotaoSeta(janela, "BtnProximo", palco + new Vector2(350f, 0f), seta, 90f);

        // Informações do carro
        RectTransform info = Ret(janela, "Info", meio, meio, meio, new Vector2(380f, 160f), new Vector2(720f, 380f));
        Imagem(info, HistoriaArte.MolduraBranca(), true);
        TMP_Text contador = Texto(info, "TxtContador", "CARRO 1 DE 1", 28, FontStyles.Bold, new Color32(220, 20, 170, 255),
                                  TextAlignmentOptions.TopLeft);
        Posicionar(contador.rectTransform, new Vector2(0f, 1f), new Vector2(44f, -34f), new Vector2(640f, 40f));
        TMP_Text nome = Texto(info, "TxtNome", "NISSAN KICKS", 66, FontStyles.Bold, roxo, TextAlignmentOptions.TopLeft);
        Posicionar(nome.rectTransform, new Vector2(0f, 1f), new Vector2(44f, -74f), new Vector2(640f, 84f));
        TMP_Text descricao = Texto(info, "TxtDescricao", "Descrição", 32, FontStyles.Normal, new Color32(80, 56, 96, 255),
                                   TextAlignmentOptions.TopLeft);
        Posicionar(descricao.rectTransform, new Vector2(0f, 1f), new Vector2(44f, -170f), new Vector2(640f, 190f));
        descricao.lineSpacing = 6f;

        // Cores
        TMP_Text txtCor = Texto(janela, "TxtCor", "COR:", 38, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        RectTransform rtCor = txtCor.rectTransform;
        rtCor.anchorMin = rtCor.anchorMax = meio;
        rtCor.anchoredPosition = new Vector2(380f, -80f);
        rtCor.sizeDelta = new Vector2(720f, 54f);

        RectTransform grade = Ret(janela, "GradeCores", meio, meio, meio, new Vector2(380f, -200f), new Vector2(720f, 170f));
        GridLayoutGroup layout = grade.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(88f, 66f);
        layout.spacing = new Vector2(22f, 22f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 6;
        layout.childAlignment = TextAnchor.MiddleCenter;

        RectTransform modeloCor = Ret(grade, "ModeloCor", meio, meio, meio, Vector2.zero, new Vector2(88f, 66f));
        Image imgCor = Imagem(modeloCor, HistoriaArte.MolduraBranca(), true);
        imgCor.raycastTarget = true;
        Button botaoCor = modeloCor.gameObject.AddComponent<Button>();
        botaoCor.targetGraphic = imgCor;
        modeloCor.gameObject.SetActive(false);

        RectTransform selecao = Ret(janela, "SelecaoCor", meio, meio, meio, Vector2.zero, new Vector2(100f, 80f));
        Imagem(selecao, HistoriaArte.MolduraSelecao(), true);
        selecao.gameObject.SetActive(false);

        // Confirmar e voltar
        Button confirmar = BotaoTexto(janela, "BtnConfirmar", "CONFIRMAR", new Vector2(250f, -360f), new Vector2(340f, 96f),
                                      new Color32(70, 190, 100, 255));
        Button voltar = BotaoTexto(janela, "BtnVoltar", "VOLTAR", new Vector2(600f, -360f), new Vector2(280f, 96f),
                                   new Color32(230, 70, 80, 255));

        TMP_Text dica = Texto(pnl, "TxtDica", "SETAS ESQUERDA / DIREITA: trocar o carro     SETAS CIMA / BAIXO: trocar a cor     " +
                              "ENTER: confirmar     ESC: voltar", 28, FontStyles.Normal, new Color32(220, 200, 240, 255),
                              TextAlignmentOptions.Center);
        Posicionar(dica.rectTransform, new Vector2(0.5f, 0f), new Vector2(-900f, 70f), new Vector2(1800f, 44f));

        GameObject somObj = new GameObject("SomGaragem", typeof(AudioSource));
        somObj.transform.SetParent(canvas, false);
        AudioSource som = somObj.GetComponent<AudioSource>();
        som.playOnAwake = false;
        som.outputAudioMixerGroup = GrupoEfeitos();

        // Salvar as imagens acima reimporta assets e pode invalidar a referência antiga: carrega de novo do disco
        CatalogoCarros doDisco = MontarCarros.Catalogo();
        if (doDisco != null) catalogo = doDisco;

        TelaGaragem tela = canvas.gameObject.AddComponent<TelaGaragem>();
        SerializedObject d = new SerializedObject(tela);
        d.FindProperty("catalogo").objectReferenceValue = catalogo;
        d.FindProperty("painel").objectReferenceValue = pnl.gameObject;
        d.FindProperty("grupo").objectReferenceValue = grupo;
        d.FindProperty("janela").objectReferenceValue = janela;
        d.FindProperty("giro").objectReferenceValue = giro;
        d.FindProperty("imgLataria").objectReferenceValue = lataria;
        d.FindProperty("imgDetalhes").objectReferenceValue = detalhes;
        d.FindProperty("txtContador").objectReferenceValue = contador;
        d.FindProperty("txtNome").objectReferenceValue = nome;
        d.FindProperty("txtDescricao").objectReferenceValue = descricao;
        d.FindProperty("txtCor").objectReferenceValue = txtCor;
        d.FindProperty("gradeCores").objectReferenceValue = grade;
        d.FindProperty("modeloBotaoCor").objectReferenceValue = botaoCor;
        d.FindProperty("selecaoCor").objectReferenceValue = selecao;
        d.FindProperty("som").objectReferenceValue = som;
        d.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(anterior.onClick, tela.Anterior);
        UnityEventTools.AddPersistentListener(proximo.onClick, tela.Proximo);
        UnityEventTools.AddPersistentListener(confirmar.onClick, tela.Confirmar);
        UnityEventTools.AddPersistentListener(voltar.onClick, tela.Voltar);

        pnl.gameObject.SetActive(false);

        // A caixa de diálogo continua por cima de tudo
        Transform dialogo = canvas.Find("PnlDialogo");
        if (dialogo != null) dialogo.SetAsLastSibling();
        return tela;
    }

    private static Button BotaoSeta(Transform pai, string nome, Vector2 posicao, Sprite seta, float giro)
    {
        Vector2 meio = new Vector2(0.5f, 0.5f);
        RectTransform rt = Ret(pai, nome, meio, meio, meio, posicao, new Vector2(110f, 110f));
        Image fundo = Imagem(rt, HistoriaArte.MolduraDialogo(), true);
        fundo.raycastTarget = true;
        RectTransform icone = Ret(rt, "Seta", meio, meio, meio, Vector2.zero, new Vector2(60f, 40f));
        icone.localRotation = Quaternion.Euler(0f, 0f, giro);
        Imagem(icone, seta, false);
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = fundo;
        return b;
    }

    private static Button BotaoTexto(Transform pai, string nome, string texto, Vector2 posicao, Vector2 tamanho, Color cor)
    {
        Vector2 meio = new Vector2(0.5f, 0.5f);
        RectTransform rt = Ret(pai, nome, meio, meio, meio, posicao, tamanho);
        Image fundo = Imagem(rt, HistoriaArte.MolduraBranca(), true);
        fundo.color = cor;
        fundo.raycastTarget = true;
        TMP_Text t = Texto(rt, "Txt", texto, 40, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Esticar(t.rectTransform, 8f);
        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = fundo;
        return b;
    }
}
