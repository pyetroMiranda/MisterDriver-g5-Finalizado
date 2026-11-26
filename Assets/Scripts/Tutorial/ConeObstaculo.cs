using UnityEngine;

public class ConeObstaculo : MonoBehaviour
{
    [SerializeField] private GameObject pnlGameOver;

    private bool bateu;

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        if (bateu)
            return;

        CarroController carro =
            colisao.gameObject.GetComponentInParent<CarroController>();

        if (carro == null)
            return;

        if (pnlGameOver == null)
        {
            Debug.LogError(
                "Preencha o campo Pnl Game Over neste cone.",
                this
            );
            return;
        }

        bateu = true;

        pnlGameOver.SetActive(true);
        Time.timeScale = 0f;
    }
}