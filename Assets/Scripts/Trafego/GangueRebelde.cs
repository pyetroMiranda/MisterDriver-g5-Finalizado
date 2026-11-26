using System.Collections.Generic;
using UnityEngine;

// A Gangue do Escapamento nas ruas: cria os carros pichados e as motos (CarroRebelde) em esquinas
// longe do jogador e solta eles pela cidade. Só aparecem quando a missão chama Soltar()
// (no Nivel1, depois que o Jason sai do mercado). Montado por Mr Driver > História > 8. Montar o Nivel1.
public class GangueRebelde : MonoBehaviour
{
    [Header("Desenhos (feitos pela ferramenta)")]
    [SerializeField] private Sprite[] carros = new Sprite[0];
    [SerializeField] private Sprite[] motos = new Sprite[0];

    [Header("Quantos")]
    [SerializeField, Min(0)] private int quantidadeCarros = 4;
    [SerializeField, Min(0)] private int quantidadeMotos = 3;
    [Tooltip("Nascem pelo menos a esta distância do jogador.")]
    [SerializeField] private float distanciaMinima = 28f;

    [Header("Direção (sem lei nenhuma)")]
    [SerializeField] private float velocidadeCarro = 9f;
    [SerializeField] private float velocidadeMoto = 10.5f;
    [SerializeField] private int ordemDesenho = 9;

    private readonly List<CarroRebelde> soltos = new List<CarroRebelde>();

    public IReadOnlyList<CarroRebelde> Soltos => soltos;

    // Usado pela ferramenta do Editor
    public void Definir(Sprite[] desenhosCarros, Sprite[] desenhosMotos)
    {
        carros = desenhosCarros;
        motos = desenhosMotos;
    }

    public void Soltar()
    {
        if (soltos.Count > 0) return;

        RedeTrafego rede = FindFirstObjectByType<RedeTrafego>();
        GrafoTrafego grafo = rede != null ? rede.Grafo : null;
        if (grafo == null || grafo.Quantidade < 2)
        {
            Debug.LogWarning("GangueRebelde: não achei a RedeTrafego com ruas. Os rebeldes não vão aparecer.", this);
            return;
        }

        CarroController jogador = FindFirstObjectByType<CarroController>();
        Vector2 longeDe = jogador != null ? (Vector2)jogador.transform.position : Vector2.zero;

        // Esquinas longe do jogador, embaralhadas
        List<int> pontos = new List<int>();
        for (int i = 0; i < grafo.Quantidade; i++)
            if (Vector2.Distance(grafo.Posicao(i), longeDe) >= distanciaMinima && grafo.Vizinhos(i).Count > 0) pontos.Add(i);
        if (pontos.Count == 0)
            for (int i = 0; i < grafo.Quantidade; i++) if (grafo.Vizinhos(i).Count > 0) pontos.Add(i);
        for (int i = 0; i < pontos.Count; i++)
        {
            int j = Random.Range(i, pontos.Count);
            (pontos[i], pontos[j]) = (pontos[j], pontos[i]);
        }

        int total = quantidadeCarros + quantidadeMotos;
        for (int k = 0; k < total && pontos.Count > 0; k++)
        {
            bool moto = k >= quantidadeCarros;
            Sprite[] lista = moto ? motos : carros;
            if (lista == null || lista.Length == 0) continue;
            Sprite desenho = lista[k % lista.Length];
            CarroRebelde r = Criar(desenho, moto, k);
            r.Iniciar(grafo, pontos[k % pontos.Count]);
            soltos.Add(r);
        }
    }

    private CarroRebelde Criar(Sprite desenho, bool moto, int indice)
    {
        GameObject obj = new GameObject(moto ? "Rebelde (moto) " + indice : "Rebelde (carro pichado) " + indice);
        obj.transform.SetParent(transform, false);
        int camada = LayerMask.NameToLayer("veiculos");
        if (camada >= 0) obj.layer = camada;

        // Sombra = o próprio desenho em preto, um pouco deslocado
        SpriteRenderer sombra = new GameObject("Sombra", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        sombra.transform.SetParent(obj.transform, false);
        sombra.transform.localPosition = new Vector3(0.12f, -0.14f, 0f);
        sombra.sprite = desenho;
        sombra.color = new Color(0f, 0f, 0f, 0.35f);
        sombra.sortingOrder = ordemDesenho - 1;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = desenho;
        sr.sortingOrder = ordemDesenho;

        Rigidbody2D corpo = obj.AddComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Kinematic;
        corpo.gravityScale = 0f;

        BoxCollider2D caixa = obj.AddComponent<BoxCollider2D>();
        Vector2 tamanho = desenho.bounds.size;
        caixa.size = new Vector2(tamanho.x * (moto ? 0.7f : 0.82f), tamanho.y * 0.94f);

        CarroRebelde r = obj.AddComponent<CarroRebelde>();
        if (moto) r.Configurar(velocidadeMoto, 300f, 0.55f, 0.15f);
        else r.Configurar(velocidadeCarro, 200f, 0f, 0.07f);
        return r;
    }
}
