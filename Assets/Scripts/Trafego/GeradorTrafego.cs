using System.Collections.Generic;
using UnityEngine;

// Coloca carros do trânsito espalhados pelas faixas da rede quando o nível começa.
// Cada carro sai com modelo, cor e "personalidade" sorteados.
// Durante o jogo, mantém a cidade movimentada perto do jogador: carros que ficaram muito longe
// ou travados por muito tempo (fora da tela) "reaparecem" numa rua em volta dele, sempre fora da vista.
[RequireComponent(typeof(RedeTrafego))]
public class GeradorTrafego : MonoBehaviour
{
    [SerializeField] private CarroTrafego prefab;
    [SerializeField, Min(0)] private int quantidade = 12;
    [Tooltip("Distância mínima entre os carros ao nascer.")]
    [SerializeField] private float distanciaEntreCarros = 10f;
    [Tooltip("Nenhum carro nasce mais perto do jogador que isso.")]
    [SerializeField] private float distanciaDoJogador = 16f;

    [Header("Movimento perto do jogador")]
    [Tooltip("Traz de volta (fora da tela) os carros que ficaram longe ou travados.")]
    [SerializeField] private bool reciclar = true;
    [Tooltip("Carro mais longe do jogador que isso é levado para perto dele.")]
    [SerializeField] private float raioAtivo = 55f;
    [Tooltip("Os carros reaparecem pelo menos a essa distância do jogador (fora da tela).")]
    [SerializeField] private float distanciaMinimaReaparecer = 24f;
    [Tooltip("Carro parado há mais que isso (segundos), fora da tela, é levado para outra rua.")]
    [SerializeField] private float tempoMaximoParado = 12f;
    [Tooltip("De quanto em quanto tempo (segundos) verifica os carros.")]
    [SerializeField] private float intervaloReciclagem = 0.5f;
    [Tooltip("Quantos carros, no máximo, mudam de lugar em cada verificação.")]
    [SerializeField, Min(1)] private int reciclarPorVez = 2;

    private static Transform pasta;   // um objeto só para guardar todos os carros gerados

    private struct Trecho
    {
        public int de, para;
        public Vector2 inicio, fim;
        public float comprimento;
    }

    private RedeTrafego rede;
    private readonly List<Trecho> trechos = new List<Trecho>();
    private float total;
    private readonly List<CarroTrafego> carros = new List<CarroTrafego>();
    private CarroController jogador;
    private float proximaReciclagem;

    private void Start()
    {
        rede = GetComponent<RedeTrafego>();
        GrafoTrafego grafo = rede.Grafo;

        if (prefab == null || grafo.Quantidade < 2)
        {
            Debug.LogWarning("GeradorTrafego: falta o prefab ou a rede tem menos de 2 pontos.", this);
            return;
        }

        // Todas as faixas retas (cada rua tem uma para cada sentido)
        for (int a = 0; a < grafo.Quantidade; a++)
        {
            foreach (int b in grafo.Vizinhos(a))
            {
                if (!grafo.TrechoDaFaixa(a, b, out Vector2 inicio, out Vector2 fim)) continue;
                float comprimento = Vector2.Distance(inicio, fim);
                if (comprimento < 3f) continue;
                trechos.Add(new Trecho { de = a, para = b, inicio = inicio, fim = fim, comprimento = comprimento });
                total += comprimento;
            }
        }

        if (trechos.Count == 0)
        {
            Debug.LogWarning("GeradorTrafego: a rede não tem ruas ligadas.", this);
            return;
        }

        if (pasta == null) pasta = new GameObject("Trânsito (gerado)").transform;

        jogador = FindFirstObjectByType<CarroController>();
        Vector2 posJogador = jogador != null ? (Vector2)jogador.transform.position : Vector2.zero;

        for (int tentativa = 0; tentativa < quantidade * 40 && carros.Count < quantidade; tentativa++)
        {
            Trecho t = SortearTrecho();
            Vector2 posicao = LugarNoTrecho(t);

            if (jogador != null && Vector2.Distance(posicao, posJogador) < distanciaDoJogador) continue;
            if (TemCarroPerto(posicao, distanciaEntreCarros, null)) continue;

            Vector2 d = (t.fim - t.inicio).normalized;
            Quaternion rotacao = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
            CarroTrafego carro = Instantiate(prefab, posicao, rotacao, pasta);
            carro.name = "Carro " + (carros.Count + 1);
            carro.Iniciar(rede, t.de, t.para, posicao);
            carros.Add(carro);
        }

        if (carros.Count < quantidade)
            Debug.Log("GeradorTrafego: só coube " + carros.Count + " de " + quantidade + " carros nas ruas.", this);

        proximaReciclagem = Time.time + 1f;
    }

    // ---------- Reciclagem ----------

    private void Update()
    {
        if (!reciclar || carros.Count == 0 || Time.time < proximaReciclagem) return;
        proximaReciclagem = Time.time + intervaloReciclagem;

        if (jogador == null || !jogador.isActiveAndEnabled)
        {
            jogador = FindFirstObjectByType<CarroController>();
            if (jogador == null) return;
        }

        Vector2 posJogador = jogador.transform.position;
        float longe2 = raioAtivo * raioAtivo;
        float foraDaTela2 = distanciaMinimaReaparecer * distanciaMinimaReaparecer;
        int movidos = 0;

        // Começa de um carro diferente a cada vez, para não favorecer os primeiros da lista
        int comeco = Random.Range(0, carros.Count);
        for (int k = 0; k < carros.Count && movidos < reciclarPorVez; k++)
        {
            CarroTrafego carro = carros[(comeco + k) % carros.Count];
            if (carro == null || !carro.isActiveAndEnabled) continue;

            float d2 = ((Vector2)carro.transform.position - posJogador).sqrMagnitude;
            bool longe = d2 > longe2;
            bool travado = carro.TempoParado > tempoMaximoParado && d2 > foraDaTela2 && !NaTela(carro.transform.position);
            if (!longe && !travado) continue;

            if (Reposicionar(carro, posJogador)) movidos++;
        }

        carros.RemoveAll(c => c == null);
    }

    // Leva o carro para uma faixa em volta do jogador, fora da tela e longe dos outros carros
    private bool Reposicionar(CarroTrafego carro, Vector2 posJogador)
    {
        float minimo = distanciaMinimaReaparecer;
        float maximo = Mathf.Max(minimo + 5f, raioAtivo - 5f);

        for (int tentativa = 0; tentativa < 30; tentativa++)
        {
            Trecho t = SortearTrechoPerto(posJogador, maximo);
            if (t.comprimento <= 0f) return false;

            Vector2 posicao = LugarNoTrecho(t);
            float d = Vector2.Distance(posicao, posJogador);
            if (d < minimo || d > maximo || NaTela(posicao)) continue;
            if (TemCarroPerto(posicao, distanciaEntreCarros, carro)) continue;

            carro.Iniciar(rede, t.de, t.para, posicao);
            return true;
        }
        return false;
    }

    // ---------- Ajudantes ----------

    private Trecho SortearTrecho()
    {
        float sorteio = Random.value * total;
        foreach (Trecho x in trechos)
        {
            sorteio -= x.comprimento;
            if (sorteio <= 0f) return x;
        }
        return trechos[trechos.Count - 1];
    }

    // Sorteia (pelo comprimento) entre os trechos que passam a menos de "raio" do ponto
    private readonly List<Trecho> pertos = new List<Trecho>();
    private Trecho SortearTrechoPerto(Vector2 ponto, float raio)
    {
        pertos.Clear();
        float soma = 0f;
        foreach (Trecho x in trechos)
        {
            if (DistanciaAoTrecho(ponto, x.inicio, x.fim) > raio) continue;
            pertos.Add(x);
            soma += x.comprimento;
        }
        if (pertos.Count == 0) return default;

        float sorteio = Random.value * soma;
        foreach (Trecho x in pertos)
        {
            sorteio -= x.comprimento;
            if (sorteio <= 0f) return x;
        }
        return pertos[pertos.Count - 1];
    }

    private static Vector2 LugarNoTrecho(Trecho t)
    {
        float margem = Mathf.Min(2.5f, t.comprimento * 0.3f) / t.comprimento;
        return Vector2.Lerp(t.inicio, t.fim, Random.Range(margem, 1f - margem));
    }

    private bool TemCarroPerto(Vector2 posicao, float distancia, CarroTrafego ignorar)
    {
        float d2 = distancia * distancia;
        foreach (CarroTrafego c in carros)
            if (c != null && c != ignorar && ((Vector2)c.transform.position - posicao).sqrMagnitude < d2) return true;
        if (jogador != null && ((Vector2)jogador.transform.position - posicao).sqrMagnitude < d2) return true;
        return false;
    }

    // O ponto aparece na câmera? (com folga, para o carro nunca "surgir" na frente do jogador)
    private static bool NaTela(Vector2 ponto)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector3 v = cam.WorldToViewportPoint(ponto);
        return v.x > -0.3f && v.x < 1.3f && v.y > -0.3f && v.y < 1.3f;
    }

    private static float DistanciaAoTrecho(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }
}
