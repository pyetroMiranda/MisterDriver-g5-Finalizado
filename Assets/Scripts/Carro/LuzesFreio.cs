using UnityEngine;

public class LuzesFreio : MonoBehaviour
{
    [Header("Ligação")]
    [SerializeField] private CarroController carro;

    [Header("Luzes de freio (sprites vermelhos)")]
    [SerializeField] private SpriteRenderer[] luzes;

    [Header("Brilho")]
    [SerializeField, Range(0f, 1f)] private float brilhoAceso = 1f;
    [SerializeField] private float velocidade = 25f;   // maior = liga e desliga mais rápido

    private void Start()
    {
        DefinirAlpha(0f);
    }

    private void Update()
    {
        // Pedal de freio (S/seta para baixo) enquanto NÃO está de ré, ou freio de mão
        bool freioPedal = carro.Freando && carro.VelocidadeComSinal > -0.05f;
        bool acesa = freioPedal || carro.FreioDeMaoAtivo;

        float alvo = acesa ? brilhoAceso : 0f;

        foreach (SpriteRenderer luz in luzes)
        {
            if (luz == null) continue;
            Color c = luz.color;
            c.a = Mathf.Lerp(c.a, alvo, velocidade * Time.deltaTime);
            luz.color = c;
        }
    }

    private void DefinirAlpha(float alpha)
    {
        foreach (SpriteRenderer luz in luzes)
        {
            if (luz == null) continue;
            Color c = luz.color;
            c.a = alpha;
            luz.color = c;
        }
    }
}