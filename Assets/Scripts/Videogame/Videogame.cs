using UnityEngine;
using UnityEngine.SceneManagement;

// O TURBO RACER 3000: o videogame "de 1987" que o Jason está jogando quando a história começa.
// O jogador acha que é o jogo de verdade... até perder e a câmera sair da TV para o quarto do Jason.
// Aqui ficam o recorde, o modo minijogo (liberado depois do Nivel1) e a "foto" da última tela
// (usada na transição da TV para o quarto, na cena Historia_Casa).
public static class Videogame
{
    public const int RecordeTonhao = 99999;   // o eterno primeiro lugar. Ninguém sabe quem é o Tonhão.
    private const string ChaveRecorde = "TurboRacerRecorde";

    // Jogando pelo menu (minijogo) em vez de começando a história
    public static bool ModoMinijogo;

    // Da partida que acabou de terminar (para as falas do Jason e a transição)
    public static int UltimosPontos;
    public static bool VeioDoVideogame;
    public static Texture2D UltimaTela { get; private set; }

    public static int Recorde => PlayerPrefs.GetInt(ChaveRecorde, 0);

    // Depois de terminar a história (ida e volta do mercado) o minijogo aparece na tela de níveis
    public static bool Desbloqueado => ProgressoNiveis.Concluido(ProgressoHistoria.PrimeiroNivel);

    // Devolve true se bateu o recorde
    public static bool RegistrarPontos(int pontos)
    {
        UltimosPontos = pontos;
        if (pontos <= Recorde) return false;
        PlayerPrefs.SetInt(ChaveRecorde, pontos);
        PlayerPrefs.Save();
        return true;
    }

    public static void GuardarTela(Texture2D tela)
    {
        LimparTela();
        UltimaTela = tela;
    }

    public static void LimparTela()
    {
        if (UltimaTela != null) Object.Destroy(UltimaTela);
        UltimaTela = null;
    }

    public static void AbrirMinijogo()
    {
        ModoMinijogo = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(ProgressoHistoria.CenaVideogame);
    }
}
