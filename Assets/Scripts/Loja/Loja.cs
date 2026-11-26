using System;
using UnityEngine;

// Cofre do jogador: guarda as moedas e o que já foi comprado (salvo em PlayerPrefs).
// Itens com níveis (melhorias) podem ser comprados várias vezes; o preço sobe a cada nível.
// É estático, então qualquer script de qualquer cena pode usar: Loja.Moedas, Loja.Comprar(item)...
public static class Loja
{
    private const string ChaveMoedas = "Loja_Moedas";
    private const string PrefixoComprado = "Loja_Comprado_";   // versão antiga (compra única)
    private const string PrefixoNivel = "Loja_Nivel_";

    // Avisos para a interface se atualizar sozinha
    public static event Action<int> AoMudarMoedas;     // envia o novo saldo
    public static event Action<ItemLoja> AoComprar;    // envia o item comprado

    // ---------- Moedas ----------

    public static int Moedas => PlayerPrefs.GetInt(ChaveMoedas, 0);

    public static void AdicionarMoedas(int quantidade)
    {
        if (quantidade <= 0) return;
        DefinirMoedas(Moedas + quantidade);
    }

    // Devolve false (e não gasta nada) se não tiver moedas suficientes
    public static bool GastarMoedas(int quantidade)
    {
        if (quantidade < 0 || Moedas < quantidade) return false;
        DefinirMoedas(Moedas - quantidade);
        return true;
    }

    public static void DefinirMoedas(int valor)
    {
        valor = Mathf.Max(0, valor);
        PlayerPrefs.SetInt(ChaveMoedas, valor);
        PlayerPrefs.Save();
        AoMudarMoedas?.Invoke(valor);
    }

    // ---------- Itens ----------

    // Quantos níveis o jogador tem deste item (0 = não comprou)
    public static int Nivel(ItemLoja item)
    {
        if (item == null) return 0;

        int nivel = PlayerPrefs.GetInt(PrefixoNivel + item.Id, -1);
        if (nivel < 0) nivel = PlayerPrefs.GetInt(PrefixoComprado + item.Id, 0) == 1 ? 1 : 0;   // compra antiga
        if (item.GratisNoInicio) nivel = Mathf.Max(nivel, 1);

        return Mathf.Clamp(nivel, 0, item.NiveisMaximos);
    }

    public static bool Comprado(ItemLoja item) => Nivel(item) > 0;

    public static bool NoMaximo(ItemLoja item) => item != null && Nivel(item) >= item.NiveisMaximos;

    // Preço do próximo nível
    public static int PrecoProximo(ItemLoja item)
    {
        if (item == null) return 0;
        return item.Preco + Nivel(item) * item.AumentoPorNivel;
    }

    public static bool PodeComprar(ItemLoja item)
    {
        return item != null && !NoMaximo(item) && Moedas >= PrecoProximo(item);
    }

    // Desconta as moedas e sobe um nível do item. Devolve false se não deu.
    public static bool Comprar(ItemLoja item)
    {
        if (!PodeComprar(item)) return false;

        int preco = PrecoProximo(item);
        if (!GastarMoedas(preco)) return false;

        int novoNivel = Nivel(item) + 1;
        PlayerPrefs.SetInt(PrefixoNivel + item.Id, novoNivel);
        PlayerPrefs.SetInt(PrefixoComprado + item.Id, 1);
        PlayerPrefs.Save();

        Debug.Log("Loja: comprou " + item.Nome + (item.TemNiveis ? " nível " + novoNivel : "") +
                  " por " + preco + " moedas. Saldo: " + Moedas);
        AoComprar?.Invoke(item);
        return true;
    }

    // Usado pelas ferramentas de teste (menu Mr Driver > Loja)
    public static void RemoverCompra(ItemLoja item)
    {
        if (item == null) return;
        PlayerPrefs.DeleteKey(PrefixoComprado + item.Id);
        PlayerPrefs.DeleteKey(PrefixoNivel + item.Id);
        PlayerPrefs.Save();
    }
}
