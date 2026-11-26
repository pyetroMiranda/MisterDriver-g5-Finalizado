using System.Collections;
using UnityEngine;

// Base dos roteiros das cenas da casa (RoteiroCasa = capítulo 1, RoteiroGaragem = capítulo 2).
// Os NPCs, as portas, os objetos e o carro da garagem perguntam ao roteiro da cena o que fazer.
public abstract class RoteiroBase : MonoBehaviour
{
    // Os objetos da casa mudam de fala depois da conversa com o pai (capítulo 1)
    public virtual bool ConversouComPai => false;

    public abstract IEnumerator ConversarCom(NpcHistoria npc);

    // Porta trancada sem falas próprias
    public virtual IEnumerator PortaFechada(PortaCasa porta) { yield break; }

    // O Jason pisou numa porta de saída liberada
    public virtual void Sair(PortaCasa porta) { }

    // O Jason examinou o carro da garagem
    public virtual IEnumerator UsarGaragem(VitrineCarro vitrine) { yield break; }

    // O Jason examinou um objeto que o roteiro controla (prateleira das compras, caixa do mercado...)
    public virtual IEnumerator InteragirCom(ObjetoRoteiro objeto) { yield break; }

    public static RoteiroBase Atual => FindFirstObjectByType<RoteiroBase>();
}
