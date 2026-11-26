using UnityEngine;

public class GatilhoTutorialCones : MonoBehaviour
{
    [SerializeField] private TutorialInicio tutorial;

    private bool usado;

    private void OnTriggerStay2D(Collider2D outro)
    {
        if (usado || tutorial == null)
            return;

        if (outro.GetComponentInParent<CarroController>() == null)
            return;

        usado = tutorial.MostrarAvisoCones();
    }
}
