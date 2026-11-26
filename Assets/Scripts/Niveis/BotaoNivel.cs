using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Botão de um nível no menu de fases.
// Fica bloqueado até o jogador terminar o nível anterior (campo "Cena Necessaria").
[RequireComponent(typeof(Button))]
public class BotaoNivel : MonoBehaviour
{
    [Header("Nível")]
    [SerializeField] private string cena = "Nivel1";      // nome exato da cena que este botão abre
    [SerializeField] private string cenaNecessaria = "";   // nível que precisa ser terminado antes (vazio = sempre liberado)
    [Tooltip("Opcional: cena que o botão abre (vazio = a própria \"cena\"). O nível 1 abre o videogame antes da casa.")]
    [SerializeField] private string cenaParaAbrir = "";

    [Header("Visual (opcional)")]
    [SerializeField] private GameObject cadeado;           // aparece quando está bloqueado
    [SerializeField] private GameObject marcaConcluido;    // aparece quando o nível já foi terminado
    [SerializeField] private GameObject numero;            // some quando está bloqueado (o cadeado fica no lugar)
    [SerializeField] private Image moldura;                // imagem de fundo do botão
    [SerializeField] private Sprite molduraLiberada;
    [SerializeField] private Sprite molduraBloqueada;

    private Button botao;

    public bool Liberado =>
        Application.CanStreamedLevelBeLoaded(cena) &&      // a cena existe e está no build
        (string.IsNullOrEmpty(cenaNecessaria) || ProgressoNiveis.Concluido(cenaNecessaria));

    private void Awake()
    {
        botao = GetComponent<Button>();
        botao.onClick.AddListener(Abrir);
    }

    private void OnEnable()
    {
        Atualizar();
    }

    public void Atualizar()
    {
        if (botao == null) botao = GetComponent<Button>();

        bool liberado = Liberado;
        botao.interactable = liberado;   // o Button já deixa o botão mais escuro quando não dá para clicar

        if (cadeado != null) cadeado.SetActive(!liberado);
        if (numero != null) numero.SetActive(liberado);
        if (marcaConcluido != null) marcaConcluido.SetActive(ProgressoNiveis.Concluido(cena));

        if (moldura != null)
        {
            Sprite sprite = liberado ? molduraLiberada : molduraBloqueada;
            if (sprite != null) moldura.sprite = sprite;
        }
    }

    private void Abrir()
    {
        if (!Liberado)
        {
            Debug.Log("Nível " + cena + " bloqueado: termine " + cenaNecessaria + " primeiro.");
            return;
        }

        Time.timeScale = 1f;
        string abrir = !string.IsNullOrEmpty(cenaParaAbrir) && Application.CanStreamedLevelBeLoaded(cenaParaAbrir) ? cenaParaAbrir : cena;
        SceneManager.LoadScene(abrir);
    }
}
