using UnityEngine;

public class LuzesRe : MonoBehaviour
{
    [Header("Ligação")]
    [SerializeField] private CarroController carro;

    [Header("Luzes de ré (sprites brancos)")]
    [SerializeField] private SpriteRenderer[] luzes;

    [Header("Brilho")]
    [SerializeField, Range(0f, 1f)] private float brilhoAceso = 1f;
    [SerializeField] private float velocidade = 20f;
    [SerializeField] private float velocidadeMinimaRe = 0.05f;

    private void Start()
    {
        DefinirAlpha(0f);
    }

    private void Update()
    {
        bool emRe = carro.VelocidadeComSinal < -velocidadeMinimaRe;
        float alvo = emRe ? brilhoAceso : 0f;

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