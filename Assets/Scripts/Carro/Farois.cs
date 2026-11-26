using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Farois : MonoBehaviour
{
    [Header("Ligação")]
    [SerializeField] private CarroController carro;

    [Header("Faróis dianteiros")]
    [SerializeField] private SpriteRenderer[] farois;

    [Header("Brilho")]
    [SerializeField, Range(0f, 1f)] private float brilhoLigado = 0.7f;
    [SerializeField, Range(0f, 1f)] private float brilhoAcelerando = 1f;  // farol "reforça" ao acelerar
    [SerializeField] private float velocidade = 15f;

    [Header("Estado inicial")]
    [SerializeField] private bool ligadoNoInicio = false;

    private bool ligado;

    private void Start()
    {
        ligado = ligadoNoInicio;
        DefinirAlpha(ligado ? brilhoLigado : 0f);
    }

    private void Update()
    {
        LerTecla();

        float alvo = 0f;
        if (ligado) alvo = carro.Acelerando ? brilhoAcelerando : brilhoLigado;

        foreach (SpriteRenderer farol in farois)
        {
            if (farol == null) continue;
            Color c = farol.color;
            c.a = Mathf.Lerp(c.a, alvo, velocidade * Time.deltaTime);
            farol.color = c;
        }
    }

    private void LerTecla()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        if (t != null && t.lKey.wasPressedThisFrame) ligado = !ligado;
#else
        if (Input.GetKeyDown(KeyCode.L)) ligado = !ligado;
#endif
    }

    private void DefinirAlpha(float alpha)
    {
        foreach (SpriteRenderer farol in farois)
        {
            if (farol == null) continue;
            Color c = farol.color;
            c.a = alpha;
            farol.color = c;
        }
    }
}