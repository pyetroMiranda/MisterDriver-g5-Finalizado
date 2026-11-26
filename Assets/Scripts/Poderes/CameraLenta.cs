using UnityEngine;

// Câmera Lenta: o jogo inteiro fica mais devagar por alguns segundos (contados em tempo real).
// Entra e sai suave. Se outra coisa mudar o tempo (pausa, conclusão), ela não briga e termina.
public class CameraLenta : PoderAtivo
{
    [Header("Câmera lenta")]
    [SerializeField, Range(0.1f, 0.9f)] private float velocidadeDoJogo = 0.4f;
    [SerializeField] private float transicao = 0.25f;   // segundos para entrar e sair

    private float tempoEsperado = 1f;
    private float fixoOriginal;
    private float tempoNoEfeito;
    private bool controlando;

    public override string Nome => "Câmera Lenta";
    protected override bool UsaTempoReal => true;

    private void Reset()
    {
        duracao = 4f;
        recarga = 30f;
    }

    protected override void Awake()
    {
        base.Awake();
        fixoOriginal = Time.fixedDeltaTime;
    }

    protected override void AoAtivar()
    {
        if (!Mathf.Approximately(Time.timeScale, 1f)) { Encerrar(); return; }
        controlando = true;
        tempoNoEfeito = 0f;
        tempoEsperado = 1f;
    }

    protected override void DuranteEfeito()
    {
        if (!controlando) return;

        // Alguém mexeu no tempo (pausa, conclusão...): sai sem atrapalhar
        if (!Mathf.Approximately(Time.timeScale, tempoEsperado)) { Soltar(); Encerrar(); return; }

        tempoNoEfeito += Time.unscaledDeltaTime;
        float restanteEfeito = duracao - tempoNoEfeito;

        // Entra devagar, fica lento, e volta devagar no final
        float k = Mathf.Min(Mathf.Clamp01(tempoNoEfeito / transicao), Mathf.Clamp01(restanteEfeito / transicao));
        Aplicar(Mathf.Lerp(1f, velocidadeDoJogo, k));
    }

    protected override void AoEncerrar()
    {
        if (controlando && Mathf.Approximately(Time.timeScale, tempoEsperado)) Aplicar(1f);
        Soltar();
    }

    private void Aplicar(float escala)
    {
        tempoEsperado = escala;
        Time.timeScale = escala;
        Time.fixedDeltaTime = fixoOriginal * escala;   // física continua suave em câmera lenta
    }

    private void Soltar()
    {
        controlando = false;
        Time.fixedDeltaTime = fixoOriginal;
    }

    private void OnDisable()
    {
        if (controlando && Mathf.Approximately(Time.timeScale, tempoEsperado)) Time.timeScale = 1f;
        Soltar();
    }
}
