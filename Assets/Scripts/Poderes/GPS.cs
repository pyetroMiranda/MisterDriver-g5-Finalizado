using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// GPS: liga/desliga com G e desenha na pista a rota mais curta até a chegada.
// As ruas são descobertas pela rede do trânsito (RedeTrafego) e pelas rotas antigas dos carros,
// mais os PontoGPS que você quiser colocar para completar o mapa.
// Se não houver ruas conhecidas, mostra uma linha reta até a chegada.
[RequireComponent(typeof(CarroController))]
public class GPS : MonoBehaviour, IPoder
{
    [Header("Loja")]
    [SerializeField] private ItemLoja itemLoja;
    [SerializeField] private bool comprado = true;   // só vale se Item Loja estiver vazio (para testar)

    [Header("Rede de ruas")]
    [Tooltip("Pontos de rotas diferentes mais perto que isso viram um cruzamento.")]
    [SerializeField] private float distanciaCruzamento = 4f;

    [Header("Linha")]
    [SerializeField] private Color cor = new Color(0.18f, 0.89f, 0.9f, 0.85f);
    [SerializeField] private float largura = 0.45f;
    [SerializeField] private float intervaloRecalculo = 0.4f;

    private LineRenderer linha;
    private Transform destino;
    private readonly List<Vector2> nos = new List<Vector2>();
    private readonly List<List<int>> vizinhos = new List<List<int>>();
    private bool ligado;
    private float proximoCalculo;

    public string Nome => "GPS";
    public string Tecla => "G";
    public string Info => ligado ? "LIGADO" : "";
    public bool Comprado => itemLoja != null ? itemLoja.Comprado : comprado;
    public bool Ativo => ligado;
    public bool Pronto => Comprado;
    public float Progresso => 1f;

    private void Start()
    {
        ChegadaNivel chegada = FindFirstObjectByType<ChegadaNivel>();
        if (chegada != null) destino = chegada.transform;

        CriarLinha();
        MontarRede();
    }

    private void Update()
    {
        if (Time.timeScale > 0f && Apertou() && Comprado)
        {
            ligado = !ligado;
            if (ligado && destino == null) Debug.Log("GPS: este nível não tem chegada (ChegadaNivel).");
        }

        bool mostrar = ligado && destino != null;
        linha.enabled = mostrar;
        if (!mostrar) return;

        if (Time.unscaledTime >= proximoCalculo)
        {
            proximoCalculo = Time.unscaledTime + intervaloRecalculo;
            Desenhar(CalcularRota(transform.position, destino.position));
        }

        // Linha "respirando" para chamar atenção
        Color c = cor;
        c.a *= 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 4f);
        linha.startColor = linha.endColor = c;
    }

    // ---------- Rede de ruas ----------

    private void MontarRede()
    {
        nos.Clear();
        vizinhos.Clear();
        List<int> donoDoNo = new List<int>();   // a qual rota cada ponto pertence
        int rotaAtual = 0;

        // Rotas dos carros do trânsito (cada rota só uma vez)
        HashSet<Transform> vistas = new HashSet<Transform>();
        foreach (CarroTrafego carro in FindObjectsByType<CarroTrafego>(FindObjectsSortMode.None))
        {
            Transform rota = carro.Rota;
            if (rota == null || !vistas.Add(rota) || rota.childCount < 2) continue;

            int primeiro = nos.Count;
            for (int i = 0; i < rota.childCount; i++)
            {
                nos.Add(rota.GetChild(i).position);
                vizinhos.Add(new List<int>());
                donoDoNo.Add(rotaAtual);
                if (i > 0) Ligar(nos.Count - 2, nos.Count - 1);
            }
            Ligar(primeiro, nos.Count - 1);   // as rotas do trânsito dão a volta
            rotaAtual++;
        }

        // Redes de ruas do trânsito novo (RedeTrafego): pontos e ruas já vêm ligados
        foreach (RedeTrafego rede in FindObjectsByType<RedeTrafego>(FindObjectsSortMode.None))
        {
            GrafoTrafego grafo = rede.Grafo;
            int primeiro = nos.Count;
            for (int i = 0; i < grafo.Quantidade; i++)
            {
                nos.Add(grafo.Posicao(i));
                vizinhos.Add(new List<int>());
                donoDoNo.Add(rotaAtual);
            }
            for (int i = 0; i < grafo.Quantidade; i++)
                foreach (int j in grafo.Vizinhos(i)) Ligar(primeiro + i, primeiro + j);
            rotaAtual++;
        }

        // Pontos extras colocados à mão
        foreach (PontoGPS ponto in FindObjectsByType<PontoGPS>(FindObjectsSortMode.None))
        {
            nos.Add(ponto.transform.position);
            vizinhos.Add(new List<int>());
            donoDoNo.Add(-1);
        }

        // Cruzamentos: pontos de rotas diferentes (ou PontoGPS) perto um do outro
        for (int a = 0; a < nos.Count; a++)
        {
            for (int b = a + 1; b < nos.Count; b++)
            {
                bool mesmaRota = donoDoNo[a] >= 0 && donoDoNo[a] == donoDoNo[b];
                float limite = donoDoNo[a] < 0 || donoDoNo[b] < 0 ? PontoGPS.RaioLigacao : distanciaCruzamento;
                if (!mesmaRota && Vector2.Distance(nos[a], nos[b]) <= limite) Ligar(a, b);
            }
        }
    }

    private void Ligar(int a, int b)
    {
        if (a == b || vizinhos[a].Contains(b)) return;
        vizinhos[a].Add(b);
        vizinhos[b].Add(a);
    }

    // ---------- Caminho mais curto (Dijkstra) ----------

    private List<Vector2> CalcularRota(Vector2 inicio, Vector2 fim)
    {
        List<Vector2> caminho = new List<Vector2> { inicio };

        int origem = MaisProximo(inicio);
        int alvo = MaisProximo(fim);
        if (origem < 0 || alvo < 0)
        {
            caminho.Add(fim);   // sem ruas conhecidas: linha reta
            return caminho;
        }

        int n = nos.Count;
        float[] distancia = new float[n];
        int[] anterior = new int[n];
        bool[] fechado = new bool[n];
        for (int i = 0; i < n; i++) { distancia[i] = float.MaxValue; anterior[i] = -1; }
        distancia[origem] = 0f;

        for (int passo = 0; passo < n; passo++)
        {
            int atual = -1;
            for (int i = 0; i < n; i++)
                if (!fechado[i] && (atual < 0 || distancia[i] < distancia[atual])) atual = i;
            if (atual < 0 || distancia[atual] == float.MaxValue || atual == alvo) break;
            fechado[atual] = true;

            foreach (int viz in vizinhos[atual])
            {
                float d = distancia[atual] + Vector2.Distance(nos[atual], nos[viz]);
                if (d < distancia[viz]) { distancia[viz] = d; anterior[viz] = atual; }
            }
        }

        if (distancia[alvo] == float.MaxValue)
        {
            caminho.Add(fim);   // ruas não se conectam: linha reta
            return caminho;
        }

        List<Vector2> pontos = new List<Vector2>();
        for (int i = alvo; i >= 0; i = anterior[i]) pontos.Add(nos[i]);
        pontos.Reverse();

        caminho.AddRange(pontos);
        caminho.Add(fim);
        return caminho;
    }

    private int MaisProximo(Vector2 p)
    {
        int melhor = -1;
        float menor = float.MaxValue;
        for (int i = 0; i < nos.Count; i++)
        {
            float d = (nos[i] - p).sqrMagnitude;
            if (d < menor) { menor = d; melhor = i; }
        }
        return melhor;
    }

    // ---------- Desenho ----------

    private void CriarLinha()
    {
        GameObject obj = new GameObject("LinhaGPS");
        linha = obj.AddComponent<LineRenderer>();
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        linha.material = new Material(shader);
        linha.widthMultiplier = largura;
        linha.numCornerVertices = 4;
        linha.numCapVertices = 4;
        linha.sortingOrder = 6;   // acima da pista, abaixo do carro
        linha.useWorldSpace = true;
        linha.enabled = false;
    }

    private void Desenhar(List<Vector2> pontos)
    {
        linha.positionCount = pontos.Count;
        for (int i = 0; i < pontos.Count; i++) linha.SetPosition(i, new Vector3(pontos[i].x, pontos[i].y, 0f));
    }

    private static bool Apertou()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        return t != null && t.gKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.G);
#endif
    }
}
