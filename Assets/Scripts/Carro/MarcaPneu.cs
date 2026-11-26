using UnityEngine;

public class MarcaPneu : MonoBehaviour
{
    [SerializeField] private float duracao = 4f;
    [SerializeField, Range(0f, 1f)] private float alphaInicial = 0.6f;

    private SpriteRenderer sr;
    private float tempo;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        SetAlpha(alphaInicial);
    }

    private void Update()
    {
        tempo += Time.deltaTime;
        SetAlpha(alphaInicial * (1f - tempo / duracao));
        if (tempo >= duracao) Destroy(gameObject);
    }

    private void SetAlpha(float a)
    {
        Color c = sr.color;
        c.a = Mathf.Clamp01(a);
        sr.color = c;
    }
}