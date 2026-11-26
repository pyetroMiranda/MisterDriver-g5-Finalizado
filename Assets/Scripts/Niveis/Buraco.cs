using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Buraco : MonoBehaviour
{
    [Header("Dano")]
    [SerializeField] private int dano = 1;
    [SerializeField] private float velocidadeMinimaDano = 0.5f;   // passar devagar não causa dano

    [Header("Lentidão")]
    [SerializeField, Range(0f, 1f)] private float velocidadeMantidaAoEntrar = 0.5f;  // freada ao cair no buraco
    [SerializeField] private float arrasto = 3f;                  // quanto o buraco "segura" o carro enquanto ele está dentro

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // Suspensão Reforçada ligada: o buraco não faz nada
    private static bool Protegido(CarroController carro)
    {
        SuspensaoReforcada suspensao = carro.GetComponent<SuspensaoReforcada>();
        return suspensao != null && suspensao.Ativo;
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        CarroController carro = outro.GetComponentInParent<CarroController>();
        if (carro == null || Protegido(carro)) return;

        float velocidadeNaEntrada = carro.VelocidadeAtual;
        carro.ReduzirVelocidade(velocidadeMantidaAoEntrar);

        CarteiraMotorista carteira = carro.GetComponent<CarteiraMotorista>();
        if (carteira != null && carteira.PodeTomarDano && velocidadeNaEntrada >= velocidadeMinimaDano)
            carteira.PerderPontos(dano);
    }

    private void OnTriggerStay2D(Collider2D outro)
    {
        CarroController carro = outro.GetComponentInParent<CarroController>();
        if (carro == null || Protegido(carro)) return;

        carro.ReduzirVelocidade(Mathf.Max(0f, 1f - arrasto * Time.fixedDeltaTime));
    }
}