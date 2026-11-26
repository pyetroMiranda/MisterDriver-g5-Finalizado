using UnityEngine;

// Desenha o carro do jogador com o modelo e a cor escolhidos na garagem.
// Fica no prefab CarroNissan_0 (instalado por Mr Driver > História > 6. Carros da garagem).
// O desenho original (o corpo do prefab) é trocado por duas camadas: a lataria (pintada) e os detalhes.
public class VisualCarroJogador : MonoBehaviour
{
    [SerializeField] private CatalogoCarros catalogo;

    [Header("Partes (montadas pela ferramenta)")]
    [SerializeField] private SpriteRenderer corpoOriginal;
    [SerializeField] private SpriteRenderer lataria;
    [SerializeField] private SpriteRenderer detalhes;
    [Tooltip("Rodas separadas do desenho original (somem nos outros modelos, que já têm rodas no desenho).")]
    [SerializeField] private SpriteRenderer[] rodas = new SpriteRenderer[0];

    [Tooltip("Tamanho do desenho original (largura x comprimento, sem a escala do carro). Os outros modelos cabem nele.")]
    [SerializeField] private Vector2 tamanhoBase = new Vector2(2.5f, 4.17f);

    private Sprite spriteOriginal;

    private void Awake()
    {
        if (corpoOriginal != null) spriteOriginal = corpoOriginal.sprite;
        Aplicar();
    }

    private void OnEnable() => GaragemJogador.AoMudar += Aplicar;
    private void OnDisable() => GaragemJogador.AoMudar -= Aplicar;

    public void Aplicar()
    {
        CatalogoCarros.Modelo m = catalogo != null ? catalogo.ModeloDe(GaragemJogador.ModeloId) : null;

        if (m == null || m.lataria == null || lataria == null)
        {
            // Sem catálogo: fica o desenho original
            if (corpoOriginal != null && spriteOriginal != null) corpoOriginal.sprite = spriteOriginal;
            if (lataria != null) lataria.sprite = null;
            if (detalhes != null) detalhes.sprite = null;
            MostrarRodas(true);
            return;
        }

        // Tira só a imagem do corpo (o PiscarAoDano liga e desliga os SpriteRenderers, então não dá para desligar este)
        if (corpoOriginal != null) corpoOriginal.sprite = null;

        lataria.sprite = m.lataria;
        lataria.color = catalogo.CorOu(GaragemJogador.CorIndice);
        if (detalhes != null)
        {
            detalhes.sprite = m.detalhes;
            detalhes.color = Color.white;
        }

        // O modelo cabe dentro do tamanho do desenho original, sem distorcer
        Vector2 t = m.lataria.bounds.size;
        float s = Mathf.Min(tamanhoBase.x / Mathf.Max(0.01f, t.x), tamanhoBase.y / Mathf.Max(0.01f, t.y));
        lataria.transform.localScale = new Vector3(s, s, 1f);
        if (detalhes != null) detalhes.transform.localScale = lataria.transform.localScale;

        MostrarRodas(m.mostrarRodas);
    }

    private void MostrarRodas(bool mostrar)
    {
        foreach (SpriteRenderer r in rodas)
        {
            if (r == null) continue;
            Color c = r.color;
            c.a = mostrar ? 1f : 0f;
            r.color = c;
        }
    }
}
