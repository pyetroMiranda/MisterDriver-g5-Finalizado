using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CarteiraMotorista))]
public class PiscarAoDano : MonoBehaviour
{
    [SerializeField] private float intervalo = 0.1f;   // tempo entre sumir e aparecer

    private CarteiraMotorista carteira;
    private SpriteRenderer[] sprites;
    private Coroutine piscando;

    private void Awake()
    {
        carteira = GetComponent<CarteiraMotorista>();
        sprites = GetComponentsInChildren<SpriteRenderer>();   // inclui o próprio objeto e os filhos
    }

    private void OnEnable()
    {
        carteira.aoPerderPontos.AddListener(AoPerderPontos);
    }

    private void OnDisable()
    {
        carteira.aoPerderPontos.RemoveListener(AoPerderPontos);
    }

    private void AoPerderPontos(int restantes)
    {
        if (piscando != null) StopCoroutine(piscando);
        piscando = StartCoroutine(Piscar());
    }

    private IEnumerator Piscar()
    {
        float fim = Time.time + carteira.TempoInvencivel;
        bool visivel = true;

        while (Time.time < fim)
        {
            visivel = !visivel;
            DefinirVisivel(visivel);
            yield return new WaitForSeconds(intervalo);
        }

        DefinirVisivel(true);   // sempre termina visível
        piscando = null;
    }

    private void DefinirVisivel(bool valor)
    {
        foreach (SpriteRenderer sr in sprites)
        {
            if (sr != null) sr.enabled = valor;
        }
    }
}