using UnityEngine;

// Ponto extra para o GPS: coloque em ruas onde não passa trânsito.
// Ele se liga sozinho aos pontos (de rotas ou outros PontoGPS) a até 12 unidades de distância.
// Na aba Scene aparece como uma bolinha verde.
public class PontoGPS : MonoBehaviour
{
    public const float RaioLigacao = 12f;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.5f);
        Gizmos.DrawSphere(transform.position, 0.3f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, RaioLigacao);
    }
}
