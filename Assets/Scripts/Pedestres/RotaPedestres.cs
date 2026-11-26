using System.Collections.Generic;
using UnityEngine;

// Caminho dos pedestres: os filhos com PontoPedestre são os pontos, na ordem da Hierarchy.
// Na aba Scene: linha ciano = calçada, linha amarela = travessia da rua.
public class RotaPedestres : MonoBehaviour
{
    [Tooltip("Marcado: os pedestres vão até o fim e voltam.\nDesmarcado: o último ponto liga no primeiro (volta completa).")]
    [SerializeField] private bool idaEVolta = true;

    private readonly List<PontoPedestre> pontos = new List<PontoPedestre>();

    public int Quantidade => pontos.Count;
    public bool IdaEVolta => idaEVolta;

    private void Awake()
    {
        Atualizar();
    }

    public void Atualizar()
    {
        pontos.Clear();
        foreach (Transform filho in transform)
        {
            PontoPedestre ponto = filho.GetComponent<PontoPedestre>();
            if (ponto != null) pontos.Add(ponto);
        }
    }

    public Vector2 Posicao(int indice) => pontos[indice].transform.position;

    // Próximo ponto no sentido atual (vira o sentido nas pontas, se for ida e volta)
    public int Proximo(int atual, ref int sentido)
    {
        int n = pontos.Count;
        int proximo = atual + sentido;

        if (idaEVolta)
        {
            if (proximo >= n || proximo < 0)
            {
                sentido = -sentido;
                proximo = atual + sentido;
            }
            return Mathf.Clamp(proximo, 0, n - 1);
        }

        return (proximo % n + n) % n;
    }

    // O trecho entre dois pontos vizinhos é travessia?
    public bool Atravessa(int de, int para)
    {
        int n = pontos.Count;
        if (para == de + 1) return Marcado(de);
        if (para == de - 1) return Marcado(para);
        if (de == n - 1 && para == 0) return Marcado(n - 1);
        if (de == 0 && para == n - 1) return Marcado(n - 1);
        return false;
    }

    public bool Marcado(int indice) => indice >= 0 && indice < pontos.Count && pontos[indice].atravessarAteOProximo;

    // ---------- Desenho na aba Scene ----------

    private void OnDrawGizmos()
    {
        List<PontoPedestre> lista = new List<PontoPedestre>();
        foreach (Transform filho in transform)
        {
            PontoPedestre p = filho.GetComponent<PontoPedestre>();
            if (p != null) lista.Add(p);
        }

        int trechos = idaEVolta ? lista.Count - 1 : lista.Count;
        for (int i = 0; i < trechos && lista.Count > 1; i++)
        {
            Vector3 a = lista[i].transform.position;
            Vector3 b = lista[(i + 1) % lista.Count].transform.position;
            Gizmos.color = lista[i].atravessarAteOProximo ? new Color(1f, 0.85f, 0.1f) : new Color(0.2f, 0.9f, 1f);
            Gizmos.DrawLine(a, b);
        }

        for (int i = 0; i < lista.Count; i++)
        {
            Gizmos.color = lista[i].atravessarAteOProximo ? new Color(1f, 0.85f, 0.1f) : new Color(0.2f, 0.9f, 1f);
            Gizmos.DrawSphere(lista[i].transform.position, 0.2f);
        }
    }
}
