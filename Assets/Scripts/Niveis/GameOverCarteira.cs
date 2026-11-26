using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Quando a carteira do jogador é suspensa (0 pontos), mostra GAME OVER e deixa recomeçar a fase.
// Liga-se sozinho em toda cena que tem CarteiraMotorista + TelaNivelConcluido (ex.: Nivel1).
// O exame (MissaoExame) tem a sua própria reprovação, então lá ele não faz nada.
public class GameOverCarteira : MonoBehaviour
{
    [SerializeField] private float esperaAntesDaTela = 0.8f;   // dá tempo de ver a batida
    [SerializeField] private string titulo = "CARTEIRA SUSPENSA!";
    [SerializeField] private string mensagem = "Você perdeu todos os pontos da carteira. Dirija com cuidado e tente de novo!";

    private CarteiraMotorista carteira;
    private TelaNivelConcluido tela;
    private bool mostrou;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Iniciar()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;
        Instalar();   // a cena que já estava aberta
    }

    private static void AoCarregarCena(Scene cena, LoadSceneMode modo) => Instalar();

    private static void Instalar()
    {
        if (FindFirstObjectByType<MissaoExame>(FindObjectsInactive.Include) != null) return;
        TelaNivelConcluido tela = FindFirstObjectByType<TelaNivelConcluido>(FindObjectsInactive.Include);
        if (tela == null) return;

        foreach (CarteiraMotorista c in FindObjectsByType<CarteiraMotorista>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.GetComponent<GameOverCarteira>() != null) continue;
            GameOverCarteira g = c.gameObject.AddComponent<GameOverCarteira>();
            g.carteira = c;
            g.tela = tela;
            c.aoSuspenderCarteira.AddListener(g.CarteiraSuspensa);
        }
    }

    private void OnDestroy()
    {
        if (carteira != null) carteira.aoSuspenderCarteira.RemoveListener(CarteiraSuspensa);
    }

    private void CarteiraSuspensa()
    {
        if (mostrou || tela == null || tela.Aberta) return;
        mostrou = true;
        StartCoroutine(Mostrar());
    }

    private IEnumerator Mostrar()
    {
        yield return new WaitForSecondsRealtime(esperaAntesDaTela);
        if (tela != null && !tela.Aberta) tela.MostrarGameOver(titulo, mensagem);
    }
}
