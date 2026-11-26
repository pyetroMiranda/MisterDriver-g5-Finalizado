using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// O carro parado na garagem da casa. Mostra o modelo e a cor escolhidos (muda na hora quando o jogador troca).
// Coberto com a capa (capítulo 1): só mostra uma fala. Descoberto (capítulo 2): abre a tela da garagem pelo roteiro.
public class VitrineCarro : Interagivel
{
    [SerializeField] private CatalogoCarros catalogo;
    [SerializeField] private SpriteRenderer lataria;
    [SerializeField] private SpriteRenderer detalhes;
    [SerializeField] private SpriteRenderer capa;
    [SerializeField] private bool coberta;
    [SerializeField] private List<Fala> falasCoberta = new List<Fala>();

    public CatalogoCarros Catalogo => catalogo;

    // Usado pela ferramenta do Editor
    public void Definir(CatalogoCarros novo, SpriteRenderer srLataria, SpriteRenderer srDetalhes, SpriteRenderer srCapa,
                        bool cobrir, List<Fala> falas)
    {
        catalogo = novo;
        lataria = srLataria;
        detalhes = srDetalhes;
        capa = srCapa;
        coberta = cobrir;
        falasCoberta = falas ?? new List<Fala>();
    }

    private void Start() => Atualizar();

    protected override void OnEnable()
    {
        base.OnEnable();
        GaragemJogador.AoMudar += Atualizar;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GaragemJogador.AoMudar -= Atualizar;
    }

    public void Atualizar()
    {
        if (capa != null) capa.enabled = coberta;

        CatalogoCarros.Modelo m = catalogo != null ? catalogo.ModeloDe(GaragemJogador.ModeloId) : null;
        if (lataria != null)
        {
            lataria.sprite = m != null ? m.latariaCasa : null;
            lataria.color = catalogo != null ? catalogo.CorOu(GaragemJogador.CorIndice) : Color.white;
            lataria.enabled = !coberta;
        }
        if (detalhes != null)
        {
            detalhes.sprite = m != null ? m.detalhesCasa : null;
            detalhes.enabled = !coberta;
        }
    }

    public override IEnumerator Interagir(JogadorCasa jogador)
    {
        if (coberta)
        {
            if (CaixaDialogo.Instancia != null) yield return CaixaDialogo.Instancia.Conversar(falasCoberta);
            yield break;
        }

        RoteiroBase roteiro = RoteiroBase.Atual;
        if (roteiro != null) yield return roteiro.UsarGaragem(this);
    }
}
