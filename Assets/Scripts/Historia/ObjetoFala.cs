using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Móvel ou objeto da casa que mostra falas quando o Jason interage (cama, TV, geladeira...).
public class ObjetoFala : Interagivel
{
    [SerializeField] private List<Fala> falas = new List<Fala>();

    [Tooltip("Opcional: falas diferentes depois que o Jason já conversou com o pai.")]
    [SerializeField] private List<Fala> falasDepois = new List<Fala>();

    public void DefinirFalas(List<Fala> novas, List<Fala> depois = null)   // usado pela ferramenta do Editor
    {
        falas = novas;
        falasDepois = depois ?? new List<Fala>();
    }

    public override IEnumerator Interagir(JogadorCasa jogador)
    {
        RoteiroBase roteiro = RoteiroBase.Atual;
        bool depois = roteiro != null && roteiro.ConversouComPai && falasDepois != null && falasDepois.Count > 0;

        CaixaDialogo dialogo = CaixaDialogo.Instancia;
        if (dialogo != null) yield return dialogo.Conversar(depois ? falasDepois : falas);
    }
}
