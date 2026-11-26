using UnityEngine;

// Muro, prédio ou canteiro da autoescola: bater nele conta uma falta na MissaoExame.
[RequireComponent(typeof(Collider2D))]
public class ObstaculoAutoescola : MonoBehaviour
{
    private MissaoExame missao;

    private void Start()
    {
        missao = FindFirstObjectByType<MissaoExame>();
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        if (missao == null || colisao.gameObject.GetComponentInParent<CarroController>() == null) return;
        missao.RegistrarFalta(false);
    }
}
