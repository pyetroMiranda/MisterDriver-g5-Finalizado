using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
public class ChegadaTutorial : MonoBehaviour
{
    [SerializeField] private GameObject pnlNivelConcluido;
    [SerializeField] private CarroController carro;
    [SerializeField] private GameObject avisoChegada;
    [SerializeField] private GameObject marcacaoChegada;
    [SerializeField] private float distanciaAviso = 4f;
    [SerializeField] private int moedasAoConcluir = 50;   // recompensa da loja ao terminar a fase

    private BoxCollider2D area;
    private Collider2D colliderCarro;
    private float tempoParado;
    private bool concluido;

    private void Start()
    {
        area = GetComponent<BoxCollider2D>();

        MostrarIndicacoes(false);

        // Se o carro não foi ligado no Inspector, procura sozinho
        if (carro == null) carro = FindFirstObjectByType<CarroController>();

        if (carro == null || pnlNivelConcluido == null)
        {
            Debug.LogError(
                "Preencha os campos Carro e Pnl Nivel Concluido.",
                this
            );

            enabled = false;
            return;
        }

        colliderCarro = carro.GetComponent<Collider2D>();

        if (colliderCarro == null)
        {
            Debug.LogError("O carro precisa de um Collider2D.", carro);
            enabled = false;
            return;
        }

        pnlNivelConcluido.SetActive(false);
    }

    private void Update()
    {
        if (carro == null || area == null)
            return;

        Vector2 posicaoCarro = carro.transform.position;
        Vector2 pontoMaisProximo = area.ClosestPoint(posicaoCarro);

        float distancia = Vector2.Distance(
            posicaoCarro,
            pontoMaisProximo
        );

        bool mostrar = !concluido && distancia <= distanciaAviso;

        MostrarIndicacoes(mostrar);
    }

    private void FixedUpdate()
    {
        if (concluido)
            return;

        Bounds chegada = area.bounds;
        Bounds veiculo = colliderCarro.bounds;

        bool inteiroDentro =
            veiculo.min.x >= chegada.min.x &&
            veiculo.max.x <= chegada.max.x &&
            veiculo.min.y >= chegada.min.y &&
            veiculo.max.y <= chegada.max.y;

        if (!inteiroDentro || carro.VelocidadeAtual > 0.05f)
        {
            tempoParado = 0f;
            return;
        }

        tempoParado += Time.fixedDeltaTime;

        if (tempoParado >= 1f)
        {
            concluido = true;
            MostrarIndicacoes(false);
            Loja.AdicionarMoedas(moedasAoConcluir);
            ProgressoNiveis.Concluir(gameObject.scene.name);   // libera o próximo nível no menu

            pnlNivelConcluido.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    private void MostrarIndicacoes(bool mostrar)
    {
        if (avisoChegada != null &&
            avisoChegada.activeSelf != mostrar)
        {
            avisoChegada.SetActive(mostrar);
        }

        if (marcacaoChegada != null &&
            marcacaoChegada.activeSelf != mostrar)
        {
            marcacaoChegada.SetActive(mostrar);
        }
    }

    public void JogarNovamente()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameObject.scene.name);
    }

    public void VoltarMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }
}
