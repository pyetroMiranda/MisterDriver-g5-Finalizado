using UnityEngine;

// Setinha que gira em volta do carro apontando para o próximo ponto do exame.
// Some quando não há destino ou quando o carro já está perto dele.
public class SetaObjetivo : MonoBehaviour
{
    [SerializeField] private Transform carro;
    [SerializeField] private SpriteRenderer desenho;
    [SerializeField] private float raio = 2.6f;
    [SerializeField] private float distanciaSumir = 5f;
    [SerializeField] private float suavizacao = 10f;

    private Transform alvo;
    private float alfa;

    public Transform Alvo
    {
        get => alvo;
        set => alvo = value;
    }

    private void Start()
    {
        if (carro == null)
        {
            CarroController c = FindFirstObjectByType<CarroController>();
            if (c != null) carro = c.transform;
        }
        if (desenho == null) desenho = GetComponentInChildren<SpriteRenderer>();
        if (desenho != null) desenho.color = new Color(1f, 1f, 1f, 0f);
    }

    private void LateUpdate()
    {
        if (carro == null || desenho == null) return;

        bool mostrar = alvo != null && alvo.gameObject.activeInHierarchy &&
                       Vector2.Distance(carro.position, alvo.position) > distanciaSumir;
        alfa = Mathf.MoveTowards(alfa, mostrar ? 1f : 0f, Time.unscaledDeltaTime * 4f);

        Color c = desenho.color;
        c.a = alfa * (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f));
        desenho.color = c;
        if (alvo == null) return;

        Vector2 direcao = ((Vector2)alvo.position - (Vector2)carro.position).normalized;
        Vector3 desejado = (Vector2)carro.position + direcao * raio;
        desejado.z = transform.position.z;

        float k = 1f - Mathf.Exp(-suavizacao * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, desejado, k);
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg - 90f;   // o desenho aponta para cima
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, 0f, angulo), k);
    }
}
