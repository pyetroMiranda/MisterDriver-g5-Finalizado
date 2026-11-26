using UnityEngine;

// Advogado (passivo): em cada nível, anula os primeiros pontos de dano na carteira.
// A CarteiraMotorista chama Absorver() antes de tirar pontos.
public class Advogado : MonoBehaviour, IPoder
{
    [Header("Loja")]
    [SerializeField] private ItemLoja itemLoja;
    [SerializeField] private bool comprado = true;   // só vale se Item Loja estiver vazio (para testar)

    [Header("Defesa")]
    [SerializeField] private int pontosPorNivel = 3;

    private int restante;
    private int total;

    public string Nome => "Advogado";
    public string Tecla => "";                       // passivo: não tem tecla
    public string Info => "x" + restante;
    public bool Comprado => itemLoja != null ? itemLoja.Comprado : comprado;
    public bool Ativo => restante > 0;
    public bool Pronto => restante > 0;
    public float Progresso => total > 0 ? restante / (float)total : 0f;

    private void Awake()
    {
        total = Comprado ? pontosPorNivel : 0;
        restante = total;
    }

    // Devolve quanto dano sobra depois da defesa
    public int Absorver(int dano)
    {
        if (restante <= 0 || dano <= 0) return dano;

        int defendido = Mathf.Min(restante, dano);
        restante -= defendido;
        Debug.Log("Advogado defendeu " + defendido + " ponto(s). Restam " + restante + " de defesa.");
        return dano - defendido;
    }
}
