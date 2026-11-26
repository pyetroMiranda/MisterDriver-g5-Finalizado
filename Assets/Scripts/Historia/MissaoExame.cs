using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Exame prático com o instrutor Gervásio (cena Historia_Exame, o "segundo tutorial").
// O Jason segue a seta por uma lista de pontos; em cada um o instrutor ensina uma coisa.
// Cada batida vira uma falta (e um comentário desesperado do instrutor).
// Com muitas faltas, ou com a carteira zerada, ele é reprovado e tenta de novo.
// No fim ganha a carteira de motorista e vai para o Nivel1.
public class MissaoExame : MonoBehaviour
{
    [Serializable]
    public class Etapa
    {
        public string objetivo = "Siga a seta até o próximo ponto";
        public PontoExame ponto;
        public List<Fala> falasAntes = new List<Fala>();
        public List<Fala> falasAoChegar = new List<Fala>();
    }

    [Header("Ligações (procura sozinho se ficar vazio)")]
    [SerializeField] private CarroController carro;
    [SerializeField] private CarteiraMotorista carteira;
    [SerializeField] private CaixaDialogo dialogo;
    [SerializeField] private ObjetivoHUD objetivo;
    [SerializeField] private SetaObjetivo seta;
    [SerializeField] private TelaCarteira telaCarteira;

    [Header("Personagens")]
    [SerializeField] private PersonagemHistoria jason;
    [SerializeField] private PersonagemHistoria instrutor;

    [Header("Regras")]
    [SerializeField, Min(1)] private int limiteFaltas = 5;
    [SerializeField] private float intervaloComentarios = 1.5f;

    [Header("Final")]
    [SerializeField] private string proximaCena = ProgressoHistoria.PrimeiroNivel;
    [Tooltip("Cena marcada como concluída quando o Jason passa (libera o próximo capítulo).")]
    [SerializeField] private string cenaConcluida = ProgressoHistoria.CenaExame;
    [Tooltip("Mostra a carteira de motorista no final (só no exame da cidade).")]
    [SerializeField] private bool mostrarCarteira = true;

    [Header("Roteiro")]
    [SerializeField] private List<Fala> falasAbertura = new List<Fala>();
    [SerializeField] private List<Etapa> etapas = new List<Etapa>();
    [Tooltip("Uma é sorteada a cada batida.")]
    [SerializeField] private List<Fala> reacoesBatida = new List<Fala>();
    [Tooltip("Uma é sorteada quando o Jason derruba um cone (se ficar vazia, usa as de batida).")]
    [SerializeField] private List<Fala> reacoesCone = new List<Fala>();
    [SerializeField] private List<Fala> falasReprovado = new List<Fala>();
    [SerializeField] private List<Fala> falasAprovadoPerfeito = new List<Fala>();
    [Tooltip("Use {faltas} no texto para mostrar o número de faltas.")]
    [SerializeField] private List<Fala> falasAprovado = new List<Fala>();
    [SerializeField] private List<Fala> falasDespedida = new List<Fala>();

    private int faltas;
    private bool reprovado;
    private bool terminou;
    private float proximoComentario;
    private float ultimaFalta = -10f;
    private Coroutine roteiro;

    public List<Etapa> Etapas => etapas;
    public int Faltas => faltas;

    // Usado pela ferramenta do Editor (autoescola: sem carteira, outra cena no final, cones)
    public void DefinirFinal(string concluida, string proxima, bool carteiraNoFinal, int limite, List<Fala> cones)
    {
        cenaConcluida = concluida;
        proximaCena = proxima;
        mostrarCarteira = carteiraNoFinal;
        limiteFaltas = Mathf.Max(1, limite);
        reacoesCone = cones;
    }

    // Usado pela ferramenta do Editor para preencher o roteiro
    public void DefinirRoteiro(List<Fala> abertura, List<Etapa> novasEtapas, List<Fala> reacoes, List<Fala> reprovadoFalas,
                               List<Fala> perfeito, List<Fala> aprovado, List<Fala> despedida)
    {
        falasAbertura = abertura;
        etapas = novasEtapas;
        reacoesBatida = reacoes;
        falasReprovado = reprovadoFalas;
        falasAprovadoPerfeito = perfeito;
        falasAprovado = aprovado;
        falasDespedida = despedida;
    }

    private void Start()
    {
        if (carro == null) carro = FindFirstObjectByType<CarroController>();
        if (carteira == null && carro != null) carteira = carro.GetComponent<CarteiraMotorista>();
        if (dialogo == null) dialogo = CaixaDialogo.Instancia;
        if (objetivo == null) objetivo = FindFirstObjectByType<ObjetivoHUD>(FindObjectsInactive.Include);
        if (seta == null) seta = FindFirstObjectByType<SetaObjetivo>(FindObjectsInactive.Include);
        if (telaCarteira == null) telaCarteira = FindFirstObjectByType<TelaCarteira>(FindObjectsInactive.Include);

        if (carro == null || dialogo == null)
        {
            Debug.LogError("MissaoExame: não encontrei o carro ou a CaixaDialogo. " +
                           "Monte a cena com Mr Driver > História > 3. Criar cena do exame.", this);
            enabled = false;
            return;
        }

        foreach (Etapa e in etapas)
            if (e.ponto != null) e.ponto.Desativar();

        if (carteira != null)
        {
            carteira.aoPerderPontos.AddListener(Bateu);
            carteira.aoSuspenderCarteira.AddListener(CarteiraSuspensa);
        }

        roteiro = StartCoroutine(Roteiro());
    }

    private void OnDestroy()
    {
        if (carteira != null)
        {
            carteira.aoPerderPontos.RemoveListener(Bateu);
            carteira.aoSuspenderCarteira.RemoveListener(CarteiraSuspensa);
        }
    }

    // ---------- Roteiro ----------

    private IEnumerator Roteiro()
    {
        yield return null;   // espera todo mundo terminar o Start()
        yield return new WaitForSecondsRealtime(0.8f);   // a tela clareando
        yield return dialogo.Conversar(falasAbertura, true);

        for (int i = 0; i < etapas.Count; i++)
        {
            Etapa etapa = etapas[i];
            if (etapa.ponto == null) continue;

            if (etapa.falasAntes.Count > 0) yield return dialogo.Conversar(etapa.falasAntes, true);

            if (objetivo != null) objetivo.Mostrar(etapa.objetivo);
            etapa.ponto.Ativar();
            if (seta != null) seta.Alvo = etapa.ponto.transform;

            while (!etapa.ponto.Concluido) yield return null;
            Sons.Tocar("Ponto");

            etapa.ponto.Desativar();
            if (seta != null) seta.Alvo = null;
            if (etapa.falasAoChegar.Count > 0) yield return dialogo.Conversar(etapa.falasAoChegar, true);
        }

        yield return Aprovar();
    }

    private IEnumerator Aprovar()
    {
        terminou = true;
        if (objetivo != null) objetivo.Esconder();
        ProgressoNiveis.Concluir(cenaConcluida);

        yield return dialogo.Conversar(Trocar(faltas == 0 ? falasAprovadoPerfeito : falasAprovado), true);

        Time.timeScale = 0f;
        if (mostrarCarteira && telaCarteira != null)
            yield return telaCarteira.Mostrar(jason != null ? jason.retrato : null, jason != null ? jason.nome : "Jason",
                                              faltas, instrutor != null ? instrutor.nome : "Gervásio");

        yield return dialogo.Conversar(falasDespedida, true);
        TransicaoTela.Carregar(proximaCena);
    }

    // ---------- Batidas ----------

    private void Bateu(int pontosRestantes) => RegistrarFalta(false);

    // Chamado pelas batidas na carteira, pelos cones (ConeAutoescola) e pelos muros (ObstaculoAutoescola)
    public void RegistrarFalta(bool cone)
    {
        if (terminou || reprovado || !enabled) return;
        if (Time.time - ultimaFalta < 0.8f) return;   // uma batida encosta várias vezes
        ultimaFalta = Time.time;

        faltas++;
        Sons.Tocar("Falta");
        if (faltas >= limiteFaltas)
        {
            Reprovar();
            return;
        }

        List<Fala> lista = cone && reacoesCone != null && reacoesCone.Count > 0 ? reacoesCone : reacoesBatida;
        if (lista.Count > 0 && Time.unscaledTime >= proximoComentario && !dialogo.Aberta)
        {
            Fala reacao = lista[UnityEngine.Random.Range(0, lista.Count)];
            dialogo.Comentar(new Fala(reacao.quem, reacao.texto + "  <size=80%>(falta " + faltas + " de " + limiteFaltas + ")</size>",
                                      reacao.emote, true), 2.6f);
            proximoComentario = Time.unscaledTime + intervaloComentarios;
        }
    }

    private void CarteiraSuspensa()
    {
        if (!terminou) Reprovar();
    }

    private void Reprovar()
    {
        if (reprovado) return;
        reprovado = true;
        if (roteiro != null) StopCoroutine(roteiro);
        StartCoroutine(RotinaReprovado());
    }

    private IEnumerator RotinaReprovado()
    {
        if (objetivo != null) objetivo.Esconder();
        if (seta != null) seta.Alvo = null;

        while (dialogo.Aberta) yield return null;
        yield return dialogo.Conversar(falasReprovado, true);
        TransicaoTela.Carregar(SceneManager.GetActiveScene().name);
    }

    private List<Fala> Trocar(List<Fala> falas)
    {
        List<Fala> lista = new List<Fala>();
        foreach (Fala f in falas)
            lista.Add(new Fala(f.quem, f.texto.Replace("{faltas}", faltas.ToString()), f.emote, f.tremer));
        return lista;
    }
}
