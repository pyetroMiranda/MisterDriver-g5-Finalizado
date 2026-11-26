using System.Collections;
using UnityEngine;

// Vaga de chegada de um nível: quando o carro para inteiro dentro dela por um instante,
// o nível é concluído, o próximo é liberado e aparece a tela de conclusão.
// Serve para qualquer nível.
// Se algum componente da cena implementar IFinalNivel (ex.: a missão da história), ele roda antes da tela
// e pode até Interromper() a conclusão (a missão continua em outra cena).
[RequireComponent(typeof(BoxCollider2D))]
public class ChegadaNivel : MonoBehaviour
{
    [Header("Ligações (procura sozinho se ficar vazio)")]
    [SerializeField] private CarroController carro;
    [SerializeField] private TelaNivelConcluido tela;

    [Header("Regras")]
    [SerializeField] private float tempoParado = 1f;               // segundos parado dentro da vaga
    [SerializeField] private float velocidadeParado = 0.05f;       // abaixo disso conta como parado
    [SerializeField] private float folga = 0.15f;                  // tolerância nas bordas da vaga

    [Header("Recompensa")]
    [SerializeField] private int moedas = 50;

    [Header("Indicações (opcional)")]
    [SerializeField] private GameObject avisoChegada;              // ex.: balão "Pare aqui"
    [SerializeField] private GameObject marcacaoChegada;           // ex.: contorno da vaga
    [SerializeField] private float distanciaAviso = 4f;            // aparecem quando o carro chega perto

    [Header("Suavidade")]
    [SerializeField] private float duracaoCameraLenta = 0.5f;

    private BoxCollider2D area;
    private Collider2D colisorCarro;
    private float tempoDentro;
    private bool concluido;
    private bool interrompido;

    // A missão da história pede para não concluir o nível agora (ex.: o Jason vai entrar no mercado)
    public void Interromper() => interrompido = true;

    private void Start()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;

        if (carro == null) carro = FindFirstObjectByType<CarroController>();
        if (carro != null) colisorCarro = carro.GetComponentInChildren<Collider2D>();
        if (tela == null) tela = FindFirstObjectByType<TelaNivelConcluido>(FindObjectsInactive.Include);

        if (carro == null || colisorCarro == null)
        {
            Debug.LogError("ChegadaNivel: não encontrei o carro (com Collider2D) nesta cena.", this);
            enabled = false;
            return;
        }

        MostrarIndicacoes(false);
    }

    private void Update()
    {
        if (concluido) return;

        Vector2 posicao = carro.transform.position;
        float distancia = Vector2.Distance(posicao, area.ClosestPoint(posicao));
        MostrarIndicacoes(distancia <= distanciaAviso);
    }

    private void FixedUpdate()
    {
        if (concluido) return;

        Bounds vaga = area.bounds;
        Bounds veiculo = colisorCarro.bounds;

        // Só X e Y importam (jogo 2D)
        bool inteiroDentro =
            veiculo.min.x >= vaga.min.x - folga && veiculo.max.x <= vaga.max.x + folga &&
            veiculo.min.y >= vaga.min.y - folga && veiculo.max.y <= vaga.max.y + folga;
        bool parado = carro.VelocidadeAtual <= velocidadeParado;

        if (!inteiroDentro || !parado)
        {
            tempoDentro = 0f;
            return;
        }

        tempoDentro += Time.fixedDeltaTime;
        if (tempoDentro >= tempoParado) StartCoroutine(Concluir());
    }

    private IEnumerator Concluir()
    {
        Sons.Tocar("Estacionou");
        concluido = true;
        MostrarIndicacoes(false);

        // O jogo vai parando suave antes da tela aparecer
        float inicio = Time.timeScale;
        float t = 0f;
        while (t < duracaoCameraLenta)
        {
            t += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(inicio, 0f, Mathf.SmoothStep(0f, 1f, t / duracaoCameraLenta));
            yield return null;
        }
        Time.timeScale = 0f;

        // Falas da história (ou outra coisa) antes da tela de conclusão
        foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (mb is IFinalNivel final && mb.isActiveAndEnabled) yield return final.AntesDaTela(this);
        if (interrompido) yield break;
        Time.timeScale = 0f;

        ProgressoNiveis.Concluir(gameObject.scene.name);   // libera o próximo nível no menu
        Loja.AdicionarMoedas(moedas);

        if (tela != null) tela.Mostrar(moedas);
        else Debug.LogWarning("ChegadaNivel: nível concluído, mas não há TelaNivelConcluido na cena.", this);
    }

    private void MostrarIndicacoes(bool mostrar)
    {
        if (avisoChegada != null && avisoChegada.activeSelf != mostrar) avisoChegada.SetActive(mostrar);
        if (marcacaoChegada != null && marcacaoChegada.activeSelf != mostrar) marcacaoChegada.SetActive(mostrar);
    }

    // Mostra a vaga em verde na aba Scene
    private void OnDrawGizmos()
    {
        BoxCollider2D caixa = GetComponent<BoxCollider2D>();
        if (caixa == null) return;
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(caixa.bounds.center, caixa.bounds.size);
    }
}

// Algo que acontece quando o carro termina o nível, antes da tela de conclusão (o jogo já está parado).
public interface IFinalNivel
{
    IEnumerator AntesDaTela(ChegadaNivel chegada);
}
