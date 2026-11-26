using UnityEngine;

public class RodasDianteiras : MonoBehaviour
{
    [Header("Ligação")]
    [SerializeField] private CarroController carro;

    [Header("Rodas")]
    [SerializeField] private Transform[] rodas;          // as duas rodas dianteiras

    [Header("Giro")]
    [SerializeField] private float anguloMaximo = 30f;    // quanto a roda pode virar
    [SerializeField] private float suavizacao = 12f;      // maior = acompanha mais rápido
    [SerializeField] private bool inverter = false;       // marque se virar para o lado errado

    private float anguloAtual;

    private void Update()
    {
        float alvo = carro.Volante * anguloMaximo * (inverter ? -1f : 1f);
        anguloAtual = Mathf.Lerp(anguloAtual, alvo, suavizacao * Time.deltaTime);

        foreach (Transform roda in rodas)
        {
            if (roda != null)
                roda.localRotation = Quaternion.Euler(0f, 0f, anguloAtual);
        }
    }
}