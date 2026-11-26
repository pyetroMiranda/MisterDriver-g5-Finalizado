using System.Collections.Generic;
using UnityEngine;

// Rede de ruas do trânsito: os filhos com PontoTrafego são os pontos (no meio da rua) e as ligações são as ruas.
// Os carros andam na faixa da DIREITA, fazem a curva dentro dos cruzamentos e escolhem um caminho diferente a cada esquina.
// Na aba Scene: linha cinza = meio da rua, setas ciano = faixas, amarelo = cruzamento, laranja = fim de rua (retorno).
// Crie com: Mr Driver > Trânsito > Gerar ruas automaticamente pelo mapa.
public class RedeTrafego : MonoBehaviour
{
    [Tooltip("Distância do meio da rua até o meio de cada faixa (rua de 11 de largura = 2,75).")]
    [SerializeField, Min(0f)] private float faixa = 2.75f;

    [Tooltip("Metade da largura dos cruzamentos: o carro começa a curva a essa distância do ponto.")]
    [SerializeField, Min(0f)] private float raioCruzamento = 5.5f;

    [SerializeField] private bool mostrarFaixas = true;

    private GrafoTrafego grafo;
    private readonly List<PontoTrafego> pontos = new List<PontoTrafego>();

    public float Faixa => faixa;
    public float RaioCruzamento => raioCruzamento;
    public IReadOnlyList<PontoTrafego> Pontos
    {
        get
        {
            if (grafo == null) Montar();
            return pontos;
        }
    }

    public GrafoTrafego Grafo
    {
        get
        {
            if (grafo == null) Montar();
            return grafo;
        }
    }

    private void Awake()
    {
        Montar();
    }

    public void Montar()
    {
        grafo = Construir(pontos);
        grafo.Finalizar();
    }

    private GrafoTrafego Construir(List<PontoTrafego> lista)
    {
        lista.Clear();
        foreach (Transform filho in transform)
        {
            PontoTrafego p = filho.GetComponent<PontoTrafego>();
            if (p != null && p.gameObject.activeInHierarchy) lista.Add(p);
        }

        GrafoTrafego g = new GrafoTrafego(faixa);
        foreach (PontoTrafego p in lista)
            g.Adicionar(p.transform.position, p.raio > 0f ? p.raio : raioCruzamento);

        for (int i = 0; i < lista.Count; i++)
        {
            foreach (PontoTrafego outro in lista[i].ligacoes)
            {
                int j = outro != null ? lista.IndexOf(outro) : -1;
                if (j >= 0) g.Ligar(i, j);
            }
        }
        return g;
    }

    // Índice do ponto na rede (-1 se não for desta rede)
    public int IndiceDe(PontoTrafego ponto)
    {
        if (grafo == null) Montar();
        return pontos.IndexOf(ponto);
    }

    // ---------- Desenho na aba Scene ----------

    private void OnDrawGizmos()
    {
        List<PontoTrafego> lista = new List<PontoTrafego>();
        GrafoTrafego g = Construir(lista);

        for (int a = 0; a < g.Quantidade; a++)
        {
            int grau = g.Vizinhos(a).Count;
            Gizmos.color = grau >= 3 ? new Color(1f, 0.85f, 0.1f) : grau == 1 ? new Color(1f, 0.45f, 0.1f)
                         : grau == 0 ? Color.red : new Color(0.2f, 0.9f, 1f);
            Gizmos.DrawSphere(g.Posicao(a), 0.6f);

            foreach (int b in g.Vizinhos(a))
            {
                if (b > a)
                {
                    Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                    Gizmos.DrawLine(g.Posicao(a), g.Posicao(b));
                }

                if (!mostrarFaixas || !g.TrechoDaFaixa(a, b, out Vector2 inicio, out Vector2 fim)) continue;
                Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
                Gizmos.DrawLine(inicio, fim);
                Seta(inicio, fim);
            }
        }
    }

    private static void Seta(Vector2 inicio, Vector2 fim)
    {
        Vector2 d = (fim - inicio).normalized;
        Vector2 lado = GrafoTrafego.Direita(d);
        Vector2 meio = Vector2.Lerp(inicio, fim, 0.5f);
        Gizmos.DrawLine(meio, meio - d * 0.9f + lado * 0.6f);
        Gizmos.DrawLine(meio, meio - d * 0.9f - lado * 0.6f);
    }
}
