using UnityEngine;

// Moeda para espalhar pela pista: o carro passa por cima e ganha moedas.
// Precisa de um Collider2D marcado como "Is Trigger".
[RequireComponent(typeof(Collider2D))]
public class Moeda : MonoBehaviour
{
    [SerializeField] private int valor = 10;
    [SerializeField] private AudioClip somColetar;   // opcional

    [Header("Animação")]
    [SerializeField] private float velocidadeGiro = 90f;   // graus por segundo (0 = parada)

    private bool coletada;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        if (velocidadeGiro != 0f)
            transform.Rotate(0f, 0f, velocidadeGiro * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (coletada) return;
        if (outro.GetComponentInParent<CarroController>() == null) return;

        coletada = true;
        Loja.AdicionarMoedas(valor);

        if (somColetar != null)
            AudioSource.PlayClipAtPoint(somColetar, transform.position);
        else
            Sons.Tocar("Moeda");

        Destroy(gameObject);
    }
}
