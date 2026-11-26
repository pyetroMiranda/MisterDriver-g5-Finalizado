using System.Collections.Generic;
using UnityEngine;

// Um ponto da rede de ruas (filho de uma RedeTrafego): fica no MEIO da rua, num cruzamento, curva ou fim de rua.
// As ligações dizem para quais pontos dá para ir daqui (sempre nos dois sentidos).
public class PontoTrafego : MonoBehaviour
{
    [Tooltip("Pontos ligados a este por uma rua (mão dupla).\n" +
             "Para ligar/desligar: selecione 2 pontos e use Mr Driver > Trânsito > Ligar ou desligar os 2 pontos selecionados.")]
    public List<PontoTrafego> ligacoes = new List<PontoTrafego>();

    [Tooltip("Metade da largura do cruzamento (onde o carro começa a curva). 0 = usa o valor da rede.")]
    [Min(0f)] public float raio;

    private void OnDrawGizmosSelected()
    {
        RedeTrafego rede = GetComponentInParent<RedeTrafego>();
        float r = raio > 0f ? raio : rede != null ? rede.RaioCruzamento : 5f;
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.6f);
        Gizmos.DrawWireCube(transform.position, new Vector3(r * 2f, r * 2f, 0f));
    }
}
