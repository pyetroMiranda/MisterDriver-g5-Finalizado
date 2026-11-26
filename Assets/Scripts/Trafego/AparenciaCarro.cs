using System;
using UnityEngine;
using Random = UnityEngine.Random;

// Um modelo de carro do trânsito (montado pela ferramenta Mr Driver > Trânsito).
[Serializable]
public class ModeloCarro
{
    public string nome = "Carro";
    [Tooltip("Imagem da lataria. Se for 'Pintável', ela é cinza e recebe a cor sorteada.")]
    public Sprite lataria;
    [Tooltip("Vidros, faróis, pneus... (não recebem cor). Pode ficar vazio.")]
    public Sprite detalhes;
    public bool pintavel = true;
    [Tooltip("Cores possíveis deste modelo. Vazio = cores comuns de carro.")]
    public Color[] cores = new Color[0];
    [Tooltip("Chance relativa de aparecer (0 = nunca).")]
    [Min(0f)] public float chance = 1f;
    [Tooltip("Largura e comprimento do carro (colisão, luzes e sombra).")]
    public Vector2 tamanho = new Vector2(2.1f, 4.2f);
    [Tooltip("Multiplica a velocidade (ônibus é mais lento, esportivo mais rápido).")]
    public float fatorVelocidade = 1f;
}

// Sorteia o modelo e a cor de cada carro do trânsito e cuida das luzes de freio e das setas.
public class AparenciaCarro : MonoBehaviour
{
    [Header("Partes (montado pela ferramenta)")]
    [SerializeField] private SpriteRenderer lataria;
    [SerializeField] private SpriteRenderer detalhes;
    [SerializeField] private SpriteRenderer sombra;
    [SerializeField] private SpriteRenderer freioEsquerdo;
    [SerializeField] private SpriteRenderer freioDireito;
    [SerializeField] private SpriteRenderer setaFrenteEsquerda;
    [SerializeField] private SpriteRenderer setaFrenteDireita;
    [SerializeField] private SpriteRenderer setaTrasEsquerda;
    [SerializeField] private SpriteRenderer setaTrasDireita;
    [Tooltip("Tamanho da imagem da sombra em unidades (para ajustar a cada modelo).")]
    [SerializeField] private Vector2 tamanhoSombra = new Vector2(2.6f, 4.6f);

    [Header("Modelos")]
    [SerializeField] private ModeloCarro[] modelos = new ModeloCarro[0];
    [SerializeField] private bool sortearAoNascer = true;

    [Header("Setas")]
    [SerializeField] private float piscadasPorSegundo = 2.2f;

    // Cores comuns nas ruas (branco, prata, preto e cinza aparecem mais), mais algumas bem vivas
    private static readonly Color32[] CoresComuns =
    {
        new Color32(245, 245, 248, 255), new Color32(245, 245, 248, 255), new Color32(240, 240, 236, 255),
        new Color32(192, 196, 204, 255), new Color32(192, 196, 204, 255), new Color32(170, 175, 182, 255),
        new Color32(52, 54, 62, 255), new Color32(52, 54, 62, 255), new Color32(96, 100, 110, 255),
        new Color32(200, 36, 44, 255), new Color32(130, 24, 40, 255), new Color32(40, 90, 200, 255),
        new Color32(26, 46, 110, 255), new Color32(40, 120, 70, 255), new Color32(210, 190, 150, 255),
        new Color32(250, 200, 40, 255), new Color32(240, 120, 30, 255), new Color32(120, 200, 230, 255),
        new Color32(255, 30, 205, 255), new Color32(45, 226, 230, 255),
    };

    private CarroTrafego carro;
    private int modeloAtual = -1;

    public ModeloCarro Modelo => modeloAtual >= 0 && modeloAtual < modelos.Length ? modelos[modeloAtual] : null;

    private void Awake()
    {
        carro = GetComponent<CarroTrafego>();
        if (sortearAoNascer) Sortear();
        else Aplicar(modeloAtual);
        AtualizarLuzes(false, 0);
    }

    // Sorteia modelo e cor (ou usa o modelo pedido, se "indice" >= 0)
    public void Sortear(int indice = -1)
    {
        if (modelos == null || modelos.Length == 0) return;

        if (indice < 0)
        {
            float total = 0f;
            foreach (ModeloCarro m in modelos) total += m != null && m.lataria != null ? m.chance : 0f;
            float sorteio = Random.value * total;
            indice = 0;
            for (int i = 0; i < modelos.Length; i++)
            {
                if (modelos[i] == null || modelos[i].lataria == null || modelos[i].chance <= 0f) continue;
                indice = i;
                sorteio -= modelos[i].chance;
                if (sorteio <= 0f) break;
            }
        }

        Aplicar(Mathf.Clamp(indice, 0, modelos.Length - 1));
    }

    private void Aplicar(int indice)
    {
        if (indice < 0 || indice >= modelos.Length || modelos[indice] == null) return;
        modeloAtual = indice;
        ModeloCarro m = modelos[indice];

        if (lataria != null)
        {
            lataria.sprite = m.lataria;
            lataria.color = !m.pintavel ? Color.white
                          : m.cores != null && m.cores.Length > 0 ? m.cores[Random.Range(0, m.cores.Length)]
                          : (Color)CoresComuns[Random.Range(0, CoresComuns.Length)];
        }

        if (detalhes != null)
        {
            detalhes.sprite = m.detalhes;
            detalhes.enabled = m.detalhes != null;
        }

        Vector2 t = m.tamanho;
        BoxCollider2D caixa = GetComponent<BoxCollider2D>();
        if (caixa != null)
        {
            caixa.size = t * 0.96f;
            caixa.offset = Vector2.zero;
        }

        if (sombra != null)
            sombra.transform.localScale = new Vector3((t.x + 0.25f) / tamanhoSombra.x, (t.y + 0.25f) / tamanhoSombra.y, 1f);

        // Luzes nos cantos do carro
        float x = t.x * 0.5f, y = t.y * 0.5f;
        Posicionar(freioEsquerdo, -x + 0.32f, -y + 0.1f);
        Posicionar(freioDireito, x - 0.32f, -y + 0.1f);
        Posicionar(setaFrenteEsquerda, -x + 0.12f, y - 0.2f);
        Posicionar(setaFrenteDireita, x - 0.12f, y - 0.2f);
        Posicionar(setaTrasEsquerda, -x + 0.12f, -y + 0.22f);
        Posicionar(setaTrasDireita, x - 0.12f, -y + 0.22f);

        if (carro == null) carro = GetComponent<CarroTrafego>();
        if (carro != null) carro.FatorModelo = m.fatorVelocidade;
    }

    private static void Posicionar(SpriteRenderer luz, float x, float y)
    {
        if (luz != null) luz.transform.localPosition = new Vector3(x, y, 0f);
    }

    // ---------- Luzes ----------

    private void Update()
    {
        if (carro == null) return;
        AtualizarLuzes(carro.Freando, carro.Seta);
    }

    private void AtualizarLuzes(bool freando, int seta)
    {
        Ligar(freioEsquerdo, freando);
        Ligar(freioDireito, freando);

        bool aceso = Mathf.Repeat(Time.time * piscadasPorSegundo, 1f) < 0.5f;
        Ligar(setaFrenteEsquerda, aceso && seta < 0);
        Ligar(setaTrasEsquerda, aceso && seta < 0);
        Ligar(setaFrenteDireita, aceso && seta > 0);
        Ligar(setaTrasDireita, aceso && seta > 0);
    }

    private static void Ligar(SpriteRenderer luz, bool ligado)
    {
        if (luz != null && luz.enabled != ligado) luz.enabled = ligado;
    }
}
