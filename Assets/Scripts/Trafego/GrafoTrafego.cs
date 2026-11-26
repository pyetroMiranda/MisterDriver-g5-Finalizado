using System.Collections.Generic;
using UnityEngine;

// Mapa das ruas usado pelos carros do trânsito: pontos (cruzamentos, curvas e fins de rua) e as ligações entre eles.
// É montado pela RedeTrafego (ou pelas rotas antigas). Também guarda os cruzamentos, que controlam a vez de cada carro.
// Mão dupla: cada ligação tem uma faixa para cada sentido, sempre do lado DIREITO de quem anda.
public class GrafoTrafego
{
    private readonly List<Vector2> posicoes = new List<Vector2>();
    private readonly List<List<int>> vizinhos = new List<List<int>>();
    private readonly List<float> raios = new List<float>();
    private Cruzamento[] cruzamentos;

    public float Faixa { get; }   // distância do meio da rua até o meio da faixa

    public GrafoTrafego(float faixa)
    {
        Faixa = faixa;
    }

    public int Quantidade => posicoes.Count;
    public Vector2 Posicao(int ponto) => posicoes[ponto];
    public List<int> Vizinhos(int ponto) => vizinhos[ponto];
    public float Raio(int ponto) => raios[ponto];

    public int Adicionar(Vector2 posicao, float raio)
    {
        posicoes.Add(posicao);
        vizinhos.Add(new List<int>());
        raios.Add(Mathf.Max(0f, raio));
        return posicoes.Count - 1;
    }

    public void Ligar(int a, int b)
    {
        if (a == b || a < 0 || b < 0 || vizinhos[a].Contains(b)) return;
        vizinhos[a].Add(b);
        vizinhos[b].Add(a);
    }

    // Lado direito de quem anda na direção "d"
    public static Vector2 Direita(Vector2 d) => new Vector2(d.y, -d.x);

    // Trecho reto da faixa que vai de "a" até "b" (entre a saída do cruzamento "a" e a entrada do cruzamento "b")
    public bool TrechoDaFaixa(int a, int b, out Vector2 inicio, out Vector2 fim)
    {
        Vector2 pa = posicoes[a], pb = posicoes[b];
        Vector2 ab = pb - pa;
        float comprimento = ab.magnitude;
        if (comprimento < 0.01f)
        {
            inicio = fim = pa;
            return false;
        }

        Vector2 d = ab / comprimento;
        Vector2 lado = Direita(d) * Faixa;
        inicio = pa + d * Mathf.Min(raios[a], comprimento * 0.48f) + lado;
        fim = pb - d * Mathf.Min(raios[b], comprimento * 0.48f) + lado;
        return Vector2.Dot(fim - inicio, d) > 0.05f;
    }

    // ---------- Cruzamentos ----------

    // Pontos ligados por uma rua curta demais para caber um carro parado viram um cruzamento só.
    // Só precisa de "vez" onde se encontram 3 ruas ou mais.
    public void Finalizar(float folgaEntreCruzamentos = 6f)
    {
        int n = posicoes.Count;
        int[] grupo = new int[n];
        for (int i = 0; i < n; i++) grupo[i] = i;

        for (int a = 0; a < n; a++)
        {
            foreach (int b in vizinhos[a])
            {
                if (b <= a || vizinhos[a].Count < 2 || vizinhos[b].Count < 2) continue;
                float distancia = Vector2.Distance(posicoes[a], posicoes[b]);
                if (distancia < raios[a] + raios[b] + folgaEntreCruzamentos) Unir(grupo, a, b);
            }
        }

        Dictionary<int, Cruzamento> porGrupo = new Dictionary<int, Cruzamento>();
        Dictionary<int, int> tamanho = new Dictionary<int, int>();
        Dictionary<int, bool> temEncontro = new Dictionary<int, bool>();
        for (int i = 0; i < n; i++)
        {
            int g = Raiz(grupo, i);
            tamanho[g] = tamanho.TryGetValue(g, out int t) ? t + 1 : 1;
            temEncontro[g] = (temEncontro.TryGetValue(g, out bool e) && e) || vizinhos[i].Count >= 3;
        }

        cruzamentos = new Cruzamento[n];
        for (int i = 0; i < n; i++)
        {
            int g = Raiz(grupo, i);
            if (!temEncontro[g]) continue;
            if (!porGrupo.TryGetValue(g, out Cruzamento c))
            {
                c = new Cruzamento(tamanho[g] == 1);
                porGrupo[g] = c;
            }
            cruzamentos[i] = c;
        }
    }

    public Cruzamento CruzamentoDe(int ponto)
    {
        if (cruzamentos == null || ponto < 0 || ponto >= cruzamentos.Length) return null;
        return cruzamentos[ponto];
    }

    private static int Raiz(int[] grupo, int i)
    {
        while (grupo[i] != i) i = grupo[i] = grupo[grupo[i]];
        return i;
    }

    private static void Unir(int[] grupo, int a, int b)
    {
        grupo[Raiz(grupo, a)] = Raiz(grupo, b);
    }
}

// Controla a vez num cruzamento: quem está dentro, quem está esperando e quem pode entrar junto.
public class Cruzamento
{
    public const int Reto = 0, Esquerda = -1, Direita = 1, Retorno = 2;

    private class Vez
    {
        public CarroTrafego carro;
        public Vector2 entrada;   // direção em que o carro entra
        public Vector2 saida;     // direção em que o carro sai
        public int conversao;
        public float desde;
        public float ultimoPedido;
    }

    private readonly List<Vez> dentro = new List<Vez>();
    private readonly List<Vez> fila = new List<Vez>();
    private readonly bool simples;   // um ponto só: dá para os dois sentidos opostos passarem juntos

    public Cruzamento(bool simples)
    {
        this.simples = simples;
    }

    public bool Esta(CarroTrafego carro) => dentro.Exists(v => v.carro == carro);

    // Pede para entrar. Devolve true se pode (e já marca o carro como "dentro").
    public bool PedirEntrada(CarroTrafego carro, Vector2 entrada, Vector2 saida, int conversao, bool forcar)
    {
        Limpar();
        if (Esta(carro)) return true;

        Vez eu = fila.Find(v => v.carro == carro);
        if (eu == null)
        {
            eu = new Vez { carro = carro, desde = Time.time };
            fila.Add(eu);
        }
        eu.entrada = entrada;
        eu.saida = saida;
        eu.conversao = conversao;
        eu.ultimoPedido = Time.time;

        if (!forcar && !PodeEntrar(eu)) return false;

        fila.Remove(eu);
        dentro.Add(eu);
        return true;
    }

    public void Desistir(CarroTrafego carro)
    {
        fila.RemoveAll(v => v.carro == carro);
    }

    public void Sair(CarroTrafego carro)
    {
        dentro.RemoveAll(v => v.carro == carro);
        fila.RemoveAll(v => v.carro == carro);
    }

    private bool PodeEntrar(Vez eu)
    {
        // Quem está esperando há mais tempo e cruzaria o meu caminho tem a vez
        // (quem não cruza, como quem vem de frente seguindo reto, pode passar junto)
        foreach (Vez outro in fila)
        {
            if (outro == eu || outro.desde >= eu.desde) continue;
            if (Time.time - outro.desde > 1.5f && !Combina(outro, eu)) return false;
        }

        foreach (Vez outro in dentro)
            if (!Combina(outro, eu)) return false;

        return true;
    }

    // Os dois caminhos podem estar no cruzamento ao mesmo tempo sem se cruzar?
    private bool Combina(Vez a, Vez b)
    {
        if (MesmaDirecao(a.entrada, b.entrada)) return true;   // um atrás do outro
        if (MesmaDirecao(a.saida, b.saida)) return false;      // os dois querem entrar na mesma faixa

        bool esquerdaA = a.conversao == Esquerda || a.conversao == Retorno;
        bool esquerdaB = b.conversao == Esquerda || b.conversao == Retorno;

        // Sentidos opostos, ninguém virando à esquerda: as faixas não se cruzam
        bool opostos = Vector2.Dot(a.entrada, b.entrada) < -0.85f;
        if (opostos && !esquerdaA && !esquerdaB) return true;

        // Conversão à direita fica colada na esquina: só atrapalha quem vai para a mesma faixa (já visto acima)
        if (simples && (a.conversao == Direita || b.conversao == Direita)) return true;

        return false;
    }

    private static bool MesmaDirecao(Vector2 a, Vector2 b) => Vector2.Dot(a, b) > 0.85f;

    // Tira quem foi destruído ou parou de pedir
    private void Limpar()
    {
        dentro.RemoveAll(v => v.carro == null || !v.carro.isActiveAndEnabled);
        fila.RemoveAll(v => v.carro == null || !v.carro.isActiveAndEnabled || Time.time - v.ultimoPedido > 0.5f);
    }
}
