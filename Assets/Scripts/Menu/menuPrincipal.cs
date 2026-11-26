using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] private GameObject painelConfiguracoes;
    [SerializeField] private GameObject painelSobre;
    [SerializeField] private GameObject painelTutorial;

    public void Jogar()
    {
        Debug.Log("Clicou em Jogar");
        // Quando criarmos a cena do jogo:
        // SceneManager.LoadScene("Jogo");
    }

    public void Configuracoes()
    {
        Debug.Log("Clicou em Configurações");
        if (painelConfiguracoes != null)
            painelConfiguracoes.SetActive(true);
    }

    public void Sobre()
    {
        Debug.Log("Clicou em Sobre");
        if (painelSobre != null)
            painelSobre.SetActive(true);
    }

    public void Tutorial()
    {
        Debug.Log("Clicou em Tutorial");
        if (painelTutorial != null)
            painelTutorial.SetActive(true);
    }

    public void Sair()
    {
        Debug.Log("Clicou em Sair");
        Application.Quit();
    }
    public void FecharConfiguracoes()
    {
        painelConfiguracoes.SetActive(false);
    }
}