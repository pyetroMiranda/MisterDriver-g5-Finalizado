using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Porta da casa (porta da frente, porta da garagem, portão da garagem).
// Trancada: bloqueia a passagem e, se o Jason esbarrar, mostra um aviso.
// Liberada: deixa passar; se for uma "saída", pisar nela termina a cena (o roteiro decide).
public class PortaCasa : Interagivel
{
    [SerializeField] private bool liberada;
    [Tooltip("Pisar nela (liberada) termina a cena. Desmarcado = é só uma passagem (ex.: a porta da garagem).")]
    [SerializeField] private bool saida = true;
    [SerializeField] private float intervaloAviso = 1.2f;
    [Tooltip("O que o Jason diz quando ela está trancada. Vazio = pergunta ao roteiro da cena.")]
    [SerializeField] private List<Fala> falasTrancada = new List<Fala>();

    [Header("Desenho (opcional)")]
    [SerializeField] private SpriteRenderer desenho;
    [Tooltip("Imagem da porta aberta (vazio = não troca).")]
    [SerializeField] private Sprite spriteAberta;
    [Tooltip("Ao abrir, o desenho enrola para cima (portão de garagem).")]
    [SerializeField] private bool enrolar;

    private float proximoAviso;

    public override bool Bloqueia => !liberada;
    public bool Liberada => liberada;

    // Usado pela ferramenta do Editor
    public void Definir(bool ehSaida, List<Fala> trancada, SpriteRenderer sr = null, Sprite aberta = null, bool enrolarAoAbrir = false)
    {
        saida = ehSaida;
        falasTrancada = trancada ?? new List<Fala>();
        desenho = sr;
        spriteAberta = aberta;
        enrolar = enrolarAoAbrir;
    }

    public void Liberar()
    {
        if (liberada) return;
        liberada = true;
        if (desenho == null) return;
        if (spriteAberta != null) desenho.sprite = spriteAberta;
        if (enrolar && isActiveAndEnabled) StartCoroutine(Enrolar());
        Sons.TocarEm(enrolar ? "PortaoGaragem" : "Porta", transform.position, 1f, 1f, 30f);
    }

    public override IEnumerator Interagir(JogadorCasa jogador)
    {
        if (liberada || Time.unscaledTime < proximoAviso) yield break;

        if (falasTrancada != null && falasTrancada.Count > 0)
        {
            if (CaixaDialogo.Instancia != null) yield return CaixaDialogo.Instancia.Conversar(falasTrancada);
        }
        else
        {
            RoteiroBase roteiro = RoteiroBase.Atual;
            if (roteiro != null) yield return roteiro.PortaFechada(this);
        }
        proximoAviso = Time.unscaledTime + intervaloAviso;
    }

    public override void Pisou(JogadorCasa jogador)
    {
        if (!liberada || !saida) return;
        if (!enrolar) Sons.Tocar("Porta");
        RoteiroBase roteiro = RoteiroBase.Atual;
        if (roteiro != null) roteiro.Sair(this);
    }

    // O portão sobe enrolando (o desenho tem o ponto de apoio em cima)
    private IEnumerator Enrolar()
    {
        Transform folha = desenho.transform;
        Vector3 inicio = folha.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.9f;
            float k = Mathf.SmoothStep(0f, 1f, t);
            folha.localScale = new Vector3(inicio.x, Mathf.Lerp(inicio.y, inicio.y * 0.12f, k), inicio.z);
            // tremidinha de portão velho
            folha.localPosition = new Vector3(Mathf.Sin(t * 60f) * 0.02f * (1f - k), folha.localPosition.y, 0f);
            yield return null;
        }
        folha.localPosition = new Vector3(0f, folha.localPosition.y, 0f);
    }
}
