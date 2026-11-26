using UnityEngine;

// Área invisível na pista: quando o carro passa, mostra a dica do tutorial com o mesmo Id.
// Precisa de um Collider2D marcado como "Is Trigger".
[RequireComponent(typeof(Collider2D))]
public class GatilhoDica : MonoBehaviour
{
    [SerializeField] private string id = "CurvaEsquerda";
    [SerializeField] private TutorialNivel tutorial;   // se ficar vazio, procura sozinho

    private bool usado;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        if (tutorial == null) tutorial = FindFirstObjectByType<TutorialNivel>();
    }

    private void OnTriggerEnter2D(Collider2D outro) => Tentar(outro);
    private void OnTriggerStay2D(Collider2D outro) => Tentar(outro);

    private void Tentar(Collider2D outro)
    {
        if (usado || tutorial == null) return;
        if (outro.GetComponentInParent<CarroController>() == null) return;

        usado = true;
        tutorial.Gatilho(id);
    }
}
