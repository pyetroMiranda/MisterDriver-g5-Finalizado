using UnityEngine;

// Sorteia o visual de cada pedestre: pele, cabelo (estilo e cor), camisa, mangas, sapato, mochila e boné.
// As imagens são cinza e recebem a cor aqui, então cada pessoa sai diferente.
public class AparenciaPedestre : MonoBehaviour
{
    [Header("Partes (montado pela ferramenta)")]
    [SerializeField] private SpriteRenderer corpo;
    [SerializeField] private SpriteRenderer cabeca;
    [SerializeField] private SpriteRenderer cabelo;
    [SerializeField] private SpriteRenderer mochila;
    [SerializeField] private SpriteRenderer bracoEsquerdo;
    [SerializeField] private SpriteRenderer bracoDireito;
    [SerializeField] private SpriteRenderer peEsquerdo;
    [SerializeField] private SpriteRenderer peDireito;

    [Header("Estilos")]
    [Tooltip("Estilos de cabelo. Um item vazio = careca.")]
    [SerializeField] private Sprite[] estilosCabelo;
    [SerializeField] private Sprite bone;

    [Header("Chances (0 a 1)")]
    [SerializeField, Range(0f, 1f)] private float chanceBone = 0.15f;
    [SerializeField, Range(0f, 1f)] private float chanceMochila = 0.35f;
    [SerializeField, Range(0f, 1f)] private float chanceMangaLonga = 0.4f;
    [SerializeField, Range(0f, 1f)] private float chanceCabeloNeon = 0.1f;

    [Header("Ao nascer")]
    [SerializeField] private bool sortearAoNascer = true;

    private static readonly Color32[] Peles =
    {
        new Color32(255, 224, 196, 255), new Color32(241, 194, 150, 255), new Color32(224, 172, 105, 255),
        new Color32(198, 134, 66, 255), new Color32(141, 85, 36, 255), new Color32(92, 56, 32, 255),
    };

    private static readonly Color32[] CabelosNaturais =
    {
        new Color32(28, 22, 22, 255), new Color32(62, 40, 26, 255), new Color32(112, 72, 40, 255),
        new Color32(200, 160, 80, 255), new Color32(236, 212, 140, 255), new Color32(176, 72, 32, 255),
        new Color32(160, 158, 165, 255),
    };

    private static readonly Color32[] CabelosNeon =
    {
        new Color32(255, 60, 200, 255), new Color32(60, 200, 255, 255), new Color32(150, 90, 255, 255),
    };

    private static readonly Color32[] Roupas =
    {
        new Color32(255, 30, 205, 255), new Color32(45, 226, 230, 255), new Color32(255, 200, 40, 255),
        new Color32(230, 60, 60, 255), new Color32(60, 180, 90, 255), new Color32(240, 240, 240, 255),
        new Color32(40, 50, 90, 255), new Color32(255, 140, 40, 255), new Color32(140, 80, 220, 255),
        new Color32(35, 35, 40, 255), new Color32(120, 200, 255, 255), new Color32(170, 110, 70, 255),
    };

    private static readonly Color32[] Sapatos =
    {
        new Color32(30, 30, 35, 255), new Color32(240, 240, 240, 255), new Color32(120, 70, 40, 255),
        new Color32(200, 40, 40, 255), new Color32(60, 90, 200, 255),
    };

    private void Awake()
    {
        if (sortearAoNascer) Sortear();
    }

    public void Sortear()
    {
        Color pele = Escolher(Peles);
        Color camisa = Escolher(Roupas);
        Color sapato = Escolher(Sapatos);

        Pintar(cabeca, pele);
        Pintar(corpo, camisa);

        // Mangas longas: braços da cor da camisa; curtas: braços da cor da pele
        Color braco = Random.value < chanceMangaLonga ? camisa : pele;
        Pintar(bracoEsquerdo, braco);
        Pintar(bracoDireito, braco);

        Pintar(peEsquerdo, sapato);
        Pintar(peDireito, sapato);

        // Cabelo ou boné
        if (cabelo != null)
        {
            if (bone != null && Random.value < chanceBone)
            {
                cabelo.sprite = bone;
                cabelo.color = Escolher(Roupas);
            }
            else
            {
                cabelo.sprite = estilosCabelo != null && estilosCabelo.Length > 0
                    ? estilosCabelo[Random.Range(0, estilosCabelo.Length)]
                    : null;
                cabelo.color = Random.value < chanceCabeloNeon ? Escolher(CabelosNeon) : Escolher(CabelosNaturais);
            }
            cabelo.enabled = cabelo.sprite != null;
        }

        // Mochila de outra cor
        if (mochila != null)
        {
            bool temMochila = Random.value < chanceMochila;
            mochila.enabled = temMochila;
            if (temMochila)
            {
                Color cor = Escolher(Roupas);
                for (int i = 0; i < 5 && cor == camisa; i++) cor = Escolher(Roupas);
                mochila.color = cor;
            }
        }
    }

    private static Color Escolher(Color32[] lista) => lista[Random.Range(0, lista.Length)];

    private static void Pintar(SpriteRenderer parte, Color cor)
    {
        if (parte != null) parte.color = cor;
    }
}
