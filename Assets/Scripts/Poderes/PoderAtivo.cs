using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Base dos poderes com tecla: cuida da loja, da tecla, da duração e da recarga.
// Cada poder novo só precisa dizer o que acontece ao ativar / encerrar.
[RequireComponent(typeof(CarroController))]
public abstract class PoderAtivo : MonoBehaviour, IPoder
{
    [Header("Loja")]
    [SerializeField] private ItemLoja itemLoja;        // item da loja que libera este poder
    [SerializeField] private bool comprado = true;     // só vale se Item Loja estiver vazio (para testar)

    [Header("Tempo")]
    [SerializeField] protected float duracao = 3f;     // 0 = efeito instantâneo
    [SerializeField] protected float recarga = 15f;

    [Header("Tecla")]
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private Key tecla = Key.B;
#else
    [SerializeField] private KeyCode tecla = KeyCode.B;
#endif
    [SerializeField] private string nomeTecla = "B";   // como aparece no HUD

    [Header("Som (opcional)")]
    [SerializeField] private AudioSource fonteAudio;
    [SerializeField] private AudioClip somAtivar;

    [Header("Eventos")]
    public UnityEvent aoAtivar;
    public UnityEvent aoEncerrar;
    public UnityEvent aoFicarPronto;

    protected CarroController carro;
    private bool ativo;
    private float restante;
    private float recargaRestante;

    public abstract string Nome { get; }
    public string Tecla => nomeTecla;
    public virtual string Info => "";
    public bool Comprado => itemLoja != null ? itemLoja.Comprado : comprado;
    public bool Ativo => ativo;
    public bool Pronto => Comprado && !ativo && recargaRestante <= 0f;

    public float Progresso => ativo
        ? (duracao > 0f ? restante / duracao : 1f)
        : 1f - recargaRestante / Mathf.Max(recarga, 0.01f);

    // Poderes que mexem no tempo do jogo contam o tempo "real"
    protected virtual bool UsaTempoReal => false;

    protected virtual void Awake()
    {
        carro = GetComponent<CarroController>();
    }

    protected virtual void Update()
    {
        // Não funciona com o jogo pausado (tutorial, pause, conclusão)
        if (Time.timeScale == 0f) return;

        float dt = UsaTempoReal ? Time.unscaledDeltaTime : Time.deltaTime;

        if (ativo)
        {
            restante -= dt;
            DuranteEfeito();
            if (restante <= 0f) Encerrar();
        }
        else if (recargaRestante > 0f)
        {
            recargaRestante -= dt;
            if (recargaRestante <= 0f)
            {
                recargaRestante = 0f;
                aoFicarPronto?.Invoke();
            }
        }

        if (Apertou())
        {
            if (Pronto) Ativar();
            else if (!Comprado) Debug.Log(Nome + " ainda não foi comprado na loja");
        }
    }

    private void Ativar()
    {
        ativo = true;
        restante = duracao;

        if (fonteAudio != null && somAtivar != null) fonteAudio.PlayOneShot(somAtivar);

        AoAtivar();
        aoAtivar?.Invoke();

        if (duracao <= 0f) Encerrar();
    }

    // Termina o efeito e começa a recarga
    protected void Encerrar()
    {
        if (!ativo) return;
        ativo = false;
        recargaRestante = recarga;
        AoEncerrar();
        aoEncerrar?.Invoke();
    }

    protected abstract void AoAtivar();
    protected virtual void AoEncerrar() { }
    protected virtual void DuranteEfeito() { }

    private bool Apertou()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard t = Keyboard.current;
        return t != null && tecla != Key.None && t[tecla].wasPressedThisFrame;
#else
        return Input.GetKeyDown(tecla);
#endif
    }
}
