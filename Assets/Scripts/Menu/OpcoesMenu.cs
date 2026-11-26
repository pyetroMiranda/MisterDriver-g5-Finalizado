using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tela de Opções: mostra a porcentagem de cada volume (quem muda o volume de verdade é o VolumesJogo),
// liga/desliga a tela cheia e recomeça a história (pede para apertar duas vezes, para ninguém apagar sem querer).
// Montado por Mr Driver > Menu > Montar telas do menu.
public class OpcoesMenu : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Slider sliderMusica;
    [SerializeField] private Slider sliderEfeitos;
    [SerializeField] private TMP_Text textoMusica;
    [SerializeField] private TMP_Text textoEfeitos;

    [Header("Botões")]
    [SerializeField] private BlocoMenu blocoTelaCheia;
    [SerializeField] private BlocoMenu blocoRecomecar;
    [SerializeField] private float tempoParaConfirmar = 3f;

    private bool telaCheia;
    private float confirmarAte = -1f;
    private float mensagemAte = -1f;

    private void OnEnable()
    {
        if (sliderMusica != null) sliderMusica.onValueChanged.AddListener(MudouMusica);
        if (sliderEfeitos != null) sliderEfeitos.onValueChanged.AddListener(MudouEfeitos);
        telaCheia = Screen.fullScreen;
        confirmarAte = -1f;
        mensagemAte = -1f;
        AtualizarTextos();
    }

    private void OnDisable()
    {
        if (sliderMusica != null) sliderMusica.onValueChanged.RemoveListener(MudouMusica);
        if (sliderEfeitos != null) sliderEfeitos.onValueChanged.RemoveListener(MudouEfeitos);
    }

    private void Update()
    {
        // O pedido de confirmação some sozinho depois de alguns segundos
        if (confirmarAte > 0f && Time.unscaledTime > confirmarAte) { confirmarAte = -1f; AtualizarTextos(); }
        if (mensagemAte > 0f && Time.unscaledTime > mensagemAte) { mensagemAte = -1f; AtualizarTextos(); }
    }

    private void MudouMusica(float valor) => AtualizarTextos();
    private void MudouEfeitos(float valor) => AtualizarTextos();

    public void AlternarTelaCheia()
    {
        telaCheia = !telaCheia;
        Screen.fullScreen = telaCheia;
        AtualizarTextos();
    }

    public void RecomecarHistoria()
    {
        if (confirmarAte < 0f)
        {
            confirmarAte = Time.unscaledTime + tempoParaConfirmar;
            AtualizarTextos();
            return;
        }

        ProgressoHistoria.Apagar();
        confirmarAte = -1f;
        mensagemAte = Time.unscaledTime + 2.5f;
        AtualizarTextos();
    }

    private void AtualizarTextos()
    {
        if (textoMusica != null && sliderMusica != null) textoMusica.text = Mathf.RoundToInt(sliderMusica.value * 100f) + "%";
        if (textoEfeitos != null && sliderEfeitos != null) textoEfeitos.text = Mathf.RoundToInt(sliderEfeitos.value * 100f) + "%";
        if (blocoTelaCheia != null) blocoTelaCheia.DefinirSubtitulo(telaCheia ? "LIGADA" : "DESLIGADA");

        if (blocoRecomecar != null)
        {
            string sub;
            if (mensagemAte > 0f) sub = "HISTÓRIA APAGADA!";
            else if (confirmarAte > 0f) sub = "TEM CERTEZA? APERTE DE NOVO";
            else sub = "VOLTA PARA A CASA DO JASON";
            blocoRecomecar.DefinirSubtitulo(sub);
        }
    }
}
