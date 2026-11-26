using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Personagem da casa (pai, mãe...). Vira para o Jason quando ele puxa conversa.
// A conversa vem do RoteiroCasa (que muda conforme a história anda); se ele não tiver, usa as falas daqui.
[RequireComponent(typeof(MovimentoGrade))]
public class NpcHistoria : Interagivel
{
    [SerializeField] private string id = "Pai";
    [SerializeField] private List<Fala> falasPadrao = new List<Fala>();

    [Header("Parado")]
    [SerializeField] private Direcao direcaoParado = Direcao.Baixo;
    [Tooltip("De vez em quando olha para os lados (fica mais vivo).")]
    [SerializeField] private bool olharEmVolta = true;

    private MovimentoGrade movimento;
    private bool conversando;

    public string Id => id;
    public MovimentoGrade Movimento => movimento;
    public override bool Bloqueia => false;   // quem bloqueia a passagem é o MovimentoGrade

    protected override void Awake()
    {
        base.Awake();
        movimento = GetComponent<MovimentoGrade>();
    }

    private void Start()
    {
        movimento.Virar(direcaoParado);
        if (olharEmVolta) StartCoroutine(OlharEmVolta());
    }

    public override bool Ocupa(Vector2Int c) => movimento != null && (movimento.Celula == c || movimento.Destino == c);

    // Usado pela ferramenta do Editor
    public void Definir(string novoId, Direcao parado, List<Fala> padrao)
    {
        id = novoId;
        direcaoParado = parado;
        falasPadrao = padrao;
    }

    // Muda para onde ele fica olhando quando está parado (o roteiro usa depois de fazê-lo andar)
    public void MudarParado(Direcao d)
    {
        direcaoParado = d;
        if (movimento != null && !movimento.Andando) movimento.Virar(d);
    }

    public override IEnumerator Interagir(JogadorCasa jogador)
    {
        conversando = true;
        movimento.VirarPara(jogador.Celula);

        RoteiroBase roteiro = RoteiroBase.Atual;
        if (roteiro != null) yield return roteiro.ConversarCom(this);
        else if (CaixaDialogo.Instancia != null) yield return CaixaDialogo.Instancia.Conversar(falasPadrao);

        yield return new WaitForSeconds(0.4f);
        if (!movimento.Andando) movimento.Virar(direcaoParado);
        conversando = false;
    }

    private IEnumerator OlharEmVolta()
    {
        Direcao[] lados = { Direcao.Esquerda, Direcao.Direita, Direcao.Baixo, Direcao.Cima };
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(2.5f, 5f));
            if (conversando || movimento.Andando || CaixaDialogo.AlgumaAberta) continue;

            movimento.Virar(lados[Random.Range(0, lados.Length)]);
            yield return new WaitForSeconds(Random.Range(0.8f, 1.5f));
            if (!conversando && !movimento.Andando) movimento.Virar(direcaoParado);
        }
    }
}
