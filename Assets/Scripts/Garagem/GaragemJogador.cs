using System;
using UnityEngine;

// O carro e a cor que o jogador escolheu na garagem (salvo em PlayerPrefs). Vale para todas as cenas.
// É estático: GaragemJogador.ModeloId, GaragemJogador.Escolher("Kicks", 0)...
public static class GaragemJogador
{
    private const string ChaveModelo = "Garagem_Modelo";
    private const string ChaveCor = "Garagem_Cor";
    public const string ModeloPadrao = "Kicks";

    // Avisa o carro do jogador e o carro da garagem para trocarem o desenho
    public static event Action AoMudar;

    public static string ModeloId => PlayerPrefs.GetString(ChaveModelo, ModeloPadrao);
    public static int CorIndice => PlayerPrefs.GetInt(ChaveCor, 0);

    public static void Escolher(string modelo, int cor)
    {
        PlayerPrefs.SetString(ChaveModelo, string.IsNullOrEmpty(modelo) ? ModeloPadrao : modelo);
        PlayerPrefs.SetInt(ChaveCor, Mathf.Max(0, cor));
        PlayerPrefs.Save();
        AoMudar?.Invoke();
    }

    // Volta para o Kicks vermelho (usado ao apagar o progresso da história)
    public static void Apagar()
    {
        PlayerPrefs.DeleteKey(ChaveModelo);
        PlayerPrefs.DeleteKey(ChaveCor);
        PlayerPrefs.Save();
        AoMudar?.Invoke();
    }
}
