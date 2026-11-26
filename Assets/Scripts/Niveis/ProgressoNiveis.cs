using UnityEngine;

// Guarda quais níveis o jogador já terminou (salvo em PlayerPrefs).
// É estático: qualquer script pode usar ProgressoNiveis.Concluir("Nivel1") ou ProgressoNiveis.Concluido("Nivel1").
public static class ProgressoNiveis
{
    private const string Prefixo = "Nivel_Concluido_";

    public static bool Concluido(string cena)
    {
        return !string.IsNullOrEmpty(cena) && PlayerPrefs.GetInt(Prefixo + cena, 0) == 1;
    }

    public static void Concluir(string cena)
    {
        if (string.IsNullOrEmpty(cena) || Concluido(cena)) return;

        PlayerPrefs.SetInt(Prefixo + cena, 1);
        PlayerPrefs.Save();
        Debug.Log("Progresso: nível " + cena + " concluído.");
    }

    // Usado pelas ferramentas de teste (menu Mr Driver > Níveis)
    public static void Desfazer(string cena)
    {
        PlayerPrefs.DeleteKey(Prefixo + cena);
        PlayerPrefs.Save();
    }
}
