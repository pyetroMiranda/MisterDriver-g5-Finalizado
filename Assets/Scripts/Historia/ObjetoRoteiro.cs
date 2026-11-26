using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Objeto que o roteiro da cena controla (ex.: a prateleira do leite, o caixa do mercado).
// Quando o Jason examina, o roteiro decide o que acontece pelo "id".
// O "destaque" (um brilhinho) pula de leve para chamar a atenção e o roteiro some com ele depois.
public class ObjetoRoteiro : Interagivel
{
    [SerializeField] private string id = "item";
    [SerializeField] private List<Fala> falas = new List<Fala>();
    [SerializeField] private Transform destaque;

    private Vector3 posicaoDestaque;

    public string Id => id;
    public List<Fala> Falas => falas;

    // Usado pela ferramenta do Editor
    public void Definir(string novoId, List<Fala> novasFalas, Transform brilho)
    {
        id = novoId;
        falas = novasFalas ?? new List<Fala>();
        destaque = brilho;
    }

    public void MostrarDestaque(bool mostrar)
    {
        if (destaque != null) destaque.gameObject.SetActive(mostrar);
    }

    private void Start()
    {
        if (destaque != null) posicaoDestaque = destaque.localPosition;
    }

    private void Update()
    {
        if (destaque == null || !destaque.gameObject.activeSelf) return;
        destaque.localPosition = posicaoDestaque + Vector3.up * (Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.18f);
    }

    public override IEnumerator Interagir(JogadorCasa jogador)
    {
        RoteiroBase roteiro = RoteiroBase.Atual;
        if (roteiro != null) yield return roteiro.InteragirCom(this);
        else if (CaixaDialogo.Instancia != null) yield return CaixaDialogo.Instancia.Conversar(falas);
    }
}
