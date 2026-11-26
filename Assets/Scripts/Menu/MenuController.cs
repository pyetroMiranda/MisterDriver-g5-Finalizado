using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public GameObject pnlMenu;
    public GameObject PnlTeste;
    public GameObject pnlSobre;
    public GameObject pnlNiveis;
    public GameObject pnlLoja;

    public void AbrirConfiguracoes()
    {
        MostrarSomente(PnlTeste);
    }

    public void AbrirSobre()
    {
        MostrarSomente(pnlSobre);
    }

    public void AbrirNiveis()
    {
        MostrarSomente(pnlNiveis);
    }

    public void AbrirLoja()
    {
        MostrarSomente(pnlLoja);
    }

    // Botão JOGAR: quem ainda não tirou a carteira continua a história de onde parou
    // (casa → autoescola → exame na cidade); quem já tirou escolhe o nível
    public void Jogar()
    {
        string capitulo = ProgressoHistoria.ProximaCena();
        if (!ProgressoHistoria.HistoriaConcluida && Application.CanStreamedLevelBeLoaded(capitulo))
            TransicaoTela.Carregar(capitulo);
        else
            AbrirNiveis();
    }

    // Botão TUTORIAL: abre a cena Tutorial (a pista de treino)
    public void AbrirTutorial()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(ProgressoHistoria.CenaTutorial);
    }

    public void VoltarMenu()
    {
        MostrarSomente(pnlMenu);
    }

    public void Sair()
    {
        Application.Quit();
    }

    // Esconde todos os painéis e mostra só o escolhido (painéis vazios são ignorados)
    private void MostrarSomente(GameObject painel)
    {
        GameObject[] paineis = { pnlMenu, PnlTeste, pnlSobre, pnlNiveis, pnlLoja };

        foreach (GameObject p in paineis)
        {
            if (p != null) p.SetActive(p == painel);
        }
    }
}
