using UnityEngine;
using UnityEngine.UI;

// Botões de TESTE da loja: "+ MOEDAS" e "RESETAR PODERES" (desfaz todas as compras da loja).
// São cópias do botão VOLTAR da própria loja, então têm o mesmo visual dos outros botões do menu.
// Só aparecem no Editor e em "Development Build" (no jogo final eles não são criados).
public static class BotoesTesteMoedas
{
    private const int Quantidade = 100;

    public static void Criar(LojaUI loja)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Transform painelLoja = loja != null ? loja.transform : null;
        if (painelLoja == null || Achar(painelLoja, "BlocoTesteMais") != null) return;

        Transform voltar = Achar(painelLoja, "BlocoVoltar");
        if (voltar == null)
        {
            Debug.LogWarning("BotoesTesteMoedas: não achei o BlocoVoltar da loja.", painelLoja);
            return;
        }

        Sprite iconeMoeda = null;
        Transform moeda = Achar(painelLoja, "Moeda");
        if (moeda != null && moeda.GetComponent<Image>() != null) iconeMoeda = moeda.GetComponent<Image>().sprite;

        Criar(voltar, "BlocoTesteMais", "+" + Quantidade, "MOEDAS (TESTE)",
              "Teste: ganha " + Quantidade + " moedas.", new Color32(60, 170, 90, 255), iconeMoeda,
              new Vector2(-40f, 98f), () => Loja.AdicionarMoedas(Quantidade));
        Criar(voltar, "BlocoTesteResetar", "RESETAR", "PODERES (TESTE)",
              "Teste: desfaz todas as compras de poderes e melhorias.", new Color32(200, 90, 50, 255), SpriteCarregado("IconeRecomecar"),
              new Vector2(-360f, 98f), () => ResetarPoderes(loja));
#endif
    }

    private static void ResetarPoderes(LojaUI loja)
    {
        if (loja.Itens == null) return;
        foreach (ItemLoja item in loja.Itens) Loja.RemoverCompra(item);
        Loja.DefinirMoedas(Loja.Moedas);   // avisa os cartões da loja para se atualizarem
        Debug.Log("Loja (teste): todas as compras foram desfeitas.");
    }

    private static void Criar(Transform modelo, string nome, string titulo, string sub, string descricao, Color cor,
                              Sprite icone, Vector2 posicao, UnityEngine.Events.UnityAction acao)
    {
        GameObject novo = Object.Instantiate(modelo.gameObject, modelo.parent);
        novo.name = nome;

        // Fica no canto de baixo, do lado direito (o VOLTAR fica do lado esquerdo)
        RectTransform rt = (RectTransform)novo.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = posicao;

        BlocoMenu bloco = novo.GetComponent<BlocoMenu>();
        if (bloco != null)
        {
            bloco.DefinirTextos(titulo, sub, descricao);
            bloco.DefinirCor(cor);
            bloco.DefinirIcone(icone);
        }

        Button botao = novo.GetComponent<Button>();
        if (botao != null)
        {
            botao.onClick = new Button.ButtonClickedEvent();   // tira o "voltar" que veio na cópia
            botao.onClick.AddListener(acao);
            botao.onClick.AddListener(() => Sons.Tocar("Moeda"));
            Navigation n = botao.navigation;
            n.mode = Navigation.Mode.Automatic;
            botao.navigation = n;
        }
        if (novo.GetComponent<SomBotao>() == null) novo.AddComponent<SomBotao>();
    }

    // O ícone de "recomeçar" já é usado na tela de Opções do menu, então já está carregado
    private static Sprite SpriteCarregado(string nome)
    {
        foreach (Sprite s in Resources.FindObjectsOfTypeAll<Sprite>())
            if (s.name == nome) return s;
        return null;
    }

    private static Transform Achar(Transform raiz, string nome)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nome) return t;
        return null;
    }
}
