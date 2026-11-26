using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CarroController))]
public class Nitro : MonoBehaviour, IPoder
{
    [Header("Loja")]
    [SerializeField] private ItemLoja itemLoja;      // item da loja que libera este poder
    [SerializeField] private bool comprado = true;   // só vale se Item Loja estiver vazio (para testar)

    [Header("Boost")]
    [SerializeField] private float duracao = 3f;                 // segundos de nitro
    [SerializeField] private float recarga = 30f;                // segundos para recarregar depois do uso
    [SerializeField] private float multiplicadorVelocidade = 1.8f;
    [SerializeField] private float multiplicadorAceleracao = 3f;
    [SerializeField] private float tempoTransicao = 0.25f;       // entrada e saída suaves

    [Header("Chamas do escapamento")]
    [SerializeField] private SpriteRenderer[] chamas;
    [SerializeField, Range(0f, 0.5f)] private float tremorChama = 0.15f;

    [Header("Eventos")]
    public UnityEvent aoAtivar;
    public UnityEvent aoFicarPronto;

    private CarroController carro;
    private bool ativo;
    private float tempoRestante;
    private float tempoRecarga;
    private float intensidade;
    private Vector3[] escalasChamas;

    public bool Comprado => itemLoja != null ? itemLoja.Comprado : comprado;
    public string Nome => "Nitro";
    public string Tecla => "CTRL";
    public string Info => "";

    // Melhorias da loja (aplicadas pelo MelhoriasCarro)
    public float BonusDuracao { get; set; }
    public float ReducaoRecarga { get; set; }
    private float DuracaoTotal => duracao + BonusDuracao;
    private float RecargaTotal => Mathf.Max(3f, recarga - ReducaoRecarga);
    public bool Ativo => ativo;
    public bool Pronto => Comprado && !ativo && tempoRecarga <= 0f;

    // 0 a 1: enquanto ativo, mostra o que sobra de nitro; enquanto recarrega, mostra o quanto já carregou
    public float Progresso => ativo ? tempoRestante / DuracaoTotal : 1f - tempoRecarga / RecargaTotal;

    private void Awake()
    {
        carro = GetComponent<CarroController>();

        escalasChamas = new Vector3[chamas.Length];
        for (int i = 0; i < chamas.Length; i++)
        {
            if (chamas[i] == null) continue;
            escalasChamas[i] = chamas[i].transform.localScale;
            DefinirAlpha(chamas[i], 0f);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (Pronto && ApertouNitro()) Ativar();

        if (ativo)
        {
            tempoRestante -= dt;
            if (tempoRestante <= 0f) Encerrar();
        }
        else if (tempoRecarga > 0f)
        {
            tempoRecarga -= dt;
            if (tempoRecarga <= 0f)
            {
                tempoRecarga = 0f;
                aoFicarPronto?.Invoke();
            }
        }

        // Entrada e saída suaves do boost
        intensidade = Mathf.MoveTowards(intensidade, ativo ? 1f : 0f, dt / Mathf.Max(tempoTransicao, 0.01f));

        carro.MultiplicadorVelocidade = Mathf.Lerp(1f, multiplicadorVelocidade, intensidade);
        carro.MultiplicadorAceleracao = Mathf.Lerp(1f, multiplicadorAceleracao, intensidade);
        carro.ForcarAcelerar = ativo;

        AtualizarChamas();
    }

    private void Ativar()
    {
        ativo = true;
        tempoRestante = DuracaoTotal;
        aoAtivar?.Invoke();
    }

    private void Encerrar()
    {
        ativo = false;
        tempoRestante = 0f;
        tempoRecarga = RecargaTotal;   // a recarga de 30s começa quando o nitro termina
    }

    private bool ApertouNitro()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        return t != null && (t.leftCtrlKey.wasPressedThisFrame || t.rightCtrlKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
#endif
    }

    private void AtualizarChamas()
    {
        for (int i = 0; i < chamas.Length; i++)
        {
            if (chamas[i] == null) continue;

            DefinirAlpha(chamas[i], intensidade);

            // Chama "tremendo" para parecer viva
            float tremor = 1f + Random.Range(-tremorChama, tremorChama);
            chamas[i].transform.localScale = escalasChamas[i] * Mathf.Lerp(0.5f, tremor, intensidade);
        }
    }

    private void DefinirAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}
